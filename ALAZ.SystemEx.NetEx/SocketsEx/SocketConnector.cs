/* ====================================================================
 * Copyright (c) 2009 Andre Luis Azevedo (az.andrel@yahoo.com.br)
 * All rights reserved.
 *                       
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the following conditions
 * are met:
 *
 * 1. Redistributions of source code must retain the above copyright
 *    notice, this list of conditions and the following disclaimer.
 *    In addition, the source code must keep original namespace names.
 *
 * 2. Redistributions in binary form must reproduce the above copyright
 *    notice, this list of conditions and the following disclaimer in
 *    the documentation and/or other materials provided with the
 *    distribution. In addition, the binary form must keep the original 
 *    namespace names and original file name.
 * 
 * 3. The name "ALAZ" or "ALAZ Library" must not be used to endorse or promote 
 *    products derived from this software without prior written permission.
 *
 * 4. Products derived from this software may not be called "ALAZ" or
 *    "ALAZ Library" nor may "ALAZ" or "ALAZ Library" appear in their 
 *    names without prior written permission of the author.
 * 
 * THIS SOFTWARE IS PROVIDED BY THE AUTHOR ``AS IS'' AND ANY
 * EXPRESSED OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE
 * IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR
 * PURPOSE ARE DISCLAIMED.  IN NO EVENT SHALL THE AUTHOR OR
 * ITS CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL,
 * SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT
 * NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
 * LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION)
 * HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT,
 * STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE)
 * ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED
 * OF THE POSSIBILITY OF SUCH DAMAGE. 
 */

using System;
using System.Threading;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace ALAZ.SystemEx.NetEx.SocketsEx
{

    /// <summary>
    /// Client socket creator.
    /// </summary>
    public class SocketConnector : BaseSocketConnectionCreator
    {

        #region Fields

        private Socket FSocket;
        private IPEndPoint FRemoteEndPoint;

        private Timer FReconnectTimer;
        private int FReconnectAttempts;
        private int FReconnectAttemptInterval;
        private int FReconnectAttempted;

        private ProxyInfo FProxyInfo;

        #endregion

        #region Constructor

        /// <summary>
        /// Base SocketConnector creator.
        /// </summary>
        /// <param name="host">
        /// Host.
        /// </param>
        /// <param name="remoteEndPoint">
        /// The remote endpoint to connect.
        /// </param>
        /// <param name="encryptType">
        /// Encrypt type.
        /// </param>
        /// <param name="compressionType">
        /// Compression type.
        /// </param>
        /// <param name="cryptoService">
        /// CryptoService. if null, will not be used.
        /// </param>
        /// <param name="localEndPoint">
        /// Local endpoint. if null, will be any address/port.
        /// </param>
        public SocketConnector(BaseSocketConnectionHost host, string name, IPEndPoint remoteEndPoint, ProxyInfo proxyData, EncryptType encryptType, CompressionType compressionType, ICryptoService cryptoService, int reconnectAttempts, int reconnectAttemptInterval, IPEndPoint localEndPoint)
            : base(host, name, localEndPoint, encryptType, compressionType, cryptoService)
        {

            FReconnectTimer = new Timer(new TimerCallback(ReconnectConnectionTimerCallBack));
            FRemoteEndPoint = remoteEndPoint;

            FReconnectAttempts = reconnectAttempts;
            FReconnectAttemptInterval = reconnectAttemptInterval;

            FReconnectAttempted = 0;

            FProxyInfo = proxyData;

        }

        #endregion

        #region Destructor

        protected override void Free(bool canAccessFinalizable)
        {

            if (FReconnectTimer != null)
            {
                FReconnectTimer.Dispose();
                FReconnectTimer = null;
            }

            if (FSocket != null)
            {
                FSocket.Close();
                FSocket = null;
            }
            
            FRemoteEndPoint = null;
            FProxyInfo = null;

            base.Free(canAccessFinalizable);

        }

        #endregion

        #region Methods

        #region Start

        public override void Start()
        {

            if (!Disposed)
            {
                BeginConnect();
            }

        }

        #endregion

        #region Stop

        public override void Stop()
        {
            Dispose();
        }

        #endregion

        #region BeginConnect

        /// <summary>
        /// Begin the connection with host.
        /// </summary>
        internal void BeginConnect()
        {

            if (!Disposed)
            {

                //----- Create Socket!
                FSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                FSocket.Bind(InternalLocalEndPoint);
                FSocket.ReceiveBufferSize = Host.SocketBufferSize;
                FSocket.SendBufferSize = Host.SocketBufferSize;

                FReconnectTimer.Change(Timeout.Infinite, Timeout.Infinite);

                SocketAsyncEventArgs e = new SocketAsyncEventArgs();
                e.Completed += new EventHandler<SocketAsyncEventArgs>(BeginConnectCallbackAsync);
                e.UserToken = this;

                if (FProxyInfo == null)
                {
                    e.RemoteEndPoint = FRemoteEndPoint;
                }
                else
                {

                    FProxyInfo.Completed = false;
                    FProxyInfo.SOCKS5Phase = SOCKS5Phase.spIdle;

                    e.RemoteEndPoint = FProxyInfo.ProxyEndPoint;

                }

                if (!FSocket.ConnectAsync(e))
                {
                    BeginConnectCallbackAsync(this, e);
                }

            }

        }

        #endregion

        #region BeginAcceptCallbackAsync

        /// <summary>
        /// Connect callback!
        /// </summary>
        /// <param name="ar"></param>
        internal void BeginConnectCallbackAsync(object sender, SocketAsyncEventArgs e)
        {

            if (!Disposed)
            {

                BaseSocketConnection connection = null;
                SocketConnector connector = null;
                Exception exception = null;

                if (e.SocketError == SocketError.Success)
                {

                    try
                    {

                        connector = (SocketConnector)e.UserToken;

                        connection = new ClientSocketConnection(Host, connector, connector.Socket);

                        //----- Adjust buffer size!
                        connector.Socket.ReceiveBufferSize = Host.SocketBufferSize;
                        connector.Socket.SendBufferSize = Host.SocketBufferSize;

                        //----- Initialize!
                        Host.AddSocketConnection(connection);
                        connection.Active = true;

                        Host.InitializeConnection(connection);

                    }
                    catch (Exception ex)
                    {
                        
                        exception = ex;

                        if (connection != null)
                        {

                            Host.DisposeConnection(connection);
                            Host.RemoveSocketConnection(connection);

                            connection = null;

                        }

                    }

                }
                else
                {
                    exception = new SocketException((int)e.SocketError);
                }

                if (exception != null)
                {
                    FReconnectAttempted++;
                    ReconnectConnection(false, exception);
                }

            }

            e.UserToken = null;
            e.Dispose();
            e = null;

        }

        #endregion

        #region ReconnectConnection

        internal void ReconnectConnection(bool resetAttempts, Exception ex)
        {

          if (!Disposed)
          {

              if (resetAttempts)
              {
                  
                  //----- Reset counter and start new connect!
                  FReconnectAttempted = 0;
                  FReconnectTimer.Change(FReconnectAttemptInterval, FReconnectAttemptInterval);

              }
              else
              {

                  //----- Check attempt count!
                  if (FReconnectAttempts > 0)
                  {

                      if (FReconnectAttempted < FReconnectAttempts)
                      {
                          Host.FireOnException(null, new ReconnectAttemptException("Reconnect attempt", this, ex, FReconnectAttempted, false));
                          FReconnectTimer.Change(FReconnectAttemptInterval, FReconnectAttemptInterval);
                      }
                      else
                      {
                          Host.FireOnException(null, new ReconnectAttemptException("Reconnect attempt", this, ex, FReconnectAttempted, true));
                      }

                  }
                  else
                  {
                      Host.FireOnException(null, new ReconnectAttemptException("Reconnect attempt", this, ex, FReconnectAttempted, true));
                  }
              
              }

          }

        }

        #endregion

        #region ReconnectConnectionTimerCallBack

        private void ReconnectConnectionTimerCallBack(Object stateInfo)
        {

            if (!Disposed)
            {
                FReconnectTimer.Change(Timeout.Infinite, Timeout.Infinite);
                BeginConnect();
            }

        }

        #endregion

        #endregion

        #region Properties

        public int ReconnectAttempts
        {
            get { return FReconnectAttempts; }
            set { FReconnectAttempts = value; }
        }

        public int ReconnectAttemptInterval
        {
            get { return FReconnectAttemptInterval; }
            set { FReconnectAttemptInterval = value; }
        }

        public IPEndPoint LocalEndPoint
        {
            get { return InternalLocalEndPoint; }
            set { InternalLocalEndPoint = value; }
        }

        public IPEndPoint RemoteEndPoint
        {
            get { return FRemoteEndPoint; }
        }
        
        public  ProxyInfo ProxyInfo
        {
            get { return FProxyInfo; }
            set { FProxyInfo = value; } 
        }

        internal Socket Socket
        {
            get { return FSocket; }
        }

        #endregion

    }

}
