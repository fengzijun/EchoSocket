using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading;
using EchoSocketCore.SocketsEx;

namespace ChatSocketService
{
    public class ChatSocketService : BaseSocketService
    {
        #region Fields

        private ReaderWriterLock FUsersSync;
        private Dictionary<long, ISocketConnection> FUsers;

        #endregion Fields

        #region Constructor

        public ChatSocketService()
        {
            FUsersSync = new ReaderWriterLock();
            FUsers = new Dictionary<long, ISocketConnection>(100);
        }

        #endregion Constructor

        #region Methods

        #region Utils

        #region SerializeMessage

        public static byte[] SerializeMessage(ChatMessage msg)
        {
            using (MemoryStream m = new MemoryStream())
            {
                BinaryFormatter bin = new BinaryFormatter();
                bin.Serialize(m, msg);

                return m.ToArray();
            }
        }

        #endregion SerializeMessage

        #region DeserializeMessage

        public static ChatMessage DeserializeMessage(byte[] buffer)
        {
            using (MemoryStream m = new MemoryStream())
            {
                m.Write(buffer, 0, buffer.Length);
                m.Position = 0;

                BinaryFormatter bin = new BinaryFormatter();

                return (ChatMessage)bin.Deserialize(m);
            }
        }

        #endregion DeserializeMessage

        #endregion Utils

        #region OnConnected

        public override void OnConnected(ConnectionEventArgs e)
        {
            StringBuilder s = new StringBuilder();

            s.Append("\r\n------------------------------------------------\r\n");
            s.Append("New Client\r\n");
            s.Append(" Connection Id " + e.Connection.Context.ConnectionId + "\r\n");
            s.Append(" Ip Address " + e.Connection.Context.RemoteEndPoint.Address + "\r\n");
            s.Append(" Tcp Port " + e.Connection.Context.RemoteEndPoint.Port + "\r\n");

            Console.WriteLine(s.ToString());
            s.Length = 0;

            e.Connection.Context.UserData = new ConnectionData(ConnectionState.csConnected);
            e.Connection.BeginReceive();
        }

        #endregion OnConnected

        #region OnSent

        public override void OnSent(MessageEventArgs e)
        {
            //if (!e.SentByServer)
            //{
            //    e.Connection.BeginReceive();
            //}
        }

        #endregion OnSent

        #region OnReceived

        public override void OnReceived(MessageEventArgs e)
        {
            ChatMessage msg = DeserializeMessage(e.Buffer);

            switch (msg.MessageType)
            {
                case MessageType.mtLogin:

                    ((ConnectionData)e.Connection.Context.UserData).ConnectionState = ConnectionState.csAuthenticated;
                    ((ConnectionData)e.Connection.Context.UserData).UserName = msg.UserInfo[0].UserName;

                    msg.UserInfo[0].UserId = e.Connection.Context.ConnectionId;
                    e.Connection.BeginSend(SerializeMessage(msg));

                    msg.MessageType = MessageType.mtAuthenticated;
                    e.Connection.AsServerConnection().BeginSendToAll(SerializeMessage(msg), false);

                    ISocketConnection[] cnns = e.Connection.AsServerConnection().GetConnections();

                    if ((cnns != null) && (cnns.Length > 0))
                    {
                        bool send = false;

                        msg.MessageType = MessageType.mtHello;
                        msg.UserInfo = new UserInfo[cnns.Length];

                        for (int i = 0; i < cnns.Length; i++)
                        {
                            if (cnns[i] != e.Connection)
                            {
                                msg.UserInfo[i].UserName = ((ConnectionData)cnns[i].Context.UserData).UserName;
                                msg.UserInfo[i].UserId = cnns[i].Context.ConnectionId;
                                send = true;
                            }
                        }

                        if (send)
                        {
                            e.Connection.AsServerConnection().BeginSend(SerializeMessage(msg));
                        }
                    }

                    break;

                case MessageType.mtMessage:

                    e.Connection.AsServerConnection().BeginSendToAll(e.Buffer, false);

                    break;

                case MessageType.mtLogout:

                    e.Connection.AsServerConnection().BeginSendToAll(SerializeMessage(msg), false);
                    break;
            }

            e.Connection.BeginReceive();
        }

        #endregion OnReceived

        #region OnDisconnected

        public override void OnDisconnected(ConnectionEventArgs e)
        {
            StringBuilder s = new StringBuilder();

            s.Append("------------------------------------------------" + "\r\n");
            s.Append("Client Disconnected\r\n");
            s.Append(" Connection Id " + e.Connection.Context.ConnectionId + "\r\n");

            e.Connection.Context.UserData = null;

            Console.WriteLine(s.ToString());

            s.Length = 0;
        }

        #endregion OnDisconnected

        #region OnException

        public override void OnException(ExceptionEventArgs e)
        {
            e.Connection.BeginDisconnect();
        }

        #endregion OnException

        #endregion Methods
    }

    [Serializable]
    public struct UserInfo
    {
        #region Fields

        private string FUserName;
        private long FUserId;

        #endregion Fields

        #region Properties

        public string UserName
        {
            get { return FUserName; }
            set { FUserName = value; }
        }

        public long UserId
        {
            get { return FUserId; }
            set { FUserId = value; }
        }

        #endregion Properties

        #region Methods

        public override string ToString()
        {
            return FUserName;
        }

        #endregion Methods
    }

    [Serializable]
    public class ChatMessage
    {
        #region Fields

        private MessageType FMessageType;
        private UserInfo[] FUsers;
        private string FMessage;

        #endregion Fields

        #region Constructor

        public ChatMessage()
        {
        }

        #endregion Constructor

        #region Properties

        public MessageType MessageType
        {
            get { return FMessageType; }
            set { FMessageType = value; }
        }

        public UserInfo[] UserInfo
        {
            get { return FUsers; }
            set { FUsers = value; }
        }

        public string Message
        {
            get { return FMessage; }
            set { FMessage = value; }
        }

        #endregion Properties
    }

    public class ConnectionData
    {
        #region Fields

        private ConnectionState FConnectionState;
        private string FUserName;

        #endregion Fields

        #region Constructor

        public ConnectionData(ConnectionState state)
        {
            FConnectionState = state;
            FUserName = String.Empty;
        }

        #endregion Constructor

        #region Properties

        public ConnectionState ConnectionState
        {
            get { return FConnectionState; }
            set { FConnectionState = value; }
        }

        public string UserName
        {
            get { return FUserName; }
            set { FUserName = value; }
        }

        #endregion Properties
    }

    public enum MessageType
    {
        mtLogin,
        mtAuthenticated,
        mtHello,
        mtLogout,
        mtMessage
    }

    public enum ConnectionState
    {
        csConnected,
        csAuthenticated,
        csDisconnected
    }
}