using System;
using System.Collections.Generic;
using System.Collections;
using System.ComponentModel;

using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Security.Cryptography.X509Certificates;
using System.Security.Cryptography;
using System.Threading;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;

using ALAZ.SystemEx;
using ALAZ.SystemEx.NetEx.SocketsEx;

using ChatSocketService;

namespace ChatClient
{
 
    public partial class frmClient : Form
    {

        private const int WM_VSCROLL = 277; // Vertical scroll
        private const int SB_PAGEBOTTOM = 7; // Scrolls to the upper right

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern int SendMessage(IntPtr hWnd, int wMsg, IntPtr wParam, IntPtr lParam);

        delegate void UpdateListDel(string text);
        delegate void AddUserDel(UserInfo[] users);
        
        UserInfo userInfo;
        
        SocketClientSync client;
        Thread read;

        public frmClient()
        {
            InitializeComponent();
        }

        private void UpdateList(string text)
        {

            if (this.InvokeRequired)
            {

                this.Invoke(new UpdateListDel(
                    delegate(string innerText)
                    {
                    
                        this.UpdateList(innerText);

                    }), text);

            }
            else
            {

                string[] data = text.Split( "\r\n".ToCharArray());

                this.lstStatus.Items.Add(DateTime.Now.ToString("hh:mm:ss.fff") + " - " + data[0]);

                if (data.Length > 1)
                {
                    this.lstStatus.Items.Add(data[2]);
                }

                SendMessage(lstStatus.Handle, WM_VSCROLL, (IntPtr)SB_PAGEBOTTOM, IntPtr.Zero);
                Application.DoEvents();
            }

        }

        private void CheckMsg(string data)
        {

            ChatMessage msg = ChatSocketService.ChatSocketService.DeserializeMessage(Encoding.GetEncoding(1252).GetBytes(data));
            
            switch(msg.MessageType)
            {
                
                case MessageType.mtLogin:
                    
                    userInfo = msg.UserInfo[0];
                    UpdateList(userInfo.UserName + " has entered!");
                    break;

                case MessageType.mtAuthenticated:

                    UpdateList(msg.UserInfo[0].UserName + " has entered!");
                    AddUser(msg.UserInfo);
                    
                    break;

                case MessageType.mtMessage:
                
                    UpdateList(msg.UserInfo[0].UserName + "\r\n" + msg.Message);
                    break;

                case MessageType.mtHello:

                    AddUser(msg.UserInfo);
                    break;

                case MessageType.mtLogout:

                    RemoveUser(msg.UserInfo);
                    break;
                    
                    
            }

        }
        
        private void AddUser(UserInfo[] users)
        {
            
            if (this.InvokeRequired)
            {

                this.Invoke(
                    new AddUserDel(
                        delegate(UserInfo[] innerUsers)
                        {
                            this.AddUser(innerUsers);
                        }
                    ), users);

            }
            else
            {

                foreach (UserInfo u in users)
                {
                    if (u.UserName != null)
                    {
                        this.lstUsers.Items.Add(u);
                    }
                }

            }
        
        }

        private void RemoveUser(UserInfo[] users)
        {

            if (this.InvokeRequired)
            {

                this.Invoke(
                    new AddUserDel(
                        delegate(UserInfo[] innerUsers)
                        {
                            this.RemoveUser(innerUsers);
                        }
                    ), users);

            }
            else
            {

                bool found = false;
                
                for (int j = 0; j < users.Length; j++)
			    {
    			
                    if (users[j].UserName != null)
                    {
                        for (int i = 0; i < this.lstUsers.Items.Count; i++)
                        {
                            
                            UserInfo u = (UserInfo) this.lstUsers.Items[i];
                            
                            if (u.UserId == users[j].UserId)
                            {
                                this.lstUsers.Items.RemoveAt(i);
                                found = true;
                                break;    
                            }

        	            }
        	            
        	        }
        	        
        	        if (found)
        	        {

                        UpdateList(users[0].UserName + " quits!");

        	            break;
        	        }

                }
                
            }

        }

        private void ThreadExecute(object data)
        {

            SocketClientSync client = (SocketClientSync)data;
            string read = null;

            while (client.Connected)
            {
                
                read = client.Read(500);

                if (client.LastException != null)
                {
                    if (!(client.LastException is TimeoutException))
                    {
                        UpdateList("Read Error! " + client.LastException.Message);
                        client.Disconnect();
                    }
                }
                else
                {

                    if (read != null)
                    {
                        CheckMsg(read);
                    }

                }

                Thread.Sleep(11);

            }

        }

        private void button1_Click(object sender, EventArgs e)
        {

            if (!client.Connected)
            {

                client.RemoteEndPoint = new IPEndPoint(IPAddress.Parse(txtHost.Text), 8090);
                
                client.DelimiterType = DelimiterType.dtMessageTailExcludeOnReceive;
                client.Delimiter = new byte[] {0xAA, 0xFF, 0xAA};

                client.EncryptType = EncryptType.etRijndael;
                client.CompressionType = CompressionType.ctNone;

                client.SocketBufferSize = 1024;
                client.MessageBufferSize = 512;

                client.Connect();
                
                if (client.Connected)
                {

                    this.lstUsers.Items.Clear();
                    this.lstStatus.Items.Clear();

                    read = new Thread(new ParameterizedThreadStart(ThreadExecute));
                    read.Start(client);
                    
                    ChatMessage msg = new ChatMessage();
                    msg.MessageType = MessageType.mtLogin;

                    msg.UserInfo = new UserInfo[1];
                    msg.UserInfo[0].UserName = txtNickName.Text;
                    
                    client.Write(ChatSocketService.ChatSocketService.SerializeMessage(msg));

                }
                else
                { 
                    UpdateList("Not Connected! " + client.LastException != null ? client.LastException.Message : String.Empty);
                }

            }

        }

        private void frmClient_Load(object sender, EventArgs e)
        {

            client = new SocketClientSync(null);
            client.OnSymmetricAuthenticate += new OnSymmetricAuthenticateEvent(client_OnSymmetricAuthenticate);

        }

        static void client_OnSymmetricAuthenticate(ISocketConnection connection, out System.Security.Cryptography.RSACryptoServiceProvider serverKey)
        {
            
            //----- Using string!

            //----- You must get the public key xml from the ALAZ certificate in you server machine.
            //----- Uncomment the following lines to get the public key from certificate.

            //---- Get certificate!
            // X509Store store = new X509Store(StoreName.My, StoreLocation.LocalMachine);
            // store.Open(OpenFlags.ReadOnly);
            // X509Certificate2 certificate = store.Certificates.Find(X509FindType.FindBySubjectName, "ALAZ Library", true)[0];

            //---- Get public key string!
            // string publicKey = certificate.PrivateKey.ToXmlString(false);

            serverKey = new RSACryptoServiceProvider();

            //----- Using string!
            if (connection.Host.HostType == HostType.htClient)
            {
                serverKey.FromXmlString("<RSAKeyValue><Modulus>z2ksxSTLHSBjY4+IEz7TZU5EclOql5pphA9+xyNQ6c1rYW6VPAmXmiXZKmsza8N++YVLAGnzR95iYyr4oL+mBz8lbhjDH2iqyQL7utbW1s87WaDC2o+82dLnLvwEqBhWpnz4tC0v0kCKayH6Jj+30l3xLdgDwReWF7YEvp6yq6nGxHOeSiioPpTtJzNhWjKGnK6oSZqthfWHewlRl2hVIrewD+JbP5JYTp/7iYptOiCwNAUZEBxODR2743D56J1AeHNc8VpZNvE3ZozIoRFhnxZw0ZpvMbgPliKPyjPeOvOFeqZUJ2zkQ7sH+gnqt67QzkOzznfuFPmTpBo0tMheyw==</Modulus><Exponent>AQAB</Exponent></RSAKeyValue>");
            }
            else
            {
                serverKey.FromXmlString("<RSAKeyValue><Modulus>z2ksxSTLHSBjY4+IEz7TZU5EclOql5pphA9+xyNQ6c1rYW6VPAmXmiXZKmsza8N++YVLAGnzR95iYyr4oL+mBz8lbhjDH2iqyQL7utbW1s87WaDC2o+82dLnLvwEqBhWpnz4tC0v0kCKayH6Jj+30l3xLdgDwReWF7YEvp6yq6nGxHOeSiioPpTtJzNhWjKGnK6oSZqthfWHewlRl2hVIrewD+JbP5JYTp/7iYptOiCwNAUZEBxODR2743D56J1AeHNc8VpZNvE3ZozIoRFhnxZw0ZpvMbgPliKPyjPeOvOFeqZUJ2zkQ7sH+gnqt67QzkOzznfuFPmTpBo0tMheyw==</Modulus><Exponent>AQAB</Exponent><P>7IhXSag5zlV+Ary/KDsMinK2Jah/WdTov6Z2XAAPHB4zOGEbhCXdgTEkIrOJNpyobF6L7mR9sTnuV5pr+vWklKkYMbxUEK+KRYo4knUvxx5ED4lFE3KUGeVz6jJ1LY5FqmQT4RTtfwZa6dxRPSgn19/k6sOqyPnnalPz30CYFAk=</P><Q>4Hs/u3UIH+CB3yf2gpupXw5yxl82YX/GuB+ZIAYopM65UlukzFl8eW1iEu42gG/UOpjfmDje+wEvIZ5gcKGjGdDgRmEbAYKNt7X6LqkhIMQqUHt0vAsNrYDXgRFVHdd8YisZ62DzAyMM9nu6v0jPTmhlJSDJwpH3s9XbVy0rmTM=</Q><DP>IF7UW087ggJvOV6tZosWP0hNpz+1Fg0uQTQ91H9pkfaMGfYoNuCbvNeF033wlFnCLvqNefWkwgFknfaTOogtmu69UektNA9iA/xTm6+P91csB1hI7M1seVLOl0mKgc6LuDL0CYS8r/qlrIWrVIxPT5rjkEFw+QpCYmnU4UPMzEk=</DP><DQ>jy7OBfmuBvcin35UBBbZv6Htn45Xl3TzAbpV51FGV2jsWBXQVe+2L5WPeteqt92clwuvgt6zi5LDx0PH68+NwweyJfIGUb4+OrG+NEj4snetLcyxNsguHz8RNmghzHkIA23OiI48MwIGYKmnAh+k6zQ3X6k8R/jm8DQ2RbKwHnU=</DQ><InverseQ>Jrbm5MzTpYI9f0jQKBFzdEdI4DeUFou4BrFpJaheh/+jhzogia+0VsK1CfuXbXgFLPV2aXpQeZYZTX/ANJEymJsp9kAELknq8O+qz6QFyfY0F4Q5H6SVuI/U40XlstYZ2ZEvjGMhXpSAnQUIZ8HJQf8nFOSoAK+HyDwPdvn5RlE=</InverseQ><D>L5hkBK1nyrxG8m7afAgbvJCUVmPqrrVpZzujDRGGnNBdxtL4ffl5h48N4ZUODLmk5p920ZZ+lExs6XLP8Rtpfxo3fadDB28eWdhMadipHkwZw3yHml4HqTijgn2kl+pV4Ainjbkc0zOqT+FRJPvUM/sIwEtkuSevcqt7NT73ozp9roswv0QHBrclCVIN0uiCqPEsfTaLeVEpg48dOh8as6l1XDlgnDGTFjkj2AgFfD27POPE3n4pJSaYJc5zNijbwrjyz8qa1nr+xBQ+yvteNDOg/1LAczP1xrypDgsl/bRHmkljYhPj40SXwK2jwyicgfgCbE3wi6O9t52D8koacQ==</D></RSAKeyValue>");
            }
            
             
        }

        private void button2_Click(object sender, EventArgs e)
        {

            if (client.Connected)
            {

                ChatMessage msg = new ChatMessage();
                msg.MessageType = MessageType.mtLogout;

                msg.UserInfo = new UserInfo[1];
                msg.UserInfo[0].UserName = userInfo.UserName;
                msg.UserInfo[0].UserId = userInfo.UserId;

                client.Write(ChatSocketService.ChatSocketService.SerializeMessage(msg));

                UpdateList("Disconnecting!");

                Thread.Sleep(1000);
                
                client.Disconnect();

                if (!client.Connected)
                {
                    UpdateList("Disconnected!");
                }
                else
                {
                    UpdateList("Not Disconnected! " + client.LastException != null ? client.LastException.Message : String.Empty);
                }

                this.lstUsers.Items.Clear();
                
                Application.DoEvents();

                read.Join();
                read = null;

                Application.DoEvents();

            }

        }

        private void frmClient_FormClosed(object sender, FormClosedEventArgs e)
        {
            
            if(client != null)
            {
            
                if (client.Connected)
                {
                    client.Disconnect();
                }

                client.Dispose();
                
            }
            
        }

        private void Send()
        {

            if (client.Connected)
            {

                ChatMessage msg = new ChatMessage();

                msg.UserInfo = new UserInfo[1];
                msg.UserInfo[0] = userInfo;

                msg.MessageType = MessageType.mtMessage;
                msg.Message = txtMessage.Text;

                client.Write(ChatSocketService.ChatSocketService.SerializeMessage(msg));

                if (client.LastException != null)
                {
                    UpdateList("Write Error! " + client.LastException.Message);
                }
                else
                {
                    txtMessage.Clear();
                    UpdateList(msg.UserInfo[0].UserName + "\r\n" + msg.Message);
                }

            }
        
        }
        
        private void button3_Click_1(object sender, EventArgs e)
        {

            Send();

        }

        private void label6_Click(object sender, EventArgs e)
        {

        }

        private void cboDelimiter_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void button4_Click(object sender, EventArgs e)
        {
            
            Random r = new Random(DateTime.Now.Millisecond);
            
            timer1.Interval = r.Next(5000, 10000);
            timer1.Start();
            
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            
        }

        private void txtMessage_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == 13)
            {
                if (txtMessage.Text.Length > 0)
                {
                    Send();
                }
            }
        }

    }

}