using System;
using System.Collections.Generic;
using System.Net;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;

namespace EchoSocketCore.SocketsEx
{
    #region SocketClientSync

    public class SocketClientSync : BaseDisposable
    {
        #region Fields

        //----- EndPoints!
        private IPEndPoint FLocalEndPoint;

        private IPEndPoint FRemoteEndPoint;

        //----- Message Types!
        private EncryptType FEncryptType;

        private CompressionType FCompressionType;
        private DelimiterType FDelimiterType;

        //----- Proxy!
        private ProxyInfo FProxyInfo;

        //----- Socket delimiter and buffer size!
        private byte[] FDelimiter;

        private int FMessageBufferSize;
        private int FSocketBufferSize;

        private event OnSymmetricAuthenticateEvent FOnSymmetricAuthenticateEvent;

        private event OnSSLClientAuthenticateEvent FOnSSLClientAuthenticateEvent;

        private event OnDisconnectEvent FOnDisconnectedEvent;

        private ISocketConnection FSocketConnection;
        private SocketClient FSocketClient;
        private SocketClientSyncSocketService FSocketClientEvents;
        private SocketClientSyncCryptService FCryptClientEvents;

        private AutoResetEvent FExceptionEvent;

        private AutoResetEvent FConnectEvent;
        private int FConnectTimeout;
        private bool FConnected;
        private object FConnectedSync;

        private AutoResetEvent FSentEvent;
        private int FSentTimeout;

        private Queue<string> FReceivedQueue;
        private AutoResetEvent FReceivedEvent;

        private ManualResetEvent FDisconnectEvent;

        private Exception FLastException;

        #endregion Fields

        #region Constructor

        public SocketClientSync(IPEndPoint host)
        {
            FReceivedEvent = new AutoResetEvent(false);
            FExceptionEvent = new AutoResetEvent(false);
            FSentEvent = new AutoResetEvent(false);
            FConnectEvent = new AutoResetEvent(false);
            FDisconnectEvent = new ManualResetEvent(false);

            FReceivedQueue = new Queue<string>();

            FConnectTimeout = 10000;
            FSentTimeout = 10000;

            FConnectedSync = new object();
            FConnected = false;

            FSocketClientEvents = new SocketClientSyncSocketService(this);
            FCryptClientEvents = new SocketClientSyncCryptService(this);

            FLocalEndPoint = null;
            FRemoteEndPoint = host;

            //----- Message Types!
            FEncryptType = EncryptType.etNone;
            FCompressionType = CompressionType.ctNone;
            FDelimiterType = DelimiterType.dtNone;

            //----- Proxy!
            FProxyInfo = null;

            //----- Socket delimiter and buffer size!
            FDelimiter = null;

            FMessageBufferSize = 4096;
            FSocketBufferSize = 2048;
        }

        #endregion Constructor

        #region Destructor

        public override void Free(bool canAccessFinalizable)
        {
            FSocketConnection = null;
            FSocketClientEvents = null;
            FCryptClientEvents = null;
            FConnectedSync = null;
            FLastException = null;

            if (FReceivedQueue != null)
            {
                FReceivedQueue.Clear();
                FReceivedQueue = null;
            }

            if (FSocketClient != null)
            {
                FSocketClient.Stop();
                FSocketClient.Dispose();
                FSocketClient = null;
            }

            if (FExceptionEvent != null)
            {
                FExceptionEvent.Close();
                FExceptionEvent = null;
            }

            if (FConnectEvent != null)
            {
                FConnectEvent.Close();
                FConnectEvent = null;
            }

            if (FSentEvent != null)
            {
                FSentEvent.Close();
                FSentEvent = null;
            }

            if (FReceivedEvent != null)
            {
                FReceivedEvent.Close();
                FReceivedEvent = null;
            }

            if (FDisconnectEvent != null)
            {
                FDisconnectEvent.Close();
                FDisconnectEvent = null;
            }

            base.Free(canAccessFinalizable);
        }

        #endregion Destructor

        #region Methods

        #region DoOnSSLClientAuthenticate

        internal void DoOnSSLClientAuthenticate(ISocketConnection connection, out string serverName, ref X509Certificate2Collection certs, ref bool checkRevocation)
        {
            serverName = String.Empty;

            if (FOnSSLClientAuthenticateEvent != null)
            {
                FOnSSLClientAuthenticateEvent(connection, out serverName, ref certs, ref checkRevocation);
            }
        }

        #endregion DoOnSSLClientAuthenticate

        #region DoOnSymmetricAuthenticate

        internal void DoOnSymmetricAuthenticate(ISocketConnection connection, out RSACryptoServiceProvider serverKey)
        {
            serverKey = new RSACryptoServiceProvider();
            serverKey.Clear();

            if (FOnSymmetricAuthenticateEvent != null)
            {
                FOnSymmetricAuthenticateEvent(connection, out serverKey);
            }
        }

        #endregion DoOnSymmetricAuthenticate

        #region Connect

        public void Connect()
        {
            if (!Disposed)
            {
                FLastException = null;

                if (!Connected)
                {
                    FConnectEvent.Reset();
                    FExceptionEvent.Reset();
                    FDisconnectEvent.Reset();

                    FSocketClient = new SocketClient(CallbackThreadType.ctWorkerThread, FSocketClientEvents, FDelimiterType, FDelimiter, FSocketBufferSize, FMessageBufferSize);

                    SocketConnector connector = FSocketClient.AddConnector("SocketClientSync", FRemoteEndPoint);

                    connector.Context.EncryptType = FEncryptType;
                    connector.Context.CompressionType = FCompressionType;
                    connector.Context.CryptoService = FCryptClientEvents;
                    connector.ProxyInfo = FProxyInfo;

                    WaitHandle[] wait = new WaitHandle[] { FConnectEvent, FExceptionEvent };

                    FSocketClient.Start();

                    int signal = WaitHandle.WaitAny(wait, FConnectTimeout, false);

                    switch (signal)
                    {
                        case 0:

                            //----- Connect!
                            FLastException = null;
                            Connected = true;

                            break;

                        case 1:

                            //----- Exception!
                            Connected = false;
                            FSocketConnection = null;

                            FSocketClient.Stop();
                            FSocketClient.Dispose();
                            FSocketClient = null;

                            break;

                        default:

                            //----- TimeOut!
                            FLastException = new TimeoutException("Connect timeout.");

                            Connected = false;
                            FSocketConnection = null;

                            FSocketClient.Stop();
                            FSocketClient.Dispose();
                            FSocketClient = null;

                            break;
                    }
                }
            }
        }

        #endregion Connect

        #region Write

        public void Write(string buffer)
        {
            Write(Encoding.GetEncoding(1252).GetBytes(buffer));
        }

        public void Write(byte[] buffer)
        {
            FLastException = null;

            if (!Disposed)
            {
                if (Connected)
                {
                    FSentEvent.Reset();
                    FExceptionEvent.Reset();

                    WaitHandle[] wait = new WaitHandle[] { FSentEvent, FDisconnectEvent, FExceptionEvent };

                    FSocketConnection.BeginSend(buffer);

                    int signaled = WaitHandle.WaitAny(wait, FSentTimeout, false);

                    switch (signaled)
                    {
                        case 0:

                            //----- Sent!
                            FLastException = null;
                            break;

                        case 1:

                            //----- Disconnected!
                            DoDisconnect();
                            break;

                        case 2:

                            //----- Exception!
                            break;

                        default:

                            //----- TimeOut!
                            FLastException = new TimeoutException("Write timeout.");
                            break;
                    }
                }
            }
        }

        #endregion Write

        #region Enqueue

        internal void Enqueue(string data)
        {
            if (!Disposed)
            {
                lock (FReceivedQueue)
                {
                    FReceivedQueue.Enqueue(data);
                    FReceivedEvent.Set();
                }
            }
        }

        #endregion Enqueue

        #region Read

        public string Read(int timeOut)
        {
            string result = null;

            if (!Disposed)
            {
                FLastException = null;

                if (Connected)
                {
                    lock (FReceivedQueue)
                    {
                        if (FReceivedQueue.Count > 0)
                        {
                            result = FReceivedQueue.Dequeue();
                        }
                    }

                    if (result == null)
                    {
                        WaitHandle[] wait = new WaitHandle[] { FReceivedEvent, FDisconnectEvent, FExceptionEvent };

                        int signaled = WaitHandle.WaitAny(wait, timeOut, false);

                        switch (signaled)
                        {
                            case 0:

                                //----- Received!
                                lock (FReceivedQueue)
                                {
                                    if (FReceivedQueue.Count > 0)
                                    {
                                        result = FReceivedQueue.Dequeue();
                                    }
                                }

                                FLastException = null;

                                break;

                            case 1:

                                //----- Disconnected!
                                DoDisconnect();
                                break;

                            case 2:

                                //----- Exception!
                                break;

                            default:

                                //----- TimeOut!
                                FLastException = new TimeoutException("Read timeout.");
                                break;
                        }
                    }
                }
            }

            return result;
        }

        #endregion Read

        #region DoDisconnect

        internal void DoDisconnect()
        {
            bool fireEvent = false;

            lock (FConnectedSync)
            {
                if (FConnected)
                {
                    //----- Disconnect!
                    FConnected = false;
                    FSocketConnection = null;

                    if (FSocketClient != null)
                    {
                        FSocketClient.Stop();
                        FSocketClient.Dispose();
                        FSocketClient = null;
                    }

                    fireEvent = true;
                }
            }

            if ((FOnDisconnectedEvent != null) && fireEvent)
            {
                FOnDisconnectedEvent();
            }
        }

        #endregion DoDisconnect

        #region Disconnect

        public void Disconnect()
        {
            if (!Disposed)
            {
                FLastException = null;

                if (Connected)
                {
                    FExceptionEvent.Reset();

                    if (FSocketConnection != null)
                    {
                        WaitHandle[] wait = new WaitHandle[] { FDisconnectEvent, FExceptionEvent };

                        FSocketConnection.BeginDisconnect();

                        int signaled = WaitHandle.WaitAny(wait, FConnectTimeout, false);

                        switch (signaled)
                        {
                            case 0:

                                DoDisconnect();
                                break;

                            case 1:

                                //----- Exception!
                                DoDisconnect();
                                break;

                            default:

                                //----- TimeOut!
                                FLastException = new TimeoutException("Disconnect timeout.");
                                break;
                        }
                    }
                }
            }
        }

        #endregion Disconnect

        #endregion Methods

        #region Properties

        public event OnDisconnectEvent OnDisconnected
        {
            add
            {
                FOnDisconnectedEvent += value;
            }

            remove
            {
                FOnDisconnectedEvent -= value;
            }
        }

        public event OnSymmetricAuthenticateEvent OnSymmetricAuthenticate
        {
            add
            {
                FOnSymmetricAuthenticateEvent += value;
            }

            remove
            {
                FOnSymmetricAuthenticateEvent -= value;
            }
        }

        public event OnSSLClientAuthenticateEvent OnSSLClientAuthenticate
        {
            add
            {
                FOnSSLClientAuthenticateEvent += value;
            }

            remove
            {
                FOnSSLClientAuthenticateEvent -= value;
            }
        }

        public IPEndPoint RemoteEndPoint
        {
            get { return FRemoteEndPoint; }
            set { FRemoteEndPoint = value; }
        }

        public IPEndPoint LocalEndPoint
        {
            get { return FLocalEndPoint; }
            set { FLocalEndPoint = value; }
        }

        public DelimiterType DelimiterType
        {
            get { return FDelimiterType; }
            set { FDelimiterType = value; }
        }

        public EncryptType EncryptType
        {
            get { return FEncryptType; }
            set { FEncryptType = value; }
        }

        public CompressionType CompressionType
        {
            get { return FCompressionType; }
            set { FCompressionType = value; }
        }

        public byte[] Delimiter
        {
            get { return FDelimiter; }
            set { FDelimiter = value; }
        }

        public ProxyInfo ProxyInfo
        {
            get { return FProxyInfo; }
            set { FProxyInfo = value; }
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

        internal ManualResetEvent DisconnectEvent
        {
            get
            {
                return FDisconnectEvent;
            }
        }

        internal AutoResetEvent ConnectEvent
        {
            get
            {
                return FConnectEvent;
            }
        }

        internal AutoResetEvent SentEvent
        {
            get
            {
                return FSentEvent;
            }
        }

        internal AutoResetEvent ExceptionEvent
        {
            get
            {
                return FExceptionEvent;
            }
        }

        internal ISocketConnection SocketConnection
        {
            get
            {
                return FSocketConnection;
            }

            set
            {
                FSocketConnection = value;
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

        public Exception LastException
        {
            get
            {
                return FLastException;
            }

            internal set
            {
                FLastException = value;
            }
        }

        #endregion Properties
    }

    #endregion SocketClientSync

    #region SocketClientSyncSocketService

    internal class SocketClientSyncSocketService : BaseSocketService
    {
        #region Fields

        private SocketClientSync FSocketClient;

        #endregion Fields

        #region Constructor

        public SocketClientSyncSocketService(SocketClientSync client)
        {
            FSocketClient = client;
        }

        #endregion Constructor

        #region Methods

        public override void OnConnected(ConnectionEventArgs e)
        {
            FSocketClient.SocketConnection = e.Connection;
            FSocketClient.SocketConnection.BeginReceive();
            FSocketClient.ConnectEvent.Set();
        }

        public override void OnException(ExceptionEventArgs e)
        {
            FSocketClient.LastException = e.Exception;
            FSocketClient.ExceptionEvent.Set();
        }

        public override void OnSent(MessageEventArgs e)
        {
            FSocketClient.SentEvent.Set();
        }

        public override void OnReceived(MessageEventArgs e)
        {
            FSocketClient.Enqueue(Encoding.GetEncoding(1252).GetString(e.Buffer));
            FSocketClient.SocketConnection.BeginReceive();
        }

        public override void OnDisconnected(ConnectionEventArgs e)
        {
            FSocketClient.DisconnectEvent.Set();
        }

        #endregion Methods
    }

    #endregion SocketClientSyncSocketService

    #region SocketClientSyncCryptService

    internal class SocketClientSyncCryptService : BaseCryptoService
    {
        #region Fields

        private SocketClientSync FSocketClient;

        #endregion Fields

        #region Constructor

        public SocketClientSyncCryptService(SocketClientSync client)
        {
            FSocketClient = client;
        }

        #endregion Constructor

        #region Methods

        public override void OnSymmetricAuthenticate(ISocketConnection connection, out RSACryptoServiceProvider serverKey)
        {
            FSocketClient.DoOnSymmetricAuthenticate(connection, out serverKey);
        }

        public override void OnSSLClientAuthenticate(ISocketConnection connection, out string serverName, ref X509Certificate2Collection certs, ref bool checkRevocation)
        {
            FSocketClient.DoOnSSLClientAuthenticate(connection, out serverName, ref certs, ref checkRevocation);
        }

        #endregion Methods
    }

    #endregion SocketClientSyncCryptService
}