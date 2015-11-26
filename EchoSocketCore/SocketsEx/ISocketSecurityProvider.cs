using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace EchoSocketCore.SocketsEx
{
    public interface ISocketSecurityProvider
    {
        ICryptoTransform CreateEncryptor();
     
        ICryptoTransform CreateDecryptor();

        bool Verify();
    }
}
