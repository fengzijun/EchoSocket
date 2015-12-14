using System.Net.Sockets;

namespace EchoSocketCore.SocketsEx
{
    /// <summary>
    /// Client socket connection implementation.
    /// </summary>
    internal class ClientSocketConnection : BaseSocketConnection, IClientSocketConnection
    {
       

        internal ClientSocketConnection(SocketContext context)
            : base(context)
        {
        }




        public override IClientSocketConnection AsClientConnection()
        {
            return (this as IClientSocketConnection);
        }

        public override IServerSocketConnection AsServerConnection()
        {
            return null;
        }


      

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


    }
}