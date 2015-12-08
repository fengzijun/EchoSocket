using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;

namespace EchoSocketCore.SocketsEx
{
    #region Exceptions

    /// <summary>
    /// Reconnect attempted exception.
    /// </summary>
    public class ReconnectAttemptException : Exception
    {
        private int FAttempt;
        private bool FMaxReached;
        private IBaseSocketConnectionCreator FCreator;

        public ReconnectAttemptException(string message, IBaseSocketConnectionCreator creator, Exception innerException, int attempt, bool maxReached)
            : base(message, innerException)
        {
            FAttempt = attempt;
            FMaxReached = maxReached;
            FCreator = creator;
        }

        public int Attempt
        {
            get { return FAttempt; }
        }

        public bool MaxReached
        {
            get { return FMaxReached; }
        }

        public IBaseSocketConnectionCreator Creator
        {
            get { return FCreator; }
        }
    }

    /// <summary>
    /// Bad Delimiter.
    /// </summary>
    public class BadDelimiterException : Exception
    {
        public BadDelimiterException(string message)
            : base(message)
        {
        }
    }

    /// <summary>
    /// Message length is greater than the maximum value.
    /// </summary>
    public class MessageLengthException : Exception
    {
        public MessageLengthException(string message)
            : base(message)
        {
        }
    }

    /// <summary>
    /// Symmetric authentication failure.
    /// </summary>
    public class SymmetricAuthenticationException : Exception
    {
        public SymmetricAuthenticationException(string message)
            : base(message)
        {
        }
    }

    /// <summary>
    /// SSL authentication failure.
    /// </summary>
    public class SSLAuthenticationException : Exception
    {
        public SSLAuthenticationException(string message)
            : base(message)
        {
        }
    }

    /// <summary>
    /// Proxy authentication failure.
    /// </summary>
    public class ProxyAuthenticationException : HttpException
    {
        public ProxyAuthenticationException(int code, string message)
            : base(code, message)
        {
        }
    }

    #endregion Exceptions
}
