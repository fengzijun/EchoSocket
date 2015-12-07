using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace EchoSocketCore.SocketsEx
{
    public class SocketClientSyncCryptService : BaseCryptoService
    {
        #region Fields

        private SocketClientSync FSocketClient;

        #endregion Fields

        #region Constructor

        public SocketClientSyncCryptService(SocketClientSync client)
        {
            FSocketClient = client;
        }

        #endregion Constructor

        #region Methods

        public override void OnSymmetricAuthenticate(ISocketConnection connection, out RSACryptoServiceProvider serverKey)
        {
            FSocketClient.DoOnSymmetricAuthenticate(connection, out serverKey);
        }

        public override void OnSSLClientAuthenticate(ISocketConnection connection, out string serverName, ref X509Certificate2Collection certs, ref bool checkRevocation)
        {
            FSocketClient.DoOnSSLClientAuthenticate(connection, out serverName, ref certs, ref checkRevocation);
        }

        #endregion Methods
    }
}
