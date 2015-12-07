using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace EchoSocketCore.SocketsEx
{
    public interface ISocketService
    {
        /// <summary>
        /// Fired when connected.
        /// </summary>
        /// <param name="e">
        /// Information about the connection.
        /// </param>
        void OnConnected(ConnectionEventArgs e);

        /// <summary>
        /// Fired when data arrives.
        /// </summary>
        /// <param name="e">
        /// Information about the Message.
        /// </param>
        void OnReceived(MessageEventArgs e);

        /// <summary>
        /// Fired when data is sent.
        /// </summary>
        /// <param name="e">
        /// Information about the Message.
        /// </param>
        void OnSent(MessageEventArgs e);

        /// <summary>
        /// Fired when disconnected.
        /// </summary>
        /// <param name="e">
        /// Information about the connection.
        /// </param>
        void OnDisconnected(ConnectionEventArgs e);

        /// <summary>
        /// Fired when exception occurs.
        /// </summary>
        /// <param name="e">
        /// Information about the exception and connection.
        /// </param>
        void OnException(ExceptionEventArgs e);
    }
}
