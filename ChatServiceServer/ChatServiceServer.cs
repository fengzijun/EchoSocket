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

            chatServer.Context.DelimiterEncrypt = new byte[] { 0xAA, 0xFF, 0xAA };
            chatServer.Context.DelimiterType = DelimiterType.dtMessageTailExcludeOnReceive;

            chatServer.Context.SocketBufferSize = 1024;
            chatServer.Context.MessageBufferSize = 512;

            //----- Socket Listener!
            SocketListener listener = chatServer.AddListener("Char Server", new IPEndPoint(IPAddress.Any, 8090));

            listener.AcceptThreads = 3;
            listener.BackLog = 50;

            listener.Context.CompressionType = CompressionType.ctNone;
            listener.Context.EncryptType = EncryptType.etRijndael;
            listener.Context.CryptoService = new ChatCryptService.ChatCryptService();

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