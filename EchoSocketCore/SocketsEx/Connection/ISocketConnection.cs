using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;

namespace EchoSocketCore.SocketsEx
{
    public interface ISocketConnection
    {
        /// <summary>
        /// Set Socket Time To Live option
        /// </summary>
        /// <param name="value">
        /// Value for TTL in seconds
        /// </param>
        void SetTTL(short value);

        /// <summary>
        /// Set Socket Linger option.
        /// </summary>
        /// <param name="lo">
        /// LingerOption value to be set
        /// </param>
        void SetLinger(LingerOption lo);

        /// <summary>
        /// Set Socket Nagle algoritm.
        /// </summary>
        /// <param name="value">
        /// Enable/Disable value
        /// </param>
        void SetNagle(bool value);

        /// <summary>
        /// Represents the connection as a IClientSocketConnection.
        /// </summary>
        /// <returns>
        ///
        /// </returns>
        IClientSocketConnection AsClientConnection();

        /// <summary>
        /// Represents the connection as a IServerSocketConnection.
        /// </summary>
        /// <returns></returns>
        IServerSocketConnection AsServerConnection();

        /// <summary>
        /// Get the connection from the connectionId.
        /// </summary>
        /// <param name="connectionId">
        /// The connectionId.
        /// </param>
        /// <returns>
        /// ISocketConnection to use.
        /// </returns>
        ISocketConnection GetConnectionById(long connectionId);

        /// <summary>
        /// Get all the connections.
        /// </summary>
        ISocketConnection[] GetConnections();

        /// <summary>
        /// Begin send data.
        /// </summary>
        /// <param name="buffer">
        /// Data to be sent.
        /// </param>
        void BeginSend(byte[] buffer);

        /// <summary>
        /// Begin receive the data.
        /// </summary>
        void BeginReceive();

        /// <summary>
        /// Begin disconnect the connection.
        /// </summary>
        void BeginDisconnect();

        SocketContext Context { get; set; }
    }
}
