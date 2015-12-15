using System;
using System.IO;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.ServiceModel.Channels;
using System.Threading;
using EchoSocketCore.ThreadingEx;

namespace EchoSocketCore.SocketsEx
{
    /// <summary>
    /// The connection host.
    /// </summary>
    public abstract class BaseSocketProvider : BaseDisposable, ISocket
    {
        private ReaderWriterLockSlim fSocketConnectionsSync;

        private ManualResetEvent fWaitCreatorsDisposing;

        private ManualResetEvent fWaitConnectionsDisposing;

        private ManualResetEvent fWaitThreadsDisposing;

        public ManualResetEvent FWaitCreatorsDisposing
        {
            get { return fWaitCreatorsDisposing; }
            set { fWaitConnectionsDisposing = value; }
        }

        public ReaderWriterLockSlim FSocketConnectionsSync
        {
            get { return fSocketConnectionsSync; }
            set { fSocketConnectionsSync = value; }
        }

        private Timer fIdleTimer;

        private SocketContext context;

        public SocketContext Context
        {
            get { return context; }
            set { context = value; }
        }

        protected Timer CheckTimeOutTimer { 
            get { return fIdleTimer; } 
            set { fIdleTimer = value; }
        }

        public bool Active
        {
            get
            {
                if (Disposed)
                {
                    return false;
                }

                lock (context.SyncActive)
                {
                    return Context.Active;
                }
            }

            internal set
            {
                lock (context.SyncActive)
                {
                    context.Active = value;
                }
            }
        }

   
        public BaseSocketProvider(HostType hostType, CallbackThreadType callbackThreadtype, ISocketService socketService, DelimiterType delimiterType, byte[] delimiter, int socketBufferSize, int messageBufferSize, int idleCheckInterval, int idleTimeOutValue)
        {
            context = new SocketContext
            {
                BufferManager = BufferManager.CreateBufferManager(0, messageBufferSize),
                SocketService = socketService,
                IdleCheckInterval = idleCheckInterval,
                IdleTimeOutValue = idleTimeOutValue,
                CallbackThreadType = callbackThreadtype,
                DelimiterUserType = delimiterType,
                DelimiterUserEncrypt = delimiter,
                MessageBufferSize = messageBufferSize,
                SocketBufferSize = socketBufferSize,
                HostType = hostType
            };

            //context.ConnectionId = context.GenerateConnectionId();

            fSocketConnectionsSync = new ReaderWriterLockSlim();
            fWaitCreatorsDisposing = new ManualResetEvent(false);
            fWaitConnectionsDisposing = new ManualResetEvent(false);
            fWaitThreadsDisposing = new ManualResetEvent(false);
        }

        public BaseSocketProvider(HostType hostType, ISocketService socketService, DelimiterType delimiterType, byte[] delimiter, int socketBufferSize, int messageBufferSize, int idleCheckInterval, int idleTimeOutValue) :
            this(hostType, CallbackThreadType.ctWorkerThread, socketService, delimiterType, delimiter, socketBufferSize, messageBufferSize, idleCheckInterval, idleTimeOutValue)
        {
        }

        public BaseSocketProvider(HostType hostType, ISocketService socketService, DelimiterType delimiterType, byte[] delimiter, int idleCheckInterval, int idleTimeOutValue)
            : this(hostType, CallbackThreadType.ctWorkerThread, socketService, delimiterType, delimiter, 1024, 1024, idleCheckInterval, idleTimeOutValue)
        {
        }

        public BaseSocketProvider(HostType hostType, ISocketService socketService, DelimiterType delimiterType, byte[] delimiter) :
            this(hostType, CallbackThreadType.ctWorkerThread, socketService, delimiterType, delimiter, 1024, 1024, 1000, 1000)
        {
        }

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

        /// <summary>
        /// Starts the base host.
        /// </summary>
        public void Start()
        {
            if (Disposed)
                return;

            int loopSleep = 0;

            foreach (BaseSocketConnectionCreator creator in context.SocketCreators)
            {
                creator.Start();
                ThreadEx.LoopSleep(ref loopSleep);
            }

            if ((context.IdleCheckInterval > 0) && (context.IdleTimeOutValue > 0))
            {
                fIdleTimer = new Timer(new TimerCallback(CheckSocketConnections));
            }

            if (fIdleTimer != null)
            {
                fIdleTimer.Change(context.IdleCheckInterval, context.IdleCheckInterval);
            }

            Active = true;
        }

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

        /// <summary>
        /// Stop the host creators.
        /// </summary>
        protected void StopCreators()
        {
            if (Disposed)
                return;

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
                        //RemoveCreator(creator);
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

        internal virtual void AddSocketConnection(BaseSocketConnection socketConnection)
        {
            if (Disposed)
                return;

            fSocketConnectionsSync.EnterWriteLock();

            try
            {
                Context.SocketConnections.Add(socketConnection.ConnectionId, socketConnection);

                socketConnection.WriteOV.Completed += new EventHandler<SocketAsyncEventArgs>(BeginSendCallbackAsync);
                socketConnection.ReadOV.Completed += new EventHandler<SocketAsyncEventArgs>(BeginReadCallbackAsync);
            }
            finally
            {
                FSocketConnectionsSync.ExitWriteLock();
            }
        }

        internal virtual void RemoveSocketConnection(BaseSocketConnection socketConnection)
        {
            if (Disposed || this == null)
                return;

            fSocketConnectionsSync.EnterWriteLock();
            var socketConnections = context.SocketConnections;

            try
            {
                socketConnections.Remove(socketConnection.ConnectionId);
            }
            finally
            {
                if (socketConnections.Count <= 0)
                {
                    fWaitConnectionsDisposing.Set();
                }

                fSocketConnectionsSync.ExitWriteLock();
            }
        }

        internal virtual void DisposeConnection(BaseSocketConnection socketConnection)
        {
            if (Disposed || this == null)
                return;

            if (socketConnection.WriteOV != null)
            {
                if (socketConnection.WriteOV.Buffer != null)
                {
                    context.BufferManager.ReturnBuffer(socketConnection.WriteOV.Buffer);
                }
            }

            if (socketConnection.ReadOV != null)
            {
                if (socketConnection.ReadOV.Buffer != null)
                {
                    context.BufferManager.ReturnBuffer(socketConnection.ReadOV.Buffer);
                }
            }

            Dispose();
        }

        internal virtual void CloseConnection(BaseSocketConnection socketConnection)
        {
            if (Disposed)
                return;

            Active = false;
            socketConnection.Context.SocketHandle.Shutdown(SocketShutdown.Send);

            lock (socketConnection.Context.WriteQueue)
            {
                if (socketConnection.Context.WriteQueue.Count > 0)
                {
                    for (int i = 1; i <= socketConnection.Context.WriteQueue.Count; i++)
                    {
                        MessageBuffer message = socketConnection.Context.WriteQueue.Dequeue();

                        if (message != null)
                        {
                            context.BufferManager.ReturnBuffer(message.Buffer);
                        }
                    }
                }
            }
        }

        protected void StopConnections()
        {
            if (Disposed)
                return;

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

        internal BaseSocketConnection[] GetSocketConnections()
        {
            BaseSocketConnection[] items = null;

            if (Disposed)
                return items;

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

            return items;
        }

        internal BaseSocketConnection GetSocketConnectionById(long connectionId)
        {
            BaseSocketConnection item = null;

            if (Disposed)
                return item;

            fSocketConnectionsSync.EnterReadLock();

            try
            {
                item = Context.SocketConnections[connectionId];
            }
            finally
            {
                fSocketConnectionsSync.ExitReadLock();
            }

            return item;
        }

        private void CheckSocketConnections(Object stateInfo)
        {
            if (Disposed)
                return;

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

        protected void AddCreator(BaseSocketConnectionCreator creator)
        {
            if (!Disposed)
            {
                lock (Context.SocketCreators)
                {
                    context.SocketCreators.Add(creator);
                }
            }
        }

        protected void RemoveCreator(BaseSocketConnectionCreator creator)
        {
            if (!Disposed)
            {
                lock (context.SocketCreators)
                {
                    context.SocketCreators.Remove(creator);

                    if (context.SocketCreators.Count <= 0)
                    {
                        fWaitCreatorsDisposing.Set();
                    }
                }
            }
        }

        protected BaseSocketConnectionCreator[] GetSocketCreators()
        {
            BaseSocketConnectionCreator[] items = null;

            if (!Disposed)
            {
                lock (context.SocketCreators)
                {
                    items = new BaseSocketConnectionCreator[Context.SocketCreators.Count];
                    context.SocketCreators.CopyTo(items, 0);
                }
            }

            return items;
        }

        internal void FireOnConnected(BaseSocketConnection connection)
        {
            if (Disposed || !connection.Active)
                return;

            try
            {
                switch (connection.Context.EventProcessing)
                {
                    case EventProcessing.epUser:

                        context.SocketService.OnConnected(new ConnectionEventArgs(connection));
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

        private void FireOnSent(BaseSocketConnection connection, bool sentByServer)
        {
            if (Disposed || !connection.Active)
                return;

            try
            {
                switch (connection.Context.EventProcessing)
                {
                    case EventProcessing.epUser:

                        context.SocketService.OnSent(new MessageEventArgs(connection, null, sentByServer));
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

        private void FireOnReceived(BaseSocketConnection connection, byte[] buffer)
        {
            if (Disposed || !connection.Active)
                return;

            try
            {
                switch (connection.Context.EventProcessing)
                {
                    case EventProcessing.epUser:

                        context.SocketService.OnReceived(new MessageEventArgs(connection, buffer, false));
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

        private void FireOnDisconnected(BaseSocketConnection connection)
        {
            if (Disposed)
                return;

            try
            {
                context.SocketService.OnDisconnected(new ConnectionEventArgs(connection));
            }
            finally
            {
            }
        }

        internal void FireOnException(BaseSocketConnection connection, Exception ex)
        {
            if (Disposed)
                return;

            if (connection == null)
            {
                context.SocketService.OnException(new ExceptionEventArgs(connection, ex));
            }
            else
            {
                if (connection.Active)
                {
                    try
                    {
                        context.SocketService.OnException(new ExceptionEventArgs(connection, ex));
                    }
                    finally
                    {
                    }
                }
            }
        }

        /// <summary>
        /// Begin send the data.
        /// </summary>
        internal void BeginSend(BaseSocketConnection connection, byte[] buffer, bool sentByServer)
        {
            if (Disposed || !connection.Active)
                return;

            byte[] sendBuffer = null;

            try
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

        public void BeginSendCallbackSSL(IAsyncResult ar)
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

        public void BeginSendCallbackSSLP(object state)
        {
            if (Disposed)
                return;

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

                if (!connection.Active)
                    return;

                connection.Context.Stream.EndWrite(ar);
                connection.SetConnectionData(0, connection.WriteOV.Count);

                context.BufferManager.ReturnBuffer(connection.WriteOV.Buffer);

                FireOnSent(connection, sentByServer);

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
            catch (Exception ex)
            {
                FireOnException(connection, ex);
            }
        }

        public void BeginSendCallbackAsync(object sender, SocketAsyncEventArgs e)
        {
            switch (context.CallbackThreadType)
            {
                case CallbackThreadType.ctWorkerThread:

                    ThreadPool.QueueUserWorkItem(new WaitCallback(BeginSendCallbackAsyncP), e);
                    break;

                case CallbackThreadType.ctIOThread:

                    BeginSendCallbackAsyncP(e);
                    break;
            }
        }

        public void BeginSendCallbackAsyncP(object state)
        {
            if (Disposed)
                return;

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

                if (!connection.Active)
                    return;

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
                        context.BufferManager.ReturnBuffer(e.Buffer);
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

        /// <summary>
        /// Receive data from connetion.
        /// </summary>
        internal void BeginReceive(BaseSocketConnection connection)
        {
            if (Disposed || !connection.Active)
                return;
            byte[] readMessage = null;

            try
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
                context.BufferManager.ReturnBuffer(readMessage);
            }
        }

        public void BeginReadCallbackSSL(IAsyncResult ar)
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

        public void BeginReadCallbackSSLP(object state)
        {
            if (Disposed)
                return;

            IAsyncResult ar = null;
            BaseSocketConnection connection = null;

            try
            {
                ar = (IAsyncResult)state;
                connection = (BaseSocketConnection)ar.AsyncState;

                if (!connection.Active)
                    return;

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
            catch (Exception ex)
            {
                FireOnException(connection, ex);
            }
        }

        public void BeginReadCallbackAsync(object sender, SocketAsyncEventArgs e)
        {
            switch (context.CallbackThreadType)
            {
                case CallbackThreadType.ctWorkerThread:

                    ThreadPool.QueueUserWorkItem(new WaitCallback(BeginReadCallbackAsyncP), e);
                    break;

                case CallbackThreadType.ctIOThread:

                    BeginReadCallbackAsyncP(e);
                    break;
            }
        }

        public void BeginReadCallbackAsyncP(object state)
        {
            if (Disposed)
                return;

            SocketAsyncEventArgs e = null;
            BaseSocketConnection connection = null;

            try
            {
                e = (SocketAsyncEventArgs)state;
                connection = (BaseSocketConnection)e.UserToken;

                if (!connection.Active)
                    return;

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
                    byte[] readMessage = connection.Context.BufferManager.TakeBuffer(Context.MessageBufferSize);
                    Buffer.BlockCopy(e.Buffer, e.Offset, readMessage, 0, remainingBytes);

                    connection.Context.BufferManager.ReturnBuffer(e.Buffer);
                    e.SetBuffer(null, 0, 0);
                    e.SetBuffer(readMessage, remainingBytes, readMessage.Length - remainingBytes);
                }
            }

            if (!connection.Active)
                return;

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

        public int ReadMessageWithNoDelimiter(BaseSocketConnection connection, SocketAsyncEventArgs e, int readBytes)
        {
            byte[] rawBuffer = null;
            rawBuffer = BufferUtils.GetRawBuffer(connection, e.Buffer, readBytes);

            FireOnReceived(connection, rawBuffer);
            return 0;
        }

        public int ReadMessageWithTail(BaseSocketConnection connection, SocketAsyncEventArgs e, int readBytes, ref bool onePacketFound)
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

        /// <summary>
        /// Begin disconnect the connection
        /// </summary>
        internal void BeginDisconnect(BaseSocketConnection connection)
        {
            if (Disposed && !connection.Active)
                return;

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

        public void BeginDisconnectCallbackAsync(object sender, SocketAsyncEventArgs e)
        {
            if (Disposed)
                return;

            BaseSocketConnection connection = null;

            try
            {
                connection = (BaseSocketConnection)e.UserToken;

                e.Completed -= new EventHandler<SocketAsyncEventArgs>(BeginDisconnectCallbackAsync);
                e.UserToken = null;
                e.Dispose();
                e = null;

                if (!connection.Active)
                    return;

                lock (connection.Context.SyncActive)
                {
                    CloseConnection(connection);
                    FireOnDisconnected(connection);
                }
            }
            finally
            {
                Console.WriteLine(connection.ConnectionId + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss:fff"));
                DisposeConnection(connection);
                RemoveSocketConnection(connection);
                
            }
        }

        internal abstract void BeginReconnect(ClientSocketConnection connection);

        internal abstract void BeginSendToAll(ServerSocketConnection connection, byte[] buffer, bool includeMe);

        internal abstract void BeginSendTo(BaseSocketConnection connectionTo, byte[] buffer);

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

        /// <summary>
        /// Initializes the connection
        /// </summary>
        /// <param name="connection"></param>
        internal void OnConnected(BaseSocketConnection connection)
        {
            if (Disposed || !connection.Active)
                return;

            try
            {
                switch (connection.Context.EventProcessing)
                {
                    case EventProcessing.epEncrypt:

                        switch (connection.Context.EncryptType)
                        {
                            case EncryptType.etRijndael:

                                if (connection.Context.HostType == HostType.htClient)
                                {
                                    ISocketSecurityProvider socketSecurityProvider = new SocketRSACryptoProvider(connection, null);
                                    MemoryStream m = socketSecurityProvider.EcryptForClient();
                                    connection.BeginSend(m.ToArray());
                                }
                                else
                                {
                                    connection.BeginReceive();
                                }

                                break;

                            case EncryptType.etSSL:

                                if (connection.Context.HostType == HostType.htClient)
                                {
                                    //----- Get SSL items
                                    X509Certificate2Collection certs = null;
                                    string serverName = null;
                                    bool checkRevocation = true;

                                    connection.Context.CryptoService.OnSSLClientAuthenticate(connection, out serverName, ref certs, ref checkRevocation);

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
                                }
                                else
                                {
                                    //----- Get SSL items!
                                    X509Certificate2 cert = null;
                                    bool clientAuthenticate = false;
                                    bool checkRevocation = true;

                                    connection.Context.CryptoService.OnSSLServerAuthenticate(connection, out cert, out clientAuthenticate, ref checkRevocation);

                                    //----- Authneticate SSL!
                                    SslStream ssl = new SslStream(new NetworkStream(connection.Context.SocketHandle));
                                    ssl.BeginAuthenticateAsServer(cert, clientAuthenticate, System.Security.Authentication.SslProtocols.Default, checkRevocation, new AsyncCallback(SslAuthenticateCallback), new AuthenticateCallbackData(connection, ssl, HostType.htServer));
                                }

                                break;
                        }

                        break;

                    case EventProcessing.epProxy:

                        ProxyInfo proxyInfo = context.ProxyInfo;
                        IPEndPoint endPoint = context.RemoteEndPoint;
                        byte[] proxyBuffer = ProxyUtils.GetProxyRequestData(proxyInfo, endPoint);

                        connection.BeginSend(proxyBuffer);

                        break;
                }
            }
            catch (Exception ex)
            {
                FireOnException(connection, ex);
            }
        }

        internal void OnSent(BaseSocketConnection connection)
        {
            if (Disposed || connection.Active)
                return;

            try
            {
                switch (connection.Context.EventProcessing)
                {
                    case EventProcessing.epEncrypt:

                        if (connection.Context.HostType == HostType.htServer)
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

        internal void OnReceived(BaseSocketConnection connection, byte[] buffer)
        {
            if (Disposed || !connection.Active)
                return;

            try
            {
                ISocketSecurityProvider socketSecurityProvider = new SocketRSACryptoProvider(connection, buffer);

                switch (connection.Context.EventProcessing)
                {
                    case EventProcessing.epEncrypt:

                        if (connection.Context.HostType == HostType.htServer)
                        {
                            //----- Deserialize authentication message

                            try
                            {
                                MemoryStream m = socketSecurityProvider.DecryptForServer();

                                BeginSend(connection, m.ToArray(), false);
                            }
                            catch (SymmetricAuthenticationException ex)
                            {
                                FireOnException(connection, ex);
                            }
                        }
                        else
                        {
                            //----- Deserialize authentication message
                            try
                            {
                                AuthMessage am = socketSecurityProvider.DecryptForClient();
                            }
                            catch (SymmetricAuthenticationException ex)
                            {
                                FireOnException(connection, ex);
                            }
                        }

                        break;

                    case EventProcessing.epProxy:

                        ProxyInfo proxyInfo = context.ProxyInfo;
                        ProxyUtils.GetProxyResponseStatus(proxyInfo, buffer);

                        if (proxyInfo.Completed)
                        {
                            connection.Context.Host.InitializeConnection(connection);
                        }
                        else
                        {
                            IPEndPoint endPoint = connection.Context.RemoteEndPoint;
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

        public void SslAuthenticateCallback(IAsyncResult ar)
        {
            if (Disposed)
                return;

            BaseSocketConnection connection = null;
            SslStream stream = null;
            bool completed = false;

            try
            {
                AuthenticateCallbackData callbackData = (AuthenticateCallbackData)ar.AsyncState;

                connection = callbackData.Connection;
                stream = callbackData.Stream;

                if (!connection.Active)
                    return;

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
            catch (Exception ex)
            {
                FireOnException(connection, ex);
            }
        }

        internal virtual void InitializeConnection(BaseSocketConnection connnection)
        {
            if (Disposed)
                return;

            switch (context.EventProcessing)
            {
                case EventProcessing.epNone:

                    if (InitializeConnectionProxy(connnection))
                    {
                        context.Host.FireOnConnected(connnection);
                    }
                    else
                    {
                        if (InitializeConnectionEncrypt(connnection))
                        {
                            context.Host.FireOnConnected(connnection);
                        }
                        else
                        {
                            context.EventProcessing = EventProcessing.epUser;
                            context.Host.FireOnConnected(connnection);
                        }
                    }

                    break;

                case EventProcessing.epProxy:

                    if (InitializeConnectionEncrypt(connnection))
                    {
                        context.Host.FireOnConnected(connnection);
                    }
                    else
                    {
                        context.EventProcessing = EventProcessing.epUser;
                        context.Host.FireOnConnected(connnection);
                    }

                    break;

                case EventProcessing.epEncrypt:

                    context.EventProcessing = EventProcessing.epUser;
                    context.Host.FireOnConnected(connnection);

                    break;
            }
        }

        internal virtual bool InitializeConnectionProxy(BaseSocketConnection connnection)
        {
            bool result = false;

            if (Disposed)
                return result;

            if (context.Creator is SocketConnector)
            {
                if (connnection.Context.ProxyInfo != null)
                {
                    context.EventProcessing = EventProcessing.epProxy;
                    result = true;
                }
            }

            return result;
        }

        internal virtual bool InitializeConnectionEncrypt(BaseSocketConnection connnection)
        {
            bool result = false;

            if (Disposed)
                return result;

            ICryptoService cryptService = connnection.Context.CryptoService;

            if ((cryptService != null) && (connnection.Context.EncryptType != EncryptType.etNone))
            {
                context.EventProcessing = EventProcessing.epEncrypt;
                result = true;
            }

            return result;
        }
    }
}