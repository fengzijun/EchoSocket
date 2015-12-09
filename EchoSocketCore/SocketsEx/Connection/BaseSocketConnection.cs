using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Security.Cryptography;

namespace EchoSocketCore.SocketsEx
{
    /// <summary>
    /// connection data 
    /// </summary>
    public abstract class BaseSocketConnection : BaseDisposable, ISocketConnection
    {
        #region Fields

        private SocketAsyncEventArgs FWriteOV;

        private SocketAsyncEventArgs FReadOV;

        private SocketContext context;

        #endregion Fields

        #region Constructor

        public BaseSocketConnection(BaseSocketConnectionHost host, Socket socket)
            : this(host, host.Context.SocketCreators[0], socket)
        {

        }

        public BaseSocketConnection(BaseSocketConnectionCreator creator, Socket socket)
            : this(creator.Context.Host, creator, socket)
        {

        }

        public BaseSocketConnection(BaseSocketConnectionHost host, BaseSocketConnectionCreator creator, Socket socket)
        {
            context = new SocketContext
            {
                ConnectionId = host.Context.GenerateConnectionId(),
                SyncData = new object(),
                Host = host,
                Creator = creator,
                SocketHandle = socket,
                SyncActive = new object(),
                Active = false,
                WriteQueue = new Queue<MessageBuffer>(),
                WriteQueueHasItems = false,
                SyncReadPending = new object(),
                ReadPending = false,
                SyncEventProcessing = new object(),
                EventProcessing = EventProcessing.epNone,
                LastAction = DateTime.Now,
            };

            FWriteOV = new SocketAsyncEventArgs();
            FReadOV = new SocketAsyncEventArgs();

         
        }

        #endregion Constructor

        #region Destructor

        public override void Free(bool canAccessFinalizable)
        {
          
            if (FReadOV != null)
            {
                Type t = typeof(SocketAsyncEventArgs);

                FieldInfo f = t.GetField("m_Completed", BindingFlags.Instance | BindingFlags.NonPublic);
                f.SetValue(FReadOV, null);

                FReadOV.SetBuffer(null, 0, 0);
                FReadOV.Dispose();
                FReadOV = null;
            }

            if (FWriteOV != null)
            {
                Type t = typeof(SocketAsyncEventArgs);

                FieldInfo f = t.GetField("m_Completed", BindingFlags.Instance | BindingFlags.NonPublic);
                f.SetValue(FWriteOV, null);

                FWriteOV.SetBuffer(null, 0, 0);
                FWriteOV.Dispose();
                FWriteOV = null;
            }

            Context.Free(canAccessFinalizable);

            base.Free(canAccessFinalizable);
        }

        #endregion Destructor

        #region Properties

    
        internal SocketAsyncEventArgs WriteOV
        {
            get { return FWriteOV; }
        }

        internal SocketAsyncEventArgs ReadOV
        {
            get { return FReadOV; }
        }

        internal bool Active
        {
            get
            {
                if (Disposed)
                {
                    return false;
                }

                lock (context.SyncActive)
                {
                    return context.Active;
                }
            }

            set
            {
                lock (context.SyncActive)
                {
                    context.Active = value;
                }
            }
        }

        #endregion Properties

        #region Methods

        internal void SetConnectionData(int readBytes, int writeBytes)
        {
            if (!Disposed)
            {
                lock (Context.SyncData)
                {
                    if (readBytes > 0)
                    {
                        Context.ReadBytes += readBytes;
                    }

                    if (writeBytes > 0)
                    {
                        Context.WriteBytes += writeBytes;
                    }

                    Context.LastAction= DateTime.Now;
                }
            }
        }

        #endregion Methods

        #region ISocketConnection Members

        #region Properties

        public SocketContext Context {

            get { return context; } 
            set { context = value; }
        }

        #endregion Properties

        #region Socket Options

        public void SetTTL(short value)
        {
            context.SocketHandle.Ttl = value;
        }

        public void SetLinger(LingerOption lo)
        {
            context.SocketHandle.LingerState = lo;
        }

        public void SetNagle(bool value)
        {
            context.SocketHandle.NoDelay = value;
        }

        #endregion Socket Options

        #region Abstract Methods

        public abstract IClientSocketConnection AsClientConnection();

        public abstract IServerSocketConnection AsServerConnection();

        #endregion Abstract Methods

        #region BeginSend

        public void BeginSend(byte[] buffer)
        {
            if (!Disposed)
            {
                context.Host.BeginSend(this, buffer, false);
            }
        }

        #endregion BeginSend

        #region BeginReceive

        public void BeginReceive()
        {
            if (!Disposed)
            {
                context.Host.BeginReceive(this);
            }
        }

        #endregion BeginReceive

        #region BeginDisconnect

        public void BeginDisconnect()
        {
            if (!Disposed)
            {
                context.Host.BeginDisconnect(this);
            }
        }

        #endregion BeginDisconnect

        #region GetConnections

        public ISocketConnection[] GetConnections()
        {
            if (!Disposed)
            {
                return context.Host.GetConnections();
            }
            else
            {
                return null;
            }
        }

        #endregion GetConnections

        #region GetConnectionById

        public ISocketConnection GetConnectionById(long id)
        {
            if (!Disposed)
            {
                return context.Host.GetSocketConnectionById(id);
            }
            else
            {
                return null;
            }
        }

        #endregion GetConnectionById

        #endregion ISocketConnection Members
    }
}