using System;
using System.Collections.Generic;
using System.ComponentModel;
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
  /// Interaction logic for SendDebugDataWindow.xaml
  /// </summary>
  public partial class SendDebugDataWindow : Window, INotifyPropertyChanged
  {
    public SendDebugDataWindow()
    {
      InitializeComponent();
    }

    private string _unhandledExceptionMessage;
    public string UnhandledExceptionMessage
    {
      get { return _unhandledExceptionMessage; }
      set
      {
        _unhandledExceptionMessage = value;
        RaisePropertyChanged("UnhandledExceptionMessage");
        RaisePropertyChanged("UnhandledExceptionVisible");
      }
    }

    public Visibility UnhandledExceptionVisible
    {
      get
      {
        if (string.IsNullOrEmpty(UnhandledExceptionMessage))
          return Visibility.Collapsed;
        return Visibility.Visible;
      }
    }

    public string UsersDescription { get; set; }

    public string UsersEMail { get; set; }

    public bool UserSendEmail { get; set; }

    private void OkClick(object sender, RoutedEventArgs e)
    {
      DialogResult = true;
      Close();
    }

    private void CancelClick(object sender, RoutedEventArgs e)
    {
      DialogResult = false;
      Close();
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
      txtEmail.Focus();
    }

    public event PropertyChangedEventHandler PropertyChanged;
    protected void RaisePropertyChanged(string propertyName)
    {
      if (PropertyChanged != null)
        PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
    }
  }
}
