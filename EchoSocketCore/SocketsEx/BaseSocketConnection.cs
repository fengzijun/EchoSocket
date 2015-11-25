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
    /// Base socket connection
    /// </summary>
    public abstract class BaseSocketConnection : BaseDisposable, ISocketConnection
    {
        #region Fields

        private object FSyncData;
 
        private object FSyncActive;

        private bool FActive;

        private object FSyncEventProcessing;

        private EventProcessing FEventProcessing;

        private Stream FStream;

        private SocketAsyncEventArgs FWriteOV;

        private Queue<MessageBuffer> FWriteQueue;

        private bool FWriteQueueHasItems;

        private SocketAsyncEventArgs FReadOV;

        private object FSyncReadPending;

        private bool FReadPending;

        private ICryptoTransform FDecryptor;
        private ICryptoTransform FEncryptor;

        #endregion Fields

        #region Constructor

        internal BaseSocketConnection(BaseSocketConnectionHost host, BaseSocketConnectionCreator creator, Socket socket)
        {
            if (Context == null)
                Context = new SocketContext();

            Context.ConnectionId = host.GetConnectionId();

            FSyncData = new object();

            Context.Host = host;
            Context.Creator = creator;
            Context.SocketHandle = socket;

            FSyncActive = new Object();
            FActive = false;

            FWriteOV = new SocketAsyncEventArgs();
            FReadOV = new SocketAsyncEventArgs();

            FWriteQueue = new Queue<MessageBuffer>();
            FWriteQueueHasItems = false;

            FSyncReadPending = new object();
            FReadPending = false;

            FSyncEventProcessing = new object();
            FEventProcessing = EventProcessing.epNone;

            Context.LastAction = DateTime.Now;

            FEncryptor = null;
            FDecryptor = null;
        }

        #endregion Constructor

        #region Destructor

        protected override void Free(bool canAccessFinalizable)
        {
            if (FWriteQueue != null)
            {
                FWriteQueue.Clear();
                FWriteQueue = null;
            }

            if (FStream != null)
            {
                FStream.Close();
                FStream = null;
            }

            if (FDecryptor != null)
            {
                FDecryptor.Dispose();
                FDecryptor = null;
            }

            if (FEncryptor != null)
            {
                FEncryptor.Dispose();
                FEncryptor = null;
            }

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

            Context = null;

            FSyncReadPending = null;
            FSyncData = null;
            FSyncEventProcessing = null;

            base.Free(canAccessFinalizable);
        }

        #endregion Destructor

        #region Properties

        internal Queue<MessageBuffer> WriteQueue
        {
            get { return FWriteQueue; }
        }

        internal bool WriteQueueHasItems
        {
            get { return FWriteQueueHasItems; }
            set { FWriteQueueHasItems = value; }
        }

        internal bool ReadPending
        {
            get { return FReadPending; }
            set { FReadPending = value; }
        }

        internal object SyncReadPending
        {
            get { return FSyncReadPending; }
        }

        internal SocketAsyncEventArgs WriteOV
        {
            get { return FWriteOV; }
        }

        internal SocketAsyncEventArgs ReadOV
        {
            get { return FReadOV; }
        }

        internal object SyncActive
        {
            get { return FSyncActive; }
        }

        internal EventProcessing EventProcessing
        {
            get
            {
                lock (FSyncEventProcessing)
                {
                    return FEventProcessing;
                }
            }

            set
            {
                lock (FSyncEventProcessing)
                {
                    FEventProcessing = value;
                }
            }
        }

        internal bool Active
        {
            get
            {
                if (Disposed)
                {
                    return false;
                }

                lock (FSyncActive)
                {
                    return FActive;
                }
            }

            set
            {
                lock (FSyncActive)
                {
                    FActive = value;
                }
            }
        }

        internal ICryptoTransform Encryptor
        {
            get { return FEncryptor; }
            set { FEncryptor = value; }
        }

        internal ICryptoTransform Decryptor
        {
            get { return FDecryptor; }
            set { FDecryptor = value; }
        }

        internal Stream Stream
        {
            get { return FStream; }
            set { FStream = value; }
        }



        internal byte[] Delimiter
        {
            get
            {
                switch (EventProcessing)
                {
                    case EventProcessing.epUser:

                        return Context.Host.Context.Delimiter;

                    case EventProcessing.epEncrypt:

                        return Context.Host.DelimiterEncrypt;

                    case EventProcessing.epProxy:

                        return null;

                    default:

                        return null;
                }
            }
        }

        internal DelimiterType DelimiterType
        {
            get
            {
                switch (EventProcessing)
                {
                    case EventProcessing.epUser:

                        return Context.Host.Context.DelimiterType;

                    case EventProcessing.epEncrypt:

                        return DelimiterType.dtMessageTailExcludeOnReceive;

                    case EventProcessing.epProxy:

                        return DelimiterType.dtNone;

                    default:

                        return DelimiterType.dtNone;
                }
            }
        }

        internal EncryptType EncryptType
        {
            get { return Context.Creator.Context.EncryptType; }
        }

        internal CompressionType CompressionType
        {
            get { return Context.Creator.Context.CompressionType; }
        }

        internal HostType HostType
        {
            get { return Context.Host.Context.HostType; }
        }

        internal BaseSocketConnectionCreator BaseCreator
        {
            get { return Context.Creator; }
        }

        internal BaseSocketConnectionHost BaseHost
        {
            get { return Context.Host; }
        }

        #endregion Properties

        #region Methods

        internal void SetConnectionData(int readBytes, int writeBytes)
        {
            if (!Disposed)
            {
                lock (FSyncData)
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

        public SocketContext Context { get; set; }

        #endregion Properties

        #region Socket Options

        public void SetTTL(short value)
        {
            Context.SocketHandle.Ttl = value;
        }

        public void SetLinger(LingerOption lo)
        {
            Context.SocketHandle.LingerState = lo;
        }

        public void SetNagle(bool value)
        {
            Context.SocketHandle.NoDelay = value;
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
                Context.Host.BeginSend(this, buffer, false);
            }
        }

        #endregion BeginSend

        #region BeginReceive

        public void BeginReceive()
        {
            if (!Disposed)
            {
                Context.Host.BeginReceive(this);
            }
        }

        #endregion BeginReceive

        #region BeginDisconnect

        public void BeginDisconnect()
        {
            if (!Disposed)
            {
                Context.Host.BeginDisconnect(this);
            }
        }

        #endregion BeginDisconnect

        #region GetConnections

        public ISocketConnection[] GetConnections()
        {
            if (!Disposed)
            {
                return Context.Host.GetConnections();
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
                return Context.Host.GetSocketConnectionById(id);
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