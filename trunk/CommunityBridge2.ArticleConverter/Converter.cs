#define PLAINTEXTCONVERTER_ENABLE_BUGFIX_FOR_FORUM

using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using CommunityBridge2.NNTPServer;
using HtmlAgilityPack;

namespace CommunityBridge2.ArticleConverter
{
    public class Converter : NNTPServer.IArticleConverter
    {
        static Converter()
        {
            try
            {
                _colorizer = new ColorCode.CodeColorizer();
            }
            catch (Exception exp)
            {
                Traces.ConvertersTraceEvent(TraceEventType.Error, 1, NNTPServer.Traces.ExceptionToString(exp));
            }

        }
        public Converter()
        {
            // Default Values
            TabAsSpace = 4;
        }

        public UsePlainTextConverters UsePlainTextConverter { get; set; }
        public bool UseCodeColorizer { get; set; }
        public int AutoLineWrap { get; set; }
        public UserDefinedTagCollection UserDefinedTags { get; set; }
        public bool ShowUserNamePostfix { get; set; }
        public bool PostsAreAlwaysFormatFlowed { get; set; }
        public int TabAsSpace { get; set; }

        private static ColorCode.CodeColorizer _colorizer = null;
        private string CodeBoxHtmlStyle { get { return "font-family:Courier New, Courier, mono;"; } }
        private string Blockquote2ndLevelBoxHtmlStyle { get { return "border-left-style: solid; border-left-width: 1px; padding: 0 0.5em;"; } }


        private static class NewLine
        {
            public const string HtmlBreak = "<br />";
            public const string Mac = "\r";
            public const string Unix = "\n";
            public const string Windows = "\r\n";
        }

        private static class CodeTag
        {
            public const string cpp = "cpp";
            public const string csharp = "c#";
            public const string vbnet = "vbnet";
            public const string sql = "sql";
            public const string html = "html";
        }

        #region NewArticleFromWebService
        /// <summary>
        /// Converts some fields from from Article to other values. For example "Body" or "Subject".
        /// This function is called after a new message was received from the web service.
        /// INFO: This method mustr be threadsafe!
        /// </summary>
        /// <param name="a"></param>
        public void NewArticleFromWebService(NNTPServer.Article a, Encoding enc)
        {
            Traces.ConvertersTraceEvent(TraceEventType.Verbose, 1, "FromWeb: Body {0} before conversion:\r\n{1}", a.Id, a.Body);

            // Add a "Re: "if the message does not have a "Re:" and is a reply:
            if ((string.IsNullOrEmpty(a.References) == false) &&
                (a.Subject.IndexOf("RE:", StringComparison.InvariantCultureIgnoreCase) < 0))
            {
                a.Subject = "Re: " + a.Subject;
            }


            // Find Newsreader Tag
            FindNewsreaderTag(a);

            a.Subject = EncodeHeaderText(a.Subject, enc);

            // Fill out X-Comments:
            var xcommentsSb = new StringBuilder();
            string userNameAddition = string.Empty;
            // X-Face:
            if ((a.IsAdministrator.HasValue) && (a.IsAdministrator.Value))
            {
                if (string.IsNullOrEmpty(a.XFace))
                    a.XFace = AdminXFace;
                if ((a.DisplayName.IndexOf("ADM", StringComparison.InvariantCultureIgnoreCase) < 0) && (a.DisplayName.IndexOf("ADMIN", StringComparison.InvariantCultureIgnoreCase) < 0))
                {
                    if (string.IsNullOrEmpty(userNameAddition) == false)
                        userNameAddition += "_";
                    userNameAddition += "ADMIN";
                }
                if (xcommentsSb.Length > 0) xcommentsSb.Append("; ");
                xcommentsSb.Append("IsAdmin");
            }
            if ((a.IsMsft.HasValue) && (a.IsMsft.Value))
            {
                if (string.IsNullOrEmpty(a.XFace))
                    a.XFace = MsftXFace;
                if ((a.DisplayName.IndexOf("MSFT", StringComparison.InvariantCultureIgnoreCase) < 0) && (a.DisplayName.IndexOf("MICROSOFT", StringComparison.InvariantCultureIgnoreCase) < 0))
                {
                    if (string.IsNullOrEmpty(userNameAddition) == false)
                        userNameAddition += "_";
                    userNameAddition += "MSFT";
                }
                if (xcommentsSb.Length > 0) xcommentsSb.Append("; ");
                xcommentsSb.Append("IsMsft");
            }
            if ((a.IsMvp.HasValue) && (a.IsMvp.Value))
            {
                if (string.IsNullOrEmpty(a.XFace))
                    a.XFace = MvpXFace;
                if (a.DisplayName.IndexOf("MVP", StringComparison.InvariantCultureIgnoreCase) < 0)
                {
                    if (string.IsNullOrEmpty(userNameAddition) == false)
                        userNameAddition += "_";
                    userNameAddition += "MVP";
                }
                if (xcommentsSb.Length > 0) xcommentsSb.Append("; ");
                xcommentsSb.Append("IsMVP");
            }

            if (a.Stars.HasValue)
            {
                if (xcommentsSb.Length > 0) xcommentsSb.Append("; ");
                xcommentsSb.AppendFormat("Stars={0}", a.Stars.Value);
            }
            if (a.Points.HasValue)
            {
                if (xcommentsSb.Length > 0) xcommentsSb.Append("; ");
                xcommentsSb.AppendFormat("Points={0}", a.Points.Value);
            }
            if (a.PostsCount.HasValue)
            {
                if (xcommentsSb.Length > 0) xcommentsSb.Append("; ");
                xcommentsSb.AppendFormat("Posts={0}", a.PostsCount.Value);
            }
            if (a.AnwsersCount.HasValue)
            {
                if (xcommentsSb.Length > 0) xcommentsSb.Append("; ");
                xcommentsSb.AppendFormat("Answers={0}", a.AnwsersCount.Value);
            }

            if (xcommentsSb.Length > 0)
            {
              if (string.IsNullOrEmpty(a.XComments))
                a.XComments = xcommentsSb.ToString();
              else
                a.XComments += "; " + xcommentsSb;
            }

          // From:

            // INFO: Only encode the username and *not* the email address
            if (string.IsNullOrEmpty(a.UserEmail) == false)
            {
                a.From = string.Format("{0} <{1}>", EncodeHeaderText(a.DisplayName, enc), a.UserEmail);
            }
            else
            {
                // Only use the additons for display names that are not known...
                if ( (ShowUserNamePostfix) && (string.IsNullOrEmpty(userNameAddition) == false))
                    a.DisplayName = a.DisplayName + " [" + userNameAddition + "]";
                //a.From = string.Format("{0} <{1}@communitybridge.example.net>", EncodeHeaderText(a.DisplayName), a.UserGuid);
                a.From = EncodeHeaderText(a.DisplayName, enc);
            }


            // Check if we have something to do with the body...
            if (string.IsNullOrEmpty(a.Body))
            {
                Traces.ConvertersTraceEvent(TraceEventType.Warning, 1, "FromWeb: Body of message {0} is empty", a.Id);
                return;
            }

            Traces.ConvertersTraceEvent(TraceEventType.Verbose, 1, "RawBody {0}:\r\n{1}", a.Id, a.Body);

            if (string.IsNullOrEmpty(a.Body))
                return;

            // Do a simple "bugfix" for a buggy Forte 6:
            // Replease "<br/>" with" "<br />"
            a.Body = a.Body.Replace("<br/>", NewLine.HtmlBreak).Replace("<br>", NewLine.HtmlBreak);

            // Remove all html code and return "text/plain"...
            if ((UsePlainTextConverter & UsePlainTextConverters.OnlyReceive) == UsePlainTextConverters.OnlyReceive)
            {
                try
                {
                    a.Body = ConvertHtmlBodyToText(a.Body);
                    a.ContentType = "text/plain; charset=utf-8"; // format=flowed ?
                    // TODO: change the body to the correct encoding here... or?
                    //a.ContentType = "text/plain; charset=iso-8859-1"; // converter?
                }
                catch(Exception exp)
                {
                  Traces.ConvertersTraceEvent(TraceEventType.Critical, 1, "Exception in ConvertHtmlBodyToText: {0}, Text:\r\n{1}", exp, a.Body);
                }
            }
            Traces.ConvertersTraceEvent(TraceEventType.Verbose, 1, "FromWeb: Body {0} after conversion:\r\n{1}", a.Id, a.Body);
        }


        private static readonly Regex FindNewsreaderTagRegex = new Regex(@"<a\s+name=(""|')?[^\""']*CommunityBridge(""|')?\s+title=(""|')?([^\'"">]+)(""|')?\s*/?>",
                                            RegexOptions.CultureInvariant | RegexOptions.Compiled | RegexOptions.Multiline | RegexOptions.IgnoreCase);
        private void FindNewsreaderTag(Article a)
        {
            try
            {
                var res = FindNewsreaderTagRegex.Match(a.Body);
                if (res.Success)
                {
                    string v = res.Groups[4].Value;
                    if (string.IsNullOrEmpty(v) == false)
                        a.XNewsreader = v;
                }
            }
            catch
            {
            }
        }

        private const string MvpXFace = @"gUfQ}Xgb$](h.`NjKoSY2tIJ\n_?&{jE!6B_f-evT`5,TuJ=)p=(DJKQT<j`DdAFd;" + "\"" + @"\\:_";
        private const string MsftXFace = @"#bRNK#0dGsQ3qsH_{2h2^s[!NX%mqI.IEv|@oGX9lR|1/zeCG/*9mtv^;pDG-0&?'Z<|O=X";
        private const string AdminXFace = @"hIFgO0(]8spLbgL+g9,+H46Gq[HW)[rbSth_]3BBYBHlL9(K-:qKv{t{J}o<W;{+D]Q>y!.@77<MSLg";

        private const string BodyTextSignatureLine = @"-- ";

        static string EncodeHeaderText(string text, Encoding enc)
        {
            if (string.IsNullOrEmpty(text))
                return string.Empty;

            // First try: Search if the test does contain non-ASCII characters:
            bool onlyAscii = true;
            foreach(var c in text)
            {
                if (c > 0x7F)
                {
                    onlyAscii = false;
                    break;
                }
            }
            if (onlyAscii) return text;

            // There are non-ASCII characters... so encode it...

            // TODO: Add supporte for "QuotePrintable"...
            //string subject = NNTPServer.MimePart.EncodeQuotedPrintable(text, Encoding.UTF8);
            //if (subject.Length*3 > text.Length)
            //    subject = 


            // Currently encode with utf8:
            text = Convert.ToBase64String(enc.GetBytes(text));
            return string.Format("=?{0}?B?{1}?=", enc.HeaderName, text);
        }


        #endregion

        #region NewArticleFromClient

        //[Flags]
        //enum StateFlags
        //{
        //    CppCodeTagActive = 0x01,
        //    VbCodeTagActive = 0x02,
        //    CsCodeTagActive = 0x04,
        //    CodeTagActive = 0x08,
        //}

        /// <summary>
        /// Converts some fields from from Article to other values. For example "Body" or "Subject".
        /// This function is called after a newclient has posted a new message to the server and before the article is sent to the web service.
        /// INFO: This method mustr be threadsafe!
        /// </summary>
        /// <param name="a"></param>
        private static Regex ContentTypeFormatFlowedRegex = new Regex(@"format\s*=\s*""?flowed""?", RegexOptions.CultureInvariant | RegexOptions.Compiled | RegexOptions.Multiline | RegexOptions.IgnoreCase);
        private static Regex ContentTypeFormatFlowedRemoveFirstSpaceRegex = new Regex(@"^\s\s", RegexOptions.CultureInvariant | RegexOptions.Compiled | RegexOptions.Multiline | RegexOptions.IgnoreCase);
        public void NewArticleFromClient(NNTPServer.Article a)
        {
            // TODO: Decoding of the subject:
            // For example: Re: Test with non-ASCII =?ISO-8859-15?Q?-=E4-?=

            Traces.ConvertersTraceEvent(TraceEventType.Verbose, 1, "FromClient: {0}:\r\n{1}", a.Subject, a.Body);

            if ((a.ContentType.IndexOf("text/plain", StringComparison.Ordinal) >= 0) || (string.IsNullOrEmpty(a.ContentType) == true))
            {

                // Check if we have format=flowed was used by the client...
                // 1. real format=flowed
                bool formatFlowed = ContentTypeFormatFlowedRegex.IsMatch(a.ContentType); 
                if (formatFlowed) // real format=flowed
                {
                    // see http://www.ietf.org/rfc/rfc3676.txt (4.4.  Space-Stuffing)
                    a.Body = ContentTypeFormatFlowedRemoveFirstSpaceRegex.Replace(a.Body, m => " ");
                }
                // 2. pseudo format=flowed (for 
                else if (PostsAreAlwaysFormatFlowed)
                    formatFlowed = true;

                if (TabAsSpace > 0)
                {
                    // replace tab (\t) with spaces ... normally this is a job for newsreader (text editor)
                    a.Body = a.Body.Replace("\t", new string(' ', TabAsSpace));
                }

                if ((UsePlainTextConverter & UsePlainTextConverters.OnlySend) == UsePlainTextConverters.OnlySend)
                {
                    try
                    {
                        string body = a.Body.Replace(NewLine.Windows, NewLine.Unix);

                        // Encode the whole body with html-tags
                        body = System.Web.HttpUtility.HtmlEncode(body);

                        // Replace "> xxx" with <blockquote>xxx</blockquote>
                        body = QuoteToHtmlCode(body);

                        // Replace code tags to <pre ...>   !!!! run QuoteToHtmlCode before!!! 
#if PLAINTEXTCONVERTER_ENABLE_BUGFIX_FOR_FORUM
                        body = CodeTagsToHtmlCode(body, a.Newsgroups.StartsWith("Answers.",StringComparison.InvariantCultureIgnoreCase), UseCodeColorizer);
#else
                        body = CodeTagsToHtmlCode(body, false, UseCodeColorizer);
#endif

                        // Replace "*abc*" to "<strong>abc</strong>", "/abc/" ...
                        body = PlainFormatTagsToHtml(body);

                        // convert user defined tags
                        body = UserDefinedTagsToHtml(body);

                        // Automatically return http-links as html-code
                        body = TextHttpToHttpLink(body);

                        // Replace \n with <br /> + "-- " => <hr />
                        body = TextNewLinesToHtmlBr(body, formatFlowed);

                        // user-defined styles
                        body = AddStylesToHtmlTags(body);

                        a.Body = OptimizeHtmlLayout(body).Replace(NewLine.Unix, NewLine.Windows);

                    }
                    catch (Exception exp)
                    {
                      Traces.ConvertersTraceEvent(TraceEventType.Critical, 1, "Exception in PlainTextConverter from NewArticleFromClient: {0}, Text:\r\n{0}", exp, a.Body);
                    }
                }
                else
                {

                    string[] lines = a.Body.Split('\n');
                    var sb = new StringBuilder();
                    for (int i = 0; i < lines.Length; i++)
                    {
                        string line = lines[i];

                        line = line.Replace("\r", string.Empty);

                        // Encode the whole body with html-tags
                        line = System.Web.HttpUtility.HtmlEncode(line);

                        // http(s) to <a href..>... <a>
                        line = TextHttpToHttpLink(line);

                        // Preserve white-space:
                        // If the first character is a space, replace it with "&nbsp;"
                        // Replace all double space ("  ") into a "&nbsp; "
                        line = HtmlTextSpaces(line);

                        // <div>...</div> encoding
                        sb.Append("<div>");
                        if (string.IsNullOrEmpty(line))
                            sb.Append("&nbsp;");
                        else
                            sb.Append(line);
                        sb.Append("</div>\r\n");
                    } // for each line

                    a.Body = sb.ToString();
                }

                // Mark the new body as html:
                if (string.IsNullOrEmpty(a.ContentType))
                {
                    // Set the content-type to "text/html if none was specified...
                    // this is needed, otherwise the Posting function will encode this text again with html...
                    a.ContentType = "text/html; charset=utf-8";
                }
                else
                {
                    a.ContentType = a.ContentType.Replace("text/plain", "text/html");
                }

                Traces.ConvertersTraceEvent(TraceEventType.Verbose, 1, "FromClient after conversion: {0}:\r\n{1}", a.Subject, a.Body);
            }
            return;
        }

        private string UserDefinedTagsToHtml(string text)
        {

            if (UserDefinedTags != null)
            {
                foreach (var tag in UserDefinedTags)
                {
                    try
                    {
                        text = UserDefinedTagToHtml(text, tag);
                    }
                    catch (Exception exp)
                    {
                      Traces.ConvertersTraceEvent(TraceEventType.Critical, 1, "Exception in UserDefinedTagsToHtml (Tag {0}): {1}, Text:\r\n{2}", tag.TagName, exp, text);
                    }
                }
            }

            // additional tags:
            var tempTag = new UserDefinedTag();
            tempTag.HtmlText = "<s>{TEXT}</s>";
            tempTag.TagName = "del";
            text = UserDefinedTagToHtml(text, tempTag);
           
            return text;

        }

        private string UserDefinedTagToHtml(string text, UserDefinedTag tag)
        {
            // erlaubt: [TAG={TEXT1}]{TEXT2}[/TAG]  => HmtlText: xxxx {TEXT1} + {TEXT2} + {TEXT1} + xxx  ... {TEXT} = {TEXT2}
            //          [TAG]{TEXT}[/TAG]           =>           xxxxx {TEXT} xxx {TEXT} + xxxx

            string regexSearchStringLeft = string.Format(@"\[{0}([^\]]*)\]", tag.TagName);
            string regexSearchStringRight = string.Format(@"\[/{0}\]", tag.TagName);
            /// ??? Warum funktioniert "\[{0}=?([^\]]*)\]" nicht? ... derzeitige Notlösung: abschneiden von "=", wenn text1.length > 1;


            Regex PlainFormatTagRegexLeft = new Regex(regexSearchStringLeft, RegexOptions.CultureInvariant | RegexOptions.Compiled | RegexOptions.Multiline | RegexOptions.IgnoreCase | RegexOptions.RightToLeft);
            Regex PlainFormatTagRegexRight = new Regex(regexSearchStringRight, RegexOptions.CultureInvariant | RegexOptions.Compiled | RegexOptions.Multiline | RegexOptions.IgnoreCase);

            string newText = text;

            Match regexRight = PlainFormatTagRegexRight.Match(newText);
            while (regexRight.Success)
            {

                Match regexLeft = PlainFormatTagRegexLeft.Match(newText, regexRight.Index);
                string text1 = string.Empty;
                string text2 = string.Empty;

                int cutPosLeft = -1;
                int cutPosRight = regexRight.Index + regexRight.Length;

                if (regexLeft.Success)
                {
                    if (TextTagInsideCodeTag(newText.Substring(0, regexLeft.Index)))
                    {
                        cutPosLeft = -1;
                    }
                    else
                    {
                        cutPosLeft = regexLeft.Index;

                        if (cutPosLeft < 0) cutPosLeft = 0;

                        text1 = System.Web.HttpUtility.HtmlDecode(regexLeft.Groups[1].Value);
                        if (text1.Length > 1) text1 = text1.Substring(1);

                        int endStartTagIndex = regexLeft.Index + regexLeft.Length;
                        text2 = System.Web.HttpUtility.HtmlDecode(newText.Substring(endStartTagIndex, regexRight.Index - endStartTagIndex));
                    }
                }

                if (cutPosLeft >= 0 && cutPosLeft < cutPosRight)
                {
                    string tagReplaceString = tag.HtmlText.Replace("{TEXT1}", text1)
                                                          .Replace("{TEXT2}", System.Web.HttpUtility.HtmlEncode(text2))
                                                          .Replace("{TEXT}", text2);

                    newText = newText.Substring(0, cutPosLeft)
                              + tagReplaceString
                              + newText.Substring(cutPosRight);
                }

                regexRight = PlainFormatTagRegexRight.Match(newText, cutPosRight);

            }

            return newText;
        }

        private static readonly Regex TextHttpToHttpLinkRegex = new Regex(@"("")?(((f|ht){1}tps?://)(&amp;|[-a-zA-Z0-9@:%_\+.~#?=/!\[\]\\])+)", 
            RegexOptions.CultureInvariant | RegexOptions.Compiled | RegexOptions.Multiline | RegexOptions.IgnoreCase);
        private static string TextHttpToHttpLink(string text)
        {

            string result = TextHttpToHttpLinkRegex.Replace(text, delegate(Match m)
            {
                if (m.Groups[1].Value.Length > 0 || TextTagInsideCodeTag(text.Substring(0, m.Index)))
                    return m.Value;
                else
                    // INFO: The string was previously encoded, so decode at least the href-value:
                    return string.Format("<a href=\"{0}\" target=_blank>{1}</a>", m.Value.Replace("&amp;", "&"), m.Value);
            });

            return result;
        }

        #endregion


        #region HtmlBodyToPlainText

        private string ConvertHtmlBodyToText(string html)
        {
            
            //string htmlText = html.Replace("\r", string.Empty);
            string htmlText = html.Replace(NewLine.Windows, NewLine.Unix).Replace(NewLine.Mac, NewLine.Unix);

            // convert "xxx <strong>abc </strong>xxx" to "xxx<strong>abc</strong> xxx"
            //    => "xxx *abc* xxx instead of "xxx *abc *xxx"
            htmlText = htmlText.Replace("<strong> ", " <strong>").Replace(" </strong>", "</strong> ");
            htmlText = htmlText.Replace("<b> ", " <b>").Replace(" </b>", "</b> ");
            htmlText = htmlText.Replace(NewLine.HtmlBreak + "</b>", "</b>" + NewLine.HtmlBreak);

            htmlText = htmlText.Replace("<em> ", " <em>").Replace(" </em>", "</em> ");
            htmlText = htmlText.Replace("<u> ", " <u>").Replace(" </u>", "</u> ");

            htmlText = htmlText.Replace(NewLine.HtmlBreak + NewLine.Unix + "</blockquote>", "</blockquote>");
            htmlText = htmlText.Replace(NewLine.HtmlBreak + "</blockquote>", "</blockquote>");

            htmlText = htmlText.Replace("<hr>" + NewLine.Unix, "<hr>");
            htmlText = htmlText.Replace(NewLine.Unix + "<hr>", "<hr>");

            htmlText = htmlText.Replace("</pre>" + NewLine.Unix, "</pre>");

            HtmlDocument doc = new HtmlDocument();
            doc.LoadHtml(htmlText);

            StringWriter sw = new StringWriter();
            ConvertTo(doc.DocumentNode, sw);

            sw.Flush();

            string bodyText = sw.ToString();

            bodyText = BlockquoteToQuotemarkReplace(bodyText, AutoLineWrap);

            bodyText = AddLineBreakToLongLines(bodyText, AutoLineWrap);

            return bodyText.Replace(NewLine.Unix, NewLine.Windows);

        }

        private static readonly Regex RemoveLineBreakRegex = new Regex(@"(-)$", RegexOptions.CultureInvariant | RegexOptions.Compiled | RegexOptions.Multiline | RegexOptions.IgnoreCase);
        private int ListTagItemNumber = 0;
        private Boolean ListTagFirstLine = true;
        private int ListTagLevel = 0;
        private const string ListTagUnsortedList = "* ";
        private const string ListTagOrderedList = "#. "; // # ... replaced with number
        private const string ListTagLevelIndent = "    ";
        private void ConvertTo(HtmlNode node, TextWriter outText)
        {
            // modified code sample: HtmlAgilityPack.1.4.0.Source ... Html2Txt / HtmlConvert.cs

            string html;
            switch (node.NodeType)
            {
                case HtmlNodeType.Comment:
                    // don't output comments
                    break;

                case HtmlNodeType.Document:
                    ConvertContentTo(node, outText);
                    break;

                case HtmlNodeType.Text:
                    // script and style must not be output

                    HtmlAgilityPack.HtmlNode textParentNode = node.ParentNode;
                    string parentName = textParentNode.Name;

                    if ((parentName == "script") || (parentName == "style"))
                        break;

                    // get text
                    html = ((HtmlTextNode)node).Text;

                    // is it in fact a special closing node output as text?
                    if (HtmlNode.IsOverlappedClosingElement(html))
                        break;

                    // convert ...
                    string plaintext = RemoveLineBreakRegex.Replace(html, delegate(Match m)
                    {
                        return m.Groups[1] + " " + NewLine.Unix;
                    });
                    plaintext = System.Web.HttpUtility.HtmlDecode(plaintext.Replace(NewLine.Unix, String.Empty));

                    switch (parentName)
                    {
                        case "u":
                            if (!textParentNode.HasChildNodes)
                            {
                                plaintext = ConvertFormatedLines(plaintext, "_");
                            }
                            break;
                        case "span":
                            // <span style="xxxx;">underline</span>
                            {
                                var a = textParentNode.Attributes["style"];
                                if (a != null)
                                {
                                    string style = a.Value;

                                    //text-decoration: line-through;
                                    if (style.IndexOf("line-through", StringComparison.InvariantCultureIgnoreCase) > 0)
                                        plaintext = "[del]" + plaintext + "[/del]";

                                    //text-decoration: underline;
                                    if (style.IndexOf("underline", StringComparison.InvariantCultureIgnoreCase) > 0)
                                        plaintext = ConvertFormatedLines(plaintext, "_");
                                }
                            }
                            break;
                        case "strong":
                        case "b":
                            if (!textParentNode.HasChildNodes)
                            {
                                plaintext = ConvertFormatedLines(plaintext, "*");
                            }
                            break;
                        case "em":
                        case "i":
                            if (!textParentNode.HasChildNodes)
                            {
                                plaintext = ConvertFormatedLines(plaintext, "/");
                            }
                            break;
                        case "li":
                            {
                                HtmlAgilityPack.HtmlNode listItemParentNode = textParentNode.ParentNode;
                                string linePrefix = new string(' ', ListTagUnsortedList.Length); // Default: UL-Syntax
                                if (listItemParentNode.Name.Equals("ol", StringComparison.InvariantCultureIgnoreCase))
                                {
                                    linePrefix = new string(' ', ListTagItemNumber.ToString().Length + ListTagOrderedList.Length - 1);
                                }

                                for (int i = 1; i < ListTagLevel; i++)
                                {
                                    linePrefix = ListTagLevelIndent + linePrefix;
                                }

                                if (ListTagFirstLine)
                                {
                                    ListTagFirstLine = false;
                                }
                                else
                                {
                                    plaintext = linePrefix + plaintext;
                                }

                                plaintext = AddLineBreakToLongLine(plaintext, AutoLineWrap - linePrefix.Length, linePrefix);
                            }
                            break;
                    } // switch (parentName)
 
                    outText.Write(plaintext);
                    break;

                case HtmlNodeType.Element:
                    {
                        // some settings befor convert node text
                        switch (node.Name)
                        {
                            case "ul":
                            case "ol":
                                ListTagLevel += 1;
                                break;
                            case "li":
                                ListTagFirstLine = true;
                                break;
                        }
                        // Convert the content of the node to text:
                        var sw = new StringWriter();

                        if (!node.Name.Equals("pre",StringComparison.InvariantCultureIgnoreCase))
                            ConvertContentTo(node, sw);

                        switch (node.Name)
                        {
                            case "pre": // <pre lang="x-cpp">  ==>  [code=cpp]
                                HtmlAgilityPack.HtmlAttributeCollection nodeAttributes = node.Attributes;
                                string lang = String.Empty;
                                if (nodeAttributes.Contains("lang"))
                                {
                                    HtmlAgilityPack.HtmlAttribute langAttribute = nodeAttributes["lang"];
                                    lang = langAttribute.Value;
                                    if (lang.IndexOf("x-", StringComparison.InvariantCultureIgnoreCase) == 0)
                                        lang = lang.Substring(2);
                                }

                                string langAttributeString = String.Empty;
                                string langTag;
                                switch (lang.ToLower(System.Globalization.CultureInfo.InvariantCulture))
                                {
                                    case CodeTag.cpp:
                                    case CodeTag.csharp:
                                    case CodeTag.vbnet:
                                    case CodeTag.sql:
                                    case CodeTag.html:
                                        langTag = lang;
                                        break;
                                    default:
                                        langTag = "code";
                                        if (lang.Length > 0) langAttributeString = "=" + lang;
                                        break;
                                }
                                if (langAttributeString.Length > 0) langTag += langAttributeString;

                                if (node.PreviousSibling != null)
                                    outText.Write(NewLine.Unix + NewLine.Unix);

                                outText.Write("[" + langTag + "]" + NewLine.Unix);
                                node.InnerHtml = node.InnerHtml.Replace("<br>", NewLine.Unix).Replace("<br />", NewLine.Unix);
                                outText.Write(System.Web.HttpUtility.HtmlDecode(node.InnerText));
                                outText.Write(NewLine.Unix + "[/" + langTag + "]");

                                if (node.NextSibling != null)
                                {
                                    if (node.NextSibling.Name.Equals("#text", StringComparison.InvariantCultureIgnoreCase))
                                        outText.Write(NewLine.Unix);
                                }

                                break;
                            case "p": // blank line if <p> (difference between <br>!)
                                if (node.PreviousSibling != null)
                                {
                                    if (!node.PreviousSibling.Name.Equals("hr", StringComparison.InvariantCultureIgnoreCase))
                                    {
                                        outText.Write(NewLine.Unix);

                                        // special  case: blockquote
                                        string previousNodeName = node.PreviousSibling.Name;
                                        if (previousNodeName.Equals("#text", StringComparison.InvariantCultureIgnoreCase))
                                        {
                                            var textNodePrevious = node.PreviousSibling.PreviousSibling;
                                            if (textNodePrevious != null)
                                                previousNodeName = textNodePrevious.Name;
                                        }

                                        if (!previousNodeName.Equals("blockquote", StringComparison.InvariantCultureIgnoreCase))
                                        {
                                            outText.Write(NewLine.Unix);
                                        }
                                    }
                                }
                                outText.Write(sw.ToString());
                                break;
                            case "br": // normal line break
                            case "div": // => move to <p>-Block?
                                outText.Write(NewLine.Unix);
                                outText.Write(sw.ToString());
                                break;
                            case "hr": // <hr class="sig">
                                outText.Write(NewLine.Unix + NewLine.Unix + BodyTextSignatureLine + NewLine.Unix);
                                outText.Write(sw.ToString());
                                break;
                            case "blockquote": // convert blockquote to newsreader style => BlockquoteToQuotemarkReplace
                                {
                                    var parentnode = node.ParentNode;
                                    if (parentnode != null)
                                    {
                                        if (!parentnode.Name.Equals("blockquote", StringComparison.InvariantCultureIgnoreCase))
                                            outText.Write(NewLine.Unix);
                                    }
                                    else
                                        outText.Write(NewLine.Unix);
                                }
                                outText.Write(NewLine.Unix + "<blockquote>");
                                outText.Write(sw.ToString());
                                outText.Write(NewLine.Unix + "</blockquote>" + NewLine.Unix);
                                break;
                            case "img":
                                {
                                    string src = String.Empty;
                                    string desc = String.Empty;

                                    var a = node.Attributes["src"];
                                    if (a != null)
                                    {
                                        src = a.Value;
                                    }
                                    if (src.Length == 0) // no scr => no image ;-)
                                    {
                                        break;
                                    }

                                    outText.Write("<" + src + ">");
                                    outText.Write(sw.ToString());
                                }
                                break;
                            case "ul":
                            case "ol":
                                ListTagItemNumber = 0;
                                if (ListTagLevel == 1)
                                    outText.Write(NewLine.Unix);
                                outText.Write(sw.ToString());
                                ListTagLevel -= 1;
                                break;
                            case "li":
                                outText.Write(NewLine.Unix);
                                {
                                    HtmlAgilityPack.HtmlNode parentNode = node.ParentNode;
                                    if (parentNode != null)
                                    {
                                        string listMark = ListTagUnsortedList;
                                        ListTagFirstLine = true;
                                        if (parentNode.Name.Equals("ol", StringComparison.InvariantCultureIgnoreCase))
                                        {
                                            ListTagItemNumber += 1;
                                            listMark = ListTagOrderedList.Replace("#", ListTagItemNumber.ToString());
                                        }

                                        for (int i = 1; i < ListTagLevel; i++)
                                        {
                                            listMark = ListTagLevelIndent + listMark;
                                        }

                                        outText.Write(listMark);
                                    }
                                }
                                outText.Write(sw.ToString());
                                break;
                            case "u":
                                if (node.HasChildNodes)
                                {
                                    outText.Write("_");
                                    outText.Write(sw.ToString());
                                    outText.Write("_");
                                }
                                break;
                            case "em":
                            case "i":
                                if (node.HasChildNodes)
                                {
                                    outText.Write("/");
                                    outText.Write(sw.ToString());
                                    outText.Write("/");
                                }
                                break;
                            case "strong":
                            case "b":
                                if (node.HasChildNodes)
                                {
                                    outText.Write("*");
                                    outText.Write(sw.ToString());
                                    outText.Write("*");
                                }
                                break;
                            case "s":
                            case "stroke":
                                if (node.HasChildNodes)
                                {
                                    outText.Write("[del]");
                                    outText.Write(sw.ToString());
                                    outText.Write("[/del]");
                                }
                                break;
                            case "a":
                                {
                                    string href = null;
                                    var a = node.Attributes["href"];
                                    if (a != null)
                                    {
                                        //href = System.Web.HttpUtility.UrlDecode(a.Value); // work item #6548
                                        href = a.Value;
                                        if (string.IsNullOrEmpty(href) == false)
                                        {
                                            // INFO: Compare the url *without* the leading "http(s)://" so we also can have <a href> like
                                            // <a href="http://www.codeplex.de/">www.codeplex.de</a>
                                            // This also leads to removing the last "/".
                                            // See also: http://communitybridge.codeplex.com/workitem/6365
                                            string internalText = sw.ToString().Trim().Trim('"', '\'');
                                            if (internalText.IndexOf("http://", StringComparison.InvariantCultureIgnoreCase) == 0)
                                                internalText = internalText.Substring(7);
                                            else if (internalText.IndexOf("https://", StringComparison.InvariantCultureIgnoreCase) == 0)
                                                internalText = internalText.Substring(8);
                                            internalText = internalText.TrimEnd('/');

                                            string internalHref = href;
                                            if (internalHref.IndexOf("http://", StringComparison.InvariantCultureIgnoreCase) == 0)
                                                internalHref = internalHref.Substring(7);
                                            else if (internalHref.IndexOf("https://", StringComparison.InvariantCultureIgnoreCase) == 0)
                                                internalHref = internalHref.Substring(8);
                                            internalHref = internalHref.TrimEnd('/');

                                            if (string.Compare(internalText, internalHref, StringComparison.InvariantCultureIgnoreCase) == 0)
                                            {
                                                outText.Write(href.Trim());
                                            }
                                            else
                                            {
                                                outText.Write(sw.ToString());
                                                outText.Write(" <" + href + ">");
                                            }
                                        }
                                        else
                                            outText.Write(sw.ToString());
                                    }
                                    else
                                        outText.Write(sw.ToString());
                                }
                                break;
                            default:
                                outText.Write(sw.ToString());
                                break;
                        } // switch node.Name
                    }
                    break;
            }  // switch node.NodeType
        }
        
        private void ConvertContentTo(HtmlNode node, TextWriter outText)
        {
            if (node == null)
                return;
            if (node.ChildNodes == null)
                return;
            foreach (HtmlNode subnode in node.ChildNodes)
            {
                ConvertTo(subnode, outText);
            }
        }


        private static readonly Regex FirstNonSpaceCharRegex = new Regex(@"[^\s]+", RegexOptions.CultureInvariant | RegexOptions.Compiled | RegexOptions.Multiline | RegexOptions.IgnoreCase);
        private static string ConvertFormatedLines(string text, string formatTag)
        {

            if (text.Trim().Length == 0)
            {
                return string.Empty;
            }

            string result = text.TrimEnd().Replace(NewLine.Unix, formatTag + NewLine.Unix + formatTag).Replace(" <br>", formatTag + "<br>" + formatTag) + formatTag;
            Match firstNonSpace = FirstNonSpaceCharRegex.Match(result);
            if (firstNonSpace.Success)
            {
                result = result.Substring(0, firstNonSpace.Index) + formatTag + result.Substring(firstNonSpace.Index);
            }
            return result;
        }


        // "<blockquote>xxx</blockqoute>" to "> xxx"
        private static readonly Regex BlockquoteNewLine1Regex = new Regex(@"(.)<(/)?blockquote>", RegexOptions.CultureInvariant | RegexOptions.Compiled | RegexOptions.Multiline | RegexOptions.IgnoreCase);
        private static readonly Regex BlockquoteNewLine2Regex = new Regex(@"<(/)?blockquote>(.)", RegexOptions.CultureInvariant | RegexOptions.Compiled | RegexOptions.Multiline | RegexOptions.IgnoreCase);
        private const char QuoteblockLinePrefix = '>';
        private static string BlockquoteToQuotemarkReplace(string body, int maxLineLen)
        {
            // @todo identify difference between /hard line breaks/ and /soft line breaks/ => floating text vs. lines

            body = BlockquoteNewLine1Regex.Replace(body, delegate(Match m)
            {
                return m.Groups[1].Value + NewLine.Unix + String.Format("<{0}blockquote>", m.Groups[2].Value);
            });

            body = BlockquoteNewLine2Regex.Replace(body, delegate(Match m)
            {
                return String.Format("<{0}blockquote>", m.Groups[1].Value) + NewLine.Unix + m.Groups[2].Value;
            });

            string[] lines = body.Split(NewLine.Unix.ToCharArray());

            Int32 quoteLevel = 0;
            Boolean firstNonQuote = false;
            Boolean newQuoteInsideQuote = false;

            Int32 maxQuoteLevel = 0;

            string q = String.Empty;

            string removeLineCode = "@REMOVELINE|" + Guid.NewGuid().ToString() + "|REMOVELINE@";

            for (int i = 0; i < lines.Length; i++)
            {
                String temp = lines[i];
                if (temp.IndexOf("<blockquote>", StringComparison.InvariantCultureIgnoreCase)==0)
                {
                    quoteLevel += 1;
                    if (quoteLevel > maxQuoteLevel) maxQuoteLevel = quoteLevel;
                    newQuoteInsideQuote = true;
                    q = new string(QuoteblockLinePrefix, quoteLevel) + " ";
                }
                else if (temp.IndexOf("</blockquote>", StringComparison.InvariantCultureIgnoreCase) == 0)
                {
                    quoteLevel -= 1;
                    q = new string(QuoteblockLinePrefix, quoteLevel) + " ";
                    if (quoteLevel == 0)
                    {
                        firstNonQuote = true;
                    }

                    if (lines[i - 1].Equals(QuoteblockLinePrefix + q, StringComparison.InvariantCultureIgnoreCase))
                    {
                        if (firstNonQuote)
                            lines[i - 1] = String.Empty;
                        else
                            lines[i - 1] = removeLineCode;
                         
                    }

                }
                else if (quoteLevel > 0)
                {
                    if (newQuoteInsideQuote && temp.Trim().Length == 0)
                    {
                        temp = removeLineCode;
                    }
                    else
                    {
                        temp = q + AddLineBreakToLongLine(temp, maxLineLen, q);
                    }
                    newQuoteInsideQuote = false;
                }
                
                if (firstNonQuote)
                {
                    firstNonQuote = false;
                    temp = NewLine.Unix + temp;
                }

                lines[i] = temp;

            }

            body = String.Join(NewLine.Unix, lines).Replace(removeLineCode + NewLine.Unix, String.Empty);

            // remove blank line after "xxxx wrote":
            Regex optimizeBlockquote = new Regex(@"^(.*(wrote|schrieb|fragte|meinte).*)\r?\n\r?\n<blockquote>", RegexOptions.CultureInvariant | RegexOptions.Compiled | RegexOptions.Multiline | RegexOptions.IgnoreCase);
                                                    // @todo add key words
            body = optimizeBlockquote.Replace(body, delegate(Match m)
            {
                return m.Groups[1].Value;
            });

            body = body.Replace("<blockquote>" + NewLine.Unix, string.Empty);

            body = body.Replace("</blockquote>" + NewLine.Unix + NewLine.Unix, "</blockquote>" + NewLine.Unix);
            body = body.Replace(NewLine.Unix + "</blockquote>", string.Empty);

            if (body.IndexOf(NewLine.Unix + NewLine.Unix + ">", StringComparison.InvariantCultureIgnoreCase) == 0)
                body = body.Substring(2);
            // @todo check ">> xxx" and not ">>xxxx"

            return body;
        }

        private static string AddLineBreakToLongLines(string text, int maxLineLen)
        {

            // @todo no word wrap inside code tags

            // Check if this option is disabled:
            if (maxLineLen <= 0)
                return text;

            string[] lines = text.Split(NewLine.Unix.ToCharArray());

            for (int i = 0; i < lines.Length; i++)
            {
                lines[i] = AddLineBreakToLongLine(lines[i], maxLineLen, string.Empty);
            }

            return String.Join(NewLine.Unix, lines); ;

        }

        private static readonly Regex SplitLineCharRegexRL = new Regex(@"(\s)", RegexOptions.CultureInvariant | RegexOptions.Compiled | RegexOptions.Multiline | RegexOptions.IgnoreCase | RegexOptions.RightToLeft);
        private static readonly Regex SplitLineCharRegexLR = new Regex(@"(\s)", RegexOptions.CultureInvariant | RegexOptions.Compiled | RegexOptions.Multiline | RegexOptions.IgnoreCase);
        private static string AddLineBreakToLongLine(string line, int maxLineLen, string linePrefix)
        {

            if (maxLineLen <= 0)
                return line;

            if (line.Length > maxLineLen && line.IndexOf(">",StringComparison.InvariantCultureIgnoreCase)!=0)
            {

                string newLine = string.Empty;
                int splitPos = 0;
                do
                {
                    string tempLine = line.Substring(0, maxLineLen);

                    splitPos = SplitLineCharRegexRL.Match(tempLine).Index;
                    if (splitPos <= 0)
                    {
                        splitPos = SplitLineCharRegexLR.Match(line).Index;
                    }

                    if (splitPos > 1) // 0 or 1 ???
                    {
                        newLine = newLine + line.Substring(0, splitPos + 1) + NewLine.Unix + linePrefix;
                        line = line.Substring(splitPos + 1);
                    }

                } while (line.Length > maxLineLen && splitPos > 1);

                line = newLine + line;

            }

            return line;
        }

        #endregion

        #region PlainTextBodyToHtml

        private static readonly Regex QuoteToHtmlCodeQuoteTextRegex = new Regex(@"\r?\n&gt;", RegexOptions.CultureInvariant | RegexOptions.Compiled | RegexOptions.Multiline | RegexOptions.IgnoreCase);
        private static readonly Regex BlockquoteStartTagRemoveNewLineRegex = new Regex(@"\n?<blockquote>\n?", RegexOptions.CultureInvariant | RegexOptions.Compiled | RegexOptions.Multiline | RegexOptions.IgnoreCase);
        private static readonly Regex BlockquoteEndTagRemoveNewLineRegex = new Regex(@"\n?</blockquote>\n?", RegexOptions.CultureInvariant | RegexOptions.Compiled | RegexOptions.Multiline | RegexOptions.IgnoreCase);
        private static string QuoteToHtmlCode(string text)
        {

            while (QuoteToHtmlCodeQuoteTextRegex.IsMatch(text))
            {
                text = QuoteToHtmlCodeReplace(text);
            }

            text = BlockquoteStartTagRemoveNewLineRegex.Replace(text, delegate(Match m)
            {
                return "<blockquote>";
            });

            text = BlockquoteEndTagRemoveNewLineRegex.Replace(text, delegate(Match m)
            {
                return "</blockquote>";
            });

            return text;

        }


        private static string QuoteToHtmlCodeReplace(string body)
        {

            string[] lines = body.Split(NewLine.Unix.ToCharArray());

            Boolean isQuote = false;

            string removeLineCode = "@REMOVELINE|" + Guid.NewGuid().ToString() + "|REMOVELINE@";

            for (int i = 0; i < lines.Length; i++)
            {
                String temp = lines[i];
                if (temp.Length > 0)
                {

                    if (temp.IndexOf("&gt;", StringComparison.InvariantCultureIgnoreCase) == 0)
                    {
                        if (temp.TrimEnd().Length > 4)
                        {
                            temp = temp.Substring(4, temp.Length - 4);
                            if (temp.Substring(0,1).Equals(" ",StringComparison.InvariantCultureIgnoreCase))
                                temp = temp.Substring(1);

                        }
                        else
                        {
                            if (isQuote == true)
                            {
                                lines[i - 1] += NewLine.HtmlBreak;
                                temp = removeLineCode;
                            }
                            else
                                temp = string.Empty;

                        }

                        if (isQuote == false)
                        {
                            isQuote = true;
                            temp = NewLine.Unix + "<blockquote>" + NewLine.Unix + temp;
                        }

                    }
                    else
                    {
                        if (isQuote)
                        {
                            isQuote = false;
                            if (lines[i - 1].LastIndexOf(NewLine.HtmlBreak) == (lines[i - 1].Length - 6))
                                lines[i - 1] = lines[i - 1].Substring(0, lines[i - 1].Length - 6);

                            temp = "</blockquote>" + NewLine.Unix + temp;
                        }
                    }
                    lines[i] = temp;
                }
            }

            string result = String.Join(NewLine.Unix, lines).Replace(NewLine.Unix + removeLineCode, string.Empty)
                                           .Replace(NewLine.Unix + "</blockquote>", "</blockquote>");

            if (result.LastIndexOf(NewLine.HtmlBreak, StringComparison.InvariantCultureIgnoreCase) == (result.Length - 7))
                result = result.Substring(0, result.LastIndexOf(NewLine.HtmlBreak,StringComparison.InvariantCultureIgnoreCase) + 1);

            return result;

            // <p></p><blockquote
        }

        private const string UnterlineHtmlStartTag = "<u>";
        private const string UnterlineHtmlEndTag = "</u>";
        private static string PlainFormatTagsToHtml(string text)
        {
            //TODO optimize code ... 

            string result = NewLine.Unix + text + NewLine.Unix;

            result = PlainFormatTagToHtml(result, "_*/", "/*_", UnterlineHtmlStartTag + "<strong><em>", "</em></strong>" + UnterlineHtmlEndTag);
            result = PlainFormatTagToHtml(result, "_/*", "*/_", UnterlineHtmlStartTag + "<em><strong>", "</strong></em>" + UnterlineHtmlEndTag);
            result = PlainFormatTagToHtml(result, "*_/", "/_*", "<strong>" + UnterlineHtmlStartTag + "<em>", "</em>" + UnterlineHtmlEndTag + "</strong>");
            result = PlainFormatTagToHtml(result, "*/_", "_*/", "<strong><em>" + UnterlineHtmlStartTag, UnterlineHtmlEndTag + "</strong></em>");
            result = PlainFormatTagToHtml(result, "/*_", "_/*", "<em><strong>"+ UnterlineHtmlStartTag, UnterlineHtmlEndTag+ "</em></strong>");
            result = PlainFormatTagToHtml(result, "/_*", "*_/", "<em>" + UnterlineHtmlStartTag + "<strong>", "</strong>" + UnterlineHtmlEndTag + "</em>");

            result = PlainFormatTagToHtml(result, "*/", "/*", "<strong><em>", "</em></strong>");
            result = PlainFormatTagToHtml(result, "/*", "*/", "<em><strong>", "</strong></em>");

            result = PlainFormatTagToHtml(result, "_/", "/_", UnterlineHtmlStartTag + "<em>", "</em>" + UnterlineHtmlEndTag);
            result = PlainFormatTagToHtml(result, "_/", "/_", "<em>" + UnterlineHtmlStartTag, UnterlineHtmlEndTag + "</em>");

            result = PlainFormatTagToHtml(result, "_*", "*_", UnterlineHtmlStartTag + "<strong>", "</strong>" + UnterlineHtmlEndTag);
            result = PlainFormatTagToHtml(result, "*_", "_*", "<strong>" + UnterlineHtmlStartTag, UnterlineHtmlEndTag + "</strong>");

            result = PlainFormatTagToHtml(result, "*", "*", "<strong>", "</strong>");
            result = PlainFormatTagToHtml(result, "_", "_", UnterlineHtmlStartTag, UnterlineHtmlEndTag);
            result = PlainFormatTagToHtml(result, "/", "/", "<em>", "</em>");

            return result.Substring(1, result.Length - 2);
        }

        private static Regex PlainFormatTagDisableFormatRegex = new Regex(@"(\n\s*\n|<blockquote>|</blockquote>)", RegexOptions.CultureInvariant | RegexOptions.Compiled | RegexOptions.Multiline | RegexOptions.IgnoreCase);
        private static string PlainFormatTagToHtml(string text, string formatTagLeft, string formatTagRight, string replaceTagStart, string replaceTagEnd)
        {

            string regexFormatTagLeft = formatTagLeft.Replace("*", @"\*").Replace("|", @"\|");
            string regexFormatTagRight = formatTagRight.Replace("*", @"\*").Replace("|", @"\|");


            // Regex PlainFormatTagRegexLeft = new Regex(@"(\s|\n|>|-|""|_|\*|/)" + formatTag.Replace("*", @"\*") + @"[^\s]", ...
            string regexSearchStringLeft = @"(\s|\r?\n|>|-|\||""|_|\*|/)";
            if (regexFormatTagLeft.IndexOf("*", StringComparison.InvariantCultureIgnoreCase) >= 0)
            {
                regexSearchStringLeft = regexSearchStringLeft.Replace(@"|\*", string.Empty);
            }
            if (regexFormatTagLeft.IndexOf("_", StringComparison.InvariantCultureIgnoreCase) >= 0)
            {
                regexSearchStringLeft = regexSearchStringLeft.Replace("|_", string.Empty);
            }
            if (regexFormatTagLeft.IndexOf("/", StringComparison.InvariantCultureIgnoreCase) >= 0)
            {
                regexSearchStringLeft = regexSearchStringLeft.Replace("|/", string.Empty);
            }

            // Regex PlainFormatTagRegexRight = new Regex(@"[^\s]" + formatTag.Replace("*", @"\*") + @"(\s|\n|<|\.|!|\?|,|-|""|_|\*|/)",  ...
            string regexSearchStringRight = @"(\s|\r?\n|<|\.|!|\?|,|-|""|_|\*|/)";
            if (regexFormatTagRight.IndexOf("*", StringComparison.InvariantCultureIgnoreCase) >= 0)
            {
                regexSearchStringRight = regexSearchStringRight.Replace(@"|\*", string.Empty);
            }
            if (regexFormatTagRight.IndexOf("_", StringComparison.InvariantCultureIgnoreCase) >= 0)
            {
                regexSearchStringRight = regexSearchStringRight.Replace("|_", string.Empty);
            }
            if (regexFormatTagRight.IndexOf("/", StringComparison.InvariantCultureIgnoreCase) >= 0)
            {
                regexSearchStringRight = regexSearchStringRight.Replace("|/", string.Empty);
            }

            regexSearchStringLeft += regexFormatTagLeft + @"[^\s\n>]";
            regexSearchStringRight = @"[^\s\n<]" + regexFormatTagRight + regexSearchStringRight;

            Regex PlainFormatTagRegexLeft = new Regex(regexSearchStringLeft, RegexOptions.CultureInvariant | RegexOptions.Compiled | RegexOptions.Multiline | RegexOptions.IgnoreCase | RegexOptions.RightToLeft);
            Regex PlainFormatTagRegexRight = new Regex(regexSearchStringRight, RegexOptions.CultureInvariant | RegexOptions.Compiled | RegexOptions.Multiline | RegexOptions.IgnoreCase);
            Match regexMatch = null;

            string newString = string.Empty;
            string oldString = text;
            int formatTagLength = formatTagLeft.Length;

            int splitPos = 0;

            regexMatch = PlainFormatTagRegexRight.Match(oldString);
            while (regexMatch.Success)
            {

                splitPos = regexMatch.Index;

                regexMatch = PlainFormatTagRegexLeft.Match(oldString.Substring(0, splitPos + 1));
                if (regexMatch.Success)
                {

                    if (! (TextTagInsideCodeTag(oldString.Substring(0, splitPos))
                           || PlainFormatTagDisableFormatRegex.IsMatch(
                                    oldString.Substring(regexMatch.Index + 1 + formatTagLength, splitPos - formatTagLength - regexMatch.Index)
                                    )
                          ))
                    {
                        newString += oldString.Substring(0, regexMatch.Index + 1)
                                     + replaceTagStart
                                     + oldString.Substring(regexMatch.Index + 1 + formatTagLength, splitPos - formatTagLength - regexMatch.Index)
                                     + replaceTagEnd;

                        oldString = oldString.Substring(splitPos + 1 + formatTagLength);
                    }
                    else
                    {
                        newString += oldString.Substring(0, splitPos + formatTagLength);
                        oldString = oldString.Substring(splitPos + formatTagLength);
                    }
                    regexMatch = PlainFormatTagRegexRight.Match(oldString);
                }

            }

            newString += oldString;

            return newString; //remove temp line breaks
        }

        private static Regex TextTagInsideCodeEndTagRegex = new Regex(@"</pre>", RegexOptions.CultureInvariant | RegexOptions.Compiled | RegexOptions.Multiline | RegexOptions.IgnoreCase | RegexOptions.RightToLeft);
        private static Regex TextTagInsideCodeStartTagRegex = new Regex(@"<pre", RegexOptions.CultureInvariant | RegexOptions.Compiled | RegexOptions.Multiline | RegexOptions.IgnoreCase | RegexOptions.RightToLeft);
        private static Boolean TextTagInsideCodeTag(string textBeforeTag)
        {
            string testString = " " + textBeforeTag + " "; // reason for " " + ???? .. i don't know, but it is necessary
            Match matchStartTag = TextTagInsideCodeStartTagRegex.Match(testString); 
            if (!matchStartTag.Success)
            {
                return false;
            }

            Match matchEndTag = TextTagInsideCodeEndTagRegex.Match(testString);
            if (!matchStartTag.Success)
            {
                return true;
            }

            if (matchStartTag.Index > matchEndTag.Index)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        private static Regex TextLinesWithSpacesOnlyRegex = new Regex(@"^\s+$", RegexOptions.CultureInvariant | RegexOptions.Compiled | RegexOptions.Multiline | RegexOptions.IgnoreCase | RegexOptions.RightToLeft);
        private static string TextNewLinesToHtmlBr(string body, Boolean formatFlowed)
        {

            string result = String.Empty;

            body = TextLinesWithSpacesOnlyRegex.Replace(body, delegate(Match m)
            {
                return string.Empty;
            });

            string[] lines;

            { // preformt code tag lines

                lines = body.Split(NewLine.Unix.ToCharArray());

                Boolean insideCodeTag = false;
                for (int i = 0; i < lines.Length; i++)
                {

                    if (lines[i].IndexOf("<pre", StringComparison.InvariantCultureIgnoreCase) >= 0)
                    {
                        insideCodeTag = !(lines[i].IndexOf("</pre>", StringComparison.InvariantCultureIgnoreCase) >= 0);
                    }
                    else if (lines[i].IndexOf("</pre>", StringComparison.InvariantCultureIgnoreCase) >= 0)
                    {
                        insideCodeTag = false;
                    }

                    if (insideCodeTag)
                    {
                        lines[i] = lines[i].TrimEnd();
                    }
                }

                body = String.Join(NewLine.Unix, lines);

            }

            // Signature code = "-- " => bug if Replace(" \n", " ")
            // begin
            if (formatFlowed)
            {
                body = (NewLine.Unix + body).Replace(NewLine.Unix + BodyTextSignatureLine + NewLine.Unix, NewLine.Unix + "@@@|SIGNATURELINE|@@@" + NewLine.Unix)
                                        .Replace(" " + NewLine.Unix, " ").Replace("@@@|SIGNATURELINE|@@@" + NewLine.Unix, BodyTextSignatureLine + NewLine.Unix);
                body = body.Substring(1);
            }
            
            // end

            body = body.Replace("</pre></blockquote>", "</pre>" + NewLine.Unix + "</blockquote>");

            lines = body.Split(NewLine.Unix.ToCharArray());
            Boolean isCodeText = false;
            int textlineNr = 0;
            int sigLine = -1;

            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i];

                if (string.Compare(line, BodyTextSignatureLine, StringComparison.InvariantCultureIgnoreCase) == 0)
                {
                    line = "</p><hr class=\"sig\" /><p>";
                    sigLine = 0;
                }

                if (!isCodeText)
                {
                    if (line.Contains("<pre"))
                    {

                        int indexOfPre = line.Substring(line.IndexOf("<pre")).IndexOf(">", StringComparison.InvariantCultureIgnoreCase);
                        int indexOfPreEnd = line.IndexOf("</pre>", indexOfPre + 1, StringComparison.InvariantCultureIgnoreCase);

                        if (indexOfPreEnd > indexOfPre)
                        {
                            line = HtmlTextSpaces(line.Substring(0, indexOfPre))
                                   + CodeTextSpaces(line.Substring(indexOfPre, indexOfPreEnd - indexOfPre))
                                   + HtmlTextSpaces(line.Substring(indexOfPreEnd));
                            line = line.Replace("</pre>", "</pre><p>");
                            textlineNr = 0;
                        }
                        else
                        {
                            isCodeText = true;
                            line = HtmlTextSpaces(line.Substring(0, indexOfPre))
                                   + CodeTextSpaces(line.Substring(indexOfPre));
                        }

                        line = line.Replace("<pre", "</p><pre");

                    }
                    else
                    {
                        if (textlineNr < 1)
                            textlineNr += 1;
                        else if (sigLine >= 0 && sigLine < 1)
                        {
                            sigLine += 1;
                        }
                        else if (line.Trim().Length == 0) // blank line ... new <p> tag
                        {
                            line = "</p><p>";
                            textlineNr = 0;
                        }
                        else
                        {
                            line = NewLine.HtmlBreak + line;
                        }

                        line = HtmlTextSpaces(line);

                    }

                }
                else if (line.Contains("</pre"))
                {

                    if (!line.Equals("</pre>", StringComparison.InvariantCultureIgnoreCase))
                    {

                        int indexOfPreEnd = line.IndexOf("</pre>", StringComparison.InvariantCultureIgnoreCase);
                        if (indexOfPreEnd > 0)
                        {
                            int indexOfPreStart = line.IndexOf("<pre", StringComparison.InvariantCultureIgnoreCase);
                            if (indexOfPreStart > 0 && indexOfPreStart < indexOfPreEnd)
                            {
                                indexOfPreStart = line.Substring(indexOfPreStart + 1).IndexOf(">", StringComparison.InvariantCultureIgnoreCase);
                            }
                            if (indexOfPreStart < 0) indexOfPreStart = 0;

                            line = CodeTextSpaces(line.Substring(indexOfPreStart, indexOfPreEnd))
                                   + line.Substring(indexOfPreEnd);

                            indexOfPreEnd = line.IndexOf("</pre>",indexOfPreStart, StringComparison.InvariantCultureIgnoreCase);


                            if (line.Length >= indexOfPreEnd && line.Substring(indexOfPreEnd).Contains("<pre"))
                            {
                                indexOfPreStart = line.Substring(indexOfPreEnd).IndexOf("<pre", StringComparison.InvariantCultureIgnoreCase);
                                indexOfPreStart = line.Substring(indexOfPreStart).IndexOf(">", StringComparison.InvariantCultureIgnoreCase);

                                indexOfPreEnd = line.Substring(indexOfPreStart).IndexOf("</pre>", StringComparison.InvariantCultureIgnoreCase);
                                if (indexOfPreEnd < 0) indexOfPreEnd = line.Length - 1;

                                line = HtmlTextSpaces(
                                            line.Substring(0, indexOfPreStart))
                                            + CodeTextSpaces(line.Substring(indexOfPreStart + 1, indexOfPreEnd - indexOfPreStart - 1)
                                       );
                            }
                            else
                            {
                                isCodeText = false;
                            }


                        }

                    } // !line.Equals("</pre>"
                    else
                    {
                        isCodeText = false;
                    }

                    line = line.Replace("</pre>", "</pre><p>");

                    if (i < (lines.Length - 1))
                    {
                        //if (System.Web.HttpUtility.HtmlDecode(lines[i + 1]).Trim().Length == 0)
                        if (lines[i + 1].Trim().Length == 0)
                        {
                            lines[i + 1] = string.Empty;
                            textlineNr = -1;
                        }
                        else
                            textlineNr = 0;
                    }


                } // line.Contains("</pre"))
                else if (isCodeText)
                {
                    // dirty - but effective to keep spaces in <pre> :-)
                    line = CodeTextSpaces(line);
                }

                lines[i] = line;

            }

            result = String.Join(NewLine.Unix, lines);

            result = result.Replace("<blockquote>", "</p><blockquote><p>")
                           .Replace("</blockquote>", "</p></blockquote><p>");
                         
            result = result.Replace(NewLine.Unix + "</p>", "</p>").Replace("<p>" + NewLine.Unix, "<p>");

            result = result.Replace(NewLine.Unix + "<p>", "<p>").Replace("</p>" + NewLine.Unix, "</p>");

            result = result.Replace("<p></p><blockquote>", "<blockquote>").Replace("</blockquote><p></p>", "</blockquote>");

            //delete first blank line before sig block:
            result = result.Replace("<p></p><hr class=\"sig\" />", "<hr class=\"sig\" />");

            result = result.Replace("<hr class=\"sig\" /><p>" + NewLine.HtmlBreak, "<hr class=\"sig\" /><p>");

            result = result.Replace(NewLine.Unix + NewLine.HtmlBreak, NewLine.HtmlBreak); // wegen forum, da sonst " <br>" entsteht

            result = result.Replace("</pre><p>" + NewLine.HtmlBreak, "</pre><p>");
            result = result.Replace("</pre><p></p>", "</pre>");
            

            result = "<p>" + result + "</p>";

            if (result.LastIndexOf("<p></p>", StringComparison.InvariantCultureIgnoreCase) == (result.Length - 7))
                result = result.Substring(0, result.Length - 7);

            if (result.IndexOf("<p></p>", StringComparison.InvariantCultureIgnoreCase) == 0)
                result = result.Substring(7);


            result = result.Replace(NewLine.HtmlBreak + "</p>", "</p>");

            return result;

        }

        private static string CodeTextSpaces(string code)
        {
#if PLAINTEXTCONVERTER_ENABLE_BUGFIX_FOR_FORUM
            return code.Replace("  ", "    ");
#else
            return code;
#endif
        }


        private static Regex HtmlTextLeadingSpaceRegex = new Regex(@"^ ", RegexOptions.CultureInvariant | RegexOptions.Compiled | RegexOptions.Multiline | RegexOptions.IgnoreCase);
        private static string HtmlTextSpaces(string text)
        {
            while (text.Contains("  "))
            {
                text = text.Replace("  ", "&nbsp; ");
            }
            text = text.Replace(NewLine.HtmlBreak + " ", NewLine.HtmlBreak + "&nbsp;");

            return HtmlTextLeadingSpaceRegex.Replace(text, delegate(Match m)
            {
                return "&nbsp;";
            });

        }

        private static string CodeTagsToHtmlCode(string text, Boolean addHmtlLineBreakToCodeLineBreak, bool useCodeColorizer)
        {
            
            //
            string result = text.Replace("<blockquote>", NewLine.Unix + "<blockquote>" + NewLine.Unix)
                                .Replace("</blockquote>", NewLine.Unix + "</blockquote>" + NewLine.Unix);

            // undefined lang: ([code] or [code=xxx])
            result = CodeTagToHtmlCode(result, string.Empty, addHmtlLineBreakToCodeLineBreak, useCodeColorizer);

            // special code tags:
            result = CodeTagToHtmlCode(result, CodeTag.cpp, addHmtlLineBreakToCodeLineBreak, useCodeColorizer);
            result = CodeTagToHtmlCode(result, CodeTag.csharp, addHmtlLineBreakToCodeLineBreak, useCodeColorizer);
            result = CodeTagToHtmlCode(result, CodeTag.vbnet, addHmtlLineBreakToCodeLineBreak, useCodeColorizer);
            result = CodeTagToHtmlCode(result, CodeTag.sql, addHmtlLineBreakToCodeLineBreak, useCodeColorizer);
            result = CodeTagToHtmlCode(result, CodeTag.html, addHmtlLineBreakToCodeLineBreak, useCodeColorizer);

            result = BlockquoteStartTagRemoveNewLineRegex.Replace(result, delegate(Match m)
            {
                return "<blockquote>";
            });

            result = BlockquoteEndTagRemoveNewLineRegex.Replace(result, delegate(Match m)
            {
                return "</blockquote>";
            });

            return result.Replace(NewLine.Unix + "<pre", "<pre");

        }

        private static string CodeTagToHtmlCode(string text, string lang, Boolean addHmtlLineBreakToCodeLineBreak, bool useCodeColorizer)
        {
            /* @todo: check [cpp] inside [vbnet] etc. 
             * ... or:
             *   1. convert cpp, vbnet, ... to code=cpp, ...
             *   2. convert [code...] to <pre>
             *   3. => bug: if cpp is inside code it will shown as [code=cpp]
             *   4. => convert [code=cpp] to [cpp]
             */

            string langName = lang;
            string codeTagStartRegexString;
            string codeTagEndRegexString;
            Boolean checkLang = false;

            string htmlStartTag = "<pre>";
            const string htmlEndTag = "</pre>";

            if (lang.Length > 0)
            {
                codeTagStartRegexString = string.Format(@"^\r?\n?\s*\[{0}\]\r?\n?", lang);
                codeTagEndRegexString = string.Format(@"\r?\n?\[/{0}\]\s*$", lang);
            }
            else
            {
                checkLang = true;
                codeTagStartRegexString = @"^\s*\[code=?(x-)?([^\]]*)?\]\r?\n?";
                codeTagEndRegexString = @"\r?\n?\[/code\]";
            }

            string result = text.Replace(NewLine.HtmlBreak, NewLine.Unix + NewLine.HtmlBreak + NewLine.Unix);

            Regex codeTagStartRegex = new Regex(codeTagStartRegexString, RegexOptions.CultureInvariant | RegexOptions.Compiled | RegexOptions.Multiline | RegexOptions.IgnoreCase);
            Regex codeTagEndRegex = new Regex(codeTagEndRegexString, RegexOptions.CultureInvariant | RegexOptions.Compiled | RegexOptions.Multiline | RegexOptions.IgnoreCase);

            // 1. codeTagStartRegex
            Match matchStart = codeTagStartRegex.Match(result);
            while (matchStart.Success)
            {

                Match matchEnd = codeTagEndRegex.Match(result, matchStart.Index + matchStart.Length);
                if (!matchEnd.Success)
                {
                    return result;
                } // no match => end CodeTagToHtmlCode

                Match matchTestStart = codeTagStartRegex.Match(result, matchStart.Index + matchStart.Length, matchEnd.Index - matchStart.Index - matchStart.Length);
                Boolean runStartTest = matchTestStart.Success;
                while (runStartTest)
                {
                    Match matchTestEnd = codeTagEndRegex.Match(result, matchEnd.Index + matchEnd.Length);
                    if (matchTestEnd.Success)
                    {
                        matchEnd = matchTestEnd;
                        matchTestStart = codeTagStartRegex.Match(result, matchTestStart.Index + matchTestStart.Length, matchEnd.Index - matchTestStart.Index - matchTestStart.Length);
                        runStartTest = matchTestStart.Success;
                    }
                    else
                        runStartTest = false;
                }

                if (checkLang)
                    langName = matchStart.Groups[2].Value;

                if (langName.Length > 0)
                    htmlStartTag = string.Format("<pre lang=\"x-{0}\">", langName);
                else
                    htmlStartTag = "<pre>";


                string replaceString = result.Substring(matchStart.Index + matchStart.Length, matchEnd.Index - matchStart.Index - matchStart.Length)
                                    .Replace(NewLine.Unix + NewLine.HtmlBreak + NewLine.Unix, NewLine.Unix);

                if (useCodeColorizer)
                {
                    // Add Code Colorization:
                    ColorCode.ILanguage ccLang = null;
                    switch (langName.ToLower(System.Globalization.CultureInfo.InvariantCulture))
                    {
                        case CodeTag.cpp:
                            ccLang = ColorCode.Languages.Cpp;
                            break;
                        case CodeTag.csharp:
                            ccLang = ColorCode.Languages.CSharp;
                            break;
                        case CodeTag.vbnet:
                            ccLang = ColorCode.Languages.VbDotNet;
                            break;
                        case CodeTag.sql:
                            ccLang = ColorCode.Languages.Sql;
                            break;
                        case CodeTag.html:
                            ccLang = ColorCode.Languages.Html;
                            break;
                        default: // [code=xxx]
                            if (langName.Length > 0)
                                try
                                {
                                    ccLang = ColorCode.Languages.FindById(langName);
                                }
                                catch (Exception exp)
                                {
                                    Traces.ConvertersTraceEvent(TraceEventType.Error, 2,
                                                                NNTPServer.Traces.ExceptionToString(exp));
                                }
                            break;
                    }

                    if ((ccLang != null) && (_colorizer != null))
                    {
                        try
                        {
                            replaceString = _colorizer.Colorize(System.Web.HttpUtility.HtmlDecode(replaceString), ccLang);

                            // remove text outside <pre>-tag:
                            int subStringStart = replaceString.IndexOf("<pre>", StringComparison.InvariantCultureIgnoreCase);
                            int subStringEnd = replaceString.LastIndexOf("</pre>", StringComparison.InvariantCultureIgnoreCase);
                            if (subStringStart>=0 && subStringEnd > subStringStart)
                                replaceString = replaceString.Substring(subStringStart + "<pre>".Length, subStringEnd - subStringStart);

                        }
                        catch (Exception exp)
                        {
                            Traces.ConvertersTraceEvent(TraceEventType.Error, 1,
                                                        NNTPServer.Traces.ExceptionToString(exp));
                        }
                    }
                }

                if (addHmtlLineBreakToCodeLineBreak) replaceString = replaceString.Replace(NewLine.Unix, NewLine.HtmlBreak);

                result = result.Substring(0, matchStart.Index)
                         + htmlStartTag
                         + replaceString
                         + htmlEndTag
                         + result.Substring(matchEnd.Index + matchEnd.Length);

                matchStart = codeTagStartRegex.Match(result, matchEnd.Index + htmlStartTag.Length - matchStart.Length + htmlEndTag.Length);
            }

            return result.Replace(NewLine.Unix + NewLine.HtmlBreak + NewLine.Unix, NewLine.HtmlBreak);

        }

        private string AddStylesToHtmlTags(string htmlText)
        {

            HtmlDocument doc = new HtmlDocument();

            doc.LoadHtml(htmlText);
            doc.OptionWriteEmptyNodes = true;
            doc.OptionAutoCloseOnEnd = false;

            HtmlNode docNode = doc.DocumentNode;
 
            AddStylesToHtmlNodeTags(docNode, "pre", CodeBoxHtmlStyle, 0, 0);
            AddStylesToHtmlNodeTags(docNode, "blockquote", Blockquote2ndLevelBoxHtmlStyle, 1, 0);

            string result = docNode.OuterHtml.Replace("<p />", "<p></p>");
            
            result = result.Replace("<p></p><blockquote", "<blockquote")
                           .Replace("<p></p></blockquote>", "</blockquote>")
                           .Replace("</blockquote><p></p>", "</blockquote>");

            return result;
        }

        private static void AddStylesToHtmlNodeTags(HtmlNode node, string tagName, string tagStyle, int startNodeLevel, int nodeLevel)
        {

            foreach (HtmlNode childNode in node.ChildNodes)
            {

                int tempLevel = nodeLevel;

                if (childNode.Name.Equals(tagName, StringComparison.InvariantCultureIgnoreCase))
                {
                    if (tempLevel >= startNodeLevel)
                    {
                        var htmlAttribute = childNode.Attributes["style"];
                        if (htmlAttribute == null)
                            childNode.Attributes.Append("style", tagStyle);
                        else
                            htmlAttribute.Value = tagStyle;
                    }
                    tempLevel += 1;
                }

                if (childNode.HasChildNodes)
                {
                    AddStylesToHtmlNodeTags(childNode, tagName, tagStyle, startNodeLevel, tempLevel);
                }
            }
        }

        private static string OptimizeHtmlLayout(string htmlText)
        {

            string result = htmlText;

            // <blockquote>code:<p></p><pre ..> => <blockquote>code:<pre ..>
            Regex optimizeHtmlRegex = new Regex(@"<blockquote>(.*)<p></p><pre([^>]*)>", RegexOptions.CultureInvariant | RegexOptions.Compiled | RegexOptions.Multiline | RegexOptions.IgnoreCase);
            result = optimizeHtmlRegex.Replace(result, delegate(Match m)
            {
                return string.Format("<blockquote>{0}<pre{1}>", m.Groups[1], m.Groups[2]);
            });

            optimizeHtmlRegex = new Regex(@"((<p></p>)+)</pre>", RegexOptions.CultureInvariant | RegexOptions.Compiled | RegexOptions.Multiline | RegexOptions.IgnoreCase);
            result = optimizeHtmlRegex.Replace(result, delegate(Match m)
            {
                return "</pre>";
            });

            optimizeHtmlRegex = new Regex(@"</pre><p></p>", RegexOptions.CultureInvariant | RegexOptions.Compiled | RegexOptions.Multiline | RegexOptions.IgnoreCase);
            result = optimizeHtmlRegex.Replace(result, delegate(Match m)
            {
                return "</pre>";
            });

            optimizeHtmlRegex = new Regex(@"<p></p>(.*)\r?\n<blockquote>", RegexOptions.CultureInvariant | RegexOptions.Compiled | RegexOptions.Multiline | RegexOptions.IgnoreCase);
            // @todo add key words
            result = optimizeHtmlRegex.Replace(result, delegate(Match m)
            {
                if (m.Groups[1].Value.IndexOf("</p>", StringComparison.InvariantCultureIgnoreCase) >= 0)
                    return m.Groups[0].Value;
                else
                    return "<p>" + m.Groups[1].Value + "</p><blockquote>";
            });


            // <br /><li>
            optimizeHtmlRegex = new Regex(NewLine.HtmlBreak +  @"<(ul|/ul|ol|/ol|li)>", RegexOptions.CultureInvariant | RegexOptions.Compiled | RegexOptions.Multiline | RegexOptions.IgnoreCase);
            result = optimizeHtmlRegex.Replace(result, delegate(Match m)
            {
                return "<" + m.Groups[1].Value + ">";
            });

#if PLAINTEXTCONVERTER_ENABLE_BUGFIX_FOR_FORUM

            result = result.Replace(" " + UnterlineHtmlStartTag, "  " + UnterlineHtmlStartTag);

#endif

            return result;

        }

        #endregion // PlainTextBodyToHtml

    }  // class Converter
}
