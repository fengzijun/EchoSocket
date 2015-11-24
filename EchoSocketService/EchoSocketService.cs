using System;
using System.Threading;
using System.Text;
using System.Security.Cryptography;
using System.Net.Sockets;
using System.Diagnostics;

using ALAZ.SystemEx.NetEx.SocketsEx;
using ALAZ.SystemEx.ThreadingEx;

namespace EchoSocketService
{
 
    #region Delegates

    public delegate void OnEventDelegate(string eventMessage);

    #endregion

    public class EchoSocketService : BaseSocketService
    {

        #region Fields

        private OnEventDelegate FOnEventDelegate;

        #endregion

        #region Constructor

        public EchoSocketService()
        {
            FOnEventDelegate = null;
        }

        public EchoSocketService(OnEventDelegate eventDelegate)
        {
            FOnEventDelegate = eventDelegate;
        }

        #endregion

        #region Methods

        #region Event
        
        private void Event(string eventMessage)
        {

            if (FOnEventDelegate != null)
            {
                FOnEventDelegate(eventMessage);
            }

        }
        
        #endregion

        #region SleepRandom
        
        public void SleepRandom(HostType hostType)
        {
            Random r = new Random(DateTime.Now.Millisecond);
            
            if (hostType == HostType.htServer)
            {
                ThreadEx.SleepEx( r.Next(1000, 2000) );
            }
            else
            {
                ThreadEx.SleepEx( r.Next(10000, 15000));
            }
            
        }
        
        #endregion

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
        
        #endregion

        #region OnConnected
        
        public override void OnConnected(ConnectionEventArgs e)
        {

            StringBuilder s = new StringBuilder();

            s.Append("------------------------------------------------" + "\r\n");
            s.Append("Connected - " + e.Connection.ConnectionId + "\r\n");
            s.Append(e.Connection.Host.HostType.ToString() + "\r\n");
            s.Append(e.Connection.Creator.Name + "\r\n");
            s.Append(e.Connection.Creator.EncryptType.ToString() + "\r\n");
            s.Append(e.Connection.Creator.CompressionType.ToString() + "\r\n");
            s.Append("------------------------------------------------" + "\r\n");

            Event(s.ToString());

            s.Length = 0;

            Thread.Sleep(123);

            if (e.Connection.Host.HostType == HostType.htServer)
            {
                e.Connection.BeginReceive();
            }
            else
            {
                byte[] b = GetMessage(e.Connection.SocketHandle.ToInt32());
                e.Connection.BeginSend(b);
            }

            
        }
        
        #endregion

        #region OnSent
        
        public override void OnSent(MessageEventArgs e)
        {

            if (!e.SentByServer)
            {
                StringBuilder s = new StringBuilder();

                s.Append("------------------------------------------------" + "\r\n");
                s.Append("Sent - " + e.Connection.ConnectionId + "\r\n");
                s.Append("Sent Bytes - " + e.Connection.WriteBytes.ToString() + "\r\n");
                s.Append("------------------------------------------------" + "\r\n");

                Event(s.ToString().Trim());

                s.Length = 0;

            }


            if (e.Connection.Host.HostType == HostType.htServer)
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
        
        #endregion

        #region OnReceived
        
        public override void OnReceived(MessageEventArgs e)
        {

            StringBuilder s = new StringBuilder();

            s.Append("------------------------------------------------" + "\r\n");
            s.Append("Received - " + e.Connection.ConnectionId + "\r\n");
            s.Append("Received Bytes - " + e.Connection.ReadBytes.ToString() + "\r\n");
            s.Append("------------------------------------------------" + "\r\n");

            Event(s.ToString());
            s.Length = 0;

            SleepRandom(e.Connection.Host.HostType);

            if (e.Connection.Host.HostType == HostType.htServer)
            {
                e.Connection.BeginSend(e.Buffer);
            }
            else
            {
                byte[] b = GetMessage(e.Connection.SocketHandle.ToInt32());
                e.Connection.BeginSend(b);
            }

        }
        
        #endregion

        #region OnDisconnected
        
        public override void OnDisconnected(ConnectionEventArgs e)
        {

            StringBuilder s = new StringBuilder();

            s.Append("------------------------------------------------" + "\r\n");
            s.Append("Disconnected - " + e.Connection.ConnectionId + "\r\n");
            s.Append("------------------------------------------------" + "\r\n");

            Event(s.ToString());
            s.Length = 0;

            if (e.Connection.Host.HostType == HostType.htServer)
            {
                //------
            }
            else
            {
                e.Connection.AsClientConnection().BeginReconnect();
            }

        }
        
        #endregion

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
                s.Append("Creator - " + ((ReconnectAttemptException)e.Exception).Creator.Name + "\r\n");
                s.Append("Creator - " + ((ReconnectAttemptException)e.Exception).Creator.EncryptType.ToString() + "\r\n");
                s.Append("Creator - " + ((ReconnectAttemptException)e.Exception).Creator.CompressionType.ToString() + "\r\n");

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
        
        #endregion

        #endregion

    }

}
