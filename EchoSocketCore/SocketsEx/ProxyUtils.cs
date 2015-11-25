using System;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;

namespace EchoSocketCore.SocketsEx
{
    public class ProxyUtils
    {
        #region GetProxyRequestData

        internal static byte[] GetProxyRequestData(ProxyInfo proxyInfo, IPEndPoint remoteEndPoint)
        {
            byte[] result = null;

            switch (proxyInfo.ProxyType)
            {
                case ProxyType.ptHTTP:

                    if (proxyInfo.ProxyCredential == null)
                    {
                        result = Encoding.GetEncoding(1252).GetBytes(String.Format("CONNECT {0}:{1} HTTP/1.1\r\nnHost: {0}:{1}\r\n\r\n", remoteEndPoint.Address, remoteEndPoint.Port));
                    }
                    else
                    {
                        string base64Encoding = Convert.ToBase64String(Encoding.GetEncoding(1252).GetBytes(proxyInfo.ProxyCredential.UserName + ":" + proxyInfo.ProxyCredential.Password));
                        result = Encoding.GetEncoding(1252).GetBytes(String.Format("CONNECT {0}:{1} HTTP/1.1\r\nHost: {0}:{1}\r\nAuthorization: Basic {2}\r\nProxy-Authorization: Basic {2}\r\n\r\n", remoteEndPoint.Address, remoteEndPoint.Port, base64Encoding));
                    }

                    break;

                case ProxyType.ptSOCKS4:
                case ProxyType.ptSOCKS4a:

                    if (proxyInfo.ProxyType == ProxyType.ptSOCKS4)
                    {
                        if (proxyInfo.ProxyCredential == null)
                        {
                            result = new byte[8 + 1];
                        }
                        else
                        {
                            result = new byte[8 + proxyInfo.ProxyCredential.UserName.Length + 1];
                        }
                    }
                    else
                    {
                        if (proxyInfo.ProxyCredential == null)
                        {
                            result = new byte[8 + 1 + 1];
                        }
                        else
                        {
                            result = new byte[8 + proxyInfo.ProxyCredential.UserName.Length + 1 + proxyInfo.ProxyCredential.Domain.Length + 1];
                        }
                    }

                    result[0] = 4;
                    result[1] = 1;

                    result[2] = Convert.ToByte((remoteEndPoint.Port & 0xFF00) >> 8);
                    result[3] = Convert.ToByte(remoteEndPoint.Port & 0xFF);

                    if (proxyInfo.ProxyType == ProxyType.ptSOCKS4)
                    {
                        Buffer.BlockCopy(remoteEndPoint.Address.GetAddressBytes(), 0, result, 4, 4);
                    }
                    else
                    {
                        result[4] = 0;
                        result[5] = 0;
                        result[6] = 0;
                        result[7] = 1;
                    }

                    if ((proxyInfo.ProxyCredential != null) && (proxyInfo.ProxyCredential.UserName != null))
                    {
                        Buffer.BlockCopy(Encoding.GetEncoding(1252).GetBytes(proxyInfo.ProxyCredential.UserName), 0, result, 8, proxyInfo.ProxyCredential.UserName.Length);
                    }

                    if ((proxyInfo.ProxyType == ProxyType.ptSOCKS4a) && (proxyInfo.ProxyCredential != null))
                    {
                        Buffer.BlockCopy(Encoding.GetEncoding(1252).GetBytes(proxyInfo.ProxyCredential.Domain), 0, result, 8 + proxyInfo.ProxyCredential.UserName.Length + 1, proxyInfo.ProxyCredential.Domain.Length);
                    }

                    break;

                case ProxyType.ptSOCKS5:

                    switch (proxyInfo.SOCKS5Phase)
                    {
                        case SOCKS5Phase.spIdle:

                            if (proxyInfo.ProxyCredential == null)
                            {
                                result = new byte[3];

                                result[0] = 5;
                                result[1] = 1;
                                result[2] = 0;
                            }
                            else
                            {
                                result = new byte[4];

                                result[0] = 5;
                                result[1] = 2;
                                result[2] = 0;
                                result[3] = 2;
                            }

                            proxyInfo.SOCKS5Phase = SOCKS5Phase.spGreeting;

                            break;

                        case SOCKS5Phase.spConnecting:

                            result = new byte[10];

                            result[0] = 5;
                            result[1] = 1;
                            result[2] = 0;
                            result[3] = 1;

                            Buffer.BlockCopy(remoteEndPoint.Address.GetAddressBytes(), 0, result, 4, 4);

                            result[8] = Convert.ToByte((remoteEndPoint.Port & 0xFF00) >> 8);
                            result[9] = Convert.ToByte(remoteEndPoint.Port & 0xFF);

                            break;

                        case SOCKS5Phase.spAuthenticating:

                            result = new byte[3 + proxyInfo.ProxyCredential.UserName.Length + proxyInfo.ProxyCredential.Password.Length];

                            result[0] = 5;
                            result[1] = Convert.ToByte(proxyInfo.ProxyCredential.UserName.Length);

                            Buffer.BlockCopy(Encoding.GetEncoding(1252).GetBytes(proxyInfo.ProxyCredential.UserName), 0, result, 2, proxyInfo.ProxyCredential.UserName.Length);

                            int passOffSet = 2 + proxyInfo.ProxyCredential.UserName.Length;
                            result[passOffSet] = Convert.ToByte(proxyInfo.ProxyCredential.Password.Length);

                            Buffer.BlockCopy(Encoding.GetEncoding(1252).GetBytes(proxyInfo.ProxyCredential.Password), 0, result, passOffSet + 1, proxyInfo.ProxyCredential.Password.Length);

                            break;
                    }

                    break;
            }

            return result;
        }

        #endregion GetProxyRequestData

        #region GetProxyResponseStatus

        internal static void GetProxyResponseStatus(ProxyInfo proxyInfo, byte[] response)
        {
            switch (proxyInfo.ProxyType)
            {
                case ProxyType.ptHTTP:

                    Match m = Regex.Match(Encoding.GetEncoding(1252).GetString(response), @"[HTTP/]\d[.]\d\s(?<code>\d+)\s(?<reason>.+)");

                    if (m.Success)
                    {
                        int code = Convert.ToInt32(m.Groups["code"].Value);

                        if (code >= 200 && code <= 299)
                        {
                            proxyInfo.Completed = true;
                        }
                        else
                        {
                            throw new ProxyAuthenticationException(code, m.Groups["reason"].Value);
                        }
                    }
                    else
                    {
                        throw new ProxyAuthenticationException(0, "Invalid proxy message response.");
                    }

                    break;

                case ProxyType.ptSOCKS4:
                case ProxyType.ptSOCKS4a:

                    if (response[1] == 0x5A)
                    {
                        proxyInfo.Completed = true;
                    }
                    else
                    {
                        switch (response[1])
                        {
                            case 0x5B:

                                throw new ProxyAuthenticationException(response[1], "Request rejected or failed.");

                            case 0x5C:

                                throw new ProxyAuthenticationException(response[1], "Client is not running identd.");

                            case 0x5D:

                                throw new ProxyAuthenticationException(response[1], "Client's identd could not confirm the user ID string in the request.");
                        }
                    }

                    break;

                case ProxyType.ptSOCKS5:

                    switch (proxyInfo.SOCKS5Phase)
                    {
                        case SOCKS5Phase.spGreeting:

                            if (response[1] != 0xFF)
                            {
                                proxyInfo.SOCKS5Authentication = (SOCKS5AuthMode)Enum.ToObject(typeof(SOCKS5AuthMode), response[1]);

                                switch (proxyInfo.SOCKS5Authentication)
                                {
                                    case SOCKS5AuthMode.saNoAuth:

                                        proxyInfo.SOCKS5Phase = SOCKS5Phase.spConnecting;
                                        break;

                                    case SOCKS5AuthMode.ssUserPass:

                                        proxyInfo.SOCKS5Phase = SOCKS5Phase.spAuthenticating;
                                        break;
                                }
                            }
                            else
                            {
                                throw new ProxyAuthenticationException(0xFF, "Authentication method not supported.");
                            }

                            break;

                        case SOCKS5Phase.spConnecting:
                        case SOCKS5Phase.spAuthenticating:

                            if (response[1] == 0x00)
                            {
                                switch (proxyInfo.SOCKS5Phase)
                                {
                                    case SOCKS5Phase.spConnecting:

                                        proxyInfo.Completed = true;
                                        break;

                                    case SOCKS5Phase.spAuthenticating:

                                        proxyInfo.SOCKS5Phase = SOCKS5Phase.spConnecting;
                                        break;
                                }
                            }
                            else
                            {
                                switch (response[1])
                                {
                                    case 0x01:

                                        throw new ProxyAuthenticationException(response[1], "General Failure.");

                                    case 0x02:

                                        throw new ProxyAuthenticationException(response[1], "Connection not allowed by ruleset.");

                                    case 0x03:

                                        throw new ProxyAuthenticationException(response[1], "Network unreachable.");

                                    case 0x04:

                                        throw new ProxyAuthenticationException(response[1], "Host unreachable.");

                                    case 0x05:

                                        throw new ProxyAuthenticationException(response[1], "Connection refused by destination host.");

                                    case 0x06:

                                        throw new ProxyAuthenticationException(response[1], "TTL expired.");

                                    case 0x07:

                                        throw new ProxyAuthenticationException(response[1], "Command not supported / protocol error.");

                                    case 0x08:

                                        throw new ProxyAuthenticationException(response[1], "Address type not supported.");
                                }
                            }

                            break;
                    }

                    break;
            }
        }

        #endregion GetProxyResponseStatus
    }
}