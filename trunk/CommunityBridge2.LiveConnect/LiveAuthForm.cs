using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace CommunityBridge2.LiveConnect
{
    public delegate void AuthCompletedCallback(AuthResult result);

    public partial class LiveAuthForm : Form
    {
        private readonly string startUrl;
        private readonly string endUrl;
        private readonly AuthCompletedCallback callback;

        public LiveAuthForm(string startUrl, string endUrl, AuthCompletedCallback callback)
        {
            this.startUrl = startUrl;
            this.endUrl = endUrl;
            this.callback = callback;
            InitializeComponent();
        }

      public Action<string> NavigatedUrl;

      private void LiveAuthForm_Load(object sender, EventArgs e)
        {
            this.webBrowser.Navigated += WebBrowser_Navigated;
            this.webBrowser.Navigate(this.startUrl);

        }

        private void WebBrowser_Navigated(object sender, WebBrowserNavigatedEventArgs e)
        {
          if (NavigatedUrl != null)
          {
            NavigatedUrl(e.Url.ToString());
          }
          if (string.IsNullOrEmpty(endUrl) == false)
          {
            if (this.webBrowser.Url.AbsoluteUri.StartsWith(this.endUrl))
            {
              if (this.callback != null)
              {
                this.callback(new AuthResult(this.webBrowser.Url));
              }
            }
          }
          else
          {
            this.Close();
          }
        }
    }
}
