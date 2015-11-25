using System;
using System.Net;
using System.Threading;
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

            //----- Socket Server!
            OnEventDelegate FEvent = new OnEventDelegate(Event);

            SocketServer echoServer = new SocketServer(CallbackThreadType.ctWorkerThread, new EchoSocketService.EchoSocketService(FEvent));

            echoServer.Context.Delimiter = new byte[] { 0xFF, 0x00, 0xFE, 0x01, 0xFD, 0x02 };
            echoServer.Context.DelimiterType = DelimiterType.dtMessageTailExcludeOnReceive;

            echoServer.Context.SocketBufferSize = 1024;
            echoServer.Context.MessageBufferSize = 2048;

            echoServer.Context.IdleCheckInterval = 60000;
            echoServer.Context.IdleTimeOutValue = 120000;

            //----- Socket Listener!
            SocketListener listener = echoServer.AddListener("Commom Port - 8090", new IPEndPoint(IPAddress.Any, 8090));

            listener.AcceptThreads = 3;
            listener.BackLog = 100;

            listener.Context.EncryptType = EncryptType.etNone;
            listener.Context.CompressionType = CompressionType.ctNone;
            listener.CryptoService = new EchoCryptService.EchoCryptService();

            echoServer.Start();

            Console.WriteLine("Started!");
            Console.WriteLine("----------------------");

            int iot = 0;
            int wt = 0;

            ThreadPool.GetAvailableThreads(out wt, out iot);
            Console.WriteLine("Threads Work - " + wt.ToString());
            Console.WriteLine("Threads I/O  - " + iot.ToString());

            string s;

            do
            {
                s = Console.ReadLine();

                if (s.Equals("g"))
                {
                    ThreadPool.GetAvailableThreads(out wt, out iot);
                    Console.WriteLine("Threads Work " + iot.ToString());
                    Console.WriteLine("Threads I/O  " + wt.ToString());
                }
            } while (s.Equals("g"));

            try
            {
                echoServer.Stop();
                echoServer.Dispose();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            echoServer = null;

            Console.WriteLine("Stopped!");
            Console.WriteLine("----------------------");
            Console.ReadLine();
        }

        private static void echoServer_OnException(Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("Service Exception! - " + ex.Message);
            Console.WriteLine("------------------------------------------------");
            Console.ResetColor();
        }

        private static void Event(string eventMessage)
        {
            if (eventMessage.Contains("Exception"))
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(eventMessage);
                Console.ResetColor();
            }
            else
            {
                Console.WriteLine(eventMessage);
            }
        }

        private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            Console.WriteLine(e.ExceptionObject.ToString());
            Console.ReadLine();
        }
    }
}