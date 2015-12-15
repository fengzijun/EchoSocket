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

        private Timer fReconnectTimer;
        private int fReconnectAttempts;
        private int fReconnectAttemptInterval;
        private int fReconnectAttempted;


        public SocketConnector(SocketContext context, int reconnectAttempts, int reconnectAttemptInterval)
            : base(context)
        {
            fReconnectAttempts = reconnectAttempts;
            fReconnectAttemptInterval = reconnectAttemptInterval;
            fReconnectAttempted = 0;

            fReconnectTimer = new Timer(new TimerCallback(ReconnectConnectionTimerCallBack));
        }

        public override void Free(bool canAccessFinalizable)
        {
            if (fReconnectTimer != null)
            {
                fReconnectTimer.Dispose();
                fReconnectTimer = null;
            }

            if (FSocket != null)
            {
                FSocket.Close();
                FSocket = null;
            }

            Context.Free(canAccessFinalizable);

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

                fReconnectTimer.Change(Timeout.Infinite, Timeout.Infinite);

                SocketAsyncEventArgs e = new SocketAsyncEventArgs();
                e.Completed += new EventHandler<SocketAsyncEventArgs>(BeginConnectCallbackAsync);
                e.UserToken = this;

                if (Context.ProxyInfo == null)
                {
                    e.RemoteEndPoint = Context.RemoteEndPoint;
                }
                else
                {
                    Context.ProxyInfo.Completed = false;
                    Context.ProxyInfo.SOCKS5Phase = SOCKS5Phase.spIdle;

                    e.RemoteEndPoint = Context.ProxyInfo.ProxyEndPoint;
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
                        Context.Host.AddSocketConnection(connection);
                        Context.Active = true;
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
                    fReconnectAttempted++;
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
                    fReconnectAttempted = 0;
                    fReconnectTimer.Change(fReconnectAttemptInterval, fReconnectAttemptInterval);
                }
                else
                {
                    //----- Check attempt count!
                    if (fReconnectAttempts > 0)
                    {
                        if (fReconnectAttempted < fReconnectAttempts)
                        {
                            Context.Host.FireOnException(null, new ReconnectAttemptException("Reconnect attempt", this, ex, fReconnectAttempted, false));
                            fReconnectTimer.Change(fReconnectAttemptInterval, fReconnectAttemptInterval);
                        }
                        else
                        {
                            Context.Host.FireOnException(null, new ReconnectAttemptException("Reconnect attempt", this, ex, fReconnectAttempted, true));
                        }
                    }
                    else
                    {
                        Context.Host.FireOnException(null, new ReconnectAttemptException("Reconnect attempt", this, ex, fReconnectAttempted, true));
                    }
                }
            }
        }

        private void ReconnectConnectionTimerCallBack(Object stateInfo)
        {
            if (!Disposed)
            {
                fReconnectTimer.Change(Timeout.Infinite, Timeout.Infinite);
                BeginConnect();
            }
        }

        public int ReconnectAttempts
        {
            get { return fReconnectAttempts; }
            set { fReconnectAttempts = value; }
        }

        public int ReconnectAttemptInterval
        {
            get { return fReconnectAttemptInterval; }
            set { fReconnectAttemptInterval = value; }
        }

      
        internal Socket Socket
        {
            get { return FSocket; }
        }
    }
}