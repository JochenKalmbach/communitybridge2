using System;
using System.Collections.Generic;

namespace CommunityBridge2.LiveConnect.Internal
{
  internal interface IServerResponseReaderObserver
    {
        void OnSuccessResponse(IDictionary<string, object> result, string rawResult);
        void OnErrorResponse(string code, string message);
        void OnInvalidJsonResponse(FormatException exception);
    }
}
