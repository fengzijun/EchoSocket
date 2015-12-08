using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Security;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace EchoSocketCore.SocketsEx
{
    public interface ICryptoService
    {
        /// <summary>
        /// Fired when symmetric encryption is used.
        /// </summary>
        /// <param name="serverKey">
        /// The RSA provider used to encrypt symmetric IV and Key.
        /// </param>
        void OnSymmetricAuthenticate(ISocketConnection connection, out RSACryptoServiceProvider serverKey);

        /// <summary>
        /// Fired when SSL encryption is used in client host.
        /// </summary>
        /// <param name="ServerName">
        /// The host name in certificate.
        /// </param>
        /// <param name="certs">
        /// The certification collection to be used (null if not using client certification).
        /// </param>
        /// <param name="checkRevocation">
        /// Indicates if the certificated must be checked for revocation.
        /// </param>
        void OnSSLClientAuthenticate(ISocketConnection connection, out string ServerName, ref X509Certificate2Collection certs, ref bool checkRevocation);

        /// <summary>
        ///
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="serverCertificate"></param>
        /// <param name="chain"></param>
        /// <param name="sslPolicyErrors"></param>
        /// <param name="acceptCertificate"></param>
        void OnSSLClientValidateServerCertificate(X509Certificate serverCertificate, X509Chain chain, SslPolicyErrors sslPolicyErrors, out bool acceptCertificate);

        /// <summary>
        /// Fired when SSL encryption is used in server host.
        /// </summary>
        /// <param name="certificate">
        /// The certificate to be used.
        /// </param>
        /// <param name="clientAuthenticate">
        /// Indicates if client connection will be authenticated (uses certificate).
        /// </param>
        /// <param name="checkRevocation">
        /// Indicates if the certificated must be checked for revocation.
        /// </param>
        void OnSSLServerAuthenticate(ISocketConnection connection, out X509Certificate2 certificate, out bool clientAuthenticate, ref bool checkRevocation);
    }
}
