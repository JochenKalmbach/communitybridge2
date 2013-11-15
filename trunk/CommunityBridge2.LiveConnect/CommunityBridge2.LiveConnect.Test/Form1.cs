using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using CommunityBridge2.LiveConnect.Public;

namespace CommunityBridge2.LiveConnect.Test
{
  public partial class Form1 : Form, IRefreshTokenHandler
  {
    public Form1()
    {
      InitializeComponent();
    }

    private void Form1_Load(object sender, EventArgs e)
    {
      this.txtClientID.Text = Properties.Settings.Default.ClientID;
      this.txtScopes.Text = Properties.Settings.Default.Scopes;
      this.txtRefreshToken.Text = Properties.Settings.Default.RefreshToken;

      OnSessionChanged();
    }

    private void btnSaveClientId_Click(object sender, EventArgs e)
    {
      Properties.Settings.Default.ClientID = this.txtClientID.Text;
      Properties.Settings.Default.Scopes = this.txtScopes.Text;
      Properties.Settings.Default.RefreshToken = this.txtRefreshToken.Text;
      Properties.Settings.Default.Save();
    }

    private LiveAuthForm authForm;

    private void btnLogin_Click(object sender, EventArgs e)
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
        string endUrl = "https://login.live.com/oauth20_desktop.srf";
        this.authForm = new LiveAuthForm(
            startUrl,
            endUrl,
            this.OnAuthCompleted);
        this.authForm.FormClosed += AuthForm_FormClosed;
        this.authForm.ShowDialog(this);
      }
    }

    private void OnAuthCompleted(AuthResult result)  // async
    {
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

    private void btnLogout_Click(object sender, EventArgs e)
    {
      this.signOutWebBrowser.Navigate(this.AuthClient.GetLogoutUrl());
      this.AuthClient = null;
      this.OnSessionChanged();
    }

    IEnumerable<string> Scopes
    {
      get
      {
        return txtScopes.Text.Split(' ').Select(p => p.Trim()).Where(p => string.IsNullOrEmpty(p) == false).ToList();
      }
    }

    private LiveAuthClient _AuthClient;
    public LiveAuthClient AuthClient
    {
      get
      {
        if (_AuthClient == null)
          AuthClient = new LiveAuthClient(txtClientID.Text, this);
        return _AuthClient;
      }
      set
      {
        if (_AuthClient != null)
        {
          _AuthClient.PropertyChanged -= AuthClientOnPropertyChanged;
        }
        _AuthClient = value;
        if (_AuthClient != null)
        {
          _AuthClient.PropertyChanged += AuthClientOnPropertyChanged;
        }
      }
    }

    private void AuthClientOnPropertyChanged(object sender, PropertyChangedEventArgs propertyChangedEventArgs)
    {
      if (string.Equals(propertyChangedEventArgs.PropertyName, "Session", StringComparison.Ordinal))
      {
        OnSessionChanged();
      }
    }

    private void OnSessionChanged()
    {
      LiveConnectSession session = this.AuthClient.Session;
      bool isSignedIn = session != null;
      this.btnLogin.Enabled = !isSignedIn;
      this.btnLogout.Enabled = isSignedIn;
      if (session != null)
      {
        this.txtRefreshToken.Text = session.RefreshToken;
        this.txtAccessToken.Text = session.AccessToken;
        this.txtExpires.Text = session.Expires.ToString("u");
      }
      else
      {
        this.txtAccessToken.Text = string.Empty;
        this.txtExpires.Text = string.Empty;
      }
    }

    Task IRefreshTokenHandler.SaveRefreshTokenAsync(RefreshTokenInfo tokenInfo)
    {
      return Task.Factory.StartNew(() =>
      {
        // Is stored in "OnSessionChanged"
        //this.BeginInvoke(new Action<RefreshTokenInfo>
        //  (
        //                   token =>
        //                     {
        //                       this.txtRefreshToken.Text = token.RefreshToken;
        //                     }
        //                   ), tokenInfo);
      });
    }

    Task<RefreshTokenInfo> IRefreshTokenHandler.RetrieveRefreshTokenAsync()
    {
      string refreshTokenInfo = txtRefreshToken.Text;
      return Task.Factory.StartNew(() =>
        {
          //string refreshTokenInfo = Properties.Settings.Default.RefreshToken;
          if (string.IsNullOrEmpty(refreshTokenInfo) == false)
            return new RefreshTokenInfo(refreshTokenInfo);
          return null;
        });
    }
  }
}
