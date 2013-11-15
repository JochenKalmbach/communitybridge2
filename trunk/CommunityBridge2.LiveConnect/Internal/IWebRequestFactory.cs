using System;
using System.Net;

namespace CommunityBridge2.LiveConnect.Internal
{
  /// <summary>
    /// An interface that allows test projects to override the outbound web request behavior.
    /// </summary>
    internal interface IWebRequestFactory
    {
        /// <summary>
        /// Create a WebRequest object.
        /// </summary>
        WebRequest CreateWebRequest(Uri url, string method);
    }
}
