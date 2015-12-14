using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;

using EchoSocketCore.ThreadingEx;

namespace EchoSocketCore.SocketsEx
{
    /// <summary>
    /// Server socket connector.
    /// </summary>
    public class SocketListener : BaseSocketConnectionCreator
    {
        #region Fields

        private Socket FSocket;
        private byte FBackLog;
        private byte FAcceptThreads;

        #endregion Fields

        #region Constructor

        
        public SocketListener(SocketContext context, byte backLog, byte acceptThreads)
            : base(context)
        {
            FBackLog = backLog;
            FAcceptThreads = acceptThreads;
        }

        #endregion Constructor

        #region Destructor

        public override void Free(bool canAccessFinalizable)
        {
            if (FSocket != null)
            {
                FSocket.Close();
                FSocket = null;
            }

            base.Free(canAccessFinalizable);
        }

        #endregion Destructor

        #region Methods

        #region Start

        public override void Start()
        {
            if (Disposed)
                return;

            FSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            FSocket.Bind(Context.LocalEndPoint);
            FSocket.Listen(FBackLog * FAcceptThreads);

            //----- Begin accept new connections!
            int loopCount = 0;
            SocketAsyncEventArgs e = null;

            for (int i = 1; i <= FAcceptThreads; i++)
            {
                e = new SocketAsyncEventArgs();
                e.UserToken = this;
                e.Completed += new EventHandler<SocketAsyncEventArgs>(BeginAcceptCallbackAsync);

                if (!FSocket.AcceptAsync(e))
                {
                    BeginAcceptCallbackAsync(this, e);
                };

                ThreadEx.LoopSleep(ref loopCount);
            }
        }

        #endregion Start

        #region BeginAcceptCallbackAsync

        internal void BeginAcceptCallbackAsync(object sender, SocketAsyncEventArgs e)
        {
            ThreadPool.QueueUserWorkItem(new WaitCallback(BeginAcceptCallback), e);
        }

        /// <summary>
        /// Accept callback!
        /// </summary>
        internal void BeginAcceptCallback(object state)
        {
            SocketAsyncEventArgs e = (SocketAsyncEventArgs)state;

            if (Disposed)
                return;

            SocketListener listener = null;
            Socket acceptedSocket = null;
            BaseSocketConnection connection = null;

            listener = (SocketListener)e.UserToken;

            if (e.SocketError == SocketError.Success)
            {
                try
                {
                    //----- Get accepted socket!
                    acceptedSocket = e.AcceptSocket;

                    //----- Adjust buffer size!
                    acceptedSocket.ReceiveBufferSize = Context.SocketBufferSize;
                    acceptedSocket.SendBufferSize = Context.SocketBufferSize;
                    Context.Creator = listener;
                    Context.SocketHandle = acceptedSocket;

                    connection = new ServerSocketConnection(Context);

                    //----- Initialize!
                    connection.Initialize();
                }
                catch
                {
                    if (connection != null)
                    {
                        if (Context.Host != null)
                        {
                            connection.Context.Host.DisposeConnection(connection);
                            connection.Context.Host.RemoveSocketConnection(connection);
                        }

                        connection = null;
                    }
                }
            }

            //---- Continue to accept!
            SocketAsyncEventArgs e2 = new SocketAsyncEventArgs();
            e2.UserToken = listener;
            e2.Completed += new EventHandler<SocketAsyncEventArgs>(BeginAcceptCallbackAsync);

            if (!listener.Socket.AcceptAsync(e2))
            {
                BeginAcceptCallbackAsync(this, e2);
            }

            e.UserToken = null;
            e.Dispose();
            e = null;
        }

        #endregion BeginAcceptCallbackAsync

        #region Stop

        public override void Stop()
        {
            Dispose();
        }

        #endregion Stop

        #endregion Methods

        #region Properties


        public byte BackLog
        {
            get { return FBackLog; }
            set { FBackLog = value; }
        }

        public byte AcceptThreads
        {
            get { return FAcceptThreads; }
            set { FAcceptThreads = value; }
        }

        internal Socket Socket
        {
            get { return FSocket; }
        }

        #endregion Properties
    }
}