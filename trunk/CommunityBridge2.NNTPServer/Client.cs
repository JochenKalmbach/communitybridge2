// QinetiQ SharePoint NNTP Server
// http://spnews.codeplex.com
// ---------------------------------------------------------------------------
// Last updated: Sep 2009
// ---------------------------------------------------------------------------
// 
// Microsoft Public License (Ms-PL)
//
// This license governs use of the accompanying software. If you use the software, you
// accept this license. If you do not accept the license, do not use the software.
//
// 1. Definitions
// The terms "reproduce," "reproduction," "derivative works," and "distribution" have the
// same meaning here as under U.S. copyright law.
// A "contribution" is the original software, or any additions or changes to the software.
// A "contributor" is any person that distributes its contribution under this license.
// "Licensed patents" are a contributor's patent claims that read directly on its contribution.
//
// 2. Grant of Rights
// (A) Copyright Grant- Subject to the terms of this license, including the license conditions and limitations in section 3, each contributor grants you a non-exclusive, worldwide, royalty-free copyright license to reproduce its contribution, prepare derivative works of its contribution, and distribute its contribution or any derivative works that you create.
// (B) Patent Grant- Subject to the terms of this license, including the license conditions and limitations in section 3, each contributor grants you a non-exclusive, worldwide, royalty-free license under its licensed patents to make, have made, use, sell, offer for sale, import, and/or otherwise dispose of its contribution in the software or derivative works of the contribution in the software.
//
// 3. Conditions and Limitations
// (A) No Trademark License- This license does not grant you rights to use any contributors' name, logo, or trademarks.
// (B) If you bring a patent claim against any contributor over patents that you claim are infringed by the software, your patent license from such contributor to the software ends automatically.
// (C) If you distribute any portion of the software, you must retain all copyright, patent, trademark, and attribution notices that are present in the software.
// (D) If you distribute any portion of the software in source code form, you may do so only under this license by including a complete copy of this license with your distribution. If you distribute any portion of the software in compiled or object code form, you may only do so under a license that complies with this license.
// (E) The software is licensed "as-is." You bear the risk of using it. The contributors give no express warranties, guarantees or conditions. You may have additional consumer rights under your local laws which this license cannot change. To the extent permitted under your local laws, the contributors exclude the implied warranties of merchantability, fitness for a particular purpose and non-infringement.
//

using System.Text;

namespace CommunityBridge2.NNTPServer
{
    public class Client
    {
        //private ServerContext _authServerContext = null;
        //private static readonly ILog _log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        /// <summary>
        /// Creates a new Client instance
        /// </summary>
        /// <param name="clientNumber">The numeric reference for this client connection</param>
        public Client(int clientNumber)
        {
            Buffer = new StringBuilder();
            CommandParameters = string.Empty;
            GroupName = string.Empty;
            ArticleReference = string.Empty;
            PreviousCommand = Command.NOTRECOGNISED;
            ClientNumber = clientNumber;
        }

        /// <summary>
        /// Creates a new Client instance
        /// </summary>
        /// <param name="clientNumber">The numeric reference for this client connection</param>
        /// <param name="articleReference">Article reference</param>
        /// <param name="groupName">Newsgroup name</param>
        public Client(int clientNumber, string groupName, string articleReference)
        {
            Buffer = new StringBuilder();
            CommandParameters = string.Empty;
            PreviousCommand = Command.NOTRECOGNISED;
            ClientNumber = clientNumber;
            GroupName = groupName;
            ArticleReference = articleReference;
        }

        /// <summary>
        /// Gets or sets the parameters for an NNTP command
        /// </summary>
        public string CommandParameters { get; set; }

        /// <summary>
        /// Gets or sets a flag to indicate that the user has been authenticated
        /// </summary>
        public bool Authenticated { get; set; }

        /// <summary>
        /// Gets or sets the previous NNTP command
        /// </summary>
        public Command PreviousCommand { get; set; }

        /// <summary>
        /// Gets or sets the numeric reference for this client connection
        /// </summary>
        public int ClientNumber { get; set; }

        /// <summary>
        /// Gets or sets the article reference (this only contains the NUMBER form; and never the id form, because only the number form will be used in NEXT, LAST, STAT...)
        /// </summary>
        public string ArticleReference { get; set; }

        /// <summary>
        /// Gets or sets the newsgroup name
        /// </summary>
        public string GroupName { get; set; }

        ///// <summary>
        ///// Gets or sets the SSPI server context
        ///// </summary>
        //public ServerContext AuthServerContext
        //{
        //    get
        //    {
        //        return _authServerContext;
        //    }
        //    set
        //    {
        //        _authServerContext = value;
        //    }
        //}

        /// <summary>
        /// Gets or sets the received data buffer
        /// </summary>
        public StringBuilder Buffer { get; set; }

        ///// <summary>
        ///// Gets the authenticated user name
        ///// </summary>
        //public string AuthenticatedClientUsername
        //{
        //    get
        //    {
        //        if (_authServerContext != null &&
        //            _authServerContext.Credential != null &&
        //            _authenticatedClientUsername.Length == 0 &&
        //            Authenticated)
        //        {
        //            try
        //            {
        //                _authServerContext.ImpersonateClient();
        //                _authenticatedClientUsername = WindowsIdentity.GetCurrent().Name;
        //                _authServerContext.RevertImpersonation();
        //            }
        //            catch (Exception ex)
        //            {
        //                Logging.Error("Failed to obtain authenticated client username", ex);
        //            }
        //        }

        //        return _authenticatedClientUsername;
        //    }
        //}

        ///// <summary>
        ///// Sets the identity of the current thread to the authenticated user
        ///// </summary>
        //public void ImpersonateClient()
        //{
        //    if (_authServerContext != null &&
        //        _authServerContext.Credential != null &&
        //        Authenticated)
        //    {
        //        _authServerContext.ImpersonateClient();
        //    }
        //}

        ///// <summary>
        ///// Sets the identity of the current thread back to the service account identity
        ///// </summary>
        //public void RevertImpersonation()
        //{
        //    if (_authServerContext != null &&
        //        _authServerContext.Credential != null &&
        //        Authenticated)
        //    {
        //        _authServerContext.RevertImpersonation();
        //    }
        //}
    }
}
