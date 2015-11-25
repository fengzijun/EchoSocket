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
using System.Net;
using System.Net.Sockets;
using System.Net.Security;
using System.Threading;
using System.Security.Cryptography.X509Certificates;

using EchoSocketCore.ThreadingEx;

namespace EchoSocketCore.SocketsEx
{

    /// <summary>
    /// Server socket connector.
    /// </summary>
    public class SocketListener : BaseSocketConnectionCreator
    {

        #region Fields

        private Socket FSocket;
        private byte FBackLog;
        private byte FAcceptThreads;

        #endregion

        #region Constructor

        /// <summary>
        /// Base SocketListener creator.
        /// </summary>
        /// <param name="host">
        /// Host.
        /// </param>
        /// <param name="localEndPoint">
        /// Local endpoint to be used.
        /// </param>
        /// <param name="encryptType">
        /// Encryption to be used.
        /// </param>
        /// <param name="compressionType">
        /// Compression to be used.
        /// </param>
        /// <param name="cryptoService">
        /// CryptoService. if null, will not be used.
        /// </param>
        /// <param name="backLog">
        /// Socket backlog queue number.
        /// </param>
        /// <param name="acceptThreads">
        /// Number of accept events to be used.
        /// </param>
        public SocketListener(BaseSocketConnectionHost host, string name, IPEndPoint localEndPoint, EncryptType encryptType, CompressionType compressionType, ICryptoService cryptoService, byte backLog, byte acceptThreads)
            : base(host, name, localEndPoint, encryptType, compressionType, cryptoService)
        {

            FBackLog = backLog;
            FAcceptThreads = acceptThreads;

        }

        #endregion

        #region Destructor

        protected override void Free(bool canAccessFinalizable)
        {

            if (FSocket != null)
            {
                FSocket.Close();
                FSocket = null;
            }

            base.Free(canAccessFinalizable);
        }

        #endregion

        #region Methods

        #region Start

        public override void Start()
        {

            if (!Disposed)
            {

                FSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                FSocket.Bind(LocalEndPoint);
                FSocket.Listen(FBackLog * FAcceptThreads);

                //----- Begin accept new connections!
                int loopCount = 0;
                SocketAsyncEventArgs e = null;

                for (int i = 1; i <= FAcceptThreads; i++)
                {
                    e = new SocketAsyncEventArgs();
                    e.UserToken = this;
                    e.Completed += new EventHandler<SocketAsyncEventArgs>(BeginAcceptCallbackAsync);

                    if (!FSocket.AcceptAsync(e))
                    {
                        BeginAcceptCallbackAsync(this, e);
                    };

                    ThreadEx.LoopSleep(ref loopCount);

                }

            }

        }

        #endregion

        #region BeginAcceptCallbackAsync

        internal void BeginAcceptCallbackAsync(object sender, SocketAsyncEventArgs e)
        {
            ThreadPool.QueueUserWorkItem(new WaitCallback(BeginAcceptCallback), e);
        }

        /// <summary>
        /// Accept callback!
        /// </summary>
        internal void BeginAcceptCallback(object state)
        {

            SocketAsyncEventArgs e = (SocketAsyncEventArgs)state;

            if (!Disposed)
            {
                
                SocketListener listener = null;
                Socket acceptedSocket = null;
                BaseSocketConnection connection = null;

                listener = (SocketListener)e.UserToken;

                if (e.SocketError == SocketError.Success)
                {

                    try
                    {

                        //----- Get accepted socket!
                        acceptedSocket = e.AcceptSocket;

                        //----- Adjust buffer size!
                        acceptedSocket.ReceiveBufferSize = Host.SocketBufferSize;
                        acceptedSocket.SendBufferSize = Host.SocketBufferSize;

                        connection = new ServerSocketConnection(Host, listener, acceptedSocket);

                        //----- Initialize!
                        Host.AddSocketConnection(connection);
                        connection.Active = true;

                        Host.InitializeConnection(connection);

                    }
                    catch
                    {

                        if (connection != null)
                        {

                            if (Host != null)
                            {
                                Host.DisposeConnection(connection);
                                Host.RemoveSocketConnection(connection);
                            }

                            connection = null;

                        }

                    }

                }

                //---- Continue to accept!
                SocketAsyncEventArgs e2 = new SocketAsyncEventArgs();
                e2.UserToken = listener;
                e2.Completed += new EventHandler<SocketAsyncEventArgs>(BeginAcceptCallbackAsync);

                if (!listener.Socket.AcceptAsync(e2))
                {
                    BeginAcceptCallbackAsync(this, e2);
                }

            }

            e.UserToken = null;
            e.Dispose();
            e = null;

        }

        #endregion

        #region Stop

        public override void Stop()
        {
            Dispose();
        }

        #endregion

        #endregion

        #region Properties

        public IPEndPoint LocalEndPoint
        {
            get { return InternalLocalEndPoint; }
        }
        
        public byte BackLog
        {
            get { return FBackLog; }
            set { FBackLog = value; }
        }

        public byte AcceptThreads
        {
            get { return FAcceptThreads; }
            set { FAcceptThreads = value; }
        }

        internal Socket Socket
        {
            get { return FSocket; }
        }

        #endregion

    }

}
