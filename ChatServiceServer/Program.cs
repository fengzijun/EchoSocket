using System.ServiceProcess;

namespace ChatServiceServer
{
    internal static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        private static void Main()
        {
            ServiceBase[] ServicesToRun;
            ServicesToRun = new ServiceBase[] { new ChatServiceServer() };
            ServiceBase.Run(ServicesToRun);
        }
    }
}