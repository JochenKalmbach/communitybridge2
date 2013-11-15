using System;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Threading;

namespace CommunityBridge2
{
  /// <summary>
  /// Interaction logic for App.xaml
  /// </summary>
    public partial class App
  {
        Mutex _singleInstanceMutex;
        protected override void OnStartup(StartupEventArgs e)
        {
            bool ok;
            _singleInstanceMutex = new Mutex(true, UserSettings.ProductName, out ok);

            if (!ok)
            {
                // Programm is already running, shutdown the current...
                Current.Shutdown();
                return;
            }

            // Initialize logging and exception handling...
            AppDomain.CurrentDomain.UnhandledException += CurrentDomainUnhandledException;

          _logFile = new LogFile(UserSettings.Default.BasePath);

            base.OnStartup(e);
        }

    private LogFile _logFile;

    static void CurrentDomainUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
      string expMsg = NNTPServer.Traces.ExceptionToString(e.ExceptionObject as Exception);
      if (string.IsNullOrEmpty(expMsg))
        expMsg = "<Unknown Exception>";
      Traces.Main_TraceEvent(System.Diagnostics.TraceEventType.Critical, 1, "UnhandledException: {0}",
                             expMsg);

      var exp = e.ExceptionObject as Exception;
      string msg = "<Unknown>";
      if (exp != null)
        msg = exp.Message;

      var dlg = new SendDebugDataWindow();
      dlg.UnhandledExceptionMessage = msg;
      if (dlg.ShowDialog() == true)
      {
        try
        {
          var app = (App) Current;
          app.SendLogs(expMsg, dlg.UsersEMail, dlg.UsersDescription, dlg.UserSendEmail);
        }
        catch { }
        Environment.Exit(-1);
      }
    }
    internal void SendLogs(string unhandledException, string userEmail, string userComment, bool userSendEmail)
    {
      string zipFile = null;
      try
      {
        zipFile = _logFile.CreateZipFile();
      }
      catch(Exception exp)
      {
        Traces.Main_TraceEvent(TraceEventType.Error, 1, NNTPServer.Traces.ExceptionToString(exp));
      }

      if (zipFile == null)
      {
        if (string.IsNullOrEmpty(unhandledException) == false)
          MessageBox.Show("Failed to ZIP the debug data! Please restart the bridge!", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
        else
          MessageBox.Show("Failed to ZIP the debug data!", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
        return;
      }
      try
      {
        string err = UploadDiagnoseData.DoIt(zipFile, unhandledException, userEmail, userComment, userSendEmail);
        if (err == null)
        {
          if (string.IsNullOrEmpty(unhandledException) == false)
            MessageBox.Show("Debug data send... Thank your for helping to improve the bridge or find problems!\r\n\r\nPlease restart the bridge!");
          else
            MessageBox.Show("Debug data send... Thank your for helping to improve the bridge or find problems!");
        }
        else
        {
          if (string.IsNullOrEmpty(unhandledException) == false)
            MessageBox.Show("Failed to send debug data:\r\n" + err + "\r\n\r\nPlease restart the bridge!", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
          else
            MessageBox.Show("Failed to send debug data:\r\n" + err, "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
      }
      catch(Exception exp)
      {
        Traces.Main_TraceEvent(TraceEventType.Error, 1, NNTPServer.Traces.ExceptionToString(exp));
      }
      try
      {
        File.Delete(zipFile);
      }
      catch(Exception exp)
      {
        Traces.Main_TraceEvent(TraceEventType.Error, 1, NNTPServer.Traces.ExceptionToString(exp));
      }
    }

    protected override void OnExit(ExitEventArgs e)
        {
            base.OnExit(e);
            if (_singleInstanceMutex != null)
                GC.KeepAlive(_singleInstanceMutex);
        }
    }
}
