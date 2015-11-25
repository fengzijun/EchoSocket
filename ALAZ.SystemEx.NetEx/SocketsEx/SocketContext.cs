using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace EchoSocketCore.SocketsEx
{
    public class SocketContext
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
    }
}
