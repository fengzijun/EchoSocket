using System.Net;

namespace EchoSocketCore.SocketsEx
{
    public class ProxyInfo
    {
        #region Fields

        private ProxyType FProxyType;
        private IPEndPoint FProxyEndPoint;
        private NetworkCredential FProxyCredential;
        private SOCKS5Phase FSOCKS5Phase;
        private SOCKS5AuthMode FSOCKS5Authentication;
        private bool FCompleted;

        #endregion Fields

        #region Constructor

        public ProxyInfo(ProxyType proxyType, IPEndPoint proxyEndPoint, NetworkCredential proxyCredential)
        {
            FProxyType = proxyType;
            FProxyEndPoint = proxyEndPoint;
            FProxyCredential = proxyCredential;
            FSOCKS5Phase = SOCKS5Phase.spIdle;
        }

        #endregion Constructor

        #region Properties

        public NetworkCredential ProxyCredential
        {
            get { return FProxyCredential; }
        }

        public IPEndPoint ProxyEndPoint
        {
            get { return FProxyEndPoint; }
        }

        public ProxyType ProxyType
        {
            get { return FProxyType; }
        }

        internal SOCKS5Phase SOCKS5Phase
        {
            get { return FSOCKS5Phase; }
            set { FSOCKS5Phase = value; }
        }

        internal SOCKS5AuthMode SOCKS5Authentication
        {
            get { return FSOCKS5Authentication; }
            set { FSOCKS5Authentication = value; }
        }

        internal bool Completed
        {
            get { return FCompleted; }
            set { FCompleted = value; }
        }

        #endregion Properties
    }
}