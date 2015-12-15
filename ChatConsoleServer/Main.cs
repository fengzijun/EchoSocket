using System;
using System.Net;
using System.Threading;

using ChatSocketService;
using EchoSocketCore.SocketsEx;

namespace ChatServer
{
    public class ChatServer
    {
        [STAThread]
        private static void Main(string[] args)
        {
            SocketServerProvider chatServer = new SocketServerProvider(CallbackThreadType.ctWorkerThread, new ChatSocketService.ChatSocketService());

            chatServer.Context.DelimiterUserEncrypt = new byte[] { 0xAA, 0xFF, 0xAA };
            chatServer.Context.DelimiterUserType = DelimiterType.dtMessageTailExcludeOnReceive;

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

            Console.WriteLine(" Chat Server Started!");
            Console.WriteLine("--------------------------------------");

            string key;
            int iot = 0;
            int wt = 0;

            do
            {
                Console.WriteLine(" Press T <ENTER> for Threads");
                Console.WriteLine(" Press C <ENTER> for Clients");
                Console.WriteLine(" Press S <ENTER> for Stop Server");
                Console.WriteLine("--------------------------------------");
                Console.Write(" -> ");

                key = Console.ReadLine().ToUpper();

                if (key.Equals("T"))
                {
                    ThreadPool.GetAvailableThreads(out wt, out iot);

                    Console.WriteLine("--------------------------------------");
                    Console.WriteLine(" I/O Threads " + iot.ToString());
                    Console.WriteLine(" Worker Threads " + wt.ToString());
                    Console.WriteLine("--------------------------------------");
                }

                if (key.Equals("C"))
                {
                    ISocketConnection[] infos = chatServer.GetConnections();

                    Console.WriteLine("\r\n--------------------------------------");
                    Console.WriteLine(" " + infos.Length.ToString() + " user(s)!\r\n");

                    foreach (ISocketConnection info in infos)
                    {
                        Console.WriteLine(" Connection Id " + info.ConnectionId.ToString());
                        Console.WriteLine(" User Name " + ((ConnectionData)info.Context.UserData).UserName);
                        Console.WriteLine(" Ip Address " + info.Context.RemoteEndPoint.Address.ToString());

                        Console.WriteLine("--------------------------------------");
                    }
                }
            } while (!key.Equals("S"));

            Console.WriteLine(" Chat Server Stopping!");
            Console.WriteLine("--------------------------------------");

            try
            {
                chatServer.Stop();
                chatServer.Dispose();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            chatServer = null;

            Console.WriteLine(" Chat Server Stopped!");
            Console.WriteLine("--------------------------------------");

            Console.ReadLine();
        }
    }
}