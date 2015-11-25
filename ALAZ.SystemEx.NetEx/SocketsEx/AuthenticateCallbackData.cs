using System.Net.Security;

namespace EchoSocketCore.SocketsEx
{
    /// <summary>
    /// Keeps connection authenticate information between callbacks.
    /// </summary>
    internal class AuthenticateCallbackData
    {
        #region Fields

        private BaseSocketConnection FConnection;
        private SslStream FStream;
        private HostType FHostType;

        #endregion Fields

        #region Constructor

        public AuthenticateCallbackData(BaseSocketConnection connection, SslStream stream, HostType hostType)
        {
            FConnection = connection;
            FStream = stream;
            FHostType = hostType;
        }

        #endregion Constructor

        #region Properties

        public BaseSocketConnection Connection
        {
            get { return FConnection; }
        }

        public SslStream Stream
        {
            get { return FStream; }
        }

        public HostType HostType
        {
            get { return FHostType; }
        }

        #endregion Properties
    }
}