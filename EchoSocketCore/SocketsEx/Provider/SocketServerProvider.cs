using System;
using System.Net;

using EchoSocketCore.ThreadingEx;

namespace EchoSocketCore.SocketsEx
{
    /// <summary>
    /// Server connection host.
    /// </summary>
    public class SocketServerProvider : BaseSocketProvider
    {
        #region Constructor

        public SocketServerProvider(CallbackThreadType callbackThreadType, ISocketService socketService)
            : base(HostType.htServer, callbackThreadType, socketService, DelimiterType.dtNone, null, 2048, 2048, 0, 0)
        {
            //-----
        }

        public SocketServerProvider(CallbackThreadType callbackThreadType, ISocketService socketService, DelimiterType delimiterType, byte[] delimiter)
            : base(HostType.htServer, callbackThreadType, socketService, delimiterType, delimiter, 2048, 2048, 0, 0)
        {
            //-----
        }

        public SocketServerProvider(CallbackThreadType callbackThreadType, ISocketService socketService, DelimiterType delimiterType, byte[] delimiter, int socketBufferSize, int messageBufferSize)
            : base(HostType.htServer, callbackThreadType, socketService, delimiterType, delimiter, socketBufferSize, messageBufferSize, 0, 0)
        {
            //-----
        }

        public SocketServerProvider(CallbackThreadType callbackThreadType, ISocketService socketService, DelimiterType delimiterType, byte[] delimiter, int socketBufferSize, int messageBufferSize, int idleCheckInterval, int idleTimeOutValue)
            : base(HostType.htServer, callbackThreadType, socketService, delimiterType, delimiter, socketBufferSize, messageBufferSize, idleCheckInterval, idleTimeOutValue)
        {
            //-----
        }

        #endregion Constructor

        #region Methods

        #region BeginReconnect

        internal override void BeginReconnect(ClientSocketConnection connection)
        {
        }

        #endregion BeginReconnect

        #region BeginSendToAll

        internal override void BeginSendToAll(ServerSocketConnection connection, byte[] buffer, bool includeMe)
        {
            if (Disposed)
                return;

            BaseSocketConnection[] items = GetSocketConnections();

            if (items != null)
            {
                int loopSleep = 0;

                foreach (BaseSocketConnection cnn in items)
                {
                    if (Disposed)
                    {
                        break;
                    }

                    try
                    {
                        if (includeMe || connection != cnn)
                        {
                            byte[] localBuffer = new byte[buffer.Length];
                            Buffer.BlockCopy(buffer, 0, localBuffer, 0, buffer.Length);

                            BeginSend(cnn, localBuffer, true);
                        }
                    }
                    finally
                    {
                        ThreadEx.LoopSleep(ref loopSleep);
                    }
                }
            }
        }

        #endregion BeginSendToAll

        #region BeginSendTo

        internal override void BeginSendTo(BaseSocketConnection connection, byte[] buffer)
        {
            if (!Disposed)
            {
                BeginSend(connection, buffer, true);
            }
        }

        #endregion BeginSendTo

        #endregion Methods

        #region AddListener

        /// <summary>
        /// Add the server connector (SocketListener).
        /// </summary>
        /// <param name="localEndPoint"></param>
        public SocketListener AddListener(string name, IPEndPoint localEndPoint)
        {
            return AddListener(name, localEndPoint, EncryptType.etNone, CompressionType.ctNone, null, 5, 2);
        }

        public SocketListener AddListener(string name, IPEndPoint localEndPoint, EncryptType encryptType, CompressionType compressionType, ICryptoService cryptoService)
        {
            return AddListener(name, localEndPoint, encryptType, compressionType, cryptoService, 5, 2);
        }

        public SocketListener AddListener(string name, IPEndPoint localEndPoint, EncryptType encryptType, CompressionType compressionType, ICryptoService cryptoService, byte backLog, byte acceptThreads)
        {
            SocketListener listener = null;

            if (!Disposed)
            {
                listener = new SocketListener(this, name, localEndPoint, encryptType, compressionType, cryptoService, backLog, acceptThreads);
                listener.AddCreator();
            }

            return listener;
        }

        #endregion AddListener

        #region Stop

        public override void Stop()
        {
            if (!Disposed)
            {
                StopCreators();
                StopConnections();
            }

            base.Stop();
        }

        #endregion Stop
    }
}