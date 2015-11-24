namespace EchoFormTemplate
{
    partial class frmEchoForm
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
            this.lstMessages = new System.Windows.Forms.ListBox();
            this.SuspendLayout();
            // 
            // lstMessages
            // 
            this.lstMessages.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.lstMessages.FormattingEnabled = true;
            this.lstMessages.ItemHeight = 16;
            this.lstMessages.Location = new System.Drawing.Point(16, 64);
            this.lstMessages.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.lstMessages.Name = "lstMessages";
            this.lstMessages.Size = new System.Drawing.Size(697, 244);
            this.lstMessages.TabIndex = 1;
            this.lstMessages.SelectedIndexChanged += new System.EventHandler(this.lstMessages_SelectedIndexChanged);
            // 
            // frmEchoForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(731, 324);
            this.Controls.Add(this.lstMessages);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.Name = "frmEchoForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Echo Form";
            this.Load += new System.EventHandler(this.frmServer_Load);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.ListBox lstMessages;



    }
}

