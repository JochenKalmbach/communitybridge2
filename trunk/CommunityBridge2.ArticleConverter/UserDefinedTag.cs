using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Xml.Serialization;

namespace CommunityBridge2.ArticleConverter
{
    public class UserDefinedTag
    {
        [Category("Tags")]
        [XmlAttribute("Tag")]
        public string TagName { get; set; }

        [Category("Tags")]
        [XmlAttribute("html")]
        public string HtmlText { get; set; }

        public override string ToString()
        {
            if (string.IsNullOrEmpty(TagName) == false)
                return "[" + TagName + "]{TEXT}[/" + TagName + "]";
            return base.ToString();
        }
    }
    public class UserDefinedTagCollection : List<UserDefinedTag>
    {
        public static string PreCompileXmlSerializer()
        {
            var ser = new XmlSerializer(typeof(UserDefinedTagCollection));
            var sw = new StringWriter();
            ser.Serialize(sw, new UserDefinedTagCollection());
            return sw.ToString();
        }
        public string GetString()
        {
            try
            {
                var ser = new XmlSerializer(typeof(UserDefinedTagCollection));
                var sw = new StringWriter();
                ser.Serialize(sw, this);
                // Remove linbreaks, so this string will be stored as a single line in the registry...
                return sw.ToString().Replace("\n", string.Empty).Replace("\r", string.Empty);
            }
            catch (Exception exp)
            {
                Traces.ConvertersTraceEvent(TraceEventType.Critical, 1, "Error while serializing UserDefinedTagCollection: {0}", NNTPServer.Traces.ExceptionToString(exp));
            }
            return string.Empty;
        }

        static public UserDefinedTagCollection FromString(string text)
        {
            try
            {
                var ser = new XmlSerializer(typeof(UserDefinedTagCollection));
                var sr = new StringReader(text);
                var res = ser.Deserialize(sr) as UserDefinedTagCollection;
                return res;
            }
            catch (Exception exp)
            {
                Traces.ConvertersTraceEvent(TraceEventType.Critical, 1, "Error while deserializing UserDefinedTagCollection: {0}\r\n{1}", text, NNTPServer.Traces.ExceptionToString(exp));
            }
            return null;
        }

    }
}
