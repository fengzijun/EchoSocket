using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Reflection;
using System.Threading;

namespace EchoSocketCore.SocketsEx
{
    /// <summary>
    /// connection data
    /// </summary>
    public abstract class BaseSocketConnection : BaseDisposable, ISocketConnection
    {
        private SocketAsyncEventArgs FWriteOV;

        private SocketAsyncEventArgs FReadOV;

        private SocketContext context;

        private ManualResetEvent fWaitConnectionsDisposing;

        private ReaderWriterLockSlim fSocketConnectionsSync;

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

        public SocketContext Context
        {
            get { return context; }
            set { context = value; }
        }

        public BaseSocketConnection(BaseSocketProvider host, Socket socket)
            : this(host, host.Context.SocketCreators[0], socket)
        {
        }

        public BaseSocketConnection(BaseSocketConnectionCreator creator, Socket socket)
            : this(creator.Context.Host, creator, socket)
        {
        }

        public BaseSocketConnection(BaseSocketProvider host, BaseSocketConnectionCreator creator, Socket socket)
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

            fWaitConnectionsDisposing = new ManualResetEvent(false);
            fSocketConnectionsSync = new ReaderWriterLockSlim();
        }

        public virtual void Initialize()
        {
            AddSocketConnection();
            Active = true;
            InitializeConnection();
        }

        public override void Free(bool canAccessFinalizable)
        {
            if (FReadOV != null)
            {
                Type t = typeof(SocketAsyncEventArgs);

                FieldInfo f = t.GetField("m_Completed", BindingFlags.Instance | BindingFlags.NonPublic);
                f.SetValue(FReadOV, null);

                //FReadOV.SetBuffer(null, 0, 0);
                FReadOV.Dispose();
                FReadOV = null;
            }

            if (FWriteOV != null)
            {
                Type t = typeof(SocketAsyncEventArgs);

                FieldInfo f = t.GetField("m_Completed", BindingFlags.Instance | BindingFlags.NonPublic);
                f.SetValue(FWriteOV, null);

                //FWriteOV.SetBuffer(null, 0, 0);

                FWriteOV.Dispose();
                FWriteOV = null;
            }

            if (fWaitConnectionsDisposing != null)
            {
                fWaitConnectionsDisposing.Set();
                fWaitConnectionsDisposing.Close();
                fWaitConnectionsDisposing = null;
            }

            Context.Free(canAccessFinalizable);

            base.Free(canAccessFinalizable);
        }

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

                    Context.LastAction = DateTime.Now;
                }
            }
        }

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

        public abstract IClientSocketConnection AsClientConnection();

        public abstract IServerSocketConnection AsServerConnection();

        public void BeginSend(byte[] buffer)
        {
            if (!Disposed)
            {
                context.Host.BeginSend(this, buffer, false);
            }
        }

        public void BeginReceive()
        {
            if (!Disposed)
            {
                context.Host.BeginReceive(this);
            }
        }

        public void BeginDisconnect()
        {
            if (!Disposed)
            {
                context.Host.BeginDisconnect(this);
            }
        }

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

        internal virtual void InitializeConnection()
        {
            if (Disposed)
                return;

            switch (context.EventProcessing)
            {
                case EventProcessing.epNone:

                    if (InitializeConnectionProxy())
                    {
                        context.Host.FireOnConnected(this);
                    }
                    else
                    {
                        if (InitializeConnectionEncrypt())
                        {
                            context.Host.FireOnConnected(this);
                        }
                        else
                        {
                            context.EventProcessing = EventProcessing.epUser;
                            context.Host.FireOnConnected(this);
                        }
                    }

                    break;

                case EventProcessing.epProxy:

                    if (InitializeConnectionEncrypt())
                    {
                        context.Host.FireOnConnected(this);
                    }
                    else
                    {
                        context.EventProcessing = EventProcessing.epUser;
                        context.Host.FireOnConnected(this);
                    }

                    break;

                case EventProcessing.epEncrypt:

                    context.EventProcessing = EventProcessing.epUser;
                    context.Host.FireOnConnected(this);

                    break;
            }
        }

        internal virtual bool InitializeConnectionProxy()
        {
            bool result = false;

            if (Disposed)
                return result;

            if (context.Creator is SocketConnector)
            {
                if (((SocketConnector)context.Creator).ProxyInfo != null)
                {
                    context.EventProcessing = EventProcessing.epProxy;
                    result = true;
                }
            }

            return result;
        }

        internal virtual bool InitializeConnectionEncrypt()
        {
            bool result = false;

            if (Disposed)
                return result;

            ICryptoService cryptService = context.Creator.Context.CryptoService;

            if ((cryptService != null) && (context.Creator.Context.EncryptType != EncryptType.etNone))
            {
                context.EventProcessing = EventProcessing.epEncrypt;
                result = true;
            }

            return result;
        }

        internal virtual void AddSocketConnection()
        {
            if (Disposed)
                return;

            fSocketConnectionsSync.EnterWriteLock();

            try
            {
                var socketConnections = context.Host.Context.SocketConnections;
                socketConnections.Add(context.ConnectionId, this);

                WriteOV.Completed += new EventHandler<SocketAsyncEventArgs>(context.Host.BeginSendCallbackAsync);
                ReadOV.Completed += new EventHandler<SocketAsyncEventArgs>(context.Host.BeginReadCallbackAsync);
            }
            finally
            {
                fSocketConnectionsSync.ExitWriteLock();
            }
        }

        internal virtual void RemoveSocketConnection()
        {
            if (Disposed || this == null)
                return;

            fSocketConnectionsSync.EnterWriteLock();
            var socketConnections = Context.Host.Context.SocketConnections;

            try
            {
                socketConnections.Remove(context.ConnectionId);
            }
            finally
            {
                if (socketConnections.Count <= 0)
                {
                    fWaitConnectionsDisposing.Set();
                }

                fSocketConnectionsSync.ExitWriteLock();
            }
        }

        internal virtual void DisposeConnection()
        {
            if (Disposed || this == null)
                return;

            if (WriteOV != null)
            {
                if (WriteOV.Buffer != null)
                {
                    context.Host.Context.BufferManager.ReturnBuffer(WriteOV.Buffer);
                }
            }

            if (ReadOV != null)
            {
                if (ReadOV.Buffer != null)
                {
                    context.Host.Context.BufferManager.ReturnBuffer(ReadOV.Buffer);
                }
            }

            Dispose();
        }

        internal virtual void CloseConnection()
        {
            if (Disposed)
                return;

            Active = false;
            Context.SocketHandle.Shutdown(SocketShutdown.Send);

            lock (Context.WriteQueue)
            {
                if (Context.WriteQueue.Count > 0)
                {
                    for (int i = 1; i <= Context.WriteQueue.Count; i++)
                    {
                        MessageBuffer message = Context.WriteQueue.Dequeue();

                        if (message != null)
                        {
                            context.Host.Context.BufferManager.ReturnBuffer(message.Buffer);
                        }
                    }
                }
            }
        }
    }
}