using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace CommunityBridge2
{
    /// <summary>
    /// Interaction logic for DebugWindow.xaml
    /// </summary>
    public partial class DebugWindow : Window
    {
        public DebugWindow()
        {
            InitializeComponent();

            this.cbNntp.IsChecked = true;
            this.cbMain.IsChecked = true;
            this.cbWebService.IsChecked = true;
        }

        protected override void OnClosed(EventArgs e)
        {
            cbNntp_Unchecked(null, null);

            base.OnClosed(e);
        }

        private void cbNntp_Checked(object sender, RoutedEventArgs e)
        {
            NNTPServer.Traces.NntpServer.Listeners.Add(new MyListener(this.lb));
            NNTPServer.Traces.NntpServer.Switch = new SourceSwitch("NNTPServer", "Verbose");
        }

        private void cbNntp_Unchecked(object sender, RoutedEventArgs e)
        {
            foreach (var l in NNTPServer.Traces.NntpServer.Listeners)
            {
                var myl = l as MyListener;
                if (myl != null)
                {
                    NNTPServer.Traces.NntpServer.Listeners.Remove(myl);
                    break;
                }
            }
        }

        class MyListener : TraceListener
        {
            public MyListener(ListBox lb)
            {
                this.Name = "MyInternalTraceListener";
                _lb = lb;
            }

            private ListBox _lb;

            public override void Write(string message)
            {
                //AddToWindow(message);
            }
            public override void WriteLine(string message)
            {
                AddToWindow(message);
            }

            void AddToWindow(string s)
            {
                if (_lb.CheckAccess() == false)
                {
                    _lb.Dispatcher.BeginInvoke(
                      System.Windows.Threading.DispatcherPriority.Normal,
                      new Action(() =>
                      {
                          if (_lb.Items.Count > 3000)
                              _lb.Items.RemoveAt(0);
                          _lb.Items.Add(s);
                      }));
                }
                else
                {
                    if (_lb.Items.Count > 3000)
                        _lb.Items.RemoveAt(0);
                    _lb.Items.Add(s);
                }
            }
        }

        private void cbTopmost_Checked(object sender, RoutedEventArgs e)
        {
            this.Topmost = true;
        }

        private void cbTopmost_Unchecked(object sender, RoutedEventArgs e)
        {
            this.Topmost = false;
        }

        private void cbMain_Checked(object sender, RoutedEventArgs e)
        {
            Traces.Main.Listeners.Add(new MyListener(this.lb));
            Traces.Main.Switch = new SourceSwitch("Main", "Verbose");
        }

        private void cbMain_Unchecked(object sender, RoutedEventArgs e)
        {
            foreach (var l in Traces.Main.Listeners)
            {
                var myl = l as MyListener;
                if (myl != null)
                {
                    Traces.Main.Listeners.Remove(myl);
                    break;
                }
            }
        }

        private void cbWebService_Checked(object sender, RoutedEventArgs e)
        {
            Traces.WebService.Listeners.Add(new MyListener(this.lb));
            Traces.WebService.Switch = new SourceSwitch("WebService", "Verbose");
        }

        private void cbWebService_Unchecked(object sender, RoutedEventArgs e)
        {
            foreach (var l in Traces.WebService.Listeners)
            {
                var myl = l as MyListener;
                if (myl != null)
                {
                    Traces.WebService.Listeners.Remove(myl);
                    break;
                }
            }
        }

        private void MenuItem_Click(object sender, RoutedEventArgs e)
        {
            StringBuilder sb = new StringBuilder();
            foreach(var o in this.lb.SelectedItems)
            {
                sb.Append(o);
                sb.AppendLine();
            }

            Clipboard.SetText(sb.ToString());
        }

        private void cbConverters_Checked(object sender, RoutedEventArgs e)
        {
            ArticleConverter.Traces.Converters.Listeners.Add(new MyListener(this.lb));
            ArticleConverter.Traces.Converters.Switch = new SourceSwitch("Converters", "Verbose");
        }

        private void cbConverters_Unchecked(object sender, RoutedEventArgs e)
        {
            foreach (var l in ArticleConverter.Traces.Converters.Listeners)
            {
                var myl = l as MyListener;
                if (myl != null)
                {
                    ArticleConverter.Traces.Converters.Listeners.Remove(myl);
                    break;
                }
            }
        }

        private void btnCopyToClipboard_Click(object sender, RoutedEventArgs e)
        {
          StringBuilder sb = new StringBuilder();
          foreach (var o in this.lb.Items)
          {
            sb.Append(o);
            sb.AppendLine();
          }

          Clipboard.SetText(sb.ToString());
        }

        private void btnClear_Click(object sender, RoutedEventArgs e)
        {
          lb.Items.Clear();

        }
    }
}
