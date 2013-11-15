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
using System.Text;
using System.Text.RegularExpressions;
using System.Collections;
using System.Diagnostics;

namespace CommunityBridge2.NNTPServer
{
	/// <summary>
	/// Summary description for Mime.
	/// </summary>
	public class Mime
    {
        #region Deconde-Mime-String
        public static string DecodeEncodedWordValue(string mimeString)
        {
            var regex = new Regex(@"=\?(?<charset>.*?)\?(?<encoding>[qQbB])\?(?<value>.*?)\?=");
            var encodedString = mimeString;
            var decodedString = string.Empty;

            while (encodedString.Length > 0)
            {
                var match = regex.Match(encodedString);
                if (match.Success)
                {
                    // If the match isn't at the start of the string, copy the initial few chars to the output
                    string plaintext = encodedString.Substring(0, match.Index);
                    // Wenn ein Leerzeichen zwischen zwei kodierten Strings ist, so wird dieses Leerzeichen nicht mitverwnedet, oder?
                    if (string.Equals(plaintext, " ", StringComparison.Ordinal) == false)
                      decodedString += plaintext;

                    var charset = match.Groups["charset"].Value;
                    var encoding = match.Groups["encoding"].Value.ToUpper();
                    var value = match.Groups["value"].Value;

                    Encoding encoder;
                    try
                    {
                        encoder = Encoding.GetEncoding(charset);
                    }
                    catch
                    {
                        encoder = Encoding.Default;  // Fallback
                    }

                    if (encoding.Equals("B"))
                    {
                        // Encoded value is Base-64
                        var bytes = Convert.FromBase64String(value);
                        decodedString += encoder.GetString(bytes);
                    }
                    else if (encoding.Equals("Q"))
                    {
                        // Encoded value is Quoted-Printable
                        // Parse looking for =XX where XX is hexadecimal
                        var regx = new Regex("(\\=([0-9A-F][0-9A-F]))", RegexOptions.IgnoreCase);
                        decodedString += regx.Replace(value, new MatchEvaluator(delegate(Match m)
                        {
                            var hex = m.Groups[2].Value;
                            var iHex = Convert.ToInt32(hex, 16);

                            // Return the string in the charset defined
                            var bytes = new byte[1];
                            bytes[0] = Convert.ToByte(iHex);
                            return encoder.GetString(bytes);
                        }));
                        decodedString = decodedString.Replace('_', ' ');
                    }
                    else
                    {
                        // Encoded value not known, return original string
                        // (Match should not be successful in this case, so this code may never get hit)
                        decodedString += encodedString;
                        break;
                    }

                    // Trim off up to and including the match, then we'll loop and try matching again.
                    encodedString = encodedString.Substring(match.Index + match.Length);
                }
                else
                {
                    // No match, not encoded, return original string
                    decodedString += encodedString;
                    break;
                }
            }
            return decodedString;
        }

        #endregion

        private string _text = String.Empty;

		public MimePart[] MimeParts
		{
			get
			{
				return GetMimeParts();
			}
		}

		public string Text
		{
			set
			{
				_text = value;
			}
			get
			{
				return _text;
			}
		}

        public string Boundary { get; set; }

		private MimePart[] GetMimeParts()
		{
			var mimeParts = new ArrayList();
			MimePart mimePart = null;
			char[] separator = {'\n'};
			string line;
			var lines = _text.Split(separator);
			var i = 0;
			var start = false;
			var startMimeHeader = false;
			var startMimeBody = false;
			var mimePartBody = new StringBuilder();
			var mimePartCount = 0;
		    string boundary = "------=_NextPart_";
            if (string.IsNullOrEmpty(Boundary) == false)
                boundary = Boundary.Trim('"', ' ');

			while(i < lines.Length)
			{
				// get the next line

				line = lines[i].TrimEnd().Replace("\0", string.Empty);

				// check for header

				if (line == "This is a multi-part message in MIME format.")
				{
					if (start)
					{
						throw new Exception("Multiple MIME format headers detected");
					}
					start = true;
					goto NextLine;
				}

				// new mime part detected

                if (line.IndexOf(boundary, StringComparison.InvariantCultureIgnoreCase) >= 0)
				{
					if (mimePartCount > 0) // not the first part
					{
						// save the body of the last mime part

                        if (mimePart == null) throw new ApplicationException("Should never happen...");
						mimePart.Body = mimePartBody.ToString();

						// add the part to the collection

						mimeParts.Add(mimePart);

						// clear out for next part

						mimePartBody = new StringBuilder();
					}

					// create a new mime part & increment the counter
					mimePart = new MimePart();
					mimePartCount++;
					startMimeHeader = true;
					startMimeBody = false;

					goto NextLine;
				}

				// check for end of header

                if ((startMimeHeader) && (line.Trim().Length == 0 || line == "\0" ))
				{
					startMimeHeader = false;
					startMimeBody = true;

					goto NextLine;
				}

				// copy header property

				if (startMimeHeader)
				{
				    int sepPos = line.IndexOf(":");
                    if (sepPos > 0)
                    {
                        var propertyName = line.Substring(0, sepPos);
                        var propertyValue = line.Substring(sepPos+1);
                        mimePart.AddProperty(propertyName.Trim(), propertyValue.Trim());
                    }
                    else
                    {
                        // multi line header:
                        var propertyValue = line;
                        if (mimePart.Properties.Length > 0)
                        {
                            mimePart.AppendToLastProperty(propertyValue.Trim());
                        }
                    }
				    //if (line.StartsWith("\t"))
                    //{
                    //    propertyName = line.Substring(line.IndexOf("\t") + 1, line.IndexOf("=") - 1);
                    //    propertyValue = line.Substring(line.IndexOf("\"") + 1, line.Length - line.IndexOf("\"") - 2);

                    //    mimePart.AddProperty(propertyName.Trim(), propertyValue.Trim());
                    //}
                    //else
                    //{
                    //    propertyName = line.Substring(0, line.IndexOf(":"));
                    //    propertyValue = line.Substring(line.IndexOf(":") + 1, line.Length - line.IndexOf(":") - 1);

                    //    if (propertyValue.Substring(propertyValue.Length - 1, 1) == ";")
                    //    {
                    //        propertyValue = propertyValue.Substring(0, propertyValue.Length - 1);
                    //    }

                    //    mimePart.AddProperty(propertyName.Trim(), propertyValue.Trim());
                    //}
				}

				if (startMimeBody)
				{
					mimePartBody.Append(line + "\n");
				}

				NextLine:

					i++;
			}

			// commit the last part

			if (mimePart != null)
			{
				mimeParts.Add(mimePart);
			}

			return (MimePart[])mimeParts.ToArray(typeof(MimePart));
		}
	}

	public struct MimePartProperty
	{
	    public string PropertyName { get; set; }

	    public string PropertyValue { get; set; }
	}

	/// <summary>
	/// Summary description for MimePart.
	/// </summary>
	public class MimePart
	{
		private string _body = String.Empty;
		private readonly List<MimePartProperty> _properties = new List<MimePartProperty>();

		public MimePart()
		{
		}

		public MimePart(string body)
		{
			_body = body;
		}
		
		public void AddProperty(string propertyName, string propertyValue)
		{
			var mimePartProperty = 
                new MimePartProperty
                {
                    PropertyName = propertyName, 
                    PropertyValue = propertyValue
                };

		    _properties.Add(mimePartProperty);
		}
        public void AppendToLastProperty(string propertyValue)
        {
            if (_properties.Count <= 0)
                return;
            MimePartProperty p = _properties[_properties.Count - 1];
            p.PropertyValue = p.PropertyValue + propertyValue;
            _properties[_properties.Count - 1] = p;
        }

	    public MimePartProperty[] Properties
		{
			get
			{
				return _properties.ToArray();
			}
		}

		public string Body
		{
			set
			{
				_body = value;
			}
			get
			{
				return _body;
			}
		}

		public object Decode()
		{
			string contentTransferEncoding = GetPropertyValue("Content-Transfer-Encoding");
			bool isQuotedPrintable = false;

			if (contentTransferEncoding == "quoted-printable")
			{
				isQuotedPrintable = true;
			}

		    string ct = GetPropertyValue("Content-Type");

			if (ct.IndexOf("text/html", StringComparison.InvariantCultureIgnoreCase) >= 0)
					return DecodeHtml(isQuotedPrintable);

			if (ct.IndexOf("text/plain", StringComparison.InvariantCultureIgnoreCase) >= 0)
					return DecodePlainText(isQuotedPrintable);

			if (ct.IndexOf("image/jpeg", StringComparison.InvariantCultureIgnoreCase) >= 0)
					return DecodeImage(contentTransferEncoding);


            return _body;
		}

		private byte[] DecodeImage(string contentTransferEncoding)
		{
		    if (contentTransferEncoding == "base64")
			{
				return Convert.FromBase64String(_body);
			}
		    return null;
		}

	    public string GetPropertyValue(string propertyName)
		{
			foreach (var mimePartProperty in Properties)
			{
                if (string.Compare(mimePartProperty.PropertyName, propertyName, StringComparison.InvariantCultureIgnoreCase) == 0)
				{
					return mimePartProperty.PropertyValue;
				}
			}

			return String.Empty;
		}

		private string DecodePlainText(bool isQuotedPrintable)
		{
			return DecodeQuotedPrintable(isQuotedPrintable);
		}

		private string DecodeHtml(bool isQuotedPrintable)
		{
		    string html = DecodeQuotedPrintable(isQuotedPrintable);
            // WHY should we replace some text here????? 
            // Disable this... because we already have HTML, so we MUST NOT decode the encoded html!!!
			//html = html.Replace("&lt;", "<");
			//html = html.Replace("&gt;", ">");
			//html = html.Replace("&apos;", "'");
			//html = html.Replace("&quot;", "\"");
			//html = html.Replace("&amp;", "&");

			return html;
		}

		static string ReplaceOctet(Match match) 
		{
			var decoded = (char)Convert.ToInt32(match.ToString().Replace("=", String.Empty), 16);

			return decoded.ToString();
		}

		private string DecodeQuotedPrintable(bool isQuotedPrintable)
		{
		    // refer to RFC 1521.5.1 (http://www.freesoft.org/CIE/RFC/1521/6.htm)

			if (isQuotedPrintable)
			{

                var returnValue = qpRegEx.Replace(_body, new MatchEvaluator(ReplaceOctet));
				var regEx = new Regex("=\n");
				return regEx.Replace(returnValue, string.Empty);
			}
		    return _body;
		}

        private static readonly Regex qpRegEx = new Regex("=[0-9A-F]{2}", RegexOptions.CultureInvariant | RegexOptions.Compiled | RegexOptions.Multiline | RegexOptions.IgnoreCase);
        public static string DecodeQuotedPrintable(string text)
        {
            if (string.IsNullOrEmpty(text))
                return text;
            try
            {
                var returnValue = qpRegEx.Replace(text, new MatchEvaluator(ReplaceOctet));
                return returnValue.Replace("=\n", " ");
            }
            catch (Exception exp)
            {
                Traces.NntpServerTraceEvent(TraceEventType.Error, "Error converting QP text: {0}\r\n\r\n{1}", text, Traces.ExceptionToString(exp));
            }
            return text;
        }

        public static string DecodeBase64(string text, string charSet)
        {
            if (string.IsNullOrEmpty(text))
                return text;
            try
            {
                var enc = Encoding.GetEncoding(charSet);
                byte[] data = Convert.FromBase64String(text);
                return enc.GetString(data);
            }
            catch (Exception exp)
            {
                Traces.NntpServerTraceEvent(TraceEventType.Error, "Error converting base64 text: {0}\r\n\r\n{1}", text, Traces.ExceptionToString(exp));
            }
            return text;
        }

	    /// <summary>
	    /// Encodes a not QP-Encoded string.
	    /// </summary>
	    /// <param name="text">The string which should be encoded.</param>
	    /// <param name="enc"></param>
	    /// <returns>The encoded string</returns>
	    public static string EncodeQuotedPrintable(string text, Encoding enc)
        {
            // See: http://dotnet-snippets.de/dns/c-quoted-printable-encoder-SID778.aspx

	        const string equalsSign = "=";
            const string replaceEqualSign = "=";

            //Alle nicht im Ascii-Zeichnsatz enthaltenen Zeichen werden ersetzt durch die hexadezimale 
            //Darstellung mit einem vorangestellten =
            //Bsp.: aus "ü" wird "=FC"
            //Bsp. mit Ersetzungszeichen "%"für das "=": aus "ü" wird "%FC"

            var sb1 = new StringBuilder();
            for (var i = 0; i < 127; i++)
            {
                sb1.Append(Convert.ToChar(i));
            }
            string ascii7BitSigns = sb1.ToString();

            var sb = new StringBuilder();
            foreach (char s in text)
            {
                if (ascii7BitSigns.LastIndexOf(s) > -1)
                    sb.Append(s);
                else
                {
                    byte[] bytes = enc.GetBytes(s.ToString());
                    foreach (var b in bytes)
                    {
                        var qp = string.Format("{0}{1}",
                            equalsSign,
                            Convert.ToString(b, 16)).Replace(equalsSign, replaceEqualSign);
                        sb.Append(qp);
                    }
                }
            }
            return sb.ToString();
        }
	}  // class MimePart
}
