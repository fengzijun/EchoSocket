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

        private event OnSymmetricAuthenticateEvent fOnSymmetricAuthenticateEvent;

        private event OnSSLClientAuthenticateEvent fOnSSLClientAuthenticateEvent;

        private event OnDisconnectEvent fOnDisconnectedEvent;

        private AutoResetEvent fExceptionEvent;

        private AutoResetEvent fConnectEvent;
  
        private AutoResetEvent fSentEvent;
    
        private AutoResetEvent fReceivedEvent;

        private ManualResetEvent fDisconnectEvent;

        private Exception fLastException;

        private SocketContext context;

        #endregion Fields

        #region Constructor

        public SocketClientSync(IPEndPoint host)
        {
            fReceivedEvent = new AutoResetEvent(false);
            fExceptionEvent = new AutoResetEvent(false);
            fSentEvent = new AutoResetEvent(false);
            fConnectEvent = new AutoResetEvent(false);
            fDisconnectEvent = new ManualResetEvent(false);

            context = new SocketContext();
            context.SocketService = new SocketClientSyncSocketService(this);
            context.CryptoService = new SocketClientSyncCryptService(this);
            context.RemoteEndPoint = host;

        }

        #endregion Constructor

        #region Destructor

        public override void Free(bool canAccessFinalizable)
        {
           
            fLastException = null;

            if (fExceptionEvent != null)
            {
                fExceptionEvent.Close();
                fExceptionEvent = null;
            }

            if (fConnectEvent != null)
            {
                fConnectEvent.Close();
                fConnectEvent = null;
            }

            if (fSentEvent != null)
            {
                fSentEvent.Close();
                fSentEvent = null;
            }

            if (fReceivedEvent != null)
            {
                fReceivedEvent.Close();
                fReceivedEvent = null;
            }

            if (fDisconnectEvent != null)
            {
                fDisconnectEvent.Close();
                fDisconnectEvent = null;
            }

            context.Free(canAccessFinalizable);

            base.Free(canAccessFinalizable);
        }

        #endregion Destructor

        #region Methods

        #region DoOnSSLClientAuthenticate

        internal void DoOnSSLClientAuthenticate(ISocketConnection connection, out string serverName, ref X509Certificate2Collection certs, ref bool checkRevocation)
        {
            serverName = String.Empty;

            if (fOnSSLClientAuthenticateEvent != null)
            {
                fOnSSLClientAuthenticateEvent(connection, out serverName, ref certs, ref checkRevocation);
            }
        }

        #endregion DoOnSSLClientAuthenticate

        #region DoOnSymmetricAuthenticate

        internal void DoOnSymmetricAuthenticate(ISocketConnection connection, out RSACryptoServiceProvider serverKey)
        {
            serverKey = new RSACryptoServiceProvider();
            serverKey.Clear();

            if (fOnSymmetricAuthenticateEvent != null)
            {
                fOnSymmetricAuthenticateEvent(connection, out serverKey);
            }
        }

        #endregion DoOnSymmetricAuthenticate

        #region Connect

        public void Connect()
        {
           
            if (Disposed || context.Connected)
                return;

            fConnectEvent.Reset();
            fExceptionEvent.Reset();
            fDisconnectEvent.Reset();

            context.Host = new SocketClientProvider(CallbackThreadType.ctWorkerThread, context.SocketService, context.DelimiterType, context.Delimiter, context.SocketBufferSize, context.MessageBufferSize);
            SocketClientProvider clientProvider = context.Host as SocketClientProvider;
            SocketConnector connector = clientProvider.AddConnector("SocketClientSync", context.RemoteEndPoint);

            connector.Context.EncryptType = context.EncryptType;
            connector.Context.CompressionType = context.CompressionType;
            connector.Context.CryptoService = context.CryptoService;
  

            WaitHandle[] wait = new WaitHandle[] { fConnectEvent, fExceptionEvent };
            clientProvider.Start();
            
            int signal = WaitHandle.WaitAny(wait, context.ConnectTimeout, false);

            switch (signal)
            {
                case 0:

                    //----- Connect!
                    fLastException = null;
                    context.Connected = true;

                    break;

                case 1:

                    //----- Exception!
                    context.Connected = false;
                    context.SocketConnection = null;

                    clientProvider.Stop();
                    clientProvider.Dispose();
                    clientProvider = null;

                    break;

                default:

                    //----- TimeOut!
                    fLastException = new TimeoutException("Connect timeout.");

                    context.Connected = false;
                    context.SocketConnection = null;

                    clientProvider.Stop();
                    clientProvider.Dispose();
                    clientProvider = null;

                    break;
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
            fLastException = null;

            if (Disposed || !context.Connected)
                return;

            fSentEvent.Reset();
            fExceptionEvent.Reset();

            WaitHandle[] wait = new WaitHandle[] { fSentEvent, fDisconnectEvent, fExceptionEvent };

            context.SocketConnection.BeginSend(buffer);

            int signaled = WaitHandle.WaitAny(wait, context.SentTimeout, false);

            switch (signaled)
            {
                case 0:

                    //----- Sent!
                    fLastException = null;
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
                    fLastException = new TimeoutException("Write timeout.");
                    break;
            }
        }

        #endregion Write

        #region Enqueue

        internal void Enqueue(string data)
        {
            if (!Disposed)
            {
                lock (context.ReceivedQueue)
                {
                    context.ReceivedQueue.Enqueue(data);
                    fReceivedEvent.Set();
                }
            }
        }

        #endregion Enqueue

        #region Read

        public string Read(int timeOut)
        {
            string result = null;

            if (Disposed || !context.Connected)
                return result;

            lock (context.ReceivedQueue)
            {
                if (context.ReceivedQueue.Count > 0)
                {
                    result = context.ReceivedQueue.Dequeue();
                }
            }

            if (result == null)
            {
                WaitHandle[] wait = new WaitHandle[] { fReceivedEvent, fDisconnectEvent, fExceptionEvent };

                int signaled = WaitHandle.WaitAny(wait, timeOut, false);

                switch (signaled)
                {
                    case 0:

                        //----- Received!
                        lock (context.ReceivedQueue)
                        {
                            if (context.ReceivedQueue.Count > 0)
                            {
                                result = context.ReceivedQueue.Dequeue();
                            }
                        }

                        fLastException = null;

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
                        fLastException = new TimeoutException("Read timeout.");
                        break;
                }
            }

            return result;
        }

        #endregion Read

        #region DoDisconnect

        internal void DoDisconnect()
        {
            bool fireEvent = false;

            lock (context.ConnectedSync)
            {
                if (context.Connected)
                {
                    //----- Disconnect!
                    context.Connected = false;
                    context.SocketConnection = null;
                    SocketClientProvider clientProvider = context.Host as SocketClientProvider;
                    if (clientProvider != null)
                    {
                        clientProvider.Stop();
                        clientProvider.Dispose();
                        clientProvider = null;
                    }

                    fireEvent = true;
                }
            }

            if ((fOnDisconnectedEvent != null) && fireEvent)
            {
                fOnDisconnectedEvent();
            }
        }

        #endregion DoDisconnect

        #region Disconnect

        public void Disconnect()
        {
            if (Disposed || !context.Connected)
                return;

            fExceptionEvent.Reset();

            if (context.SocketConnection != null)
            {
                WaitHandle[] wait = new WaitHandle[] { fDisconnectEvent, fExceptionEvent };

                context.SocketConnection.BeginDisconnect();

                int signaled = WaitHandle.WaitAny(wait, context.ConnectTimeout, false);

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
                        fLastException = new TimeoutException("Disconnect timeout.");
                        break;
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
                fOnDisconnectedEvent += value;
            }

            remove
            {
                fOnDisconnectedEvent -= value;
            }
        }

        public event OnSymmetricAuthenticateEvent OnSymmetricAuthenticate
        {
            add
            {
                fOnSymmetricAuthenticateEvent += value;
            }

            remove
            {
                fOnSymmetricAuthenticateEvent -= value;
            }
        }

        public event OnSSLClientAuthenticateEvent OnSSLClientAuthenticate
        {
            add
            {
                fOnSSLClientAuthenticateEvent += value;
            }

            remove
            {
                fOnSSLClientAuthenticateEvent -= value;
            }
        }

     

        internal ManualResetEvent DisconnectEvent
        {
            get
            {
                return fDisconnectEvent;
            }
        }

        internal AutoResetEvent ConnectEvent
        {
            get
            {
                return fConnectEvent;
            }
        }

        internal AutoResetEvent SentEvent
        {
            get
            {
                return fSentEvent;
            }
        }

        internal AutoResetEvent ExceptionEvent
        {
            get
            {
                return fExceptionEvent;
            }
        }

     

        public Exception LastException
        {
            get
            {
                return fLastException;
            }

            internal set
            {
                fLastException = value;
            }
        }

        public SocketContext Context
        {
            get { return context; }
            set { context = value; }
        }

        #endregion Properties
    }

    #endregion SocketClientSync

}