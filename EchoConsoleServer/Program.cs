using System;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Security.Cryptography;
using System.Text;

using EchoSocketService;
using EchoCryptService;

using EchoSocketCore.SocketsEx;

namespace Main
{

    class MainClass
    {

        [STAThread]
        static void Main(string[] args)
        {

            AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(CurrentDomain_UnhandledException);

            //----- Socket Server!
            OnEventDelegate FEvent = new OnEventDelegate(Event);

            SocketServer echoServer = new SocketServer(CallbackThreadType.ctWorkerThread, new EchoSocketService.EchoSocketService(FEvent));

            echoServer.Delimiter = new byte[] { 0xFF, 0x00, 0xFE, 0x01, 0xFD, 0x02 };
            echoServer.DelimiterType = DelimiterType.dtMessageTailExcludeOnReceive;
            
            echoServer.SocketBufferSize = 1024;
            echoServer.MessageBufferSize = 2048;
            
            echoServer.IdleCheckInterval = 60000;
            echoServer.IdleTimeOutValue = 120000;

            //----- Socket Listener!
            SocketListener listener = echoServer.AddListener("Commom Port - 8090", new IPEndPoint(IPAddress.Any, 8090));

            listener.AcceptThreads = 3;
            listener.BackLog = 100;

            listener.EncryptType = EncryptType.etNone;
            listener.CompressionType = CompressionType.ctNone;
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
            catch(Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            echoServer = null;

            Console.WriteLine("Stopped!");
            Console.WriteLine("----------------------");
            Console.ReadLine();

        }

        static void echoServer_OnException(Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("Service Exception! - " + ex.Message);
            Console.WriteLine("------------------------------------------------");
            Console.ResetColor();
        }

        static void Event(string eventMessage)
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

        static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            Console.WriteLine(e.ExceptionObject.ToString());
            Console.ReadLine();
        }

    }

}
