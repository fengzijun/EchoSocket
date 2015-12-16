using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;

namespace EchoSocketCore.SocketsEx.Context
{
    public class SocketClientSyncContext : BaseDisposable
    {
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

        private ISocketConnection FSocketConnection;
        private SocketClient FSocketClient;
        private SocketClientSyncSocketService FSocketClientEvents;
        private SocketClientSyncCryptService FCryptClientEvents;

        private int FConnectTimeout;
        private bool FConnected;
        private object FConnectedSync;

        private int FSentTimeout;

        private Queue<string> FReceivedQueue;

        public SocketClient SocketClient
        {
            get { return FSocketClient; }
            set { FSocketClient = value; }
        }

        public SocketClientSyncSocketService SocketClientEvents
        {
            get { return FSocketClientEvents; }
            set { FSocketClientEvents = value; }
        }

        public SocketClientSyncCryptService CryptClientEvents
        {
            get { return FCryptClientEvents; }
            set { FCryptClientEvents = value; }
        }

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


        public override void Free(bool canAccessFinalizable)
        {
            FSocketClientEvents = null;
            CryptClientEvents = null;
            ReceivedQueue = null;
            SocketConnection = null;
            ProxyInfo = null;
            SocketClient = null;

            base.Free(canAccessFinalizable);
        }

    }
}
