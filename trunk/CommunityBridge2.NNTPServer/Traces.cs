using System;
using System.Text;
using System.Diagnostics;

namespace CommunityBridge2.NNTPServer
{
    public static class Traces
    {
        public static readonly TraceSource NntpServer = new TraceSource("NNTPServer");

        public static void NntpServerTraceEvent(TraceEventType eventType, string message)
        {
          NntpServer.TraceEvent(eventType, 1, message);
          NntpServer.Flush();
        }
        public static void NntpServerTraceEvent(TraceEventType eventType, string format, params object[] args)
        {
          NntpServer.TraceEvent(eventType, 1, format, args);
          NntpServer.Flush();
        }
        public static void NntpServerTraceEvent(TraceEventType eventType, Client client, string message)
        {
          if (client != null)
          {
              message = string.Format("[Client: {0}] ", client.ClientNumber) + message;
          }
          NntpServer.TraceEvent(eventType, 1, message);
          NntpServer.Flush();
        }
        public static void NntpServerTraceEvent(TraceEventType eventType, Client client, string format, params object[] args)
        {
          if (client != null)
          {
            format = string.Format("[Client: {0}] ", client.ClientNumber) + format;
          }
          NntpServer.TraceEvent(eventType, 1, format, args);
          NntpServer.Flush();
        }

        public static string ExceptionToString(Exception exp)
        {
            if (exp == null) return string.Empty;

          try
          {
            var sb = new StringBuilder();
            while (exp != null)
            {
              sb.Append("Exception:");
              sb.AppendLine();
              var exT = exp.GetType();
              if (exT != null)
              {
                sb.AppendFormat("Type {0}", exT.FullName);
                sb.AppendLine();
              }
              try
              {
                // There is a case in which the "Source" could not be retrived....
                // Type System.NullReferenceException
                // Source: System.Data.SqlServerCe
                // Message: Object reference not set to an instance of an object.
                // Stack-Trace:
                // at System.Data.SqlServerCe.SqlCeException.get_Source()
                // at CommunityBridge2.NNTPServer.Traces.ExceptionToString(Exception exp)
                // at CommunityBridge2.Answers.ForumDataSource.LoadNewsgroupsToStream(Action`1 groupAction) in C:\Daten\Privat\MVP\Answers-CommunityBridge\Dev\CommunityBridge2_Source\CommunityBridge2\ForumDataSourceAnswers.cs:line 158
                sb.AppendFormat("Source: {0}", exp.Source);
              }
              catch
              {
                sb.Append("Source: <not able to retrive 'Source'>!");
              }
              sb.AppendLine();
              sb.AppendFormat("Message: {0}", exp.Message);
              sb.AppendLine();
              var se = exp as System.Net.Sockets.SocketException;
              if (se != null)
              {
                sb.AppendFormat("Socket ErrorCode: {0}", se.ErrorCode);
                sb.AppendLine();
              }
              var fePf = exp as System.ServiceModel.FaultException<Microsoft.Support.Community.Core.ErrorHandling.PlatformFault>;
              if (fePf != null)
              {
                if (fePf.Detail != null)
                {
                  sb.AppendFormat("PlatformFault.Details: {0}", fePf.Detail.Details);
                  sb.AppendLine();
                  sb.AppendFormat("PlatformFault.Type: {0}", fePf.Detail.Type);
                  sb.AppendLine();
                  sb.AppendFormat("PlatformFault.ErrorCode: {0}", fePf.Detail.ErrorCode);
                  sb.AppendLine();
                }
              }

              sb.Append("Stack-Trace:");
              sb.AppendLine();
              sb.Append(exp.StackTrace);
              exp = exp.InnerException;
            }
            return sb.ToString();
          }
          catch
          {
          }
          try
          {
            return exp.ToString();

          }
          catch
          {
          }
          return "<Could not convert exc eption to string!>";

        }

    }
}
