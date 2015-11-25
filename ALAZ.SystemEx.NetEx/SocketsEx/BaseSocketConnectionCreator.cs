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

        //----- Local endpoint of creator!
        private IPEndPoint FLocalEndPoint;

        //----- Host!
        private BaseSocketConnectionHost FHost;

        private string FName;

        private EncryptType FEncryptType;
        private CompressionType FCompressionType;

        private ICryptoService FCryptoService;

        #endregion Fields

        #region Constructor

        public BaseSocketConnectionCreator(BaseSocketConnectionHost host, string name, IPEndPoint localEndPoint, EncryptType encryptType, CompressionType compressionType, ICryptoService cryptoService)
        {


            FHost = host;
            FName = name;
            FLocalEndPoint = localEndPoint;
            FCompressionType = compressionType;
            FEncryptType = encryptType;

            FCryptoService = cryptoService;
        }

        #endregion Constructor

        #region Destructor

        protected override void Free(bool canAccessFinalizable)
        {
            FLocalEndPoint = null;
            FCryptoService = null;
            FHost = null;

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

        internal BaseSocketConnectionHost Host
        {
            get { return FHost; }
        }

        public string Name
        {
            get { return FName; }
        }

        public ICryptoService CryptoService
        {
            get { return FCryptoService; }
            set { FCryptoService = value; }
        }

        public EncryptType EncryptType
        {
            get { return FEncryptType; }
            set { FEncryptType = value; }
        }

        internal IPEndPoint InternalLocalEndPoint
        {
            get { return FLocalEndPoint; }
            set { FLocalEndPoint = value; }
        }

        public CompressionType CompressionType
        {
            get { return FCompressionType; }
            set { FCompressionType = value; }
        }

        public SocketCreatorContext Context { get; set; }

        #endregion Properties
    }
}