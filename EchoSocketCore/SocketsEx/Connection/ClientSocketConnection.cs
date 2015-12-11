using System.Net.Sockets;

namespace EchoSocketCore.SocketsEx
{
    /// <summary>
    /// Client socket connection implementation.
    /// </summary>
    internal class ClientSocketConnection : BaseSocketConnection, IClientSocketConnection
    {
        #region Constructor

        internal ClientSocketConnection(BaseSocketProvider host, BaseSocketConnectionCreator creator, Socket socket)
            : base(host, creator, socket)
        {
        }

        #endregion Constructor

        #region ISocketConnection Members

        public override IClientSocketConnection AsClientConnection()
        {
            return (this as IClientSocketConnection);
        }

        public override IServerSocketConnection AsServerConnection()
        {
            return null;
        }

        #endregion ISocketConnection Members

        #region IClientSocketConnection Members

        public ProxyInfo ProxyInfo
        {
            get
            {
                return ((SocketConnector)Context.Creator).ProxyInfo;
            }
        }

        public void BeginReconnect()
        {
            Context.Host.BeginReconnect(this);
        }

        #endregion IClientSocketConnection Members
    }
}