﻿using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Text;

namespace CommunityBridge2.LiveConnect.Internal.Operations
{
  internal abstract class WebOperation : Operation
    {
        #region Constructors

        protected WebOperation(Uri url, string body, SynchronizationContextWrapper syncContext)
            : base(syncContext)
        {
            Debug.Assert(url != null, "url must not be null.");

            this.Url = url;
            this.Body = body;
        }

        #endregion

        #region Properties

        public string Body { get; internal set; }

        public WebRequest Request { get; internal set; }

        public Uri Url { get; internal set; }

        #endregion

        #region Public Methods

        public override void Cancel()
        {
            if (this.Status == OperationStatus.Cancelled || this.Status == OperationStatus.Completed)
            {
                // no-op
                return;
            }

            this.Status = OperationStatus.Cancelled;

            if (this.Request != null)
            {
                this.Request.Abort();
            }
            else
            {
                this.OnCancel();
            }
        }

        #endregion

        #region Protected Methods

        /// <summary>
        /// Callback used for WebRequest.BeginGetRequestStream.
        /// </summary>
        protected virtual void OnGetRequestStreamCompleted(IAsyncResult ar)
        {
            if (ar.IsCompleted)
            {
                try
                {
                    using (Stream requestStream = this.Request.EndGetRequestStream(ar))
                    {
                        if (!string.IsNullOrEmpty(this.Body))
                        {
                            byte[] dataInBytes = Encoding.UTF8.GetBytes(this.Body);
                            requestStream.Write(dataInBytes, 0, dataInBytes.Length);
                        }
                    }

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
                catch (IOException)
                {
                    this.OnWebResponseReceived(null);
                }
            }
        }

        /// <summary>
        /// Callback used for WebRequest.BeginGetResponse.
        /// </summary>
        protected void OnGetResponseCompleted(IAsyncResult ar)
        {
            if (ar.IsCompleted)
            {
                try
                {
                    this.OnWebResponseReceived(this.Request.EndGetResponse(ar));
                }
                catch (WebException exception)
                {
                    if (exception.Status == WebExceptionStatus.RequestCanceled)
                    {
                        this.OnCancel();
                    }
                    else
                    {
                        using (exception.Response)
                        {
                            this.OnWebResponseReceived(exception.Response);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Abstract method that is called when a WebResponse was received from the server.
        /// The response could be successful or contain an error or null on an exception.
        /// </summary>
        /// <param name="response">The WebResponse from the server or null if their was an IOException.</param>
        protected abstract void OnWebResponseReceived(WebResponse response);

        #endregion
    }
}