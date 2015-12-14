using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.ServiceModel.Channels;
using System.Threading;

namespace EchoSocketCore.SocketsEx
{
    public class SocketContext : BaseDisposable
    {
        public SocketContext()
        {
            connectionId = 1000;
            SyncActive = new object();
            Active = false;
            WriteQueue = new Queue<MessageBuffer>();
            WriteQueueHasItems = false;
            SyncReadPending = new object();
            ReadPending = false;
            SyncEventProcessing = new object();
            EventProcessing = EventProcessing.epNone;
            LastAction = DateTime.Now;
            SyncData = new object();
            SocketCreators = new List<BaseSocketConnectionCreator>();
            SocketConnections = new Dictionary<long, BaseSocketConnection>();
            DelimiterEncrypt = new byte[] { 0xFE, 0xDC, 0xBA, 0x98, 0xBA, 0xDC, 0xFE };
              ReceivedQueue = new Queue<string>();
                ConnectTimeout = 10000;
                SentTimeout = 10000;
                ConnectedSync = new object();
                Connected = false;

              CompressionType = CompressionType.ctNone;
                DelimiterUserType = DelimiterType.dtNone;
                MessageBufferSize = 4096;
                SocketBufferSize = 2048;
                EncryptType = EncryptType.etNone;

        }

        public int IdleCheckInterval { get; set; }

        public int IdleTimeOutValue { get; set; }

        public HostType HostType { get; set; }

        /// <summary>
        /// Connection user data.
        /// </summary>
        public object UserData { get; set; }

        private long connectionId;

        /// <summary>
        /// Connection Session Id.
        /// </summary>
        public long ConnectionId
        {
            get { return connectionId; }
            set { connectionId = value; }
        }

        /// <summary>
        /// Connection Creator object.
        /// </summary>
        public BaseSocketConnectionCreator Creator { get; set; }

        /// <summary>
        /// Connection Host object.
        /// </summary>
        public BaseSocketProvider Host { get; set; }

        /// <summary>
        /// Handle of the OS Socket.
        /// </summary>
        public Socket SocketHandle { get; set; }

        /// <summary>
        /// Local socket endpoint.
        /// </summary>
        public IPEndPoint LocalEndPoint { get; set; }

        /// <summary>
        /// Remote socket endpoint.
        /// </summary>
        public IPEndPoint RemoteEndPoint { get; set; }

        public DateTime LastAction { get; set; }

        public long ReadBytes { get; set; }

        public long WriteBytes { get; set; }

        public ICryptoTransform Encryptor { get; set; }

        public ICryptoTransform Decryptor { get; set; }

        public Stream Stream { get; set; }

        public object SyncData { get; set; }

        public object SyncActive { get; set; }

        public CallbackThreadType CallbackThreadType { get; set; }

        public Dictionary<long, BaseSocketConnection> SocketConnections { get; set; }

        public BufferManager BufferManager { get; set; }

        public List<BaseSocketConnectionCreator> SocketCreators { get; set; }

        public bool Active { get; set; }

        public object SyncEventProcessing { get; set; }

        public Queue<MessageBuffer> WriteQueue { get; set; }

        public bool WriteQueueHasItems { get; set; }

        public object SyncReadPending { get; set; }

        public bool ReadPending { get; set; }

        private EventProcessing eventProcessing;

        public EventProcessing EventProcessing
        {
            get
            {
                lock (SyncEventProcessing)
                {
                    return eventProcessing;
                }
            }

            set
            {
                lock (SyncEventProcessing)
                {
                    eventProcessing = value;
                }
            }
        }

        public byte[] DelimiterEncrypt { get; set; }

        public ISocketService SocketService { get; set; }

        public byte[] DelimiterUserEncrypt { get; set; }

        public byte[] Delimiter
        {
            get
            {
                switch (eventProcessing)
                {
                    case EventProcessing.epUser:

                        return DelimiterUserEncrypt;

                    case EventProcessing.epEncrypt:

                        return DelimiterEncrypt;

                    case EventProcessing.epProxy:

                        return null;

                    default:

                        return null;
                }
            }
        }

        public DelimiterType DelimiterUserType { get; set; }

        public DelimiterType DelimiterType
        {
            get
            {
                switch (eventProcessing)
                {
                    case EventProcessing.epUser:

                        return DelimiterUserType;

                    case EventProcessing.epEncrypt:

                        return DelimiterType.dtMessageTailExcludeOnReceive;

                    case EventProcessing.epProxy:

                        return DelimiterType.dtNone;

                    default:

                        return DelimiterType.dtNone;
                }
            }
        }

        public string Name { get; set; }

        public CompressionType CompressionType { get; set; }

        public EncryptType EncryptType { get; set; }

        public ICryptoService CryptoService { get; set; }

        private ProxyInfo fProxyInfo;

        public ProxyInfo ProxyInfo
        {
            get { return fProxyInfo; }
            set { fProxyInfo = value; }
        }

        private int FMessageBufferSize;
        private int FSocketBufferSize;

        private ISocketConnection fSocketConnection;

        private int FConnectTimeout;
        private bool FConnected;
        private object FConnectedSync;

        private int FSentTimeout;

        private Queue<string> FReceivedQueue;

        public object ConnectedSync
        {
            get { return FConnectedSync; }
            set { FConnectedSync = value; }
        }

        public int SentTimeout
        {
            get { return FSentTimeout; }
            set { FSentTimeout = value; }
        }

        public int ConnectTimeout
        {
            get { return FConnectTimeout; }
            set { FConnectTimeout = value; }
        }

        public Queue<string> ReceivedQueue
        {
            get { return FReceivedQueue; }
            set { FReceivedQueue = value; }
        }

        public int MessageBufferSize
        {
            get { return FMessageBufferSize; }
            set { FMessageBufferSize = value; }
        }

        public int SocketBufferSize
        {
            get { return FSocketBufferSize; }
            set { FSocketBufferSize = value; }
        }

        internal ISocketConnection SocketConnection
        {
            get
            {
                return fSocketConnection;
            }

            set
            {
                fSocketConnection = value;
            }
        }

        public bool Connected
        {
            get
            {
                bool connected = false;

                lock (FConnectedSync)
                {
                    connected = FConnected;
                }

                return connected;
            }

            internal set
            {
                lock (FConnectedSync)
                {
                    FConnected = value;
                }
            }
        }

        public long CurrentConnectionId { get { return connectionId; } }

        public long GenerateConnectionId()
        {
            return Interlocked.Increment(ref connectionId);
        }

        public override void Free(bool canAccessFinalizable)
        {
            SocketHandle = null;
            Stream = null;
            Creator = null;
            Host = null;
            WriteQueue = null;
            SocketService = null;
            CryptoService = null;
            ReceivedQueue = null;
            SocketConnection = null;
            ProxyInfo = null;

            if (SocketConnections != null)
                SocketConnections = null;

            if (SocketCreators != null)
                SocketCreators = null;

            if (BufferManager != null)
                BufferManager = null;

            if (SocketService != null)
                SocketService = null;


            base.Free(canAccessFinalizable);
        }
    }
}