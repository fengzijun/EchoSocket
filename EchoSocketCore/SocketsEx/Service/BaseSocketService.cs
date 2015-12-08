using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace EchoSocketCore.SocketsEx
{
    public abstract class BaseSocketService : ISocketService
    {
        #region ISocketService Members

        public virtual void OnConnected(ConnectionEventArgs e)
        {
        }

        public virtual void OnSent(MessageEventArgs e)
        {
        }

        public virtual void OnReceived(MessageEventArgs e)
        {
        }

        public virtual void OnDisconnected(ConnectionEventArgs e)
        {
        }

        public virtual void OnException(ExceptionEventArgs e)
        {
        }

        #endregion ISocketService Members
    }
}
