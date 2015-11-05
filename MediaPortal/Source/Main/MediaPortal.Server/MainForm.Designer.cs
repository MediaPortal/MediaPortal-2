namespace MediaPortal.Server
{
  partial class MainForm
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
      this.serverTrayIcon = new System.Windows.Forms.NotifyIcon(this.components);
      this.lvClients = new System.Windows.Forms.ListView();
      this.colClient = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
      this.colSystem = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
      this.colConnectionState = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
      this.lbAttachedClients = new System.Windows.Forms.Label();
      this.SuspendLayout();
      // 
      // serverTrayIcon
      // 
      this.serverTrayIcon.BalloonTipText = "MediaPortal 2 Server";
      this.serverTrayIcon.BalloonTipTitle = "MediaPortal 2";
      this.serverTrayIcon.Text = "MediaPortal 2";
      this.serverTrayIcon.Visible = true;
      // 
      // lvClients
      // 
      this.lvClients.Alignment = System.Windows.Forms.ListViewAlignment.Default;
      this.lvClients.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
      this.lvClients.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.colClient,
            this.colSystem,
            this.colConnectionState});
      this.lvClients.FullRowSelect = true;
      this.lvClients.HideSelection = false;
      this.lvClients.Location = new System.Drawing.Point(0, 23);
      this.lvClients.Name = "lvClients";
      this.lvClients.Size = new System.Drawing.Size(538, 186);
      this.lvClients.TabIndex = 2;
      this.lvClients.UseCompatibleStateImageBehavior = false;
      this.lvClients.View = System.Windows.Forms.View.Details;
      // 
      // colClient
      // 
      this.colClient.Text = "Client";
      this.colClient.Width = 200;
      // 
      // colSystem
      // 
      this.colSystem.Text = "System";
      this.colSystem.Width = 200;
      // 
      // colConnectionState
      // 
      this.colConnectionState.Text = "Connection state";
      this.colConnectionState.Width = 100;
      // 
      // lbAttachedClients
      // 
      this.lbAttachedClients.AutoSize = true;
      this.lbAttachedClients.Location = new System.Drawing.Point(6, 5);
      this.lbAttachedClients.Name = "lbAttachedClients";
      this.lbAttachedClients.Size = new System.Drawing.Size(83, 13);
      this.lbAttachedClients.TabIndex = 0;
      this.lbAttachedClients.Text = "Attached &clients";
      // 
      // MainForm
      // 
      this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
      this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
      this.ClientSize = new System.Drawing.Size(538, 209);
      this.Controls.Add(this.lbAttachedClients);
      this.Controls.Add(this.lvClients);
      this.Name = "MainForm";
      this.Text = "MediaPortal 2 Server Application";
      this.FormClosed += new System.Windows.Forms.FormClosedEventHandler(this.OnMainFormClosed);
      this.Shown += new System.EventHandler(this.OnMainFormShown);
      this.ResumeLayout(false);
      this.PerformLayout();

    }

    #endregion

    private System.Windows.Forms.NotifyIcon serverTrayIcon;
    private System.Windows.Forms.ListView lvClients;
    private System.Windows.Forms.ColumnHeader colClient;
    private System.Windows.Forms.ColumnHeader colSystem;
    private System.Windows.Forms.ColumnHeader colConnectionState;
    private System.Windows.Forms.Label lbAttachedClients;
  }
}