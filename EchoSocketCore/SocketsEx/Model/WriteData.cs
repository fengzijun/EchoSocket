namespace EchoSocketCore.SocketsEx
{
    internal class WriteData
    {
        #region Fields

        private BaseSocketConnection FConnection;
        private bool FSentByServer;

        #endregion Fields

        #region Constructor

        public WriteData(BaseSocketConnection connection, bool sentByServer)
        {
            FConnection = connection;
            FSentByServer = sentByServer;
        }

        #endregion Constructor

        #region Properties

        public BaseSocketConnection Connection
        {
            get { return FConnection; }
            set { FConnection = value; }
        }

        public bool SentByServer
        {
            get { return FSentByServer; }
        }

        #endregion Properties
    }
}