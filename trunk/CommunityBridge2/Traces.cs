using System.Diagnostics;

namespace CommunityBridge2
{
    public static class Traces
    {
        public readonly static TraceSource Main = new TraceSource("Main");

        public static void Main_TraceEvent(TraceEventType eventType, int id, string message)
        {
            Main.TraceEvent(eventType, id, message);
            Main.Flush();
        }
        public static void Main_TraceEvent(TraceEventType eventType, int id, string format, params object[] args)
        {
            Main.TraceEvent(eventType, id, format, args);
            Main.Flush();
        }

        public readonly static TraceSource WebService = new TraceSource("WebService");

        public static void WebService_TraceEvent(TraceEventType eventType, int id, string message)
        {
            WebService.TraceEvent(eventType, id, message);
            WebService.Flush();
        }
        public static void WebService_TraceEvent(TraceEventType eventType, int id, string format, params object[] args)
        {
            WebService.TraceEvent(eventType, id, format, args);
            WebService.Flush();
        }
    }
}
