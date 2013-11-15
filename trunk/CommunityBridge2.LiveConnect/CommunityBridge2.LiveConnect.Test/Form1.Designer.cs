namespace CommunityBridge2.LiveConnect.Test
{
  partial class Form1
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
      this.label1 = new System.Windows.Forms.Label();
      this.label2 = new System.Windows.Forms.Label();
      this.txtClientID = new System.Windows.Forms.TextBox();
      this.txtRefreshToken = new System.Windows.Forms.TextBox();
      this.btnSaveClientId = new System.Windows.Forms.Button();
      this.label3 = new System.Windows.Forms.Label();
      this.txtAccessToken = new System.Windows.Forms.TextBox();
      this.txtScopes = new System.Windows.Forms.TextBox();
      this.label4 = new System.Windows.Forms.Label();
      this.btnLogin = new System.Windows.Forms.Button();
      this.btnLogout = new System.Windows.Forms.Button();
      this.signOutWebBrowser = new System.Windows.Forms.WebBrowser();
      this.label5 = new System.Windows.Forms.Label();
      this.txtExpires = new System.Windows.Forms.TextBox();
      this.SuspendLayout();
      // 
      // label1
      // 
      this.label1.AutoSize = true;
      this.label1.Location = new System.Drawing.Point(12, 9);
      this.label1.Name = "label1";
      this.label1.Size = new System.Drawing.Size(42, 13);
      this.label1.TabIndex = 0;
      this.label1.Text = "ClientId";
      // 
      // label2
      // 
      this.label2.AutoSize = true;
      this.label2.Location = new System.Drawing.Point(12, 61);
      this.label2.Name = "label2";
      this.label2.Size = new System.Drawing.Size(75, 13);
      this.label2.TabIndex = 1;
      this.label2.Text = "RefreshToken";
      // 
      // txtClientID
      // 
      this.txtClientID.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
      this.txtClientID.Location = new System.Drawing.Point(115, 9);
      this.txtClientID.Name = "txtClientID";
      this.txtClientID.Size = new System.Drawing.Size(289, 20);
      this.txtClientID.TabIndex = 2;
      // 
      // txtRefreshToken
      // 
      this.txtRefreshToken.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
      this.txtRefreshToken.Location = new System.Drawing.Point(115, 61);
      this.txtRefreshToken.Multiline = true;
      this.txtRefreshToken.Name = "txtRefreshToken";
      this.txtRefreshToken.ScrollBars = System.Windows.Forms.ScrollBars.Both;
      this.txtRefreshToken.Size = new System.Drawing.Size(289, 77);
      this.txtRefreshToken.TabIndex = 3;
      // 
      // btnSaveClientId
      // 
      this.btnSaveClientId.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
      this.btnSaveClientId.Location = new System.Drawing.Point(410, 7);
      this.btnSaveClientId.Name = "btnSaveClientId";
      this.btnSaveClientId.Size = new System.Drawing.Size(75, 131);
      this.btnSaveClientId.TabIndex = 4;
      this.btnSaveClientId.Text = "Save";
      this.btnSaveClientId.UseVisualStyleBackColor = true;
      this.btnSaveClientId.Click += new System.EventHandler(this.btnSaveClientId_Click);
      // 
      // label3
      // 
      this.label3.AutoSize = true;
      this.label3.Location = new System.Drawing.Point(12, 144);
      this.label3.Name = "label3";
      this.label3.Size = new System.Drawing.Size(73, 13);
      this.label3.TabIndex = 5;
      this.label3.Text = "AccessToken";
      // 
      // txtAccessToken
      // 
      this.txtAccessToken.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
      this.txtAccessToken.Location = new System.Drawing.Point(115, 144);
      this.txtAccessToken.Multiline = true;
      this.txtAccessToken.Name = "txtAccessToken";
      this.txtAccessToken.ReadOnly = true;
      this.txtAccessToken.ScrollBars = System.Windows.Forms.ScrollBars.Both;
      this.txtAccessToken.Size = new System.Drawing.Size(370, 77);
      this.txtAccessToken.TabIndex = 6;
      // 
      // txtScopes
      // 
      this.txtScopes.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
      this.txtScopes.Location = new System.Drawing.Point(115, 35);
      this.txtScopes.Name = "txtScopes";
      this.txtScopes.Size = new System.Drawing.Size(289, 20);
      this.txtScopes.TabIndex = 8;
      // 
      // label4
      // 
      this.label4.AutoSize = true;
      this.label4.Location = new System.Drawing.Point(12, 35);
      this.label4.Name = "label4";
      this.label4.Size = new System.Drawing.Size(43, 13);
      this.label4.TabIndex = 7;
      this.label4.Text = "Scopes";
      // 
      // btnLogin
      // 
      this.btnLogin.Location = new System.Drawing.Point(15, 258);
      this.btnLogin.Name = "btnLogin";
      this.btnLogin.Size = new System.Drawing.Size(75, 23);
      this.btnLogin.TabIndex = 9;
      this.btnLogin.Text = "Login";
      this.btnLogin.UseVisualStyleBackColor = true;
      this.btnLogin.Click += new System.EventHandler(this.btnLogin_Click);
      // 
      // btnLogout
      // 
      this.btnLogout.Location = new System.Drawing.Point(96, 258);
      this.btnLogout.Name = "btnLogout";
      this.btnLogout.Size = new System.Drawing.Size(75, 23);
      this.btnLogout.TabIndex = 10;
      this.btnLogout.Text = "Logout";
      this.btnLogout.UseVisualStyleBackColor = true;
      this.btnLogout.Click += new System.EventHandler(this.btnLogout_Click);
      // 
      // signOutWebBrowser
      // 
      this.signOutWebBrowser.Location = new System.Drawing.Point(177, 258);
      this.signOutWebBrowser.MinimumSize = new System.Drawing.Size(20, 20);
      this.signOutWebBrowser.Name = "signOutWebBrowser";
      this.signOutWebBrowser.Size = new System.Drawing.Size(26, 23);
      this.signOutWebBrowser.TabIndex = 11;
      this.signOutWebBrowser.Visible = false;
      // 
      // label5
      // 
      this.label5.AutoSize = true;
      this.label5.Location = new System.Drawing.Point(12, 227);
      this.label5.Name = "label5";
      this.label5.Size = new System.Drawing.Size(41, 13);
      this.label5.TabIndex = 12;
      this.label5.Text = "Expires";
      // 
      // txtExpires
      // 
      this.txtExpires.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
      this.txtExpires.Location = new System.Drawing.Point(115, 227);
      this.txtExpires.Name = "txtExpires";
      this.txtExpires.ReadOnly = true;
      this.txtExpires.Size = new System.Drawing.Size(289, 20);
      this.txtExpires.TabIndex = 13;
      // 
      // Form1
      // 
      this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
      this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
      this.ClientSize = new System.Drawing.Size(497, 372);
      this.Controls.Add(this.txtExpires);
      this.Controls.Add(this.label5);
      this.Controls.Add(this.signOutWebBrowser);
      this.Controls.Add(this.btnLogout);
      this.Controls.Add(this.btnLogin);
      this.Controls.Add(this.txtScopes);
      this.Controls.Add(this.label4);
      this.Controls.Add(this.txtAccessToken);
      this.Controls.Add(this.label3);
      this.Controls.Add(this.btnSaveClientId);
      this.Controls.Add(this.txtRefreshToken);
      this.Controls.Add(this.txtClientID);
      this.Controls.Add(this.label2);
      this.Controls.Add(this.label1);
      this.Name = "Form1";
      this.Text = "Windows Live Connect - Test";
      this.Load += new System.EventHandler(this.Form1_Load);
      this.ResumeLayout(false);
      this.PerformLayout();

    }

    #endregion

    private System.Windows.Forms.Label label1;
    private System.Windows.Forms.Label label2;
    private System.Windows.Forms.TextBox txtClientID;
    private System.Windows.Forms.TextBox txtRefreshToken;
    private System.Windows.Forms.Button btnSaveClientId;
    private System.Windows.Forms.Label label3;
    private System.Windows.Forms.TextBox txtAccessToken;
    private System.Windows.Forms.TextBox txtScopes;
    private System.Windows.Forms.Label label4;
    private System.Windows.Forms.Button btnLogin;
    private System.Windows.Forms.Button btnLogout;
    private System.Windows.Forms.WebBrowser signOutWebBrowser;
    private System.Windows.Forms.Label label5;
    private System.Windows.Forms.TextBox txtExpires;
  }
}

