using System.Net;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Threading;

namespace EchoSocketCore.SocketsEx
{
    /// <summary>
    /// Connection creator using in BaseSocketConnectionHost.
    /// </summary>
    public abstract class BaseSocketConnectionCreator : BaseDisposable, IBaseSocketConnectionCreator
    {


        #region Constructor

        public BaseSocketConnectionCreator(BaseSocketConnectionHost host, string name, IPEndPoint localEndPoint, EncryptType encryptType, CompressionType compressionType, ICryptoService cryptoService, IPEndPoint remoteEndPoint)
        {
            Context = new SocketCreatorContext
            {
                CompressionType = compressionType,
                CryptoService = cryptoService,
                Host = host,
                EncryptType = encryptType,
                Name = name,
                LocalEndPoint = localEndPoint,
                RemotEndPoint = remoteEndPoint
            };

            //fWaitCreatorsDisposing = new ManualResetEvent(false);

        }

        public BaseSocketConnectionCreator(BaseSocketConnectionHost host, string name, IPEndPoint localEndPoint, EncryptType encryptType, CompressionType compressionType, ICryptoService cryptoService)
            : this(host, name, localEndPoint, encryptType, compressionType, cryptoService, null)
        {

        }

        public BaseSocketConnectionCreator(BaseSocketConnectionHost host, string name, IPEndPoint localEndPoint, EncryptType encryptType, ICryptoService cryptoService, IPEndPoint remoteEndPoint) :
            this(host, name, localEndPoint, encryptType, CompressionType.ctNone, cryptoService, remoteEndPoint)
        { }

        public BaseSocketConnectionCreator(BaseSocketConnectionHost host, string name, IPEndPoint localEndPoint, EncryptType encryptType, ICryptoService cryptoService) :
            this(host, name, localEndPoint, encryptType, CompressionType.ctNone, cryptoService, null)
        { }

        public BaseSocketConnectionCreator(BaseSocketConnectionHost host, string name, IPEndPoint localEndPoint, ICryptoService cryptoService, IPEndPoint remoteEndPoint) :
            this(host, name, localEndPoint, EncryptType.etNone, CompressionType.ctNone, cryptoService, remoteEndPoint)
        { }

        public BaseSocketConnectionCreator(BaseSocketConnectionHost host, string name, IPEndPoint localEndPoint, ICryptoService cryptoService) :
            this(host, name, localEndPoint, EncryptType.etNone, CompressionType.ctNone, cryptoService, null)
        { }

        #endregion Constructor

        #region Destructor

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

        #endregion Destructor

        #region Methods

        #region ValidateServerCertificateCallback

        internal bool ValidateServerCertificateCallback(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
        {
            bool acceptCertificate = false;
            Context.CryptoService.OnSSLClientValidateServerCertificate(certificate, chain, sslPolicyErrors, out acceptCertificate);

            return acceptCertificate;
        }

        #endregion ValidateServerCertificateCallback

        #region Abstract Methods

        public abstract void Start();

        public abstract void Stop();

        #endregion Abstract Methods

        #endregion Methods

        #region Properties

        public SocketCreatorContext Context { get; set; }

        #endregion Properties

        public void AddCreator()
        {
            if (!Disposed)
            {
                lock (Context.Host.Context.SocketCreators)
                {
                    Context.Host.Context.SocketCreators.Add(this);
                }
            }
        }

        public void RemoveCreator()
        {
            if (!Disposed)
            {
                lock (Context.Host.Context.SocketCreators)
                {
                    Context.Host.Context.SocketCreators.Remove(this);

                    if (Context.Host.Context.SocketCreators.Count <= 0)
                    {
                        Context.Host.FWaitCreatorsDisposing.Set();
                    }
                }
            }
        }


    }
}