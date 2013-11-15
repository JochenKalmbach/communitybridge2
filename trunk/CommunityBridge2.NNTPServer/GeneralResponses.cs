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

namespace CommunityBridge2.NNTPServer
{
    public static class GeneralResponses
    {
      static GeneralResponses()
      {
        Version v = System.Reflection.Assembly.GetEntryAssembly().GetName().Version;
        string fileVersion = v.ToString();
        // TODO: Change this if RTMed
        ServerReadyPostingAllowed = string.Format("200 Community Forums NNTP Server (Answers) {0} Ready - posting allowed\r\n", fileVersion);
        ServerReadyPostingNotAllowed = string.Format("200 Community Forums NNTP Server (Answers) {0} Ready - posting not allowed\r\n", fileVersion);
      }

      public static void SetName(string name)
      {
        ServerReadyPostingAllowed = string.Format("200 {0} Ready - posting allowed\r\n", name);
        ServerReadyPostingNotAllowed = string.Format("200 {0} Ready - posting not allowed\r\n", name);
      }

        #region Codes 100..199
        public const string HelpTextFollows = "100 help text follows\r\n";
        #endregion


        #region Codes 200..299
        public static string ServerReadyPostingAllowed;
        public static string ServerReadyPostingNotAllowed;
        public const string SlaveStatusNoted = "202 slave status noted\r\n";
        public const string Goodbye = "205 goodbye.\r\n";
        public const string ListOfGroupsFollow = "215 list of newsgroups follows\r\n";
        public const string OrderOfOverviewFields = "215 order of fields in overview database\r\n";
        public const string ListOfNewGroupsFollow = "231 list of new newsgroups follows\r\n";
        public const string ArticleTransferredOk = "235 article transferred ok\r\n";
        public const string ArticlePostedOk = "240 article posted ok\r\n";
        public const string HeaderFollows = "221 header follows\r\n";
        public const string OverviewInformationFollows = "224 overview information follows\r\n";
        public const string ListOfNewsArticlesFollow = "230 list of new articles by message-id follows\r\n";
        public static string GroupSelected(int numberArticles, int firstArticle, int lastArticle, string groupName)
        {
            return String.Format("211 {0} {1} {2} {3} group selected\r\n", numberArticles, firstArticle, lastArticle, groupName);
        }
        public static string GroupArticlesNumbersFollow(int numberArticles, int firstArticle, int lastArticle, string groupName)
        {
            return String.Format("211 {0} {1} {2} {3} Article numbers follow (multi-line)\r\n", numberArticles, firstArticle, lastArticle, groupName);
        }
        public static string ArticleRetrievedHeadBodyFollow(int articleNumber, string articleId)
        {
            return String.Format("220 {0} {1} article retrieved - head and body follow\r\n", articleNumber, articleId);
        }
        public static string ArticleRetrievedHeadFollows(int articleNumber, string articleId)
        {
            return String.Format("221 {0} {1} article retrieved - head follows\r\n", articleNumber, articleId);
        }
        public static string ArticleRetrievedBodyFollows(int articleNumber, string articleId)
        {
            return String.Format("222 {0} {1} article retrieved - body follows\r\n", articleNumber, articleId);
        }
        public static string ArticleRetrievedRequestTextSeperately(int articleNumber, string articleId)
        {
            return String.Format("223 {0} {1} article retrieved - request text separately\r\n", articleNumber, articleId);
        }
        public const string PackagesFollowNtlm = "281 Packages follow\r\nNTLM\r\n";
        public const string AuthenticationAccepted = "281 Authentication ok\r\n";
        #endregion

        #region Codes 300..399
        public const string SendArticleIHave = "335 send article to be transferred.  End with <CR-LF>.<CR-LF>\r\n";
        public const string SendArticlePost = "340 send article to be transferred.  End with <CR-LF>.<CR-LF>\r\n";
        public const string MoreAuthenticationInformationRequired = "381 More authentication information required\r\n";
        public const string ProtocolSupportedProceed = "381 Protocol supported, proceed\r\n";
        #endregion

        #region Codes 400..499
        public const string ServerArchiveOffline = "403 Archive server temporarily offline\r\n";
        public const string ArticleRetrievedNoGroupSelected = "412 no newsgroup has been selected\r\n";
        public const string ArticleRetrievedNoArticleSelected = "420 no current article has been selected\r\n";
        public const string ArticleRetrievedNoNextArticleInGroup = "421 no next article in this group\r\n";
        public const string ArticleRetrievedNoPreviousArticleInGroup = "422 no previous article in this group\r\n";
        public const string ArticleRetrievedNoArticleInGroup = "423 no such article number in this group\r\n";
        public const string ArticleRetrievedNoArticle = "430 no such article found\r\n";
        public const string ArticleNotWanted = "435 article not wanted - do not send it\r\n";
        public const string TransferFailed = "436 transfer failed - try again later\r\n";
        public const string ArticleRejected = "437 article rejected - do not try again\r\n";
        public const string NoSuchGroup = "411 no such news group\r\n";
        public const string PostingNotAllowed = "440 posting not allowed\r\n";
        public const string PostingFailed = "441 posting failed\r\n";
        public const string PostingFailedExcessiveLength = "441 posting failed - the size of the post is too large\r\n";
        public const string PostingFailedSubjectLineBlank = "441 posting failed - the subject line cannot be blank\r\n";
        public const string PostingFailedTextPartMissingInMime = "441 posting failed - the text part or html part in mime data is missing\r\n";
        public const string PostingFailedAccessDeniedMultipost = "441 posting failed - access denied - multipost not allowed\r\n";
        public const string PostingFailedAccessDenied = "441 posting failed - access denied\r\n";
        public const string PostingFailedGroupNotFound = "441 posting failed - selected group not found\r\n";
        public const string AuthenticationRequired = "480 Authentication required\r\n";
        public const string AuthenticationRejected = "482 Authentication rejected\r\n";
        #endregion

        #region Codes 500..599
        public const string CommandNotRecognised = "500 command not recognised\r\n";
        public const string CommandSyntaxError = "501 command syntax error\r\n";
        public const string NotAnArticleIdOrNumber = "501 not an article number or a message id\r\n";
        public const string AccessDenied = "502 access restriction or permission denied\r\n";
        public const string ProgramFault = "503 program fault - command not performed\r\n";
        #endregion

        #region Misc Responses
        public const string ResponseEnd = ".\r\n";
        #endregion
    }
}
