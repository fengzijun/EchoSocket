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
        private Socket FSocket;

        private Timer FReconnectTimer;
        private int FReconnectAttempts;
        private int FReconnectAttemptInterval;
        private int FReconnectAttempted;

        private ProxyInfo FProxyInfo;

        public SocketConnector(SocketContext context)
            : base(context)
        {
            
        }

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

        public override void Start()
        {
            if (!Disposed)
            {
                BeginConnect();
            }
        }

        public override void Stop()
        {
            Dispose();
        }

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
                FSocket.ReceiveBufferSize = Context.SocketBufferSize;
                FSocket.SendBufferSize = Context.SocketBufferSize;

                FReconnectTimer.Change(Timeout.Infinite, Timeout.Infinite);

                SocketAsyncEventArgs e = new SocketAsyncEventArgs();
                e.Completed += new EventHandler<SocketAsyncEventArgs>(BeginConnectCallbackAsync);
                e.UserToken = this;

                if (FProxyInfo == null)
                {
                    e.RemoteEndPoint = Context.RemoteEndPoint;
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

                        Context.Creator = connector;
                        Context.SocketHandle = connector.Socket;

                        connection = new ClientSocketConnection(Context);

                        //----- Adjust buffer size!
                        connector.Socket.ReceiveBufferSize = Context.SocketBufferSize;
                        connector.Socket.SendBufferSize = Context.SocketBufferSize; ;

                        //----- Initialize!
                        connection.Initialize();
                    }
                    catch (Exception ex)
                    {
                        exception = ex;

                        if (connection != null)
                        {
                            connection.Context.Host.DisposeConnection(connection);
                            connection.Context.Host.RemoveSocketConnection(connection);

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

        private void ReconnectConnectionTimerCallBack(Object stateInfo)
        {
            if (!Disposed)
            {
                FReconnectTimer.Change(Timeout.Infinite, Timeout.Infinite);
                BeginConnect();
            }
        }

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
    }
}