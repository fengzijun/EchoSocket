using System;

namespace EchoSocketCore.SocketsEx
{
    /// <summary>
    /// Base event arguments for connection events.
    /// </summary>
    public class ConnectionEventArgs : EventArgs
    {
        #region Fields

        private ISocketConnection FConnection;

        #endregion Fields

        #region Constructor

        public ConnectionEventArgs(ISocketConnection connection)
        {
            FConnection = connection;
        }

        #endregion Constructor

        #region Properties

        /// <summary>
        /// Gets the ISocketConnection from event.
        /// </summary>
        public ISocketConnection Connection
        {
            get { return FConnection; }
        }

        #endregion Properties
    }
}