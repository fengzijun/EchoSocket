namespace EchoSocketCore.SocketsEx
{
    internal class MessageBuffer
    {
        #region Fields

        private byte[] FBuffer;
        private int FCount;
        private bool FSentByServer;

        #endregion Fields

        #region Constructor

        public MessageBuffer(byte[] buffer, int count, bool sentByServer)
        {
            FBuffer = buffer;
            FCount = count;
            FSentByServer = sentByServer;
        }

        #endregion Constructor

        #region Properties

        public byte[] Buffer
        {
            get { return FBuffer; }
            set { FBuffer = value; }
        }

        public int Count
        {
            get { return FCount; }
            set { FCount = value; }
        }

        public bool SentByServer
        {
            get { return FSentByServer; }
            set { FSentByServer = value; }
        }

        #endregion Properties
    }
}