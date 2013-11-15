﻿using System;
using System.Net;
using CommunityBridge2.LiveConnect.Public;

namespace CommunityBridge2.LiveConnect.Internal.Operations
{
  internal class ApiWriteOperation : ApiOperation
    {
        #region Constructors

        public ApiWriteOperation(
            LiveConnectClient client, 
            Uri url, 
            ApiMethod method, 
            string body, 
            SynchronizationContextWrapper syncContext)
            : base(client, url, method, body, syncContext)
        {
        }

        #endregion

        #region Protected methods

        protected override void OnExecute()
        {
            if (this.PrepareRequest())
            {
                try
                {
                    this.Request.BeginGetRequestStream(this.OnGetRequestStreamCompleted, null);
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

        #endregion
    }
}
