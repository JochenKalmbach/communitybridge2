// QinetiQ SharePoint NNTP Server
// http://spnews.codeplex.com
// ---------------------------------------------------------------------------
// Last updated: Sep 2009
// ---------------------------------------------------------------------------
// 
// Microsoft Public License (Ms-PL)
//
// This license governs use of the accompanying software. If you use the software, you
// accept this license. If you do not accept the license, do not use the software.
//
// 1. Definitions
// The terms "reproduce," "reproduction," "derivative works," and "distribution" have the
// same meaning here as under U.S. copyright law.
// A "contribution" is the original software, or any additions or changes to the software.
// A "contributor" is any person that distributes its contribution under this license.
// "Licensed patents" are a contributor's patent claims that read directly on its contribution.
//
// 2. Grant of Rights
// (A) Copyright Grant- Subject to the terms of this license, including the license conditions and limitations in section 3, each contributor grants you a non-exclusive, worldwide, royalty-free copyright license to reproduce its contribution, prepare derivative works of its contribution, and distribute its contribution or any derivative works that you create.
// (B) Patent Grant- Subject to the terms of this license, including the license conditions and limitations in section 3, each contributor grants you a non-exclusive, worldwide, royalty-free license under its licensed patents to make, have made, use, sell, offer for sale, import, and/or otherwise dispose of its contribution in the software or derivative works of the contribution in the software.
//
// 3. Conditions and Limitations
// (A) No Trademark License- This license does not grant you rights to use any contributors' name, logo, or trademarks.
// (B) If you bring a patent claim against any contributor over patents that you claim are infringed by the software, your patent license from such contributor to the software ends automatically.
// (C) If you distribute any portion of the software, you must retain all copyright, patent, trademark, and attribution notices that are present in the software.
// (D) If you distribute any portion of the software in source code form, you may do so only under this license by including a complete copy of this license with your distribution. If you distribute any portion of the software in compiled or object code form, you may only do so under a license that complies with this license.
// (E) The software is licensed "as-is." You bear the risk of using it. The contributors give no express warranties, guarantees or conditions. You may have additional consumer rights under your local laws which this license cannot change. To the extent permitted under your local laws, the contributors exclude the implied warranties of merchantability, fitness for a particular purpose and non-infringement.
//

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Linq;
using System.Collections;
using System.Text.RegularExpressions;
using System.Timers;

namespace CommunityBridge2.NNTPServer
{
    public abstract class DataProvider : IDisposable 
    {
        private bool _newsgroupCacheValid;


        public bool InMimeUseHtml { get; set; }
        /// <summary>
        /// Gets or sets the maximum post length
        /// </summary>
        public int MaxPostLengthBytes { get; set; }

        private readonly object _maxCachedArticlesSync = new object();
        private int _maxCachedArticles = 30000;
        public int MaxCachedArticles
        {
            get
            {
                lock(_maxCachedArticlesSync)
                {
                    return _maxCachedArticles;
                }
            }
            set
            {
                lock(_maxCachedArticlesSync)
                {
                    _maxCachedArticles = value;
                    if (_maxCachedArticles < 100)
                        _maxCachedArticles = 100;
                }
            }
        }

        readonly Dictionary<string, Newsgroup> _groupList = new Dictionary<string, Newsgroup>(StringComparer.InvariantCultureIgnoreCase);

        /// <summary>
        /// Gets or sets the list of newsgroups
        /// </summary>
        public Dictionary<string, Newsgroup> GroupList
        {
            get { return _groupList; }
        }

        private readonly Timer _articlesCacheCheckTimer;

        protected DataProvider()
        {
            MaxPostLengthBytes = 1048576;
            _articlesCacheCheckTimer = new Timer {Interval = 30000};
            _articlesCacheCheckTimer.Start();
            _articlesCacheCheckTimer.Elapsed += _articlesCacheCheckTimer_Elapsed;
        }

        void _articlesCacheCheckTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            int maxArticles = MaxCachedArticles;
            int maxArticlesPerGroup;
            lock (GroupList)
            {
                if (GroupList.Count <= 0)
                    return;
                maxArticlesPerGroup = maxArticles/this.GroupList.Count;
            }
            maxArticlesPerGroup = Math.Max(100, maxArticlesPerGroup);

            // Now check each group and reduce the number of stored articles, if appropriate...))
            lock(GroupList)
            {
                foreach(var g in GroupList.Values)
                {
                    lock(g.Articles)
                    {
                        if (g.Articles.Count > 0)
                        {
                            int toManyCount = g.Articles.Count - maxArticlesPerGroup;
                            if (toManyCount > 0)
                            {
                                // First sort by "CacheDateTimeAdded"
                                var sortedArticles = g.Articles.Values.OrderBy(p => p.CacheDateTimeAdded).ToList();
                                for (int i = 0; i < toManyCount; i++)
                                    g.Articles.Remove(sortedArticles[i].Number);
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// This loads the list of newsgroups and outputs it directly to the client via the <paramref name="groupAction"/>.
        /// </summary>
        /// <param name="clientUsername"></param>
        /// <param name="groupAction"></param>
        /// <returns>Returns <c>true</c> if now exception was thrown while processing the request</returns>
        public bool GetNewsgroupListToStream(string clientUsername, Action<Newsgroup> groupAction)
        {
            return LoadNewsgroupsToStream(groupAction);
        }

        /// <summary>
        /// Returns a list of newsgroups that have been added since the given date
        /// </summary>
        /// <param name="clientUsername">Authenticated username</param>
        /// <param name="fromDate">Date threshold</param>
        /// <param name="groupAction"></param>
        /// <returns>Returns <c>true</c> if now exception was thrown while processing the request</returns>
        public abstract bool GetNewsgroupListFromDate(string clientUsername, DateTime fromDate, Action<Newsgroup> groupAction);

      /// <summary>
      /// This will return the newsgroup
      /// </summary>
      /// <param name="clientUsername">Optional username</param>
      /// <param name="groupName">The name of the group to retrive</param>
      /// <param name="updateFirstLastNumber">If this is <c>true</c> the FirstArticle and LastArticle of the newsgroup should be actualized! If it is <c>false</c> just a refeence to the latest cache group is nesseccary.</param>
      /// <param name="exceptionOccured">Specifies if the group was not found or if an exeption was catched if the return value is <c>null</c></param>
      /// <returns></returns>
      public abstract Newsgroup GetNewsgroup(string clientUsername, string groupName, bool updateFirstLastNumber, out bool exceptionOccured);


        /// <summary>
        /// Returns the article based on the given ID
        /// </summary>
        /// <param name="clientUsername">Authenticated username</param>
        /// <param name="groupName">Newsgroup name</param>
        /// <param name="articleId">ID of the article</param>
        /// <returns>Article object</returns>
        public abstract Article GetArticleById(string clientUsername, string groupName, string articleId);

        /// <summary>
        /// Returns the article based on the given number
        /// </summary>
        /// <param name="clientUsername">Authenticated username</param>
        /// <param name="groupName">Newsgroup name</param>
        /// <param name="articleNumber">Article number</param>
        /// <returns>Article object</returns>
        public abstract Article GetArticleByNumber(string clientUsername, string groupName, int articleNumber);

        /// <summary>
        /// Gets the next article in the list from the given position
        /// </summary>
        /// <param name="clientUsername">Authenticated username</param>
        /// <param name="groupName">Newsgroup name</param>
        /// <param name="articleNumber">Number of the article</param>
        /// <returns>Article object</returns>
        public Article GetNextArticleByNumber(string clientUsername, string groupName, int articleNumber)
        {
          bool exceptionOccured;
          Newsgroup group = GetNewsgroup(clientUsername, groupName, true, out exceptionOccured);

            int newArticleNo = articleNumber + 1;
            while (newArticleNo <= group.LastArticle)
            {
                var a = GetArticleByNumber(clientUsername, groupName, newArticleNo);
                if (a != null)
                    return a;
                newArticleNo++;
            }
            return null;
        }

        /// <summary>
        /// Gets the next article in the list from the given position
        /// </summary>
        /// <param name="clientUsername">Authenticated username</param>
        /// <param name="groupName">Newsgroup name</param>
        /// <param name="articleId">ID of the article</param>
        /// <returns>Article object</returns>
        public Article GetNextArticleById(string clientUsername, string groupName, string articleId)
        {
            var a = GetArticleById(clientUsername, groupName, articleId);
            if (a == null) return null;
            return GetNextArticleByNumber(clientUsername, groupName, a.Number);
        }

        /// <summary>
        /// Gets the last (previous) article in the list
        /// </summary>
        /// <param name="clientUsername">Authenticated username</param>
        /// <param name="groupName">Newsgroup name</param>
        /// <returns>Article object</returns>
        public Article GetLastArticleByNumber(string clientUsername, string groupName, int articleNumber)
        {
            bool exceptionOccured;
            Newsgroup group = GetNewsgroup(clientUsername, groupName, true, out exceptionOccured);

            int newArticleNo = articleNumber - 1;
            while (newArticleNo >= group.FirstArticle)
            {
                var a = GetArticleByNumber(clientUsername, groupName, newArticleNo);
                if (a != null)
                    return a;
                newArticleNo--;
            }
            return null;
        }

        public Article GetLastArticleById(string clientUsername, string groupName, string articleId)
        {
            var a = GetArticleById(clientUsername, groupName, articleId);
            if (a == null) return null;
            return GetLastArticleByNumber(clientUsername, groupName, a.Number);
        }

        public abstract void GetArticlesByNumberToStream(string clientUsername, string groupName, int firstArticle, int lastArticle, Action<IList<Article>> articlesProgressAction);

        /// <summary>
        /// Returns the article body
        /// </summary>
        /// <param name="clientUsername">Authenticated username</param>
        /// <param name="groupName">Newsgroup name</param>
        /// <param name="article">Article object</param>
        /// <returns>Article body</returns>
        public string GetArticleBodyResponseText(string clientUsername, string groupName, Article article)
        {
            return article.GetArticleBodyResponseText();
        }

        /// <summary>
        /// Returns all available newsgroups via the <paramref name="groupAction"/>.
        /// </summary>
        /// <param name="groupAction">Callback for each group</param>
        /// <returns>Returns <c>true</c> if now exception was thrown while processing the request</returns>
        protected abstract bool LoadNewsgroupsToStream(Action<Newsgroup> groupAction);

        /// <summary>
        /// Sets a flag to specify that the newsgroup cache is valid
        /// </summary>
        public void SetNewsgroupCacheValid()
        {
            _newsgroupCacheValid = true;
        }

        /// <summary>
        /// Returns the flag to indicate if the newsgroup cache is valid or not
        /// </summary>
        /// <returns></returns>
        public bool IsNewsgroupCacheValid()
        {
            return _newsgroupCacheValid;
        }

        public abstract Newsgroup GetNewsgroupFromCacheOrServer(string groupName);

        private static readonly Regex _charSetRegex = new Regex(@"charset=""{0,1}([^\s^""^;]+)""{0,1}", RegexOptions.CultureInvariant | RegexOptions.Compiled | RegexOptions.Singleline | RegexOptions.IgnoreCase);
        private static readonly Regex _bondaryRegex = new Regex(@"\s*boundary=([^\s^;]*)(;|\s|$)", RegexOptions.CultureInvariant | RegexOptions.Compiled | RegexOptions.Singleline | RegexOptions.IgnoreCase);

        /// <summary>
        /// Persists an article to the backing store
        /// </summary>
        /// <param name="clientUsername">Authenticated username</param>
        /// <param name="data">Article data posted from NNTP client</param>
        /// <returns>String which will be returned to the client</returns>
        public string PostArticle(string clientUsername, string data)
        {
            try
            {
                if (data.Length > MaxPostLengthBytes)
                {
                    //System.Diagnostics.Trace.WriteLine("Posting failed - maximum post length exceeded");
                    //return PostStatus.FailedExcessiveLength;
                    return GeneralResponses.PostingFailedExcessiveLength;
                }

                var articleBody = new StringBuilder();
                var article = new Article {ContentType = string.Empty};
                char[] delimiter = { '\r' };
                bool isHeader = true;
                bool isMimePost = false;

                // undo dot stuffing
                //data = data.Replace("\r\n..\r\n", "\r\n.\r\n");
                //data = data.Replace("\r\n..", "\r\n.");
                string[] lines = data.Split(delimiter);
                //Regex extendedChars = new Regex("[^\\u0009-\\u007e]", RegexOptions.Multiline | RegexOptions.IgnoreCase);

                string lastKeyValue = string.Empty;
                string line;
                foreach (var lineIter in lines)
                {
                    line = lineIter.Replace("\n", string.Empty);
                    //line = extendedChars.Replace(line, string.Empty);
                    //System.Diagnostics.Trace.WriteLine("Processing line -|" + line + "|");
                    if (isHeader && string.IsNullOrEmpty(line))
                    {
                        isHeader = false;
                        continue;
                    }

                    if (isHeader)
                    {
                        var nameValuePair = new ArrayList(2);
                        int sepPos = line.IndexOf(": ");
                        if ((sepPos > 0) && (line.IndexOf(" ") > 0))
                        {
                            nameValuePair.Add(line.Substring(0, sepPos).Trim());
                            nameValuePair.Add(line.Substring(sepPos + 2).Trim());
                            lastKeyValue = nameValuePair[0].ToString();
                        }
                        else
                        {
                            nameValuePair.Add(string.Empty);
                            nameValuePair.Add(line);
                        }
                        
                        string keyValue = nameValuePair[0].ToString();
                        if (string.Compare(keyValue, HeaderNames.From, StringComparison.InvariantCultureIgnoreCase) == 0)
                        {
                            article.From = nameValuePair[1].ToString();
                        }
                        else if (string.Compare(keyValue, HeaderNames.Date, StringComparison.InvariantCultureIgnoreCase) == 0)
                        {
                            article.Date = nameValuePair[1].ToString();
                        }
                        else if (string.Compare(keyValue, HeaderNames.Subject, StringComparison.InvariantCultureIgnoreCase) == 0)
                        {
                            // Also support newreaders with post the "subject" in multiple lines
                            article.Subject = nameValuePair[1].ToString();
                        }
                        else if (string.Compare(keyValue, HeaderNames.Newsgroups, StringComparison.InvariantCultureIgnoreCase) == 0)
                        {
                            article.Newsgroups = nameValuePair[1].ToString().Replace("\"", string.Empty).Replace("'", string.Empty);
                        }
                        else if (string.Compare(keyValue, HeaderNames.UserAgent, StringComparison.InvariantCultureIgnoreCase) == 0)
                        {
                            article.UserAgent = nameValuePair[1].ToString();
                        }
                        else if (string.Compare(keyValue, HeaderNames.XNewsreader, StringComparison.InvariantCultureIgnoreCase) == 0)
                        {
                            if (string.IsNullOrEmpty(article.UserAgent))
                                article.UserAgent = nameValuePair[1].ToString();  // The "User-Agent" is embedded into the html, so either use "User-Agent" or "X-Newsreader"
                            article.XNewsreader = nameValuePair[1].ToString();
                        }
                            // TODO: Also support of "X-Mailer:" !?
                        else if (string.Compare(keyValue, HeaderNames.References, StringComparison.InvariantCultureIgnoreCase) == 0)
                        {
                            article.References = nameValuePair[1].ToString();
                        }
                        else if (string.Compare(keyValue, HeaderNames.ContentType, StringComparison.InvariantCultureIgnoreCase) == 0)
                        {
                            // Also support newreaders with post the "content-type" in multiple lines, like:
                            // Content-Type: text/plain;
                            //      format=flowed;
                            //      charset="iso-8859-1";
                            //      reply-type=original

                            article.ContentType = nameValuePair[1].ToString();
                        }
                        else if (string.Compare(keyValue, HeaderNames.ContentTransferEncoding, StringComparison.InvariantCultureIgnoreCase) == 0)
                        {
                            article.ContentTransferEncoding = nameValuePair[1].ToString();
                        }
                        else if (keyValue.Length == 0)
                        {
                          // Multi-Line Header:
                          if (string.Compare(lastKeyValue, HeaderNames.From, StringComparison.InvariantCultureIgnoreCase) == 0)
                          {
                            article.From += nameValuePair[1].ToString();
                          }
                          else if (string.Compare(lastKeyValue, HeaderNames.Date, StringComparison.InvariantCultureIgnoreCase) == 0)
                          {
                            article.Date += nameValuePair[1].ToString();
                          }
                          else if (string.Compare(lastKeyValue, HeaderNames.Subject, StringComparison.InvariantCultureIgnoreCase) == 0)
                          {
                            article.Subject += nameValuePair[1].ToString();
                          }
                          else if (string.Compare(lastKeyValue, HeaderNames.Newsgroups, StringComparison.InvariantCultureIgnoreCase) == 0)
                          {
                            article.Newsgroups += nameValuePair[1].ToString().Replace("\"", string.Empty).Replace("'", string.Empty);
                          }
                          else if (string.Compare(lastKeyValue, HeaderNames.UserAgent, StringComparison.InvariantCultureIgnoreCase) == 0)
                          {
                            article.UserAgent += nameValuePair[1].ToString();
                          }
                          else if (string.Compare(lastKeyValue, HeaderNames.XNewsreader, StringComparison.InvariantCultureIgnoreCase) == 0)
                          {
                            //if (string.IsNullOrEmpty(article.UserAgent))
                            //  article.UserAgent = nameValuePair[1].ToString();  // The "User-Agent" is embedded into the html, so either use "User-Agent" or "X-Newsreader"
                            article.XNewsreader += nameValuePair[1].ToString();
                          }
                          // TODO: Also support of "X-Mailer:" !?
                          else if (string.Compare(lastKeyValue, HeaderNames.References, StringComparison.InvariantCultureIgnoreCase) == 0)
                          {
                            article.References += nameValuePair[1].ToString();
                          }
                          else if (string.Compare(lastKeyValue, HeaderNames.ContentType, StringComparison.InvariantCultureIgnoreCase) == 0)
                          {
                            article.ContentType += nameValuePair[1].ToString();
                          }
                          else if (string.Compare(lastKeyValue, HeaderNames.ContentTransferEncoding, StringComparison.InvariantCultureIgnoreCase) == 0)
                          {
                            article.ContentTransferEncoding += nameValuePair[1].ToString();
                          }
                        }
                        else
                        {
                        }
                    }  // isHeader
                    else
                    {
                        // Body:
                        // undo dot stuff (remove the first dott, if there are two dots...
                        if (line.IndexOf("..", StringComparison.InvariantCultureIgnoreCase) == 0)
                            line = line.Substring(1);
                        articleBody.Append(line + "\n");
                    }
                }  // foreach

                article.Body = articleBody.ToString();

                // Check for mimePostings:
                if (article.ContentType.IndexOf("multipart", StringComparison.InvariantCultureIgnoreCase) == 0)
                {
                    isMimePost = true;
                }


                if (isMimePost)
                {
                    var mime = new Mime {Text = article.Body};

                    // Exract boundary:
                    var m2 = _bondaryRegex.Match(article.ContentType);
                    if (m2.Success)
                        mime.Boundary = m2.Groups[1].Value;

                    string textPlain = null;
                    string textPlainContentType = null;
                    string textHtml = null;
                    string textHtmlContentType = null;

                    foreach (var mimePart in mime.MimeParts)
                    {
                        var ct = mimePart.GetPropertyValue("Content-Type");
                        if (ct.IndexOf("text/plain", StringComparison.InvariantCultureIgnoreCase) >= 0)
                        {
                            textPlainContentType = ct;
                            textPlain = (string)mimePart.Decode();
                        }
                        if (ct.IndexOf("text/html", StringComparison.InvariantCultureIgnoreCase) >= 0)
                        {
                            textHtmlContentType = ct;
                            textHtml = (string)mimePart.Decode();
                        }
                        if ((textPlain != null) && (textHtml != null))
                            break;
                    }

                    if ((textPlain == null) && (textHtml == null))
                    {
                        //System.Diagnostics.Trace.WriteLine("Posting failed - text part not found");
                        //return PostStatus.FailedTextPartMissingInHtml;
                        return GeneralResponses.PostingFailedTextPartMissingInMime;
                    }
                    if (InMimeUseHtml && (textHtml != null))
                    {
                        article.Body = textHtml;
                        article.ContentType = textHtmlContentType;
                        Traces.NntpServerTraceEvent(TraceEventType.Verbose, "MIME-Part: HTML selected");
                    }
                    else
                    {
                        if (textPlain != null)
                        {
                            article.Body = textPlain;
                            article.ContentType = textPlainContentType;
                            Traces.NntpServerTraceEvent(TraceEventType.Verbose, "MIME-Part: plain/text selected");
                        }
                        else
                        {
                            article.Body = textHtml;
                            article.ContentType = textHtmlContentType;
                            Traces.NntpServerTraceEvent(TraceEventType.Verbose, "MIME-Part: HTML selected (no plain/text available)");
                        }
                    }
                }

                // Transcode the "body" according to the "charset", if one is specified:
                var charSetMatch = _charSetRegex.Match(article.ContentType);
                string charSet = Server.EncodingRecv.HeaderName;  // default
                if (charSetMatch.Success)
                {
                    charSet = charSetMatch.Groups[1].Value;
                }

                if (isMimePost == false)
                {
                    if (article.ContentTransferEncoding.IndexOf("quoted-printable", StringComparison.InvariantCultureIgnoreCase) >= 0)
                    {
                        article.Body = MimePart.DecodeQuotedPrintable(article.Body);
                    }
                    else if (article.ContentTransferEncoding.IndexOf("base64", StringComparison.InvariantCultureIgnoreCase) >= 0)
                    {
                        article.Body = MimePart.DecodeBase64(article.Body, charSet);
                    }
                }

                // Re-Encode after all data is now in the body...
                ReEncode(article, charSet);

                // post must have subject
                if (article.Subject.Trim().Length == 0)
                {
                    //System.Diagnostics.Trace.WriteLine("Posting failed - no subject line");
                    //return PostStatus.FailedSubjectLineBlank;
                    return GeneralResponses.PostingFailedSubjectLineBlank;
                }

                // Decode Subject
                article.Subject = Mime.DecodeEncodedWordValue(article.Subject);

                var articles = new List<Article>();

                // Disabled cross posting (2010-06-06)
                //// Cross-Postings (Multiple-Postings) are allowed for "primary messages":
                //if ( (string.IsNullOrEmpty(article.References)) && (article.Newsgroups.IndexOf(",", StringComparison.InvariantCultureIgnoreCase) > 0) )
                //{
                //    string[] groupNames = article.Newsgroups.Split(',');
                //    foreach (var groupName in groupNames)
                //    {
                //        Newsgroup group = GetNewsgroupFromCacheOrServer(groupName.Trim());
                //        if (group != null && group.PostingAllowed)
                //        {
                //            // copy the article...
                //            var crossPostArticle = new Article();
                //            crossPostArticle.From = article.From;
                //            crossPostArticle.Body = article.Body;
                //            crossPostArticle.Date = article.Date;
                //            crossPostArticle.Subject = article.Subject;
                //            crossPostArticle.ParentNewsgroup = group.GroupName;
                //            //crossPostArticle.References = article.References;
                //            crossPostArticle.Newsgroups = article.Newsgroups;
                //            crossPostArticle.UserAgent = article.UserAgent;
                //            crossPostArticle.ContentType = article.ContentType;
                //            crossPostArticle.ContentTransferEncoding = article.ContentTransferEncoding;
                //            crossPostArticle.XNewsreader = article.XNewsreader;

                //            // add cross-posted footnote
                //            //crossPostArticle.Body += "\n\n[X-Posted To: " + article.Newsgroups + "]\n";

                //            articles.Add(crossPostArticle);
                //        }
                //        else
                //        {
                //            // indicate posting failure
                //            //System.Diagnostics.Trace.WriteLine("Posting failed - user [" + clientUsername + "] does not have permission to post to group [" + groupName + "]");
                //            return PostStatus.FailedAccessDenied;
                //        }
                //    }  // foreach

                //    if (articles.Count <= 0)
                //    {
                //        return PostStatus.FailedGroupNotFound;
                //    }
                //    else
                //    {
                //        SaveArticles(clientUsername, articles);
                //    }
                //}
                //else
                {
                    // Only one group or a reply:
                    if (article.Newsgroups.IndexOf(",", StringComparison.InvariantCultureIgnoreCase) > 0)
                    {
                        // Cross-Post are not allowed fro answers!
                        return GeneralResponses.PostingFailedAccessDeniedMultipost;
                    }
                    var group = GetNewsgroupFromCacheOrServer(article.Newsgroups);
                    if (group != null && group.PostingAllowed)
                    {
                        article.ParentNewsgroup = group.GroupName;
                        articles.Add(article);
                        SaveArticles(clientUsername, articles);
                    }
                    else
                    {
                        // indicate posting failure
                        if (group != null)
                        {
                            //System.Diagnostics.Trace.WriteLine("Posting failed - user [" + clientUsername + "] does not have permission to post to group [" + group.GroupName + "]");
                            //return PostStatus.FailedAccessDenied;
                            return GeneralResponses.PostingFailedAccessDenied;
                        }
                        //System.Diagnostics.Trace.WriteLine("Posting failed - newsgroup [" + article.Newsgroups + "] could not be found");
                        //return PostStatus.FailedGroupNotFound;
                        return GeneralResponses.PostingFailedGroupNotFound;
                    }
                }

                return GeneralResponses.ArticlePostedOk;
            }
            catch (Exception exp)
            {
                //System.Diagnostics.Trace.WriteLine("Error in DataProvider.PostArticle - " + ex.Message + " :: " + ex.StackTrace);
                Traces.NntpServerTraceEvent(System.Diagnostics.TraceEventType.Critical, "Error in DataProvider.PostArticle: {0}", Traces.ExceptionToString(exp));

                var resp = string.Format("441 posting failed {0}\r\n", NntpServer.GetErrorResponseFromExeption(exp));

                return resp;
            }
        }

        private void ReEncode(Article article, string charSet)
        {
            try
            {
                var enc = Encoding.GetEncoding(charSet);
                if (enc.CodePage != Server.EncodingRecv.CodePage)
                {
                    var raw = Server.EncodingRecv.GetBytes(article.Body);
                    var str = enc.GetString(raw);
                    article.Body = str;
                }
            }
            catch (Exception exp)
            {
                Traces.NntpServerTraceEvent(
                    TraceEventType.Critical, 
                    "PostArticle: Could not convert into the desired charset: {0}, {1}",
                    article.ContentType,
                    Traces.ExceptionToString(exp));
            }
        }

        protected abstract void SaveArticles(string clientUsername, List<Article> articles);

        #region IDisposable Members

        private bool _disposed;

        private void Dispose(bool disposing)
        {
            if (!(_disposed))
            {
                if (disposing) 
                {
                    CleanUp();
                }
            } 
            _disposed = true;
        }  
        
        public void Dispose()
        { 
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected void CleanUp()
        {
            //if (_articlesInvalidationTimer != null)
            //{
            //    _articlesInvalidationTimer.Dispose();
            //}
        }
        
        #endregion

      public event ProgressDataEventHandler ProgressData;
      protected void OnProgressData(string text)
      {
        ProgressDataEventHandler handler = ProgressData;
        if (handler != null)
          handler(this, new ProgressDataEventArgs {Text = text});
      }
    }

  public class ProgressDataEventArgs : EventArgs
  {
    public string Text;
  }

  public delegate void ProgressDataEventHandler(object sender, ProgressDataEventArgs e);
}
