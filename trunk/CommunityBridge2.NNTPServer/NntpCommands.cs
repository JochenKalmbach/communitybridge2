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

namespace CommunityBridge2.NNTPServer
{
    // ReSharper disable InconsistentNaming
    public enum Command
    {
        ARTICLE,
        BODY,
        HEAD,
        STAT,
        NEWNEWS,
        GROUP,
        HELP,
        IHAVE,
        LAST,
        LIST,
        NEXT,
        NEWGROUPS,
        POST,
        POSTDATA,
        QUIT,
        SLAVE,
        NOTRECOGNISED,
        SYNTAXERROR,
        ADHOC,
        XHDR,
        ENDOFDATA,
        AUTHINFO,
        XOVER,
        MODE,
        DATE,
        LISTGROUP
    }
    // ReSharper restore InconsistentNaming
    
    public abstract class NntpCommand
    {
        private readonly string _transferCompleteResponse = string.Empty;

        public NntpCommand()
        {
            ClientUsername = string.Empty;
        }

        public DataProvider DataProvider
        {
            get 
            { 
                return Provider; 
            }
            set 
            { 
                Provider = value; 
            }
        }

        public Command Command { get; set; }

        public DataProvider Provider { get; set; }

        public virtual string Parse(string parameters, Action<string> writeAction, Client client)
        {
            PerfCounters.IncrementCounter(PerfCounterName.ResponseCommandNotRecognised);
            return GeneralResponses.CommandNotRecognised;
        }

        public string TransferCompleteResponse
        {
            get
            {
                return _transferCompleteResponse;
            }
        }

        public bool PostingAllowed { get; set; }

        public bool PostCancelled
        {
            get
            {
                return CancelPost;
            }
        }

        public bool CancelPost { get; set; }

        public string ClientUsername { get; set; }

        public byte[] AuthToken { get; set; }

        protected static void ValidateArgument(string argName, string argValue)
        {
            if (argValue == null)
            {
                throw new ArgumentNullException(argName, "Argument is null");
            }
            if (argValue.Trim().Length == 0)
            {
                throw new ArgumentException("Argument is empty", argName);
            }
        }
    }

    public class NntpCommandNotRecognised : NntpCommand
    {
        public NntpCommandNotRecognised()
        {
            Command = Command.NOTRECOGNISED;
        }
    }

    public class NntpCommandSyntaxError : NntpCommand
    {
        private readonly string _reason;

        public NntpCommandSyntaxError(string reason)
        {
            Command = Command.SYNTAXERROR;
            _reason = reason;
        }

        public override string Parse(string parameters, Action<string> writeAction, Client client)
        {
            PerfCounters.IncrementCounter(PerfCounterName.ResponseCommandSyntaxError);
            return GeneralResponses.CommandSyntaxError;
        }
    }

    public class NntpCommandAdHoc : NntpCommand
    {
        private readonly string _response = string.Empty;

        public NntpCommandAdHoc(string response)
        {
            Command = Command.ADHOC;
            _response = response;
        }

        public override string Parse(string parameters, Action<string> writeAction, Client client)
        {
            return _response;
        }
    }

    public class NntpCommandStat : NntpCommandArticle
    {
        public NntpCommandStat(string groupName) : base(groupName)
        {
            Command = Command.STAT;
            GroupName = groupName;
        }
    }

    public class NntpCommandXHdr : NntpCommand
    {
        private readonly string _groupName = string.Empty;

        public NntpCommandXHdr(string groupName)
        {
            Command = Command.XHDR;
            _groupName = groupName;
        }

        public override string Parse(string parameters, Action<string> writeAction, Client client)
        {
            //System.Diagnostics.Trace.WriteLine("Parsing XHDR");
            
            if (_groupName.Trim().Length == 0)
            {
                PerfCounters.IncrementCounter(PerfCounterName.ResponseArticleRetrievedNoGroupSelected);
                return GeneralResponses.ArticleRetrievedNoGroupSelected;
            }

            char[] delimiter = { ' ' };
            string[] parametersSplit = parameters.Split(delimiter);

            if (parametersSplit.Length == 0)
            {
                PerfCounters.IncrementCounter(PerfCounterName.ResponseCommandSyntaxError);
                return GeneralResponses.CommandSyntaxError;
            }

            // Check if the group exists
          bool exceptionOccured;
          if (DataProvider.GetNewsgroup(ClientUsername, _groupName, false, out exceptionOccured) == null)
            {
              if (exceptionOccured)
              {
                //PerfCounters.IncrementCounter(PerfCounterName.Of);
                return GeneralResponses.ServerArchiveOffline;
              }
              else
              {
                PerfCounters.IncrementCounter(PerfCounterName.ResponseNoSuchGroup);
                return GeneralResponses.NoSuchGroup;
              }
            } 
            
            string headerName = parametersSplit[0];
            int firstArticle = 0;
            int lastArticle = 0;
            int articleNumber = 0;
            string articleId = string.Empty;
            bool replyWithArticleId = false;

            if (parametersSplit.Length == 2)
            {
                if (parametersSplit[1].StartsWith("<") && parametersSplit[1].EndsWith(">"))
                {
                    articleId = parametersSplit[1];
                    replyWithArticleId = true;
                }
                else
                {
                    if (parametersSplit[1].IndexOf("-") > 0)
                    {
                        firstArticle = Convert.ToInt32(parametersSplit[1].Substring(0, parametersSplit[1].IndexOf("-")));
                        string lastArticleStr = parametersSplit[1].Substring(parametersSplit[1].IndexOf("-") + 1);

                        if (lastArticleStr.Length == 0)
                        {
                            // Get the actual LastArticle number:
                          bool exceptionOccured2;
                            var g = DataProvider.GetNewsgroup(ClientUsername, _groupName, true, out exceptionOccured2);
                            if (g == null)
                            {
                              if (exceptionOccured2)
                                return GeneralResponses.ServerArchiveOffline;
                              PerfCounters.IncrementCounter(PerfCounterName.ResponseNoSuchGroup);
                              return GeneralResponses.NoSuchGroup;
                            }
                            lastArticle = g.LastArticle;
                        }
                        else
                        {
                            lastArticle = Convert.ToInt32(lastArticleStr);
                        }
                    }
                    else
                    {
                        articleNumber = Convert.ToInt32(parametersSplit[1]);
                    }
                }
            }
            else
            {
                if (client.ArticleReference.Trim().Length == 0)
                {
                    PerfCounters.IncrementCounter(PerfCounterName.ResponseArticleRetrievedNoArticleSelected);
                    return GeneralResponses.ArticleRetrievedNoArticleSelected;
                }
                var articleTemp = new Article();
                articleTemp.ArticleReference = client.ArticleReference;
                articleId = articleTemp.Id;
                articleNumber = articleTemp.Number;
            }

            var articles = new Dictionary<int, Article>();
            Article article;

            if (articleId.Length > 0)
            {
                article = DataProvider.GetArticleById(ClientUsername, _groupName, articleId);

                if (article == null)
                {
                    PerfCounters.IncrementCounter(PerfCounterName.ResponseArticleRetrievedNoArticleInGroup);
                    return GeneralResponses.ArticleRetrievedNoArticleInGroup;
                }

                articles.Add(article.Number, article);
            }
            else if (articleNumber != 0)
            {
                article = DataProvider.GetArticleByNumber(ClientUsername, _groupName, articleNumber);

                if (article == null)
                {
                    PerfCounters.IncrementCounter(PerfCounterName.ResponseArticleRetrievedNoArticleInGroup);
                    return GeneralResponses.ArticleRetrievedNoArticleInGroup;
                }

                articles.Add(article.Number, article);
            }
            else
            {
                bool firstResponse = true;
                // range of articles
                DataProvider.GetArticlesByNumberToStream(ClientUsername, _groupName, firstArticle, lastArticle, 
                    p =>
                        {
                            if (firstResponse)
                            {
                                PerfCounters.IncrementCounter(PerfCounterName.ResponseHeaderFollows);
                                writeAction(GeneralResponses.HeaderFollows);
                                firstResponse = false;
                            }
                            var sb = new StringBuilder();
                            foreach (var a in p)
                            {
                                sb.Length = 0;
                                sb.Append(a.Number.ToString());
                                sb.Append(" ");
                                sb.Append(a.GetHeaderByName(headerName).Value);
                                sb.Append("\r\n");
                                writeAction(sb.ToString());
                            }
                        }
                    );

                if (firstResponse == false)
                {
                    //System.Diagnostics.Trace.WriteLine("Done GetArticlesByNumber");
                    writeAction(GeneralResponses.ResponseEnd);
                    return string.Empty;
                }

                PerfCounters.IncrementCounter(PerfCounterName.ResponseArticleRetrievedNoArticleInGroup);
                return GeneralResponses.ArticleRetrievedNoArticleInGroup;
            }

            var response = new StringBuilder();

            PerfCounters.IncrementCounter(PerfCounterName.ResponseHeaderFollows);
            response.Append(GeneralResponses.HeaderFollows);

            foreach (var key in articles)
            {
                // It must always return the *Article Number*, except if an articleId was provided...
                if ( (replyWithArticleId) && (articleId.Length != 0))
                {
                    response.Append(key.Value.Id);
                }
                else
                {
                    response.Append(key.Value.Number.ToString());
                }
                response.Append(" " + key.Value.GetHeaderByName(headerName).Value + "\r\n");
            }

            response.Append(GeneralResponses.ResponseEnd);

            //System.Diagnostics.Trace.WriteLine("Response generated");

            return response.ToString();
        }
    }

    public class NntpCommandXOver : NntpCommand
    {
        private readonly string _groupName = string.Empty;

        public NntpCommandXOver(string groupName)
        {
            Command = Command.XOVER;
            _groupName = groupName;
        }

        public override string Parse(string parameters, Action<string> writeAction, Client client)
        {
            if (_groupName.Trim().Length == 0)
            {
                PerfCounters.IncrementCounter(PerfCounterName.ResponseArticleRetrievedNoGroupSelected);
                return GeneralResponses.ArticleRetrievedNoGroupSelected;
            }

            bool exceptionOccured;
            if (DataProvider.GetNewsgroup(ClientUsername, _groupName, false, out exceptionOccured) == null)
            {
              if (exceptionOccured)
                return GeneralResponses.ServerArchiveOffline;
              PerfCounters.IncrementCounter(PerfCounterName.ResponseNoSuchGroup);
              return GeneralResponses.NoSuchGroup;
            }

            var firstArticle = 0;
            var lastArticle = 0;
            var articleNumber = 0;
            var articleId = string.Empty;

            // For more info about the parameter see:
            // In RFC 2980 it is specified as:
            //   The optional range argument may be any of the following:
            //   (1) an article number
            //   (2) an article number followed by a dash to indicate all following
            //   (3) an article number followed by a dash followed by another article number

            if (parameters.Length > 0)
            {
                string[] parts = parameters.Split(new[] {'-', ' '});
                if (parameters.IndexOf("<") == 0)
                {
                    // The parameter is an article Id, so use this to get the article:
                    articleId = parameters.Trim();
                }
                else if (parts.Length > 1)
                {
                    firstArticle = Convert.ToInt32(parts[0]);
                    var lastArticleStr = parts[1];

                    if (lastArticleStr.Trim().Length == 0)
                    {
                        // Check if the separator was a dash (do not allow a space as last separator)
                        if (parameters.IndexOf('-') < 0)
                        {
                            articleNumber = Convert.ToInt32(parameters);
                        }
                        else
                        {
                          bool exceptionOccured3;
                          var g = DataProvider.GetNewsgroup(ClientUsername, _groupName, true, out exceptionOccured3);
                            if (g == null)
                            {
                              if (exceptionOccured3)
                                return GeneralResponses.ServerArchiveOffline;
                              PerfCounters.IncrementCounter(PerfCounterName.ResponseNoSuchGroup);
                              return GeneralResponses.NoSuchGroup;
                            }
                            lastArticle = g.LastArticle;
                        }
                    }
                    else
                    {
                        lastArticle = Convert.ToInt32(lastArticleStr);
                    }
                }
                else
                {
                    articleNumber = Convert.ToInt32(parameters);
                    //RevisedArticleReference = articleNumber.ToString();
                }
            }
            else
            {
                var articleTemp = new Article();
                articleTemp.ArticleReference = client.ArticleReference;
                articleId = articleTemp.Id;
                articleNumber = articleTemp.Number;
            }

            var articles = new Dictionary<int, Article>();
            Article article = null;

            if (articleId.Length > 0)
            {
                article = DataProvider.GetArticleById(ClientUsername, _groupName, articleId);

                if (article == null)
                {
                    PerfCounters.IncrementCounter(PerfCounterName.ResponseArticleRetrievedNoArticleInGroup);
                    return GeneralResponses.ArticleRetrievedNoArticleInGroup;
                }

                articles.Add(article.Number, article);
            }
            else if (articleNumber != 0)
            {
                article = DataProvider.GetArticleByNumber(ClientUsername, _groupName, articleNumber);

                if (article == null) 
                {
                    PerfCounters.IncrementCounter(PerfCounterName.ResponseArticleRetrievedNoArticleInGroup);
                    return GeneralResponses.ArticleRetrievedNoArticleInGroup;
                }

                articles.Add(article.Number, article);
            }
            else
            {
                // range of articles
                bool firstResponse = true;
                // range of articles
                //System.Diagnostics.Trace.WriteLine("Calling GetArticlesByNumber");
                DataProvider.GetArticlesByNumberToStream(ClientUsername, _groupName, firstArticle, lastArticle,
                    p =>
                    {
                        if ((p != null) && (p.Count > 0))
                        {
                            if (firstResponse)
                            {
                                PerfCounters.IncrementCounter(PerfCounterName.ResponseOverviewInformationFollows);
                                writeAction(GeneralResponses.OverviewInformationFollows);
                                firstResponse = false;
                            }
                            var sb = new StringBuilder();
                            foreach (var a in p)
                            {
                                sb.Length = 0;
                                sb.Append(a.Number);
                                sb.Append("\t");
                                sb.Append(a.Subject);
                                sb.Append("\t");
                                sb.Append(a.From);
                                sb.Append("\t");
                                sb.Append(a.Date);
                                sb.Append("\t");
                                sb.Append(a.Id);
                                sb.Append("\t");
                                sb.Append(a.References);
                                sb.Append("\t");
                                sb.Append(a.Size);
                                sb.Append("\t");
                                sb.Append(a.Lines);
                                sb.Append("\t");
                                sb.Append(a.XRef);
                                sb.Append("\r\n");
                                writeAction(sb.ToString());
                            }
                        }
                    }
                    );

                if (firstResponse == false)
                {
                    // at least one response sent...
                    writeAction(GeneralResponses.ResponseEnd);
                    return string.Empty;
                }

                // No response sent.. so no articles found...
                PerfCounters.IncrementCounter(PerfCounterName.ResponseArticleRetrievedNoArticleInGroup);
                return GeneralResponses.ArticleRetrievedNoArticleInGroup;
            }

            var response = new StringBuilder();

            PerfCounters.IncrementCounter(PerfCounterName.ResponseOverviewInformationFollows);
            response.Append(GeneralResponses.OverviewInformationFollows);

            foreach (KeyValuePair<int, Article> key in articles)
            {
                Article articleMatch = key.Value;
                response.Append(articleMatch.Number + "\t");
                response.Append(articleMatch.Subject + "\t");
                response.Append(articleMatch.From + "\t");
                response.Append(articleMatch.Date + "\t");
                response.Append(articleMatch.Id + "\t");
                response.Append(articleMatch.References + "\t");
                response.Append(articleMatch.Size + "\t");
                response.Append(articleMatch.Lines + "\t");
                response.Append(articleMatch.XRef);
                response.Append("\r\n");
            }

            response.Append(GeneralResponses.ResponseEnd);

            return response.ToString();
        }
    }

    public class NntpCommandArticle : NntpCommand
    {
        public NntpCommandArticle(string groupName)
        {
            Command = Command.ARTICLE;
            GroupName = groupName;
        }

        public string GroupName { get; set; }

        public Encoding ToClientEncoding { get; set; }

        public override string Parse(string parameters, Action<string> writeAction, Client client)
        {
            var article = new Article();

            if (parameters.Trim().Length == 0)
            {
                if (client.ArticleReference.Trim().Length == 0)
                {
                    PerfCounters.IncrementCounter(PerfCounterName.ResponseArticleRetrievedNoArticleSelected);
                    return GeneralResponses.ArticleRetrievedNoArticleSelected;
                }
                article.ArticleReference = client.ArticleReference;
            }
            else
            {
                try
                {
                    // This might throw an exception, if the passed parameter
                    // is not a <...> (id) and the number is not a number
                    // For example "ARTICLE a" will throw an exception here...
                    article.ArticleReference = parameters;
                }
                catch (Exception exp)
                {
                    Traces.NntpServerTraceEvent(System.Diagnostics.TraceEventType.Warning, client, "Article.Parse: Could not convert parameter {0} to ArticleReference: {1}", parameters, Traces.ExceptionToString(exp));
                    PerfCounters.IncrementCounter(PerfCounterName.ResponseArticleRetrievedNoArticle);
                    return GeneralResponses.NotAnArticleIdOrNumber;
                }
            }

            bool articleIdUsed = false;
            switch (Command)
            {
                case Command.NEXT:
                    if (article.Number > 0)
                    {
                        if (GroupName.Trim().Length == 0)
                        {
                            PerfCounters.IncrementCounter(PerfCounterName.ResponseArticleRetrievedNoGroupSelected);
                            return GeneralResponses.ArticleRetrievedNoGroupSelected;
                        }
                        article = DataProvider.GetNextArticleByNumber(ClientUsername, GroupName, article.Number);
                    }
                    else
                    {
                        articleIdUsed = true;
                        article = DataProvider.GetNextArticleById(ClientUsername, GroupName, article.Id);
                    }
                    break;

                case Command.LAST:
                    if (article.Number > 0)
                    {
                        if (GroupName.Trim().Length == 0)
                        {
                            PerfCounters.IncrementCounter(PerfCounterName.ResponseArticleRetrievedNoGroupSelected);
                            return GeneralResponses.ArticleRetrievedNoGroupSelected;
                        }
                        article = DataProvider.GetLastArticleByNumber(ClientUsername, GroupName, article.Number);
                    }
                    else
                    {
                        articleIdUsed = true;
                        article = DataProvider.GetLastArticleById(ClientUsername, GroupName, article.Id);
                    }
                    break;

                default:
                    if (article.Number > 0)
                    {
                        if (GroupName.Trim().Length == 0)
                        {
                            PerfCounters.IncrementCounter(PerfCounterName.ResponseArticleRetrievedNoGroupSelected);
                            return GeneralResponses.ArticleRetrievedNoGroupSelected;
                        }
                        article = DataProvider.GetArticleByNumber(ClientUsername, GroupName, article.Number);
                    }
                    else
                    {
                        articleIdUsed = true;
                        article = DataProvider.GetArticleById(ClientUsername, GroupName, article.Id);
                    }
                    break;
            }

            if (article != null)
            {
                var response = new StringBuilder();

                // valid article found, so store the current article number
                client.ArticleReference = article.Number.ToString();

                switch (Command)
                {
                    case Command.STAT:
                    case Command.NEXT:
                    case Command.LAST:
                        PerfCounters.IncrementCounter(PerfCounterName.ResponseArticleRetrievedRequestTextSeperately);
                        return GeneralResponses.ArticleRetrievedRequestTextSeperately(article.Number, article.Id);
                    
                    case Command.HEAD:
                        PerfCounters.IncrementCounter(PerfCounterName.ResponseArticleRetrievedHeadFollows);
                        response.Append(GeneralResponses.ArticleRetrievedHeadFollows(article.Number, article.Id));
                        response.Append(article.GetHeaderResponseText(ToClientEncoding));
                        response.Append(GeneralResponses.ResponseEnd);
                        return response.ToString();
                    
                    case Command.BODY:
                        PerfCounters.IncrementCounter(PerfCounterName.ResponseArticleRetrievedBodyFollows);
                        response.Append(GeneralResponses.ArticleRetrievedBodyFollows(article.Number, article.Id));
                        response.Append(DataProvider.GetArticleBodyResponseText(ClientUsername, GroupName, article));
                        response.Append("\r\n");
                        response.Append(GeneralResponses.ResponseEnd);
                        return response.ToString();

                    case Command.ARTICLE:
                        PerfCounters.IncrementCounter(PerfCounterName.ResponseArticleRetrievedHeadBodyFollow);
                        response.Append(GeneralResponses.ArticleRetrievedHeadBodyFollow(article.Number, article.Id));
                        response.Append(article.GetHeaderResponseText(ToClientEncoding));
                        response.Append("\r\n");
                        response.Append(DataProvider.GetArticleBodyResponseText(ClientUsername, GroupName, article));
                        response.Append("\r\n");
                        response.Append(GeneralResponses.ResponseEnd);
                        return response.ToString();
                }
            }

            if (articleIdUsed)
            {
                PerfCounters.IncrementCounter(PerfCounterName.ResponseArticleRetrievedNoArticle);
                return GeneralResponses.ArticleRetrievedNoArticle;
            }
            PerfCounters.IncrementCounter(PerfCounterName.ResponseArticleRetrievedNoArticleInGroup);
            return GeneralResponses.ArticleRetrievedNoArticleInGroup;
        }
    }

    public class NntpCommandBody : NntpCommandArticle
    {
        public NntpCommandBody(string groupName) : base(groupName)
        {
            Command = Command.BODY;
            GroupName = groupName;
        }
    }

    public class NntpCommandHead : NntpCommandArticle
    {
        public NntpCommandHead(string groupName) : base(groupName)
        {
            Command = Command.HEAD;
            GroupName = groupName;
        }
    }

    public class NntpCommandHelp : NntpCommand
    {
        public NntpCommandHelp()
        {
            Command = Command.HELP;
        }

        public override string Parse(string parameters, Action<string> writeAction, Client client)
        {
            var response = new StringBuilder();
            PerfCounters.IncrementCounter(PerfCounterName.ResponseHelpTextFollows);
            response.Append(GeneralResponses.HelpTextFollows);
            foreach (string command in Enum.GetNames(typeof(Command)))
            {
                response.Append(command + "\r\n");
            }
            response.Append(GeneralResponses.ResponseEnd);
            return response.ToString();
        }
    }

    public class NntpCommandIHave : NntpCommand
    {
        public NntpCommandIHave()
        {
            Command = Command.IHAVE;
        }

        public override string Parse(string parameters, Action<string> writeAction, Client client)
        {
            string response = GeneralResponses.CommandNotRecognised;
            return response;
        }
    }

    public class NntpCommandMode : NntpCommand
    {
        public NntpCommandMode()
        {
            Command = Command.MODE;
        }

        public override string Parse(string parameters, Action<string> writeAction, Client client)
        {
            if (parameters.ToUpper() == "READER")
            {
                if (PostingAllowed)
                {
                    PerfCounters.IncrementCounter(PerfCounterName.ResponseServerReadyPostingAllowed);
                    return GeneralResponses.ServerReadyPostingAllowed;
                }
                PerfCounters.IncrementCounter(PerfCounterName.ResponseServerReadyPostingNotAllowed);
                return GeneralResponses.ServerReadyPostingNotAllowed;
            }
            PerfCounters.IncrementCounter(PerfCounterName.ResponseCommandNotRecognised);
            return GeneralResponses.CommandNotRecognised;
        }
    }

    public class NntpCommandLast : NntpCommandArticle
    {
        public NntpCommandLast(string groupName) : base(groupName)
        {
            Command = Command.LAST;
            GroupName = groupName;
        }
    }

    public class NntpCommandNewGroups : NntpCommand
    {
        public NntpCommandNewGroups()
        {
            Command = Command.NEWGROUPS;
        }

        public override string Parse(string parameters, Action<string> writeAction, Client client)
        {
            PerfCounters.IncrementCounter(PerfCounterName.ResponseListOfNewGroupsFollow);

            string[] dtGroups = parameters.Split(' ');
            if (dtGroups.Length < 2)
                return GeneralResponses.CommandSyntaxError;

            string dateStr = dtGroups[0].Trim();
            string timeStr = dtGroups[1].Trim();

            DateTime fromDate;
            DateTimeKind kind = DateTimeKind.Local;

            if (dateStr.Length == 6)
            {
                // http://tools.ietf.org/html/rfc3977#section-7.3
                // The date is specified as 6 or 8 digits in the format [xx]yymmdd,
                // where xx is the first two digits of the year (19-99), yy is the last
                // two digits of the year (00-99), mm is the month (01-12), and dd is
                // the day of the month (01-31).  Clients SHOULD specify all four digits
                // of the year.  If the first two digits of the year are not specified
                // (this is supported only for backward compatibility), the year is to
                // be taken from the current century if yy is smaller than or equal to
                // the current year, and the previous century otherwise.
                try
                {
                    int selectedYear;
                    int year = int.Parse(dateStr.Substring(0, 2));
                    int curYear = DateTime.Now.Year%100;
                    if (year <= curYear)
                        selectedYear = year + ((DateTime.Now.Year/100)*100);
                    else
                        selectedYear = year + (((DateTime.Now.Year-100)/100)*100);
                    dateStr = selectedYear.ToString() + dateStr.Substring(2);
                }
                catch (Exception exp)
                {
                    Traces.NntpServerTraceEvent(TraceEventType.Error, client, "Invalid date ({0}) specified in NEWGROUPS: {1}", parameters, Traces.ExceptionToString(exp));
                    return GeneralResponses.CommandSyntaxError;
                }
            }

            try
            {
                int year = Convert.ToInt32(dateStr.Substring(0, 4));
                int month = Convert.ToInt32(dateStr.Substring(4, 2));
                int day = Convert.ToInt32(dateStr.Substring(6, 2));

                int hour = Convert.ToInt32(timeStr.Substring(0, 2));
                int minute = Convert.ToInt32(timeStr.Substring(2, 2));
                int second = Convert.ToInt32(timeStr.Substring(4, 2));

                if ((dtGroups.Length > 2) && (string.Compare(dtGroups[2].Trim(), "GMT", StringComparison.InvariantCultureIgnoreCase) == 0))
                {
                    kind = DateTimeKind.Utc;
                }

                fromDate = new DateTime(year, month, day, hour, minute, second, kind);
            }
            catch (Exception exp)
            {
              Traces.NntpServerTraceEvent(TraceEventType.Error, client, "Invalid date ({0}) specified: {1}", dateStr, Traces.ExceptionToString(exp));
              return GeneralResponses.CommandSyntaxError;
            }


            int cnt = 0;
            bool success = DataProvider.GetNewsgroupListFromDate(ClientUsername, fromDate,
                group =>
                    {
                        if (cnt == 0)
                        {
                            writeAction(GeneralResponses.ListOfNewGroupsFollow);
                        }
                        writeAction(String.Format("{0} {1} {2} {3}\r\n", group.GroupName, group.LastArticle,
                                                  group.FirstArticle, (group.PostingAllowed ? "y" : "n")));
                        cnt++;
                    });
            if ((cnt == 0) && (success == false))
                writeAction(GeneralResponses.ServerArchiveOffline);
            else
            {
                // There where no errors... so check if some groups were returned...
                if (cnt > 0)
                {
                    writeAction(GeneralResponses.ResponseEnd);
                }
                else
                {
                    // if not, returns an empty list
                    writeAction(GeneralResponses.ListOfGroupsFollow);
                    writeAction(GeneralResponses.ResponseEnd);
                }
            }

            return string.Empty;
        }
    }

    public class NntpCommandNewNews : NntpCommand
    {
        public NntpCommandNewNews()
        {
            Command = Command.NEWNEWS;
        }

        public override string Parse(string parameters, Action<string> writeAction, Client client)
        {
            var response = string.Empty;
            return response;
        }
    }

    public class NntpCommandNext : NntpCommandArticle
    {
        public NntpCommandNext(string groupName) : base(groupName)
        {
            Command = Command.NEXT;
            GroupName = groupName;
        }
    }

    public class NntpCommandPost : NntpCommand
    {
        public NntpCommandPost(bool postingAllowed)
        {
            Command = Command.POST;
            PostingAllowed = postingAllowed;
        }

        public override string Parse(string parameters, Action<string> writeAction, Client client)
        {
            if (PostingAllowed)
            {
                PerfCounters.IncrementCounter(PerfCounterName.ResponseSendArticlePost);
                return GeneralResponses.SendArticlePost;
            }
            CancelPost = true;
            PerfCounters.IncrementCounter(PerfCounterName.ResponsePostingNotAllowed);
            return GeneralResponses.PostingNotAllowed;
        }
    }

    public class NntpCommandPostData : NntpCommand
    {
        public NntpCommandPostData()
        {
            Command = Command.POSTDATA;
        }

        public override string Parse(string parameters, Action<string> writeAction, Client client)
        {
            string response = DataProvider.PostArticle(ClientUsername, parameters);

            //switch (result)
            //{
            //    case PostStatus.Success:
            //        PerfCounters.IncrementCounter(PerfCounterName.ResponseArticlePostedOk);
            //        response = GeneralResponses.ArticlePostedOk;
            //        break;
                
            //    case PostStatus.FailedGeneral:
            //        PerfCounters.IncrementCounter(PerfCounterName.ResponsePostingFailed);
            //        response = GeneralResponses.PostingFailed;
            //        break;

            //    case PostStatus.FailedExcessiveLength:
            //        PerfCounters.IncrementCounter(PerfCounterName.ResponsePostingFailedExcessiveLength);
            //        response = GeneralResponses.PostingFailedExcessiveLength;
            //        break;

            //    case PostStatus.FailedSubjectLineBlank:
            //        PerfCounters.IncrementCounter(PerfCounterName.ResponsePostingFailedSubjectLineBlank);
            //        response = GeneralResponses.PostingFailedSubjectLineBlank;
            //        break;

            //    case PostStatus.FailedTextPartMissingInHtml:
            //        PerfCounters.IncrementCounter(PerfCounterName.ResponsePostingFailedTextPartMissingInHtml);
            //        response = GeneralResponses.PostingFailedTextPartMissingInHtml;
            //        break;

            //    case PostStatus.FailedAccessDenied:
            //        PerfCounters.IncrementCounter(PerfCounterName.ResponsePostingFailedAccessDenied);
            //        response = GeneralResponses.PostingFailedAccessDenied;
            //        break;

            //    case PostStatus.FailedGroupNotFound:
            //        PerfCounters.IncrementCounter(PerfCounterName.ResponsePostingFailedGroupNotFound);
            //        response = GeneralResponses.PostingFailedGroupNotFound;
            //        break;
            //}

            return response;
        }
    }

    public class NntpCommandQuit : NntpCommand
    {
        public NntpCommandQuit()
        {
            Command = Command.QUIT;
        }

        public override string Parse(string parameters, Action<string> writeAction, Client client)
        {
            PerfCounters.IncrementCounter(PerfCounterName.ResponseGoodbye);
            return GeneralResponses.Goodbye;
        }
    }

    public class NntpCommandAuthInfo : NntpCommand
    {
        public NntpCommandAuthInfo()
        {
            Command = Command.AUTHINFO;
        }

        public override string Parse(string parameters, Action<string> writeAction, Client client)
        {
            char[] delimiter = { ' ' };
            string[] parametersArray = parameters.Split(delimiter);

            if (parametersArray.Length == 2 && parametersArray[0].ToLower() == "generic")
            {
                if (parametersArray[1].ToLower() == "ntlm")
                {
                    PerfCounters.IncrementCounter(PerfCounterName.ResponseProtocolSupportedProceed);
                    return GeneralResponses.ProtocolSupportedProceed;
                }
                //baseAuthToken = Convert.FromBase64String(parametersArray[1]);

                return "381 <token>\r\n";
            }
            PerfCounters.IncrementCounter(PerfCounterName.ResponsePackagesFollowNtlm);
            return GeneralResponses.PackagesFollowNtlm + GeneralResponses.ResponseEnd;
        }
    }

    public class NntpCommandSlave : NntpCommand
    {
        public NntpCommandSlave()
        {
            Command = Command.SLAVE;
        }

        public override string Parse(string parameters, Action<string> writeAction, Client client)
        {
            return GeneralResponses.SlaveStatusNoted;
        }
    }

    public class NntpCommandDate : NntpCommand
    {
        public NntpCommandDate()
        {
            Command = Command.DATE;
        }

        public override string Parse(string parameters, Action<string> writeAction, Client client)
        {
            // Should reply with: 111 201005305512
            return string.Format("111 {0}\r\n", DateTime.Now.ToString("yyyyMMddHHmmss", System.Globalization.CultureInfo.InvariantCulture));
        }
    }


    public class NntpCommandGroup : NntpCommand
    {
        public NntpCommandGroup()
        {
            Command = Command.GROUP;
        }

        public override string Parse(string parameters, Action<string> writeAction, Client client)
        {
            return GetSelectedNewsgroup(parameters, client);
        }

        private string GetSelectedNewsgroup(string groupName, Client client)
        {
          bool exceptionOccured;
          var group = DataProvider.GetNewsgroup(ClientUsername, groupName, true, out exceptionOccured);

            if (group != null)
            {
                client.GroupName = group.GroupName;  // save the groupname in the clients data
                // It also selects by default the first article in the group:
                client.ArticleReference = group.FirstArticle.ToString();
                PerfCounters.IncrementCounter(PerfCounterName.ResponseGroupSelected);
                return GeneralResponses.GroupSelected(group.NumberOfArticles, group.FirstArticle, group.LastArticle, group.GroupName);
            }
            if (exceptionOccured)
              return GeneralResponses.ServerArchiveOffline;
          PerfCounters.IncrementCounter(PerfCounterName.ResponseNoSuchGroup);
          return GeneralResponses.NoSuchGroup;
        }
    }

    public class NntpCommandList : NntpCommand
    {
        public NntpCommandList()
        {
            Command = Command.LIST;
        }

        public override string Parse(string parameters, Action<string> writeAction, Client client)
        {
            var response = new StringBuilder();

            if (string.Compare(parameters, "overview.fmt", StringComparison.InvariantCultureIgnoreCase) == 0)
            {
                response.Append(GeneralResponses.OrderOfOverviewFields);
                response.Append(HeaderNames.Subject + ":\r\n");
                response.Append(HeaderNames.From + ":\r\n");
                response.Append(HeaderNames.Date + ":\r\n");
                response.Append(HeaderNames.MessageId + ":\r\n");
                response.Append(HeaderNames.References + ":\r\n");
                response.Append(":bytes\r\n");
                response.Append(":lines\r\n");
                response.Append(HeaderNames.XRef + ":full\r\n");
                response.Append(GeneralResponses.ResponseEnd);
            }
            else if (parameters.IndexOf("NEWSGROUPS", StringComparison.InvariantCultureIgnoreCase) == 0)
            {
                PerfCounters.IncrementCounter(PerfCounterName.ResponseListOfGroupsFollow);

                // Directly stream the list into the output action function
                //writeAction(GeneralResponses.ListOfGroupsFollow);
                bool success;
                int cnt = GetNewsgroupListToStream(writeAction, 1, GeneralResponses.ListOfGroupsFollow, out success);
                if ((cnt == 0) && (success == false))
                    writeAction(GeneralResponses.ServerArchiveOffline);
                else
                {
                    // There where no errors... so check if some groups were returned...
                    if (cnt > 0)
                    {
                        writeAction(GeneralResponses.ResponseEnd);
                    }
                    else
                    {
                        // if not, returns an empty list
                        writeAction(GeneralResponses.ListOfGroupsFollow);
                        writeAction(GeneralResponses.ResponseEnd);
                    }
                }
            }
            // TODO: Implement additional LIST parameters, like
            // - LIST ACTIVE [wildmat]
            // - LIST NEWSGROUPS [wildmat]
            else
            {
                PerfCounters.IncrementCounter(PerfCounterName.ResponseListOfGroupsFollow);

                // Directly stream the list into the output action function
                //writeAction(GeneralResponses.ListOfGroupsFollow);
                bool noErrors;
                int cnt = GetNewsgroupListToStream(writeAction, 0, GeneralResponses.ListOfGroupsFollow, out noErrors);
                if ((cnt == 0) && (noErrors == false) )
                    writeAction(GeneralResponses.ServerArchiveOffline);
                else
                {
                    if (cnt > 0)
                        writeAction(GeneralResponses.ResponseEnd);
                    else
                    {
                        writeAction(GeneralResponses.ListOfGroupsFollow);
                        writeAction(GeneralResponses.ResponseEnd);
                    }
                }
            }

            return response.ToString();
        }

        // format: 
        //  0: Normal
        //  1: NEWSGROUPS (name, description)
        private int GetNewsgroupListToStream(Action<string> writeAction, int format, string firstReply, out bool success)
        {
            int idx = 0;
            success = DataProvider.GetNewsgroupListToStream(ClientUsername,
                group =>
                {
                    if (idx == 0)
                    {
                        writeAction(firstReply);
                    }
                    if (format == 1)
                    {
                        var descBytes = Encoding.ASCII.GetBytes(group.Description);
                        var desc = Encoding.ASCII.GetString(descBytes);
                        writeAction(String.Format("{0}\t{1}\r\n", group.GroupName, desc));
                    }
                    else
                        writeAction(String.Format("{0} {1} {2} {3}\r\n", group.GroupName, group.LastArticle, group.FirstArticle, (group.PostingAllowed ? "y" : "n")));
                    idx++;
                });
            return idx;
        }
    }

    public class NntpCommandListGroup : NntpCommand
    {
        // See: http://tools.ietf.org/html/rfc3977#section-6.1.2
        // LISTGROUP [group [range]]

        private string _groupName = string.Empty;

        public NntpCommandListGroup(string groupName)
        {
            Command = Command.LISTGROUP;
            _groupName = groupName;
        }

        public override string Parse(string parameters, Action<string> writeAction, Client client)
        {
            var args = parameters.Split(' ');
            if (parameters.Trim().Length <= 0)
            {
                if (_groupName.Trim().Length <= 0)
                {
                    PerfCounters.IncrementCounter(PerfCounterName.ResponseArticleRetrievedNoGroupSelected);
                    return GeneralResponses.ArticleRetrievedNoGroupSelected;
                }
            }
            else
            {
                _groupName = args[0];
            }

            bool exceptionOccured;
            if (DataProvider.GetNewsgroup(ClientUsername, _groupName, false, out exceptionOccured) == null)
            {
              if (exceptionOccured)
                return GeneralResponses.ServerArchiveOffline;
              PerfCounters.IncrementCounter(PerfCounterName.ResponseNoSuchGroup);
              return GeneralResponses.NoSuchGroup;
            }

            var firstArticle = 0;
            var lastArticle = 0;
            var articleNumber = 0;
            var articleId = string.Empty;

            // For more info about the parameter see:
            // In RFC 2980 it is specified as:
            //   The optional range argument may be any of the following:
            //   (1) an article number
            //   (2) an article number followed by a dash to indicate all following
            //   (3) an article number followed by a dash followed by another article number

            if (args.Length > 1)
            {
                string[] parts = args[1].Split(new[] {'-'});
                if (args[1].IndexOf("<") == 0)
                {
                    // The parameter is an article Id, so use this to get the article:
                    articleId = args[1].Trim();
                }
                else if (parts.Length > 1)
                {
                    firstArticle = Convert.ToInt32(parts[0]);
                    var lastArticleStr = parts[1];

                    if (lastArticleStr.Trim().Length == 0)
                    {
                        // Check if the separator was a dash (do not allow a space as last separator)
                        if (args[1].IndexOf('-') < 0)
                        {
                            articleNumber = Convert.ToInt32(args[1]);
                        }
                        else
                        {
                          bool exceptionOccured2;
                          var g = DataProvider.GetNewsgroup(ClientUsername, _groupName, true, out exceptionOccured2);
                            if (g == null)
                            {
                              if (exceptionOccured2)
                                return GeneralResponses.ServerArchiveOffline;
                              PerfCounters.IncrementCounter(PerfCounterName.ResponseNoSuchGroup);
                              return GeneralResponses.NoSuchGroup;
                            }
                            lastArticle = g.LastArticle;
                        }
                    }
                    else
                    {
                        lastArticle = Convert.ToInt32(lastArticleStr);
                    }
                }
                else
                {
                    articleNumber = Convert.ToInt32(args[1]);
                    //RevisedArticleReference = articleNumber.ToString();
                }
            }
            else
            {
                // return the complete list...
              bool exceptionOccured3;
              var g = DataProvider.GetNewsgroup(ClientUsername, _groupName, true, out exceptionOccured3);
              if (g == null)
              {
                if (exceptionOccured3)
                  return GeneralResponses.ServerArchiveOffline;
                PerfCounters.IncrementCounter(PerfCounterName.ResponseNoSuchGroup);
                return GeneralResponses.NoSuchGroup;
              }
              firstArticle = g.FirstArticle;
                lastArticle = g.LastArticle;
            }

            //var articles = new Dictionary<int, Article>();
            Article article = null;

            if (articleId.Length > 0)
            {
                article = DataProvider.GetArticleById(ClientUsername, _groupName, articleId);

                if (article == null)
                {
                    PerfCounters.IncrementCounter(PerfCounterName.ResponseArticleRetrievedNoArticleInGroup);
                    return GeneralResponses.ArticleRetrievedNoArticleInGroup;
                }

                //articles.Add(article.Number, article);
            }
            else if (articleNumber != 0)
            {
                article = DataProvider.GetArticleByNumber(ClientUsername, _groupName, articleNumber);

                if (article == null) 
                {
                    PerfCounters.IncrementCounter(PerfCounterName.ResponseArticleRetrievedNoArticleInGroup);
                    return GeneralResponses.ArticleRetrievedNoArticleInGroup;
                }

                //articles.Add(article.Number, article);
            }
            else
            {
                // range of articles
                //PerfCounters.IncrementCounter(PerfCounterName.ResponseOverviewInformationFollows);
                writeAction(GeneralResponses.GroupArticlesNumbersFollow(firstArticle, firstArticle, lastArticle, _groupName));

                // range of articles
                //System.Diagnostics.Trace.WriteLine("Calling GetArticlesByNumber");
                DataProvider.GetArticlesByNumberToStream(ClientUsername, _groupName, firstArticle, lastArticle,
                    p =>
                    {
                        if ((p != null) && (p.Count > 0))
                        {
                            var sb = new StringBuilder();
                            foreach (var a in p)
                            {
                                sb.Length = 0;
                                sb.Append(a.Number);
                                sb.Append("\r\n");
                                writeAction(sb.ToString());
                            }
                        }
                    }
                    );

                // Update current selected group/article:
              bool exceptionOccured3;
              var g = DataProvider.GetNewsgroup(ClientUsername, _groupName, true, out exceptionOccured3);
                client.GroupName = g.GroupName;  // save the groupname in the clients data
                // It also selects by default the first article in the group:
                client.ArticleReference = g.FirstArticle.ToString();

                // at least one response sent...
                writeAction(GeneralResponses.ResponseEnd);
                return string.Empty;
            }

            var response = new StringBuilder();

            PerfCounters.IncrementCounter(PerfCounterName.ResponseOverviewInformationFollows);
            response.Append(GeneralResponses.GroupArticlesNumbersFollow(1, article.Number, article.Number, _groupName));

            response.Append(article.Number);
            response.Append("\r\n");

            response.Append(GeneralResponses.ResponseEnd);

            // Update current selected group/article:
          bool exceptionOccured4;
            var g2 = DataProvider.GetNewsgroup(ClientUsername, _groupName, true, out exceptionOccured4);
            client.GroupName = g2.GroupName;  // save the groupname in the clients data
            // It also selects by default the first article in the group:
            client.ArticleReference = g2.FirstArticle.ToString();


            return response.ToString();
        }
    }

}
