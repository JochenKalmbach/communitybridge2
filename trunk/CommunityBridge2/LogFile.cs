using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;

namespace CommunityBridge2
{
  internal class LogFile : TraceListener
  {
    public LogFile(string basePath)
    {
      _basePath = basePath;
      LogFileName = Path.Combine(_basePath, "LogFile.txt");
      LogFileNameOld = LogFileName + ".old";

      var t = new Thread(WriterThread);
      t.IsBackground = true;
      t.Priority = ThreadPriority.BelowNormal;
      t.Start();

      NNTPServer.Traces.NntpServer.Listeners.Add(this);
      NNTPServer.Traces.NntpServer.Switch = new SourceSwitch("NNTPServer", "Verbose");

      Traces.Main.Listeners.Add(this);
      Traces.Main.Switch = new SourceSwitch("Main", "Verbose");

      Traces.WebService.Listeners.Add(this);
      Traces.WebService.Switch = new SourceSwitch("WebService", "Verbose");

      //ArticleConverter.Traces.Converters.Listeners.Add(this);
      //ArticleConverter.Traces.Converters.Switch = new SourceSwitch("Converters", "Verbose");
    }

    #region LogWriter

    private readonly Queue<string> _queue = new Queue<string>();
    private readonly AutoResetEvent _event = new AutoResetEvent(false);
    private const long MaxFileSize = 5*1024*1024;  // 5 MB
    private readonly string _basePath;
    private object _WriterLock = new object();

    private string LogFileName;
    private string LogFileNameOld;


    private StreamWriter OpenLogFile(bool tooBig)
    {
      if (tooBig)
      {
        if (File.Exists(LogFileNameOld))
        {
          try
          {
            // be sure it contains no read-only
            File.SetAttributes(LogFileNameOld, FileAttributes.Normal);
          }
          catch { }
          // delete it
          File.Delete(LogFileNameOld);
        }

        if (File.Exists(LogFileName))
        {
          File.Move(LogFileName, LogFileNameOld);
        }
      }

      return new StreamWriter(LogFileName, true);
    }
    void WriterThread()
    {
      StreamWriter sw = null;

      bool tooBig = false;
      while(true)
      {
        _event.WaitOne();

        try
        {
          if (sw == null)
          {
            lock (_WriterLock)
            {
              sw = OpenLogFile(tooBig);
              tooBig = false;
            }
          }

          bool msgWritten = false;
          string msg;
          lock (_WriterLock)
          {
            do
            {
              msg = null;
              lock (this)
              {
                if (_queue.Count > 0)
                  msg = _queue.Dequeue();
              }
              if (msg != null)
              {
                if ((msg.Length > 200) && (msg.IndexOf("Exception", StringComparison.Ordinal) < 0))
                  msg = msg.Substring(0, 200);

                sw.WriteLine(msg);
                msgWritten = true;
              }
            } while (msg != null);
            if (msgWritten)
            {
              sw.Flush();
              if (sw.BaseStream.Position > MaxFileSize)
              {
                sw.Close();
                sw = null;
                tooBig = true;
              }
            }
          }  // WriterLock
        }
        catch (Exception)
        {
          // TODO:
          return; // Terminate
        }
      }  // while(true)
    }  // WriterThread

    void AddMessage(string text)
    {
      if (text == null)
        return;
      lock(this)
      {
        _queue.Enqueue(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss,ffff") + " " + text);
        _event.Set();
      }
    }
    #endregion

    public override void Write(string message)
    {
    }

    public override void WriteLine(string message)
    {
      AddMessage(message);
    }

    /// <summary>
    /// Returns the (temporary) name of the ZIP-File
    /// </summary>
    /// <returns></returns>
    public string CreateZipFile()
    {
      int flushWaitCount = 0;
      bool countIsZero = false;
      do
      {
        lock (this)
        {
          if (_queue.Count > 0)
            Thread.Sleep(100);
          else
            countIsZero = true;
          flushWaitCount++;
        }
      } while ((countIsZero == false) && (flushWaitCount < 5));

      lock(_WriterLock)
      {
        string fn = DateTime.Now.ToString("yyyyMMdd_HHmmssffff");
        fn = Path.Combine(Path.GetDirectoryName(LogFileName), fn + ".zip");
        if (File.Exists(fn))
          File.Delete(fn);
        using (var zip = new Ionic.Zip.ZipFile())
        {
          if (File.Exists(LogFileName))
            zip.AddFile(LogFileName);
          if  (File.Exists(LogFileNameOld))
            zip.AddFile(LogFileNameOld);
          zip.Save(fn);
          return fn;
        }
      }
    }
  }
}
