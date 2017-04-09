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

using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;

namespace CommunityBridge2.NNTPServer
{
    public class NntpServer : Server, IDisposable
    {
        private readonly DataProvider _dataProvider;
        private readonly Dictionary<int, Client> _clients = new Dictionary<int, Client>();
        private readonly bool _postingAllowed = true;
        private readonly object _perfCounterLock = new object();

        public bool ListGroupDisabled = false;

        public NntpServer(DataProvider dataProvider)
        {
            _dataProvider = dataProvider;
        }

        public NntpServer(DataProvider dataProvider, bool postingAllowed)
        {
            _dataProvider = dataProvider;
            _postingAllowed = postingAllowed;
        }

        //public bool DetailedErrorResponse { get; set; }

        private NntpCommand ClassifyCommand(Client client)
        {
            var data = client.Buffer.ToString();
            string commandText;
            var parameterText = string.Empty;

            NntpCommand nntpCommand;
            Command command;

            if (data.Trim().Length == 0)
            {
                Traces.NntpServerTraceEvent(TraceEventType.Warning, client, "ClassifyCommand: Command Empty!");
                //System.Diagnostics.Trace.WriteLine("Command empty");
                return new NntpCommandNotRecognised();
            }

            Traces.NntpServerTraceEvent(TraceEventType.Verbose, client, "ClassifyCommand: {0}", data);

            // seperate command name and parameters
            if (data.IndexOf(" ") > 0)
            {
                commandText = data.Substring(0, data.IndexOf(" "));
                parameterText = data.Substring(data.IndexOf(" ") + 1);
            }
            else
            {
                commandText = data.Trim();
            }

            //System.Diagnostics.Trace.WriteLine("Command text is " + commandText);
            //System.Diagnostics.Trace.WriteLine("Parameter text is " + parameterText);

            try
            {
                command = (Command) Enum.Parse(typeof (Command), commandText, true);
            }
            catch
            {
                Traces.NntpServerTraceEvent(TraceEventType.Warning, client,
                                             "ClassifyCommand: Command unknown: Command: {0}, Parameter: {1}",
                                             commandText, parameterText);
                return new NntpCommandNotRecognised();
            }

            switch (command)
            {
                case Command.ARTICLE:
                    nntpCommand = new NntpCommandArticle(client.GroupName)
                    {
                        ToClientEncoding = this.EncodingSend
                    };
                    break;

                case Command.LIST:
                    nntpCommand = new NntpCommandList();
                    break;

                case Command.GROUP:
                    nntpCommand = new NntpCommandGroup();
                    //client.GroupName = parameterText;
                    break;

                case Command.BODY:
                    nntpCommand = new NntpCommandBody(client.GroupName);
                    break;

                case Command.HEAD:
                    nntpCommand = new NntpCommandHead(client.GroupName)
                    {
                        ToClientEncoding = this.EncodingSend
                    };
                    break;

                case Command.HELP:
                    nntpCommand = new NntpCommandHelp();
                    break;

                case Command.IHAVE:
                    nntpCommand = new NntpCommandIHave();
                    break;

                case Command.LAST:
                    nntpCommand = new NntpCommandLast(client.GroupName);
                    break;

                case Command.NEWGROUPS:
                    nntpCommand = new NntpCommandNewGroups();
                    break;

                case Command.NEWNEWS:
                    nntpCommand = new NntpCommandNewNews();
                    break;

                case Command.NEXT:
                    nntpCommand = new NntpCommandNext(client.GroupName);
                    break;

                case Command.POST:
                    nntpCommand = new NntpCommandPost(_postingAllowed);
                    break;

                case Command.QUIT:
                    nntpCommand = new NntpCommandQuit();
                    break;

                case Command.SLAVE:
                    nntpCommand = new NntpCommandSlave();
                    break;

                case Command.DATE:
                    nntpCommand = new NntpCommandDate();
                    break;

                case Command.MODE:
                    nntpCommand =
                        new NntpCommandMode
                            {
                                PostingAllowed = _postingAllowed
                            };
                    break;

                case Command.STAT:
                    nntpCommand = new NntpCommandStat(client.GroupName);
                    break;

                case Command.XHDR:
                    nntpCommand = new NntpCommandXHdr(client.GroupName);
                    break;

                case Command.XOVER:
                    nntpCommand = new NntpCommandXOver(client.GroupName);
                    break;

                case Command.LISTGROUP:
                    if (ListGroupDisabled)
                    {
                        return new NntpCommandNotRecognised();
                    }
                    nntpCommand = new NntpCommandListGroup(client.GroupName);
                    break;

                case Command.AUTHINFO:
                    nntpCommand = new NntpCommandAuthInfo();
                    break;

                default:
                    nntpCommand = new NntpCommandNotRecognised();
                    break;
            }

            nntpCommand.Provider = _dataProvider;
            client.CommandParameters = parameterText;
            client.PreviousCommand = command;
            return nntpCommand;
        }

        protected override void ClientConnected(int clientNumber)
        {
          Traces.NntpServerTraceEvent(TraceEventType.Verbose, string.Format("[Client {0}] ClientConnected", clientNumber));
          lock (_clients)
            {
                _clients.Add(clientNumber, new Client(clientNumber));
            }

            // log connection
            PerfCounters.IncrementCounter(PerfCounterName.CurrentConnections);
            PerfCounters.IncrementCounter(PerfCounterName.TotalConnections);

            if (_postingAllowed)
            {
                SendData(GeneralResponses.ServerReadyPostingAllowed, clientNumber);
            }
            else 
            {
                SendData(GeneralResponses.ServerReadyPostingNotAllowed, clientNumber);
            }
        }

        protected override void ClientDisconnected(int clientNumber)
        {
            Traces.NntpServerTraceEvent(TraceEventType.Verbose, "[Client {0}] ClientDisconnected", clientNumber);

            //Client client = GetClient(clientNumber);
            //if (client != null &&
            //    client.AuthServerContext != null)
            //{
            //    client.AuthServerContext.Dispose();
            //}
            lock (_clients)
            {
                try
                {
                    _clients.Remove(clientNumber);
                }
                catch
                {
                    // continue
                    Traces.NntpServerTraceEvent(TraceEventType.Warning, "[Client {0}] ClientDisconnected: Unable to remove client", clientNumber);
                }
            }

            // log disconnection
            PerfCounters.DecrementCounter(PerfCounterName.CurrentConnections);
        }

        private Client GetClient(int clientNumber)
        {
            Client client = null;

            if (_clients != null)
            {
                lock (_clients)
                {
                    try
                    {
                        client = _clients[clientNumber];
                    }
                    catch
                    {
                        // continue
                        Traces.NntpServerTraceEvent(TraceEventType.Warning, "[Client {0}] GetClient: Unable to get client", clientNumber);
                    }
                }
            }

            return client;
        }

        protected override void DataReceived(string data, int clientNumber)
        {
            try
            {
                Stopwatch sw = new Stopwatch();

                sw.Start();

                NntpCommand nntpCommand = null;
                var client = GetClient(clientNumber);
                string response;

                if (client == null)
                {
                    throw new Exception("Client object for [" + clientNumber + "] is not available");
                }

                //LogMessage(clientNumber, client.AuthenticatedClientUsername, data, NO_LOGGING, string.Empty);
                //LogMessage(clientNumber, string.Empty, data, NO_LOGGING, string.Empty);

                // log receive data size
                PerfCounters.IncrementCounterBy(PerfCounterName.BytesReceived, data.Length);

                bool parseCommand = false;
                string additionalData = string.Empty;

                if (client.PreviousCommand == Command.POST)
                {
                    Traces.NntpServerTraceEvent(TraceEventType.Verbose, client, "DataReceived: Client is posting data");

                    //System.Diagnostics.Debug.WriteLine("Buffer: {0}", client.Buffer);
                    //System.Diagnostics.Debug.WriteLine("Recv: {0}", data);
                    // client is posting data, 
                    // keep buffering until dotted terminator reached
                    string postData = data;
                    /*
                    string ascSeq = string.Empty;
                    for (int i = 0; i < postData.Length; i++)
                    {
                        ascSeq += (int)postData[i] + " ";
                    }
                    //System.Diagnostics.Trace.WriteLine("ascSeq - " + ascSeq);
                    */

                    // Append the data to the internal buffer
                    client.Buffer.Append(postData);

                    // Then extract (at least) the last 5 chars (or min the number of currently received bytes) to determine the termination...
                    string lastChars = string.Empty;
                    if (client.Buffer.Length >= 5)
                    {
                        int len = Math.Max(postData.Length, 5);
                        lastChars = client.Buffer.ToString(client.Buffer.Length - len, len);
                    }

                    // Check for "termination" signal...
                    if (lastChars.IndexOf("\r\n.\r\n") >= 0)
                    {
                        //System.Diagnostics.Trace.WriteLine("Termination sequence sent");
                        var buf = client.Buffer.ToString();

                        // extract post data up to the dot terminator
                        client.Buffer = new StringBuilder(buf.Substring(0, buf.IndexOf("\r\n.\r\n")));

                        // grab any data received after the terminator
                        additionalData = buf.Substring(buf.IndexOf("\r\n.\r\n") + 5);
                        
                        // flag to say we wish to process the received data
                        parseCommand = true;
                    }
                    //else if (lastChars.StartsWith(".\r\n"))
                    //{
                    //    // WHAT does this mean???
                    //    // no post data in this request
                    //    postData = string.Empty;

                    //    // grab any data received after the terminator
                    //    additionalData = data.Substring(3);

                    //    // flag to say we wish to process the received data
                    //    parseCommand = true;
                    //}

                    //client.Buffer.Append(postData);

                    if (parseCommand)
                    {
                        //System.Diagnostics.Trace.WriteLine("Posting article");

                        // create a new command
                        nntpCommand = 
                            new NntpCommandPostData
                            {
                                Provider = _dataProvider
                            };
                        // TODO: nntpCommand.ClientUsername = client.AuthenticatedClientUsername;

                        // log command
                        PerfCounters.IncrementCounter(PerfCounterName.TotalNntpCommandPostData);

                        // ensure command executed in context of authenticated user
                        // TODO: client.ImpersonateClient();

                        Traces.NntpServerTraceEvent(TraceEventType.Verbose, client, "Received: {0}: {1}", nntpCommand.Command, client.Buffer.ToString());

                        // post the article
                        response = nntpCommand.Parse(client.Buffer.ToString(), null, client);

                        Traces.NntpServerTraceEvent(TraceEventType.Verbose, client, "Response: {0}", response);

                        // revert to service security context
                        // TODO: client.RevertImpersonation();

                        // send back success/fail message
                        SendData(response, clientNumber);

                        PerfCounters.IncrementCounter(PerfCounterName.CommandsProcessedPerSecond);

                        // clear down buffer & reset command
                        client.Buffer = new StringBuilder();
                        client.PreviousCommand = Command.NOTRECOGNISED;
                        nntpCommand = null;
                    }
                }
                else
                {
                    //System.Diagnostics.Trace.WriteLine("Client not posting data");
                    // client is not posting data,
                    // wait until CR-LF pair is reached
                    string postData = data;
                    if (postData.IndexOf("\r\n") >= 0)
                    {
                        //System.Diagnostics.Trace.WriteLine("CR-LF pair received");

                        // extract up to end of line
                        postData = postData.Substring(0, postData.IndexOf("\r\n"));

                        // obtain further data (should not normally be present)
                        additionalData = data.Substring(data.IndexOf("\r\n") + 2);

                        parseCommand = true;
                    }
                    client.Buffer.Append(postData);

                    if (parseCommand)
                    {
                        // determine type of command and extract parameters
                        nntpCommand = ClassifyCommand(client);

                        // clear down buffer
                        client.Buffer = new StringBuilder();
                    }
                }
                
                if (nntpCommand != null)
                {
                    // ensure command executed in context of authenticated user
                    // TODO: client.ImpersonateClient();

                    // execute the command
                    var dataSendInAction = false;
                    Action<string> streamWriter = p =>
                        {
                            SendData(p, client.ClientNumber);
                            dataSendInAction = true;
                            Traces.NntpServerTraceEvent(TraceEventType.Verbose, client, "Received: Response: {0}", p);
                        };

                    Traces.NntpServerTraceEvent(TraceEventType.Verbose, client, "Received: Command: {0}, Parameters: {1}", nntpCommand.Command, client.CommandParameters);

                    response = nntpCommand.Parse(client.CommandParameters, streamWriter, client);


                    // revert to service security context
                    // TODO: client.RevertImpersonation();

                    // log command
                    PerfCounterName perfCounterNameForCommand = GetTotalCounterFromCommand(client.PreviousCommand);
                    PerfCounters.IncrementCounter(perfCounterNameForCommand);

                    // client attempting to authenticate more than once
                    // then re-authenticate
                    if (client.PreviousCommand == Command.AUTHINFO &&
                        nntpCommand.AuthToken == null)
                    {
                        if (client.Authenticated)
                        {
                            // TODO: client.AuthServerContext.Dispose();
                            // TODO: client.AuthServerContext = null;
                            client.Authenticated = false;
                        }
                    }
                    
                    // steps 2 and 3 of the authentication process
                    if (client.PreviousCommand == Command.AUTHINFO && 
                        nntpCommand.AuthToken != null)
                    {
                        try
                        {
                            // TODO: ServerCredential serverCredential = new ServerCredential(Credential.Package.NTLM);
                            //if (client.AuthServerContext == null)
                            //{
                            //    client.AuthServerContext = new ServerContext(serverCredential, nntpCommand.AuthToken);
                            //    response = response.Replace("<token>", Convert.ToBase64String(client.AuthServerContext.Token));
                            //}
                            //else
                            //{
                                //client.AuthServerContext.Accept(nntpCommand.AuthToken);
                                client.Authenticated = true;
                                response = GeneralResponses.AuthenticationAccepted;
                            //    PerfCounters.IncrementCounter(PerfCounterName.ResponseAuthenticationAccepted);
                            //}
                        }
                        catch (Exception ex)
                        {
                            // error during authentication (e.g. access denied)
                            // return response to client
                            Traces.NntpServerTraceEvent(TraceEventType.Critical, client, "Failed to authenticate: {0}", Traces.ExceptionToString((ex)));
                            // TODO: client.AuthServerContext.Dispose();
                            // TODO: client.AuthServerContext = null;
                            client.Authenticated = false;
                            response = GeneralResponses.AccessDenied;
                            PerfCounters.IncrementCounter(PerfCounterName.ResponseAccessDenied);
                        }
                    }

                    //// if user not logged in, send back authenticated required
                    //// message
                    //if (client.PreviousCommand != Command.MODE &&
                    //    client.PreviousCommand != Command.AUTHINFO &&
                    //    (!client.Authenticated ||
                    //      (client.Authenticated &&
                    //       client.AuthenticatedClientUsername.Length == 0)))
                    //{
                    //    response = GeneralResponses.AuthenticationRequired;
                    //    PerfCounters.IncrementCounter(PerfCounterName.ResponseAuthenticationRequired);
                    //}

                    // send response to client
                    if (dataSendInAction == false)
                    {
                        if (string.IsNullOrEmpty(response) == false)
                        {
                            SendData(response, clientNumber);

                            Traces.NntpServerTraceEvent(TraceEventType.Verbose, client, "Received: Response: {0}", response);


                            PerfCounters.IncrementCounter(PerfCounterName.CommandsProcessedPerSecond);

                            // log sent data size
                            PerfCounters.IncrementCounterBy(PerfCounterName.BytesSent, response.Length);
                        }
                        else
                        {
                            throw new Exception("No data returned from parsed command [" + client.PreviousCommand + "]");
                        }
                    }
                    

                    // adjust client state after command execution
                    switch (client.PreviousCommand)
                    {
                        case Command.POST:
                            if (nntpCommand.PostCancelled)
                            {
                                Trace.WriteLine("Posting is not allowed, reset status on client object");
                                client.PreviousCommand = Command.NOTRECOGNISED;
                            }
                            break;

                        case Command.QUIT:
                            DisconnectClient(clientNumber);
                            break;
                    }
                }

                // continue processing if needed
                // additional commands may follow the command 
                // or post data just processed
                if (additionalData.Trim().Length > 0)
                {
                    DataReceived(additionalData, clientNumber);
                }

                sw.Stop();

                lock (_perfCounterLock)
                {
                    PerfCounters.IncrementCounterBy(PerfCounterName.AverageProcessingTime, sw.ElapsedTicks);
                    PerfCounters.IncrementCounter(PerfCounterName.AverageProcessingTimeBase);
                }
            }
            catch (Exception ex)
            {
              Traces.NntpServerTraceEvent(TraceEventType.Critical, "[Client {0}] DataReceived failed: {1}", clientNumber, Traces.ExceptionToString(ex));
                if (clientNumber > 0)
                {
                    PerfCounters.IncrementCounter(PerfCounterName.ResponseProgramFault);
                    // INFO: Reply with more specific error message:

                    //if (DetailedErrorResponse)
                    //{

                    var resp = string.Format(
                        "503 program fault - command not performed {0}\r\n",
                        GetErrorResponseFromExeption(ex));
                    SendData(resp, clientNumber);
                    //}
                    //else
                    //{
                    //    SendData(GeneralResponses.ProgramFault, clientNumber);
                    //}
                }
            }
        }

        public static string GetErrorResponseFromExeption(Exception ex)
        {
            var ae = ex as AggregateException;
            if (ae != null)
            {
                if (ae.InnerException != null)
                {
                    ex = ae.InnerException;
                }
            }
            var expTyp = ex.GetType();

            // Restrict the message to 250 chars..
            var expStr = ex.Message.Substring(0, Math.Min(250, ex.Message.Length));
            if (expTyp != null)
                expStr = expTyp.FullName + ": " + expStr;

            // Remove CR/LF
            expStr = expStr.Replace("\n", string.Empty).Replace("\r", string.Empty).Replace("\t", string.Empty);
            return expStr;
        }


        private static PerfCounterName GetTotalCounterFromCommand(Command command)
        {
            switch (command)
            {
                case Command.ADHOC:
                    return PerfCounterName.TotalNntpCommandAdhoc;
                case Command.ARTICLE:
                    return PerfCounterName.TotalNntpCommandArticle;
                case Command.AUTHINFO:
                    return PerfCounterName.TotalNntpCommandAuthInfo;
                case Command.BODY:
                    return PerfCounterName.TotalNntpCommandBody;
                case Command.ENDOFDATA:
                    return PerfCounterName.TotalNntpCommandEndOfData;
                case Command.GROUP:
                    return PerfCounterName.TotalNntpCommandGroup;
                case Command.HEAD:
                    return PerfCounterName.TotalNntpCommandHead;
                case Command.HELP:
                    return PerfCounterName.TotalNntpCommandHelp;
                case Command.IHAVE:
                    return PerfCounterName.TotalNntpCommandIHave;
                case Command.LAST:
                    return PerfCounterName.TotalNntpCommandLast;
                case Command.LIST:
                    return PerfCounterName.TotalNntpCommandList;
                case Command.MODE:
                    return PerfCounterName.TotalNntpCommandMode;
                case Command.NEWGROUPS:
                    return PerfCounterName.TotalNntpCommandNewgroups;
                case Command.NEWNEWS:
                    return PerfCounterName.TotalNntpCommandNewNews;
                case Command.NEXT:
                    return PerfCounterName.TotalNntpCommandNext;
                case Command.NOTRECOGNISED:
                    return PerfCounterName.TotalNntpCommandNotRecognised;
                case Command.POST:
                    return PerfCounterName.TotalNntpCommandPost;
                case Command.POSTDATA:
                    return PerfCounterName.TotalNntpCommandPostData;
                case Command.QUIT:
                    return PerfCounterName.TotalNntpCommandQuit;
                case Command.SLAVE:
                    return PerfCounterName.TotalNntpCommandSlave;
                case Command.STAT:
                    return PerfCounterName.TotalNntpCommandStat;
                case Command.SYNTAXERROR:
                    return PerfCounterName.TotalNntpCommandSyntaxError;
                case Command.XHDR:
                    return PerfCounterName.TotalNntpCommandXHdr;
                case Command.XOVER:
                    return PerfCounterName.TotalNntpCommandXOver;
                default:
                    return PerfCounterName.TotalNntpCommandNotRecognised;
            }
        }

        #region IDisposable Members

        private bool _disposed;

        private void Dispose(bool disposing)
        {
            if (!(_disposed))
            {
                if (disposing) 
                {
                    if (_dataProvider != null) 
                    {
                        _dataProvider.Dispose(); 
                    }
                    CloseSockets();
                }
            } 
            _disposed = true;
        }  
        
        public new void Dispose()
        { 
            Dispose(true);
            GC.SuppressFinalize(this);
        } 
        
        #endregion
    }
}
