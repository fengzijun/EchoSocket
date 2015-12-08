using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace EchoSocketCore.SocketsEx
{
    public interface IServerSocketConnection : ISocketConnection
    {
        /// <summary>
        /// Begin send data to all server connections.
        /// </summary>
        /// <param name="buffer">
        /// Data to be sent.
        /// </param>
        /// <param name="includeMe">
        /// Includes the current connection in send磗 loop
        /// </param>
        void BeginSendToAll(byte[] buffer, bool includeMe);

        /// <summary>
        /// Begin send data to the connection.
        /// </summary>
        /// <param name="connection">
        /// The connection that the data will be sent.
        /// </param>
        /// <param name="buffer">
        /// Data to be sent.
        /// </param>
        void BeginSendTo(ISocketConnection connection, byte[] buffer);

    }
}
