using System.Net.Sockets;

namespace EchoSocketCore.SocketsEx
{
    /// <summary>
    /// Server connection implementation.
    /// </summary>
    internal class ServerSocketConnection : BaseSocketConnection, IServerSocketConnection
    {
        #region Constructor

        internal ServerSocketConnection(BaseSocketConnectionHost host, BaseSocketConnectionCreator creator, Socket socket)
            : base(host, creator, socket)
        {
            //-----
        }

        #endregion Constructor

        #region ISocketConnection Members

        public override IClientSocketConnection AsClientConnection()
        {
            return null;
        }

        public override IServerSocketConnection AsServerConnection()
        {
            return (this as IServerSocketConnection);
        }

        #endregion ISocketConnection Members

        #region IServerSocketConnection Members

        public void BeginSendToAll(byte[] buffer, bool includeMe)
        {
            BaseHost.BeginSendToAll(this, buffer, includeMe);
        }

        public void BeginSendTo(ISocketConnection connection, byte[] buffer)
        {
            BaseHost.BeginSendTo((BaseSocketConnection)connection, buffer);
        }

        #endregion IServerSocketConnection Members
    }
}