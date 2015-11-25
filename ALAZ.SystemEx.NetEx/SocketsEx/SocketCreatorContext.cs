using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;

namespace EchoSocketCore.SocketsEx
{
    public class SocketCreatorContext
    {
        public string Name { get; set; }

        public CompressionType CompressionType { get; set; }

        public EncryptType EncryptType { get; set; }

        public BaseSocketConnectionHost Host { get; set; }

        public IPEndPoint localEndPoint { get; set; }

    }
}
