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
        private int fAttempt;
        private bool fMaxReached;
        private ISocket fCreator;

       public ReconnectAttemptException(string message, ISocket creator, Exception innerException, int attempt, bool maxReached)
            : base(message, innerException)
        {
            fAttempt = attempt;
            fMaxReached = maxReached;
            fCreator = creator;
        }

        public int Attempt
        {
            get { return fAttempt; }
        }

        public bool MaxReached
        {
            get { return fMaxReached; }
        }

        public ISocket Creator
        {
            get { return fCreator; }
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
