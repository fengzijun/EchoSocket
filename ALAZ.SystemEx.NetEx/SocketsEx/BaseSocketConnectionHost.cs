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
using System.Collections.Generic;
using System.Threading;

using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

using System.Net;
using System.Net.Security;
using System.Net.Sockets;

using System.IO;
using System.Text;

using System.ServiceModel.Channels;
using System.Runtime.Serialization.Formatters.Binary;

using EchoSocketCore.ThreadingEx;

namespace EchoSocketCore.SocketsEx
{

    /// <summary>
    /// The connection host.
    /// </summary>
    public abstract class BaseSocketConnectionHost : BaseDisposable, IBaseSocketConnectionHost
    {

        #region Fields

        private bool FActive;
        private object FSyncActive;
        
        private HostType FHostType;
        private long FConnectionId;

        private DelimiterType FDelimiterType;
        private CallbackThreadType FCallbackThreadType;

        private int FMessageBufferSize;
        private int FSocketBufferSize;

        //----- Enumerates the connections and creators!
        private ReaderWriterLockSlim FSocketConnectionsSync;
        private Dictionary<long, BaseSocketConnection> FSocketConnections;
        private BufferManager FBufferManager;

        private List<BaseSocketConnectionCreator> FSocketCreators;

        //----- The Socket Service.
        private ISocketService FSocketService;

        //----- Waits for objects removing!
        private ManualResetEvent FWaitCreatorsDisposing;
        private ManualResetEvent FWaitConnectionsDisposing;
        private ManualResetEvent FWaitThreadsDisposing;

        //----- Check idle timer!
        private Timer FIdleTimer;
        private int FIdleCheckInterval;
        private int FIdleTimeOutValue;

        //----- Socket delimiter and buffer size!
        private byte[] FDelimiter;
        private byte[] FDelimiterEncrypt;

        #endregion

        #region Constructor

        public BaseSocketConnectionHost(HostType hostType, CallbackThreadType callbackThreadtype, ISocketService socketService, DelimiterType delimiterType, byte[] delimiter, int socketBufferSize, int messageBufferSize, int idleCheckInterval, int idleTimeOutValue)
        {

            FHostType = hostType;
            FConnectionId = 1000;

            FSocketConnectionsSync = new ReaderWriterLockSlim();

            FSocketConnections = new Dictionary<long, BaseSocketConnection>();
            FSocketCreators = new List<BaseSocketConnectionCreator>();
            FBufferManager = BufferManager.CreateBufferManager(0, messageBufferSize);
            FSocketService = socketService;

            FWaitCreatorsDisposing = new ManualResetEvent(false);
            FWaitConnectionsDisposing = new ManualResetEvent(false);
            FWaitThreadsDisposing = new ManualResetEvent(false);

            FIdleCheckInterval = idleCheckInterval;
            FIdleTimeOutValue = idleTimeOutValue;

            FCallbackThreadType = callbackThreadtype;
            FDelimiterType = delimiterType;

            FDelimiter = delimiter;
            FDelimiterEncrypt = new byte[] { 0xFE, 0xDC, 0xBA, 0x98, 0xBA, 0xDC, 0xFE };

            FMessageBufferSize = messageBufferSize;
            FSocketBufferSize = socketBufferSize;

            FActive = false;
            FSyncActive = new Object();

        }

        #endregion

        #region Destructor

        protected override void Free(bool canAccessFinalizable)
        {

            if (FIdleTimer != null)
            {
                FIdleTimer.Change(Timeout.Infinite, Timeout.Infinite);
                FIdleTimer.Dispose();
                FIdleTimer = null;
            }

            if (FWaitCreatorsDisposing != null)
            {
                FWaitCreatorsDisposing.Set();
                FWaitCreatorsDisposing.Close();
                FWaitCreatorsDisposing = null;
            }

            if (FWaitConnectionsDisposing != null)
            {
                FWaitConnectionsDisposing.Set();
                FWaitConnectionsDisposing.Close();
                FWaitConnectionsDisposing = null;
            }

            if (FWaitThreadsDisposing != null)
            {
                FWaitThreadsDisposing.Set();
                FWaitThreadsDisposing.Close();
                FWaitThreadsDisposing = null;
            }

            if (FSocketConnections != null)
            {
                FSocketConnections.Clear();
                FSocketConnections = null;
            }

            if (FSocketCreators != null)
            {
                FSocketCreators.Clear();
                FSocketCreators = null;
            }

            if (FBufferManager != null)
            {
                FBufferManager.Clear();
                FBufferManager = null;
            }

            FSocketConnectionsSync = null;
            FSocketService = null;
            FDelimiter = null;
            FDelimiterEncrypt = null;

            base.Free(canAccessFinalizable);

        }

        #endregion

        #region Methods

        #region Start

        /// <summary>
        /// Starts the base host.
        /// </summary>
        public void Start()
        {

            if (!Disposed)
            {

                //ThreadPool.SetMinThreads(2, Environment.ProcessorCount * 2);
                //ThreadPool.SetMaxThreads(48, Environment.ProcessorCount * 2);

                int loopSleep = 0;

                foreach (BaseSocketConnectionCreator creator in FSocketCreators)
                {
                    creator.Start();
                    ThreadEx.LoopSleep(ref loopSleep);
                }

                if ((FIdleCheckInterval > 0) && (FIdleTimeOutValue > 0))
                {
                    FIdleTimer = new Timer(new TimerCallback(CheckSocketConnections));
                }

                if (FIdleTimer != null)
                {
                    FIdleTimer.Change(FIdleCheckInterval, FIdleCheckInterval);
                }

                Active = true;

            }

        }

        #endregion

        #region Stop

        /// <summary>
        /// Stop the base host.
        /// </summary>
        public virtual void Stop()
        {
            
            if (!Disposed)
            {
                Active = false;
            }

        }

        #endregion

        #region StopCreators

        /// <summary>
        /// Stop the host creators.
        /// </summary>
        protected void StopCreators()
        {

            if (!Disposed)
            {

                //----- Stop Creators!
                BaseSocketConnectionCreator[] creators = GetSocketCreators();

                if (creators != null)
                {

                    FWaitCreatorsDisposing.Reset();

                    int loopCount = 0;

                    foreach (BaseSocketConnectionCreator creator in creators)
                    {

                        try
                        {
                            creator.Stop();
                        }
                        finally
                        {

                            RemoveCreator(creator);
                            creator.Dispose();

                            ThreadEx.LoopSleep(ref loopCount);

                        }

                    }

                    if (creators.Length > 0)
                    {
                        FWaitCreatorsDisposing.WaitOne(Timeout.Infinite, false);
                    }

                }
            }
        }

        #endregion

        #region StopConnections

        protected void StopConnections()
        {

            if (!Disposed)
            {

                //----- Stop Connections!
                BaseSocketConnection[] connections = GetSocketConnections();

                if (connections != null)
                {

                    FWaitConnectionsDisposing.Reset();
                    
                    int loopSleep = 0;

                    foreach (BaseSocketConnection connection in connections)
                    {
                        connection.BeginDisconnect();
                        ThreadEx.LoopSleep(ref loopSleep);
                    }

                    if (connections.Length > 0)
                    {
                        FWaitConnectionsDisposing.WaitOne(Timeout.Infinite, false);
                    }

                }

            }

        }

        #endregion

        #region Fire Methods

        #region FireOnConnected

        internal void FireOnConnected(BaseSocketConnection connection)
        {

            if (!Disposed)
            {

                if (connection.Active)
                {

                    try
                    {

                        switch (connection.EventProcessing)
                        {
                            
                            case EventProcessing.epUser:

                                FSocketService.OnConnected(new ConnectionEventArgs(connection));
                                break;

                            case EventProcessing.epEncrypt:

                                OnConnected(connection);
                                break;

                            case EventProcessing.epProxy:

                                OnConnected(connection);
                                break;

                        }

                    }
                    finally
                    { 
                    //
                    }

                }

            }

        }

        #endregion

        #region FireOnSent

        private void FireOnSent(BaseSocketConnection connection, bool sentByServer)
        {

            if (!Disposed)
            {
                
                if (connection.Active)
                {

                    try
                    {

                        switch (connection.EventProcessing)
                        {

                            case EventProcessing.epUser:

                                FSocketService.OnSent(new MessageEventArgs(connection, null, sentByServer));
                                break;

                            case EventProcessing.epEncrypt:

                                OnSent(connection);
                                break;

                            case EventProcessing.epProxy:

                                OnSent(connection);
                                break;

                        }

                    }
                    finally
                    {
                        //
                    }

                }

            }

        }

        #endregion

        #region FireOnReceived

        private void FireOnReceived(BaseSocketConnection connection, byte[] buffer)
        {

            if (!Disposed)
            {

                if (connection.Active)
                {

                    try
                    {

                        switch (connection.EventProcessing)
                        {

                            case EventProcessing.epUser:

                                FSocketService.OnReceived(new MessageEventArgs(connection, buffer, false));
                                break;

                            case EventProcessing.epEncrypt:

                                OnReceived(connection, buffer);
                                break;

                            case EventProcessing.epProxy:

                                OnReceived(connection, buffer);
                                break;

                        }

                    }
                    finally
                    {
                        //
                    }

                }

            }

        }

        #endregion

        #region FireOnDisconnected

        private void FireOnDisconnected(BaseSocketConnection connection)
        {
            
            if (!Disposed)
            {

                try
                {
                    FSocketService.OnDisconnected(new ConnectionEventArgs(connection));
                }
                finally
                {
                }

            }

        }

        #endregion

        #region FireOnException

        internal void FireOnException(BaseSocketConnection connection, Exception ex)
        {

            if (!Disposed)
            {

                if (connection == null)
                {
                    FSocketService.OnException(new ExceptionEventArgs(connection, ex));
                }
                else
                {

                    if (connection.Active)
                    {

                        try
                        {
                            FSocketService.OnException(new ExceptionEventArgs(connection, ex));
                        }
                        finally
                        {
                        }
                    }
                }

            }

        }

        #endregion

        #endregion

        #region Begin Methods

        #region BeginSend

        /// <summary>
        /// Begin send the data.
        /// </summary>
        internal void BeginSend(BaseSocketConnection connection, byte[] buffer, bool sentByServer)
        {

            if (!Disposed)
            {

                byte[] sendBuffer = null;

                try
                {

                    if (connection.Active)
                    {

                        if ( (connection.EventProcessing == EventProcessing.epUser) && (buffer.Length > FMessageBufferSize) )
                        {
                            throw new MessageLengthException("Message length is greater than Host maximum message length.");
                        }

                        bool completedAsync = true;
                        int bufferSize = 0;

                        sendBuffer = BufferUtils.GetPacketBuffer(connection, buffer, ref bufferSize);

                        lock (connection.WriteQueue)
                        {

                            if (connection.WriteQueueHasItems)
                            {

                                //----- If the connection is sending, enqueue the message!
                                MessageBuffer message = new MessageBuffer(sendBuffer, bufferSize, sentByServer);
                                connection.WriteQueue.Enqueue(message);

                            }
                            else
                            {

                                connection.WriteOV.SetBuffer(sendBuffer, 0, bufferSize);
                                connection.WriteOV.UserToken = new WriteData(connection, sentByServer);

                                //----- If the connection is not sending, send the message!
                                if (connection.Stream != null)
                                {
                                    //----- Ssl!
                                    connection.Stream.BeginWrite(connection.WriteOV.Buffer, 0, bufferSize, new AsyncCallback(BeginSendCallbackSSL), new WriteData(connection, sentByServer));
                                }
                                else
                                {
                                    //----- Socket!
                                    completedAsync = connection.Socket.SendAsync(connection.WriteOV);

                                }

                                connection.WriteQueueHasItems = true;

                            }

                        }

                        sendBuffer = null;

                        if (!completedAsync)
                        {
                            BeginSendCallbackAsync(this, connection.WriteOV);
                        }

                    }

                }
                catch (SocketException soex)
                {

                    if ((soex.SocketErrorCode == SocketError.ConnectionReset)
                        || (soex.SocketErrorCode == SocketError.ConnectionAborted)
                        || (soex.SocketErrorCode == SocketError.NotConnected)
                        || (soex.SocketErrorCode == SocketError.Shutdown)
                        || (soex.SocketErrorCode == SocketError.Disconnecting))
                    {
                        connection.BeginDisconnect();
                    }
                    else
                    {
                        FireOnException(connection, soex);
                    }


                }
                catch (Exception ex)
                {
                    FireOnException(connection, ex);
                }

                if (sendBuffer != null)
                {
                    FBufferManager.ReturnBuffer(sendBuffer);
                }

            }

        }

        #endregion

        #region BeginSendCallbackSSL

        private void BeginSendCallbackSSL(IAsyncResult ar)
        {

            switch (FCallbackThreadType)
            {
             
                case CallbackThreadType.ctWorkerThread:
                    
                    ThreadPool.QueueUserWorkItem(new WaitCallback(BeginSendCallbackSSLP), ar);
                    break;

                case CallbackThreadType.ctIOThread:
                    
                    BeginSendCallbackSSLP(ar);
                    break;

            }

        }

        private void BeginSendCallbackSSLP(object state)
        {

            if (!Disposed)
            {

                IAsyncResult ar = null;
                WriteData writeData = null;
                BaseSocketConnection connection = null;
                bool sentByServer = false;

                try
                {

                    ar = (IAsyncResult)state;
                    writeData = (WriteData)ar.AsyncState;

                    connection = writeData.Connection;
                    sentByServer = writeData.SentByServer;

                    writeData.Connection = null;

                    if (connection.Active)
                    {

                        //----- Ssl!
                        connection.Stream.EndWrite(ar);
                        connection.SetConnectionData(0, connection.WriteOV.Count);

                        FBufferManager.ReturnBuffer(connection.WriteOV.Buffer);

                        FireOnSent(connection, sentByServer);

                        if (connection.Active)
                        {

                            lock (connection.WriteQueue)
                            {

                                if (connection.WriteQueue.Count > 0)
                                {

                                    MessageBuffer messageBuffer = connection.WriteQueue.Dequeue();

                                    connection.WriteOV.SetBuffer(messageBuffer.Buffer, 0, messageBuffer.Count);
                                    connection.WriteOV.UserToken = new WriteData(connection, messageBuffer.SentByServer);

                                    connection.Stream.BeginWrite(connection.WriteOV.Buffer, 0, messageBuffer.Count, new AsyncCallback(BeginSendCallbackSSL), new WriteData(connection, sentByServer));

                                }
                                else
                                {
                                    connection.WriteQueueHasItems = false;
                                }

                            }

                        }

                    }

                }
                catch (Exception ex)
                {
                    FireOnException(connection, ex);
                }

            }

        }

        #endregion

        #region BeginSendCallbackAsync

        private void BeginSendCallbackAsync(object sender, SocketAsyncEventArgs e)
        {

            switch (FCallbackThreadType)
            {

                case CallbackThreadType.ctWorkerThread:
        
                    ThreadPool.QueueUserWorkItem(new WaitCallback(BeginSendCallbackAsyncP), e);
                    break;
                
                case CallbackThreadType.ctIOThread:
                    
                    BeginSendCallbackAsyncP(e);
                    break;

            }

        }

        private void BeginSendCallbackAsyncP(object state)
        {

            if (!Disposed)
            {

                SocketAsyncEventArgs e = null;
                WriteData writeData = null;
                BaseSocketConnection connection = null;

                bool sentByServer = false;
                bool canReadQueue = true;

                try
                {

                    e = (SocketAsyncEventArgs)state;
                    writeData = (WriteData)e.UserToken;

                    connection = writeData.Connection;
                    sentByServer = writeData.SentByServer;

                    writeData.Connection = null;

                    if (connection.Active)
                    {
                        
                        if (e.SocketError == SocketError.Success)
                        {

                            connection.SetConnectionData(0, e.BytesTransferred);

                            if ((e.Offset + e.BytesTransferred) < e.Count)
                            {

                                //----- Continue to send until all bytes are sent!
                                e.SetBuffer(e.Offset + e.BytesTransferred, e.Count - e.BytesTransferred - e.Offset);

                                if (!connection.Socket.SendAsync(e))
                                {
                                    BeginSendCallbackAsync(this, e);
                                }

                                canReadQueue = false;

                            }
                            else
                            {

                                FBufferManager.ReturnBuffer(e.Buffer);
                                e.SetBuffer(null, 0, 0);

                                FireOnSent(connection, sentByServer);

                            }

                        }
                        else
                        {

                            canReadQueue = false;

                            if ((e.SocketError == SocketError.ConnectionReset)
                                || (e.SocketError == SocketError.NotConnected)
                                || (e.SocketError == SocketError.Shutdown)
                                || (e.SocketError == SocketError.ConnectionAborted)
                                || (e.SocketError == SocketError.Disconnecting))
                            {
                                connection.BeginDisconnect();
                            }
                            else
                            {
                                FireOnException(connection, new SocketException((int)e.SocketError));
                            }

                        }

                        //----- Check Queue!
                        if (canReadQueue)
                        {

                            bool completedAsync = true;

                            if (connection.Active)
                            {

                                lock (connection.WriteQueue)
                                {

                                    if (connection.WriteQueue.Count > 0)
                                    {

                                        //----- If has items, send it!
                                        MessageBuffer sendMessage = connection.WriteQueue.Dequeue();

                                        e.SetBuffer(sendMessage.Buffer, 0, sendMessage.Count);
                                        e.UserToken = new WriteData(connection, sendMessage.SentByServer);

                                        completedAsync = connection.Socket.SendAsync(e);

                                    }
                                    else
                                    {
                                        connection.WriteQueueHasItems = false;
                                    }

                                }

                                if (!completedAsync)
                                {
                                    BeginSendCallbackAsync(this, e);
                                }

                            }

                        }

                    }

                }
                catch (SocketException soex)
                {

                    if ((soex.SocketErrorCode == SocketError.ConnectionReset)
                        || (e.SocketError == SocketError.NotConnected)
                        || (soex.SocketErrorCode == SocketError.Shutdown)
                        || (soex.SocketErrorCode == SocketError.ConnectionAborted)
                        || (soex.SocketErrorCode == SocketError.Disconnecting))
                    {
                        connection.BeginDisconnect();
                    }
                    else
                    {
                        FireOnException(connection, soex);
                    }

                }
                catch (Exception ex)
                {
                    FireOnException(connection, ex);
                }

            }

        }

        #endregion

        #region BeginReceive

        /// <summary>
        /// Receive data from connetion.
        /// </summary>
        internal void BeginReceive(BaseSocketConnection connection)
        {

            if (!Disposed)
            {

                byte[] readMessage = null;

                try
                {

                    if (connection.Active)
                    {

                        bool completedAsync = true;

                        lock (connection.SyncReadPending)
                        {

                            if (!connection.ReadPending)
                            {

                                //----- if the connection is not receiving, start the receive!
                                if (connection.EventProcessing == EventProcessing.epUser)
                                {
                                    readMessage = FBufferManager.TakeBuffer(FMessageBufferSize);
                                }
                                else
                                {
                                    readMessage = FBufferManager.TakeBuffer(2048);
                                }

                                connection.ReadOV.SetBuffer(readMessage, 0, readMessage.Length);
                                connection.ReadOV.UserToken = connection;

                                if (connection.Stream != null)
                                {
                                    //----- Ssl!
                                    connection.Stream.BeginRead(connection.ReadOV.Buffer, 0, readMessage.Length, new AsyncCallback(BeginReadCallbackSSL), connection);
                                }
                                else
                                {
                                    completedAsync = connection.Socket.ReceiveAsync(connection.ReadOV);
                                }

                                connection.ReadPending = true;

                            }

                        }

                        if (!completedAsync)
                        {
                            BeginReadCallbackAsync(this, connection.ReadOV);
                        }

                        readMessage = null;

                    }

                }
                catch (SocketException soex)
                {

                    if ((soex.SocketErrorCode == SocketError.ConnectionReset)
                        || (soex.SocketErrorCode == SocketError.NotConnected)
                        || (soex.SocketErrorCode == SocketError.ConnectionAborted)
                        || (soex.SocketErrorCode == SocketError.Shutdown)
                        || (soex.SocketErrorCode == SocketError.Disconnecting))
                    {
                        connection.BeginDisconnect();
                    }
                    else
                    {
                        FireOnException(connection, soex);
                    }

                }
                catch (Exception ex)
                {
                    FireOnException(connection, ex);
                }

                if (readMessage != null)
                {
                    FBufferManager.ReturnBuffer(readMessage);
                }

            }

        }

        #endregion

        #region BeginReadCallbackSSL

        private void BeginReadCallbackSSL(IAsyncResult ar)
        {

            switch (FCallbackThreadType)
            {
                case CallbackThreadType.ctWorkerThread:
                    
                    ThreadPool.QueueUserWorkItem(new WaitCallback(BeginReadCallbackSSLP), ar);
                    break;

                case CallbackThreadType.ctIOThread:

                    BeginReadCallbackSSLP(ar);
                    break;
            }

        }

        private void BeginReadCallbackSSLP(object state)
        {

            if (!Disposed)
            {

                IAsyncResult ar = null;
                BaseSocketConnection connection = null;

                try
                {

                    ar = (IAsyncResult)state;
                    connection = (BaseSocketConnection)ar.AsyncState;

                    if (connection.Active)
                    {

                        int readBytes = 0;

                        readBytes = connection.Stream.EndRead(ar);
                        connection.SetConnectionData(readBytes, 0);
                        
                        if (readBytes > 0)
                        {
                            ReadFromConnection(connection, readBytes);
                        }
                        else
                        {
                            connection.BeginDisconnect();
                        }

                    }
                }
                catch (Exception ex)
                {
                    FireOnException(connection, ex);
                }

            }

        }

        #endregion

        #region BeginReadCallbackAsync

        private void BeginReadCallbackAsync(object sender, SocketAsyncEventArgs e)
        {
            switch (FCallbackThreadType)
            {
                case CallbackThreadType.ctWorkerThread:

                    ThreadPool.QueueUserWorkItem(new WaitCallback(BeginReadCallbackAsyncP), e);
                    break;

                case CallbackThreadType.ctIOThread:

                    BeginReadCallbackAsyncP(e);
                    break;

            }
        }

        private void BeginReadCallbackAsyncP(object state)
        {

            if (!Disposed)
            {

                SocketAsyncEventArgs e = null;
                BaseSocketConnection connection = null;

                try
                {

                    e = (SocketAsyncEventArgs)state;
                    connection = (BaseSocketConnection) e.UserToken;

                    if (connection.Active)
                    {

                        if (e.SocketError == SocketError.Success)
                        {

                            connection.SetConnectionData(e.BytesTransferred, 0);

                            if (e.BytesTransferred > 0)
                            {
                                ReadFromConnection(connection, e.BytesTransferred);
                            }
                            else
                            {
                                //----- Is has no data to read then the connection has been terminated!
                                connection.BeginDisconnect();
                            }

                        }
                        
                        else
                        {

                            if ((e.SocketError == SocketError.ConnectionReset)
                                || (e.SocketError == SocketError.NotConnected)
                                || (e.SocketError == SocketError.Shutdown)
                                || (e.SocketError == SocketError.ConnectionAborted)
                                || (e.SocketError == SocketError.Disconnecting))
                            {
                                connection.BeginDisconnect();
                            }
                            else
                            {
                                FireOnException(connection, new SocketException((int)e.SocketError));
                            }

                        }

                    }

                }
                catch (SocketException soex)
                {

                    if ((soex.SocketErrorCode == SocketError.ConnectionReset)
                        || (soex.SocketErrorCode == SocketError.NotConnected)
                        || (soex.SocketErrorCode == SocketError.Shutdown)
                        || (soex.SocketErrorCode == SocketError.ConnectionAborted)
                        || (soex.SocketErrorCode == SocketError.Disconnecting))
                    {
                        connection.BeginDisconnect();
                    }
                    else
                    {
                        FireOnException(connection, soex);
                    }

                }
                catch (Exception ex)
                {
                    FireOnException(connection, ex);
                }

            }

        }

        #endregion

        #region ReadFromConnection

        private void ReadFromConnection(BaseSocketConnection connection, int readBytes)
        {

            
            bool onePacketFound = false;
            int remainingBytes = 0;
            SocketAsyncEventArgs e = connection.ReadOV;

            switch (connection.DelimiterType)
            {

                case DelimiterType.dtNone:

                    //----- Message with no delimiter!
                    remainingBytes = ReadMessageWithNoDelimiter(connection, e, readBytes);
                    break;

                case DelimiterType.dtMessageTailExcludeOnReceive:
                case DelimiterType.dtMessageTailIncludeOnReceive:


                    //----- Message with tail!
                    remainingBytes = ReadMessageWithTail(connection, e, readBytes, ref onePacketFound);
                    break;

            }

            if (remainingBytes == 0)
            {
                e.SetBuffer(0, e.Buffer.Length);
            }
            else
            {

                if (!onePacketFound)
                {
                    e.SetBuffer(remainingBytes, e.Buffer.Length - remainingBytes);
                }
                else
                {

                    byte[] readMessage = connection.BaseHost.BufferManager.TakeBuffer(FMessageBufferSize);
                    Buffer.BlockCopy(e.Buffer, e.Offset, readMessage, 0, remainingBytes);

                    connection.BaseHost.BufferManager.ReturnBuffer(e.Buffer);
                    e.SetBuffer(null, 0, 0);
                    e.SetBuffer(readMessage, remainingBytes, readMessage.Length - remainingBytes);

                }

            }

            if (connection.Active)
            {

                //----- Read!
                bool completedAsync = true;

                if (connection.Stream != null)
                {
                    connection.Stream.BeginRead(e.Buffer, 0, e.Count, new AsyncCallback(BeginReadCallbackSSL), connection);
                }
                else
                {
                    completedAsync = connection.Socket.ReceiveAsync(e);
                }

                if (!completedAsync)
                {
                    BeginReadCallbackAsync(this, e);
                }

            }

        }

        #endregion

        #region ReadMessageWithNoDelimiter

        private int ReadMessageWithNoDelimiter(BaseSocketConnection connection, SocketAsyncEventArgs e, int readBytes)
        {

            byte[] rawBuffer = null;
            rawBuffer = BufferUtils.GetRawBuffer(connection, e.Buffer, readBytes);
            
            FireOnReceived(connection, rawBuffer);
            return 0;

        }

        #endregion

        #region ReadMessageWithTail

        private int ReadMessageWithTail(BaseSocketConnection connection, SocketAsyncEventArgs e, int readBytes, ref bool onePacketFound)
        {

            byte[] rawBuffer = null;

            byte[] delimiter = connection.Delimiter;
            int delimiterSize = delimiter.Length;
            
            bool readPacket = false;
            bool packetFound = false;

            int remainingBytes = readBytes + e.Offset;

            int bufferLength = e.Buffer.Length;
            byte[] buffer = e.Buffer;
            int offsetToFind = 0;
            int offsetBuffer = e.Offset;

            do
            {

                rawBuffer = null;
                packetFound = false;
                readPacket = false;

                while (offsetToFind < bufferLength)
                {

                    offsetToFind = Array.IndexOf<byte>(buffer, delimiter[0], offsetToFind);

                    if (offsetToFind == -1)
                    {
                        packetFound = false;
                        break;
                    }
                    else
                    {

                        if (delimiterSize == 1)
                        {
                            offsetToFind++;
                            packetFound = true;
                            break;
                        }
                        else
                        {

                            packetFound = true;

                            for (int i = 1; i < delimiterSize; i++)
                            {

                                offsetToFind++;

                                if (buffer[offsetToFind] != delimiter[i])
                                {
                                    packetFound = false;
                                    break;
                                }

                            }

                            if (packetFound)
                            {
                                break;
                            }

                        }

                    }

                }

                if (packetFound)
                {

                    onePacketFound = true;

                    rawBuffer = BufferUtils.GetRawBufferWithTail(connection, e, offsetToFind, delimiterSize);
                    rawBuffer = CryptUtils.DecryptData(connection, rawBuffer, FMessageBufferSize);

                    offsetToFind += 1;
                    remainingBytes -= (offsetToFind - e.Offset);

                    e.SetBuffer(offsetToFind, bufferLength - offsetToFind);
                    offsetBuffer = offsetToFind;

                    FireOnReceived(connection, rawBuffer);

                    if (remainingBytes == 0)
                    {
                        readPacket = false;
                    }
                    else
                    {
                        readPacket = true;
                    }

                }
                else
                {
                    readPacket = false;
                }

            } while (readPacket);

            return remainingBytes;

        }

        #endregion

        #region BeginDisconnect

        /// <summary>
        /// Begin disconnect the connection
        /// </summary>
        internal void BeginDisconnect(BaseSocketConnection connection)
        {

            if (!Disposed)
            {

                if (connection.Active)
                {

                    try
                    {

                        SocketAsyncEventArgs e = new SocketAsyncEventArgs();
                        e.Completed += new EventHandler<SocketAsyncEventArgs>(BeginDisconnectCallbackAsync);
                        e.UserToken = connection;

                        if (!connection.Socket.DisconnectAsync(e))
                        {
                            BeginDisconnectCallbackAsync(this, e);
                        }

                    }
                    catch (Exception ex)
                    {
                        FireOnException(connection, ex);
                    }

                }

            }

        }

        #endregion

        #region BeginDisconnectCallbackAsync

        private void BeginDisconnectCallbackAsync(object sender, SocketAsyncEventArgs e)
        {

            if (!Disposed)
            {

                BaseSocketConnection connection = null;

                try
                {

                    connection = (BaseSocketConnection)e.UserToken;

                    e.Completed -= new EventHandler<SocketAsyncEventArgs>(BeginDisconnectCallbackAsync);
                    e.UserToken = null;
                    e.Dispose();
                    e = null;

                    if (connection.Active)
                    {
                        
                        lock (connection.SyncActive)
                        {
                            CloseConnection(connection);
                            FireOnDisconnected(connection);
                        }

                    }

                }
                finally
                {
                    DisposeConnection(connection);
                    RemoveSocketConnection(connection);
                    connection = null;
                }

            }

        }

        #endregion
        
        #endregion

        #region Abstract Methods

        internal abstract void BeginReconnect(ClientSocketConnection connection);
        internal abstract void BeginSendToAll(ServerSocketConnection connection, byte[] buffer, bool includeMe);
        internal abstract void BeginSendTo(BaseSocketConnection connectionTo, byte[] buffer);

        #endregion

        #region Public Methods

        public ISocketConnection[] GetConnections()
        {

            ISocketConnection[] result = null;

            if (!Disposed)
            {
                result = GetSocketConnections();
            }

            return result;

        }

        public ISocketConnection GetConnectionById(long connectionId)
        {
            
            ISocketConnection result = null;

            if (!Disposed)
            {
                result = GetSocketConnectionById(connectionId);
            }

            return result;

        }

        #endregion

        #endregion

        #region Connection Methods

        #region InitializeConnection

        /// <summary>
        /// Initializes the connection
        /// </summary>
        /// <param name="connection"></param>
        internal virtual void InitializeConnection(BaseSocketConnection connection)
        {

            if (!Disposed)
            {

                switch (connection.EventProcessing)
	            {
		            
                    case EventProcessing.epNone:

                        if (InitializeConnectionProxy(connection))
                        {
                            FireOnConnected(connection);
                        }
                        else
                        {

                            if (InitializeConnectionEncrypt(connection))
                            {
                                FireOnConnected(connection);
                            }
                            else
                            {
                                connection.EventProcessing = EventProcessing.epUser;
                                FireOnConnected(connection);
                            }

                        }

                        break;

                    case EventProcessing.epProxy:

                        if (InitializeConnectionEncrypt(connection))
                        {
                            FireOnConnected(connection);
                        }
                        else
                        {
                            connection.EventProcessing = EventProcessing.epUser;
                            FireOnConnected(connection);
                        }

                        break;
            
                    case EventProcessing.epEncrypt:

                        connection.EventProcessing = EventProcessing.epUser;
                        FireOnConnected(connection);

                        break;
    
	            }

            }

        }

        #endregion

        #region InitializeConnectionProxy

        private bool InitializeConnectionProxy(BaseSocketConnection connection)
        {

            bool result = false;

            if (!Disposed)
            {

                if (connection.BaseCreator is SocketConnector)
                {

                    if (((SocketConnector)connection.BaseCreator).ProxyInfo != null)
                    {
                        connection.EventProcessing = EventProcessing.epProxy;
                        result = true;
                    }

                }

            }

            return result;

        }

        #endregion

        #region InitializeConnectionEncrypt

        internal bool InitializeConnectionEncrypt(BaseSocketConnection connection)
        {

            bool result = false;

            if (!Disposed)
            {

                ICryptoService cryptService = connection.BaseCreator.CryptoService;

                if ((cryptService != null) && (connection.EncryptType != EncryptType.etNone))
                {
                    connection.EventProcessing = EventProcessing.epEncrypt;
                    result = true;
                }

            }

            return result;

        }

        #endregion

        #region GetConnectionId

        internal long GetConnectionId()
        {
            return Interlocked.Increment(ref FConnectionId);
        }

        #endregion

        #region AddSocketConnection

        internal void AddSocketConnection(BaseSocketConnection socketConnection)
        {

            if (!Disposed)
            {

                FSocketConnectionsSync.EnterWriteLock();

                try
                {
                    FSocketConnections.Add(socketConnection.ConnectionId, socketConnection);

                    socketConnection.WriteOV.Completed += new EventHandler<SocketAsyncEventArgs>(BeginSendCallbackAsync);
                    socketConnection.ReadOV.Completed += new EventHandler<SocketAsyncEventArgs>(BeginReadCallbackAsync);

                }
                finally
                {
                    FSocketConnectionsSync.ExitWriteLock();
                }

            }

        }

        #endregion

        #region RemoveSocketConnection

        internal void RemoveSocketConnection(BaseSocketConnection socketConnection)
        {

          if (!Disposed)
          {

              if (socketConnection != null)
              {


                  FSocketConnectionsSync.EnterWriteLock();

                  try
                  {

                      FSocketConnections.Remove(socketConnection.ConnectionId);

                  }
                  finally
                  {

                      if (FSocketConnections.Count <= 0)
                      {
                          FWaitConnectionsDisposing.Set();
                      }

                      FSocketConnectionsSync.ExitWriteLock();

                  }

              }

        }

        }

        #endregion

        #region DisposeAndNullConnection

        internal void DisposeConnection(BaseSocketConnection connection)
        {

            if (!Disposed)
            {

                if (connection != null)
                {

                    if (connection.WriteOV != null)
                    {

                        if (connection.WriteOV.Buffer != null)
                        {
                            FBufferManager.ReturnBuffer(connection.WriteOV.Buffer);
                        }

                    }

                    if (connection.ReadOV != null)
                    {

                        if (connection.ReadOV.Buffer != null)
                        {
                            FBufferManager.ReturnBuffer(connection.ReadOV.Buffer);
                        }

                    }
                    
                    connection.Dispose();

                }

            }

        }

        #endregion

        #region CloseConnection

        internal void CloseConnection(BaseSocketConnection connection)
        {

            if (!Disposed)
            {

                connection.Active = false;
                connection.Socket.Shutdown(SocketShutdown.Send);

                lock (connection.WriteQueue)
                {

                    if (connection.WriteQueue.Count > 0)
                    {

                        for (int i = 1; i <= connection.WriteQueue.Count; i++)
                        {

                            MessageBuffer message = connection.WriteQueue.Dequeue();

                            if (message != null)
                            {
                                FBufferManager.ReturnBuffer(message.Buffer);
                            }

                        }

                    }

                }
            }
        }

        #endregion

        #region GetSocketConnections

        internal BaseSocketConnection[] GetSocketConnections()
        {

            BaseSocketConnection[] items = null;

            if (!Disposed)
            {

                FSocketConnectionsSync.EnterReadLock();

                try
                {
                    items = new BaseSocketConnection[FSocketConnections.Count];
                    FSocketConnections.Values.CopyTo(items, 0);
                }
                finally
                {
                    FSocketConnectionsSync.ExitReadLock();
                }

            }

            return items;

        }

        #endregion

        #region GetSocketConnectionById

        internal BaseSocketConnection GetSocketConnectionById(long connectionId)
        {

            BaseSocketConnection item = null;

            if (!Disposed)
            {

                
                FSocketConnectionsSync.EnterReadLock();

                try
                {
                    item = FSocketConnections[connectionId];
                }
                finally
                {
                    FSocketConnectionsSync.ExitReadLock();
                }

            }

            return item;

        }

        #endregion

        #region CheckSocketConnections

        private void CheckSocketConnections(Object stateInfo)
        {

            if (!Disposed)
            {

                //----- Disable timer event!
                FIdleTimer.Change(Timeout.Infinite, Timeout.Infinite);

                try
                {

                    //----- Get connections!
                    BaseSocketConnection[] items = GetSocketConnections();

                    if (items != null)
                    {

                        int loopSleep = 0;
                        
                        foreach (BaseSocketConnection cnn in items)
                        {

                            if (Disposed)
                            {
                                break;
                            }
                            
                            try
                            {

                                if (cnn != null)
                                {

                                    //----- Check the idle timeout!
                                    if (DateTime.Now > (cnn.LastAction.AddMilliseconds(FIdleTimeOutValue)))
                                    {
                                        cnn.BeginDisconnect();
                                    }

                                }

                            }
                            finally
                            {

                                ThreadEx.LoopSleep(ref loopSleep);

                            }

                        }

                    }

                }
                finally
                {
                    
                    if (!Disposed)
                    {
                        //----- Restart the timer event!
                        FIdleTimer.Change(FIdleCheckInterval, FIdleCheckInterval);
                    }

                }

                GC.Collect();

            }

        }

        #endregion

        #region Creators Methods

        #region AddCreator

        protected void AddCreator(BaseSocketConnectionCreator creator)
        {

            if (!Disposed)
            {
                lock (FSocketCreators)
                {
                    FSocketCreators.Add(creator);
                }

            }

        }

        #endregion

        #region RemoveCreator

        protected void RemoveCreator(BaseSocketConnectionCreator creator)
        {
            
            if (!Disposed)
            {
                
                lock (FSocketCreators)
                {
                    
                    FSocketCreators.Remove(creator);

                    if (FSocketCreators.Count <= 0)
                    {
                        FWaitCreatorsDisposing.Set();
                    }

                }
            }
        }

        #endregion

        #region GetSocketCreators

        protected BaseSocketConnectionCreator[] GetSocketCreators()
        {

            BaseSocketConnectionCreator[] items = null;

            if (!Disposed)
            {
                lock (FSocketCreators)
                {
                    items = new BaseSocketConnectionCreator[FSocketCreators.Count];
                    FSocketCreators.CopyTo(items, 0);
                }

            }

            return items;

        }

        #endregion

        #endregion

        #endregion

        #region EventProcessing Methods

        #region OnConnected

        internal void OnConnected(BaseSocketConnection connection)
        {

            if (!Disposed)
            {

                try
                {

                    if (connection.Active)
                    {


                        switch (connection.EventProcessing)
                        {

                            case EventProcessing.epEncrypt:

                                switch (connection.EncryptType)
                                {

                                    case EncryptType.etRijndael:


                                        if (connection.Host.HostType == HostType.htClient)
                                        {

                                            #region Client

                                            //----- Generate client asymmetric key pair (public and private)
                                            RSACryptoServiceProvider clientKeyPair = new RSACryptoServiceProvider(2048);

                                            //----- Get the server public key
                                            RSACryptoServiceProvider serverPublicKey;
                                            connection.BaseCreator.CryptoService.OnSymmetricAuthenticate(connection, out serverPublicKey);

                                            //----- Generates symmetric algoritm
                                            SymmetricAlgorithm sa = CryptUtils.CreateSymmetricAlgoritm(connection.EncryptType);

                                            //----- Adjust connection cryptors
                                            connection.Encryptor = sa.CreateEncryptor();
                                            connection.Decryptor = sa.CreateDecryptor();

                                            //----- Create authenticate message
                                            AuthMessage am = new AuthMessage();

                                            //----- Encrypt session IV and session Key with server public key
                                            am.SessionIV = serverPublicKey.Encrypt(sa.IV, true);
                                            am.SessionKey = serverPublicKey.Encrypt(sa.Key, true);

                                            //----- Encrypt client public key with symmetric algorithm
                                            am.ClientKey = CryptUtils.EncryptDataForAuthenticate(connection.Encryptor, Encoding.UTF8.GetBytes(clientKeyPair.ToXmlString(false)));

                                            //----- Create hash salt!
                                            am.Data = new byte[32];
                                            RNGCryptoServiceProvider.Create().GetBytes(am.Data);

                                            MemoryStream m = new MemoryStream();

                                            //----- Create a sign with am.SourceKey, am.SessionKey and am.Data (salt)!
                                            m.Write(am.SessionKey, 0, am.SessionKey.Length);
                                            m.Write(am.ClientKey, 0, am.ClientKey.Length);
                                            m.Write(am.Data, 0, am.Data.Length);

                                            am.Sign = clientKeyPair.SignData(CryptUtils.EncryptDataForAuthenticate(connection.Encryptor, m.ToArray()), "SHA256");

                                            //----- Serialize authentication message
                                            m.SetLength(0);
                                            new BinaryFormatter().Serialize(m, am);

                                            connection.BeginSend(m.ToArray());

                                            m.Close();

                                            am.SessionIV.Initialize();
                                            am.SessionKey.Initialize();

                                            serverPublicKey.Clear();
                                            clientKeyPair.Clear();

                                            #endregion

                                        }
                                        else
                                        {

                                            #region Server

                                            connection.BeginReceive();

                                            #endregion

                                        }

                                        break;

                                    case EncryptType.etSSL:


                                        if (connection.Host.HostType == HostType.htClient)
                                        {

                                            #region Client

                                            //----- Get SSL items
                                            X509Certificate2Collection certs = null;
                                            string serverName = null;
                                            bool checkRevocation = true;

                                            connection.BaseCreator.CryptoService.OnSSLClientAuthenticate(connection, out serverName, ref certs, ref checkRevocation);

                                            //----- Authenticate SSL!
                                            SslStream ssl = new SslStream(new NetworkStream(connection.Socket), true, new RemoteCertificateValidationCallback(connection.BaseCreator.ValidateServerCertificateCallback));

                                            if (certs == null)
                                            {
                                                ssl.BeginAuthenticateAsClient(serverName, new AsyncCallback(SslAuthenticateCallback), new AuthenticateCallbackData(connection, ssl, HostType.htClient));
                                            }
                                            else
                                            {
                                                ssl.BeginAuthenticateAsClient(serverName, certs, System.Security.Authentication.SslProtocols.Tls, checkRevocation, new AsyncCallback(SslAuthenticateCallback), new AuthenticateCallbackData(connection, ssl, HostType.htClient));
                                            }

                                            #endregion

                                        }
                                        else
                                        {

                                            #region Server

                                            //----- Get SSL items!
                                            X509Certificate2 cert = null;
                                            bool clientAuthenticate = false;
                                            bool checkRevocation = true;

                                            connection.BaseCreator.CryptoService.OnSSLServerAuthenticate(connection, out cert, out clientAuthenticate, ref checkRevocation);

                                            //----- Authneticate SSL!
                                            SslStream ssl = new SslStream(new NetworkStream(connection.Socket));
                                            ssl.BeginAuthenticateAsServer(cert, clientAuthenticate, System.Security.Authentication.SslProtocols.Default, checkRevocation, new AsyncCallback(SslAuthenticateCallback), new AuthenticateCallbackData(connection, ssl, HostType.htServer));

                                            #endregion

                                        }

                                        break;

                                }

                                break;

                            case EventProcessing.epProxy:

                                ProxyInfo proxyInfo = ((SocketConnector) connection.BaseCreator).ProxyInfo;
                                IPEndPoint endPoint = ((SocketConnector) connection.BaseCreator).RemoteEndPoint;
                                byte[] proxyBuffer = ProxyUtils.GetProxyRequestData(proxyInfo, endPoint);

                                connection.BeginSend(proxyBuffer);

                                break;

                        }

                        

                    }

                }
                catch(Exception ex)
                {
                    FireOnException(connection, ex);
                }

            }

        }

        #endregion

        #region OnSent

        internal void OnSent(BaseSocketConnection connection)
        {

            if (!Disposed)
            {

                if (connection.Active)
                {

                    try
                    {

                        switch (connection.EventProcessing)
                        {

                            case EventProcessing.epEncrypt:

                                if (connection.Host.HostType == HostType.htServer)
                                {
                                    connection.EventProcessing = EventProcessing.epUser;
                                    FireOnConnected(connection);
                                }
                                else
                                {
                                    connection.BeginReceive();
                                }

                                break;

                            case EventProcessing.epProxy:

                                connection.BeginReceive();
                                break;

                        }


                    }
                    catch (Exception ex)
                    {
                        FireOnException(connection, ex);
                    }

                }

            }

        }

        #endregion

        #region OnReceived

        internal void OnReceived(BaseSocketConnection connection, byte[] buffer)
        {

            if (!Disposed)
            {

                if (connection.Active)
                {

                    try
                    {

                        switch (connection.EventProcessing)
                        {

                            case EventProcessing.epEncrypt:

                                if (connection.Host.HostType == HostType.htServer)
                                {

                                    #region Server

                                    //----- Deserialize authentication message
                                    MemoryStream m = new MemoryStream();
                                    m.Write(buffer, 0, buffer.Length);
                                    m.Position = 0;

                                    BinaryFormatter b = new BinaryFormatter();

                                    AuthMessage am = null;

                                    try
                                    {
                                        am = (AuthMessage) b.Deserialize(m);
                                    }
                                    catch
                                    {
                                        am = null;
                                    }

                                    if (am != null)
                                    {

                                        //----- Server private key
                                        RSACryptoServiceProvider serverPrivateKey;
                                        connection.BaseCreator.CryptoService.OnSymmetricAuthenticate(connection, out serverPrivateKey);

                                        //----- Decrypt session Key and session IV with server private key
                                        SymmetricAlgorithm sa = CryptUtils.CreateSymmetricAlgoritm(connection.Creator.EncryptType);
                                        sa.Key = serverPrivateKey.Decrypt(am.SessionKey, true);
                                        sa.IV = serverPrivateKey.Decrypt(am.SessionIV, true);

                                        //----- Adjust connection cryptors
                                        connection.Encryptor = sa.CreateEncryptor();
                                        connection.Decryptor = sa.CreateDecryptor();

                                        //----- Verify sign
                                        RSACryptoServiceProvider clientPublicKey = new RSACryptoServiceProvider();
                                        clientPublicKey.FromXmlString(Encoding.UTF8.GetString(CryptUtils.DecryptDataForAuthenticate(connection.Decryptor, am.ClientKey)));

                                        m.SetLength(0);
                                        m.Write(am.SessionKey, 0, am.SessionKey.Length);
                                        m.Write(am.ClientKey, 0, am.ClientKey.Length);
                                        m.Write(am.Data, 0, am.Data.Length);

                                        am.SessionIV.Initialize();
                                        am.SessionKey.Initialize();
                                        am.ClientKey.Initialize();

                                        if (clientPublicKey.VerifyData(CryptUtils.EncryptDataForAuthenticate(connection.Encryptor, m.ToArray()), "SHA256", am.Sign))
                                        {

                                            am.Data = new byte[32];
                                            RNGCryptoServiceProvider.Create().GetBytes(am.Data);

                                            am.SessionIV = null;
                                            am.SessionKey = null;
                                            am.ClientKey = null;
                                            am.Sign = serverPrivateKey.SignData(am.Data, "SHA256");

                                            m.SetLength(0);
                                            b.Serialize(m, am);

                                            BeginSend(connection, m.ToArray(), false);

                                        }
                                        else
                                        {
                                            FireOnException(connection, new SymmetricAuthenticationException("Symmetric sign error."));
                                        }

                                        am.Sign.Initialize();
                                        m.Close();

                                        serverPrivateKey.Clear();
                                        clientPublicKey.Clear();

                                    }
                                    else
                                    {
                                        FireOnException(connection, new SymmetricAuthenticationException("Symmetric sign error."));
                                    }

                                    #endregion

                                }
                                else
                                {

                                    #region Client

                                    //----- Deserialize authentication message
                                    MemoryStream m = new MemoryStream();
                                    m.Write(buffer, 0, buffer.Length);
                                    m.Position = 0;

                                    AuthMessage am = null;
                                    BinaryFormatter b = new BinaryFormatter();

                                    try
                                    {
                                        am = (AuthMessage)b.Deserialize(m);
                                    }
                                    catch
                                    {
                                        am = null;
                                    }

                                    if (am != null)
                                    {

                                        RSACryptoServiceProvider serverPublicKey;
                                        connection.BaseCreator.CryptoService.OnSymmetricAuthenticate(connection, out serverPublicKey);

                                        //----- Verify sign
                                        if (serverPublicKey.VerifyData(am.Data, "SHA256", am.Sign))
                                        {
                                            connection.EventProcessing = EventProcessing.epUser;
                                            FireOnConnected(connection);
                                        }
                                        else
                                        {
                                            FireOnException(connection, new SymmetricAuthenticationException("Symmetric sign error."));
                                        }

                                        am.Data.Initialize();
                                        am.Sign.Initialize();

                                        serverPublicKey.Clear();

                                    }
                                    else
                                    {
                                        FireOnException(connection, new SymmetricAuthenticationException("Symmetric sign error."));
                                    }

                                    m.Close();

                                    #endregion

                                }

                                break;

                            case EventProcessing.epProxy:

                                ProxyInfo proxyInfo = ((SocketConnector)connection.BaseCreator).ProxyInfo;
                                ProxyUtils.GetProxyResponseStatus(proxyInfo, buffer);

                                if (proxyInfo.Completed)
                                {

                                    InitializeConnection(connection);

                                }
                                else
                                {

                                    IPEndPoint endPoint = ((SocketConnector)connection.BaseCreator).RemoteEndPoint;
                                    byte[] proxyBuffer = ProxyUtils.GetProxyRequestData(proxyInfo, endPoint);

                                    connection.BeginSend(proxyBuffer);

                                }

                                break;


                        }

                    }
                    catch (Exception ex)
                    {
                        FireOnException(connection, ex);
                    }

                }

            }

        }

        #endregion

        #region SslAuthenticateCallback

        private void SslAuthenticateCallback(IAsyncResult ar)
        {

            if (!Disposed)
            {

                BaseSocketConnection connection = null;
                SslStream stream = null;
                bool completed = false;

                try
                {

                    AuthenticateCallbackData callbackData = (AuthenticateCallbackData)ar.AsyncState;

                    connection = callbackData.Connection;
                    stream = callbackData.Stream;

                    if (connection.Active)
                    {

                        if (callbackData.HostType == HostType.htClient)
                        {
                            stream.EndAuthenticateAsClient(ar);
                        }
                        else
                        {
                            stream.EndAuthenticateAsServer(ar);
                        }

                        if ((stream.IsSigned && stream.IsEncrypted))
                        {
                            completed = true;
                        }

                        callbackData = null;
                        connection.Stream = stream;

                        if (completed)
                        {
                            connection.EventProcessing = EventProcessing.epUser;
                            FireOnConnected(connection);
                        }
                        else
                        {
                            FireOnException(connection, new SSLAuthenticationException("Ssl authenticate is not signed or not encrypted."));
                        }

                    }

                }
                catch (Exception ex)
                {
                    FireOnException(connection, ex);
                }

            }

        }

        #endregion

        #endregion

        #region Properties

        internal BufferManager BufferManager
        {
            get { return FBufferManager; }
        }

        public int SocketBufferSize
        {
            get { return FSocketBufferSize; }
            set { FSocketBufferSize = value; }  
        }

        public int MessageBufferSize
        {
            get { return FMessageBufferSize; }
            set { FMessageBufferSize = value; }  
        }

        public byte[] DelimiterEncrypt
        {
            get { return FDelimiterEncrypt; }
            set { FDelimiterEncrypt = value; }
        }

        public byte[] Delimiter
        {
            get { return FDelimiter; }
            set { FDelimiter = value; }
        }

        public DelimiterType DelimiterType
        {
            get { return FDelimiterType; }
            set { FDelimiterType = value; }
        }

        public ISocketService SocketService
        {
            get { return FSocketService; }
        }

        protected Timer CheckTimeOutTimer
        {
            get { return CheckTimeOutTimer; }
        }

        public int IdleCheckInterval
        {
            get { return FIdleCheckInterval; }
            set { FIdleCheckInterval = value; }
        }

        public int IdleTimeOutValue
        {
            get { return FIdleTimeOutValue; }
            set { FIdleTimeOutValue = value; } 
        }

        public HostType HostType
        {
            get { return FHostType; }
        }

        public bool Active
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

            internal set
            {
                lock (FSyncActive)
                {
                    FActive = value;
                }
            }

        }


        #endregion

    }

}
