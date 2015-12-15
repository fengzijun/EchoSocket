using System.Net;

namespace EchoSocketCore.SocketsEx
{
    public class ProxyInfo
    {
        #region Fields

        private ProxyType fProxyType;
        private IPEndPoint fProxyEndPoint;
        private NetworkCredential fProxyCredential;
        private SOCKS5Phase fSOCKS5Phase;
        private SOCKS5AuthMode fSOCKS5Authentication;
        private bool fCompleted;

        #endregion Fields

        #region Constructor

        public ProxyInfo(ProxyType proxyType, IPEndPoint proxyEndPoint, NetworkCredential proxyCredential)
        {
            fProxyType = proxyType;
            fProxyEndPoint = proxyEndPoint;
            fProxyCredential = proxyCredential;
            fSOCKS5Phase = SOCKS5Phase.spIdle;
        }

        #endregion Constructor

        #region Properties

        public NetworkCredential ProxyCredential
        {
            get { return fProxyCredential; }
        }

        public IPEndPoint ProxyEndPoint
        {
            get { return fProxyEndPoint; }
        }

        public ProxyType ProxyType
        {
            get { return fProxyType; }
        }

        internal SOCKS5Phase SOCKS5Phase
        {
            get { return fSOCKS5Phase; }
            set { fSOCKS5Phase = value; }
        }

        internal SOCKS5AuthMode SOCKS5Authentication
        {
            get { return fSOCKS5Authentication; }
            set { fSOCKS5Authentication = value; }
        }

        internal bool Completed
        {
            get { return fCompleted; }
            set { fCompleted = value; }
        }

        #endregion Properties
    }
}