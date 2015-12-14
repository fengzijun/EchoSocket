using System.Net;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Threading;

namespace EchoSocketCore.SocketsEx
{
    /// <summary>
    /// Connection creator using in BaseSocketProvider.
    /// </summary>
    public abstract class BaseSocketConnectionCreator : BaseDisposable, ISocket
    {
        private SocketContext context;


        public BaseSocketConnectionCreator(SocketContext context)
        {
            //Context = new SocketContext
            //{
            //    CompressionType = compressionType,
            //    CryptoService = cryptoService,
            //    Host = host,
            //    EncryptType = encryptType,
            //    Name = name,
            //    LocalEndPoint = localEndPoint,
            //     RemoteEndPoint = remoteEndPoint
            //};

            //fWaitCreatorsDisposing = new ManualResetEvent(false);

            this.context = context;
        }


        public override void Free(bool canAccessFinalizable)
        {


            //if (fWaitCreatorsDisposing != null)
            //{
            //    fWaitCreatorsDisposing.Set();
            //    fWaitCreatorsDisposing.Close();
            //    fWaitCreatorsDisposing = null;
            //}

            Context.Free(canAccessFinalizable);

            base.Free(canAccessFinalizable);
        }


        internal bool ValidateServerCertificateCallback(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
        {
            bool acceptCertificate = false;
            Context.CryptoService.OnSSLClientValidateServerCertificate(certificate, chain, sslPolicyErrors, out acceptCertificate);

            return acceptCertificate;
        }


        public abstract void Start();

        public abstract void Stop();



        public SocketContext Context { 
            get {return context;}
            set { context = value; }
        }

    }
}