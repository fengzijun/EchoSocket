using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Security;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace EchoSocketCore.SocketsEx
{
    public abstract class BaseCryptoService : ICryptoService
    {
        #region ICryptoService Members

        public virtual void OnSymmetricAuthenticate(ISocketConnection connection, out RSACryptoServiceProvider serverKey)
        {
            serverKey = new RSACryptoServiceProvider();
            serverKey.Clear();
        }

        public virtual void OnSSLClientAuthenticate(ISocketConnection connection, out string serverName, ref X509Certificate2Collection certs, ref bool checkRevocation)
        {
            serverName = String.Empty;
        }

        public virtual void OnSSLServerAuthenticate(ISocketConnection connection, out X509Certificate2 certificate, out bool clientAuthenticate, ref bool checkRevocation)
        {
            certificate = new X509Certificate2();
            clientAuthenticate = true;
        }

        public virtual void OnSSLClientValidateServerCertificate(X509Certificate serverCertificate, X509Chain chain, SslPolicyErrors sslPolicyErrors, out bool acceptCertificate)
        {
            acceptCertificate = false;
        }

        #endregion ICryptoService Members
    }
}
