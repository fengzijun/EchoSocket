using System.Net;

namespace EchoSocketCore.SocketsEx
{
    /// <summary>
    /// Socket client host.
    /// </summary>
    public class SocketClient : BaseSocketConnectionHost
    {
        #region Constructor

        public SocketClient(CallbackThreadType callbackThreadType, ISocketService socketService)
            : base(HostType.htClient, callbackThreadType, socketService, DelimiterType.dtNone, null, 2048, 2048, 0, 0)
        {
            //-----
        }

        public SocketClient(CallbackThreadType callbackThreadType, ISocketService socketService, DelimiterType delimiterType, byte[] delimiter)
            : base(HostType.htClient, callbackThreadType, socketService, delimiterType, delimiter, 2048, 2048, 0, 0)
        {
            //-----
        }

        public SocketClient(CallbackThreadType callbackThreadType, ISocketService socketService, DelimiterType delimiterType, byte[] delimiter, int socketBufferSize, int messageBufferSize)
            : base(HostType.htClient, callbackThreadType, socketService, delimiterType, delimiter, socketBufferSize, messageBufferSize, 0, 0)
        {
            //-----
        }

        public SocketClient(CallbackThreadType callbackThreadType, ISocketService socketService, DelimiterType delimiterType, byte[] delimiter, int socketBufferSize, int messageBufferSize, int idleCheckInterval, int idleTimeOutValue)
            : base(HostType.htClient, callbackThreadType, socketService, delimiterType, delimiter, socketBufferSize, messageBufferSize, idleCheckInterval, idleTimeOutValue)
        {
            //-----
        }

        #endregion Constructor

        #region Methods

        #region BeginReconnect

        /// <summary>
        /// Reconnects the connection adjusting the reconnect timer.
        /// </summary>
        /// <param name="connection">
        /// Connection.
        /// </param>
        /// <param name="sleepTimeOutValue">
        /// Sleep timeout before reconnect.
        /// </param>
        internal override void BeginReconnect(ClientSocketConnection connection)
        {
            if (!Disposed)
            {
                if (connection != null)
                {
                    SocketConnector connector = (SocketConnector)connection.Context.Creator;

                    if (connector != null)
                    {
                        connector.ReconnectConnection(true, null);
                    }
                }
            }
        }

        #endregion BeginReconnect

        #region BeginSendToAll

        internal override void BeginSendToAll(ServerSocketConnection connection, byte[] buffer, bool includeMe)
        {
        }

        #endregion BeginSendToAll

        #region BeginSendTo

        internal override void BeginSendTo(BaseSocketConnection connectionTo, byte[] buffer)
        {
        }

        #endregion BeginSendTo

        #region AddConnector

        /// <summary>
        /// Adds the client connector (SocketConnector).
        /// </summary>
        /// <param name="remoteEndPoint"></param>
        public SocketConnector AddConnector(string name, IPEndPoint remoteEndPoint)
        {
            return AddConnector(name, remoteEndPoint, null, EncryptType.etNone, CompressionType.ctNone, null, 0, 0, new IPEndPoint(IPAddress.Any, 0));
        }

        public SocketConnector AddConnector(string name, IPEndPoint remoteEndPoint, ProxyInfo proxyData)
        {
            return AddConnector(name, remoteEndPoint, proxyData, EncryptType.etNone, CompressionType.ctNone, null, 0, 0, new IPEndPoint(IPAddress.Any, 0));
        }

        public SocketConnector AddConnector(string name, IPEndPoint remoteEndPoint, ProxyInfo proxyData, EncryptType encryptType, CompressionType compressionType, ICryptoService cryptoService)
        {
            return AddConnector(name, remoteEndPoint, proxyData, encryptType, compressionType, cryptoService, 0, 0, new IPEndPoint(IPAddress.Any, 0));
        }

        public SocketConnector AddConnector(string name, IPEndPoint remoteEndPoint, ProxyInfo proxyData, EncryptType encryptType, CompressionType compressionType, ICryptoService cryptoService, int reconnectAttempts, int reconnectAttemptInterval)
        {
            return AddConnector(name, remoteEndPoint, proxyData, encryptType, compressionType, cryptoService, reconnectAttempts, reconnectAttemptInterval, new IPEndPoint(IPAddress.Any, 0));
        }

        public SocketConnector AddConnector(string name, IPEndPoint remoteEndPoint, ProxyInfo proxyData, EncryptType encryptType, CompressionType compressionType, ICryptoService cryptoService, int reconnectAttempts, int reconnectAttemptInterval, IPEndPoint localEndPoint)
        {
            SocketConnector result = null;

            if (!Disposed)
            {
                result = new SocketConnector(this, name, remoteEndPoint, proxyData, encryptType, compressionType, cryptoService, reconnectAttempts, reconnectAttemptInterval, localEndPoint);
                result.AddCreator();
            }

            return result;
        }

        #endregion AddConnector

        #region Stop

        public override void Stop()
        {
            if (!Disposed)
            {
                StopConnections();
                StopCreators();
            }

            base.Stop();
        }

        #endregion Stop

        #endregion Methods
    }
}