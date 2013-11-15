using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace QinetiQ.Enterprise.SharePoint.Nntp
{
    public class Logger
    {
        private string _logPath = @"C:\Program Files\QinetiQ\Logs\QinetiQ.Enterprise.SharePoint.Nntp.log";

        public Logger()
        {
        }

        public Logger(string logPath)
        {
            _logPath = logPath;
        }

        public void Error(string message, Exception ex)
        {
            try
            {
                //ILog log = LogManager.GetLogger("Default");
                //log.Error(message, ex);
                ErrorInternal(string.Empty, ex);
            }
            catch
            {
            }
        }

        public void Debug(string message)
        {
            try
            {
                ErrorLog(message);
            }
            catch
            {
            }
        }

        private void ErrorInternal(string message, Exception ex)
        {
            ErrorLog((message.Length > 0 ? message + " :: " : string.Empty) + ex.Message + " :: " + ex.StackTrace + " :: " + (ex.InnerException != null ? " :: " + ex.InnerException.Message : String.Empty));
        }

        private void ErrorLog(string message)
        {
            string logFormat = DateTime.Now.ToShortDateString().ToString() + " " + DateTime.Now.ToLongTimeString().ToString() + " ==> ";

            using (StreamWriter sw = new StreamWriter(_logPath, true))
            {
                sw.WriteLine(logFormat + message);
                sw.Flush();
                sw.Close();
            }
        }

        public bool IsErrorEnabled
        {
            get
            {
                return true;
            }
        }
    }
}
