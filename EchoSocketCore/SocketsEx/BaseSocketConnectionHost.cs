using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Runtime.Serialization.Formatters.Binary;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.ServiceModel.Channels;
using System.Text;
using System.Threading;
using EchoSocketCore.ThreadingEx;

namespace EchoSocketCore.SocketsEx
{
    /// <summary>
    /// The connection host.
    /// </summary>
    public abstract class BaseSocketConnectionHost : BaseDisposable, IBaseSocketConnectionHost
    {
        #region Fields

        private ReaderWriterLockSlim fSocketConnectionsSync;

        private ManualResetEvent fWaitCreatorsDisposing;

        private ManualResetEvent fWaitConnectionsDisposing;

        private ManualResetEvent fWaitThreadsDisposing;

        private ISocketSecurityProvider socketSecurityProvider; 

        private Timer fIdleTimer;

        #endregion Fields

        #region Constructor

        public BaseSocketConnectionHost(HostType hostType, CallbackThreadType callbackThreadtype, ISocketService socketService, DelimiterType delimiterType, byte[] delimiter, int socketBufferSize, int messageBufferSize, int idleCheckInterval, int idleTimeOutValue)
        {
            Context = new SocketHostContext
            {
                Active = false,
                SyncActive = new object(),
                SocketCreators = new List<BaseSocketConnectionCreator>(),
                SocketConnections = new Dictionary<long, BaseSocketConnection>(),
                BufferManager = BufferManager.CreateBufferManager(0, messageBufferSize),
                SocketService = socketService,
                IdleCheckInterval = idleCheckInterval,
                IdleTimeOutValue = idleTimeOutValue,
                CallbackThreadType = callbackThreadtype,
                DelimiterType = delimiterType,
                Delimiter = delimiter,
                DelimiterEncrypt = new byte[] { 0xFE, 0xDC, 0xBA, 0x98, 0xBA, 0xDC, 0xFE },
                MessageBufferSize = messageBufferSize,
                SocketBufferSize = socketBufferSize,
                HostType = hostType

            };
               
            fSocketConnectionsSync = new ReaderWriterLockSlim();
            fWaitCreatorsDisposing = new ManualResetEvent(false);
            fWaitConnectionsDisposing = new ManualResetEvent(false);
            fWaitThreadsDisposing = new ManualResetEvent(false);

        }

        public BaseSocketConnectionHost(HostType hostType, ISocketService socketService, DelimiterType delimiterType, byte[] delimiter, int socketBufferSize, int messageBufferSize, int idleCheckInterval, int idleTimeOutValue):
            this(hostType, CallbackThreadType.ctWorkerThread, socketService, delimiterType,delimiter, socketBufferSize, messageBufferSize, idleCheckInterval, idleTimeOutValue)
        {

        }

        public BaseSocketConnectionHost(HostType hostType, ISocketService socketService, DelimiterType delimiterType, byte[] delimiter, int idleCheckInterval, int idleTimeOutValue)
            : this(hostType, CallbackThreadType.ctWorkerThread, socketService, delimiterType, delimiter, 1024, 1024, idleCheckInterval, idleTimeOutValue)
        {

        }

         public BaseSocketConnectionHost(HostType hostType, ISocketService socketService, DelimiterType delimiterType, byte[] delimiter) :
            this(hostType, CallbackThreadType.ctWorkerThread, socketService, delimiterType, delimiter, 1024, 1024, 1000, 1000)
        {

        }

        #endregion Constructor

        #region Destructor

        public override void Free(bool canAccessFinalizable)
        {
            if (fIdleTimer != null)
            {
                fIdleTimer.Change(Timeout.Infinite, Timeout.Infinite);
                fIdleTimer.Dispose();
                fIdleTimer = null;
            }

            if (fWaitCreatorsDisposing != null)
            {
                fWaitCreatorsDisposing.Set();
                fWaitCreatorsDisposing.Close();
                fWaitCreatorsDisposing = null;
            }

            if (fWaitConnectionsDisposing != null)
            {
                fWaitConnectionsDisposing.Set();
                fWaitConnectionsDisposing.Close();
                fWaitConnectionsDisposing = null;
            }

            if (fWaitThreadsDisposing != null)
            {
                fWaitThreadsDisposing.Set();
                fWaitThreadsDisposing.Close();
                fWaitThreadsDisposing = null;
            }

            Context.Free(canAccessFinalizable);

            base.Free(canAccessFinalizable);
        }

        #endregion Destructor

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

                foreach (BaseSocketConnectionCreator creator in Context.SocketCreators)
                {
                    creator.Start();
                    ThreadEx.LoopSleep(ref loopSleep);
                }

                if ((Context.IdleCheckInterval > 0) && (Context.IdleTimeOutValue > 0))
                {
                    fIdleTimer = new Timer(new TimerCallback(CheckSocketConnections));
                }

                if (fIdleTimer != null)
                {
                    fIdleTimer.Change(Context.IdleCheckInterval, Context.IdleCheckInterval);
                }

                Active = true;
            }
        }

        #endregion Start

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

        #endregion Stop

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
                    fWaitCreatorsDisposing.Reset();

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
                        fWaitCreatorsDisposing.WaitOne(Timeout.Infinite, false);
                    }
                }
            }
        }

        #endregion StopCreators

        #region StopConnections

        protected void StopConnections()
        {
            if (!Disposed)
            {
                //----- Stop Connections!
                BaseSocketConnection[] connections = GetSocketConnections();

                if (connections != null)
                {
                    fWaitConnectionsDisposing.Reset();

                    int loopSleep = 0;

                    foreach (BaseSocketConnection connection in connections)
                    {
                        connection.BeginDisconnect();
                        ThreadEx.LoopSleep(ref loopSleep);
                    }

                    if (connections.Length > 0)
                    {
                        fWaitConnectionsDisposing.WaitOne(Timeout.Infinite, false);
                    }
                }
            }
        }

        #endregion StopConnections

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
                        switch (connection.Context.EventProcessing)
                        {
                            case EventProcessing.epUser:

                                Context.SocketService.OnConnected(new ConnectionEventArgs(connection));
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

        #endregion FireOnConnected

        #region FireOnSent

        private void FireOnSent(BaseSocketConnection connection, bool sentByServer)
        {
            if (!Disposed)
            {
                if (connection.Active)
                {
                    try
                    {
                        switch (connection.Context.EventProcessing)
                        {
                            case EventProcessing.epUser:

                                Context.SocketService.OnSent(new MessageEventArgs(connection, null, sentByServer));
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

        #endregion FireOnSent

        #region FireOnReceived

        private void FireOnReceived(BaseSocketConnection connection, byte[] buffer)
        {
            if (!Disposed)
            {
                if (connection.Active)
                {
                    try
                    {
                        switch (connection.Context.EventProcessing)
                        {
                            case EventProcessing.epUser:

                                Context.SocketService.OnReceived(new MessageEventArgs(connection, buffer, false));
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

        #endregion FireOnReceived

        #region FireOnDisconnected

        private void FireOnDisconnected(BaseSocketConnection connection)
        {
            if (!Disposed)
            {
                try
                {
                    Context.SocketService.OnDisconnected(new ConnectionEventArgs(connection));
                }
                finally
                {
                }
            }
        }

        #endregion FireOnDisconnected

        #region FireOnException

        internal void FireOnException(BaseSocketConnection connection, Exception ex)
        {
            if (!Disposed)
            {
                if (connection == null)
                {
                    Context.SocketService.OnException(new ExceptionEventArgs(connection, ex));
                }
                else
                {
                    if (connection.Active)
                    {
                        try
                        {
                            Context.SocketService.OnException(new ExceptionEventArgs(connection, ex));
                        }
                        finally
                        {
                        }
                    }
                }
            }
        }

        #endregion FireOnException

        #endregion Fire Methods

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
                        if ((connection.Context.EventProcessing == EventProcessing.epUser) && (buffer.Length > Context.MessageBufferSize))
                        {
                            throw new MessageLengthException("Message length is greater than Host maximum message length.");
                        }

                        bool completedAsync = true;
                        int bufferSize = 0;

                        sendBuffer = BufferUtils.GetPacketBuffer(connection, buffer, ref bufferSize);

                        lock (connection.Context.WriteQueue)
                        {
                            if (connection.Context.WriteQueueHasItems)
                            {
                                //----- If the connection is sending, enqueue the message!
                                MessageBuffer message = new MessageBuffer(sendBuffer, bufferSize, sentByServer);
                                connection.Context.WriteQueue.Enqueue(message);
                            }
                            else
                            {
                                connection.WriteOV.SetBuffer(sendBuffer, 0, bufferSize);
                                connection.WriteOV.UserToken = new WriteData(connection, sentByServer);

                                //----- If the connection is not sending, send the message!
                                if (connection.Context.Stream != null)
                                {
                                    //----- Ssl!
                                    connection.Context.Stream.BeginWrite(connection.WriteOV.Buffer, 0, bufferSize, new AsyncCallback(BeginSendCallbackSSL), new WriteData(connection, sentByServer));
                                }
                                else
                                {
                                    //----- Socket!
                                    completedAsync = connection.Context.SocketHandle.SendAsync(connection.WriteOV);
                                }

                                connection.Context.WriteQueueHasItems = true;
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
                    Context.BufferManager.ReturnBuffer(sendBuffer);
                }
            }
        }

        #endregion BeginSend

        #region BeginSendCallbackSSL

        private void BeginSendCallbackSSL(IAsyncResult ar)
        {
            switch (Context.CallbackThreadType)
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
                        connection.Context.Stream.EndWrite(ar);
                        connection.SetConnectionData(0, connection.WriteOV.Count);

                        Context.BufferManager.ReturnBuffer(connection.WriteOV.Buffer);

                        FireOnSent(connection, sentByServer);

                        if (connection.Active)
                        {
                            lock (connection.Context.WriteQueue)
                            {
                                if (connection.Context.WriteQueue.Count > 0)
                                {
                                    MessageBuffer messageBuffer = connection.Context.WriteQueue.Dequeue();

                                    connection.WriteOV.SetBuffer(messageBuffer.Buffer, 0, messageBuffer.Count);
                                    connection.WriteOV.UserToken = new WriteData(connection, messageBuffer.SentByServer);

                                    connection.Context.Stream.BeginWrite(connection.WriteOV.Buffer, 0, messageBuffer.Count, new AsyncCallback(BeginSendCallbackSSL), new WriteData(connection, sentByServer));
                                }
                                else
                                {
                                    connection.Context.WriteQueueHasItems = false;
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

        #endregion BeginSendCallbackSSL

        #region BeginSendCallbackAsync

        private void BeginSendCallbackAsync(object sender, SocketAsyncEventArgs e)
        {
            switch (Context.CallbackThreadType)
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

                                if (!connection.Context.SocketHandle.SendAsync(e))
                                {
                                    BeginSendCallbackAsync(this, e);
                                }

                                canReadQueue = false;
                            }
                            else
                            {
                                Context.BufferManager.ReturnBuffer(e.Buffer);
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
                                lock (connection.Context.WriteQueue)
                                {
                                    if (connection.Context.WriteQueue.Count > 0)
                                    {
                                        //----- If has items, send it!
                                        MessageBuffer sendMessage = connection.Context.WriteQueue.Dequeue();

                                        e.SetBuffer(sendMessage.Buffer, 0, sendMessage.Count);
                                        e.UserToken = new WriteData(connection, sendMessage.SentByServer);

                                        completedAsync = connection.Context.SocketHandle.SendAsync(e);
                                    }
                                    else
                                    {
                                        connection.Context.WriteQueueHasItems = false;
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

        #endregion BeginSendCallbackAsync

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

                        lock (connection.Context.SyncReadPending)
                        {
                            if (!connection.Context.ReadPending)
                            {
                                //----- if the connection is not receiving, start the receive!
                                if (connection.Context.EventProcessing == EventProcessing.epUser)
                                {
                                    readMessage = Context.BufferManager.TakeBuffer(Context.MessageBufferSize);
                                }
                                else
                                {
                                    readMessage = Context.BufferManager.TakeBuffer(2048);
                                }

                                connection.ReadOV.SetBuffer(readMessage, 0, readMessage.Length);
                                connection.ReadOV.UserToken = connection;

                                if (connection.Context.Stream != null)
                                {
                                    //----- Ssl!
                                    connection.Context.Stream.BeginRead(connection.ReadOV.Buffer, 0, readMessage.Length, new AsyncCallback(BeginReadCallbackSSL), connection);
                                }
                                else
                                {
                                    completedAsync = connection.Context.SocketHandle.ReceiveAsync(connection.ReadOV);
                                }

                                connection.Context.ReadPending = true;
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
                    Context.BufferManager.ReturnBuffer(readMessage);
                }
            }
        }

        #endregion BeginReceive

        #region BeginReadCallbackSSL

        private void BeginReadCallbackSSL(IAsyncResult ar)
        {
            switch (Context.CallbackThreadType)
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

                        readBytes = connection.Context.Stream.EndRead(ar);
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

        #endregion BeginReadCallbackSSL

        #region BeginReadCallbackAsync

        private void BeginReadCallbackAsync(object sender, SocketAsyncEventArgs e)
        {
            switch (Context.CallbackThreadType)
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
                    connection = (BaseSocketConnection)e.UserToken;

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

        #endregion BeginReadCallbackAsync

        #region ReadFromConnection

        private void ReadFromConnection(BaseSocketConnection connection, int readBytes)
        {
            bool onePacketFound = false;
            int remainingBytes = 0;
            SocketAsyncEventArgs e = connection.ReadOV;

            switch (connection.Context.DelimiterType)
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
                    byte[] readMessage = connection.Context.Host.Context.BufferManager.TakeBuffer(Context.MessageBufferSize);
                    Buffer.BlockCopy(e.Buffer, e.Offset, readMessage, 0, remainingBytes);

                    connection.Context.Host.Context.BufferManager.ReturnBuffer(e.Buffer);
                    e.SetBuffer(null, 0, 0);
                    e.SetBuffer(readMessage, remainingBytes, readMessage.Length - remainingBytes);
                }
            }

            if (connection.Active)
            {
                //----- Read!
                bool completedAsync = true;

                if (connection.Context.Stream != null)
                {
                    connection.Context.Stream.BeginRead(e.Buffer, 0, e.Count, new AsyncCallback(BeginReadCallbackSSL), connection);
                }
                else
                {
                    completedAsync = connection.Context.SocketHandle.ReceiveAsync(e);
                }

                if (!completedAsync)
                {
                    BeginReadCallbackAsync(this, e);
                }
            }
        }

        #endregion ReadFromConnection

        #region ReadMessageWithNoDelimiter

        private int ReadMessageWithNoDelimiter(BaseSocketConnection connection, SocketAsyncEventArgs e, int readBytes)
        {
            byte[] rawBuffer = null;
            rawBuffer = BufferUtils.GetRawBuffer(connection, e.Buffer, readBytes);

            FireOnReceived(connection, rawBuffer);
            return 0;
        }

        #endregion ReadMessageWithNoDelimiter

        #region ReadMessageWithTail

        private int ReadMessageWithTail(BaseSocketConnection connection, SocketAsyncEventArgs e, int readBytes, ref bool onePacketFound)
        {
            byte[] rawBuffer = null;

            byte[] delimiter = connection.Context.Delimiter;
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
                    rawBuffer = CryptUtils.DecryptData(connection, rawBuffer, Context.MessageBufferSize);

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

        #endregion ReadMessageWithTail

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

                        if (!connection.Context.SocketHandle.DisconnectAsync(e))
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

        #endregion BeginDisconnect

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
                        lock (connection.Context.SyncActive)
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

        #endregion BeginDisconnectCallbackAsync

        #endregion Begin Methods

        #region Abstract Methods

        internal abstract void BeginReconnect(ClientSocketConnection connection);

        internal abstract void BeginSendToAll(ServerSocketConnection connection, byte[] buffer, bool includeMe);

        internal abstract void BeginSendTo(BaseSocketConnection connectionTo, byte[] buffer);

        #endregion Abstract Methods

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

        #endregion Public Methods

        #endregion Methods

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
                switch (connection.Context.EventProcessing)
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
                                connection.Context.EventProcessing = EventProcessing.epUser;
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
                            connection.Context.EventProcessing = EventProcessing.epUser;
                            FireOnConnected(connection);
                        }

                        break;

                    case EventProcessing.epEncrypt:

                        connection.Context.EventProcessing = EventProcessing.epUser;
                        FireOnConnected(connection);

                        break;
                }
            }
        }

        #endregion InitializeConnection

        #region InitializeConnectionProxy

        private bool InitializeConnectionProxy(BaseSocketConnection connection)
        {
            bool result = false;

            if (!Disposed)
            {
                if (connection.Context.Creator is SocketConnector)
                {
                    if (((SocketConnector)connection.Context.Creator).ProxyInfo != null)
                    {
                        connection.Context.EventProcessing = EventProcessing.epProxy;
                        result = true;
                    }
                }
            }

            return result;
        }

        #endregion InitializeConnectionProxy

        #region InitializeConnectionEncrypt

        internal bool InitializeConnectionEncrypt(BaseSocketConnection connection)
        {
            bool result = false;

            if (!Disposed)
            {
                ICryptoService cryptService = connection.Context.Creator.Context.CryptoService;

                if ((cryptService != null) && (connection.Context.Creator.Context.EncryptType != EncryptType.etNone))
                {
                    connection.Context.EventProcessing = EventProcessing.epEncrypt;
                    result = true;
                }
            }

            return result;
        }

        #endregion InitializeConnectionEncrypt


        #region AddSocketConnection

        internal void AddSocketConnection(BaseSocketConnection socketConnection)
        {
            if (!Disposed)
            {
                fSocketConnectionsSync.EnterWriteLock();

                try
                {
                    Context.SocketConnections.Add(socketConnection.Context.ConnectionId, socketConnection);

                    socketConnection.WriteOV.Completed += new EventHandler<SocketAsyncEventArgs>(BeginSendCallbackAsync);
                    socketConnection.ReadOV.Completed += new EventHandler<SocketAsyncEventArgs>(BeginReadCallbackAsync);
                }
                finally
                {
                    fSocketConnectionsSync.ExitWriteLock();
                }
            }
        }

        #endregion AddSocketConnection

        #region RemoveSocketConnection

        internal void RemoveSocketConnection(BaseSocketConnection socketConnection)
        {
            if (!Disposed)
            {
                if (socketConnection != null)
                {
                    fSocketConnectionsSync.EnterWriteLock();

                    try
                    {
                        Context.SocketConnections.Remove(socketConnection.Context.ConnectionId);
                    }
                    finally
                    {
                        if (Context.SocketConnections.Count <= 0)
                        {
                            fWaitConnectionsDisposing.Set();
                        }

                        fSocketConnectionsSync.ExitWriteLock();
                    }
                }
            }
        }

        #endregion RemoveSocketConnection

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
                            Context.BufferManager.ReturnBuffer(connection.WriteOV.Buffer);
                        }
                    }

                    if (connection.ReadOV != null)
                    {
                        if (connection.ReadOV.Buffer != null)
                        {
                            Context.BufferManager.ReturnBuffer(connection.ReadOV.Buffer);
                        }
                    }

                    connection.Dispose();
                }
            }
        }

        #endregion DisposeAndNullConnection

        #region CloseConnection

        internal void CloseConnection(BaseSocketConnection connection)
        {
            if (!Disposed)
            {
                connection.Active = false;
                connection.Context.SocketHandle.Shutdown(SocketShutdown.Send);

                lock (connection.Context.WriteQueue)
                {
                    if (connection.Context.WriteQueue.Count > 0)
                    {
                        for (int i = 1; i <= connection.Context.WriteQueue.Count; i++)
                        {
                            MessageBuffer message = connection.Context.WriteQueue.Dequeue();

                            if (message != null)
                            {
                                Context.BufferManager.ReturnBuffer(message.Buffer);
                            }
                        }
                    }
                }
            }
        }

        #endregion CloseConnection

        #region GetSocketConnections

        internal BaseSocketConnection[] GetSocketConnections()
        {
            BaseSocketConnection[] items = null;

            if (!Disposed)
            {
                fSocketConnectionsSync.EnterReadLock();

                try
                {
                    items = new BaseSocketConnection[Context.SocketConnections.Count];
                    Context.SocketConnections.Values.CopyTo(items, 0);
                }
                finally
                {
                    fSocketConnectionsSync.ExitReadLock();
                }
            }

            return items;
        }

        #endregion GetSocketConnections

        #region GetSocketConnectionById

        internal BaseSocketConnection GetSocketConnectionById(long connectionId)
        {
            BaseSocketConnection item = null;

            if (!Disposed)
            {
                fSocketConnectionsSync.EnterReadLock();

                try
                {
                    item = Context.SocketConnections[connectionId];
                }
                finally
                {
                    fSocketConnectionsSync.ExitReadLock();
                }
            }

            return item;
        }

        #endregion GetSocketConnectionById

        #region CheckSocketConnections

        private void CheckSocketConnections(Object stateInfo)
        {
            if (!Disposed)
            {
                //----- Disable timer event!
                fIdleTimer.Change(Timeout.Infinite, Timeout.Infinite);

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
                                    if (DateTime.Now > (cnn.Context.LastAction.AddMilliseconds(Context.IdleTimeOutValue)))
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
                        fIdleTimer.Change(Context.IdleCheckInterval, Context.IdleCheckInterval);
                    }
                }

                GC.Collect();
            }
        }

        #endregion CheckSocketConnections

        #region Creators Methods

        #region AddCreator

        protected void AddCreator(BaseSocketConnectionCreator creator)
        {
            if (!Disposed)
            {
                lock (Context.SocketCreators)
                {
                    Context.SocketCreators.Add(creator);
                }
            }
        }

        #endregion AddCreator

        #region RemoveCreator

        protected void RemoveCreator(BaseSocketConnectionCreator creator)
        {
            if (!Disposed)
            {
                lock (Context.SocketCreators)
                {
                    Context.SocketCreators.Remove(creator);

                    if (Context.SocketCreators.Count <= 0)
                    {
                        fWaitCreatorsDisposing.Set();
                    }
                }
            }
        }

        #endregion RemoveCreator

        #region GetSocketCreators

        protected BaseSocketConnectionCreator[] GetSocketCreators()
        {
            BaseSocketConnectionCreator[] items = null;

            if (!Disposed)
            {
                lock (Context.SocketCreators)
                {
                    items = new BaseSocketConnectionCreator[Context.SocketCreators.Count];
                    Context.SocketCreators.CopyTo(items, 0);
                }
            }

            return items;
        }

        #endregion GetSocketCreators

        #endregion Creators Methods

        #endregion Connection Methods

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
                        switch (connection.Context.EventProcessing)
                        {
                            case EventProcessing.epEncrypt:

                                switch (connection.Context.Creator.Context.EncryptType)
                                {
                                    case EncryptType.etRijndael:

                                        if (connection.Context.Host.Context.HostType == HostType.htClient)
                                        {
                                            #region Client

                                            //----- Create authenticate message
                                            AuthMessage am = new AuthMessage();

                                            //----- Generate client asymmetric key pair (public and private)
                                            RSACryptoServiceProvider clientKeyPair = new RSACryptoServiceProvider(2048);

                                            //----- Get the server public key
                                            RSACryptoServiceProvider serverPublicKey;
                                            connection.Context.Creator.Context.CryptoService.OnSymmetricAuthenticate(connection, out serverPublicKey);

                                            //----- Generates symmetric algoritm
                                            SymmetricAlgorithm sa = CryptUtils.CreateSymmetricAlgoritm(connection.Context.Creator.Context.EncryptType);

                                            socketSecurityProvider = new SocketRSACryptoProvider(connection, am, new byte[] { });

                                            //----- Adjust connection cryptors
                                            connection.Context.Encryptor = sa.CreateEncryptor();
                                            connection.Context.Decryptor = sa.CreateDecryptor();


                                            

                                            //----- Encrypt session IV and session Key with server public key
                                            am.SessionIV = serverPublicKey.Encrypt(sa.IV, true);
                                            am.SessionKey = serverPublicKey.Encrypt(sa.Key, true);

                                            //----- Encrypt client public key with symmetric algorithm
                                            am.ClientKey = CryptUtils.EncryptDataForAuthenticate(connection.Context.Encryptor, Encoding.UTF8.GetBytes(clientKeyPair.ToXmlString(false)));

                                            //----- Create hash salt!
                                            am.Data = new byte[32];
                                            RNGCryptoServiceProvider.Create().GetBytes(am.Data);

                                            MemoryStream m = new MemoryStream();

                                            //----- Create a sign with am.SourceKey, am.SessionKey and am.Data (salt)!
                                            m.Write(am.SessionKey, 0, am.SessionKey.Length);
                                            m.Write(am.ClientKey, 0, am.ClientKey.Length);
                                            m.Write(am.Data, 0, am.Data.Length);

                                            am.Sign = clientKeyPair.SignData(CryptUtils.EncryptDataForAuthenticate(connection.Context.Encryptor, m.ToArray()), "SHA256");

                                            //----- Serialize authentication message
                                            m.SetLength(0);
                                            new BinaryFormatter().Serialize(m, am);

                                            connection.BeginSend(m.ToArray());

                                            m.Close();

                                            am.SessionIV.Initialize();
                                            am.SessionKey.Initialize();

                                            serverPublicKey.Clear();
                                            clientKeyPair.Clear();

                                            #endregion Client
                                        }
                                        else
                                        {
                                            #region Server

                                            connection.BeginReceive();

                                            #endregion Server
                                        }

                                        break;

                                    case EncryptType.etSSL:

                                        if (connection.Context.Host.Context.HostType == HostType.htClient)
                                        {
                                            #region Client

                                            //----- Get SSL items
                                            X509Certificate2Collection certs = null;
                                            string serverName = null;
                                            bool checkRevocation = true;

                                            connection.Context.Creator.Context.CryptoService.OnSSLClientAuthenticate(connection, out serverName, ref certs, ref checkRevocation);

                                            //----- Authenticate SSL!
                                            SslStream ssl = new SslStream(new NetworkStream(connection.Context.SocketHandle), true, new RemoteCertificateValidationCallback(connection.Context.Creator.ValidateServerCertificateCallback));

                                            if (certs == null)
                                            {
                                                ssl.BeginAuthenticateAsClient(serverName, new AsyncCallback(SslAuthenticateCallback), new AuthenticateCallbackData(connection, ssl, HostType.htClient));
                                            }
                                            else
                                            {
                                                ssl.BeginAuthenticateAsClient(serverName, certs, System.Security.Authentication.SslProtocols.Tls, checkRevocation, new AsyncCallback(SslAuthenticateCallback), new AuthenticateCallbackData(connection, ssl, HostType.htClient));
                                            }

                                            #endregion Client
                                        }
                                        else
                                        {
                                            #region Server

                                            //----- Get SSL items!
                                            X509Certificate2 cert = null;
                                            bool clientAuthenticate = false;
                                            bool checkRevocation = true;

                                            connection.Context.Creator.Context.CryptoService.OnSSLServerAuthenticate(connection, out cert, out clientAuthenticate, ref checkRevocation);

                                            //----- Authneticate SSL!
                                            SslStream ssl = new SslStream(new NetworkStream(connection.Context.SocketHandle));
                                            ssl.BeginAuthenticateAsServer(cert, clientAuthenticate, System.Security.Authentication.SslProtocols.Default, checkRevocation, new AsyncCallback(SslAuthenticateCallback), new AuthenticateCallbackData(connection, ssl, HostType.htServer));

                                            #endregion Server
                                        }

                                        break;
                                }

                                break;

                            case EventProcessing.epProxy:

                                ProxyInfo proxyInfo = ((SocketConnector)connection.Context.Creator).ProxyInfo;
                                IPEndPoint endPoint = ((SocketConnector)connection.Context.Creator).Context.RemotEndPoint;
                                byte[] proxyBuffer = ProxyUtils.GetProxyRequestData(proxyInfo, endPoint);

                                connection.BeginSend(proxyBuffer);

                                break;
                        }
                    }
                }
                catch (Exception ex)
                {
                    FireOnException(connection, ex);
                }
            }
        }

        #endregion OnConnected

        #region OnSent

        internal void OnSent(BaseSocketConnection connection)
        {
            if (!Disposed)
            {
                if (connection.Active)
                {
                    try
                    {
                        switch (connection.Context.EventProcessing)
                        {
                            case EventProcessing.epEncrypt:

                                if (connection.Context.Host.Context.HostType == HostType.htServer)
                                {
                                    connection.Context.EventProcessing = EventProcessing.epUser;
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

        #endregion OnSent

        #region OnReceived

        internal void OnReceived(BaseSocketConnection connection, byte[] buffer)
        {
            if (!Disposed)
            {
                if (connection.Active)
                {
                    try
                    {
                        switch (connection.Context.EventProcessing)
                        {
                            case EventProcessing.epEncrypt:

                                if (connection.Context.Host.Context.HostType == HostType.htServer)
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
                                        am = (AuthMessage)b.Deserialize(m);
                                    }
                                    catch
                                    {
                                        am = null;
                                    }

                                    if (am != null)
                                    {
                                        socketSecurityProvider = new SocketRSACryptoProvider(connection, am, buffer);

                                        //----- Adjust connection cryptors
                                        connection.Context.Encryptor = socketSecurityProvider.CreateEncryptor();
                                        connection.Context.Decryptor = socketSecurityProvider.CreateDecryptor();

                                        if (socketSecurityProvider.Verify())
                                        {
                                            am.Data = new byte[32];
                                            RNGCryptoServiceProvider.Create().GetBytes(am.Data);

                                            am.SessionIV = null;
                                            am.SessionKey = null;
                                            am.ClientKey = null;
                                            am.Sign = socketSecurityProvider.ServerPrivateKey.SignData(am.Data, "SHA256");

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

                                        socketSecurityProvider.ServerPrivateKey.Clear();
                                        //clientPublicKey.Clear();
                                    }
                                    else
                                    {
                                        FireOnException(connection, new SymmetricAuthenticationException("Symmetric sign error."));
                                    }

                                    #endregion Server
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
                                        connection.Context.Creator.Context.CryptoService.OnSymmetricAuthenticate(connection, out serverPublicKey);

                                        //----- Verify sign
                                        if (serverPublicKey.VerifyData(am.Data, "SHA256", am.Sign))
                                        {
                                            connection.Context.EventProcessing = EventProcessing.epUser;
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

                                    #endregion Client
                                }

                                break;

                            case EventProcessing.epProxy:

                                ProxyInfo proxyInfo = ((SocketConnector)connection.Context.Creator).ProxyInfo;
                                ProxyUtils.GetProxyResponseStatus(proxyInfo, buffer);

                                if (proxyInfo.Completed)
                                {
                                    InitializeConnection(connection);
                                }
                                else
                                {
                                    IPEndPoint endPoint = ((SocketConnector)connection.Context.Creator).Context.RemotEndPoint;
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

        #endregion OnReceived

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
                        connection.Context.Stream = stream;

                        if (completed)
                        {
                            connection.Context.EventProcessing = EventProcessing.epUser;
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

        #endregion SslAuthenticateCallback

        #endregion EventProcessing Methods

        #region Properties

        public SocketHostContext Context { get; set; }

        protected Timer CheckTimeOutTimer
        {
            get { return CheckTimeOutTimer; }
        }

        public bool Active
        {
            get
            {
                if (Disposed)
                {
                    return false;
                }

                lock (Context.SyncActive)
                {
                    return Context.Active;
                }
            }

            internal set
            {
                lock (Context.SyncActive)
                {
                    Context.Active = value;
                }
            }
        }

        #endregion Properties
    }
}