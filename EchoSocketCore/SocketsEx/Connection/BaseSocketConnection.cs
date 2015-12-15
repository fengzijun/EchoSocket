using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Reflection;
using System.Threading;
using EchoSocketCore.SocketsEx.Model;

namespace EchoSocketCore.SocketsEx
{
    /// <summary>
    /// connection data
    /// </summary>
    public abstract class BaseSocketConnection : BaseDisposable, ISocketConnection
    {
        private SocketAsyncEventArgs FWriteOV;

        private SocketAsyncEventArgs FReadOV;

        private SocketContext context;

        private long connectionId;

        public long ConnectionId { 
            get { return connectionId; }
            set { connectionId = value; }
        }


        internal SocketAsyncEventArgs WriteOV
        {
            get { return FWriteOV; }
        }

        internal SocketAsyncEventArgs ReadOV
        {
            get { return FReadOV; }
        }

        internal bool Active
        {
            get
            {
                if (Disposed)
                {
                    return false;
                }

                lock (context.SyncActive)
                {
                    return context.Active;
                }
            }

            set
            {
                lock (context.SyncActive)
                {
                    context.Active = value;
                }
            }
        }

        public SocketContext Context
        {
            get { return context; }
            set { context = value; }
        }


        public BaseSocketConnection(SocketContext context)
        {
            this.context = context;
            this.connectionId = IdProvider.GetConnectionId();
            FWriteOV = new SocketAsyncEventArgs();
            FReadOV = new SocketAsyncEventArgs();
        }



        public override void Free(bool canAccessFinalizable)
        {
            if (FReadOV != null)
            {
                Type t = typeof(SocketAsyncEventArgs);

                FieldInfo f = t.GetField("m_Completed", BindingFlags.Instance | BindingFlags.NonPublic);
                f.SetValue(FReadOV, null);

                //FReadOV.SetBuffer(null, 0, 0);
                FReadOV.Dispose();
                FReadOV = null;
            }

            if (FWriteOV != null)
            {
                Type t = typeof(SocketAsyncEventArgs);

                FieldInfo f = t.GetField("m_Completed", BindingFlags.Instance | BindingFlags.NonPublic);
                f.SetValue(FWriteOV, null);

                //FWriteOV.SetBuffer(null, 0, 0);

                FWriteOV.Dispose();
                FWriteOV = null;
            }

       
            Context.Free(canAccessFinalizable);

            base.Free(canAccessFinalizable);
        }

        internal void SetConnectionData(int readBytes, int writeBytes)
        {
            if (!Disposed)
            {
                lock (Context.SyncData)
                {
                    if (readBytes > 0)
                    {
                        Context.ReadBytes += readBytes;
                    }

                    if (writeBytes > 0)
                    {
                        Context.WriteBytes += writeBytes;
                    }

                    Context.LastAction = DateTime.Now;
                }
            }
        }

        public void SetTTL(short value)
        {
            context.SocketHandle.Ttl = value;
        }

        public void SetLinger(LingerOption lo)
        {
            context.SocketHandle.LingerState = lo;
        }

        public void SetNagle(bool value)
        {
            context.SocketHandle.NoDelay = value;
        }

        public abstract IClientSocketConnection AsClientConnection();

        public abstract IServerSocketConnection AsServerConnection();

        public void BeginSend(byte[] buffer)
        {
            if (!Disposed)
            {
                context.Host.BeginSend(this, buffer, false);
            }
        }

        public void BeginReceive()
        {
            if (!Disposed)
            {
                context.Host.BeginReceive(this);
            }
        }

        public void BeginDisconnect()
        {
            if (!Disposed)
            {
                context.Host.BeginDisconnect(this);
            }
        }

        public ISocketConnection[] GetConnections()
        {
            if (!Disposed)
            {
                return context.Host.GetConnections();
            }
            else
            {
                return null;
            }
        }

        public ISocketConnection GetConnectionById(long id)
        {
            if (!Disposed)
            {
                return context.Host.GetSocketConnectionById(id);
            }
            else
            {
                return null;
            }
        }

      

   
    }
}