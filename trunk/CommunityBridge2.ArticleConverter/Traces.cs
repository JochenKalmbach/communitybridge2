using System;
using System.Text;
using System.Diagnostics;

namespace CommunityBridge2.ArticleConverter
{
    public static class Traces
    {
        public static readonly TraceSource Converters = new TraceSource("Converters");

        public static void ConvertersTraceEvent(TraceEventType eventType, int id, string message)
        {
            Converters.TraceEvent(eventType, id, message);
            Converters.Flush();
        }
        public static void ConvertersTraceEvent(TraceEventType eventType, int id, string format, params object[] args)
        {
            Converters.TraceEvent(eventType, id, format, args);
            Converters.Flush();
        }

        public static string ExceptionToString(Exception exp)
        {
            return NNTPServer.Traces.ExceptionToString(exp);
        }
    }
}
