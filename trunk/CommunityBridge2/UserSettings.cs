using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using CommunityBridge2.NNTPServer;
using Microsoft.Win32;

namespace CommunityBridge2
{
    internal class UserSettings
    {
        static UserSettings()
        {
          _productName = "Community Bridge 2";
            _productNameWithVersion = _productName + " (unknown)";
            _companyName = "Community";

            var entryAssembly = Assembly.GetEntryAssembly();
            if (entryAssembly != null)
            {
                object[] attrs = entryAssembly.GetCustomAttributes(typeof(AssemblyProductAttribute), false);
                if (attrs != null && attrs.Length > 0)
                {
                    var s = ((AssemblyProductAttribute)attrs[0]).Product;
                    if (string.IsNullOrEmpty(s) == false)
                    {
                      _productName = s;
                      var v = entryAssembly.GetName().Version;
                      _productNameWithVersion = _productName + " (" + v + ")";
                    }
                }
                attrs = entryAssembly.GetCustomAttributes(typeof(AssemblyCompanyAttribute), false);
                if (attrs != null && attrs.Length > 0)
                {
                    var s = ((AssemblyCompanyAttribute)attrs[0]).Company;
                    if (string.IsNullOrEmpty(s) == false)
                        _companyName = s;
                }
            }
        }
        private UserSettings()
        {
          Scopes = "wl.signin wl.offline_access";
          ClientId = "000000004C108A13";

          BasePath = @"%AppData%\Community\CommunityBridge2";
          BasePath = Environment.ExpandEnvironmentVariables(BasePath);
          if (Directory.Exists(BasePath) == false)
          {
            Directory.CreateDirectory(BasePath);
          }

          MaxThreadCountOnFirstRetrival = 300;
        }

        public UserSettings Clone()
        {
            var u = new UserSettings();
            //u._articlePageSize = this._articlePageSize;
            u._autoMinimize = this._autoMinimize;
            u._autoStart = this._autoStart;
            u._bindToWorld = this._bindToWorld;
            //u._detailedErrorResponse = this._detailedErrorResponse;
            u._domainName = this._domainName;
            //u._listPageSize = this._listPageSize;
            u._port = this._port;
            u._usePlainTextConverter = this._usePlainTextConverter;
            //u._userEmail = this._userEmail;
            //u._userGuid = this._userGuid;
            //u._userName = this._userName;
            //u._authenticationBlob = this._authenticationBlob;
            u.Scopes = this.Scopes;
            u.ClientId = this.ClientId;
            u.RefreshToken = this.RefreshToken;

            u._autoLineWrap = this._autoLineWrap;
            u._encodingForClient = this._encodingForClient;
            u._InMimeUse = this._InMimeUse;
            //u._useAnswersForums = this._useAnswersForums;
            //u._useSocialForums = this._useSocialForums;
            u._disableUserAgentInfo = this._disableUserAgentInfo;
            u._disableLISTGROUP = this._disableLISTGROUP;
            u._showUserNamePostfix = this._showUserNamePostfix;
            u._postsAreAlwaysFormatFlowed = this._postsAreAlwaysFormatFlowed;
            u._tabAsSpace = this._tabAsSpace;
            u._addHistoryToArticle = this._addHistoryToArticle;
          u.BasePath = this.BasePath;

            u._userDefinedTags = new ArticleConverter.UserDefinedTagCollection();
            u._userDefinedTags.AddRange(this._userDefinedTags);

            u._userMappings = new ArticleConverter.UserMappingCollection();
            u._userMappings.AddRange(this._userMappings);

          u.MaxThreadCountOnFirstRetrival = MaxThreadCountOnFirstRetrival;
          u._metaInfo = this._metaInfo;
          u.ShowUsersSignature = this.ShowUsersSignature;
          u._updateInfoMode = this._updateInfoMode;
          u.MessageInfos = this.MessageInfos;
          u.SendSupersedesHeader = this.SendSupersedesHeader;

            return u;
        }

        private RegistryKey UserAppDataRegistryForWriting
        {
            get
            {
                string regPath = string.Format(CultureInfo.InvariantCulture, @"Software\{0}\{1}",
                                               _companyName, _productName);
                return Registry.CurrentUser.CreateSubKey(regPath);
            }
        }
        private RegistryKey UserAppDataRegistryForReading
        {
            get
            {
                string regPath = string.Format(CultureInfo.InvariantCulture, @"Software\{0}\{1}",
                                               _companyName, _productName);
                return Registry.CurrentUser.OpenSubKey(regPath);
            }
        }

        private static string _productName;
        public static string ProductName
        {
            get { return _productName; }
        }

        private static string _productNameWithVersion;
        public static string ProductNameWithVersion
        {
          get { return _productNameWithVersion; }
        }

        private static string _companyName;
        public static string CompanyName
        {
            get { return _companyName; }
        }

        // CurrentUser\Software\CompanyName\ProductName
        private static object syncObj = new object();
        private static UserSettings _default;
        public static UserSettings Default
        {
            get
            {
                lock (syncObj)
                {
                    if (_default == null)
                    {
                        _default = new UserSettings();
                        _default.Load();
                    }
                }
                return _default;
            }
            set
            {
                if (value == null) throw new ArgumentNullException();
                _default = value;
            }
        }

        #region Load/Save
        void Load()
        {
            try
            {
                using(var r = UserAppDataRegistryForReading)
                {
                    if (r == null) return;

                    int? i;
                    bool? b;
                    string s;

                    b = GetBoolean(r, "AutoStart");
                    if (b.HasValue)
                        AutoStart = b.Value;

                    b = GetBoolean(r, "AutoMinimize");
                    if (b.HasValue)
                        AutoMinimize = b.Value;

                    //b = GetBoolean(r, "DetailedErrorResponse");
                    //if (b.HasValue)
                    //    DetailedErrorResponse = b.Value;

                    b = GetBoolean(r, "BindToWorld");
                    if (b.HasValue)
                        BindToWorld = b.Value;

                    UsePlainTextConverters? ptc = GetEnum<UsePlainTextConverters>(r, "UsePlainTextConverterEnum");
                    if (ptc.HasValue)
                        UsePlainTextConverter = ptc.Value;

                    b = GetBoolean(r, "PostsAreAlwaysFormatFlowed");
                    if (b.HasValue)
                        PostsAreAlwaysFormatFlowed = b.Value;

                    i = GetInt32(r, "Port");
                    if (i.HasValue)
                        Port = i.Value;

                    //i = GetInt32(r, "ListPageSize");
                    //if (i.HasValue)
                    //    ListPageSize = i.Value;

                    //i = GetInt32(r, "ArticlePageSize");
                    //if (i.HasValue)
                    //    ArticlePageSize = i.Value;

                    s = GetString(r, "DomainName");
                    DomainName = s;

                    //s = GetString(r, "UserEmail");
                    //UserEmail = s;

                    //s = GetString(r, "UserName");
                    //UserName = s;

                    s = GetString(r, "RefreshToken");
                    this.RefreshToken = s;

                    s = GetString(r, "ClientId");
                    if (string.IsNullOrEmpty(s) == false)
                      this.ClientId = s;

                    s = GetString(r, "Scopes");
                    if (string.IsNullOrEmpty(s) == false)
                      this.Scopes = s;

                  //s = GetString(r, "AuthenticationBlob");
                    //AuthenticationBlob = s;

                    i = GetInt32(r, "AutoLineWrap");
                    if (i.HasValue)
                        AutoLineWrap = i.Value;

                    s = GetString(r, "EncodingForClient");
                    EncodingForClient = s;

                    //s = GetString(r, "UserGuid");
                    //if (string.IsNullOrEmpty(s) == false)
                    //{
                    //    try
                    //    {
                    //        UserGuid = new Guid(s);
                    //    }
                    //    catch { }
                    //}

                    MimeContentType? mt = GetEnum<MimeContentType>(r, "InMimeUse");
                    if (mt.HasValue)
                        InMimeUse = mt.Value;

                    //b = GetBoolean(r, "UseAnswersForums");
                    //if (b.HasValue)
                    //    UseAnswersForums = b.Value;

                    //b = GetBoolean(r, "UseSocialForums");
                    //if (b.HasValue)
                    //    UseSocialForums = b.Value;

                    ArticleConverter.UserDefinedTagCollection.PreCompileXmlSerializer();
                    s = GetString(r, "UserDefinedTags");
                    if (string.IsNullOrEmpty(s) == false)
                    {
                        var u = ArticleConverter.UserDefinedTagCollection.FromString(s);
                        if (u != null)
                            _userDefinedTags = u;
                    }

                    ArticleConverter.UserMappingCollection.PreCompileXmlSerializer();
                    s = GetString(r, "UserMappings");
                    if (string.IsNullOrEmpty(s) == false)
                    {
                        var u = ArticleConverter.UserMappingCollection.FromString(s);
                        if (u != null)
                            _userMappings = u;
                    }

                    b = GetBoolean(r, "DisableUserAgentInfo");
                    if (b.HasValue)
                        DisableUserAgentInfo = b.Value;

                    b = GetBoolean(r, "DisableLISTGROUP");
                    if (b.HasValue)
                        DisableLISTGROUP = b.Value;

                    b = GetBoolean(r, "ShowUserNamePostfix");
                    if (b.HasValue)
                        ShowUserNamePostfix = b.Value;

                    i = GetInt32(r, "TabAsSpace");
                    if (i.HasValue)
                        TabAsSpace = i.Value;

                    b = GetBoolean(r, "UseCodeColorizer");
                    if (b.HasValue)
                      UseCodeColorizer = b.Value;

                    b = GetBoolean(r, "AddHistoryToArticle");
                    if (b.HasValue)
                        AddHistoryToArticle = b.Value;

                  i = GetInt32(r, "MaxThreadCountOnFirstretrival");
                  if (i.HasValue)
                    MaxThreadCountOnFirstRetrival = i.Value;

                  MetaInfoDisplay? mt2 = GetEnum<MetaInfoDisplay>(r, "MetaInfo");
                  if (mt2.HasValue)
                    MetaInfo = mt2.Value;

                  b = GetBoolean(r, "ShowUsersSignature");
                  if (b.HasValue)
                    ShowUsersSignature = b.Value;

                  UpdateInfoModeEnum? uim = GetEnum<UpdateInfoModeEnum>(r, "UpdateInfoMode");
                  if (uim.HasValue)
                    UpdateInfoMode = uim.Value;

                  MessageInfoEnum? mie = GetEnum<MessageInfoEnum>(r, "MessageInfos");
                  if (mie.HasValue)
                    MessageInfos = mie.Value;

                  b = GetBoolean(r, "SendSupersedesHeader");
                  if (b.HasValue)
                    SendSupersedesHeader = b.Value;
                }
            }
            catch (Exception exp)
            {
                Traces.Main_TraceEvent(TraceEventType.Critical, 1, "Error loading settings from the registry: {0}", NNTPServer.Traces.ExceptionToString(exp));
            }
        }

        public void Save()
        {
          lock (this)
          {
            try
            {
              using (var r = UserAppDataRegistryForWriting)
              {
                SetBoolean(r, "AutoStart", AutoStart);

                SetBoolean(r, "AutoMinimize", AutoMinimize);

                //SetBoolean(r, "DetailedErrorResponse", DetailedErrorResponse);

                SetBoolean(r, "BindToWorld", BindToWorld);

                SetEnum(r, "UsePlainTextConverterEnum", UsePlainTextConverter);

                SetBoolean(r, "PostsAreAlwaysFormatFlowed", PostsAreAlwaysFormatFlowed);

                SetInt32(r, "Port", Port);

                //SetInt32(r, "ListPageSize", ListPageSize);

                //SetInt32(r, "ArticlePageSize", ArticlePageSize);

                SetString(r, "DomainName", DomainName);

                //SetString(r, "UserEmail", UserEmail);

                //SetString(r, "UserName", UserName);

                SetString(r, "ClientId", ClientId);
                SetString(r, "RefreshToken", RefreshToken);
                SetString(r, "Scopes", Scopes);

                //SetString(r, "AuthenticationBlob", AuthenticationBlob);

                SetInt32(r, "AutoLineWrap", AutoLineWrap);

                SetString(r, "EncodingForClient", EncodingForClient);

                //SetString(r, "UserGuid", UserGuid.ToString());

                SetEnum(r, "InMimeUse", InMimeUse);

                //SetBoolean(r, "UseAnswersForums", UseAnswersForums);

                //SetBoolean(r, "UseSocialForums", UseSocialForums);

                SetString(r, "UserDefinedTags", UserDefinedTags.GetString());

                SetString(r, "UserMappings", UserMappings.GetString());

                SetBoolean(r, "DisableUserAgentInfo", DisableUserAgentInfo);

                SetBoolean(r, "DisableLISTGROUP", DisableLISTGROUP);

                SetBoolean(r, "ShowUserNamePostfix", ShowUserNamePostfix);

                SetInt32(r, "TabAsSpace", TabAsSpace);

                SetBoolean(r, "UseCodeColorizer", UseCodeColorizer);

                SetBoolean(r, "AddHistoryToArticle", AddHistoryToArticle);

                SetInt32(r, "MaxThreadCountOnFirstretrival", MaxThreadCountOnFirstRetrival);

                SetEnum(r, "MetaInfo", MetaInfo);

                SetBoolean(r, "ShowUsersSignature", ShowUsersSignature);

                SetEnum(r, "UpdateInfoMode", UpdateInfoMode);

                SetEnum(r, "MessageInfos", MessageInfos);

                SetBoolean(r, "SendSupersedesHeader", SendSupersedesHeader);
              }
            }
            catch (Exception exp)
            {
              Traces.Main_TraceEvent(TraceEventType.Critical, 1, "Error saving settings to the registry: {0}",
                                     NNTPServer.Traces.ExceptionToString(exp));
            }
          }
        }

        #endregion

        #region Registry Helper
        bool? GetBoolean(RegistryKey key, string name)
        {
            try
            {
                var iv = GetInt32(key, name);
                if (iv.HasValue)
                {
                    if (iv.Value != 0)
                        return true;
                    return false;
                }
            }
            catch (Exception exp)
            {
                Traces.Main_TraceEvent(TraceEventType.Critical, 1, "GetBoolean: Error: {0}", NNTPServer.Traces.ExceptionToString(exp));
            }
            return null;
        }
        void SetBoolean(RegistryKey key, string name, bool value)
        {
            try
            {
                key.SetValue(name, value ? 1 : 0, RegistryValueKind.DWord);
            }
            catch (Exception exp)
            {
                Traces.Main_TraceEvent(TraceEventType.Critical, 1, "SetBoolean: Error: {0}", NNTPServer.Traces.ExceptionToString(exp));
            }
        }

        static int? GetInt32(RegistryKey key, string name)
        {
            try
            {
                var o = key.GetValue(name);
                if (o is Int32)
                    return (Int32)o;
                var ic = o as IConvertible;
                if (ic != null)
                {
                    try
                    {
                        return ic.ToInt32(System.Globalization.CultureInfo.InvariantCulture);
                    }
                    catch(Exception exp)
                    {
                        Traces.Main_TraceEvent(TraceEventType.Critical, 1, "GetInt32: Error.ToInt32: {0}", NNTPServer.Traces.ExceptionToString(exp));
                    }
                }
            }
            catch (Exception exp)
            {
                Traces.Main_TraceEvent(TraceEventType.Critical, 1, "GetInt32: Error: {0}", NNTPServer.Traces.ExceptionToString(exp));
            }
            return null;
        }
        void SetInt32(RegistryKey key, string name, int value)
        {
            try
            {
                key.SetValue(name, value, RegistryValueKind.DWord);
            }
            catch (Exception exp)
            {
                Traces.Main_TraceEvent(TraceEventType.Critical, 1, "SetInt32: Error: {0}", NNTPServer.Traces.ExceptionToString(exp));
            }
        }

        string GetString(RegistryKey key, string name)
        {
            try
            {
                var o = key.GetValue(name);
                if (o is string)
                    return (string)o;
                var ic = o as IConvertible;
                if (ic != null)
                {
                    try
                    {
                        return ic.ToString(System.Globalization.CultureInfo.InvariantCulture);
                    }
                    catch(Exception exp)
                    {
                        Traces.Main_TraceEvent(TraceEventType.Critical, 1, "GetString.ToString: Error: {0}", NNTPServer.Traces.ExceptionToString(exp));
                    }
                }
                if (o != null)
                    return o.ToString();
            }
            catch (Exception exp)
            {
                Traces.Main_TraceEvent(TraceEventType.Critical, 1, "GetString: Error: {0}", NNTPServer.Traces.ExceptionToString(exp));
            }
            return null;
        }
        void SetString(RegistryKey key, string name, string value)
        {
            key.SetValue(name, value ?? string.Empty, RegistryValueKind.String);
        }

        void SetEnum(RegistryKey key, string name, Enum value)
        {
            int iVal = 0;
            try
            {
                var ic = value as IConvertible;
                if (ic != null)
                    iVal = ic.ToInt32(System.Globalization.CultureInfo.InvariantCulture);
                else
                    throw new NotSupportedException("Could not convert enum to int");
            }
            catch (Exception exp)
            {
                Traces.Main_TraceEvent(TraceEventType.Critical, 1, "SetEnum {0}: Error: {1}", value, NNTPServer.Traces.ExceptionToString(exp));
                return;
            }
            SetInt32(key, name, iVal);
        }
        Nullable<T> GetEnum<T>(RegistryKey key, string name) where T : struct
        {
            int? iVal = GetInt32(key, name);
            if (iVal.HasValue)
            {
                if (Enum.IsDefined(typeof(T), iVal))
                {
                    return (T)Enum.ToObject(typeof(T), iVal);
                }
            }
            return null;
        }
        #endregion

        #region Setting Properties

        private bool _autoStart = true;
        [Category("General")]
        [DefaultValue(true)]
        [Description("If this is true, the NNTP server will automatically start after you have started the application")]
        public bool AutoStart
        {
            get { return _autoStart; }
            set { _autoStart = value; }
        }

        private bool _autoMinimize = false;
        [Category("General")]
        [DefaultValue(false)]
        [Description("If this is true, the application will automatically minimized after the NNTP server was automatically started")]
        public bool AutoMinimize
        {
            get { return _autoMinimize; }
            set { _autoMinimize = value; }
        }

        private int _port = 120;
        [Category("NNTP-Server")]
        [DefaultValue(119)]
        [Description("The port on which the NNTP server should be listen. Normal: 119")]
        public int Port
        {
            get { return _port; }
            set
            {
                if (value > 0)
                    _port = value;
            }
        }
      
        private string _domainName = "communitybridge2.codeplex.com";
        [Category("Header-Converter")]
        [DefaultValue("communitybridge2.codeplex.com")]
        [Description("This is the domain name which will be added to the messageId. This domain name will only be visible to your client and will never go to the web forums")]
        public string DomainName
        {
            get
            {
                return _domainName;
            }
            set
            {
                _domainName = value;
                if ((string.IsNullOrEmpty(_domainName)) || (_domainName.Trim().Length <= 0))
                    _domainName = "communitybridge2.codeplex.com";
            }
        }

        private bool _bindToWorld = false;
        [Category("NNTP-Server")]
        [DefaultValue(false)]
        [Description("If this is true, the NNTP server will listen to 0.0.0.0, so it can be accessed from any other computer in your network. Otherwise it will only listen to apps on the local computer (default for security reasons)")]
        public bool BindToWorld
        {
            get {
                return _bindToWorld;
            }
            set {
                _bindToWorld = value;
            }
        }

        private UsePlainTextConverters _usePlainTextConverter = UsePlainTextConverters.None;
        [Category("PlainText-Converter")]
        [DefaultValue(UsePlainTextConverters.None)]
        [Description("Specifies if the 'PlainText-Converter' should be used; for most clients this is recommended")]
        public UsePlainTextConverters UsePlainTextConverter
        {
            get
            {
                return _usePlainTextConverter;
            }
            set
            {
                _usePlainTextConverter = value;
            }
        }

        [Category("LiveId")]
        [DefaultValue("wl.signin wl.offline_access")]
        [Description("This represents the 'Scopes' which must be requested for the authentication, separated by space")]
        public string Scopes { get; set; }

        [Category("LiveId")]
        [DefaultValue("000000004C0F8E22")]  // TODO: Replace with MSDN-ClientID!
        [Description("This represents the 'ClientID' which must be requested for the authentication")]
        public string ClientId { get; set; }

        [Category("LiveId")]
        [DefaultValue(null)]
        [Description("This is used to store the 'RefreshToken' for automatic login")]
        public string RefreshToken { get; set; }

        //string _authenticationBlob;
        //[Category("LiveId")]
        //[DefaultValue("")]
        //[Description("This is the string which will be used to do the auto login fpr LiveId")]
        //public string AuthenticationBlob
        //{
        //    get
        //    {
        //        return _authenticationBlob;
        //    }
        //    set
        //    {
        //        _authenticationBlob = value;
        //    }
        //}

        int _autoLineWrap = 0;
        [Category("PlainText-Converter")]
        [Description("A value of 0 means that auto line wrapping is disabled. A greater value will wrap the received lines from the web-service after the specified number of chars.")]
        [DefaultValue(0)]
        public int AutoLineWrap
        {
            get
            {
                return _autoLineWrap;
            }
            set
            {
                _autoLineWrap = value;
            }
        }

        private string _encodingForClient;
        [DefaultValue("utf-8")]
        [TypeConverter(typeof(MyEncodingConverter))]
        [Category("Messages")]
        [Description("Here you can set the encoding in whioch the articles should be sent to your newsreader. Normally it should be 'utf-8' but you can switch it to some other encoding for client which do not understand utf-8 (like Agent)")]
        public string EncodingForClient
        {
            get
            {
                return _encodingForClient;
;
            }
            set
            {
                _encodingForClient = value;
            }
        }

        internal Encoding EncodingForClientEncoding
        {
            get
            {
                try
                {
                    if (string.IsNullOrEmpty(_encodingForClient))
                        return Encoding.UTF8;
                    return Encoding.GetEncoding(_encodingForClient);
                }
                catch (Exception exp)
                {
                    Traces.Main_TraceEvent(TraceEventType.Error, 1, "Could not convert encoding {0}: {1}", _encodingForClient, NNTPServer.Traces.ExceptionToString(exp));
                }
                return Encoding.UTF8;
            }
        }

        public enum MimeContentType {
            TextHtml,
            TextPlain,
        }

        private MimeContentType _InMimeUse = MimeContentType.TextHtml;
        [Category("Messages")]
        [Description("If the newsclient is senting the post in MIME-multipart, this setting decides which part of the mime message is taken. Either text/plain (which can optional converted via the text converter) or text/html (which will directly go to the web-service)")]
        [DefaultValue(MimeContentType.TextHtml)]
        public MimeContentType InMimeUse
        {
            get { return _InMimeUse; }
            set { _InMimeUse = value; }
        }

        private bool _disableUserAgentInfo;
        [Category("General")]
        [DefaultValue(false)]
        [Description("If this is true, the info of the user-agent is not appended to the posted article")]
        public bool DisableUserAgentInfo
        {
            get
            {
                return _disableUserAgentInfo;
            }
            set
            {
                _disableUserAgentInfo = value;
            }
        }

        private bool _disableLISTGROUP;
        [Category("NNTP-Server")]
        [DefaultValue(false)]
        [Description("If this is true, the NNTP command 'LISTGROUP' will be disabled and it will return '501 syntax error'")]
        public bool DisableLISTGROUP
        {
            get
            {
                return _disableLISTGROUP;
            }
            set
            {
                _disableLISTGROUP = value;
            }
        }

        private ArticleConverter.UserDefinedTagCollection _userDefinedTags = new ArticleConverter.UserDefinedTagCollection();
        [Category("PlainText-Converter")]
        public ArticleConverter.UserDefinedTagCollection UserDefinedTags
        {
            get
            {
                return _userDefinedTags;
            }
        }

        private bool _postsAreAlwaysFormatFlowed = true;
        [Category("PlainText-Converter")]
        [DefaultValue(true)]
        [Description("If this is true, all lines which ends with a space are converted into a soft-linebreak (no linebreak), if it is false, only those posting will be converted, which has Content-Type=format=flowed set")]
        public bool PostsAreAlwaysFormatFlowed
        {
            get { return _postsAreAlwaysFormatFlowed; }
            set { _postsAreAlwaysFormatFlowed = value; }
        }


        private ArticleConverter.UserMappingCollection _userMappings = new ArticleConverter.UserMappingCollection();
        [Category("Header-Converter")]
        [Description("This table will be automatically generated if a posting was sent. It will be used to display the sent email address while receiving posts from the web serivce")]
        public ArticleConverter.UserMappingCollection UserMappings
        {
            get
            {
                return _userMappings;
            }
        }

        private bool _showUserNamePostfix = true;
        [Category("Header-Converter")]
        [DefaultValue(true)]
        [Description("If this is true, the name MVP, MSFT, ADMIN will be appended in the username (if not already in the username)")]
        public bool ShowUserNamePostfix
        {
            get { return _showUserNamePostfix; }
            set { _showUserNamePostfix = value; }
        }

        private int _tabAsSpace = 4;
        [Category("PlainText-Converter")]
        [DefaultValue(4)]
        [Description("If this value is > 0, it will replace all tabs with the number of spaces specified")]
        public int TabAsSpace
        {
            get { return _tabAsSpace; }
            set { _tabAsSpace = value; }
        }

        private bool _useCodeColorizer = true;
        [Category("PlainText-Converter")]
        [DefaultValue(true)]
        [Description("If this is true, the code colorizer will be used inside the plaintext converter")]
        public bool UseCodeColorizer
        {
            get { return _useCodeColorizer; }
            set { _useCodeColorizer = value; }
        }

        private bool _addHistoryToArticle = true;
        [Category("Messages")]
        [DefaultValue(true)]
        [Description("If this is true, possible 'history' entries will be added to the article signature")]
        public bool AddHistoryToArticle
        {
            get { return _addHistoryToArticle; }
            set { _addHistoryToArticle = value; }
        }

        private bool _disableArticleCache = false;
        [Category("General")]
        [DefaultValue(false)]
        [Description("If this is true, all articles will be retrived from the web-service. No article will be cached in the bridge! This will increase the time to get an article, depending on the way the newsreader requests the article.")]
        public bool DisableArticleCache
        {
            get { return _disableArticleCache; }
            set { _disableArticleCache = value; }
        }

        #endregion

      [Category("General")]
        public string BasePath { get; private set; }

      [Category("Messages")]
      public int MaxThreadCountOnFirstRetrival { get; set; }

      public enum MetaInfoDisplay
      {
        None,
        InSubject,
        InSignature,
        InSubjectAndSignature,
      }

      private MetaInfoDisplay _metaInfo = MetaInfoDisplay.InSignature;

      [DefaultValue(MetaInfoDisplay.InSignature)]
      [Category("Messages")]
      public MetaInfoDisplay MetaInfo
      {
        get { return _metaInfo; }
        set { _metaInfo = value; }
      }

      [DefaultValue(false)]
      [Category("Messages")]
      public bool ShowUsersSignature { get; set; }


      private bool _useFileStorage = true;

      [DefaultValue(true)]
      [Category("General")]
      public bool UseFileStorage
      {
        get { return _useFileStorage; }
        set { _useFileStorage = value; }
      }

      private UpdateInfoModeEnum _updateInfoMode = UpdateInfoModeEnum.BasedOnLastReply;
      [Category("Messages")]
      public UpdateInfoModeEnum UpdateInfoMode
      {
        get { return _updateInfoMode; }
        set { _updateInfoMode = value; }
      }

      public enum UpdateInfoModeEnum
      {
        /// <summary>
        /// Default behavior as the old Answers-Bridge
        /// </summary>
        BasedOnLastReply,
        /// <summary>
        /// In addition to the first, it also checks for obsolete messages (deleted, moved, merged)
        /// </summary>
        BasedOnLastReplyAndObsoleteThreads,
        /// <summary>
        /// It checks for all modifications like, MarkAnswer, Edited, a.s.o.
        /// </summary>
        BasedOnLastActivity,
      }

      [Category("Messages")]
      public bool SendSupersedesHeader { get; set; }

      public enum MessageInfoEnum
      {
        EndOfSubjectAndSignature,
        BeginOfBody,
        InSignature,
        None,
        EndOfSubject,
      }

      private MessageInfoEnum _messageInfos = MessageInfoEnum.EndOfSubjectAndSignature;
      [Category("Messages")]
      public MessageInfoEnum MessageInfos
      {
        get { return _messageInfos; }
        set { _messageInfos = value; }
      }


    }  // class UserSettings


    #region MyEncodingConverter
    class MyEncodingConverter : TypeConverter
    {
        private EncodingInfo[] _encodings;
        private StandardValuesCollection _stdValues;

        public override bool GetStandardValuesSupported(ITypeDescriptorContext context)
        {
            return true;
        }
        public override StandardValuesCollection GetStandardValues(ITypeDescriptorContext context)
        {
            try
            {
                if (_encodings == null)
                    _encodings = Encoding.GetEncodings();
                List<string> il = new List<string>();
                foreach (var e in _encodings)
                {
                    if (e.GetEncoding().IsMailNewsDisplay)
                        il.Add(e.Name);
                }
                _stdValues = new StandardValuesCollection(il.OrderBy(p => p).ToList());
            }
            catch { }
            return _stdValues;
        }

        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
        {
            if (sourceType == typeof(string))
            {
                return true;
            }
            return base.CanConvertFrom(context, sourceType);
        }
        // Overrides the ConvertFrom method of TypeConverter.
        public override object ConvertFrom(ITypeDescriptorContext context,
           CultureInfo culture, object value)
        {
            if (value == null)
                return null;
            if (value is string)
            {
                return value as string;
            }
            return base.ConvertFrom(context, culture, value);
        }
        // Overrides the ConvertTo method of TypeConverter.
        public override object ConvertTo(ITypeDescriptorContext context,
           CultureInfo culture, object value, Type destinationType)
        {
            if (value == null) return string.Empty;
            if (destinationType == typeof(string))
            {
                return value.ToString();
            }
            return base.ConvertTo(context, culture, value, destinationType);
        }
    }  // MyEncodingConverter
    #endregion
}
