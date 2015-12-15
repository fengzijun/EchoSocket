namespace EchoSocketCore.SocketsEx
{
    internal class WriteData
    {
        #region Fields

        private BaseSocketConnection fConnection;
        private bool fSentByServer;

        #endregion Fields

        #region Constructor

        public WriteData(BaseSocketConnection connection, bool sentByServer)
        {
            fConnection = connection;
            fSentByServer = sentByServer;
        }

        #endregion Constructor

        #region Properties

        public BaseSocketConnection Connection
        {
            get { return fConnection; }
            set { fConnection = value; }
        }

        public bool SentByServer
        {
            get { return fSentByServer; }
        }

        #endregion Properties
    }
}