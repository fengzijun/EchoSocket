using System;
using System.Net;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using EchoSocketCore.SocketsEx;

namespace ClientSynch
{
    public partial class frmClient : Form
    {
        private delegate void UpdateListDel(string text);

        private SocketClientSync client;
        private Thread read;

        public frmClient()
        {
            InitializeComponent();
        }

        private void UpdateList(string text)
        {
            if (this.InvokeRequired)
            {
                this.BeginInvoke(new UpdateListDel(
                    delegate(string innerText)
                    {
                        this.UpdateList(innerText);
                    }), text);
            }
            else
            {
                this.lstStatus.Items.Insert(0, DateTime.Now.ToString("hh:mm:ss.fff") + " - " + text);
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
                        UpdateList("Read <- " + read);
                    }
                }

                Thread.Sleep(11);
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (!client.Connected)
            {
                client.RemoteEndPoint = new IPEndPoint(IPAddress.Parse(txtHost.Text), Convert.ToInt32(txtPort.Text));

                client.DelimiterType = (DelimiterType)Enum.Parse(typeof(DelimiterType), cboDelimiter.Text);
                //client.Delimiter = Encoding.ASCII.GetBytes(txtDelimiter.Text);
                client.Delimiter = new byte[] { 0xFF, 0x00, 0xFE, 0x01, 0xFD, 0x02 };

                client.EncryptType = (EncryptType)Enum.Parse(typeof(EncryptType), cboEncrypt.Text);
                client.CompressionType = (CompressionType)Enum.Parse(typeof(CompressionType), cboCompression.Text);

                if (cboProxyType.SelectedIndex > -1)
                {
                    client.ProxyInfo = new ProxyInfo((ProxyType)Enum.Parse(typeof(ProxyType), cboProxyType.Text),
                                new IPEndPoint(IPAddress.Parse(txtProxyHost.Text), Convert.ToInt32(txtProxyPort.Text)),
                                new NetworkCredential(txtProxyUser.Text, txtProxyPass.Text, txtProxyDomain.Text));
                }

                client.Connect();

                if (client.Connected)
                {
                    UpdateList("Connected!");

                    read = new Thread(new ParameterizedThreadStart(ThreadExecute));
                    read.Start(client);
                }
                else
                {
                    UpdateList("Not Connected! " + client.LastException != null ? client.LastException.Message : String.Empty);
                }
            }
        }

        private void frmClient_Load(object sender, EventArgs e)
        {
            txtDelimiter.Text = Encoding.ASCII.GetString(new byte[] { 0xFF, 0x00, 0xFE, 0x01, 0xFD, 0x02 });

            cboCompression.SelectedIndex = 0;
            cboDelimiter.SelectedIndex = 0;
            cboEncrypt.SelectedIndex = 0;
            cboProxyType.SelectedIndex = -1;

            client = new SocketClientSync(null);

            client.SocketBufferSize = 1024;
            client.MessageBufferSize = 2048;

            client.OnSymmetricAuthenticate += new OnSymmetricAuthenticateEvent(client_OnSymmetricAuthenticate);
            client.OnSSLClientAuthenticate += new OnSSLClientAuthenticateEvent(client_OnSSLClientAuthenticate);
            client.OnDisconnected += new OnDisconnectEvent(client_OnDisconnected);
        }

        private void client_OnDisconnected()
        {
            UpdateList("Disconnected Event!");
        }

        private static void client_OnSSLClientAuthenticate(ISocketConnection connection, out string serverName, ref X509Certificate2Collection certs, ref bool checkRevocation)
        {
            serverName = "ALAZ Library";

            X509Store store = new X509Store(StoreName.My, StoreLocation.LocalMachine);
            store.Open(OpenFlags.ReadOnly);

            certs = store.Certificates.Find(X509FindType.FindBySubjectName, serverName, true);
            checkRevocation = false;

            store.Close();
        }

        private static void client_OnSymmetricAuthenticate(ISocketConnection connection, out System.Security.Cryptography.RSACryptoServiceProvider serverKey)
        {
            serverKey = new RSACryptoServiceProvider();

            //----- Using string!
            if (connection.Context.Host.HostType == HostType.htClient)
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
                client.Disconnect();

                if (!client.Connected)
                {
                    UpdateList("Disconnected!");
                }
                else
                {
                    UpdateList("Not Disconnected! " + client.LastException != null ? client.LastException.Message : String.Empty);
                }

                read.Join();
                read = null;
            }
        }

        private void frmClient_FormClosed(object sender, FormClosedEventArgs e)
        {
            client.Dispose();
        }

        private void button3_Click_1(object sender, EventArgs e)
        {
            if (client.Connected)
            {
                client.Write(txtMessage.Text);

                if (client.LastException != null)
                {
                    UpdateList("Write Error! " + client.LastException.Message);
                }
                else
                {
                    UpdateList("Write -> " + txtMessage.Text);
                }
            }
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
            button3.PerformClick();
        }

        private void groupBox4_Enter(object sender, EventArgs e)
        {
        }

        private void groupBox1_Enter(object sender, EventArgs e)
        {
        }
    }
}