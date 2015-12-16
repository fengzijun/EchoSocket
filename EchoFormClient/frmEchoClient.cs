using System;
using System.Net;
using System.Text;
using EchoSocketCore.SocketsEx;

namespace EchoFormClient
{
    public partial class frmEchoClient : EchoFormTemplate.frmEchoForm
    {
        private SocketClient FEchoClient;

        public frmEchoClient()
        {
            InitializeComponent();
        }

        private void cmdStart_Click(object sender, EventArgs e)
        {
            AddConnector();
            FEchoClient.Start();

            Event("Started!");
            Event("---------------------------------");
        }

        private void cmdStop_Click(object sender, EventArgs e)
        {
            FEchoClient.Stop();

            Event("Stopped!");
            Event("---------------------------------");
        }

        private void frmEchoClient_Load(object sender, EventArgs e)
        {
            FEchoClient = new SocketClient(CallbackThreadType.ctWorkerThread, new EchoSocketService.EchoSocketService(FEvent), DelimiterType.dtMessageTailExcludeOnReceive, Encoding.GetEncoding(1252).GetBytes("ALAZ"), 1024 * 2, 1024 * 16);
        }

        private void AddConnector()
        {
            for (int i = 1; i <= 50; i++)
            {
                FEchoClient.AddConnector(String.Empty, new IPEndPoint(IPAddress.Loopback, 8092), null, EncryptType.etNone, CompressionType.ctNone, new EchoCryptService.EchoCryptService());
            }
        }
    }
}