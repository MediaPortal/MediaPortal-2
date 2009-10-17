namespace MediaPortal
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
      System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainForm));
      this.serverTrayIcon = new System.Windows.Forms.NotifyIcon(this.components);
      this.label1 = new System.Windows.Forms.Label();
      this.SuspendLayout();
      // 
      // serverTrayIcon
      // 
      this.serverTrayIcon.BalloonTipText = "MediaPortal-II Server";
      this.serverTrayIcon.BalloonTipTitle = "MediaPortal-II";
      this.serverTrayIcon.Icon = ((System.Drawing.Icon)(resources.GetObject("serverTrayIcon.Icon")));
      this.serverTrayIcon.Text = "MediaPortal-II";
      this.serverTrayIcon.Visible = true;
      // 
      // label1
      // 
      this.label1.Dock = System.Windows.Forms.DockStyle.Fill;
      this.label1.Font = new System.Drawing.Font("Microsoft Sans Serif", 15.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
      this.label1.Location = new System.Drawing.Point(0, 0);
      this.label1.Name = "label1";
      this.label1.Size = new System.Drawing.Size(469, 177);
      this.label1.TabIndex = 0;
      this.label1.Text = "This form will be created soon...";
      this.label1.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
      // 
      // MainForm
      // 
      this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
      this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
      this.ClientSize = new System.Drawing.Size(469, 177);
      this.Controls.Add(this.label1);
      this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
      this.Name = "MainForm";
      this.Text = "MediaPortal-II Server Application";
      this.ResumeLayout(false);

    }

    #endregion

    private System.Windows.Forms.NotifyIcon serverTrayIcon;
    private System.Windows.Forms.Label label1;
  }
}