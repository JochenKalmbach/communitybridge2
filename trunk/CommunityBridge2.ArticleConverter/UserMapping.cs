using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Serialization;

namespace CommunityBridge2.ArticleConverter
{
    public class UserMapping
    {
        public string UserName { get; set; }
        public Guid? Id { get; set; }
        public string UserEmail { get; set; }
        //public string Brand { get; set; }
    }

    public class UserMappingCollection : List<UserMapping>
    {
        public static string PreCompileXmlSerializer()
        {
            var ser = new XmlSerializer(typeof(UserMappingCollection));
            var sw = new StringWriter();
            ser.Serialize(sw, new UserMappingCollection());
            return sw.ToString();
        }
        public string GetString()
        {
            try
            {
                var ser = new XmlSerializer(typeof(UserMappingCollection));
                var sw = new StringWriter();
                ser.Serialize(sw, this);
                // Remove linbreaks, so this string will be stored as a single line in the registry...
                return sw.ToString().Replace("\n", string.Empty).Replace("\r", string.Empty);
            }
            catch (Exception exp)
            {
                Traces.ConvertersTraceEvent(TraceEventType.Critical, 1, "Error while serializing UserMappingCollection: {0}", NNTPServer.Traces.ExceptionToString(exp));
            }
            return string.Empty;
        }

        static public UserMappingCollection FromString(string text)
        {
            try
            {
                var ser = new XmlSerializer(typeof(UserMappingCollection));
                var sr = new StringReader(text);
                var res = ser.Deserialize(sr) as UserMappingCollection;
                return res;
            }
            catch (Exception exp)
            {
                Traces.ConvertersTraceEvent(TraceEventType.Critical, 1, "Error while deserializing UserMappingCollection: {0}\r\n{1}", text, NNTPServer.Traces.ExceptionToString(exp));
            }
            return null;
        }
    }
}
