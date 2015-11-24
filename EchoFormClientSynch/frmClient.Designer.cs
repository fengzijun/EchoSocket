namespace ClientSynch
{
    partial class frmClient
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.groupBox4 = new System.Windows.Forms.GroupBox();
            this.txtProxyUser = new System.Windows.Forms.TextBox();
            this.label13 = new System.Windows.Forms.Label();
            this.cboProxyType = new System.Windows.Forms.ComboBox();
            this.txtProxyDomain = new System.Windows.Forms.TextBox();
            this.label8 = new System.Windows.Forms.Label();
            this.label11 = new System.Windows.Forms.Label();
            this.txtProxyHost = new System.Windows.Forms.TextBox();
            this.txtProxyPass = new System.Windows.Forms.TextBox();
            this.label10 = new System.Windows.Forms.Label();
            this.label12 = new System.Windows.Forms.Label();
            this.txtProxyPort = new System.Windows.Forms.TextBox();
            this.label9 = new System.Windows.Forms.Label();
            this.txtDelimiter = new System.Windows.Forms.TextBox();
            this.label7 = new System.Windows.Forms.Label();
            this.label6 = new System.Windows.Forms.Label();
            this.cboDelimiter = new System.Windows.Forms.ComboBox();
            this.label5 = new System.Windows.Forms.Label();
            this.cboCompression = new System.Windows.Forms.ComboBox();
            this.label4 = new System.Windows.Forms.Label();
            this.cboEncrypt = new System.Windows.Forms.ComboBox();
            this.button2 = new System.Windows.Forms.Button();
            this.label2 = new System.Windows.Forms.Label();
            this.txtPort = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.txtHost = new System.Windows.Forms.TextBox();
            this.button1 = new System.Windows.Forms.Button();
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            this.lstStatus = new System.Windows.Forms.ListBox();
            this.groupBox3 = new System.Windows.Forms.GroupBox();
            this.button4 = new System.Windows.Forms.Button();
            this.label3 = new System.Windows.Forms.Label();
            this.txtMessage = new System.Windows.Forms.TextBox();
            this.button3 = new System.Windows.Forms.Button();
            this.timer1 = new System.Windows.Forms.Timer(this.components);
            this.groupBox1.SuspendLayout();
            this.groupBox4.SuspendLayout();
            this.groupBox2.SuspendLayout();
            this.groupBox3.SuspendLayout();
            this.SuspendLayout();
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.groupBox4);
            this.groupBox1.Controls.Add(this.txtDelimiter);
            this.groupBox1.Controls.Add(this.label7);
            this.groupBox1.Controls.Add(this.label6);
            this.groupBox1.Controls.Add(this.cboDelimiter);
            this.groupBox1.Controls.Add(this.label5);
            this.groupBox1.Controls.Add(this.cboCompression);
            this.groupBox1.Controls.Add(this.label4);
            this.groupBox1.Controls.Add(this.cboEncrypt);
            this.groupBox1.Controls.Add(this.button2);
            this.groupBox1.Controls.Add(this.label2);
            this.groupBox1.Controls.Add(this.txtPort);
            this.groupBox1.Controls.Add(this.label1);
            this.groupBox1.Controls.Add(this.txtHost);
            this.groupBox1.Controls.Add(this.button1);
            this.groupBox1.Location = new System.Drawing.Point(16, 15);
            this.groupBox1.Margin = new System.Windows.Forms.Padding(4);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Padding = new System.Windows.Forms.Padding(4);
            this.groupBox1.Size = new System.Drawing.Size(711, 294);
            this.groupBox1.TabIndex = 0;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = " Connection ";
            this.groupBox1.Enter += new System.EventHandler(this.groupBox1_Enter);
            // 
            // groupBox4
            // 
            this.groupBox4.Controls.Add(this.txtProxyUser);
            this.groupBox4.Controls.Add(this.label13);
            this.groupBox4.Controls.Add(this.cboProxyType);
            this.groupBox4.Controls.Add(this.txtProxyDomain);
            this.groupBox4.Controls.Add(this.label8);
            this.groupBox4.Controls.Add(this.label11);
            this.groupBox4.Controls.Add(this.txtProxyHost);
            this.groupBox4.Controls.Add(this.txtProxyPass);
            this.groupBox4.Controls.Add(this.label10);
            this.groupBox4.Controls.Add(this.label12);
            this.groupBox4.Controls.Add(this.txtProxyPort);
            this.groupBox4.Controls.Add(this.label9);
            this.groupBox4.Location = new System.Drawing.Point(12, 132);
            this.groupBox4.Margin = new System.Windows.Forms.Padding(4);
            this.groupBox4.Name = "groupBox4";
            this.groupBox4.Padding = new System.Windows.Forms.Padding(4);
            this.groupBox4.Size = new System.Drawing.Size(685, 150);
            this.groupBox4.TabIndex = 26;
            this.groupBox4.TabStop = false;
            this.groupBox4.Text = " Proxy Info ";
            this.groupBox4.Enter += new System.EventHandler(this.groupBox4_Enter);
            // 
            // txtProxyUser
            // 
            this.txtProxyUser.Location = new System.Drawing.Point(19, 101);
            this.txtProxyUser.Margin = new System.Windows.Forms.Padding(4);
            this.txtProxyUser.Name = "txtProxyUser";
            this.txtProxyUser.Size = new System.Drawing.Size(132, 22);
            this.txtProxyUser.TabIndex = 20;
            this.txtProxyUser.Text = "aazevedo";
            // 
            // label13
            // 
            this.label13.AutoSize = true;
            this.label13.Location = new System.Drawing.Point(308, 81);
            this.label13.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label13.Name = "label13";
            this.label13.Size = new System.Drawing.Size(56, 17);
            this.label13.TabIndex = 25;
            this.label13.Text = "Domain";
            // 
            // cboProxyType
            // 
            this.cboProxyType.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cboProxyType.FormattingEnabled = true;
            this.cboProxyType.Items.AddRange(new object[] {
            "ptSOCKS4",
            "ptSOCKS4a",
            "ptSOCKS5",
            "ptHTTP"});
            this.cboProxyType.Location = new System.Drawing.Point(19, 48);
            this.cboProxyType.Margin = new System.Windows.Forms.Padding(4);
            this.cboProxyType.Name = "cboProxyType";
            this.cboProxyType.Size = new System.Drawing.Size(201, 24);
            this.cboProxyType.TabIndex = 14;
            // 
            // txtProxyDomain
            // 
            this.txtProxyDomain.Location = new System.Drawing.Point(309, 101);
            this.txtProxyDomain.Margin = new System.Windows.Forms.Padding(4);
            this.txtProxyDomain.Name = "txtProxyDomain";
            this.txtProxyDomain.Size = new System.Drawing.Size(139, 22);
            this.txtProxyDomain.TabIndex = 24;
            this.txtProxyDomain.Text = "mydomain";
            // 
            // label8
            // 
            this.label8.AutoSize = true;
            this.label8.Location = new System.Drawing.Point(15, 28);
            this.label8.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label8.Name = "label8";
            this.label8.Size = new System.Drawing.Size(75, 17);
            this.label8.TabIndex = 15;
            this.label8.Text = "ProxyType";
            // 
            // label11
            // 
            this.label11.AutoSize = true;
            this.label11.Location = new System.Drawing.Point(160, 81);
            this.label11.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label11.Name = "label11";
            this.label11.Size = new System.Drawing.Size(39, 17);
            this.label11.TabIndex = 23;
            this.label11.Text = "Pass";
            // 
            // txtProxyHost
            // 
            this.txtProxyHost.Location = new System.Drawing.Point(229, 49);
            this.txtProxyHost.Margin = new System.Windows.Forms.Padding(4);
            this.txtProxyHost.Name = "txtProxyHost";
            this.txtProxyHost.Size = new System.Drawing.Size(132, 22);
            this.txtProxyHost.TabIndex = 16;
            this.txtProxyHost.Text = "127.0.0.1";
            // 
            // txtProxyPass
            // 
            this.txtProxyPass.Location = new System.Drawing.Point(161, 101);
            this.txtProxyPass.Margin = new System.Windows.Forms.Padding(4);
            this.txtProxyPass.Name = "txtProxyPass";
            this.txtProxyPass.Size = new System.Drawing.Size(139, 22);
            this.txtProxyPass.TabIndex = 22;
            this.txtProxyPass.Text = "1234";
            // 
            // label10
            // 
            this.label10.AutoSize = true;
            this.label10.Location = new System.Drawing.Point(225, 30);
            this.label10.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label10.Name = "label10";
            this.label10.Size = new System.Drawing.Size(37, 17);
            this.label10.TabIndex = 17;
            this.label10.Text = "Host";
            // 
            // label12
            // 
            this.label12.AutoSize = true;
            this.label12.Location = new System.Drawing.Point(15, 81);
            this.label12.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label12.Name = "label12";
            this.label12.Size = new System.Drawing.Size(38, 17);
            this.label12.TabIndex = 21;
            this.label12.Text = "User";
            // 
            // txtProxyPort
            // 
            this.txtProxyPort.Location = new System.Drawing.Point(372, 49);
            this.txtProxyPort.Margin = new System.Windows.Forms.Padding(4);
            this.txtProxyPort.Name = "txtProxyPort";
            this.txtProxyPort.Size = new System.Drawing.Size(76, 22);
            this.txtProxyPort.TabIndex = 18;
            this.txtProxyPort.Text = "8000";
            // 
            // label9
            // 
            this.label9.AutoSize = true;
            this.label9.Location = new System.Drawing.Point(371, 30);
            this.label9.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label9.Name = "label9";
            this.label9.Size = new System.Drawing.Size(34, 17);
            this.label9.TabIndex = 19;
            this.label9.Text = "Port";
            // 
            // txtDelimiter
            // 
            this.txtDelimiter.Location = new System.Drawing.Point(441, 44);
            this.txtDelimiter.Margin = new System.Windows.Forms.Padding(4);
            this.txtDelimiter.Name = "txtDelimiter";
            this.txtDelimiter.Size = new System.Drawing.Size(132, 22);
            this.txtDelimiter.TabIndex = 13;
            this.txtDelimiter.Text = "Delimiter Text";
            // 
            // label7
            // 
            this.label7.AutoSize = true;
            this.label7.Location = new System.Drawing.Point(437, 25);
            this.label7.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(94, 17);
            this.label7.TabIndex = 12;
            this.label7.Text = "Delimiter Text";
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(227, 25);
            this.label6.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(95, 17);
            this.label6.TabIndex = 11;
            this.label6.Text = "DelimiterType";
            this.label6.Click += new System.EventHandler(this.label6_Click);
            // 
            // cboDelimiter
            // 
            this.cboDelimiter.DisplayMember = "1";
            this.cboDelimiter.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cboDelimiter.FormattingEnabled = true;
            this.cboDelimiter.Items.AddRange(new object[] {
            "dtNone",
            "dtMessageTailExcludeOnReceive",
            "dtMessageTailIncludeOnReceive"});
            this.cboDelimiter.Location = new System.Drawing.Point(231, 44);
            this.cboDelimiter.Margin = new System.Windows.Forms.Padding(4);
            this.cboDelimiter.Name = "cboDelimiter";
            this.cboDelimiter.Size = new System.Drawing.Size(201, 24);
            this.cboDelimiter.TabIndex = 10;
            this.cboDelimiter.ValueMember = "1";
            this.cboDelimiter.SelectedIndexChanged += new System.EventHandler(this.cboDelimiter_SelectedIndexChanged);
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(227, 79);
            this.label5.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(122, 17);
            this.label5.TabIndex = 9;
            this.label5.Text = "CompressionType";
            // 
            // cboCompression
            // 
            this.cboCompression.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cboCompression.FormattingEnabled = true;
            this.cboCompression.Items.AddRange(new object[] {
            "ctNone",
            "ctGZIP"});
            this.cboCompression.Location = new System.Drawing.Point(231, 98);
            this.cboCompression.Margin = new System.Windows.Forms.Padding(4);
            this.cboCompression.Name = "cboCompression";
            this.cboCompression.Size = new System.Drawing.Size(201, 24);
            this.cboCompression.TabIndex = 8;
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(16, 79);
            this.label4.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(88, 17);
            this.label4.TabIndex = 7;
            this.label4.Text = "EncryptType";
            // 
            // cboEncrypt
            // 
            this.cboEncrypt.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cboEncrypt.FormattingEnabled = true;
            this.cboEncrypt.Items.AddRange(new object[] {
            "etNone",
            "etRijndael",
            "etSSL"});
            this.cboEncrypt.Location = new System.Drawing.Point(20, 98);
            this.cboEncrypt.Margin = new System.Windows.Forms.Padding(4);
            this.cboEncrypt.Name = "cboEncrypt";
            this.cboEncrypt.Size = new System.Drawing.Size(201, 24);
            this.cboEncrypt.TabIndex = 6;
            // 
            // button2
            // 
            this.button2.Location = new System.Drawing.Point(597, 79);
            this.button2.Margin = new System.Windows.Forms.Padding(4);
            this.button2.Name = "button2";
            this.button2.Size = new System.Drawing.Size(100, 28);
            this.button2.TabIndex = 5;
            this.button2.Text = "Disconnect";
            this.button2.UseVisualStyleBackColor = true;
            this.button2.Click += new System.EventHandler(this.button2_Click);
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(161, 25);
            this.label2.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(34, 17);
            this.label2.TabIndex = 4;
            this.label2.Text = "Port";
            // 
            // txtPort
            // 
            this.txtPort.Location = new System.Drawing.Point(163, 44);
            this.txtPort.Margin = new System.Windows.Forms.Padding(4);
            this.txtPort.Name = "txtPort";
            this.txtPort.Size = new System.Drawing.Size(59, 22);
            this.txtPort.TabIndex = 3;
            this.txtPort.Text = "8090";
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(16, 25);
            this.label1.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(37, 17);
            this.label1.TabIndex = 2;
            this.label1.Text = "Host";
            // 
            // txtHost
            // 
            this.txtHost.Location = new System.Drawing.Point(20, 44);
            this.txtHost.Margin = new System.Windows.Forms.Padding(4);
            this.txtHost.Name = "txtHost";
            this.txtHost.Size = new System.Drawing.Size(132, 22);
            this.txtHost.TabIndex = 1;
            this.txtHost.Text = "127.0.0.1";
            // 
            // button1
            // 
            this.button1.Location = new System.Drawing.Point(597, 44);
            this.button1.Margin = new System.Windows.Forms.Padding(4);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(100, 28);
            this.button1.TabIndex = 0;
            this.button1.Text = "Connect";
            this.button1.UseVisualStyleBackColor = true;
            this.button1.Click += new System.EventHandler(this.button1_Click);
            // 
            // groupBox2
            // 
            this.groupBox2.Controls.Add(this.lstStatus);
            this.groupBox2.Location = new System.Drawing.Point(16, 412);
            this.groupBox2.Margin = new System.Windows.Forms.Padding(4);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.Padding = new System.Windows.Forms.Padding(4);
            this.groupBox2.Size = new System.Drawing.Size(711, 310);
            this.groupBox2.TabIndex = 1;
            this.groupBox2.TabStop = false;
            this.groupBox2.Text = " Status ";
            // 
            // lstStatus
            // 
            this.lstStatus.FormattingEnabled = true;
            this.lstStatus.HorizontalScrollbar = true;
            this.lstStatus.ItemHeight = 16;
            this.lstStatus.Location = new System.Drawing.Point(19, 23);
            this.lstStatus.Margin = new System.Windows.Forms.Padding(4);
            this.lstStatus.Name = "lstStatus";
            this.lstStatus.Size = new System.Drawing.Size(677, 276);
            this.lstStatus.TabIndex = 0;
            // 
            // groupBox3
            // 
            this.groupBox3.Controls.Add(this.button4);
            this.groupBox3.Controls.Add(this.label3);
            this.groupBox3.Controls.Add(this.txtMessage);
            this.groupBox3.Controls.Add(this.button3);
            this.groupBox3.Location = new System.Drawing.Point(16, 316);
            this.groupBox3.Margin = new System.Windows.Forms.Padding(4);
            this.groupBox3.Name = "groupBox3";
            this.groupBox3.Padding = new System.Windows.Forms.Padding(4);
            this.groupBox3.Size = new System.Drawing.Size(711, 89);
            this.groupBox3.TabIndex = 8;
            this.groupBox3.TabStop = false;
            this.groupBox3.Text = " Chat ";
            // 
            // button4
            // 
            this.button4.Location = new System.Drawing.Point(597, 17);
            this.button4.Margin = new System.Windows.Forms.Padding(4);
            this.button4.Name = "button4";
            this.button4.Size = new System.Drawing.Size(100, 28);
            this.button4.TabIndex = 26;
            this.button4.Text = "Timer";
            this.button4.UseVisualStyleBackColor = true;
            this.button4.Click += new System.EventHandler(this.button4_Click);
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(16, 23);
            this.label3.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(65, 17);
            this.label3.TabIndex = 10;
            this.label3.Text = "Message";
            // 
            // txtMessage
            // 
            this.txtMessage.Location = new System.Drawing.Point(20, 43);
            this.txtMessage.Margin = new System.Windows.Forms.Padding(4);
            this.txtMessage.Name = "txtMessage";
            this.txtMessage.Size = new System.Drawing.Size(553, 22);
            this.txtMessage.TabIndex = 9;
            this.txtMessage.Text = "Test for Echo!";
            // 
            // button3
            // 
            this.button3.Location = new System.Drawing.Point(597, 53);
            this.button3.Margin = new System.Windows.Forms.Padding(4);
            this.button3.Name = "button3";
            this.button3.Size = new System.Drawing.Size(100, 28);
            this.button3.TabIndex = 8;
            this.button3.Text = "Send";
            this.button3.UseVisualStyleBackColor = true;
            this.button3.Click += new System.EventHandler(this.button3_Click_1);
            // 
            // timer1
            // 
            this.timer1.Tick += new System.EventHandler(this.timer1_Tick);
            // 
            // frmClient
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(740, 737);
            this.Controls.Add(this.groupBox3);
            this.Controls.Add(this.groupBox2);
            this.Controls.Add(this.groupBox1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.Margin = new System.Windows.Forms.Padding(4);
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "frmClient";
            this.Text = "EchoFormClientSynch";
            this.Load += new System.EventHandler(this.frmClient_Load);
            this.FormClosed += new System.Windows.Forms.FormClosedEventHandler(this.frmClient_FormClosed);
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.groupBox4.ResumeLayout(false);
            this.groupBox4.PerformLayout();
            this.groupBox2.ResumeLayout(false);
            this.groupBox3.ResumeLayout(false);
            this.groupBox3.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox txtHost;
        private System.Windows.Forms.Button button1;
        private System.Windows.Forms.Button button2;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.TextBox txtPort;
        private System.Windows.Forms.GroupBox groupBox2;
        private System.Windows.Forms.ListBox lstStatus;
        private System.Windows.Forms.TextBox txtDelimiter;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.ComboBox cboDelimiter;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.ComboBox cboCompression;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.ComboBox cboEncrypt;
        private System.Windows.Forms.GroupBox groupBox3;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.TextBox txtMessage;
        private System.Windows.Forms.Button button3;
        private System.Windows.Forms.Label label8;
        private System.Windows.Forms.ComboBox cboProxyType;
        private System.Windows.Forms.GroupBox groupBox4;
        private System.Windows.Forms.TextBox txtProxyUser;
        private System.Windows.Forms.Label label13;
        private System.Windows.Forms.TextBox txtProxyDomain;
        private System.Windows.Forms.Label label11;
        private System.Windows.Forms.TextBox txtProxyHost;
        private System.Windows.Forms.TextBox txtProxyPass;
        private System.Windows.Forms.Label label10;
        private System.Windows.Forms.Label label12;
        private System.Windows.Forms.TextBox txtProxyPort;
        private System.Windows.Forms.Label label9;
        private System.Windows.Forms.Button button4;
        private System.Windows.Forms.Timer timer1;
    }
}