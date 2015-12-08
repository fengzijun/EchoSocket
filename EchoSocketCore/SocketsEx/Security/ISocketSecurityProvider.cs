using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace EchoSocketCore.SocketsEx
{
    public interface ISocketSecurityProvider
    {
       

        MemoryStream DecryptForServer();

        MemoryStream EcryptForClient();

        AuthMessage DecryptForClient();

        //bool Verify();

        //RSACryptoServiceProvider ServerPrivateKey { get; set; }
    }
}
