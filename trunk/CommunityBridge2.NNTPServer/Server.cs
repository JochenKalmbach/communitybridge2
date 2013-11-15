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
using System.Net.Sockets;
using System.Net;
using System.Threading;

namespace CommunityBridge2.NNTPServer
{
    public abstract class Server : IDisposable
    {
        #region Declarations

        // This codepage will be used for reading the whle data.
        // The reason is, that *every* byte combination can be represented in this coding, because it is a SBCS.
        // This is for example not true for utf-8, because it is a MBCS.
        public const int DATA_RECV_ENCODING_CODE_PAGE = 1252; // Windows-1252  // old: 28591; // iso-8859-1 
        private readonly Dictionary<int, Socket> _workerSockets = new Dictionary<int, Socket>();
        private int _clients;
        private Socket _primarySocket;
        private AsyncCallback _workerReceiveCallBack;
        private AsyncCallback _workerSendCallBack;
        protected const int NullClient = -1;
        protected const int NoLogging = -2;

        #endregion

        #region Public Methods

        public void Start(int port, int pendingConnectionsLength, bool bindToWorld, out string errorString)
        {
            errorString = null;
            try
            {
                _primarySocket = new Socket(AddressFamily.InterNetwork,
                                            SocketType.Stream,
                                            ProtocolType.Tcp);

                IPEndPoint ipLocal;
                if (bindToWorld)
                    ipLocal = new IPEndPoint(IPAddress.Any, port);
                else
                    ipLocal = new IPEndPoint(IPAddress.Loopback, port);

                // bind to local address
                _primarySocket.Bind(ipLocal);

                // start listening
                _primarySocket.Listen(pendingConnectionsLength);

                // call back for client connections
                _primarySocket.BeginAccept(new AsyncCallback(OnClientConnect), null);
            }
            catch (SocketException se)
            {
                errorString = se.Message;
                Traces.NntpServerTraceEvent(System.Diagnostics.TraceEventType.Critical, "Server.Start failed: {0}", Traces.ExceptionToString(se));
            }
            catch (Exception ex)
            {
                // TODO: Logging.Error("Failed to establish master listener on port [" + port.ToString() + "]", ex);
                Traces.NntpServerTraceEvent(System.Diagnostics.TraceEventType.Critical, "Server.Start failed: {0}", Traces.ExceptionToString(ex));
                errorString = ex.Message;

                throw new Exception("Failed to establish master listener", ex);
            }
        }

        public void Stop()
        {
            CloseSockets();
        }

        #endregion

        #region Callbacks

        public AsyncCallback WorkerReceiveCallBack
        {
            get { return _workerReceiveCallBack; }
            set { _workerReceiveCallBack = value; }
        }

        public AsyncCallback WorkerSendCallBack
        {
            get { return _workerSendCallBack; }
            set { _workerSendCallBack = value; }
        }

        public void OnClientConnect(IAsyncResult ar)
        {
            int currentClient = 0;
            try
            {
                // get the worker socket for this connection
                Socket workerSocket = _primarySocket.EndAccept(ar);

                lock (_workerSockets)
                {
                    Interlocked.Increment(ref _clients);
                    currentClient = _clients;
                    _workerSockets.Add(currentClient, workerSocket);
                }

                ClientConnected(currentClient);
                WaitForData(workerSocket, currentClient);
            }
            catch (ObjectDisposedException)
            {
                //System.Diagnostics.Trace.WriteLine("#" + currentClient.ToString() + " - OnClientConnection: Socket has been closed");
                //Logging.Error("OnClientConnect dispose error", ex);
                return;
            }
            catch (SocketException se)
            {
                if (se.SocketErrorCode == SocketError.ConnectionReset) // connection reset by peer
                {
                    // remove worker socket of closed client
                    if (currentClient > 0)
                        DisconnectClient(currentClient);
                }
                else if (se.SocketErrorCode == SocketError.ConnectionAborted) // connection aborted
                {
                    // remove worker socket of closed client
                    if (currentClient > 0)
                        DisconnectClient(currentClient);
                }
                else
                {
                    Traces.NntpServerTraceEvent(System.Diagnostics.TraceEventType.Critical, "[Client {0}] Server.OnClientConnect failed: {1}", currentClient, Traces.ExceptionToString(se));
                }
            }
            catch (Exception ex)
            {
                Traces.NntpServerTraceEvent(System.Diagnostics.TraceEventType.Critical, "[Client {0}] Server.OnClientConnect failed: {1}", currentClient, Traces.ExceptionToString(ex));
            }
            finally
            {
                try
                {
                    // register for another connection
                    _primarySocket.BeginAccept(new AsyncCallback(OnClientConnect), null);
                }
                catch (ObjectDisposedException)
                {
                    // This occurs when the main socket will be closed
                }
            }
        }

        public void WaitForData(Socket socket, int clientNumber)
        {
            try
            {
                if (_workerReceiveCallBack == null)
                {
                    _workerReceiveCallBack = new AsyncCallback(OnDataReceived);
                }
                var socketPacket = new SocketPacket(socket, clientNumber);

                socket.BeginReceive(socketPacket.Buffer, 
                                    0,
                                    socketPacket.Buffer.Length,
                                    SocketFlags.None,
                                    _workerReceiveCallBack,
                                    socketPacket);
            }
            catch (ObjectDisposedException)
            {
                //System.Diagnostics.Trace.WriteLine("#" + clientNumber.ToString() + " - WaitForData: Socket has been closed");
                return;
            }
            catch (SocketException se)
            {
                if (se.SocketErrorCode == SocketError.ConnectionReset) // connection reset by peer
                {
                    // remove worker socket of closed client
                    DisconnectClient(clientNumber);
                }
                else if (se.SocketErrorCode == SocketError.ConnectionAborted) // connection aborted
                {
                    // remove worker socket of closed client
                    DisconnectClient(clientNumber);
                }
                else
                {
                    Traces.NntpServerTraceEvent(System.Diagnostics.TraceEventType.Critical, "[Client {0}] Server.WaitForData failed: {1}", clientNumber, Traces.ExceptionToString(se));
                }
                return;
            }
            catch (Exception ex)
            {
                Traces.NntpServerTraceEvent(System.Diagnostics.TraceEventType.Critical, "[Client {0}] Server.WaitForData failed: {1}", clientNumber, Traces.ExceptionToString(ex));
            }
        }

        public void OnDataSend(IAsyncResult ar)
        {
            var socketPacket = (SocketPacket)ar.AsyncState;

            try
            {
                socketPacket.ClientSocket.EndSend(ar);
            }
            catch (ObjectDisposedException)
            {
                //System.Diagnostics.Trace.WriteLine("#" + socketPacket.ClientNumber.ToString() + " - OnDataSend: Socket has been closed");
                return;
            }
            catch (SocketException se)
            {
                if (se.SocketErrorCode == SocketError.ConnectionReset) // connection reset by peer
                {
                    // remove worker socket of closed client
                    DisconnectClient(socketPacket.ClientNumber);
                }
                else if (se.SocketErrorCode == SocketError.ConnectionAborted) // connection aborted
                {
                    // remove worker socket of closed client
                    DisconnectClient(socketPacket.ClientNumber);
                }
                else
                {
                    Traces.NntpServerTraceEvent(System.Diagnostics.TraceEventType.Critical, "[Client {0}] Server.OnDataSend failed: {1}", socketPacket.ClientNumber, Traces.ExceptionToString(se));
                }
                return;
            }
            catch (Exception ex)
            {
              Traces.NntpServerTraceEvent(System.Diagnostics.TraceEventType.Critical, "[Client {0}] Server.OnDataSend failed: {1}", socketPacket.ClientNumber, Traces.ExceptionToString(ex));
            }
        }

        public void OnDataReceived(IAsyncResult ar)
        {
            var socketPacket = (SocketPacket)ar.AsyncState;

            try
            {
                // complete BeginReceive() - returns number of characters received
                int charsReceived = socketPacket.ClientSocket.EndReceive(ar);

                if (charsReceived > 0)
                {
                    // extract data
                    string data = EncodingRecv.GetString(socketPacket.Buffer, 0, charsReceived);

                    DataReceived(data, socketPacket.ClientNumber);

                    // continue to receive data if still connected
                    WaitForData(socketPacket.ClientSocket, socketPacket.ClientNumber);
                }
                else
                {
                    DisconnectClient(socketPacket.ClientNumber);
                }
            }
            catch (ObjectDisposedException)
            {
                //System.Diagnostics.Trace.WriteLine("#" + socketPacket.ClientNumber.ToString() + " - OnDataReceived: Socket has been closed");
                return;
            }
            catch (SocketException se)
            {
                if (se.SocketErrorCode == SocketError.ConnectionReset) // connection reset by peer
                {
                    // remove worker socket of closed client
                    DisconnectClient(socketPacket.ClientNumber);
                }
                else if (se.SocketErrorCode == SocketError.ConnectionAborted) // connection aborted
                {
                    // remove worker socket of closed client
                    DisconnectClient(socketPacket.ClientNumber);
                }
                else
                {
                    Traces.NntpServerTraceEvent(System.Diagnostics.TraceEventType.Critical, "[Client {0}] Server.OnDataReceived failed: {1}", socketPacket.ClientNumber, Traces.ExceptionToString(se));
                }
                return;
            }
            catch (Exception ex)
            {
              Traces.NntpServerTraceEvent(System.Diagnostics.TraceEventType.Critical, "[Client {0}] Server.OnDataReceived failed: {1}", socketPacket.ClientNumber, Traces.ExceptionToString(ex));
            }
        }

        #endregion

        Encoding _encodingSend = Encoding.UTF8;
        public Encoding EncodingSend
        {
            get { return _encodingSend; }
            set { _encodingSend = value; }
        }

        public static Encoding EncodingRecv = Encoding.GetEncoding(DATA_RECV_ENCODING_CODE_PAGE);

        protected void SendData(string data, int clientNumber)
        {
            byte[] buffer;

            try
            {
                buffer = _encodingSend.GetBytes(data);
            }
            catch (Exception ex)
            {
                Traces.NntpServerTraceEvent(System.Diagnostics.TraceEventType.Critical, "[Client {0}] Server.SendData failed to encode data: {1}\r\n{2}", clientNumber, data, Traces.ExceptionToString(ex));
                return;
            }

            Socket workerSocket;
            try
            {
                lock (_workerSockets)
                {
                    workerSocket = _workerSockets[clientNumber];
                }
            }
            catch
            {
                // continue
                //System.Diagnostics.Trace.WriteLine("Server::SendData - unable to access entry [" + clientNumber + "]");
                return;
            }
            if (workerSocket == null)
            {
                Traces.NntpServerTraceEvent(System.Diagnostics.TraceEventType.Warning, "[Client {0}] Server.SendData failed (client removed from the worker list)", clientNumber);
                return;
            }
            try
            {
                if (_workerSendCallBack == null)
                {
                    _workerSendCallBack = new AsyncCallback(OnDataSend);
                }
                var socketPacket = new SocketPacket(workerSocket, clientNumber);

                workerSocket.BeginSend(buffer,
                                       0,
                                       buffer.Length,
                                       SocketFlags.None,
                                       _workerSendCallBack,
                                       socketPacket);
            }
            catch (ObjectDisposedException)
            {
                //System.Diagnostics.Trace.WriteLine("#" + clientNumber.ToString() + " - OnDataSend: Socket has been closed");
                return;
            }
            catch (SocketException se)
            {
                if (se.SocketErrorCode == SocketError.ConnectionReset) // connection reset by peer
                {
                    // remove worker socket of closed client
                    DisconnectClient(clientNumber);
                }
                else if (se.SocketErrorCode == SocketError.ConnectionAborted) // connection aborted
                {
                    // remove worker socket of closed client
                    DisconnectClient(clientNumber);
                }
                else
                {
                    Traces.NntpServerTraceEvent(System.Diagnostics.TraceEventType.Critical, "[Client {0}] Server.SendData failed: {1}", clientNumber, Traces.ExceptionToString(se));
                }
                return;
            }
            catch (Exception ex)
            {
                Traces.NntpServerTraceEvent(System.Diagnostics.TraceEventType.Critical, "[Client {0}] Server.SendData failed: {1}", clientNumber, Traces.ExceptionToString(ex));
            }
        }

        protected void CloseSockets()
        {
            try
            {
                if (_primarySocket != null)
                {
                    _primarySocket.Close();
                }

                lock (_workerSockets)
                {
                    foreach (KeyValuePair<int, Socket> key in _workerSockets)
                    {
                        Socket workerSocket = key.Value;

                        if (workerSocket != null)
                        {
                            workerSocket.Close();
                        }
                    }

                    _workerSockets.Clear();
                    _clients = 0;
                }
            }
            catch (Exception ex)
            {
                Traces.NntpServerTraceEvent(System.Diagnostics.TraceEventType.Error, "Server.CloseSockets failed: {0}", Traces.ExceptionToString(ex));
            }
        }
        
        protected void DisconnectClient(int clientNumber)
        {
            try
            {
                Socket workerSocket;

                try
                {
                    lock (_workerSockets)
                    {
                        workerSocket = _workerSockets[clientNumber];
                    }
                }
                catch
                {
                    //System.Diagnostics.Trace.WriteLine("Server::DisconnectClient - unable to access entry [" + clientNumber + "]");
                    return;
                }

                if (workerSocket != null)
                {
                    workerSocket.Close();

                    try
                    {
                        lock (_workerSockets)
                        {
                            _workerSockets.Remove(clientNumber);
                        }
                    }
// ReSharper disable EmptyGeneralCatchClause
                    catch
// ReSharper restore EmptyGeneralCatchClause
                    {
                        // continue
                        //System.Diagnostics.Trace.WriteLine("Server::DisconnectClient - unable to remove entry [" + clientNumber + "]");
                    }

                    ClientDisconnected(clientNumber);
                }
            }
            catch (Exception ex)
            {
                Traces.NntpServerTraceEvent(System.Diagnostics.TraceEventType.Error, "[Client {0}] Server.DisconnectClient failed: {1}", clientNumber, Traces.ExceptionToString(ex));
            }
        }
        

        #region Inherited Virtual Members

        protected virtual void ClientConnected(int clientNumber)
        {
        }

        protected virtual void ClientDisconnected(int clientNumber)
        {
        }

        protected virtual void DataReceived(string data, int clientNumber)
        {
        }

        #endregion

        #region IDisposable Members

        private bool _disposed;

        private void Dispose(bool disposing)
        {
            if (!(this._disposed))
            {
                if (disposing)
                {
                    CloseSockets();
                }
            }
            this._disposed = true;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        #endregion
    }
}
