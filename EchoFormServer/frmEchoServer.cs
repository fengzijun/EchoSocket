using System;
using System.Net;
using System.Text;
using EchoSocketCore.SocketsEx;

namespace EchoFormServer
{
    public partial class frmEchoServer : EchoFormTemplate.frmEchoForm
    {
        private SocketServerProvider FEchoServer;

        public frmEchoServer()
        {
            InitializeComponent();
        }

        private void frmEchoServer_Load(object sender, EventArgs e)
        {
            FEchoServer = new SocketServerProvider(CallbackThreadType.ctWorkerThread, new EchoSocketService.EchoSocketService(FEvent), DelimiterType.dtMessageTailExcludeOnReceive, Encoding.GetEncoding(1252).GetBytes("ALAZ"), 1024 * 2, 1024 * 16);
        }

        private void AddListener()
        {
            FEchoServer.AddListener(String.Empty, new IPEndPoint(IPAddress.Any, 8092), EncryptType.etNone, CompressionType.ctNone, new EchoCryptService.EchoCryptService(), 50, 3);
        }

        private void cmdStart_Click(object sender, EventArgs e)
        {
            AddListener();
            FEchoServer.Start();

            Event("Started!");
            Event("---------------------------------");
        }

        private void cmdStop_Click(object sender, EventArgs e)
        {
            FEchoServer.Stop();

            Event("Stopped!");
            Event("---------------------------------");
        }
    }
}