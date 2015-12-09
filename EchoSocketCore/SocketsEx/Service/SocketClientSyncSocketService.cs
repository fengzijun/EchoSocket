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

        private SocketClientSync fSocketClient;

        #endregion Fields

        #region Constructor

        public SocketClientSyncSocketService(SocketClientSync client)
        {
            fSocketClient = client;
        }

        #endregion Constructor

        #region Methods

        public override void OnConnected(ConnectionEventArgs e)
        {
            fSocketClient.Context.SocketConnection = e.Connection;
            fSocketClient.Context.SocketConnection.BeginReceive();
            fSocketClient.ConnectEvent.Set();
        }

        public override void OnException(ExceptionEventArgs e)
        {
            fSocketClient.LastException = e.Exception;
            fSocketClient.ExceptionEvent.Set();
        }

        public override void OnSent(MessageEventArgs e)
        {
            fSocketClient.SentEvent.Set();
        }

        public override void OnReceived(MessageEventArgs e)
        {
            fSocketClient.Enqueue(Encoding.GetEncoding(1252).GetString(e.Buffer));
            fSocketClient.Context.SocketConnection.BeginReceive();
        }

        public override void OnDisconnected(ConnectionEventArgs e)
        {
            fSocketClient.DisconnectEvent.Set();
        }

        #endregion Methods
    }
}
