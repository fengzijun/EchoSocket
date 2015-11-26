using System;
using System.Net;
using EchoSocketCore.SocketsEx;
using EchoSocketService;

namespace Main
{
    internal class MainClass
    {
        [STAThread]
        private static void Main(string[] args)
        {
            AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(CurrentDomain_UnhandledException);

            EncryptType et = EncryptType.etNone;
            CompressionType ct = CompressionType.ctNone;
            int port = 8090;
            int connections = 50;

            if (args.Length >= 1)
            {
                port = Convert.ToInt32(args[0]);
            }

            if (args.Length >= 2)
            {
                et = (EncryptType)Enum.Parse(typeof(EncryptType), args[1], true);
            }

            if (args.Length >= 3)
            {
                ct = (CompressionType)Enum.Parse(typeof(CompressionType), args[2], true);
            }

            //----- Socket Client!
            OnEventDelegate FEvent = new OnEventDelegate(Event);

            SocketClient echoClient = new SocketClient(CallbackThreadType.ctWorkerThread, new EchoSocketService.EchoSocketService(FEvent));

            echoClient.Context.Delimiter = new byte[] { 0xFF, 0x00, 0xFE, 0x01, 0xFD, 0x02 };
            echoClient.Context.DelimiterType = DelimiterType.dtMessageTailExcludeOnReceive;

            echoClient.Context.SocketBufferSize = 1024;
            echoClient.Context.MessageBufferSize = 2048;

            echoClient.Context.IdleCheckInterval = 60000;
            echoClient.Context.IdleTimeOutValue = 120000;

            //----- Socket Connectors!
            SocketConnector connector = null;

            for (int i = 0; i < connections; i++)
            {
                connector = echoClient.AddConnector("Connector " + i.ToString(), new IPEndPoint(IPAddress.Loopback, 8090));

                /*
                connector.ProxyInfo = new ProxyInfo(
                    ProxyType.ptHTTP,
                    new IPEndPoint(IPAddress.Loopback, 8080),
                    new NetworkCredential("test", "1234"));
                */

                connector.Context.CryptoService = new EchoCryptService.EchoCryptService();
                connector.Context.CompressionType = ct;
                connector.Context.EncryptType = et;

                connector.ReconnectAttempts = 10;
                connector.ReconnectAttemptInterval = 5000;
            }

            Console.Title = "EchoConsoleClient / " + connections.ToString() + " Connections / " + Enum.GetName(typeof(EncryptType), et) + " / " + Enum.GetName(typeof(CompressionType), ct);

            echoClient.Start();

            Console.WriteLine("Started!");
            Console.WriteLine("----------------------");

            Console.ReadLine();

            try
            {
                echoClient.Stop();
                echoClient.Dispose();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            echoClient = null;

            Console.WriteLine("Stopped!");
            Console.WriteLine("----------------------");
            Console.ReadLine();
        }

        private static void Event(string eventMessage)
        {
            Console.WriteLine(eventMessage);
        }

        private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            Console.WriteLine(e.ExceptionObject.ToString());
            Console.ReadLine();
        }
    }
}