using System.Net.Sockets;

namespace EchoSocketCore.SocketsEx
{
    /// <summary>
    /// Server connection implementation.
    /// </summary>
    internal class ServerSocketConnection : BaseSocketConnection, IServerSocketConnection
    {
   
        public ServerSocketConnection(SocketContext context):base(context)
        {
         
        }



        public override IClientSocketConnection AsClientConnection()
        {
            return null;
        }

        public override IServerSocketConnection AsServerConnection()
        {
            return (this as IServerSocketConnection);
        }



    

        public void BeginSendToAll(byte[] buffer, bool includeMe)
        {
            Context.Host.BeginSendToAll(this, buffer, includeMe);
        }

        public void BeginSendTo(ISocketConnection connection, byte[] buffer)
        {
            Context.Host.BeginSendTo((BaseSocketConnection)connection, buffer);
        }


    }
}