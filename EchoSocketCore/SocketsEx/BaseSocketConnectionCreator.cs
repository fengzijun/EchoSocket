using System.Net;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;

namespace EchoSocketCore.SocketsEx
{
    /// <summary>
    /// Connection creator using in BaseSocketConnectionHost.
    /// </summary>
    public abstract class BaseSocketConnectionCreator : BaseDisposable, IBaseSocketConnectionCreator
    {
        #region Fields


        private ICryptoService FCryptoService;

        #endregion Fields

        #region Constructor

        public BaseSocketConnectionCreator(BaseSocketConnectionHost host, string name, IPEndPoint localEndPoint, EncryptType encryptType, CompressionType compressionType, ICryptoService cryptoService)
        {
            if (Context == null)
                Context = new SocketCreatorContext();

            Context.Host = host;
            Context.Name = name;
            Context.localEndPoint = localEndPoint;
            Context.CompressionType = compressionType;
            Context.EncryptType = encryptType;

            FCryptoService = cryptoService;
        }

        #endregion Constructor

        #region Destructor

        protected override void Free(bool canAccessFinalizable)
        {
          
            FCryptoService = null;
            Context = null;

            base.Free(canAccessFinalizable);
        }

        #endregion Destructor

        #region Methods

        #region ValidateServerCertificateCallback

        internal bool ValidateServerCertificateCallback(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
        {
            bool acceptCertificate = false;
            FCryptoService.OnSSLClientValidateServerCertificate(certificate, chain, sslPolicyErrors, out acceptCertificate);

            return acceptCertificate;
        }

        #endregion ValidateServerCertificateCallback

        #region Abstract Methods

        public abstract void Start();

        public abstract void Stop();

        #endregion Abstract Methods

        #endregion Methods

        #region Properties


        public ICryptoService CryptoService
        {
            get { return FCryptoService; }
            set { FCryptoService = value; }
        }


        public SocketCreatorContext Context { get; set; }

        #endregion Properties
    }
}