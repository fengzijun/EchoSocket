using System;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Web;

namespace EchoSocketCore.SocketsEx
{
    #region Delegates

    public delegate void OnDisconnectEvent();

    public delegate void OnSymmetricAuthenticateEvent(ISocketConnection connection, out RSACryptoServiceProvider serverKey);

    public delegate void OnSSLClientAuthenticateEvent(ISocketConnection connection, out string ServerName, ref X509Certificate2Collection certs, ref bool checkRevocation);

    public delegate void OnSSLServerAuthenticateEvent(ISocketConnection connection, out X509Certificate2 certificate, out bool clientAuthenticate, ref bool checkRevocation);

    #endregion Delegates

    #region Structures

    #region AuthMessage

    [Serializable]
    public class AuthMessage
    {
        public byte[] SessionKey;
        public byte[] SessionIV;
        public byte[] ClientKey;
        public byte[] Data;
        public byte[] Sign;
    }

    #endregion AuthMessage

    #endregion Structures

    #region Enums

    #region CallbackThreadType

    public enum CallbackThreadType
    {
        ctWorkerThread,
        ctIOThread
    }

    #endregion CallbackThreadType

    #region HostType

    /// <summary>
    /// Defines the host type.
    /// </summary>
    public enum HostType
    {
        htServer,
        htClient
    }

    #endregion HostType

    #region EncryptType

    /// <summary>
    /// Defines the encrypt method used.
    /// </summary>
    public enum EncryptType
    {
        etNone,
        etRijndael,
        etSSL
    }

    #endregion EncryptType

    #region CompressionType

    /// <summary>
    /// Defines the compression method used.
    /// </summary>
    public enum CompressionType
    {
        ctNone,
        ctGZIP
    }

    #endregion CompressionType

    #region DelimiterType

    /// <summary>
    /// Defines message delimiter type.
    /// </summary>
    public enum DelimiterType
    {
        dtNone,
        dtMessageTailExcludeOnReceive,
        dtMessageTailIncludeOnReceive
    }

    #endregion DelimiterType

    #region EventProcessing

    public enum EventProcessing
    {
        epNone,
        epProxy,
        epEncrypt,
        epUser
    }

    #endregion EventProcessing

    #region ProxyType

    /// <summary>
    /// Defines the proxy host type.
    /// </summary>
    public enum ProxyType
    {
        ptSOCKS4,
        ptSOCKS4a,
        ptSOCKS5,
        ptHTTP
    }

    #endregion ProxyType

    #region SOCKS5AuthMode

    /// <summary>
    /// Defines the SOCK5 authentication mode.
    /// </summary>
    internal enum SOCKS5AuthMode
    {
        saNoAuth = 0,
        ssUserPass = 2
    }

    #endregion SOCKS5AuthMode

    #region SOCKS5Phase

    /// <summary>
    /// Defines the SOCKS5 authentication phase
    /// </summary>
    internal enum SOCKS5Phase
    {
        spIdle,
        spGreeting,
        spAuthenticating,
        spConnecting
    }

    #endregion SOCKS5Phase

    #endregion Enums


}