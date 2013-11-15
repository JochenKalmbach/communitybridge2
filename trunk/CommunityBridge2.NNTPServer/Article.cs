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
using System.Text;

namespace CommunityBridge2.NNTPServer
{
    public class Article
    {
        private int _number;
        private string _id = string.Empty;
        private string _body = string.Empty;
        private string _from = string.Empty;
        private string _newsgroups = string.Empty;
        private string _subject = string.Empty;
        private string _date = string.Empty; // RFC1123 format
        private string _path = string.Empty;
        private string _references = string.Empty;
        private string _supersedes = string.Empty;
        private string _parentNewsgroup = string.Empty;
        private string _crossPostedGroups = string.Empty;
        private string _contentType = "text/html; charset=utf-8";  // "text/plain; charset=ISO-8859-1"
        private string _contentTransferEncoding = "8bit";
        private string _xNewsreader = "Community Forums NNTP Server 1.0.0.0";
        private string _xNNTPServer = string.Empty;
        private string _userAgent = string.Empty;
        private string _mimeVersion = "1.0";
        private string _xComments = string.Empty;
        public string _xFace = string.Empty;
        public string _archivedAt = string.Empty;

        private static readonly string FileVersion;
      private static string _MyXNewsreaderString;

        static Article()
        {
            Version v = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
            FileVersion = v.ToString();
            _MyXNewsreaderString = string.Format("Community Forums NNTP Server {0}", FileVersion);
        }

        public static void SetName(string name)
        {
          _MyXNewsreaderString = name;
          //ServerReadyPostingNotAllowed = string.Format("200 {0} Ready - posting not allowed\r\n", name);
        }

        public static string MyXNewsreaderString
        {
          get { return _MyXNewsreaderString; }
        }

        /// <summary>
        /// Default constructor
        /// </summary>
        public Article()
        {
            _xNewsreader = MyXNewsreaderString;
            _xNNTPServer = MyXNewsreaderString;
        }

        /// <summary>
        /// Creates a new Article
        /// </summary>
        /// <param name="number">Number of the article</param>
        public Article(int number)
        {
            _xNewsreader = MyXNewsreaderString;
            _xNNTPServer = MyXNewsreaderString;
            _number = number;
        }

        /// <summary>
        /// Creates a new Article
        /// </summary>
        /// <param name="number">Number of the article</param>
        /// <param name="id">ID of the article</param>
        public Article(int number, string id)
        {
            _xNewsreader = MyXNewsreaderString;
            _xNNTPServer = MyXNewsreaderString;
            _number = number;
            _id = id;
        }

        /// <summary>
        /// Gets the size (number of characters) of the article body.
        /// Returns 0 if the article body has not yet been loaded.
        /// </summary>
        public int Size
        {
            get
            {
                return _body.Length;
            }
        }

        /// <summary>
        /// Gets the number of lines in the body of the article
        /// </summary>
        public string Lines
        {
            get
            {
                if (!_body.Contains("\n"))
                {
                    return "1";
                }
                char[] delimiter = { ' ' };
                string[] lines = _body.Split(delimiter);
                return lines.Length.ToString();
            }
        }

        /// <summary>
        /// Gets or sets the subject line of the article
        /// </summary>
        public string Subject
        {
            get
            {
                return _subject;
            }
            set
            {
                if (value != null)
                {
                    _subject = value.Replace("\n", string.Empty).Replace("\r", string.Empty).Replace("\t", string.Empty).Trim();
                    if (_subject.Length <= 0)
                        _subject = "(no subject)";
                }
                else
                {
                    _subject = "(no subject)";
                }
            }
        }

        /// <summary>
        /// Gets or sets the 'From' header for the article
        /// </summary>
        public string From
        {
            get
            {
                return _from;
            }
            set
            {
                if (value != null)
                {
                    _from = value.Replace("\n", string.Empty).Replace("\r", string.Empty).Replace("\t", string.Empty).Trim();
                    if (_from.Length <= 0)
                        _from = "(no sender)";
                }
                else
                {
                    _from = "(no sender)";
                }
            }
        }

        /// <summary>
        /// Gets or sets the 'Date' header for the article
        /// </summary>
        public string Date
        {
            get
            {
                return _date;
            }
            set
            {
                if (value != null)
                {
                    _date = value.Replace("\n", string.Empty).Replace("\r", string.Empty).Replace("\t", string.Empty).Trim();
                }
                else
                {
                    _date = string.Empty;
                }
            }
        }

        /// <summary>
        /// Gets or sets the body of the article
        /// </summary>
        public string Body
        {
            get
            {
                return _body;
            }
            set
            {
                _body = value ?? string.Empty;
                _cachedBodyResponseText = null;
            }
        }

        /// <summary>
        /// Gets or sets the 'References' header for the article
        /// </summary>
        public string References
        {
            get
            {
                return _references;
            }
            set
            {
                if (value != null)
                {
                    _references = value.Replace("\n", string.Empty).Replace("\r", string.Empty).Replace("\t", string.Empty).Trim();
                }
                else
                {
                    _references = string.Empty;
                }
            }
        }

        /// <summary>
        /// Gets or sets the 'Supersedes' header for the article
        /// </summary>
        public string Supersedes
        {
          get
          {
            return _supersedes;
          }
          set
          {
            if (value != null)
            {
              _supersedes = value.Replace("\n", string.Empty).Replace("\r", string.Empty).Replace("\t", string.Empty).Trim();
            }
            else
            {
              _supersedes = string.Empty;
            }
          }
        }

        /// <summary>
        /// Gets or sets the 'Newsgroups' header for the article
        /// </summary>
        public string Newsgroups
        {
            get
            {
                return _newsgroups;
            }
            set
            {
                if (value != null)
                {
                    _newsgroups = value.Replace("\n", string.Empty).Replace("\r", string.Empty).Replace("\t", string.Empty).Trim();
                }
                else
                {
                    _newsgroups = string.Empty;
                }
            }
        }

        /// <summary>
        /// Gets or sets the the name of the newsgroup containing this article (typically first in Newsgroups list)
        /// </summary>
        public string ParentNewsgroup
        {
            get
            {
                return _parentNewsgroup;
            }
            set
            {
                if (value != null)
                {
                    _parentNewsgroup = value.Replace("\n", string.Empty).Replace("\r", string.Empty).Replace("\t", string.Empty).Trim();
                }
                else
                {
                    _parentNewsgroup = string.Empty;
                }
            }
        }

        /// <summary>
        /// Gets or sets the 'Path' header for the article
        /// </summary>
        public string Path
        {
            get
            {
                return _path;
            }
            set
            {
                if (value != null)
                {
                    _path = value.Replace("\n", string.Empty).Replace("\r", string.Empty).Replace("\t", string.Empty).Trim();
                }
                else
                {
                    _path = string.Empty;
                }
            }
        }

        /// <summary>
        /// Gets or sets the number of the article (corresponds to SharePoint list item ID)
        /// </summary>
        public int Number
        {
            get
            {
                return _number;
            }
            set
            {
                _number = value;
            }
        }

        /// <summary>
        /// Gets or sets the ID of the article (typically list item GUID plus suffix)
        /// </summary>
        public string Id
        {
            get
            {
                return _id;
            }
            set
            {
                if (value != null)
                {
                    _id = value.Replace("\n", string.Empty).Replace("\r", string.Empty).Replace("\t", string.Empty).Trim();
                }
                else
                {
                    _id = string.Empty;
                }
            }
        }

        public string ContentType
        {
            get
            {
                return _contentType;
            }
            set
            {
                if (value != null)
                {
                    _contentType = value.Replace("\n", string.Empty).Replace("\r", string.Empty).Replace("\t", string.Empty).Trim();
                }
                else
                {
                    _contentType = string.Empty;
                }
            }
        }

        public string ContentTransferEncoding
        {
            get
            {
                return _contentTransferEncoding;
            }
            set
            {
                if (value != null)
                {
                    _contentTransferEncoding = value.Replace("\n", string.Empty).Replace("\r", string.Empty).Replace("\t", string.Empty).Trim();
                }
                else
                {
                    _contentTransferEncoding = string.Empty;
                }
            }
        }

        public string MimeVersion
        {
            get
            {
                return _mimeVersion;
            }
            set
            {
                if (value != null)
                {
                    _mimeVersion = value.Replace("\n", string.Empty).Replace("\r", string.Empty).Replace("\t", string.Empty).Trim();
                }
                else
                {
                    _mimeVersion = string.Empty;
                }
            }
        }

        public string XComments
        {
            get
            {
                return _xComments;
            }
            set
            {
                if (value != null)
                {
                    _xComments = value.Replace("\n", string.Empty).Replace("\r", string.Empty).Replace("\t", string.Empty).Trim();
                }
                else
                {
                    _xComments = string.Empty;
                }
            }
        }

        public string XFace
        {
            get
            {
                return _xFace;
            }
            set
            {
                if (value != null)
                {
                    _xFace = value.Replace("\n", string.Empty).Replace("\r", string.Empty).Replace("\t", string.Empty).Trim();
                }
                else
                {
                    _xFace = string.Empty;
                }
            }
        }

        // See: http://www.ietf.org/rfc/rfc5064.txt
        public string ArchivedAt
        {
            get
            {
                return _archivedAt;
            }
            set
            {
                if (value != null)
                {
                    _archivedAt = value.Replace("\n", string.Empty).Replace("\r", string.Empty).Replace("\t", string.Empty).Trim();
                }
                else
                {
                    _archivedAt = string.Empty;
                }
            }
        }

        /// <summary>
        /// Gets or sets the 'XRef' header for the article
        /// </summary>
        public string XRef
        {
            get
            {
                return String.Format("{0} {1}:{2}", _path, _parentNewsgroup, _number);
            }
        }

        /// <summary>
        /// Returns the value of the given article header field
        /// </summary>
        /// <param name="headerName">The name of the header field</param>
        /// <returns>ArticleHeader object</returns>
        public ArticleHeader GetHeaderByName(string headerName)
        {
            string headerValue;

            if (string.Compare(headerName, HeaderNames.From, StringComparison.InvariantCultureIgnoreCase) == 0)
            {
                    headerValue = _from;
            }
            else if (string.Compare(headerName, HeaderNames.Subject, StringComparison.InvariantCultureIgnoreCase) == 0)
            {
                    headerValue = _subject;
            }
            else if (string.Compare(headerName, HeaderNames.Date, StringComparison.InvariantCultureIgnoreCase) == 0)
            {
                    headerValue = _date;
            }
            else if (string.Compare(headerName, HeaderNames.Lines, StringComparison.InvariantCultureIgnoreCase) == 0)
            {
                    headerValue = Lines;
            }
            else if (string.Compare(headerName, HeaderNames.MessageId, StringComparison.InvariantCultureIgnoreCase) == 0)
            {
                    headerValue = _id;
            }
            else if (string.Compare(headerName, HeaderNames.Newsgroups, StringComparison.InvariantCultureIgnoreCase) == 0)
            {
                    headerValue = _newsgroups;
            }
            else if (string.Compare(headerName, HeaderNames.Path, StringComparison.InvariantCultureIgnoreCase) == 0)
            {
                    headerValue = _path;
            }
            else if (string.Compare(headerName, HeaderNames.XRef, StringComparison.InvariantCultureIgnoreCase) == 0)
            {
                    headerValue = XRef;
            }
            else if (string.Compare(headerName, HeaderNames.References, StringComparison.InvariantCultureIgnoreCase) == 0)
            {
                    headerValue = _references;
            }
            else if (string.Compare(headerName, HeaderNames.Supersedes, StringComparison.InvariantCultureIgnoreCase) == 0)
            {
              headerValue = _supersedes;
            }
            else if (string.Compare(headerName, HeaderNames.ContentType, StringComparison.InvariantCultureIgnoreCase) == 0)
            {
                headerValue = _contentType;
            }
            else if (string.Compare(headerName, HeaderNames.MimeVersion, StringComparison.InvariantCultureIgnoreCase) == 0)
            {
                headerValue = _mimeVersion;
            }
            else if (string.Compare(headerName, HeaderNames.XComments, StringComparison.InvariantCultureIgnoreCase) == 0)
            {
                headerValue = _xComments;
            }
            else if (string.Compare(headerName, HeaderNames.ArchivedAt, StringComparison.InvariantCultureIgnoreCase) == 0)
            {
                headerValue = _archivedAt;
            }
            else
            {
                    headerValue = string.Empty;
            }

            return new ArticleHeader(headerName, headerValue);
        }

        /// <summary>
        /// Gets or sets the article reference.  The value may be a string (guid + suffix) or number (list item ID)
        /// </summary>
        public object ArticleReference
        {
            get
            {
                if (_id.Trim().Length > 0)
                {
                    return _id;
                }
                return _number;
            }
            set
            {
                string reference = value.ToString();
                if (reference.Trim().Length > 0)
                {
                    if (reference.StartsWith("<") && reference.EndsWith(">"))
                    {
                        _id = reference;
                    }
                    else
                    {
                        _number = Convert.ToInt32(reference);
                    }
                }
            }
        }

        /// <summary>
        /// Gets the complete article header
        /// </summary>
        public string GetHeaderResponseText(Encoding toClientEncoding)
        {
            const string seperator = ": ";
            const string newLine = "\r\n";

            if (toClientEncoding != null)
            {
                if (string.Compare(toClientEncoding.HeaderName, "utf-8", StringComparison.InvariantCultureIgnoreCase) != 0)
                {
                    // It is a different encoding, so be sure to set the Content-Type correctly:
                    if (_contentType != null)
                        _contentType = _contentType.Replace("utf-8", toClientEncoding.HeaderName);
                }
            }

            var headers = new StringBuilder();

            headers.Append(HeaderNames.From);
            headers.Append(seperator);
            headers.Append(_from);
            headers.Append(newLine);
            headers.Append(HeaderNames.Subject);
            headers.Append(seperator);
            headers.Append(_subject);
            headers.Append(newLine);
            headers.Append(HeaderNames.Date);
            headers.Append(seperator);
            headers.Append(_date);
            headers.Append(newLine);
            headers.Append(HeaderNames.Lines);
            headers.Append(seperator);
            headers.Append(Lines);
            headers.Append(newLine);
            headers.Append(HeaderNames.MessageId);
            headers.Append(seperator);
            headers.Append(_id);
            headers.Append(newLine);
            headers.Append(HeaderNames.Newsgroups);
            headers.Append(seperator);
            headers.Append(_newsgroups);
            headers.Append(newLine);
            headers.Append(HeaderNames.Path);
            headers.Append(seperator);
            headers.Append(_path);
            headers.Append(newLine);
            headers.Append(HeaderNames.XRef);
            headers.Append(seperator);
            headers.Append(XRef);
            headers.Append(newLine);
            headers.Append(HeaderNames.MimeVersion);
            headers.Append(seperator);
            headers.Append(_mimeVersion);
            headers.Append(newLine);
            headers.Append(HeaderNames.ContentType);
            headers.Append(seperator);
            headers.Append(ContentType);
            headers.Append(newLine);
            headers.Append(HeaderNames.ContentTransferEncoding);
            headers.Append(seperator);
            headers.Append(ContentTransferEncoding);
            headers.Append(newLine);
            headers.Append(HeaderNames.XNewsreader);
            headers.Append(seperator);
            headers.Append(_xNewsreader);
            headers.Append(newLine);
            if (string.IsNullOrEmpty(_xComments) == false)
            {
                headers.Append(HeaderNames.XComments);
                headers.Append(seperator);
                headers.Append(_xComments);
                headers.Append(newLine);
            }
            if (string.IsNullOrEmpty(_xNNTPServer) == false)
            {
                headers.Append(HeaderNames.XNNTPServer);
                headers.Append(seperator);
                headers.Append(_xNNTPServer);
                headers.Append(newLine);
            }
            if (string.IsNullOrEmpty(_xFace) == false)
            {
                headers.Append(HeaderNames.XFace);
                headers.Append(seperator);
                headers.Append(_xFace);
                headers.Append(newLine);
            }
            if (string.IsNullOrEmpty(_archivedAt) == false)
            {
                headers.Append(HeaderNames.ArchivedAt);
                headers.Append(seperator);
                headers.Append(_archivedAt);
                headers.Append(newLine);
            }
            if (string.IsNullOrEmpty(_references) == false)
            {
                headers.Append(HeaderNames.References);
                headers.Append(seperator);
                headers.Append(_references);
                headers.Append(newLine);
            }
            if (string.IsNullOrEmpty(_supersedes) == false)
            {
              headers.Append(HeaderNames.Supersedes);
              headers.Append(seperator);
              headers.Append(_supersedes);
              headers.Append(newLine);
            }
            return headers.ToString();
        }

        /// <summary>
        /// Gets or sets the newsgroups for cross posting
        /// </summary>
        public string CrossPostedGroups
        {
            get
            {
                return _crossPostedGroups;
            }
            set
            {
                if (value != null)
                {
                    _crossPostedGroups = value.Replace("\n", string.Empty).Replace("\r", string.Empty).Replace("\t", string.Empty).Trim();
                }
                else
                {
                    _crossPostedGroups = string.Empty;
                }
            }
        }

        public DateTime CacheDateTimeAdded = DateTime.Now;

        public string XNewsreader
        {
            get { return _xNewsreader; }
            set
            {
                if (value != null)
                {
                    _xNewsreader = value.Replace("\n", string.Empty).Replace("\r", string.Empty).Replace("\t", string.Empty).Trim();
                }
                else
                {
                    _xNewsreader = string.Empty;
                }
            }
        }

        public string XNNTPServer
        {
            get { return _xNNTPServer; }
            set
            {
                if (value != null)
                {
                    _xNNTPServer = value.Replace("\n", string.Empty).Replace("\r", string.Empty).Replace("\t", string.Empty).Trim();
                }
                else
                {
                    _xNNTPServer = string.Empty;
                }
            }
        }

        public string UserAgent
        {
            get { return _userAgent; }
            set
            {
                if (value != null)
                {
                    _userAgent = value.Replace("\n", string.Empty).Replace("\r", string.Empty).Replace("\t", string.Empty).Trim();
                }
                else
                {
                    _userAgent = string.Empty;
                }
            }
        }

        private string _cachedBodyResponseText = null;
        public string GetArticleBodyResponseText()
        {
            if (string.IsNullOrEmpty(_cachedBodyResponseText) == false)
                return _cachedBodyResponseText;

            string[] lines = Body.Split('\n');
            var sb = new StringBuilder(Body.Length + (lines.Length*2));
            foreach(var l in lines)
            {
                if (l.IndexOf(".", StringComparison.InvariantCultureIgnoreCase) == 0)
                    sb.Append("." + l);
                else
                {
                    sb.Append(l);
                }
                sb.Append("\n");
            }
            _cachedBodyResponseText = sb.ToString();
            return _cachedBodyResponseText;
        }

        #region NNTPBridge Special

        public string Brand;
        public bool? IsAdministrator;
        public bool? IsMsft;
        public bool? IsMvp;
        public int? Points;
        public int? PostsCount;
        public int? AnwsersCount;
        public int? Stars;
        public string UserEmail;
        public string DisplayName;  // The Username
        public Guid UserGuid;

        public Guid Guid;
        public Guid DiscussionId;
        #endregion
    }  // class Article

    [Flags]
    public enum UsePlainTextConverters
    {
        None = 0,
        OnlyReceive = 1,
        OnlySend = 2,
        SendAndReceive = 3,
    }

    public interface IArticleConverter
    {
        void NewArticleFromWebService(Article a, Encoding enc);
        void NewArticleFromClient(Article a);
        UsePlainTextConverters UsePlainTextConverter { get; set; }
        int AutoLineWrap { get; set; }
    }
}
