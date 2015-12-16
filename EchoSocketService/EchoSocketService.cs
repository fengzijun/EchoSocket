using System;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using EchoSocketCore.SocketsEx;
using EchoSocketCore.ThreadingEx;

namespace EchoSocketService
{
    #region Delegates

    public delegate void OnEventDelegate(string eventMessage);

    #endregion Delegates

    public class EchoSocketService : BaseSocketService
    {
        #region Fields

        private OnEventDelegate FOnEventDelegate;

        #endregion Fields

        #region Constructor

        public EchoSocketService()
        {
            FOnEventDelegate = null;
        }

        public EchoSocketService(OnEventDelegate eventDelegate)
        {
            FOnEventDelegate = eventDelegate;
        }

        #endregion Constructor

        #region Methods

        #region Event

        private void Event(string eventMessage)
        {
            if (FOnEventDelegate != null)
            {
                FOnEventDelegate(eventMessage);
            }
        }

        #endregion Event

        #region SleepRandom

        public void SleepRandom(HostType hostType)
        {
            Random r = new Random(DateTime.Now.Millisecond);

            if (hostType == HostType.htServer)
            {
                ThreadEx.SleepEx(r.Next(1000, 2000));
            }
            else
            {
                ThreadEx.SleepEx(r.Next(10000, 15000));
            }
        }

        #endregion SleepRandom

        #region GetMessage

        public byte[] GetMessage(int handle)
        {
            Random r = new Random(handle + DateTime.Now.Millisecond);

            byte[] message = new byte[r.Next(246, 1000)];

            for (int i = 0; i < message.Length; i++)
            {
                message[i] = (byte)r.Next(32, 122);
            }

            return message;
        }

        #endregion GetMessage

        #region OnConnected

        public override void OnConnected(ConnectionEventArgs e)
        {
            StringBuilder s = new StringBuilder();

            s.Append("------------------------------------------------" + "\r\n");
            s.Append("Connected - " + e.Connection.Context.ConnectionId + "\r\n");
            s.Append(e.Connection.Context.Host.Context.HostType.ToString() + "\r\n");
            s.Append(e.Connection.Context.Creator.Context.Name + "\r\n");
            s.Append(e.Connection.Context.Creator.Context.EncryptType.ToString() + "\r\n");
            s.Append(e.Connection.Context.Creator.Context.CompressionType.ToString() + "\r\n");
            s.Append("------------------------------------------------" + "\r\n");

            Event(s.ToString());

            s.Length = 0;

            Thread.Sleep(123);

            if (e.Connection.Context.Host.Context.HostType == HostType.htServer)
            {
                e.Connection.BeginReceive();
            }
            else
            {
                byte[] b = GetMessage(e.Connection.Context.SocketHandle.Handle.ToInt32());
                e.Connection.BeginSend(b);
            }
        }

        #endregion OnConnected

        #region OnSent

        public override void OnSent(MessageEventArgs e)
        {
            if (!e.SentByServer)
            {
                StringBuilder s = new StringBuilder();

                s.Append("------------------------------------------------" + "\r\n");
                s.Append("Sent - " + e.Connection.Context.ConnectionId + "\r\n");
                s.Append("Sent Bytes - " + e.Connection.Context.WriteBytes.ToString() + "\r\n");
                s.Append("------------------------------------------------" + "\r\n");

                Event(s.ToString().Trim());

                s.Length = 0;
            }

            if (e.Connection.Context.Host.Context.HostType == HostType.htServer)
            {
                if (!e.SentByServer)
                {
                    e.Connection.BeginReceive();
                }
            }
            else
            {
                e.Connection.BeginReceive();
            }
        }

        #endregion OnSent

        #region OnReceived

        public override void OnReceived(MessageEventArgs e)
        {
            StringBuilder s = new StringBuilder();

            s.Append("------------------------------------------------" + "\r\n");
            s.Append("Received - " + e.Connection.Context.ConnectionId + "\r\n");
            s.Append("Received Bytes - " + e.Connection.Context.ReadBytes.ToString() + "\r\n");
            s.Append("------------------------------------------------" + "\r\n");

            Event(s.ToString());
            s.Length = 0;

            SleepRandom(e.Connection.Context.Host.Context.HostType);

            if (e.Connection.Context.Host.Context.HostType == HostType.htServer)
            {
                e.Connection.BeginSend(e.Buffer);
            }
            else
            {
                byte[] b = GetMessage(e.Connection.Context.SocketHandle.Handle.ToInt32());
                e.Connection.BeginSend(b);
            }
        }

        #endregion OnReceived

        #region OnDisconnected

        public override void OnDisconnected(ConnectionEventArgs e)
        {
            StringBuilder s = new StringBuilder();

            s.Append("------------------------------------------------" + "\r\n");
            s.Append("Disconnected - " + e.Connection.Context.ConnectionId + "\r\n");
            s.Append("------------------------------------------------" + "\r\n");

            Event(s.ToString());
            s.Length = 0;

            if (e.Connection.Context.Host.Context.HostType == HostType.htServer)
            {
                //------
            }
            else
            {
                //e.Connection.AsClientConnection().BeginReconnect();
            }
        }

        #endregion OnDisconnected

        #region OnException

        public override void OnException(ExceptionEventArgs e)
        {
            StringBuilder s = new StringBuilder();

            s.Append("------------------------------------------------" + "\r\n");
            s.Append("Exception - " + e.Exception.GetType().ToString() + "\r\n");
            s.Append("Exception Message - " + e.Exception.Message + "\r\n");

            if (e.Exception is ReconnectAttemptException)
            {
                s.Append("Attempted   - " + ((ReconnectAttemptException)e.Exception).Attempt.ToString() + "\r\n");
                s.Append("Max Reached - " + ((ReconnectAttemptException)e.Exception).MaxReached.ToString() + "\r\n");
                s.Append("------------------------------------------------" + "\r\n");
                s.Append("Inner Error - " + ((SocketException)e.Exception.InnerException).ErrorCode.ToString() + "\r\n");
                s.Append("------------------------------------------------" + "\r\n");
                s.Append("Creator - " + ((ReconnectAttemptException)e.Exception).Creator.Context.Name + "\r\n");
                s.Append("Creator - " + ((ReconnectAttemptException)e.Exception).Creator.Context.EncryptType.ToString() + "\r\n");
                s.Append("Creator - " + ((ReconnectAttemptException)e.Exception).Creator.Context.CompressionType.ToString() + "\r\n");
            }

            if (e.Exception is SocketException)
            {
                s.Append("Socket Error - " + ((SocketException)e.Exception).ErrorCode.ToString() + "\r\n");
            }

            s.Append("------------------------------------------------" + "\r\n");

            Event(s.ToString());
            s.Length = 0;

            if (e.Connection != null)
            {
                e.Connection.BeginDisconnect();
            }
        }

        #endregion OnException

        #endregion Methods
    }
}