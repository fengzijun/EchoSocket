using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace EchoSocketCore.SocketsEx
{
    public interface IClientSocketConnection : ISocketConnection
    {
        /// <summary>
        /// Proxy information.
        /// </summary>
        ProxyInfo ProxyInfo
        {
            get;
        }

        /// <summary>
        /// Begin reconnect the connection.
        /// </summary>
        void BeginReconnect();
    }
}
