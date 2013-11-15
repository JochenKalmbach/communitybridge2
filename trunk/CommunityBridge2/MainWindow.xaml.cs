using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Interop;
using System.Xml.Serialization;
using CommunityBridge2.LiveConnect;
using CommunityBridge2.LiveConnect.Public;
using CommunityBridge2.NNTPServer;
using CommunityBridge2.WebServiceDataSource;
using Application = System.Windows.Application;
using IWin32Window = System.Windows.Interop.IWin32Window;
using MessageBox = System.Windows.MessageBox;

namespace CommunityBridge2
{
  /// <summary>
  /// Interaction logic for MainWindow.xaml
  /// </summary>
  public partial class MainWindow : INotifyPropertyChanged
  {
    private static readonly RoutedUICommand _options = new RoutedUICommand("Options", "Options", typeof(MainWindow));
    public static RoutedUICommand Options { get { return _options; } }

    private static readonly RoutedUICommand _infos = new RoutedUICommand("Infos", "Infos", typeof(MainWindow));
    public static RoutedUICommand Info { get { return _infos; } }

    private static readonly RoutedUICommand _sendDebugFiles = new RoutedUICommand("SendDebugFiles", "SendDebugFiles", typeof(MainWindow));
    public static RoutedUICommand SendDebugFiles { get { return _sendDebugFiles; } }

    private static readonly RoutedUICommand _logout = new RoutedUICommand("Logout", "Logout", typeof(MainWindow));
    public static RoutedUICommand Logout { get { return _logout; } }


    private NNTPServer.NntpServer _nntpServer;
    private Answers.ForumDataSource _forumsDataSource;
    private bool _started;
    private System.Windows.Forms.NotifyIcon _notifyIcon;

    public bool Started
    {
      get { return _started; }
      set
      {
        _started = value;
        if (_started)
        {
          lblInfo.Text = "Server started.";
          cmdStart.Content = "Stop";
          //StartWlidAutoRefresh();
        }
        else
        {
          lblInfo.Text = "Server stopped.";
          cmdStart.Content = "Start";
          //StopWlidAutoRefresh();
        }
        RaisePropertyChanged("Started");
      }
    }

    //#region WLID auto refresh

    private volatile Microsoft.Support.Community.CpsAuthHeaderBehavior _answersAuthHeader;

    //private System.Threading.AutoResetEvent _cyclingWlidEndEvent;
    //private System.Threading.Thread _cyclingWlidThread;
    //void StartWlidAutoRefresh()
    //{
    //  if (_cyclingWlidEndEvent == null)
    //  {
    //    _cyclingWlidEndEvent = new AutoResetEvent(false);
    //  }
    //  _cyclingWlidEndEvent.Reset();  // Be sure the event is not set

    //  // Start here a LiveId auto-refresh thread:
    //  _cyclingWlidThread = new Thread(WlidAutoRefreshThread);
    //  _cyclingWlidThread.IsBackground = true;
    //  _cyclingWlidThread.Start();
    //}
    //void StopWlidAutoRefresh()
    //{
    //  _cyclingWlidEndEvent.Set();
    //  _cyclingWlidThread.Join();
    //  _cyclingWlidThread = null;
    //}


    //const string answerServiceName = "cpslite.community.services.support.microsoft.com";
    //void WlidAutoRefreshThread()
    //{
    //  while (_cyclingWlidEndEvent.WaitOne(new TimeSpan(0, 57, 0)) == false)  // refresh every 57 minutes
    //  {
    //    var identity = PassportHelper.CurrentIdentity;
    //    var authHeader = _answersAuthHeader;
    //    if ((authHeader != null) && (identity != null))
    //    {
    //      Traces.Main_TraceEvent(TraceEventType.Verbose, 1, "LiveId: Try auto-re-authentication");
    //      bool suceeded = false;
    //      var authenticationData = new AuthenticationInformation();
    //      try
    //      {
    //        suceeded = PassportHelper.ReAuthenticateSilent(identity, ref authenticationData,
    //                                                       answerServiceName, "MBI",
    //                                                       true);
    //      }
    //      catch (Exception exp)
    //      {
    //        Traces.Main_TraceEvent(TraceEventType.Error, 1, "LiveId: Re-Authenticate failed: {0}", NNTPServer.Traces.ExceptionToString(exp));
    //      }
    //      if (suceeded && (authenticationData.Ticket != null))
    //      {
    //        Traces.Main_TraceEvent(TraceEventType.Verbose, 1, "LiveId: Re-Authenticate: UserName: {0}, Ticket: {1}",
    //                               authenticationData.UserName, authenticationData.Ticket);

    //        // Set the ticket in the Answer-WebService:
    //        authHeader.UpdateTicket(authenticationData.Ticket);

    //        // Also store the data (lob) in the UserSettings
    //        if (string.IsNullOrEmpty(UserSettings.Default.AuthenticationBlob) == false)
    //        {
    //          UserSettings.Default.AuthenticationBlob = authenticationData.AuthBlob;
    //          UserSettings.Default.Save();
    //        }
    //      }
    //      else
    //      {
    //        // Reset the auto login, if the authentication has failed...
    //        Traces.Main_TraceEvent(TraceEventType.Error, 1, "Could not re-authenticate with LiveId!");
    //        //UserSettings.Default.AuthenticationBlob = string.Empty;
    //        //throw new ApplicationException("Could not authenticate with LiveId!");
    //      } // identity != null
    //    } // while
    //  }
    //}  // WlidAutoRefreshThread

    //#endregion

    public MainWindow()
    {
      sMainWindow = this;
      InitializeComponent();
      this.DataContext = this;

      ApplySettings();

      Loaded += MainWindow_Loaded;
    }

    bool _loaded;
    void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
      _loaded = true;

      RemoveCloseCmd();

      InitTrayIcon();

      if (Started == false)
      {
        if (UserSettings.Default.AutoStart)
        {
          StartBridgeAsync(delegate(bool ok, string errorString)
          {
            if (ok)
            {
              if (UserSettings.Default.AutoMinimize)
              {
                WindowState = WindowState.Minimized;
              }
            }
            else
            {
              MessageBox.Show(this, errorString);
            }
          });
        }
      }
    }

    #region TrayIcon

    private void InitTrayIcon()
    {
      // NotifyToSystemTray:
      _notifyIcon = new System.Windows.Forms.NotifyIcon
      {
        BalloonTipText = "The app has been minimized. Click the tray icon to show.",
        BalloonTipTitle = UserSettings.ProductNameWithVersion,
        Text = UserSettings.ProductNameWithVersion,
        Icon = Properties.Resources.CommunityBridge2
      };
      _notifyIcon.Click += NotifyIconClick;

      Closing += OnClose;
      StateChanged += OnStateChanged;
      IsVisibleChanged += OnIsVisibleChanged;
    }

    void OnClose(object sender, System.ComponentModel.CancelEventArgs args)
    {
      _notifyIcon.Dispose();
      _notifyIcon = null;
    }

    private WindowState _storedWindowState = WindowState.Normal;
    void OnStateChanged(object sender, EventArgs args)
    {
      if (WindowState == WindowState.Minimized)
      {
        Hide();
        //if (_notifyIcon != null) _notifyIcon.ShowBalloonTip(1000);
      }
      else
        _storedWindowState = WindowState;
    }
    void OnIsVisibleChanged(object sender, DependencyPropertyChangedEventArgs args)
    {
      CheckTrayIcon();
    }

    void NotifyIconClick(object sender, EventArgs e)
    {
      Show();
      WindowState = _storedWindowState;
    }
    void CheckTrayIcon()
    {
      ShowTrayIcon(!IsVisible);
    }

    void ShowTrayIcon(bool show)
    {
      if (_notifyIcon != null)
        _notifyIcon.Visible = show;
    }

    #endregion

    private void StartBridgeAsync(Action<bool, string> onFinishedCallback)
    {
      int port = 119;
      int parsedPort;
      if (int.TryParse(txtPort.Text, out parsedPort))
        port = parsedPort;

      //bool detailedErrorResponse = UserSettings.Default.DetailedErrorResponse;
      string domainName = UserSettings.Default.DomainName;

      lblInfo.Text = "Starting server... please wait...";
      cmdStart.IsEnabled = false;

      var thread = new System.Threading.Thread(
          delegate(object o)
          {
            var t = o as MainWindow;
            var bRes = false;
            var error = string.Empty;
            try
            {
              StartBridgeInternal(t, port, domainName);
              bRes = true;
            }
            catch (Exception exp)
            {
              Traces.Main_TraceEvent(TraceEventType.Error, 1, "StartBridgeInternal: {0}", NNTPServer.Traces.ExceptionToString(exp));
              error = exp.Message;
            }
            t.Dispatcher.Invoke(
                System.Windows.Threading.DispatcherPriority.Normal,
                new Action(
                    delegate
                    {
                      if (bRes)
                      {
                        t.Started = true;
                      }
                      else
                      {
                        t.lblInfo.Text = error;
                        t.ApplySettings();  // for correcting the "LiveId auologin" menu entry
                      }
                      t.cmdStart.IsEnabled = true;
                      if (onFinishedCallback != null)
                        onFinishedCallback(bRes, error);
                    }));
          });

      thread.IsBackground = true;
      thread.SetApartmentState(System.Threading.ApartmentState.STA);
      thread.Start(this);
    }


    private static void StartBridgeInternal(MainWindow t, int port, string domainName)
    {
      string authenticationTicket = null;

      Traces.Main_TraceEvent(TraceEventType.Verbose, 1, "LiveConnect: Try authentication");
      t.DoLogin();
      authenticationTicket = t.AccessToken;
      if (string.IsNullOrEmpty(authenticationTicket))
      {
        // Reset the RefreshToken, if the authentication has failed...
        UserSettings.Default.RefreshToken = string.Empty;
        Traces.Main_TraceEvent(TraceEventType.Error, 1, "Could not authenticate with LiveConnect!");
        throw new ApplicationException("Could not authenticate with LiveConnect!");
      }
      Traces.Main_TraceEvent(TraceEventType.Information, 1, "Authenticated: AccessToken: {0}", authenticationTicket);

      //// Authenticate with Live Id
      //var authenticationData = new AuthenticationInformation();
      //Traces.Main_TraceEvent(TraceEventType.Verbose, 1, "LiveId: Try authentication");
      //try
      //{
      //  PassportHelper.AuthenticateUser(UserSettings.Default.AuthenticationBlob, ref authenticationData,
      //    answerServiceName, "MBI", true);
      //}
      //catch
      //{
      //  // Reset the auto login, if the authentication has failed...
      //  UserSettings.Default.AuthenticationBlob = string.Empty;
      //  throw;
      //}
      //Traces.Main_TraceEvent(TraceEventType.Verbose, 1, "LiveId: UserName: {0}, Ticket: {1}", authenticationData.UserName, authenticationData.Ticket);
      //if (authenticationData.Ticket == null)
      //{
      //  // Reset the auto login, if the authentication has failed...
      //  UserSettings.Default.AuthenticationBlob = string.Empty;
      //  Traces.Main_TraceEvent(TraceEventType.Error, 1, "Could not authenticate with LiveId!");
      //  throw new ApplicationException("Could not authenticate with LiveId!");
      //}
      //string authenticationTicket = authenticationData.Ticket;

      // Create the forums-ServiceProvider
      Traces.Main_TraceEvent(TraceEventType.Verbose, 1, "Create forums service provider: {0}", "social");

      var provider = new QnAClient("httpLiveAuth");
      if (provider.Endpoint.Behaviors.Contains(typeof(Microsoft.Support.Community.CpsAuthHeaderBehavior)))
      {
        provider.Endpoint.Behaviors.Remove(typeof(Microsoft.Support.Community.CpsAuthHeaderBehavior));
      }
      t._answersAuthHeader = new Microsoft.Support.Community.CpsAuthHeaderBehavior(authenticationTicket);
      provider.Endpoint.Behaviors.Add(t._answersAuthHeader);

      foreach (var op in provider.Endpoint.Contract.Operations)
      {
        var dcsb =
          op.Behaviors.Find<System.ServiceModel.Description.DataContractSerializerOperationBehavior>();
        //if (dcsb == null)
        //{
        //  dcsb = new System.ServiceModel.Description.DataContractSerializerOperationBehavior(op);
        //}
        if (dcsb != null)
        {
          const int maxObj = 65536*100;
          if (dcsb.MaxItemsInObjectGraph < maxObj)
          dcsb.MaxItemsInObjectGraph = maxObj; // Default is 65536; increase it...
          //dcsb.IgnoreExtensionDataObject = true;
        }
      }

      // Try to test the provider once:
      try
      {
        provider.GetSupportedLocales();
      }
      catch (Exception exp)
      {
        Traces.Main_TraceEvent(TraceEventType.Error, 1, NNTPServer.Traces.ExceptionToString(exp));
        throw;
      }

      // Create our DataSource for the forums
      Traces.Main_TraceEvent(TraceEventType.Verbose, 1, "Creating datasource for NNTP server");
      t._forumsDataSource = new Answers.ForumDataSource(provider, domainName);
      t._forumsDataSource.UsePlainTextConverter = UserSettings.Default.UsePlainTextConverter;
      t._forumsDataSource.AutoLineWrap = UserSettings.Default.AutoLineWrap;
      t._forumsDataSource.HeaderEncoding = UserSettings.Default.EncodingForClientEncoding;
      t._forumsDataSource.InMimeUseHtml = (UserSettings.Default.InMimeUse == UserSettings.MimeContentType.TextHtml);
      t._forumsDataSource.UserDefinedTags = UserSettings.Default.UserDefinedTags;
      t._forumsDataSource.ShowUserNamePostfix = UserSettings.Default.ShowUserNamePostfix;
      t._forumsDataSource.PostsAreAlwaysFormatFlowed = UserSettings.Default.PostsAreAlwaysFormatFlowed;
      t._forumsDataSource.TabAsSpace = UserSettings.Default.TabAsSpace;
      t._forumsDataSource.UseCodeColorizer = UserSettings.Default.UseCodeColorizer;
      t._forumsDataSource.AddHistoryToArticle = UserSettings.Default.AddHistoryToArticle;

      t._forumsDataSource.ProgressData += t._forumsDataSource_ProgressData;


      // Now start the NNTP-Server
      Traces.Main_TraceEvent(TraceEventType.Verbose, 1, "Starting NNTP server");
      t._nntpServer = new NNTPServer.NntpServer(t._forumsDataSource, true);
      t._nntpServer.EncodingSend = UserSettings.Default.EncodingForClientEncoding;
      t._nntpServer.ListGroupDisabled = UserSettings.Default.DisableLISTGROUP;
      //t._nntpServer.DetailedErrorResponse = detailedErrorResponse;
      string errorMessage;
      t._nntpServer.Start(port, 64, UserSettings.Default.BindToWorld, out errorMessage);
      if (errorMessage != null)
      {
        throw new ApplicationException(errorMessage);
      }
    }

    #region Live Connect 
      private string _AccessToken;
      internal string AccessToken
      {
        get { return _AccessToken; }
        set
        {
          _AccessToken = value;

          var authHeader = _answersAuthHeader;
          if (authHeader != null)
          {
            authHeader.UpdateTicket(_AccessToken);
          }
          //// Update the AuthenticationTicket
          //if (this._forumsProviders != null)
          //{
          //  foreach (MicrosoftForumsServiceProvider f in _forumsProviders)
          //  {
          //    f.AuthenticationTicket = _AccessToken;
          //  }
          //}
        }
      }

      internal string RefreshToken
      {
        get { return UserSettings.Default.RefreshToken; }
        set 
        { 
          UserSettings.Default.RefreshToken = value; 
          UserSettings.Default.Save();
        }
      }

      private LiveConnectHelper _liveConnectHelper;
      private void DoLogin()
      {
        //if (_liveConnectHelper == null)
        {
          _liveConnectHelper = new LiveConnectHelper();
          {
            // Notify about Session changes...
            _liveConnectHelper.SessionChanged += delegate(string accessToken, string refreshToken)
              {
                Traces.Main_TraceEvent(TraceEventType.Information, 1,
                                       "Authenticated: Session changed! AccessToken: {0}, RefreshToken: {1}",
                                       accessToken, refreshToken);
                AccessToken = accessToken;
                RefreshToken = refreshToken;
              };
          }
        }
        _liveConnectHelper.DoLogin(Win32Wnd);
      }

      private void DoLogout()
      {
        if (_liveConnectHelper == null)
        {
          _liveConnectHelper = new LiveConnectHelper();
        }
        _liveConnectHelper.DoLogout(Win32Wnd);
        UserSettings.Default.RefreshToken = null;
        UserSettings.Default.Save();
        // Terminate app
        this.Close();
      }

      Wpf32Window Win32Wnd { get; set; }

      private delegate void SessionChangedEventHandler(string accessToken, string refreshToken);
      private class LiveConnectHelper : IRefreshTokenHandler
      {
        public void DoLogin(System.Windows.Forms.IWin32Window wnd)
        {
          Task<AggregateException> task = AuthClient.IntializeAsync(Scopes).ContinueWith(t =>
            {
              if (t.Exception != null)
                return t.Exception;
              return null;
            });
          task.Wait();

          LiveConnectSession session = this.AuthClient.Session;
          bool isSignedIn = session != null;
          if (isSignedIn == false)
          {
            string startUrl = this.AuthClient.GetLoginUrl(this.Scopes);
            const string endUrl = "https://login.live.com/oauth20_desktop.srf";
            this.authForm = new LiveAuthForm(
              startUrl,
              endUrl,
              this.OnAuthCompleted);
            this.authForm.FormClosed += AuthForm_FormClosed;
            this.authForm.NavigatedUrl = OnAuthNavigatedUrl;
            this.authForm.ShowDialog(wnd);
          }
        }

        private LiveAuthForm authForm;
        private LiveAuthClient _authClient;

        private LiveAuthClient AuthClient
        {
          get
          {
            if (_authClient == null)
            {
              _authClient = new LiveAuthClient(UserSettings.Default.ClientId, this);
              _authClient.PropertyChanged += AuthClientOnPropertyChanged;
            }
            return _authClient;
          }
        }

        private void AuthClientOnPropertyChanged(object sender, PropertyChangedEventArgs propertyChangedEventArgs)
        {
          if (string.Equals(propertyChangedEventArgs.PropertyName, "Session", StringComparison.Ordinal))
          {
            OnSessionChanged();
          }
        }

        private void OnAuthNavigatedUrl(string url)
        {
          Traces.Main_TraceEvent(TraceEventType.Information, 1, string.Format("AuthNavigatedUrl: {0}", url));
        }
        private void OnAuthCompleted(AuthResult result)  // async
        {
          Traces.Main_TraceEvent(TraceEventType.Information, 1, string.Format("AuthResult: {0} - {1} - {2}", 
            result.ErrorCode, result.ErrorDescription, result.AuthorizeCode));
          this.CleanupAuthForm();
          if (result.AuthorizeCode != null)
          {
            Task task = this.AuthClient.ExchangeAuthCodeAsync(result.AuthorizeCode).ContinueWith(t =>
            {
              if (t.Exception != null)
                return t.Exception;
              return null;
            });
            task.Wait();
          }
        }

        void AuthForm_FormClosed(object sender, FormClosedEventArgs e)
        {
          this.CleanupAuthForm();
        }

        private void CleanupAuthForm()
        {
          if (this.authForm != null)
          {
            this.authForm.Dispose();
            this.authForm = null;
          }
        }

        void RefreshThread(object o)
        {
          while (true)
          {
            Thread.Sleep(50 * 60 * 1000);  // Wait 50 Minutes...
            //Thread.Sleep(1 * 60 * 1000);  // Wait 1 Minute for test...
            LiveAuthClient client = this.AuthClient;
            if (client != null)
            {
              Traces.Main_TraceEvent(TraceEventType.Information, 1, "Try to refresh token!");
              bool res = client.ForceRefreshToken(result =>
                {
                  // Result of refresh token
                  Traces.Main_TraceEvent(TraceEventType.Information, 1, "RefreshToken Result: State: {0}, Status: {1}", result.State, result.Status);

                  // Inform the others that the accesstoken has changed...
                  // INFO: Do we need to synchronize the access to the token!?
                  OnSessionChanged();
                }
                );
              if (res == false)
              {
                Traces.Main_TraceEvent(TraceEventType.Error, 1, "RefreshToken failed (false)");
              }
            }
          }
        }

        private System.Threading.Thread _RefreshThread;
        private void OnSessionChanged()
        {
          LiveConnectSession session = this.AuthClient.Session;
          //bool isSignedIn = session != null;
          if (session != null)
          {
            Traces.Main_TraceEvent(TraceEventType.Error, 1, "Session changed: Expires: {0}, AccessToken: {1}", session.Expires.ToString("u", System.Globalization.CultureInfo.InvariantCulture), session.AccessToken);

            if (_RefreshThread == null)
            {
              _RefreshThread = new Thread(RefreshThread);
              _RefreshThread.IsBackground = true;
              _RefreshThread.Name = "RefreshThread";
              _RefreshThread.Start(null);
            }

            var handler = SessionChanged;
            if (handler != null)
            {
              handler(session.AccessToken, session.RefreshToken);
            }
          }
        }

        public event SessionChangedEventHandler SessionChanged;

        private IEnumerable<string> Scopes
        {
          get
          {
            return
              UserSettings.Default.Scopes.Split(' ')
                          .Select(p => p.Trim())
                          .Where(p => string.IsNullOrEmpty(p) == false)
                          .ToList();
          }
        }

        Task IRefreshTokenHandler.SaveRefreshTokenAsync(RefreshTokenInfo tokenInfo)
        {
          // RefreshToken is saved if the property is set in the mainwindow; so no action here...
          return Task.Factory.StartNew(() => { });
        }

        Task<RefreshTokenInfo> IRefreshTokenHandler.RetrieveRefreshTokenAsync()
        {
          string refreshTokenInfo = UserSettings.Default.RefreshToken;
          return Task.Factory.StartNew(() =>
          {
            if (string.IsNullOrEmpty(refreshTokenInfo) == false)
              return new RefreshTokenInfo(refreshTokenInfo);
            return null;
          });
        }


        internal void DoLogout(System.Windows.Forms.IWin32Window wnd)
        {
          Task<AggregateException> task = AuthClient.IntializeAsync(Scopes).ContinueWith(t =>
            {
              if (t.Exception != null)
                return t.Exception;
              return null;
            });
          task.Wait();

          {
            string startUrl = this.AuthClient.GetLogoutUrl();
            //const string endUrl = "https://login.live.com/oauth20_desktop.srf";
            this.authForm = new LiveAuthForm(
              startUrl,
              null,
              null);
            this.authForm.FormClosed += AuthForm_FormClosed;
            this.authForm.ShowDialog(wnd);
          }
        }
      }

      public class Wpf32Window : System.Windows.Forms.IWin32Window
      {
        public IntPtr Handle { get; private set; }

        public Wpf32Window(Window wpfWindow)
        {
          Handle = new WindowInteropHelper(wpfWindow).Handle;
        }
      }    
    #endregion

    void _forumsDataSource_ProgressData(object sender, ProgressDataEventArgs e)
    {
      Dispatcher.BeginInvoke(
        System.Windows.Threading.DispatcherPriority.Normal,
        new Action(() =>
                     {
                       lblInfo.Text = e.Text;
                     }));
    }

    protected override void OnClosed(EventArgs e)
    {
      base.OnClosed(e);
      if (_loaded)
      {
        int parsedPort;
        if (int.TryParse(txtPort.Text, out parsedPort))
          UserSettings.Default.Port = parsedPort;

        UserSettings.Default.Save();
      }
    }

    private void CmdStartClick(object sender, RoutedEventArgs e)
    {
      if (Started)
      {
        _nntpServer.Stop();
        _nntpServer.Dispose();
        _nntpServer = null;
        _forumsDataSource = null;
        cmdStart.Content = "Start";
        Started = false;
        return;
      }

      StartBridgeAsync(
          delegate(bool ok, string errorString)
          {
            if (ok == false)
            {
              MessageBox.Show(this, errorString);
            }
          });
    }

    private void CmdLoadNewsgroupListClick(object sender, RoutedEventArgs e)
    {
      cmdLoadNewsgroupList.IsEnabled = false;
      ThreadPool.QueueUserWorkItem(delegate(object o)
      {
        var t = o as MainWindow;
        try
        {
          var idx = 0;
          Traces.Main_TraceEvent(TraceEventType.Information, 1, "Start prefetching newsgroup list");
          SetPrefetchInfo(t, "Start prefetching newsgroup list", false);
          var groups = t._forumsDataSource.PrefetchNewsgroupList(
              p => SetPrefetchInfo(t, string.Format("Group {0}: {1}", ++idx, p.GroupName), false));
          SetPrefetchInfo(t, string.Format("DONE: ({0} newsgroups)", idx), true);
          Traces.Main_TraceEvent(TraceEventType.Information, 1, "Prefetching DONE; {0} newsgroups", idx);
          if ((groups != null) && (groups.Count > 0))
          {
            // Save UI-Cache and refresh
            if (!t.Dispatcher.CheckAccess())
            {
              t.Dispatcher.Invoke(
                System.Windows.Threading.DispatcherPriority.Normal,
                new Action(
                  delegate
                    {
                      var uic = new NewsgroupAnswersCollection();
                      uic.AddRange(groups.Select(p => new NewsgroupAnswers()
                      {
                        Name = p.GroupName
                      }).OrderBy(p2 => p2.Name, StringComparer.InvariantCultureIgnoreCase)
                                                         );
                      SaveUICache(uic);
                    }));
            }
          }
        }
        catch (Exception exp)
        {
          SetPrefetchInfo(t, string.Format("Exception: {0}", NNTPServer.Traces.ExceptionToString(exp)), true);
        }
      }, this);
    }
    private static void SetPrefetchInfo(MainWindow t, string text, bool finished)
    {
      //if (string.IsNullOrEmpty(text) == false)
      //  Traces.Main_TraceEvent(TraceEventType.Information, 1, text);

      t.Dispatcher.BeginInvoke(
        System.Windows.Threading.DispatcherPriority.Normal,
        new Action(
        delegate
        {
          t.txtPrefetchInfo.Text = text;
          if (finished)
            t.cmdLoadNewsgroupList.IsEnabled = true;
        }));
    }

    private void CbAutoStartChecked(object sender, RoutedEventArgs e)
    {
      if (cbAutoStart.IsChecked.HasValue)
        UserSettings.Default.AutoStart = cbAutoStart.IsChecked.Value;
    }

    private void CbAutoMinimizeChecked(object sender, RoutedEventArgs e)
    {
      if (cbAutoMinimize.IsChecked.HasValue)
        UserSettings.Default.AutoMinimize = cbAutoMinimize.IsChecked.Value;
    }

    private void cbUsePlainTextConverter_Click(object sender, RoutedEventArgs e)
    {
      if (cbUsePlainTextConverter.IsChecked.HasValue)
      {
        UsePlainTextConverters us = UsePlainTextConverters.None;
        if (cbUsePlainTextConverter.IsChecked.Value)
          us = UsePlainTextConverters.SendAndReceive;
        if (_forumsDataSource != null)
        {
          _forumsDataSource.UsePlainTextConverter = us;
        }
        UserSettings.Default.UsePlainTextConverter = us;
      }

    }

    private void OnCloseExecute(object sender, ExecutedRoutedEventArgs e)
    {
      this.Close();
    }

    private void OnCanCloseExecute(object sender, CanExecuteRoutedEventArgs e)
    {
      e.CanExecute = true;
      e.Handled = true;
    }

    private void OnInfoExecute(object sender, ExecutedRoutedEventArgs e)
    {
      var d = new InfoDialog();
      d.Owner = this;
      d.ShowDialog();
    }

    private void OnCanInfoExecute(object sender, CanExecuteRoutedEventArgs e)
    {
      e.CanExecute = true;
      e.Handled = true;
    }

    private void OnSendDebugFilesExecute(object sender, ExecutedRoutedEventArgs e)
    {
      var app = (App) Application.Current;
      var dlg = new SendDebugDataWindow();
      dlg.Owner = this;
      if (dlg.ShowDialog() == true)
      {      
        app.SendLogs(null, dlg.UsersEMail, dlg.UsersDescription, dlg.UserSendEmail);
      }
    }

    private void OnCanSendDebugFilesExecute(object sender, CanExecuteRoutedEventArgs e)
    {
      e.CanExecute = true;
      e.Handled = true;
    }

    private void OnOptionsExecute(object sender, ExecutedRoutedEventArgs e)
    {
      var d = new AdvancedSettingsDialog();
      d.SelectedObject = UserSettings.Default.Clone();
      d.Owner = this;
      if (d.ShowDialog() == true)
      {
        UserSettings.Default = d.SelectedObject as UserSettings;
        ApplySettings();
      }
    }

    private void OnLogoutExecute(object sender, ExecutedRoutedEventArgs e)
    {
      this.DoLogout();
    }

    private void OnCanLogoutExecute(object sender, CanExecuteRoutedEventArgs e)
    {
      e.CanExecute = true;
      e.Handled = true;
    }

    private void ApplySettings()
    {
      Title = UserSettings.ProductNameWithVersion;
      GeneralResponses.SetName(UserSettings.ProductNameWithVersion);
      Article.SetName(UserSettings.ProductNameWithVersion);


      cbAutoStart.IsChecked = UserSettings.Default.AutoStart;
      cbAutoMinimize.IsChecked = UserSettings.Default.AutoMinimize;
      if (UserSettings.Default.UsePlainTextConverter == UsePlainTextConverters.None)
        cbUsePlainTextConverter.IsChecked = false;
      else if (UserSettings.Default.UsePlainTextConverter == UsePlainTextConverters.SendAndReceive)
        cbUsePlainTextConverter.IsChecked = true;
      else
        cbUsePlainTextConverter.IsChecked = null;

      txtPort.Text = UserSettings.Default.Port.ToString();

      if (_forumsDataSource != null)
      {
        _forumsDataSource.ClearCache();
        _forumsDataSource.UsePlainTextConverter = UserSettings.Default.UsePlainTextConverter;
        _forumsDataSource.AutoLineWrap = UserSettings.Default.AutoLineWrap;
        _forumsDataSource.HeaderEncoding = UserSettings.Default.EncodingForClientEncoding;
        _forumsDataSource.InMimeUseHtml = (UserSettings.Default.InMimeUse == UserSettings.MimeContentType.TextHtml);
        _forumsDataSource.UserDefinedTags = UserSettings.Default.UserDefinedTags;
        _forumsDataSource.ShowUserNamePostfix = UserSettings.Default.ShowUserNamePostfix;
        _forumsDataSource.PostsAreAlwaysFormatFlowed = UserSettings.Default.PostsAreAlwaysFormatFlowed;
        _forumsDataSource.TabAsSpace = UserSettings.Default.TabAsSpace;
        _forumsDataSource.UseCodeColorizer = UserSettings.Default.UseCodeColorizer;
        _forumsDataSource.AddHistoryToArticle = UserSettings.Default.AddHistoryToArticle;
      }

      if (_nntpServer != null)
      {
        _nntpServer.EncodingSend = UserSettings.Default.EncodingForClientEncoding;
        _nntpServer.ListGroupDisabled = UserSettings.Default.DisableLISTGROUP;
      }
    }

    private void OnCanOptionsExecute(object sender, CanExecuteRoutedEventArgs e)
    {
      e.CanExecute = true;
      e.Handled = true;
    }

    private DebugWindow _debugWindow;
    private void mnuDebugWindow_Click(object sender, RoutedEventArgs e)
    {
      try
      {
        if (mnuDebugWindow.IsChecked)
        {
          _debugWindow = new DebugWindow();
          _debugWindow.Owner = this;
          _debugWindow.Show();
          _debugWindow.Closed += new EventHandler(_debugWindow_Closed);
        }
        else
        {
          _debugWindow.Close();
          _debugWindow = null;
        }
      }
      catch (Exception exp)
      {
        Traces.Main_TraceEvent(TraceEventType.Error, 1, "Error in mnuDebugWindow_click: {0}", NNTPServer.Traces.ExceptionToString(exp));
      }
    }

    void _debugWindow_Closed(object sender, EventArgs e)
    {
      mnuDebugWindow.IsChecked = false;
    }

    private void cmdExit_Click(object sender, RoutedEventArgs e)
    {
      Close();
    }

    #region Win32
    //private const int GWL_STYLE = -16;
    //private const int WS_SYSMENU = 0x80000;
    //[DllImport("user32.dll", SetLastError = true)]
    //private static extern int GetWindowLong(IntPtr hWnd, int nIndex);
    //[DllImport("user32.dll")]
    //private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

    //const int MF_BYPOSITION = 0x400;
    private const int MF_BYCOMMAND = 0x0;
    private const int SC_CLOSE = 0xF060;
    [DllImport("User32")]
    private static extern int RemoveMenu(IntPtr hMenu, int nPosition, int wFlags);
    [DllImport("User32")]
    private static extern IntPtr GetSystemMenu(IntPtr hWnd, bool bRevert);
    [DllImport("User32")]
    private static extern int GetMenuItemCount(IntPtr hWnd);

    void RemoveCloseCmd()
    {
      var hwnd = new System.Windows.Interop.WindowInteropHelper(this).Handle;
      IntPtr hMenu = GetSystemMenu(hwnd, false);
      int menuItemCount = GetMenuItemCount(hMenu);
      //RemoveMenu(hMenu, menuItemCount - 1, MF_BYPOSITION);
      RemoveMenu(hMenu, SC_CLOSE, MF_BYCOMMAND);
    }
    #endregion

    #region NewsgroupList

    private void SaveUICache(NewsgroupAnswersCollection groups)
    {
      var ui = new UICache();
      ui.NewsgroupAnswers = groups;
      ui.Save(GetFileName());

      // force reloading of list...
      _groupView = null;
      _groupViewSource = null;
      firstTime = true;
      RaisePropertyChanged("Newsgroups");
    }

    private UICache LoadUICache()
    {
      return UICache.Load(GetFileName());
    }

    string GetFileName()
    {
      return Path.Combine(UserSettings.Default.BasePath, "UICache.xml");
    }

    private bool firstTime = true;
    private NewsgroupAnswersCollection _groupViewSource;
    private ICollectionView _groupView;
    public ICollectionView Newsgroups
    {

      get
      {
        if ((_groupView == null) && (firstTime))
        {
          firstTime = false;
          var uc = LoadUICache();
          if ((uc != null) && (uc.NewsgroupAnswers != null))
          {
            var g = new NewsgroupAnswersCollection() ;
            g.AddRange(uc.NewsgroupAnswers.OrderBy(p2 => p2.Name, StringComparer.InvariantCultureIgnoreCase));
            _groupViewSource = g;
            _groupView = CollectionViewSource.GetDefaultView(g);
            _groupView.Filter = MyFilter;
            foreach(var f in g)
            {
              // Connect Prefetch-Callback to group
              f.OnPrefetch += f_OnPrefetch;
              // INFO: We are aware that this migth lead to mem-leaks; but a user normally does not press "prefetch groups" that often...
            }
            UpdateFilterData();
          }
        }
        return _groupView;
      }
    }

    void f_OnPrefetch(object sender, EventArgs e)
    {
      var g = sender as NewsgroupAnswers;
      if (g != null)
      {
        g.Info = "?";
        g.Action.ChangeCanExecute(false);
        System.Threading.ThreadPool.QueueUserWorkItem(delegate(object o)
        {
          var t = o as MainWindow;
          try
          {
            bool exceptionOccured;
            t._forumsDataSource.GetNewsgroup("<self>", g.Name, true, out exceptionOccured);
          }
          catch(Exception exp)
          {
            Traces.Main_TraceEvent(TraceEventType.Error, 1, NNTPServer.Traces.ExceptionToString(exp));
            NewsgroupUpdateInfo(g.Name, string.Format("EXCEPTION: {0}", exp.Message), true);
          }
          NewsgroupUpdateInfo(g.Name, null, true);  // Stelle auf jeden Fall sicher, dass der Button wieder enabled wird
        }, this);
      }
    }

    bool MyFilter(object o)
    {
      if (_searchText == null)
        return true;
      var g = o as NewsgroupAnswers;
      if (g != null)
      {
        // AND filter!
        var matchCount = new int[_searchText.Length];
        int idx = 0;
        foreach (var s in _searchText)
        {
          if (string.IsNullOrEmpty(s))
          {
            matchCount[idx] = 1;
            idx++;
            continue;
          }

          if (string.IsNullOrEmpty(g.Name) == false)
          {
            if (g.Name.IndexOf(s, StringComparison.CurrentCultureIgnoreCase) >= 0)
            {
              matchCount[idx]++;
            }
          }

          idx++;
        }

        bool allGreater0 = true;
        foreach (var i in matchCount)
        {
          if (i <= 0)
            allGreater0 = false;
        }
        if (allGreater0)
          return true;
        return false;
      }
      return true;
    }

    private string[] _searchText;
    private string _NewsgroupSearchText = string.Empty;
    public string NewsgroupSearchText
    {
      get
      {
        return _NewsgroupSearchText;
      }
      set
      {
        _NewsgroupSearchText = value;
        if (string.IsNullOrEmpty(_NewsgroupSearchText))
          _searchText = null;
        else
          _searchText = _NewsgroupSearchText.Split(' ');
        if (_groupView != null)
        {
          _groupView.Refresh();
          UpdateFilterData();
        }
      }
    }

    private void UpdateFilterData()
    {
      if (_groupView != null)
      {
        var sl = _groupView.SourceCollection as System.Collections.ICollection;
        if (sl != null)
        {
          int? act = null;
          int total = sl.Count;
          var lcv = _groupView as CollectionView;
          if (lcv != null)
            act = lcv.Count;
          if (act.HasValue)
            _filterInfo = string.Format("{0} / {1}", act.Value, total);
          else
            _filterInfo = string.Format("{0}", total);
        }
      }
      RaisePropertyChanged("FilterInfo");
    }

    private string _filterInfo = string.Empty;
    public string FilterInfo
    {
      get { return _filterInfo; }
    }

    #endregion

    public event PropertyChangedEventHandler PropertyChanged;
    void RaisePropertyChanged(string name)
    {
      if (PropertyChanged != null)
        PropertyChanged(this, new PropertyChangedEventArgs(name));
    }


    internal static MainWindow sMainWindow;
    internal static void NewsgroupUpdateInfo(string groupName, string text, bool enabledPrefetch = false)
    {
      if (string.IsNullOrEmpty(text) == false)
        Traces.Main_TraceEvent(TraceEventType.Information, 1, groupName + ": " + text);

      if (sMainWindow == null) return;

      sMainWindow.Dispatcher.BeginInvoke(
        System.Windows.Threading.DispatcherPriority.Normal,
        new Action(() =>
        {
          // Try to find the group in the prefetch list and update the text...
          if (sMainWindow._groupViewSource != null)
          {
            var uic = sMainWindow._groupViewSource.FirstOrDefault(
              p => string.Equals(p.Name, groupName, StringComparison.OrdinalIgnoreCase));
            if (uic != null)
            {
              if (string.IsNullOrEmpty(text) == false)
                uic.Info = text;
              if (enabledPrefetch)
                uic.Action.ChangeCanExecute(true);
            }
          }
        }));
    }
  }// class MainWindow

  public class UICache
  {
    private NewsgroupAnswersCollection _newsgroupAnswers = new NewsgroupAnswersCollection();
    public NewsgroupAnswersCollection NewsgroupAnswers
    {
      get { return _newsgroupAnswers; }
      set { _newsgroupAnswers = value; }
    }

    public void Save(string filename)
    {
      try
      {
        var path = Path.GetDirectoryName(filename);
        if (Directory.Exists(path) == false)
          Directory.CreateDirectory(path);
        var ser = new XmlSerializer(typeof(UICache));
        using (var sw = new StreamWriter(filename))
        {
          ser.Serialize(sw, this);
        }
      }
      catch (Exception exp)
      {
        Traces.Main_TraceEvent(TraceEventType.Critical, 1, "Error while serializing UICache: {0}", NNTPServer.Traces.ExceptionToString(exp));
      }
    }

    static public UICache Load(string fileName)
    {
      try
      {
        if (Directory.Exists(Path.GetDirectoryName(fileName)) == false)
          return null;
        if (File.Exists(fileName) == false)
          return null;

        var ser = new XmlSerializer(typeof(UICache));
        using (var sr = new StreamReader(fileName))
        {
          var res = ser.Deserialize(sr) as UICache;
          return res;
        }
      }
      catch (Exception exp)
      {
        Traces.Main_TraceEvent(TraceEventType.Critical, 1, "Error while deserializing UICache: {0}\r\n{1}", fileName, NNTPServer.Traces.ExceptionToString(exp));
      }
      return null;
    }
  }


  public class NewsgroupAnswers : INotifyPropertyChanged
  {
    public NewsgroupAnswers()
    {
      Action = new RelayCommand(OnAction);
      _actionText = "Prefetch";
    }

    [XmlAttribute("n")]
    public string Name { get; set; }

    private string _actionText;
    [XmlIgnore]
    public string ActionText
    {
      get { return _actionText; }
      set
      {
        _actionText = value;
        FirePropertyChanged("ActionText");
      }
    }

    private string _info;
    [XmlIgnore]
    public string Info
    {
      get { return _info; }
      set
      {
        _info = value;
        FirePropertyChanged("Info");
      }
    }


    [XmlIgnore]
    public RelayCommand Action { get; set; }

    private void OnAction(object parameter)
    {
      if (OnPrefetch != null)
        OnPrefetch(this, EventArgs.Empty);
    }

    public event EventHandler OnPrefetch;

    public event PropertyChangedEventHandler PropertyChanged;
    protected void FirePropertyChanged(string propertyName)
    {
      if (PropertyChanged != null)
        PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
    }
  }
  public class NewsgroupAnswersCollection : List<NewsgroupAnswers> { }
}
