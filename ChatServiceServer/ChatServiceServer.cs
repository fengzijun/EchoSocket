using System.Net;
using System.ServiceProcess;
using EchoSocketCore.SocketsEx;

namespace ChatServiceServer
{
    public partial class ChatServiceServer : ServiceBase
    {
        private SocketServer chatServer;

        public ChatServiceServer()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            chatServer = new SocketServer(CallbackThreadType.ctWorkerThread, new ChatSocketService.ChatSocketService());

            chatServer.Delimiter = new byte[] { 0xAA, 0xFF, 0xAA };
            chatServer.DelimiterType = DelimiterType.dtMessageTailExcludeOnReceive;

            chatServer.SocketBufferSize = 1024;
            chatServer.MessageBufferSize = 512;

            //----- Socket Listener!
            SocketListener listener = chatServer.AddListener("Char Server", new IPEndPoint(IPAddress.Any, 8090));

            listener.AcceptThreads = 3;
            listener.BackLog = 50;

            listener.CompressionType = CompressionType.ctNone;
            listener.EncryptType = EncryptType.etRijndael;
            listener.CryptoService = new ChatCryptService.ChatCryptService();

            chatServer.Start();
        }

        protected override void OnStop()
        {
            chatServer.Stop();
            chatServer.Dispose();
            chatServer = null;
        }
    }
}