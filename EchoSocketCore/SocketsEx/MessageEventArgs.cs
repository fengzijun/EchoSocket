namespace EchoSocketCore.SocketsEx
{
    /// <summary>
    /// Message event arguments for message events.
    /// </summary>
    public class MessageEventArgs : ConnectionEventArgs
    {
        #region Fields

        private byte[] FBuffer;
        private bool FSentByserver;

        #endregion Fields

        #region Constructor

        public MessageEventArgs(ISocketConnection connection, byte[] buffer, bool sentByServer)
            : base(connection)
        {
            FBuffer = buffer;
            FSentByserver = sentByServer;
        }

        #endregion Constructor

        #region Properties

        /// <summary>
        /// Gets sent or received buffer.
        /// </summary>
        public byte[] Buffer
        {
            get { return FBuffer; }
        }

        /// <summary>
        /// Indicates if event was fired by server´s BeginSendTo() or BeginSendToAll().
        /// </summary>
        public bool SentByServer
        {
            get { return FSentByserver; }
        }

        #endregion Properties
    }
}