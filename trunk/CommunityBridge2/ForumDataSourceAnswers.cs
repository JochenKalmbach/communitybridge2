using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;
using CommunityBridge2.ArticleConverter;
using CommunityBridge2.NNTPServer;
using System.Text.RegularExpressions;
using System.Text;
using CommunityBridge2.WebServiceAnswers;
using Microsoft.Support.Community.DataLayer.Entity;
using Forum = Microsoft.Support.Community.DataLayer.Entity.Forum;
using Thread = Microsoft.Support.Community.DataLayer.Entity.Thread;

namespace CommunityBridge2.Answers
{
  //    // RFCs:
  //    // http://tools.ietf.org/html/rfc850 Standard for Interchange of USENET Messages
  //    // http://tools.ietf.org/html/rfc977
  //    // http://tools.ietf.org/html/rfc2980
  //    // http://tools.ietf.org/html/rfc3977
  //    // http://tools.ietf.org/html/rfc2076

  internal class ForumDataSource : DataProvider
  {
    private readonly QnAClient _serviceProviders;

    public ForumDataSource(QnAClient serviceProviders, string domainName)
    {
      _serviceProviders = serviceProviders;
      _domainName = domainName;
      _management = new MsgNumberManagement(UserSettings.Default.BasePath, _domainName, UserSettings.Default.UseFileStorage);
    }

    public Encoding HeaderEncoding = Encoding.UTF8;

    private readonly string _domainName;

    private readonly MsgNumberManagement _management;

    #region DataProvider-Implenentation

    public IList<NNTPServer.Newsgroup> PrefetchNewsgroupList(Action<Newsgroup> stateCallback)
    {
      LoadNewsgroupsToStream(stateCallback);
      return GroupList.Values.ToList();
    }

    public void ClearCache()
    {
      lock (GroupList)
      {
        GroupList.Clear();
      }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <returns>Returns <c>true</c> if now exception was thrown while processing the request</returns>
    /// <remarks>
    /// It might happen that this function is called twice!
    /// For example if you are currently reading the newsgrouplist and then a client is trying to read articles from a subscribed newsgroup...
    /// </remarks>
    protected override bool LoadNewsgroupsToStream(Action<Newsgroup> groupAction)
    {
      bool res = true;
      lock (this)
      {
        if (IsNewsgroupCacheValid())
        {
          // copy the list to a local list, so we do not need the lock for the callback
          List<Newsgroup> localGroups;
          lock (GroupList)
          {
            localGroups = new List<Newsgroup>(GroupList.Values);
          }
          if (groupAction != null)
          {
            foreach (var g in localGroups)
              groupAction(g);
          }
          return true;
        }

        // INFO: Always return every group...
        //var internalList = new List<ForumNewsgroup>();
        var provider = _serviceProviders;
        try
        {
          string[] locales = provider.GetSupportedLocales();
          //string[] locales = new[] {"en-us"};

          foreach (var loc in locales)
          {
            try
            {
              // Load the list of all newsgroups:
              var localProvider = provider;
              var forums = localProvider.GetForumList(loc);

              foreach (var f in forums.Items)
              {
                //if (string.Equals(f.ShortName, "windows", StringComparison.OrdinalIgnoreCase) == false) continue;
                // Build different newsgroups depending on the meta-data
                var uniqueInfos = _management.LoadAllMetaData(localProvider, f.Id, loc, ForumNewsgroup.GetForumName(f), true);
                var g = new ForumNewsgroup(f, localProvider, null, uniqueInfos);
                _management.SaveAllMetaData(g);
                _management.SaveGroupFilterData(g);

                // Update the Msg# from the local mapping database: because this group might already been available in the _groupList...
                lock(GroupList)
                {
                  ForumNewsgroup existingGroup = null;
                  if (GroupList.ContainsKey(g.GroupName))
                  {
                    existingGroup = GroupList[g.GroupName] as ForumNewsgroup;
                  }
                  if (existingGroup != null)
                  {
                    g = existingGroup; // use the existing group in order to prevent problems with the database
                  }
                  else
                  {
                    GroupList.Add(g.GroupName, g);
                  }
                }
                _management.GetMaxMessageNumber(g);

                if (groupAction != null)
                  groupAction(g);

                foreach (var mdI in uniqueInfos)
                {
                  var g2 = new ForumNewsgroup(f, localProvider, mdI, uniqueInfos);
                  _management.SaveGroupFilterData(g2);

                  lock (GroupList)
                  {
                    ForumNewsgroup existingGroup = null;
                    if (GroupList.ContainsKey(g2.GroupName))
                    {
                      existingGroup = GroupList[g2.GroupName] as ForumNewsgroup;
                    }
                    if (existingGroup != null)
                    {
                      g2 = existingGroup; // use the existing group in order to prevent problems with the database
                    }
                    else
                    {
                      GroupList.Add(g2.GroupName, g2);
                    }
                  }

                  // Update the Msg# from the local mapping database:
                  _management.GetMaxMessageNumber(g2);
                  //internalList.Add(g2);
                  if (groupAction != null)
                    groupAction(g2);
                }
              }
            }
            catch (Exception exp2)
            {
              res = false;
              Traces.Main_TraceEvent(TraceEventType.Error, 1,
                                     "Error during LoadNewsgroupsToStream (GetForumList) ({1}: {0}",
                                     NNTPServer.Traces.ExceptionToString(exp2), loc);
            }
          }
          Debug.WriteLine("Finished downloading of forums.");
        }
        catch (Exception exp)
        {
          res = false;
          Traces.Main_TraceEvent(TraceEventType.Error, 1,
                                 "Error during LoadNewsgroupsToStream (GetSupportedLocales): {0}",
                                 NNTPServer.Traces.ExceptionToString(exp));
        }

        //lock (GroupList)
        //{
        //  foreach (var g in internalList)
        //  {
        //    if (GroupList.ContainsKey(g.GroupName) == false)
        //      GroupList.Add(g.GroupName, g);
        //  }
        //}

        if (res)
          SetNewsgroupCacheValid();
      } // lock

      return res;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <remarks>
    /// It might happen that this function is called twice!
    /// For example if you are currently reading the newsgrouplist and then a client is trying to read articles from a subscribed newsgroup...
    /// </remarks>
    public override bool GetNewsgroupListFromDate(string clientUsername, DateTime fromDate,
                                                  Action<Newsgroup> groupAction)
    {
      //// For now, we just return the whole list; I have not stored the group-data in a database...
      //return GetNewsgroupListToStream(clientUsername, groupAction);
      // Just return! We do not support this currently...
      return true;
    }

    // 
    /// <summary>
    /// 
    /// </summary>
    /// <param name="clientUsername"></param>
    /// <param name="groupName"></param>
    /// <param name="updateFirstLastNumber">If this is <c>true</c>, then always get the newgroup info from the sever; 
    /// so we have always the corrent NNTPMaxNumber!</param>
    /// <returns></returns>
    public override Newsgroup GetNewsgroup(string clientUsername, string groupName, bool updateFirstLastNumber, out bool exceptionOccured)
    {
      exceptionOccured = false;
      // 1st Part: Brand (Answers)
      // 2nt Part: Locale (e.g. en-us)
      // 3rd Part: Groupname (shortname)
      string[] groupParts = groupName.Trim().Split('.');
      if (groupParts.Length <= 2)
      {
        Traces.Main_TraceEvent(TraceEventType.Verbose, 1, "GetNewsgroup failed (invalid groupname): {0}", groupName);
        return null;
      }

      // First try to find the group (ServiceProvider) in the cache...
      QnAClient provider = _serviceProviders;
      ForumNewsgroup cachedGroup = null;
      lock (GroupList)
      {
        if (GroupList.ContainsKey(groupName))
        {
          cachedGroup = GroupList[groupName] as ForumNewsgroup;
          if ((cachedGroup != null) && (cachedGroup.Provider != null))
            provider = cachedGroup.Provider;
        }
      }

      // If we just need the group without actual data, then return the cached group
      if ((updateFirstLastNumber == false) && (cachedGroup != null))
        return cachedGroup;

      if (cachedGroup == null)
      {
        // Try to find the group from the web service
        try
        {
          string locale = groupParts[1];
          // We need to query the web-service for the supported groups with this locale...
          var forums = provider.GetForumList(locale);
          // Check if the groupname is supported for this locale
          if ((forums != null) && (forums.Items != null))
          {
            foreach (var f in forums.Items)
            {
              if (string.Equals(f.ShortName, groupParts[2], StringComparison.OrdinalIgnoreCase))
              {
                lock (GroupList)
                {
                  if (GroupList.ContainsKey(groupName) == false)
                  {
                    string baseGroupName = string.Join(".", new [] {groupParts[0], groupParts[1], groupParts[2]});

                    var unqiueMd = _management.LoadAllMetaData(provider, f.Id, locale, ForumNewsgroup.GetForumName(f));

                    var md = _management.LoadGroupFilterData(baseGroupName, groupName);
                    cachedGroup = new ForumNewsgroup(f, provider, md, unqiueMd);
                    if (string.Equals(cachedGroup.GroupName, groupName, StringComparison.OrdinalIgnoreCase) == false)
                    {
                      MainWindow.NewsgroupUpdateInfo(groupName,
                                                     "Metadata is missing for this group; please re-download all groups (Prefetch newsgroup list)!");
                      Traces.Main_TraceEvent(TraceEventType.Error, 1, 
                          "Metadata is missing for this group '{0}'; please re-download all groups (LIST-command)!",
                          groupName);
                      return null;
                    }
                    GroupList.Add(groupName, cachedGroup);
                  }
                  else
                  {
                    // If the group is now in the cache, then use this group!
                    cachedGroup = (ForumNewsgroup) GroupList[groupName]; // this cast must match
                  }
                }
                break;
              }
            }
          }
        }
        catch (Exception exp)
        {
          exceptionOccured = true;
          Traces.Main_TraceEvent(TraceEventType.Verbose, 1, "GetNewsgroup failed (invalid locale; GetForumList): {0}, {1}",
                                 groupName, NNTPServer.Traces.ExceptionToString(exp));
          return null;
        }
      }

      if (cachedGroup == null)
      {
        // Group not found...
        Traces.Main_TraceEvent(TraceEventType.Verbose, 1,
                               "GetNewsgroup failed (invalid groupname; cachedGroup==null): {0}", groupName);
        return null;
      }

      if (updateFirstLastNumber)
      {
        var articles = _management.UpdateGroupFromWebService(cachedGroup, provider, OnProgressData);
        if (articles != null)
        {
          if (UserSettings.Default.DisableArticleCache == false)
          {
            foreach (var a in articles)
            {
              ConvertNewArticleFromWebService(a);
              lock (cachedGroup.Articles)
              {
                cachedGroup.Articles[a.Number] = a;
              }
            }
          }
        }
      }

      return cachedGroup;
    }

    public override Newsgroup GetNewsgroupFromCacheOrServer(string groupName)
    {
      if (string.IsNullOrEmpty(groupName))
        return null;
      groupName = groupName.Trim();
      ForumNewsgroup res = null;
      lock (GroupList)
      {
        if (GroupList.ContainsKey(groupName))
          res = GroupList[groupName] as ForumNewsgroup;
      }
      if (res == null)
      {
        bool exceptionOccured;
        res = GetNewsgroup(null, groupName, false, out exceptionOccured) as ForumNewsgroup;
        lock (GroupList)
        {
          if (GroupList.ContainsKey(groupName) == false)
            GroupList[groupName] = res;
        }
      }

      return res;
    }

    public override Article GetArticleById(string clientUsername, string groupName, string articleId)
    {
      var g = GetNewsgroupFromCacheOrServer(groupName) as ForumNewsgroup;
      return GetArticleById(g, articleId);
    }

    private ForumArticle GetArticleById(ForumNewsgroup g, string articleId)
    {
      if (g == null)
        throw new ApplicationException("No group provided");

      DateTime? activityDateUtc;
      Guid? id = ForumArticle.IdToGuid(articleId, out activityDateUtc);
      if (id == null) return null;

      return GetArticleByIdInternal(g, id.Value, activityDateUtc);
    }

    private ForumArticle GetArticleByIdInternal(ForumNewsgroup g, Guid id, DateTime? activityDateUtc, bool mustUseCorrectMsgNo = true)
    {
      if (g == null)
      {
        return null;
      }

      if (UserSettings.Default.DisableArticleCache == false)
      {
        // Check if the article is in my cache...
        lock (g.Articles)
        {
          foreach (var ar in g.Articles.Values)
          {
            var fa = ar as ForumArticle;
            if ((fa != null) && (fa.Guid == id))
              return fa;
          }
        }
      }

      var a = _management.GetMessageById(g, id, activityDateUtc, _serviceProviders, mustUseCorrectMsgNo);
      if (a == null) return null;

      ConvertNewArticleFromWebService(a);

      if (mustUseCorrectMsgNo)
      {
        // Only store the message if the Msg# is correct!
        if (UserSettings.Default.DisableArticleCache == false)
        {
          lock (g.Articles)
          {
            g.Articles[a.Number] = a;
          }
        }
      }
      return a;
    }

    #region IArticleConverter

    public UsePlainTextConverters UsePlainTextConverter
    {
      get { return _converter.UsePlainTextConverter; }
      set { _converter.UsePlainTextConverter = value; }
    }

    public ArticleConverter.UserDefinedTagCollection UserDefinedTags
    {
      set { _converter.UserDefinedTags = value; }
    }

    public int AutoLineWrap
    {
      get { return _converter.AutoLineWrap; }
      set { _converter.AutoLineWrap = value; }
    }

    public bool ShowUserNamePostfix
    {
      get { return _converter.ShowUserNamePostfix; }
      set { _converter.ShowUserNamePostfix = value; }
    }

    public bool PostsAreAlwaysFormatFlowed
    {
      get { return _converter.PostsAreAlwaysFormatFlowed; }
      set { _converter.PostsAreAlwaysFormatFlowed = value; }
    }

    public int TabAsSpace
    {
      get { return _converter.TabAsSpace; }
      set { _converter.TabAsSpace = value; }
    }

    public bool UseCodeColorizer
    {
      get { return _converter.UseCodeColorizer; }
      set { _converter.UseCodeColorizer = value; }
    }

    public bool AddHistoryToArticle { get; set; }

    private readonly ArticleConverter.Converter _converter = new ArticleConverter.Converter();

    private void ConvertNewArticleFromWebService(Article a)
    {
      try
      {
        _converter.NewArticleFromWebService(a, HeaderEncoding);
      }
      catch (Exception exp)
      {
        Traces.Main_TraceEvent(TraceEventType.Error, 1, "ConvertNewArticleFromWebService failed: {0}",
                               NNTPServer.Traces.ExceptionToString(exp));
      }
    }

    private void ConvertNewArticleFromNewsClientToWebService(Article a)
    {
      try
      {
        _converter.NewArticleFromClient(a);
      }
      catch (Exception exp)
      {
        Traces.Main_TraceEvent(TraceEventType.Error, 1, "ConvertNewArticleFromNewsClientToWebService failed: {0}",
                               NNTPServer.Traces.ExceptionToString(exp));
      }
    }

    #endregion

    public override Article GetArticleByNumber(string clientUsername, string groupName, int articleNumber)
    {
      var g = GetNewsgroupFromCacheOrServer(groupName) as ForumNewsgroup;
      if (g == null) return null;
      if (UserSettings.Default.DisableArticleCache == false)
      {
        lock (g.Articles)
        {
          if (g.Articles.ContainsKey(articleNumber))
            return g.Articles[articleNumber];
        }
      }

      var a = _management.GetMessageByMsgNo(g, articleNumber, _serviceProviders);
      if (a == null) return null;

      ConvertNewArticleFromWebService(a);

      if (UserSettings.Default.DisableArticleCache == false)
      {
        lock (g.Articles)
        {
          g.Articles[a.Number] = a;
        }
      }
      return a;
    }

    public override void GetArticlesByNumberToStream(string clientUsername, string groupName, int firstArticle,
                                                     int lastArticle, Action<IList<Article>> articlesProgressAction)
    {
      // Check if the number has the correct order... some clients may sent it XOVER 234-230 instead of "XOVER 230-234"
      if (firstArticle > lastArticle)
      {
        // the numbers are in the wrong oder, so correct it...
        var tmp = firstArticle;
        firstArticle = lastArticle;
        lastArticle = tmp;
      }

      ForumNewsgroup g;
      try
      {
        g = GetNewsgroupFromCacheOrServer(groupName) as ForumNewsgroup;
        if (g == null) return;

        lock (g)
        {
          if (g.ArticlesAvailable == false)
          {
            // If we never had checked for acrticles, we first need to do this...
            var articles = _management.UpdateGroupFromWebService(g, _serviceProviders, OnProgressData);
            if (articles != null)
            {
              if (UserSettings.Default.DisableArticleCache == false)
              {
                foreach (var a in articles)
                {
                  ConvertNewArticleFromWebService(a);
                  lock (g.Articles)
                  {
                    g.Articles[a.Number] = a;
                  }
                }
              }
            }
          }
        }
      }
      catch (Exception exp)
      {
        Traces.Main_TraceEvent(TraceEventType.Error, 1, NNTPServer.Traces.ExceptionToString(exp));
        return;
      }

      // Be sure we do not ask too much...
      if (firstArticle < g.FirstArticle)
        firstArticle = g.FirstArticle;
      if (lastArticle > g.LastArticle)
        lastArticle = g.LastArticle;

      for (int no = firstArticle; no <= lastArticle; no++)
      {
        Article a = null;
        if (UserSettings.Default.DisableArticleCache == false)
        {
          lock (g.Articles)
          {
            if (g.Articles.ContainsKey(no))
              a = g.Articles[no];
          }
        }
        if (a == null)
        {
          Article res = null;
          // INFO: Currently it is not possible to detect if there is an web-service error, or if the article could not be retrived...
          //try
          //{
          // Some internal events get catched, like "User is not anllowed to..."
            res = _management.GetMessageByMsgNo(g, no, _serviceProviders);
          //}
          //catch (Exception exp2)
          //{
          //  Traces.Main_TraceEvent(TraceEventType.Error, 1, NNTPServer.Traces.ExceptionToString(exp2));
          //}
          if (res != null)
          {
            a = res;
            ConvertNewArticleFromWebService(a);
            if (UserSettings.Default.DisableArticleCache == false)
            {
              lock (g.Articles)
              {
                if (g.Articles.ContainsKey(no) == false)
                  g.Articles[no] = a;
              }
            }
          }
        }
        if (a != null)
          articlesProgressAction(new [] {a});
      }
    }

    private static readonly Regex RemoveUnusedhtmlStuffRegex = new Regex(".*<body[^>]*>\r*\n*(.*)\r*\n*</\\s*body>", 
            RegexOptions.CultureInvariant | RegexOptions.Compiled | RegexOptions.Singleline | RegexOptions.IgnoreCase);
        private static string RemoveUnsuedHtmlStuff(string text)
        {
            var m = RemoveUnusedhtmlStuffRegex.Match(text);
            if (m.Success)
            {
                return m.Groups[1].Value;
            }
            return text;
        }

        protected override void SaveArticles(string clientUsername, List<Article> articles)
        {
            foreach (var a in articles)
            {
                var g = GetNewsgroupFromCacheOrServer(a.ParentNewsgroup) as ForumNewsgroup;
                if (g == null)
                    throw new ApplicationException("Newsgroup not found!");

                ConvertNewArticleFromNewsClientToWebService(a);

                if (a.ContentType.IndexOf("text/html", StringComparison.InvariantCultureIgnoreCase) >= 0)
                {
                    a.Body = RemoveUnsuedHtmlStuff(a.Body);
                }
                else //if (a.ContentType.IndexOf("text/plain", StringComparison.InvariantCultureIgnoreCase) >= 0)
                {
                    // It seems to be plain text, so convert it to "html"...
                    a.Body = a.Body.Replace("\r", string.Empty);
                    a.Body = System.Web.HttpUtility.HtmlEncode(a.Body);
                    a.Body = a.Body.Replace("\n", "<br />");
                }

                if ((UserSettings.Default.DisableUserAgentInfo == false) && (string.IsNullOrEmpty(a.Body) == false))
                {
                    a.Body = a.Body + string.Format("<a name=\"{0}_CommunityBridge\" title=\"{1} via {2}\" />", Guid.NewGuid().ToString(), a.UserAgent, Article.MyXNewsreaderString);
                }

                // Check if this is a new post or a reply:
                Guid myThreadGuid;
                if (string.IsNullOrEmpty(a.References))
                {
                  Traces.WebService_TraceEvent(TraceEventType.Verbose, 1, "CreateQuestionThread: ForumId: {0}, Subject: {1}, Content: {2}", g.ForumId, a.Subject, a.Body);
                  // INFO: This is not suppotred!
                  throw new ApplicationException("Creating new threads is not supported!");
                }
                else
                {
                  // FIrst get the parent Message, so we can retrive the discussionId (threadId)
                  // retrive the last reference:
                  string[] refes = a.References.Split(' ');
                  var res = GetArticleById(g, refes[refes.Length - 1].Trim());
                  if (res == null)
                    throw new ApplicationException("Parent message not found!");

                  Traces.WebService_TraceEvent(TraceEventType.Verbose, 1, "CreateReply: ForumId: {0}, DiscussionId: {1}, ThreadId: {2}, Content: {3}", g.ForumId, res.DiscussionId, res.Guid, a.Body);

                  myThreadGuid = _serviceProviders.AddMessage(res.DiscussionId, res.Guid, a.Body);
                }

                // Auto detect my email and username (guid):
                try
                {
                    // Try to find the email address in the post:
                    var m = emailFinderRegEx.Match(a.From);
                    if (m.Success)
                    {
                      string userName = m.Groups[1].Value.Trim(' ', '<', '>');
                        string email = m.Groups[3].Value;

                        // try to find this email in the usermapping collection:
                        bool bFound = false;
                        lock (UserSettings.Default.UserMappings)
                        {
                            foreach (var um in UserSettings.Default.UserMappings)
                            {
                                if (string.Equals(um.UserEmail, email, 
                                    StringComparison.InvariantCultureIgnoreCase))
                                {
                                    // Address is already known...
                                    bFound = true;
                                    break;
                                }
                            }
                        }
                        if (bFound == false)
                        {
                            // I have not yet this email address, so find the user guid for the just posted article:
                          // INFO: The article is not yet in the cache, so we have no Msg#!
                            var a2 = GetArticleByIdInternal(g, myThreadGuid, null, false);
                            if (a2 != null)
                            {
                                var userGuid = a2.UserGuid;
                                // Now store the data in the user settings
                                bool bGuidFound = false;
                                lock (UserSettings.Default.UserMappings)
                                {
                                    foreach (var um in UserSettings.Default.UserMappings)
                                    {
                                        if (um.Id == userGuid)
                                        {
                                            bGuidFound = true;
                                            um.UserEmail = email;
                                        }
                                    }
                                    if (bGuidFound == false)
                                    {
                                      var um = new UserMapping();
                                      um.Id = userGuid;
                                      um.UserEmail = email;
                                      if ((string.IsNullOrEmpty(a2.DisplayName) == false) && (a2.DisplayName.Contains("<null>") == false))
                                        um.UserName = a2.DisplayName;
                                      else
                                      {
                                        if (string.IsNullOrEmpty(userName) == false)
                                          um.UserName = userName;
                                      }
                                      if (string.IsNullOrEmpty(um.UserName) == false)
                                        UserSettings.Default.UserMappings.Add(um);
                                    }
                                }  // lock
                            }
                        }
                    }
                }
                catch (Exception exp)
                {
                    Traces.Main_TraceEvent(TraceEventType.Error, 1, "Error in retrieving own article: {0}", NNTPServer.Traces.ExceptionToString(exp));                    
                }
            }
        }

        Regex emailFinderRegEx = new Regex(@"^(.*(\s|<))([a-z0-9!#$%&'*+/=?^_`{|}~-]+(?:\.[a-z0-9!#$%&'*+/=?^_`{|}~-]+)*@(?:[a-z0-9](?:[a-z0-9-]*[a-z0-9])?\.)+[a-z0-9](?:[a-z0-9-]*[a-z0-9])?)(>|s|$)",
            RegexOptions.CultureInvariant | RegexOptions.Compiled | RegexOptions.Singleline | RegexOptions.IgnoreCase);

        #endregion
    }  // class ForumDataSource

    public class ForumNewsgroup : Newsgroup
    {
      internal const int DefaultMsgNumber = 1000;

      internal static string GetForumName(Forum forum)
      {
        return string.Format("{0}.{1}.{2}", "Answers", forum.LocaleName, forum.ShortName);
      }
      public ForumNewsgroup(Forum forum, QnAClient provider, MetaDataInfo metaDataInfo, IEnumerable<MetaDataInfo> uniqueInfos)
        : base(GetForumName(forum), 1, DefaultMsgNumber, true, DefaultMsgNumber, DateTime.Now)
      {
        UniqueInfos = uniqueInfos;
        BaseGroupName = GroupName;
        SubGroupName = string.Empty;
        if (metaDataInfo != null)
        {
          SubGroupName = metaDataInfo.Name;
          GroupName = GroupName + "." + metaDataInfo.Name;
          MetaDataInfo = metaDataInfo;
        }
        Provider = provider;
        Locale = forum.LocaleName.ToLowerInvariant();
        ShortName = forum.ShortName;

        ForumId = forum.Id;
        DisplayName = forum.DisplayName;
        Description = forum.Description;
        if (string.IsNullOrEmpty(Description) == false)
        {
          Description = Description.Replace("\n", " ").Replace("\r", string.Empty).Replace("\t", string.Empty);
        }
      }

      internal QnAClient Provider;
      internal Guid ForumId;
      internal string Locale;
      internal string BaseGroupName;
      internal string SubGroupName;
      internal string ShortName;
      /// <summary>
      /// Only contains the meta-data that must be used to filter this group
      /// </summary>
      internal MetaDataInfo MetaDataInfo;
      /// <summary>
      /// This contains all (unique) meta-data infos so we can show the shortname instead of the (long) display name
      /// </summary>
      internal IEnumerable<MetaDataInfo> UniqueInfos;

      internal bool ArticlesAvailable;
    }  // class ForumNewsgroup

    public class ForumArticle : Article
    {
      public ForumArticle(Message msg, WebServiceAnswers.Mapping dbMap, ForumNewsgroup g, string domainName, int msgNo)
            : base(msgNo, GuidToId(msg.Id, domainName, null, null))
        {
          var metaInfo = UserSettings.Default.MetaInfo;
          // If we support sub-group names with different subjects, then we need to generate a differen MessageId, 
          // because otherwise some newsreaders will show the subject from other groups...
          if (string.IsNullOrEmpty(g.SubGroupName) == false)
            Id = GuidToId(msg.Id, domainName, "." + g.SubGroupName, dbMap == null ? null : dbMap.ActivityDate);
          else
            Id = GuidToId(msg.Id, domainName, null, dbMap == null ? null : dbMap.ActivityDate);


        DateTime? activityDateUtc = null;
        if (dbMap != null)
          activityDateUtc = dbMap.ActivityDateUtc;


        Guid = msg.Id;
        DiscussionId = msg.ThreadId;
        Body = msg.MessageText;


        // Nimm das Date von dem Mapping, wenn Activitiy gesetzt!
        if (activityDateUtc.HasValue)
        {
          Date = string.Format(
                "{0} +0000",
                activityDateUtc.Value.ToString("ddd, d MMM yyyy HH:mm:ss", System.Globalization.CultureInfo.InvariantCulture)
                );
        }
        else
        {
          Date = string.Format(
                "{0} +0000",
                msg.CreatedDate.ToString("ddd, d MMM yyyy HH:mm:ss", System.Globalization.CultureInfo.InvariantCulture)
                );
        }

        string author = null;
            if (msg.Author != null)
            {
              if (string.IsNullOrEmpty(msg.Author.DisplayName) == false)
              {
                author = msg.Author.DisplayName;
              }
            }

            string subject = string.Empty;
            if (dbMap != null)
            {
              subject = dbMap.Title;
              if (string.IsNullOrEmpty(author) && (string.IsNullOrEmpty(dbMap.Author) == false))
                author = dbMap.Author;

              // Meta-Data-Info:
              if ((metaInfo == UserSettings.MetaInfoDisplay.InSubject) ||
                  (metaInfo == UserSettings.MetaInfoDisplay.InSubjectAndSignature))
              {
                if (string.IsNullOrEmpty(dbMap.MetaData) == false)
                {
                  string metaDataAsString = GetMetaDataString(g, dbMap.MetaData);
                  if (string.IsNullOrEmpty(metaDataAsString) == false)
                    subject = "[" + metaDataAsString + "] " + subject;
                }
              }
            }

        if (string.IsNullOrEmpty(author))
              author = "Unknown <null>";

        // X-Comment Header
            var xc = new StringBuilder();

        var tagsString = new List<string>();

        if (msg.IsAbuse)
        {
          tagsString.Add("Abuse");
          if (xc.Length > 0) xc.Append("; ");
          xc.Append("Abuse");
        }
        if (msg.IsAnswer)
        {
          tagsString.Add("Answer");
          if (xc.Length > 0) xc.Append("; ");
          xc.Append("Answer");
        }
        if (msg.IsAsset)
        {
          tagsString.Add("Asset");
          if (xc.Length > 0) xc.Append("; ");
          xc.Append("Asset");
        }
        if (msg.IsDeleted)
        {
          tagsString.Add("Deleted");
          if (xc.Length > 0) xc.Append("; ");
          xc.Append("Deleted");
        }
        if (msg.IsHidden)
        {
          tagsString.Add("Hidden");
          if (xc.Length > 0) xc.Append("; ");
          xc.Append("Hidden");
        }
        if (msg.IsPrivate)
        {
          tagsString.Add("Private");
          if (xc.Length > 0) xc.Append("; ");
          xc.Append("Private");
        }
        if (msg.IsSuggestedAsAsset)
        {
          tagsString.Add("SuggestedAsAsset");
          if (xc.Length > 0) xc.Append("; ");
          xc.Append("SuggestedAsAsset");
        }

        if (string.IsNullOrEmpty(subject) == false)
        {
          subject = subject.Replace("\n", string.Empty).Replace("\r", string.Empty);

          if (msg.ParentId.HasValue)
          {
            Subject = "Re: " + subject;
          }
          else
          {
            Subject = subject;
          }
        }

        //if (UserSettings.Default.U)



            author = author.Replace("\n", string.Empty).Replace("\r", string.Empty).Replace("\t", string.Empty);

            UserGuid = msg.AuthorId;  // This is always present


        if (activityDateUtc.HasValue)
        {
          // Die Message verweist auf sich selber, nur mit einer zusätzlichen Aktivität...
          // Deshalb setze die Reference auf mich selber, aber ohne das ActivityDate
          if (string.IsNullOrEmpty(g.SubGroupName) == false)
          {
            References = GuidToId(msg.Id, domainName, "." + g.SubGroupName, null);
          }
          else
          {
            References = GuidToId(msg.Id, domainName, null, null);
          }
          if (UserSettings.Default.SendSupersedesHeader)
          {
            Supersedes = References;
          }
        }

        // Primary message (default)
        if (msg.ParentId.HasValue)
        {
          string refStr;
          if (string.IsNullOrEmpty(g.SubGroupName) == false)
          {
            refStr = GuidToId(msg.ParentId.Value, domainName, "." + g.SubGroupName, null);
          }
          else
          {
            refStr = GuidToId(msg.ParentId.Value, domainName, null, null);
          }
          if (string.IsNullOrEmpty(References))
          {
            References = refStr;
          }
          else
          {
            References = refStr + " " + References;
          }
        }


        Newsgroups = g.GroupName;
        ParentNewsgroup = Newsgroups;
        Path = "LOCALHOST." + domainName;

        // URL: Build an URL for this discussion thread
        string url = string.Format("http://answers.microsoft.com/message/{0}", Guid);
        ArchivedAt = "<" + url + ">";


        if (msg.Author != null)
        {
          if (msg.Author.Affiliations != null)
          {
            string affStr = string.Empty;
            foreach (var af in msg.Author.Affiliations)
            {
              if (string.IsNullOrEmpty(af) == false)
              {
                if (string.IsNullOrEmpty(affStr))
                  affStr = af;
                else
                  affStr += ", " + af;

                if (xc.Length > 0) xc.Append("; ");
                xc.Append(af);
              }
            }
            if (string.IsNullOrEmpty(affStr) == false)
              author += " [" + affStr + "]";
          }

          if (msg.Author.CurrentAwards != null)
          {
            // TODO !?
          }
          if (msg.Author.UserRoles != null)
          {
            foreach (var ur in msg.Author.UserRoles)
            {
              if (ur != null)
              {
                if (xc.Length > 0) xc.Append("; ");
                xc.AppendFormat("UserRole({0} - {1})", ur.ForumName, ur.Name);
              }
            }
          }

          if (string.IsNullOrEmpty(msg.Author.EmailAddress) == false)
            UserEmail = msg.Author.EmailAddress;

            if (msg.Author.Statistics != null)
            {
              if (msg.Author.Statistics.AbuseVotes != 0)
              {
                if (xc.Length > 0) xc.Append("; ");
                xc.AppendFormat("AbuseVote: {0}", msg.Author.Statistics.AbuseVotes);
              }
              if (msg.Author.Statistics.Answers > 0)
              {
                if (xc.Length > 0) xc.Append("; ");
                xc.AppendFormat("Answers: {0}", msg.Author.Statistics.Answers);
              }
              if (msg.Author.Statistics.Followers > 0)
              {
                if (xc.Length > 0) xc.Append("; ");
                xc.AppendFormat("Followers: {0}", msg.Author.Statistics.Followers);
              }
              if (msg.Author.Statistics.HelpfulVotes > 0)
              {
                if (xc.Length > 0) xc.Append("; ");
                xc.AppendFormat("HelpfulVotes: {0}", msg.Author.Statistics.HelpfulVotes);
              }
              if (msg.Author.Statistics.MostHelpfulReplies > 0)
              {
                if (xc.Length > 0) xc.Append("; ");
                xc.AppendFormat("MostHelpfulReplies: {0}", msg.Author.Statistics.MostHelpfulReplies);
              }
              if (msg.Author.Statistics.Posts > 0)
              {
                if (xc.Length > 0) xc.Append("; ");
                xc.AppendFormat("Posts: {0}", msg.Author.Statistics.Posts);
              }
            }
        }
        XComments = xc.ToString();

        From = author;
        DisplayName = author;

        // Add the Article History if available:
        var mhStr = new StringBuilder();

        if (UserSettings.Default.ShowUsersSignature)
        {
          if ((msg.Author != null) && (string.IsNullOrEmpty(msg.Author.Signature) == false))
          {
            mhStr.Append(System.Web.HttpUtility.HtmlEncode(msg.Author.Signature));
            mhStr.Append("<hr/>");
          }
        }
        mhStr.AppendFormat("<a href='{0}'>{0}</a><br/>", url);

        if ((metaInfo == UserSettings.MetaInfoDisplay.InSignature) || (metaInfo == UserSettings.MetaInfoDisplay.InSubjectAndSignature))
        {
          if (dbMap != null)
          {
            if (string.IsNullOrEmpty(dbMap.MetaData) == false)
            {
              string metaDataAsString = GetMetaDataString(g, dbMap.MetaData);
              if (string.IsNullOrEmpty(metaDataAsString) == false)
                mhStr.Append("Meta tags: " + metaDataAsString + "<br/>");
            }
          }
        }

        if ((msg.MessageHistory != null)  && (UserSettings.Default.AddHistoryToArticle))
        {
          foreach (var mh in msg.MessageHistory)
          {
            if (string.IsNullOrEmpty(mh.Action) == false)
            {
              if (mhStr.Length > 0) 
              {
                mhStr.AppendLine();
                mhStr.Append("<br/>");
              }
              string txt;
              if (string.IsNullOrEmpty(mh.ReasonText))
              {
                txt = string.Format(System.Globalization.CultureInfo.InvariantCulture, "{0:ddd, d MMM yyyy HH:mm:ss}  +0000: " 
                //txt = string.Format(System.Globalization.CultureInfo.CurrentUICulture, "{0:G}: " 
                                         + "{1} {2} {4}", mh.ActionDate,
                                         mh.Action, mh.Reason,
                                         mh.ReasonText, mh.User == null ? string.Empty : mh.User.DisplayName);
              }
              else
              {
                txt = string.Format(System.Globalization.CultureInfo.InvariantCulture, "{0:ddd, d MMM yyyy HH:mm:ss}  +0000: " 
                //txt = string.Format(System.Globalization.CultureInfo.CurrentUICulture,"{0:G}: " 
                                         + "{1} {2} ({3}) {4}", mh.ActionDate,
                                         mh.Action, mh.Reason,
                                         mh.ReasonText, mh.User == null ? string.Empty : mh.User.DisplayName);
              }

              mhStr.Append(System.Web.HttpUtility.HtmlEncode(txt));
              mhStr.AppendLine();
            }
          }
        }  // History

        // Display the activity action in the body
        if ( (dbMap != null) && (activityDateUtc.HasValue) && (string.IsNullOrEmpty(dbMap.ActivityAction) == false))
        {
          tagsString.Add(dbMap.ActivityAction);
        }
        if ( (dbMap != null) && (activityDateUtc.HasValue))
        {
          tagsString.Add("MODIFIED");
        }

        var mci = UserSettings.Default.MessageInfos;
        if (mci == UserSettings.MessageInfoEnum.EndOfSubjectAndSignature)
        {
          if (tagsString.Count > 0)
          {
            Subject = Subject + " [" + string.Join(", ", tagsString) + "]";
            mhStr.Append("<br/>-----<br/><strong>" + string.Join("<br/>", tagsString) + "</strong><br/>-----<br/>");
          }
        }
        else if (mci == UserSettings.MessageInfoEnum.BeginOfBody)
        {
          if (tagsString.Count > 0)
            Body = "<p>-----<br/><strong>"
            + string.Join("<br/>", tagsString)
            + "</strong><br/>-----<br/></p>" + Environment.NewLine
            + Body;
        }
        else if (mci == UserSettings.MessageInfoEnum.InSignature)
        {
          if (tagsString.Count > 0)
            mhStr.Append("<br/>-----<br/><strong>" + string.Join("<br/>", tagsString) + "</strong><br/>-----<br/>");
        }
        else if (mci == UserSettings.MessageInfoEnum.EndOfSubject)
        {
          if (tagsString.Count > 0)
          {
            Subject = Subject + " [" + string.Join(", ", tagsString) + "]";
          }
        }


        Body += Environment.NewLine + "<hr/>" + mhStr;

        // Set UserEmail:
        try
        {
            // Try to find the UserEmail from the automatic mapping table...
            if (string.IsNullOrEmpty(UserEmail))
            {
                lock (UserSettings.Default.UserMappings)
                {
                    foreach (var um in UserSettings.Default.UserMappings)
                    {
                        if (um.Id == UserGuid)
                        {
                            UserEmail = um.UserEmail;
                            break;
                        }
                    }
                }
            }
        }
        catch (Exception exp)
        {
            Traces.Main_TraceEvent(TraceEventType.Error, 1, "Error in UserMapping: {0}",
                                    NNTPServer.Traces.ExceptionToString(exp));
        }
      }

      private string GetMetaDataString(ForumNewsgroup group, string metaData)
      {
        if (string.IsNullOrEmpty(metaData))
          return null;
        if (group.UniqueInfos == null)
          return null;
        string[] guids = metaData.Split(':');

        var sb = new StringBuilder();
        foreach(var idStr in guids)
        {
          Guid? id = null;
          bool found = false;
          try
          {
            id = Guid.ParseExact(idStr, "D");
          }
          catch (Exception)
          {
          }
          // Try to find this meta-data in the group; only if this is not present in the group, then display it...
          if (id != null) 
          {
              // Now try to dfind it in the infos...
              var info = group.UniqueInfos.FirstOrDefault(p => p.FilterIds[0] == id);
              if (info != null)
              {
                if (sb.Length > 0) sb.Append("; ");
                sb.Append(info.Name);
                found = true;
              }
          }
          if (found == false)
          {
            if (sb.Length > 0) sb.Append("; ");
            sb.Append(idStr);
          }
        }
        return sb.ToString();
      }


      // The "-" is a valid character in the messageId field:
        // http://www.w3.org/Protocols/rfc1036/rfc1036.html#z2
        public static string GuidToId(Guid guid, string domainName, string suffix, DateTime? activityDateAsUtc)
        {
            var s = guid.ToString();

            if (activityDateAsUtc.HasValue)
          {
            s += "$" + activityDateAsUtc.Value.ToString("o", System.Globalization.CultureInfo.InvariantCulture);
          }

          if (string.IsNullOrEmpty(suffix))
            return "<" + s + "@" + domainName + ">";
          return "<" + s + "@" + domainName + suffix + ">";
        }

        public static Guid? IdToGuid(string id, out DateTime? activityDateAsUtc)
        {
          activityDateAsUtc = null;
            if (id == null) return null;
            if (id.StartsWith("<") == false) return null;
            id = id.Trim('<', '>');
            var parts = id.Split('@', '$');

            // The first part is always the id:
            Guid guid;
            try
            {
              guid = new Guid(parts[0]);
            }
            catch
            {
                return null;
            }
          // The second part is the activityDate, if available
            if (parts.Length > 2)
            {
              try
              {
                activityDateAsUtc = DateTime.ParseExact(parts[1], "o", System.Globalization.CultureInfo.InvariantCulture);
              }
              catch
              {
              }
            }

          return guid;
        }
    }



  /// <summary>
  /// This class is responsible for providing the corret message number for a forum / tread / message
  /// </summary>
  /// <remarks>
  /// The concept is as follows:
  /// - At the beginning, the max. Msgä is 1000
  /// - If the first message is going to be retrived, then the last x days of messages are retrived from the forum
  /// 
  /// 
  /// - Last message number (Msg#) of the group -
  /// There must be a difference between the first time and the later requests.
  /// The first time, we need to find out how many messages we want to retrive from the web-service.
  /// The logic will be:
  /// - Retrive the last xxx threads via "GetThreadListByForumId(id, locale, metadataFilter[], threadfilter[], threadSortOrder?, sortDirection, startRow, maxRows, optional)"
  ///   - With this, we have a list of the last xxx threads with the corresponding "ReplyCountField" from the ThreadStatistics
  ///   - Then we start the Msg# with "10000" (constant)
  ///   - Then we calculate the last Msg# by "threads + (foreach += thread.ReplyCount) and we also save the Msg# for each thread
  ///   - Alternatively we request the whole list of replies to each thread and store the id
  ///   - After we have all messages, we sort it by date and generate the Msg#
  /// </remarks>
  internal class MsgNumberManagement
  {
    public MsgNumberManagement(string basePath, string domainName, bool useFileStorage)
    {
      _domainName = domainName;

      _baseDir = System.IO.Path.Combine(basePath, "Data");
      if (System.IO.Directory.Exists(_baseDir) == false)
      {
        System.IO.Directory.CreateDirectory(_baseDir);
      }

      _db = new WebServiceAnswers.LocalDbAccess(_baseDir);
    }

    private readonly WebServiceAnswers.LocalDbAccess _db;
    private readonly string _domainName;

    private readonly string _baseDir;

    private Dictionary<string, object> _dirsCheck = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
    private string IniFile(string baseGroupName, string fileName)
    {
      string dir = System.IO.Path.Combine(_baseDir, baseGroupName);
      if (_dirsCheck.ContainsKey(dir) == false)
      {
        if (System.IO.Directory.Exists(dir) == false)
        {
          System.IO.Directory.CreateDirectory(dir);
        }
        _dirsCheck.Add(dir, null);
      }
      return System.IO.Path.Combine(dir, fileName);
    }

    /// <summary>
    /// Sets the max. Msg# and the number of messages for the given forum. It returns <c>false</c> if there are no messages stored for this forum.
    /// </summary>
    /// <param name="group"></param>
    /// <returns></returns>
    public bool GetMaxMessageNumber(ForumNewsgroup group)
    {
      lock (group)
      {
        using (var con = _db.CreateConnection(group.BaseGroupName, group.SubGroupName, false))
          // prevent the database from being created if it does not yet exist
        {
          if (con == null)
          {
            group.ArticlesAvailable = false;
            return false;
          }
          if (con.Mappings.Any() == false)
          {
            group.ArticlesAvailable = false;
            return false;
          }
          long min = con.Mappings.Min(p => p.MessageNumber);
          long max = con.Mappings.Max(p => p.MessageNumber);
          group.FirstArticle = (int) min;
          group.LastArticle = (int) max;
          group.NumberOfArticles = (int) (max - min);
          group.ArticlesAvailable = true;
          return true;
        }
      }
    }

    private DateTime? GetMaxDateForThreads(ForumNewsgroup group)
    {
      using (var con = _db.CreateConnection(group.BaseGroupName, group.SubGroupName))
      {
        if (con.Mappings.Where(p => p.LastReplyDate != null).Any() == false)
          return null;
        var dt = con.Mappings.Where(p => p.LastReplyDate != null).Max(p => p.LastReplyDate.Value);
        return new DateTime(dt.Year, dt.Month, dt.Day, dt.Hour, dt.Minute, dt.Second, dt.Millisecond, DateTimeKind.Utc);
      }
    }

    private DateTime? GetMaxDateForThreadsByLastReplyOrActivity(ForumNewsgroup group)
    {
      DateTime? res = null;
      using (var con = _db.CreateConnection(group.BaseGroupName, group.SubGroupName))
      {
        if (con.Mappings.Where(p => p.LastReplyDate != null).Any())
          res = con.Mappings.Where(p => p.LastReplyDate != null).Max(p => p.LastReplyDate.Value);
        if (con.Mappings.Where(p => p.LastReplyDate != null).Any())
        {
          DateTime ad = con.Mappings.Where(p => p.ActivityDate != null).Max(p => p.ActivityDate.Value);
          if (res.HasValue == false)
          {
            res = ad;
          }
          else if (res.Value < ad)
          {
            res = ad;
          }
        }

        if (res.HasValue)
          return new DateTime(res.Value.Year, res.Value.Month, res.Value.Day, res.Value.Hour, res.Value.Minute, res.Value.Second, res.Value.Millisecond, DateTimeKind.Utc);
      }
      return res;
    }


    /// <summary>
    /// Updates the group from the web service.
    /// </summary>
    /// <param name="group"></param>
    /// <param name="provider"></param>
    /// <remarks>
    /// This method must be multi-threaded save, because it might be accessed form a client 
    /// in different threads if for example the server is too slow...
    /// </remarks>
    /// <returns>It returns (some of the) new articles</returns>
    public IEnumerable<ForumArticle> UpdateGroupFromWebService(ForumNewsgroup group, QnAClient provider, Action<string> progress)
    {
      bool useLastActivity = UserSettings.Default.UpdateInfoMode == UserSettings.UpdateInfoModeEnum.BasedOnLastActivity;
      // Lock on the group...
      lock (group)
      {
        var articles = new List<ForumArticle>();

        // First get the Msg# from the local mapping table:
        GetMaxMessageNumber(group);

        // Enthält die Messages, welche nacher gespeichert werden sollen!
        // Diese werden aber erst noch auf doppelte Einträge verglichen!
        var mappings = new List<WebServiceAnswers.Mapping>();

        IEnumerable<Thread> threads = null;
        DateTime? lastDate = null;
        
        if (useLastActivity == false)
        {
          lastDate = GetMaxDateForThreads(group);
        }
        else
        {
          lastDate = GetMaxDateForThreadsByLastReplyOrActivity(group);
        }

        if ((group.ArticlesAvailable == false) || (lastDate == null))
        {

          // It is the first time, we are asking articles from this forum/locale from the web-service
          // Here we fetch the latest threads, ordered by the LastReplyDate (or?)
          // How many should be fetch?
          //var sw = Stopwatch.StartNew();

          threads = GetThreads(provider, group, null, progress, InternalThreadFilter.FirstAccess);

          if ((threads == null) || (threads.Any() == false))
          {
            // INFO: Special handling of forums which have 0 threads!!!
            group.FirstArticle = group.LastArticle;
            group.NumberOfArticles = 0;
            return null;
          }

          //DebugMsg(string.Format("{0}: ThreadCount: {1}; Duration: {2} ms; from: {3:O} to {4:O}",  group.GroupName, threads.Count(), sw.ElapsedMilliseconds,  threads.Min(p => p.CreatedDate), threads.Max(p => p.CreatedDate) ));
        }
        else
        {
          // We have already some messages in the list
          // Now only query the last threads, which have changed since the last message date...

          if (useLastActivity)
          {
            threads = GetThreads(provider, group, lastDate.Value, progress, InternalThreadFilter.LastActivity);
          }
          else
          {
            // INFO: Here we need to make two calls, because the web-service only returns threads with at least one reply if 
            // we use the reply-filter; so we also need to get threads which has no replies yet...
            // We do it in the following order to prevent missing threads:
            // 1. Get Threads without replies
            // 2. Get threads with replies
            // If between 1 and 2 a thread gets his first reply then we have duplicate threads (which are later filtered)
            // If we would do it the other way, we would miss this thread!
              IEnumerable<Thread> threads1 = null;
              // 2012-07-17: After changing to Answers 2.4, MS now sets the "LastReplyDate" also in the first message, if there is no reply!
              // Therefor we need to remove the query to the threads without replies...
              // Here the e-mail from Anurag:
              // One change which was done in 2.4 due to performance reasons was to populate the last reply date for threads the moment 
              // they get created(so initially both created date and last reply date are same).  

            //threads1 = GetThreads(provider, group, lastDate.Value, progress, InternalThreadFilter.NewThreadsSince);

            var threads2 = GetThreads(provider, group, lastDate.Value, progress, InternalThreadFilter.LastRepliesSince);

            // Now agretate these two together...
            if ((threads1 != null) && (threads2 == null))
            {
              threads = threads1;
            }
            else if ((threads2 != null) && (threads1 == null))
            {
              threads = threads2;
            }
            else if ((threads2 != null) && (threads1 != null))
            {
              // merge them together..
              var t = new List<Thread>();
              t.AddRange(threads1);
              t.AddRange(threads2);
              threads = t;
            }
          }  // !LastActivity

          if (UserSettings.Default.UpdateInfoMode ==
                 UserSettings.UpdateInfoModeEnum.BasedOnLastReplyAndObsoleteThreads)
          {
            // Check for obsolete threads:
            ObsoleteThread[] obsoleteThreads = provider.GetObsoleteThreadList(lastDate.Value);
            if ((obsoleteThreads != null) && (obsoleteThreads.Length > 0))
            {
              using (var con = _db.CreateConnection(group.BaseGroupName, group.SubGroupName))
              {
                // Check if this threads are in my current news-group
                foreach (var obsoleteThread in obsoleteThreads)
                {
                  WebServiceAnswers.Mapping msg =
                    con.Mappings.FirstOrDefault(p => p.ThreadId == obsoleteThread.ThreadId);
                  if (msg != null)
                  {
                    mappings.Add(new WebServiceAnswers.Mapping()
                                   {
                                     //ThreadId = m.ThreadId,
                                     CreatedDate = obsoleteThread.ActivityDate,
                                     MessageId = msg.MessageId,
                                     Author = "[" + UserSettings.ProductName + "]",
                                     Title = msg.Title,
                                     //Tag = m,
                                     MetaData = msg.MetaData,
                                     ActivityDate = obsoleteThread.ActivityDate,
                                     ActivityAction = obsoleteThread.ThreadAction.ToString()
                                   });
                  }
                }
              }
            }
          }

          if ((threads == null) && (mappings.Count <= 0))
          {
            Traces.Main_TraceEvent(TraceEventType.Warning, 1,
                                   "No threads returned for {0} with last date of {1}", group.GroupName, lastDate);
            return null;  // Es gibt nichts neues..
          }
        }


        //var sw2 = Stopwatch.StartNew();
        int tCount = 0;
        if (threads != null)
          tCount = threads.Count();
        if ((threads != null) && (tCount > 0))
        {
          // Now try to find the threads, we are not know of...
          int idx = 0;
          int mCount = 0;
          Parallel.ForEach(
            threads, new ParallelOptions() {MaxDegreeOfParallelism = 5},
            p =>
              {
                p.MessageTemp = null;
                int actIdx = System.Threading.Interlocked.Add(ref idx, 1);
                if (actIdx%10 == 0)
                {
                  MainWindow.NewsgroupUpdateInfo(group.GroupName,
                                                 string.Format("Getting message infos for threads {0} / {1}", actIdx,
                                                               tCount));
                  if (progress != null)
                    progress(string.Format("{0}: Getting message infos for threads {1} / {2}", group.GroupName, actIdx,
                                           tCount));
                }
                var msgs = GetMessages(provider, p);
                if (msgs != null)
                {
                  p.MessageTemp = msgs.ToArray();
                  System.Threading.Interlocked.Add(ref mCount, p.MessageTemp.Length);
                }
              });

          //if (group.ArticlesAvailable == false)
          //{
          //  DebugMsg(string.Format("{0}: GetMessage: ThreadCount: {1}; MessageCount: {2}; Duration: {3} ms",
          //                         group.GroupName, tCount, mCount, sw2.ElapsedMilliseconds));
          //}

          //sw2.Reset();
          //sw2.Start();

          // Now we have all messages... create a Database-Mapping from it...
          foreach (var t in threads)
          {
            var mdString = BuildMetaDataString(group, t);
            DateTime? lastActivity = null;
            // I should only save a new message if the message was *modified*! It makes no sence to create a new message if a the thread had a new "activity"...
            // because the new activity will be seen in one of the messages...
            if ((useLastActivity) && (t.LastActivityDate != t.CreatedDate))
              lastActivity = t.LastActivityDate;
            mappings.Add(new WebServiceAnswers.Mapping()
                           {
                             ThreadId = t.Id,
                             CreatedDate = t.CreatedDate,
                             MessageId = t.MessageId,
                             Author = t.Author == null ? null : t.Author.DisplayName,
                             Title = t.Title,
                             MetaData = mdString,
                             LastReplyDate = t.LastReplyDate,
                             ActivityDate = lastActivity,
                           });
            if (t.MessageTemp != null)
            {
              foreach (var m in t.MessageTemp)
              {
                DateTime? lastActivityMsg = null;
                if ((useLastActivity) && (m.CreatedDate != m.LastModifiedDate))
                  lastActivityMsg = m.LastModifiedDate;
                mappings.Add(new WebServiceAnswers.Mapping()
                               {
                                 //ThreadId = m.ThreadId,  // MUST be NULL! so we can identify the Thread by the "ThreadId" If we need to knwo the parent-thread, we need to add a "ParentThreadId" column!
                                 CreatedDate = m.CreatedDate,
                                 MessageId = m.Id,
                                 Author = m.Author == null ? null : m.Author.DisplayName,
                                 Title = t.Title,
                                 Tag = m,
                                 MetaData = mdString,
                                 // INFO: Speichere auch das LastReplyDate der einzelnen Message!
                                 // Diese habe ich ja jetzt korrekt abgefragt!
                                 LastReplyDate = t.LastReplyDate,
                                 ActivityDate = lastActivityMsg,
                               });
              }
            }
          } // foreach thread

          // So... now save the data to the database... avoid duplicates...

          if (mappings.Count > 50)
          {
            MainWindow.NewsgroupUpdateInfo(group.GroupName,
                                           string.Format("Checking {0} messages for duplicates and saving messages",
                                                         mappings.Count));
            if (progress != null)
              progress(string.Format("{0}: Checking {1} messages for duplicates and saving messages", group.GroupName,
                                     mappings.Count));
          }
        }  // threads != null

        var acceptedMapping = new List<WebServiceAnswers.Mapping>();
        using (var con = _db.CreateConnection(group.BaseGroupName, group.SubGroupName))
        {
          foreach (var m in mappings)
          {
            Mapping m1 = m;
            // Check for duplicates... check MsgId and check also ActivityDate
            if (con.Mappings.Any(p => (p.MessageId == m1.MessageId) && (p.ActivityDate == null)) == false)
            {
              m1.ActivityDate = null; // Stelle sicher, dass ich auch kein ActivityDate gesetzt habe, da es ja bisher noch keine Msg gibt und es immer eine geben muss *ohne* ActivityDate!
              acceptedMapping.Add(m1);
            }
            else if (m1.ActivityDate.HasValue)
            {
              // So, es gibt auf jeden Fall schon eine Msg *ohne* ActivityDate, jetzt kann ich ja noch zusätzliche anfügen..., falls ich diese oben noch nicht angefügt habe...
              if (con.Mappings.Any(p => (p.MessageId == m1.MessageId) && (p.ActivityDate == m1.ActivityDate.Value)) == false)
              {
                acceptedMapping.Add(m1);
              }
            }
          }

          if (acceptedMapping.Count > 0)
          {
            // now sort by date and assign a Msg#!
            acceptedMapping.Sort((a, b) => DateTime.Compare(a.CreatedDate, b.CreatedDate));
            int msgNo = group.LastArticle + 1;
            if (group.ArticlesAvailable == false)
            {
              msgNo = group.LastArticle - (acceptedMapping.Count - 1);
              // Be sure I am not getting zero or negative...
              if (msgNo <= 0)
                msgNo = 1; // start with 1
            }

            foreach (var m2 in acceptedMapping)
            {
              m2.Id = Guid.NewGuid();
              // Assign a Msg#
              m2.MessageNumber = msgNo++;

              var msg = m2.Tag as Message;
              if (msg != null)
              {
                var a = new ForumArticle(msg, m2, group, _domainName, (int)m2.MessageNumber);
                articles.Add(a);
              }

              // INFO: Special Handling of DateTimes in SQL CE!
              // All times from the web-servcie are in UTC / SQL-CE does not understand UTC, so it converts it to local time
              if (m2.LastReplyDate.HasValue)
              {
                var dt = m2.LastReplyDate.Value;
                m2.LastReplyDate = new DateTime(dt.Year, dt.Month, dt.Day, dt.Hour, dt.Minute, dt.Second, dt.Millisecond, DateTimeKind.Unspecified);
              }

              if (m2.ActivityDate.HasValue)
              {
                var dt = m2.ActivityDate.Value;
                m2.ActivityDate = new DateTime(dt.Year, dt.Month, dt.Day, dt.Hour, dt.Minute, dt.Second, dt.Millisecond, DateTimeKind.Unspecified);
              }

              con.Mappings.AddObject(m2);
            }

            con.SaveChanges();

            // ...and now query the max number from the database again...
            GetMaxMessageNumber(group);
          }
        }

        //if (group.ArticlesAvailable == false)
        //{
        //  DebugMsg(string.Format("{0}: Saving in database: Duration: {1} ms",
        //                         group.GroupName, sw2.ElapsedMilliseconds));
        //}


        MainWindow.NewsgroupUpdateInfo(group.GroupName, string.Format("Finished updating message infos ({0} - {1})", group.FirstArticle, group.LastArticle), true);
        if (progress != null)
        {
          progress(string.Format("{0}: Finished updating message infos", group.GroupName));
        }

        return articles;
      }
    }

    private string BuildMetaDataString(ForumNewsgroup group, Thread thread)
    {
      if (thread == null)
        return null;
      if (thread.ThreadMetaDataList == null)
        return null;
      var sb = new StringBuilder();
      foreach (var md in thread.ThreadMetaDataList)
      {
        if (md != null)
        {
          if (sb.Length > 0) sb.Append(":");
          sb.Append(md.MetaDataId);
        }
      }
      return sb.ToString();
    }

    enum InternalThreadFilter
    {
      FirstAccess,
      NewThreadsSince,
      LastRepliesSince,
      LastActivity,
    }
    private static IEnumerable<Thread> GetThreads(QnAClient provider, ForumNewsgroup group, DateTime? since, Action<string> progress, InternalThreadFilter internalThreadFilter)
    {
      const int pageSize = 51;
      ThreadSortOrder sortOrder = ThreadSortOrder.LastReplyDate;

      ThreadFilter[] filter = null;
      if (since != null)
      {
        if (since.Value.Kind != DateTimeKind.Utc)
        {
          // Convert it always to UTC:
          since = new DateTime(since.Value.Year, since.Value.Month, since.Value.Day, since.Value.Hour, since.Value.Minute, since.Value.Second, since.Value.Millisecond, DateTimeKind.Utc);
        }
        switch(internalThreadFilter)
        {
          case InternalThreadFilter.LastRepliesSince:
            filter = new ThreadFilter[]
                       {
                         new ThreadDateRangeFilter()
                           {
                             filterType = ThreadDateRangeFilter.FilterType.ReplyDate, 
                             StartDate = since.Value.AddSeconds(-1),  // substract 1 second, so it should always retruns at least the last message...
                             EndDate = DateTime.UtcNow.AddHours(2)
                           }
                       };
            break;
          case InternalThreadFilter.NewThreadsSince:
            filter = new ThreadFilter[]
                       {
                         new ThreadDateRangeFilter()
                           {
                             filterType = ThreadDateRangeFilter.FilterType.CreateDate, 
                             StartDate = since.Value.AddSeconds(-1),  // substract 1 second, so it should always retruns at least the last message...
                             EndDate = DateTime.UtcNow.AddHours(2)
                           },
                         new ReplyThreadFilter() {ReplyCount = 1, Operator = FilterOperator.LessThan}
                       };
            break;
          case InternalThreadFilter.LastActivity:
            filter = new ThreadFilter[]
                       {
                         new ThreadDateRangeFilter()
                           {
                             filterType = ThreadDateRangeFilter.FilterType.ActivityDate, 
                             StartDate = since.Value.AddSeconds(-1),  // substract 1 second, so it should always retruns at least the last message...
                             EndDate = DateTime.UtcNow.AddHours(2)
                           }
                       };
            sortOrder = ThreadSortOrder.LastActivityDate;
            break;
        }

      }
      else
      {
        Debug.Assert(internalThreadFilter == InternalThreadFilter.FirstAccess);
      }
      Guid[] metaDataFilter = null;
      if (group.MetaDataInfo != null)
        metaDataFilter = group.MetaDataInfo.FilterIds;

      var resThreads = new Dictionary<Guid, Thread>();

      if (filter == null)
      {
        MainWindow.NewsgroupUpdateInfo(group.GroupName, "First access; retrieving threads...");
        if (progress != null)
          progress(string.Format("{0}: First access; retriving threads...", group.GroupName));
      }
      else
      {
        string txt;
        if (internalThreadFilter == InternalThreadFilter.LastRepliesSince)
          txt = string.Format("Retrieving last replies since {0:s}...", since);
        else if  (internalThreadFilter == InternalThreadFilter.LastActivity)
          txt = string.Format("Retrieving last activities since {0:s}...", since);
        else
          txt = string.Format("Retrieving new threads since {0:s}...", since);
        MainWindow.NewsgroupUpdateInfo(group.GroupName, txt);
        if (progress != null)
          progress(string.Format("{0}: {1}", group.GroupName, txt));
      }

      // The first call should be not parallel! Then we know how many threads we have (threads.TotalResultCount) and then we should start calling in parallel!
      var threads = provider.GetThreadListByForumId(
        group.ForumId, group.Locale, metaDataFilter, filter, sortOrder,
        SortDirection.Desc, 1, pageSize,
        AdditionalThreadDataOptions.Author);
      if ((threads == null) || (threads.Items == null))
      {
        if (filter == null)
        {
          MainWindow.NewsgroupUpdateInfo(group.GroupName, "No threads available...", true);
        }
        else
        {
          MainWindow.NewsgroupUpdateInfo(group.GroupName, string.Format("No new threads available... (old: {0} - {1})", group.FirstArticle, group.LastArticle), true);
        }
        if (progress != null)
          progress(string.Format("{0}: No (new) threads available...", group.GroupName));
        return null;
      }

      // Add ressult to my internal result-list
      foreach(var t in threads.Items)
      {
        if (resThreads.ContainsKey(t.Id) == false)
          resThreads.Add(t.Id, t);
      }

      int maxCount = Math.Min(UserSettings.Default.MaxThreadCountOnFirstRetrival, threads.TotalResultCount);
      if (maxCount > resThreads.Count)
      {
        // calculate the max-Size
        int count = (int) Math.Ceiling((double) maxCount/(pageSize - 1));
        //int overlappCounter = 0;  // This does not work, because the threads are asynchronous...
        Parallel.For(
          1, count, new ParallelOptions() {MaxDegreeOfParallelism = 5},
          p =>
            {
              int startRow = p*(pageSize - 1) + 1; // -overlappCounter; // StartRow must be 1 or greater!
              int maxpageSize = Math.Min(pageSize, maxCount - startRow);
              if (maxpageSize <= 0)
                maxpageSize = 1;
              // allow 1 thread to be overlapped; this might happen if between two calls a new thread is created
              var threads2 = provider.GetThreadListByForumId(group.ForumId, group.Locale, metaDataFilter, filter,
                                                             sortOrder,
                                                             SortDirection.Desc, startRow, maxpageSize,
                                                             AdditionalThreadDataOptions.Author);
              if ((threads2 != null) && (threads2.Items != null))
              {
                foreach (var t in threads2.Items)
                {
                  lock (resThreads)
                  {
                    if (resThreads.ContainsKey(t.Id) == false)
                      resThreads.Add(t.Id, t);
                  }
                }
                int actCount = 0;
                lock (resThreads)
                {
                  actCount = resThreads.Count;
                }

                if (filter == null)
                {
                  MainWindow.NewsgroupUpdateInfo(group.GroupName, string.Format("First access; retriving {0} / {1}", actCount, maxCount));
                  if (progress != null)
                  {
                    progress(string.Format("{0}: First access; retriving {1} / {2}",
                                           group.GroupName, actCount, maxCount));
                  }
                }
                else
                {
                  MainWindow.NewsgroupUpdateInfo(group.GroupName,
                                                 string.Format("Retriving {0} / {1}", actCount, maxCount));
                  if (progress != null)
                  {
                    progress(string.Format("{0}: retriving {1} / {2}",
                                           group.GroupName, actCount, maxCount));
                  }
                }
              }
            }
          );
      }

      return resThreads.Values;
    }

    private IEnumerable<Message> GetMessages(QnAClient provider, Thread thread)
    {
      const int pageSize = 50;

      var res = new Dictionary<Guid, Message>();

      // The first call should be not parallel! Then we know how many messages we have (msgs.TotalResultCount) and then we should start calling in parallel!
      var msgs = provider.GetMessageListByThreadId(thread.Id, MessageSortOrder.CreatedDate, SortDirection.Desc,
                                             1, pageSize, AdditionalMessageDataOptions.Author | AdditionalMessageDataOptions.MessageHistory);
      if ((msgs == null) || (msgs.Items == null))
        return null;

      if (msgs.TotalResultCount > pageSize)
      {
        // calculate the max-Size
        int count = (int) Math.Ceiling((double) msgs.TotalResultCount/(pageSize - 1));
        // Now call it parallel...
        Parallel.For(
          1, count, new ParallelOptions() {MaxDegreeOfParallelism = 5},
          p =>
          {
            int startRow = p * (pageSize - 1) + 1; // StartRow must be 1 or greater!
            // allow 1 message to be overlapped; this might happen if between two calls a new message is created
            var msgs2 = provider.GetMessageListByThreadId(thread.Id, MessageSortOrder.CreatedDate, SortDirection.Desc,
                                                   startRow, pageSize, AdditionalMessageDataOptions.Author);
            if ((msgs2 != null) && (msgs2.Items != null))
            {
              foreach (var m2 in msgs2.Items)
              {
                lock (res)
                {
                  if (res.ContainsKey(m2.Id) == false)
                    res.Add(m2.Id, m2);
                }
              }
            }
          }
          );
      }
      foreach(var m in msgs.Items)
      {
        if (res.ContainsKey(m.Id) == false)
          res.Add(m.Id, m);
      }

      return res.Values;
    }

    public ForumArticle GetMessageById(ForumNewsgroup forumNewsgroup, Guid id, DateTime? activityDateUtc, QnAClient provider, bool mustUseCorrectMsgNo = true)
    {
      WebServiceAnswers.Mapping map = null;
      int msgNo = 0;
      if (mustUseCorrectMsgNo)
      {
        lock (forumNewsgroup)
        {
          using (var con = _db.CreateConnection(forumNewsgroup.BaseGroupName, forumNewsgroup.SubGroupName))
          {
            if (activityDateUtc.HasValue)
            {
              map = con.Mappings.FirstOrDefault(p => (p.MessageId == id) && (p.ActivityDate == activityDateUtc.Value));
            }
            else
            {
              map = con.Mappings.FirstOrDefault(p => (p.MessageId == id) && (p.ActivityDate == null));
            }
          }
        }
        if (map == null)
        {
          return null;
        }
        msgNo = (int) map.MessageNumber;
      }

      return InternalGetMsgById(forumNewsgroup, id, provider, map, msgNo);
    }

    public Article GetMessageByMsgNo(ForumNewsgroup forumNewsgroup, int articleNumber, QnAClient provider)
    {
      WebServiceAnswers.Mapping map;
      lock (forumNewsgroup)
      {
        using (var con = _db.CreateConnection(forumNewsgroup.BaseGroupName, forumNewsgroup.SubGroupName))
        {
          map = con.Mappings.FirstOrDefault(p => p.MessageNumber == articleNumber);
        }
      }
      if (map == null)
      {
        return null;
      }
      return InternalGetMsgById(forumNewsgroup, map.MessageId, provider, map, (int)map.MessageNumber);
    }

    private ForumArticle InternalGetMsgById(ForumNewsgroup forumNewsgroup, Guid id, QnAClient provider, WebServiceAnswers.Mapping map, int msgNo)
    {
      Message res;
      try
      {
         res = provider.GetMessage(id, AdditionalMessageDataOptions.All);
      }
      catch (System.ServiceModel.FaultException<Microsoft.Support.Community.Core.ErrorHandling.PlatformFault> exp)
      {
        if (exp.Detail != null)
        {
          switch (exp.Detail.ErrorCode)
          {
            case 52011:
              Traces.Main_TraceEvent(TraceEventType.Warning, 1, "GetMessage_52011_({0}): {1}", id, exp.Message);
              return null;
            case 52012:
              Traces.Main_TraceEvent(TraceEventType.Warning, 1, "GetMessage_52012_({0}): {1}", id, exp.Message);
              return null;
            case 52013:
              Traces.Main_TraceEvent(TraceEventType.Warning, 1, "GetMessage_52013_({0}): {1}", id, exp.Message);
              return null;
          }
        }
        throw;
      }
      catch(Exception exp)
      {
        // Special handling of deleted messages:
        if (exp.Message != null)
        {
          if (exp.Message.IndexOf("This item has been deleted", StringComparison.OrdinalIgnoreCase) >= 0)
          {
            Traces.Main_TraceEvent(TraceEventType.Warning, 1, "GetMessage({0}): {1}", id, exp.Message);
            return null;
          }
          // User is not allowed to view hidden/deleted messages.
          if (exp.Message.IndexOf("User is not allowed", StringComparison.OrdinalIgnoreCase) >= 0)
          {
            Traces.Main_TraceEvent(TraceEventType.Warning, 1, "GetMessage({0}): {1}", id, exp.Message);
            return null;
          }
          if (exp.Message.IndexOf("Insufficient permission", StringComparison.OrdinalIgnoreCase) >= 0)
          {
            Traces.Main_TraceEvent(TraceEventType.Warning, 1, "GetMessage({0}): {1}", id, exp.Message);
            return null;
          }
        }
        throw;
      }
      if (res == null)
        return null;
      return new ForumArticle(res, map, forumNewsgroup, _domainName, msgNo);
    }

    private static void GetMetaDataString(int deep, MetaData metaData, StringBuilder result, List<string> subForumNames, string parentName)
    {
      string myNewsGroupName = parentName;
      if (metaData.MetaDataTypes.Any(p => p.Id == MetaDataInfo.MetaValue))
      {
        myNewsGroupName += "." + metaData.ShortName;
        subForumNames.Add(myNewsGroupName);
      }
      var deepStr = new string(' ', deep * 2);
      result.AppendFormat("{0} {1:00} Short: '{2}' Display: '{3}' Locale: '{4}, Id: {5}<br/>", deepStr, deep, metaData.ShortName, metaData.DisplayName, metaData.LocaleName, metaData.Id);
      foreach (var types in metaData.MetaDataTypes)
      {
        result.AppendFormat("{0}  {1:00} Type: {2}, Id: {3}<br/>", deepStr, deep, types.ShortName, types.Id);
      }
      foreach (var childMetaData in metaData.ChildMetaData)
      {
        GetMetaDataString(deep + 1, childMetaData, result, subForumNames, myNewsGroupName);
      }
    }

    public void SaveGroupFilterData(ForumNewsgroup g)
    {
      if ((g.MetaDataInfo != null) && (g.MetaDataInfo.FilterIds != null))
      {
        var sb = new StringBuilder();
        foreach (var fd in g.MetaDataInfo.FilterIds)
        {
          if (sb.Length > 0) sb.Append(":");
          sb.Append(fd.ToString("D"));
        }
        string fn = IniFile(g.BaseGroupName, "FilterData.ini");
        IniHelper.SetString(g.GroupName.ToLowerInvariant(), "Name", g.MetaDataInfo.Name, fn);
        IniHelper.SetString(g.GroupName.ToLowerInvariant(), "FilterData", sb.ToString(), fn);
      }
    }
    public MetaDataInfo LoadGroupFilterData(string baseGroupName, string groupName)
    {
      string fn = IniFile(baseGroupName, "FilterData.ini");
      string s = IniHelper.GetString(groupName.ToLowerInvariant(), "FilterData", fn);
      if (string.IsNullOrEmpty(s))
        return null;
      string[] ids = s.Split(':');
      Guid[] guids = new Guid[ids.Length];
      for (int i = 0; i < ids.Length; i++)
      {
        guids[i] = Guid.ParseExact(ids[i], "D");
      }
      var md = new MetaDataInfo();
      md.FilterIds = guids;
      md.Name = IniHelper.GetString(groupName.ToLowerInvariant(), "Name", fn);
      return md;
    }

    public void SaveAllMetaData(ForumNewsgroup g)
    {
      if (g.UniqueInfos != null)
      {
        string fn = IniFile(g.BaseGroupName, "AllMetaData.ini");
        foreach (var md in g.UniqueInfos)
        {
          IniHelper.SetString(md.FilterIds[0].ToString("D"), "Name", md.Name, fn);
        }
      }
    }
    public IEnumerable<MetaDataInfo> LoadAllMetaData(QnAClient provider, Guid forumId, string locale, string baseGroupName, bool forceUpdate = false)
    {
      var infos = new List<MetaDataInfo>();
      string fn = IniFile(baseGroupName, "AllMetaData.ini");
      string[] sections = IniHelper.GetSectionNamesFromIni(fn);
      if (sections != null)
      {
        foreach (var s in sections)
        {
          var md = new MetaDataInfo();
          md.FilterIds = new[] {Guid.ParseExact(s, "D")};
          md.Name = IniHelper.GetString(s, "Name", fn);
          infos.Add(md);
        }
      }
      if ((provider != null) && ((infos.Count <= 0) || forceUpdate) )
      {
        var result = provider.GetMetaDataListByForumId(forumId, locale);
        var uniqueInfos = new List<MetaDataInfo>();
        if (result != null)
        {
          foreach (var metaData in result)
          {
            MetaDataInfo.GetMetaDataInfos(metaData, uniqueInfos, null);
          }
        }
        infos = uniqueInfos.ToList();
        //System.Diagnostics.Debug.WriteLine(string.Join(Environment.NewLine, infos.Select(p => p.Name)));
      }

      return infos;
    }
  }  // class MsgNumberManagement
  public class MetaDataInfo
  {
    public string Name;
    public Guid[] FilterIds;

    private static Guid ForumFilterField = new Guid("f31897ff-5441-e011-9767-d8d385dcbb12");
    private static Guid ThreadDataField = new Guid("f51897ff-5441-e011-9767-d8d385dcbb12");
    internal static Guid MetaValue = new Guid("f41897ff-5441-e011-9767-d8d385dcbb12");

    public static void GetMetaDataInfos(MetaData metaData, List<MetaDataInfo> uniqueInfos, MetaDataInfo parent)
    {
      MetaDataInfo myInfo = parent;
      if (metaData.MetaDataTypes.Any(p => p.Id == MetaValue))
      {
        // INFO: This builds the meta-data as one subfolder with unique values
        myInfo = new MetaDataInfo { Name = metaData.ShortName, FilterIds = new[] { metaData.Id } };
        if (uniqueInfos.Any(p => p.FilterIds[0] == metaData.Id) == false)
        {
          uniqueInfos.Add(myInfo);
        }

        // Optional: Build the sub-folders also...
        if ( (parent != null) 
          && (parent.FilterIds.Length == 1)  // alternative: Restrict to max. 2 levels...
          )
        {
          // INFO: This builds the meta-data like it is defined in the tree:
          string newName = parent.Name + "." + metaData.ShortName;
          var newIds = new List<Guid>();
          newIds.AddRange(parent.FilterIds);
          newIds.Add(metaData.Id);
          myInfo = new MetaDataInfo {Name = newName, FilterIds = newIds.ToArray()};
          uniqueInfos.Add(myInfo);
        }
      }
      foreach (var childMetaData in metaData.ChildMetaData)
      {
        GetMetaDataInfos(childMetaData, uniqueInfos, myInfo);
      }
    }

  }  // class MetaDataInfo

}
