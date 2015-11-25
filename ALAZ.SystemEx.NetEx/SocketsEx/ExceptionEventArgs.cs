using System;

namespace EchoSocketCore.SocketsEx
{
    /// <summary>
    /// Exception event arguments for exception event.
    /// </summary>
    public class ExceptionEventArgs : ConnectionEventArgs
    {
        #region Fields

        private Exception FException;

        #endregion Fields

        #region Constructor

        public ExceptionEventArgs(ISocketConnection connection, Exception exception)
            : base(connection)
        {
            FException = exception;
        }

        #endregion Constructor

        #region Properties

        public Exception Exception
        {
            get { return FException; }
        }

        #endregion Properties
    }
}