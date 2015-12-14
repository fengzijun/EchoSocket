//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.ServiceModel.Channels;
//using System.Text;
//using System.Threading;

//namespace EchoSocketCore.SocketsEx
//{
//    public class SocketProviderContext : BaseDisposable
//    {

//        public SocketProviderContext()
//        {
//            connectionId = 1000;
//        }

//        private long connectionId ;

//        public int SocketBufferSize { get; set; }

//        public int MessageBufferSize { get; set; }

//        public byte[] Delimiter { get; set; }

//        public DelimiterType DelimiterType { get; set; }

//        public int IdleCheckInterval { get; set; }

//        public int IdleTimeOutValue { get; set; }

//        public HostType HostType { get; set; }

//        public bool Active { get; set; }

//        public object SyncActive { get; set; }


//        public CallbackThreadType CallbackThreadType { get; set; }

//        public Dictionary<long, BaseSocketConnection> SocketConnections { get; set; }

//        public BufferManager BufferManager { get; set; }

//        public List<BaseSocketConnectionCreator> SocketCreators { get; set; }

//        public byte[] DelimiterEncrypt { get; set; }

//        public ISocketService SocketService { get; set; }

//        public override void Free(bool canAccessFinalizable)
//        {
//            if (SocketConnections != null)
//                SocketConnections = null;

//            if (SocketCreators != null)
//                SocketCreators = null;

//            if (BufferManager != null)
//                BufferManager = null;

//            if (SocketService != null)
//                SocketService = null;

//            base.Free(canAccessFinalizable);
//        }

//        public long CurrentConnectionId { get { return connectionId; } }

//        public long GenerateConnectionId()
//        {
//            return Interlocked.Increment(ref connectionId);
//        }

//    }
//}
