using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;

namespace EchoSocketCore.SocketsEx
{
    public class SocketContext : BaseDisposable
    {
        /// <summary>
        /// Connection user data.
        /// </summary>
        public object UserData { get; set; }

        /// <summary>
        /// Connection Session Id.
        /// </summary>
        public long ConnectionId { get; set; }

        /// <summary>
        /// Connection Creator object.
        /// </summary>
        public BaseSocketConnectionCreator Creator { get; set; }

        /// <summary>
        /// Connection Host object.
        /// </summary>
        public BaseSocketConnectionHost Host { get; set; }

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

        public byte[] Delimiter
        {
            get
            {
                switch (eventProcessing)
                {
                    case EventProcessing.epUser:

                        return Host.Context.Delimiter;

                    case EventProcessing.epEncrypt:

                        return Host.Context.DelimiterEncrypt;

                    case EventProcessing.epProxy:

                        return null;

                    default:

                        return null;
                }
            }
        }


        public DelimiterType DelimiterType
        {
            get
            {
                switch (eventProcessing)
                {
                    case EventProcessing.epUser:

                        return Host.Context.DelimiterType;

                    case EventProcessing.epEncrypt:

                        return DelimiterType.dtMessageTailExcludeOnReceive;

                    case EventProcessing.epProxy:

                        return DelimiterType.dtNone;

                    default:

                        return DelimiterType.dtNone;
                }
            }
        }

        public override void Free(bool canAccessFinalizable)
        {
            SocketHandle = null;
            Stream = null;
            Creator = null;
            Host = null;
            WriteQueue = null;

            base.Free(canAccessFinalizable);
        }
    }
}
