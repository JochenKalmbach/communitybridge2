using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using CommunityBridge2.WebServiceDataSource.Forums;

namespace CommunityBridge2.WebServiceDataSource
{
    /// <summary>
    /// The synchronization service for the MSDN Forums
    /// </summary>
    public class MicrosoftForumsServiceProvider
    {
        // The login ticket from passport
        private string _passportTicket;

        // The forum service used to conenct to the web forums
        private readonly ForumsServiceClient _service;

        public string EndpointConfigurationName;

        /// <summary>
        /// Create the Microsoft Forums Service provider
        /// </summary>
        public MicrosoftForumsServiceProvider()
        {
          _service = new ForumsServiceClient();
          EndpointConfigurationName = _service.Endpoint.Name;

            //// This would be overridden in the application by DefaultSyncBatchSize in app.config
            BatchSize = 200;
        }

        /// <summary>
        /// Create the Microsoft Forums Service provider and pass in the ticket needed to login to the service with
        /// </summary>
        /// <remarks>If ticket is invalid (null, empty are incorrect) then you can still connect
        /// but some functions will throw unauthorized exception</remarks>
        /// <param name="endpointConfiguration"></param>
        public MicrosoftForumsServiceProvider(string endpointConfiguration)
        {
            EndpointConfigurationName = endpointConfiguration;
          _service = new ForumsServiceClient(endpointConfiguration);

            // This would be overridden in the application by DefaultSyncBatchSize in app.config
            BatchSize = 200;
        }

        /// <summary>
        /// The Uri that this service provider is connected to
        /// </summary>
        public Uri Uri
        {
            get
            {
                var forumService = _service as ClientBase<IForumsService>;
                if (forumService != null)
                {
                    return forumService.Endpoint.Address.Uri;
                }

                return null;
            }
        }

        /// <summary>
        /// The Name of the endpoint that this service provider is connected to
        /// </summary>
        public string EndpointName
        {
            get
            {
                var forumService = _service as ClientBase<IForumsService>;
                if (forumService != null)
                {
                    return forumService.Endpoint.Name;
                }

                return string.Empty;
            }
        }

        /// <summary>
        /// If this is <c>true</c>, this web service will not used. Can be changed viy user settings on the fly
        /// </summary>
        public bool IsDisabled { get; set; }


        /// <summary>
        /// Get or set the passport ticket used to authenticate the user
        /// </summary>
        public string AuthenticationTicket
        {
            get
            {
                return _passportTicket;
            }
            set
            {
                _passportTicket = value;
                // Check if this is a wcf service, if so add new endpoint
                var forumService = _service as ClientBase<IForumsService>;
                if (forumService != null)
                {
                    if (forumService.Endpoint.Behaviors.Contains(typeof(AuthenticationInspector)))
                    {
                        forumService.Endpoint.Behaviors.Remove(typeof(AuthenticationInspector));
                    }

                    forumService.Endpoint.Behaviors.Add(new AuthenticationInspector(_passportTicket));

                }
            }
        }

        /// <summary>
        /// The underlying Forum Service
        /// </summary>
        public IForumsService ForumsService
        {
            get
            {
                return _service;
            }
        }

        /// <summary>
        /// The size which we will pull batches of data from the server
        /// </summary>
        public int BatchSize
        {
            get;
            set;
        }

        /// <summary>
        /// Get a collection of all the supported brands
        /// </summary>
        /// <returns>collection of brands</returns>
        public IList<string> SupportedBrands
        {
            get
            {
                return ForumsService.GetActiveBrands();
            }
        }

        /// <summary>
        /// Get a collection of all the supported locales
        /// </summary>
        /// <returns>collection of locales</returns>
        public IList<KeyValuePair<int, string>> SupportedLocales
        {
            get
            {
                var supportedLocalesList = new List<KeyValuePair<int, string>>();
                var activeLocales = new List<string>(ForumsService.GetActiveLocales());

                foreach (var activeLocale in activeLocales)
                {
                    try
                    {
                        supportedLocalesList.Add(new KeyValuePair<int, string>(
                         System.Globalization.CultureInfo.GetCultureInfo(activeLocale).LCID, activeLocale));
                        
                    }
                    catch(ArgumentException)
                    {
                        // Ignore any locale that throws System.ArgumentException because they are not supported 
                        // on this platform. Example: "en-IN" is not supported on pre-Vista operating systems
                    }
                }
                return supportedLocalesList;
            }
        }

        #region Private Methods

        /// <summary>
        /// Repeatedly execute a function that has paging in batches and covnerts and concatenates the results.
        /// It will execute this method with new paging indicies until it returns
        /// less results than the batchSide
        /// </summary>
        /// <typeparam name="T">The type we are aggregating batches of</typeparam>
        /// <typeparam name="T2"></typeparam>
        /// <param name="converter"></param>
        /// <param name="generator">Function to call</param>
        /// <returns>Aggregated list of T</returns>
        private List<T2> BuildResultsInBatch<T, T2>(Func<T, T2> converter, Func<int, int, List<T>> generator)
        {
          int index = 0;
          var concatResults = new List<T2>();
          List<T> results;
          do
          {
            results = generator(index, BatchSize);
            if (results != null)
            {
                concatResults.AddRange(results.Select(converter));
              //concatResults.AddRange(results);
              index++;
            }

          } while (results != null && results.Count >= BatchSize);

          return concatResults;
        }

        private delegate TResult MyFunc<T1, T2, T3, TResult>(T1 a, T2 b, out T3 c);

        private List<T2> BuildResultsInBatch_<T, T2>(Func<T, T2> converter, MyFunc<int, int, bool, List<T>> generator, Action<IList<T2>> progressResult)
        {
            int index = 0;
            var concatResults = new List<T2>();
            bool finished;
            do
            {
                List<T> results = generator(index, BatchSize, out finished);
                if (results != null)
                {
                    IEnumerable<T2> convertedResults = results.Select(converter);
                    if (progressResult != null)
                        progressResult(convertedResults.ToList());
                    concatResults.AddRange(convertedResults);
                    index++;
                }

            } while (finished == false);

            return concatResults;
        }

        #endregion

        /// <summary>
        /// Gets a list of all the forums 
        /// </summary>
        /// <param name="brand"></param>
        /// <param name="converter"></param>
        /// <param name="progress"></param>
        /// <returns>List of Forums for the given brand and locale</returns>
        public IList<T> GetAvailableForums<T>(
            string brand,
            Func<ForumNewsGroup, T> converter,
            Action<int, int> progress)
            where T : class
        {
            if (string.IsNullOrEmpty(brand)) throw new ArgumentNullException("brand");

            return BuildResultsInBatch(converter,
                delegate(int index, int batchSize)
                {
                    //var results = ForumsService.GetAllForumsByBrand(brand, index, batchSize);
                    var results = ForumsService.GetAllForumNewsGroupsByBrand(brand, index, batchSize);
                    var res = new List<ForumNewsGroup>(results.Results);
                    if (progress != null)
                        progress((batchSize * index) + res.Count, results.TotalRecords);
                    return res;
                });
        }
        //public IList<T> GetAvailableForums2<T>(
        //    string brand,
        //    Func<Forum, T> converter,
        //    Action<int, int> progress)
        //    where T : class
        //{
        //    if (string.IsNullOrEmpty(brand)) throw new ArgumentNullException("brand");

        //    return BuildResultsInBatch(converter,
        //        delegate(int index, int batchSize)
        //        {
        //            var results = ForumsService.GetAllForumsByBrand(brand, index, batchSize);
        //            //var results = ForumsService.GetAllForumNewsGroupsByBrand(brand, index, batchSize);
        //            var res = new List<Forum>(results.Results);
        //            if (progress != null)
        //                progress((batchSize * index) + res.Count, results.TotalRecords);
        //            return res;
        //        });
        //}

        /// <summary>
        /// Gets a list of articles
        /// </summary>
        /// <param name="lastArticle"></param>
        /// <param name="loadBody"></param>
        /// <param name="converter"></param>
        /// <param name="progress"></param>
        /// <param name="forumId"></param>
        /// <param name="firstArticle"></param>
        /// <returns>List of Forums for the given brand and locale</returns>
        public IList<T> GetForumMessagesBriefForNntp<T>(
            Guid forumId,
            int firstArticle, 
            int lastArticle,
            bool loadBody,
            Func<BriefMessage, T> converter,
            Action<IList<T>> progress)
            where T : class
        {
            const int myBatchSize = 150;
            int lastFromIdx = -1;
            return BuildResultsInBatch_(converter,
                delegate(int index, int batchSize, out bool finished)
                {
                    finished = false;
                    int fromIdx = firstArticle + (index*myBatchSize);
                    int toIdx = Math.Min(fromIdx + myBatchSize-1, lastArticle);

                    if ((fromIdx > toIdx) || (fromIdx == lastFromIdx))
                    {
                        finished = true;
                        return null;
                    }

                    var results = ForumsService.GetForumMessagesBriefForNNTP(forumId, fromIdx, toIdx, loadBody);
                    var res = new List<BriefMessage>(results.Results);
                    lastFromIdx = fromIdx;

                    return res;
                }, 
                r =>
                    {
                        if (progress != null)
                            progress(r);
                    }
                );
        }

        public IList<T> GetAvailableForumSince<T>(
            string brand,
            DateTime createdSince,
            Func<ForumNewsGroup, T> converter,
            Action<int, int> progress)
            where T : class
        {
            if (string.IsNullOrEmpty(brand)) throw new ArgumentNullException("brand");

            return BuildResultsInBatch(converter,
                delegate(int index, int batchSize)
                {
                    var results = ForumsService.GetAllNewForumNewsGroupsByBrand(brand, index, batchSize, createdSince);
                    var res = new List<ForumNewsGroup>(results.Results);
                    if (progress != null)
                        progress((batchSize * index) + res.Count, results.TotalRecords);
                    return res;
                });
        }

        //public IList<BriefMessage> GetForumMessages(Guid forumId, bool loadBody, Action<int, int, IList<BriefMessage>> progress)
        //{
        //  return BuildResultsInBatch<BriefMessage>(
        //      delegate(int index, int batchSize)
        //      {
        //        var results = ForumsService.GetForumMessagesBrief(forumId, index, batchSize, loadBody);
        //        var res = new List<BriefMessage>(results.Results);
        //        if (progress != null)
        //          progress((batchSize * index) + res.Count, results.TotalRecords, res);
        //        return res;
        //      });
        //}

    }
}
