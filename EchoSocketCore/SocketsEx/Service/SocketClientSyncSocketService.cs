using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using EchoSocketCore.SocketsEx;

namespace EchoSocketCore.SocketsEx
{
    public class SocketClientSyncSocketService : BaseSocketService
    {
        #region Fields

        private SocketClientSync FSocketClient;

        #endregion Fields

        #region Constructor

        public SocketClientSyncSocketService(SocketClientSync client)
        {
            FSocketClient = client;
        }

        #endregion Constructor

        #region Methods

        public override void OnConnected(ConnectionEventArgs e)
        {
            FSocketClient.Context.SocketConnection = e.Connection;
            FSocketClient.Context.SocketConnection.BeginReceive();
            FSocketClient.ConnectEvent.Set();
        }

        public override void OnException(ExceptionEventArgs e)
        {
            FSocketClient.LastException = e.Exception;
            FSocketClient.ExceptionEvent.Set();
        }

        public override void OnSent(MessageEventArgs e)
        {
            FSocketClient.SentEvent.Set();
        }

        public override void OnReceived(MessageEventArgs e)
        {
            FSocketClient.Enqueue(Encoding.GetEncoding(1252).GetString(e.Buffer));
            FSocketClient.Context.SocketConnection.BeginReceive();
        }

        public override void OnDisconnected(ConnectionEventArgs e)
        {
            FSocketClient.DisconnectEvent.Set();
        }

        #endregion Methods
    }
}
