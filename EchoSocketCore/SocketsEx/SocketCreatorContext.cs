using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;

namespace EchoSocketCore.SocketsEx
{
    public class SocketCreatorContext:BaseDisposable
    {
        public string Name { get; set; }

        public CompressionType CompressionType { get; set; }

        public EncryptType EncryptType { get; set; }

        public BaseSocketConnectionHost Host { get; set; }

        public IPEndPoint LocalEndPoint { get; set; }

        public IPEndPoint RemotEndPoint { get; set; }

        public ICryptoService CryptoService { get; set; }


        public override void Free(bool canAccessFinalizable)
        {
            CryptoService = null;
            Host = null;

            base.Free(canAccessFinalizable);
        }
    }
}
