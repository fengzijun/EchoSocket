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
using System.Security.Cryptography;
using System.IO;
using System.Collections.Specialized;
using System.Collections.Generic;
using System.Reflection;

namespace EchoSocketCore.SocketsEx
{

    /// <summary>
    /// Base socket connection
    /// </summary>
    public abstract class BaseSocketConnection : BaseDisposable, ISocketConnection
    {

        #region Fields

        //----- Connection!
        private object FUserData;
        private long FId;

        private object FSyncData;
        private DateTime FLastAction;
        private long FWriteBytes;
        private long FReadBytes;

        //----- Active!
        private object FSyncActive;
        private bool FActive;

        //----- Event Processing!
        private object FSyncEventProcessing;
        private EventProcessing FEventProcessing;

        //----- Connection Host and Creator!
        private BaseSocketConnectionHost FHost;
        private BaseSocketConnectionCreator FCreator;

        //----- Socket and Stream!
        private Socket FSocket;
        private Stream FStream;

        //----- Write items!
        SocketAsyncEventArgs FWriteOV;
        private Queue<MessageBuffer> FWriteQueue;
        private bool FWriteQueueHasItems;

        //----- Read items!
        SocketAsyncEventArgs FReadOV;
        private object FSyncReadPending;
        private bool FReadPending;

        private ICryptoTransform FDecryptor;
        private ICryptoTransform FEncryptor;

        #endregion

        #region Constructor

        internal BaseSocketConnection(BaseSocketConnectionHost host, BaseSocketConnectionCreator creator, Socket socket)
        {

            //----- Connection Id!
            FId = host.GetConnectionId();
            
            FSyncData = new object();
            FReadBytes = 0;
            FWriteBytes = 0;

            FHost = host;
            FCreator = creator;
            FSocket = socket;

            FSyncActive = new Object();
            FActive = false;

            FWriteOV = new SocketAsyncEventArgs();
            FReadOV = new SocketAsyncEventArgs();

            FWriteQueue = new Queue<MessageBuffer>();
            FWriteQueueHasItems = false;

            FSyncReadPending = new object();
            FReadPending = false;

            FSyncEventProcessing = new object();
            FEventProcessing = EventProcessing.epNone;

            FLastAction = DateTime.Now;

            FUserData = null;
            FEncryptor = null;
            FDecryptor = null;

        }

        #endregion

        #region Destructor

        protected override void Free(bool canAccessFinalizable)
        {

            if (FWriteQueue != null)
            {
                FWriteQueue.Clear();
                FWriteQueue = null;
            }

            if (FStream != null)
            {
                FStream.Close();
                FStream = null;
            }

            if (FDecryptor != null)
            {
                FDecryptor.Dispose();
                FDecryptor = null;
            }

            if (FEncryptor != null)
            {
                FEncryptor.Dispose();
                FEncryptor = null;
            }

            if (FReadOV != null)
            {
                
                Type t = typeof(SocketAsyncEventArgs);

                FieldInfo f = t.GetField("m_Completed", BindingFlags.Instance | BindingFlags.NonPublic);
                f.SetValue(FReadOV, null);

                FReadOV.SetBuffer(null, 0, 0);
                FReadOV.Dispose();
                FReadOV = null;

            }

            if (FWriteOV != null)
            {

                Type t = typeof(SocketAsyncEventArgs);

                FieldInfo f = t.GetField("m_Completed", BindingFlags.Instance | BindingFlags.NonPublic);
                f.SetValue(FWriteOV, null);

                FWriteOV.SetBuffer(null, 0, 0);
                FWriteOV.Dispose();
                FWriteOV = null;

            }

            if (FSocket != null)
            {
                FSocket.Close();
                FSocket = null;
            }

            FHost = null;
            FCreator = null;
            FSyncReadPending = null;
            FSyncData = null;
            FSyncEventProcessing = null;

            base.Free(canAccessFinalizable);

        }

        #endregion

        #region Properties

        internal Queue<MessageBuffer> WriteQueue
        {
            get { return FWriteQueue; }
        }

        internal bool WriteQueueHasItems
        {
            get { return FWriteQueueHasItems; }
            set { FWriteQueueHasItems = value; }
        }

        internal bool ReadPending
        {
            get { return FReadPending; }
            set { FReadPending = value; }
        }

        internal object SyncReadPending
        {
            get { return FSyncReadPending; }
        }

        internal SocketAsyncEventArgs WriteOV
        {
            get { return FWriteOV; }
        }

        internal SocketAsyncEventArgs ReadOV
        {
            get { return FReadOV; }
        }

        internal object SyncActive
        {
            get { return FSyncActive; }
        }

        internal EventProcessing EventProcessing
        {

            get
            {
                lock (FSyncEventProcessing)
                {
                    return FEventProcessing;
                }
            }

            set
            {
                lock (FSyncEventProcessing)
                {
                    FEventProcessing = value;
                }
            }

        }

        internal bool Active
        {

            get 
            {
                if (Disposed)
                {
                    return false;
                }

                lock (FSyncActive)
                {
                    return FActive;
                }
            }

            set 
            {
                lock (FSyncActive)
                {
                    FActive = value;    
                }
            }

        }

        internal ICryptoTransform Encryptor
        {
            get { return FEncryptor; }
            set { FEncryptor = value; }
        }

        internal ICryptoTransform Decryptor
        {
            get { return FDecryptor; }
            set { FDecryptor = value; }
        }

        internal Stream Stream
        {
            get { return FStream; }
            set { FStream = value; }
        }

        internal Socket Socket
        {
            get { return FSocket; }
            set { FSocket = value; }
        }

        internal byte[] Delimiter
        {
            
            get
            {

                switch (EventProcessing)
                {

                    case EventProcessing.epUser:
                        
                        return FHost.Delimiter;

                    case EventProcessing.epEncrypt:
                        
                        return FHost.DelimiterEncrypt;

                    case EventProcessing.epProxy:

                        return null;

                    default:

                        return null;

                }

            }

        }

        internal DelimiterType DelimiterType
        {
            get
            {

                switch (EventProcessing)
                {

                    case EventProcessing.epUser:

                        return FHost.DelimiterType;

                    case EventProcessing.epEncrypt:

                        return DelimiterType.dtMessageTailExcludeOnReceive;

                    case EventProcessing.epProxy:

                        return DelimiterType.dtNone;

                    default:

                        return DelimiterType.dtNone;

                }

            }

        }

        internal EncryptType EncryptType
        {
            get { return FCreator.EncryptType; }
        }

        internal CompressionType CompressionType
        {
            get { return FCreator.CompressionType; }
        }

        internal HostType HostType
        {
            get { return FHost.HostType; }
        }

        internal BaseSocketConnectionCreator BaseCreator
        {
            get { return FCreator; }
        }

        internal BaseSocketConnectionHost BaseHost
        {
            get { return FHost; }
        }

        #endregion

        #region Methods

        internal void SetConnectionData(int readBytes, int writeBytes)
        {

            if (!Disposed)
            {

                lock (FSyncData)
                {
                 
                    if (readBytes > 0)
                    {
                        FReadBytes += readBytes;
                    }

                    if (writeBytes > 0)
                    {
                        FWriteBytes += writeBytes;
                    }

                    FLastAction = DateTime.Now;

                }

            }

        }

        #endregion

        #region ISocketConnection Members

        #region Properties

        public object UserData
        {
            get { return FUserData; }
            set { FUserData = value; }
        }

        public IPEndPoint LocalEndPoint
        {
            get { return (IPEndPoint)FSocket.LocalEndPoint; }
        }

        public IPEndPoint RemoteEndPoint
        {
            get { return (IPEndPoint)FSocket.RemoteEndPoint; }
        }

        public IntPtr SocketHandle
        {
            get { return FSocket.Handle; }
        }

        public long ConnectionId
        {
          get { return FId; }
        }

        public IBaseSocketConnectionCreator Creator
        {
          get { return FCreator; }
        }

        public IBaseSocketConnectionHost Host
        {
            get { return FHost; }
        }

        public DateTime LastAction
        {
            get { return FLastAction; }
        }

        public long ReadBytes
        {
            get { return FReadBytes; }
        }

        public long WriteBytes
        {
            get { return FWriteBytes; }
        }

        #endregion

        #region Socket Options

        public void SetTTL(short value)
        {
          FSocket.Ttl = value;
        }

        public void SetLinger(LingerOption lo)
        {
          FSocket.LingerState = lo;
        }

        public void SetNagle(bool value)
        {
          FSocket.NoDelay = value;
        }

        #endregion

        #region Abstract Methods

        public abstract IClientSocketConnection AsClientConnection();
        public abstract IServerSocketConnection AsServerConnection();

        #endregion

        #region BeginSend

        public void BeginSend(byte[] buffer)
        {
            if (!Disposed)
            {
                FHost.BeginSend(this, buffer, false);
            }
        }

        #endregion

        #region BeginReceive

        public void BeginReceive()
        {
            if (!Disposed)
            {
                FHost.BeginReceive(this);
            }
        }

        #endregion

        #region BeginDisconnect

        public void BeginDisconnect()
        {
            if (!Disposed)
            {
                FHost.BeginDisconnect(this);
            }
        }

        #endregion

        #region GetConnections

        public ISocketConnection[] GetConnections()
        {

            if (!Disposed)
            {
                return FHost.GetConnections();
            }
            else
            {
                return null;
            }

        }

        #endregion

        #region GetConnectionById

        public ISocketConnection GetConnectionById(long id)
        {

            if (!Disposed)
            {
                return FHost.GetSocketConnectionById(id);
            }
            else
            {
                return null;
            }

        }

        #endregion

        #endregion

    }

}
