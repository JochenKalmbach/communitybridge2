﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using CommunityBridge2.LiveConnect.Internal.Serialization;
using CommunityBridge2.LiveConnect.Public;

namespace CommunityBridge2.LiveConnect.Internal.Operations
{
  /// <summary>
    /// Represents a single operation that makes a web request to the API service.
    /// </summary>
    internal class ApiOperation : WebOperation
    {
        #region Class member variables

        internal const string ContentTypeJson = @"application/json;charset=UTF-8";
        internal const string AuthorizationHeader = "Authorization";
        internal const string LibraryHeader = "X-HTTP-Live-Library";
        internal const string ApiError = "error";
        internal const string ApiErrorCode = "code";
        internal const string ApiErrorMessage = "message";
        internal const string ApiClientErrorCode = "client_error";
        internal const string ApiServerErrorCode = "server_error";
        internal const string MoveRequestBodyTemplate = @"{{ ""destination"" : ""{0}"" }}";

        #endregion

        #region Instance member variables

        private bool refreshed;

        #endregion

        #region Constructors

        /// <summary>
        /// Constructs a new ApiOperation object.
        /// </summary>
        public ApiOperation(
            LiveConnectClient client, 
            Uri url, 
            ApiMethod method, 
            string body, 
            SynchronizationContextWrapper syncContext)
            : base(url, body, syncContext)
        {
            this.Method = method;
            this.LiveClient = client;
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets the reference to the LiveConnectClient object.
        /// </summary>
        public LiveConnectClient LiveClient { get; private set; }

        /// <summary>
        /// Gets the API method this operation represents.
        /// </summary>
        public ApiMethod Method { get; private set; }

        /// <summary>
        /// Gets and sets the operation completed callback delegate.
        /// </summary>
        public Action<LiveOperationResult> OperationCompletedCallback { get; set; }

        #endregion

        #region Internal Methods

        /// <summary>
        /// Serializes the request body.
        /// </summary>
        internal static string SerializePostBody(IDictionary<string, object> body)
        {
            if (body == null)
            {
                throw new ArgumentNullException("body");
            }

            var sb = new StringBuilder();
            using (var sw = new StringWriter(sb))
            {
                var jw = new JsonWriter(sw);
                jw.WriteValue(body);
                sw.Flush();
            }

            return sb.ToString();
        }

        #endregion

        #region Protected Methods

        /// <summary>
        /// Parses a string response body and creates a LiveOperationResult object from it.
        /// </summary>
        protected LiveOperationResult CreateOperationResultFrom(string responseBody)
        {
            if (string.IsNullOrEmpty(responseBody))
            {
                if (this.Method != ApiMethod.Delete)
                {
                    var error = new LiveConnectException(
                        ApiClientErrorCode, 
                        ResourceHelper.GetString("NoResponseData"));
                    return new LiveOperationResult(error, false);
                }

                return new LiveOperationResult(new DynamicDictionary(), responseBody);
            }

            return LiveOperationResult.FromResponse(responseBody);
        }

        /// <summary>
        /// Parses the WebResponse's body and creates a LiveOperationResult object from it.
        /// </summary>
        protected LiveOperationResult CreateOperationResultFrom(WebResponse response)
        {
            LiveOperationResult opResult;

            bool nullResponse = (response == null);
            try
            {
                Stream responseStream = (!nullResponse) ? response.GetResponseStream() : null;
                if (nullResponse || responseStream == null)
                {
                    var error = new LiveConnectException(
                        ApiOperation.ApiClientErrorCode, 
                        ResourceHelper.GetString("ConnectionError"));

                    opResult = new LiveOperationResult(error, false);
                }
                else
                {
                    using (var sr = new StreamReader(responseStream))
                    {
                        string rawResult = sr.ReadToEnd();
                        opResult = this.CreateOperationResultFrom(rawResult);
                    }
                }
            }
            finally
            {
                if (!nullResponse)
                {
                    ((IDisposable)response).Dispose();
                }
            }

            return opResult;
        }

        /// <summary>
        /// Overwrites the base OnExecute to refresh access token if neccessary.
        /// </summary>
        protected override void OnExecute()
        {
            if (this.PrepareRequest())
            {
                try
                {
                    this.Request.BeginGetResponse(this.OnGetResponseCompleted, null);
                }
                catch (WebException exception)
                {
                    if (exception.Status == WebExceptionStatus.RequestCanceled)
                    {
                        this.OnCancel();
                    }
                    else
                    {
                        this.OnWebResponseReceived(exception.Response);
                    }
                }
            }
        }

        /// <summary>
        /// Called when the operation is cancelled.
        /// </summary>
        protected override void OnCancel()
        {
            this.OnOperationCompleted(new LiveOperationResult(null, true));
        }

        /// <summary>
        /// Calls the OperationCompletedCallback delegate.
        /// This method is called when the ApiOperation is completed.
        /// </summary>
        protected void OnOperationCompleted(LiveOperationResult opResult)
        {
            Action<LiveOperationResult> callback = this.OperationCompletedCallback;
            if (callback != null)
            {
                callback(opResult);
            }
        }

        /// <summary>
        /// Called when the operation has a WebResponse from the server to handle.
        /// </summary>
        protected override void OnWebResponseReceived(WebResponse response)
        {
            this.OnOperationCompleted(this.CreateOperationResultFrom(response));
        }

        /// <summary>
        /// Prepares the web request. Sets up the correct method, headers, etc.
        /// </summary>
        protected bool PrepareRequest()
        {
            if (!this.RefreshTokenIfNeeded())
            {
                string httpMethod;

                switch (this.Method)
                {
                    case ApiMethod.Upload:
                        httpMethod = HttpMethods.Put;
                        break;

                    case ApiMethod.Download:
                        httpMethod = HttpMethods.Get;
                        break;

                    default:
                        httpMethod = this.Method.ToString().ToUpperInvariant();
                        break;
                }

                this.Request = WebRequestFactory.Current.CreateWebRequest(this.Url, httpMethod);
                this.Request.Headers[ApiOperation.AuthorizationHeader] =
                    AuthConstants.BearerTokenType + " " + this.LiveClient.Session.AccessToken;
                this.Request.Headers[ApiOperation.LibraryHeader] = Platform.GetLibraryHeaderValue();

                if (!string.IsNullOrEmpty(this.Body))
                {
                    this.Request.ContentType = ApiOperation.ContentTypeJson;
                }
                 
                return true;
            }

            return false;
        }

        /// <summary>
        /// Checks if the access token is still valid.  If not, refreshes the token.
        /// </summary>
        protected bool RefreshTokenIfNeeded()
        {
            bool needsRefresh = false;
            LiveAuthClient authClient = this.LiveClient.Session.AuthClient;
            if (!this.refreshed && authClient != null)
            {
                this.refreshed = true;

                needsRefresh = authClient.RefreshToken(this.OnRefreshTokenOperationCompleted);
            }

            return needsRefresh;
        }

        #endregion

        #region Private methods

        /// <summary>
        /// Handles the refresh token completed event.  Restarts the operation.
        /// </summary>
        private void OnRefreshTokenOperationCompleted(LiveLoginResult result)
        {
            if (result.Status == LiveConnectSessionStatus.Connected)
            {
                this.LiveClient.Session = result.Session;
            }

            // We will attempt to perform the operation even if refresh fails.
            this.InternalExecute();
        }

        #endregion
    }
}
