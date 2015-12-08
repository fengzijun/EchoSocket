using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace EchoSocketCore.SocketsEx
{
    /// <summary>
    /// Client socket creator.
    /// </summary>
    public class SocketConnector : BaseSocketConnectionCreator
    {
        #region Fields

        private Socket FSocket;

        private Timer FReconnectTimer;
        private int FReconnectAttempts;
        private int FReconnectAttemptInterval;
        private int FReconnectAttempted;

        private ProxyInfo FProxyInfo;

        #endregion Fields

        #region Constructor

        public SocketConnector(BaseSocketConnectionHost host, string name, IPEndPoint remoteEndPoint, ProxyInfo proxyData, EncryptType encryptType, CompressionType compressionType, ICryptoService cryptoService, int reconnectAttempts, int reconnectAttemptInterval, IPEndPoint localEndPoint)
            : base(host, name, localEndPoint, encryptType, compressionType, cryptoService, remoteEndPoint)
        {
            FReconnectTimer = new Timer(new TimerCallback(ReconnectConnectionTimerCallBack));

            FReconnectAttempts = reconnectAttempts;
            FReconnectAttemptInterval = reconnectAttemptInterval;

            FReconnectAttempted = 0;

            FProxyInfo = proxyData;
        }

        #endregion Constructor

        #region Destructor

        public override void Free(bool canAccessFinalizable)
        {
            if (FReconnectTimer != null)
            {
                FReconnectTimer.Dispose();
                FReconnectTimer = null;
            }

            if (FSocket != null)
            {
                FSocket.Close();
                FSocket = null;
            }

            FProxyInfo = null;

            base.Free(canAccessFinalizable);
        }

        #endregion Destructor

        #region Methods

        #region Start

        public override void Start()
        {
            if (!Disposed)
            {
                BeginConnect();
            }
        }

        #endregion Start

        #region Stop

        public override void Stop()
        {
            Dispose();
        }

        #endregion Stop

        #region BeginConnect

        /// <summary>
        /// Begin the connection with host.
        /// </summary>
        internal void BeginConnect()
        {
            if (!Disposed)
            {
                //----- Create Socket!
                FSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                FSocket.Bind(Context.LocalEndPoint);
                FSocket.ReceiveBufferSize = Context.Host.Context.SocketBufferSize;
                FSocket.SendBufferSize = Context.Host.Context.SocketBufferSize;

                FReconnectTimer.Change(Timeout.Infinite, Timeout.Infinite);

                SocketAsyncEventArgs e = new SocketAsyncEventArgs();
                e.Completed += new EventHandler<SocketAsyncEventArgs>(BeginConnectCallbackAsync);
                e.UserToken = this;

                if (FProxyInfo == null)
                {
                    e.RemoteEndPoint = Context.RemotEndPoint;
                }
                else
                {
                    FProxyInfo.Completed = false;
                    FProxyInfo.SOCKS5Phase = SOCKS5Phase.spIdle;

                    e.RemoteEndPoint = FProxyInfo.ProxyEndPoint;
                }

                if (!FSocket.ConnectAsync(e))
                {
                    BeginConnectCallbackAsync(this, e);
                }
            }
        }

        #endregion BeginConnect

        #region BeginAcceptCallbackAsync

        /// <summary>
        /// Connect callback!
        /// </summary>
        /// <param name="ar"></param>
        internal void BeginConnectCallbackAsync(object sender, SocketAsyncEventArgs e)
        {
            if (!Disposed)
            {
                BaseSocketConnection connection = null;
                SocketConnector connector = null;
                Exception exception = null;

                if (e.SocketError == SocketError.Success)
                {
                    try
                    {
                        connector = (SocketConnector)e.UserToken;

                        connection = new ClientSocketConnection(Context.Host, connector, connector.Socket);

                        //----- Adjust buffer size!
                        connector.Socket.ReceiveBufferSize = Context.Host.Context.SocketBufferSize;
                        connector.Socket.SendBufferSize = Context.Host.Context.SocketBufferSize; ;

                        //----- Initialize!
                        Context.Host.AddSocketConnection(connection);
                        connection.Active = true;

                        Context.Host.InitializeConnection(connection);
                    }
                    catch (Exception ex)
                    {
                        exception = ex;

                        if (connection != null)
                        {
                            Context.Host.DisposeConnection(connection);
                            Context.Host.RemoveSocketConnection(connection);

                            connection = null;
                        }
                    }
                }
                else
                {
                    exception = new SocketException((int)e.SocketError);
                }

                if (exception != null)
                {
                    FReconnectAttempted++;
                    ReconnectConnection(false, exception);
                }
            }

            e.UserToken = null;
            e.Dispose();
            e = null;
        }

        #endregion BeginAcceptCallbackAsync

        #region ReconnectConnection

        internal void ReconnectConnection(bool resetAttempts, Exception ex)
        {
            if (!Disposed)
            {
                if (resetAttempts)
                {
                    //----- Reset counter and start new connect!
                    FReconnectAttempted = 0;
                    FReconnectTimer.Change(FReconnectAttemptInterval, FReconnectAttemptInterval);
                }
                else
                {
                    //----- Check attempt count!
                    if (FReconnectAttempts > 0)
                    {
                        if (FReconnectAttempted < FReconnectAttempts)
                        {
                            Context.Host.FireOnException(null, new ReconnectAttemptException("Reconnect attempt", this, ex, FReconnectAttempted, false));
                            FReconnectTimer.Change(FReconnectAttemptInterval, FReconnectAttemptInterval);
                        }
                        else
                        {
                            Context.Host.FireOnException(null, new ReconnectAttemptException("Reconnect attempt", this, ex, FReconnectAttempted, true));
                        }
                    }
                    else
                    {
                        Context.Host.FireOnException(null, new ReconnectAttemptException("Reconnect attempt", this, ex, FReconnectAttempted, true));
                    }
                }
            }
        }

        #endregion ReconnectConnection

        #region ReconnectConnectionTimerCallBack

        private void ReconnectConnectionTimerCallBack(Object stateInfo)
        {
            if (!Disposed)
            {
                FReconnectTimer.Change(Timeout.Infinite, Timeout.Infinite);
                BeginConnect();
            }
        }

        #endregion ReconnectConnectionTimerCallBack

        #endregion Methods

        #region Properties

        public int ReconnectAttempts
        {
            get { return FReconnectAttempts; }
            set { FReconnectAttempts = value; }
        }

        public int ReconnectAttemptInterval
        {
            get { return FReconnectAttemptInterval; }
            set { FReconnectAttemptInterval = value; }
        }

   

        public ProxyInfo ProxyInfo
        {
            get { return FProxyInfo; }
            set { FProxyInfo = value; }
        }

        internal Socket Socket
        {
            get { return FSocket; }
        }

        #endregion Properties
    }
}