using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using Microsoft.Support.Community.DataLayer.Entity;
using System.Globalization;

namespace CommunityBridge2.WebServiceAnswers
{
    public class SwaggerAccess //: IForumData
    {
        static SwaggerAccess()
        {
            FillDict();
        }
        public static Action<string> Log { get; set; }
        public static Action<string, Exception> LogExp;

        public static string AuthTicket { get; private set; }

        #region GetSupportedLocales (fixed)
        public string[] GetSupportedLocales()
        {
            // INFO: List provided by Tom Chen on So 02.04.2017 00:52
            string[] cultures = {
                "en-us", "de-de"
                //"en-us", "es-es", "pt-br", "de-de", "it-it", "fr-fr", "nl-nl",
                //"ru-ru", "ko-ko", "tr-tr", "ja-jp", "zh-hans", "zh-hant", "cs-cz",
                //"hu-hu", "pl-pl", "ar-sa", "he-il", "da-dk", "el-gr", "et-ee",
                //"fi-fi", "nb-no", "sv-se", "th-th", "id-id", "vi-vn"
            };
            return cultures;
        }
        #endregion

        #region GetForumList

        public static Dictionary<string, string> _CorrectNames = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        private static void FillDict()
        {
            _CorrectNames.Add("band", "band");
            _CorrectNames.Add("insider", "insider");
            _CorrectNames.Add("newmsn", "newmsn");
            _CorrectNames.Add("skype_old", "skype_old");
            _CorrectNames.Add("Bing", "Bing");
            _CorrectNames.Add("skype", "skype");
            _CorrectNames.Add("MSN", "MSN");
            _CorrectNames.Add("mac", "mac");
            _CorrectNames.Add("msoffice", "msoffice");
            _CorrectNames.Add("OneDrive", "OneDrive");
            _CorrectNames.Add("outlook_com", "outlook_com");
            _CorrectNames.Add("Windows", "Windows");
            _CorrectNames.Add("Office", "Office");
            _CorrectNames.Add("windowslive", "windowslive");
            _CorrectNames.Add("ie", "ie");
            _CorrectNames.Add("musicandvideo", "musicandvideo");
            _CorrectNames.Add("WinPhone", "WinPhone");
            _CorrectNames.Add("Protect", "Protect");
            _CorrectNames.Add("Feedback", "Feedback");
            _CorrectNames.Add("cortana", "cortana");
            _CorrectNames.Add("Surface", "Surface");
            _CorrectNames.Add("Mobiledevices", "Mobiledevices");
            _CorrectNames.Add("Moderator", "Moderator");
        }

        private static string GetCorrectShortName(string name)
        {
            if (_CorrectNames.ContainsKey(name))
            {
                return _CorrectNames[name];
            }
            return name;
        }

        public Forum2017[] GetForumList(string localeName)
        {
            Log?.Invoke($"GetForumList: locale={localeName}");
            var c = new Swagger.ForumClient();

            try
            {
                var res = c.GetForumsByLocaleAsyncAsync(localeName).Result;
                foreach(var r in res)
                {
                    r.ShortName = GetCorrectShortName(r.ShortName);
                }
                return res.ToArray();
            }
            catch(Exception exp)
            {
                LogExp?.Invoke("GetForumsByLocaleAsyncAsync", exp);
                throw;
            }
        }
        #endregion

        #region  GetMetaDataList (TODO)
        public MetaData2017[] GetMetaDataListByForumId(Forum2017 forum, string localeName)
        {
            Log?.Invoke($"GetMetaDataListByForumId: shortName={forum.ShortName}, locale={localeName}");
            var url = $"https://answers.microsoft.com/{localeName}/forum/filtermenu?forumName={forum.ShortName}";
            try
            {
                var resp = GetResponse(url, System.Threading.CancellationToken.None).Result;
                return resp.ToArray();
            }
            catch(AggregateException ae)
            {
                LogExp?.Invoke("GetForumsByLocaleAsyncAsync", ae.InnerException);
                throw ae;
            }
        }
        #endregion

        #region GetResponse (private)
        private async Task<System.Collections.ObjectModel.ObservableCollection<MetaData2017>> GetResponse(string url_, System.Threading.CancellationToken cancellationToken)
        {
            var client_ = new System.Net.Http.HttpClient();
            try
            {
                using (var request_ = new System.Net.Http.HttpRequestMessage())
                {
                    request_.Method = new System.Net.Http.HttpMethod("GET");
                    request_.RequestUri = new System.Uri(url_, System.UriKind.RelativeOrAbsolute);
                    request_.Headers.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
                    var response_ = await client_.SendAsync(request_, System.Net.Http.HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);
                    try
                    {
                        var headers_ = System.Linq.Enumerable.ToDictionary(response_.Headers, h_ => h_.Key, h_ => h_.Value);
                        foreach (var item_ in response_.Content.Headers)
                            headers_[item_.Key] = item_.Value;

                        var status_ = ((int)response_.StatusCode).ToString();
                        if (status_ == "200")
                        {
                            var responseData_ = await response_.Content.ReadAsStringAsync().ConfigureAwait(false);
                            var result_ = default(System.Collections.ObjectModel.ObservableCollection<MetaData2017>);
                            try
                            {
                                result_ = Newtonsoft.Json.JsonConvert.DeserializeObject<System.Collections.ObjectModel.ObservableCollection<MetaData2017>>(responseData_);
                                return result_;
                            }
                            catch (System.Exception exception)
                            {
                                throw new Swagger.SwaggerException("Could not deserialize the response body.", status_, responseData_, headers_, exception);
                            }
                        }
                        else
                        if (status_ != "200" && status_ != "204")
                        {
                            var responseData_ = await response_.Content.ReadAsStringAsync().ConfigureAwait(false);
                            throw new Swagger.SwaggerException("The HTTP status code of the response was not expected (" + (int)response_.StatusCode + ").", status_, responseData_, headers_, null);
                        }

                        return default(System.Collections.ObjectModel.ObservableCollection<MetaData2017>);
                    }
                    finally
                    {
                        if (response_ != null)
                            response_.Dispose();
                    }
                }
            }
            finally
            {
                if (client_ != null)
                    client_.Dispose();
            }
        }
        #endregion

        private static CultureInfo _Culture = CultureInfo.InvariantCulture;
        private static string FirstUpperCase(string name)
        {
            if (string.IsNullOrEmpty(name)) return name;
            string s = name.ToLower(_Culture);
            string first = s.Substring(0, 1);
            string rest = string.Empty;
            if (s.Length > 1)
                rest = s.Substring(1);
            first = first.ToUpper(_Culture);
            return first + rest;
        }
        private static string FirstLowerCase(string name)
        {
            if (string.IsNullOrEmpty(name)) return name;
            return name.ToLower(_Culture);
        }

        public Swagger.PagedResultOfContent GetThreadListByForumId(Guid forumId, string forumShortName, string localeName, string[] shortNames, DateTime? since, /*ThreadFilter[] threadFilters,*/ ThreadSortOrder? sortOrder, SortDirection? sortDirection, int startRow, int maxRows, AdditionalThreadDataOptions additionalThreadDataOptions)
        {
            var c = new Swagger.ThreadsClient();
            var filter = $"(f0 eq '{FirstUpperCase(forumShortName)}' or f0 eq '{FirstLowerCase(forumShortName)}')";
            filter += $" and languagelocale eq '{localeName}'";
            if (shortNames != null && shortNames.Length > 0)
            {
                filter += $" and f1 eq '{shortNames[0]}'";
                if (shortNames.Length > 1)
                {
                    filter += $" and f2 eq '{shortNames[1]}'";
                }
            }
            if (since.HasValue)
            {
                DateTimeOffset dto = since.Value;
                filter += " and modifieddate gt " + dto.ToString("o", CultureInfo.InvariantCulture);
            }
            var order = "modifieddate desc";

            Log?.Invoke($"GetThreadListByForumId: filter={filter}, order={order}");
            try
            {
                var res = c.QueryAsyncAsync(filter, order, maxRows, startRow - 1, System.Threading.CancellationToken.None).Result;
                return res;
            }
            catch(AggregateException ae)
            {
                LogExp?.Invoke("GetThreadListByForumId", ae.InnerException);
                throw ae;
            }
        }

        public Swagger.Message GetMessage(Guid threadId, Guid messageId, AdditionalMessageDataOptions additionalMessageDataOptions)
        {
            var c = new Swagger.MessageClient();
            Log?.Invoke($"GetMessage: threadId={threadId}, messageId={messageId}");
            try
            {
                return c.GetAsync(threadId, messageId).Result;
            }
            catch(AggregateException ae)
            {
                LogExp?.Invoke("GetMessage", ae.InnerException);
                throw ae.InnerException;
            }
        }

        public Swagger.Content GetThread(Guid threadId, AdditionalMessageDataOptions additionalMessageDataOptions)
        {
            var c = new Swagger.ThreadsClient();
            Log?.Invoke($"GetThread: threadId={threadId}");
            try
            {
                return c.GetAsyncAsync(threadId).Result;
            }
            catch (AggregateException ae)
            {
                LogExp?.Invoke("GetThread", ae.InnerException);
                throw ae.InnerException;
                throw;
            }
        }

        public Swagger.PagedResultOfIResource GetMessageListByThreadId(Guid threadId, MessageSortOrder? sortOrder, SortDirection? sortDir, int startRow, int maxRows, AdditionalMessageDataOptions additionalMessageDataOptions)
        {
            var c = new Swagger.MessageClient();
            try
            {
                Log?.Invoke($"GetMessageListByThreadId: threadId={threadId}");
                var res = c.GetListAsync(threadId, null, startRow - 1, maxRows, null, Swagger.Direction._0).Result;
                return res;
            }
            catch(AggregateException ae)
            {
                LogExp?.Invoke("GetMessageListByThreadId", ae.InnerException);
            }
            catch (Exception exp)
            {
                LogExp?.Invoke("GetMessageListByThreadId", exp);
            }

            var res3 = new Swagger.PagedResultOfIResource();
            return res3;
        }
        
        public Guid? AddMessage(Guid threadId, Guid? parentId, string messageText)
        {
            var c = new Swagger.MessageClient();
            var m = new Swagger.Message();
            m.ContentKey = threadId;
            if (threadId != parentId)
            {
                m.ReplyToMessageKey = parentId;
            }
            m.Text = messageText;
            try
            {
                var res = c.CreateAsyncAsync(m).Result;
                return res.Id;
            }
            catch (AggregateException ae)
            {
                LogExp?.Invoke("GetMessageListByThreadId", ae.InnerException);
                throw ae;
            }
        }

        public void UpdateAuthTicket(string ticket)
        {
            AuthTicket = ticket;
        }
    }

    namespace Swagger
    {
        partial class ForumClient
        {
            partial void PrepareRequest(System.Net.Http.HttpClient request, string url)
            {
                string ticket = HttpUtility.UrlDecode(SwaggerAccess.AuthTicket);
                request.DefaultRequestHeaders.TryAddWithoutValidation("live_connect_access_token", ticket); // SwaggerAccess.AuthTicket);
            }
        }
        partial class ThreadsClient
        {
            partial void PrepareRequest(System.Net.Http.HttpClient request, string url)
            {
                string ticket = HttpUtility.UrlDecode(SwaggerAccess.AuthTicket);
                request.DefaultRequestHeaders.TryAddWithoutValidation("live_connect_access_token", ticket); // SwaggerAccess.AuthTicket);
            }
        }

        partial class MessageClient
        {
            partial void PrepareRequest(System.Net.Http.HttpClient request, string url)
            {
                string ticket = HttpUtility.UrlDecode(SwaggerAccess.AuthTicket);
                request.DefaultRequestHeaders.TryAddWithoutValidation("live_connect_access_token", ticket); // SwaggerAccess.AuthTicket);
            }
        }
    }

    public class Forum2017
    {
        public Guid Id => ForumKey;
        public Guid ForumKey { get; set; }
        public string LocaleName => Locale;
        public string Locale { get; set; }
        public string ShortName { get; set; }
        public string DisplayName { get; set; }

    }

    public class MetaData2017
    {
        public Guid Id { get; set; }
        public Guid ParentId { get; set; }
        public string ShortName { get; set; }

        public string DisplayName { get; set; }

        public int Type { get; set; }

        public int Level { get; set; }

        public int Order { get; set; }

        public List<MetaData2017> Children { get; set; } = new List<MetaData2017>();

    }

}
