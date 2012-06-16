namespace UPnPLightServer
{
  partial class FormUPnPLightServer
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
      this.gbServer = new System.Windows.Forms.GroupBox();
      this.lLightState = new System.Windows.Forms.Label();
      this.lTextLightState = new System.Windows.Forms.Label();
      this.panel1 = new System.Windows.Forms.Panel();
      this.cbActivateServer = new System.Windows.Forms.CheckBox();
      this.label1 = new System.Windows.Forms.Label();
      this.gbServer.SuspendLayout();
      this.panel1.SuspendLayout();
      this.SuspendLayout();
      // 
      // gbServer
      // 
      this.gbServer.Controls.Add(this.label1);
      this.gbServer.Controls.Add(this.lLightState);
      this.gbServer.Controls.Add(this.lTextLightState);
      this.gbServer.Dock = System.Windows.Forms.DockStyle.Fill;
      this.gbServer.Location = new System.Drawing.Point(154, 0);
      this.gbServer.Name = "gbServer";
      this.gbServer.Size = new System.Drawing.Size(212, 119);
      this.gbServer.TabIndex = 1;
      this.gbServer.TabStop = false;
      this.gbServer.Text = "LightServer";
      // 
      // lLightState
      // 
      this.lLightState.AutoSize = true;
      this.lLightState.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
      this.lLightState.Location = new System.Drawing.Point(64, 29);
      this.lLightState.Name = "lLightState";
      this.lLightState.Size = new System.Drawing.Size(24, 13);
      this.lLightState.TabIndex = 1;
      this.lLightState.Text = "Off";
      // 
      // lTextLightState
      // 
      this.lTextLightState.AutoSize = true;
      this.lTextLightState.Location = new System.Drawing.Point(12, 29);
      this.lTextLightState.Name = "lTextLightState";
      this.lTextLightState.Size = new System.Drawing.Size(33, 13);
      this.lTextLightState.TabIndex = 0;
      this.lTextLightState.Text = "Light:";
      // 
      // panel1
      // 
      this.panel1.Controls.Add(this.cbActivateServer);
      this.panel1.Dock = System.Windows.Forms.DockStyle.Left;
      this.panel1.Location = new System.Drawing.Point(0, 0);
      this.panel1.Name = "panel1";
      this.panel1.Size = new System.Drawing.Size(154, 119);
      this.panel1.TabIndex = 0;
      // 
      // cbActivateServer
      // 
      this.cbActivateServer.AutoSize = true;
      this.cbActivateServer.Checked = true;
      this.cbActivateServer.CheckState = System.Windows.Forms.CheckState.Checked;
      this.cbActivateServer.Dock = System.Windows.Forms.DockStyle.Top;
      this.cbActivateServer.Location = new System.Drawing.Point(0, 0);
      this.cbActivateServer.Name = "cbActivateServer";
      this.cbActivateServer.Size = new System.Drawing.Size(154, 17);
      this.cbActivateServer.TabIndex = 1;
      this.cbActivateServer.Text = "Server &activated";
      this.cbActivateServer.UseVisualStyleBackColor = true;
      this.cbActivateServer.CheckedChanged += new System.EventHandler(this.cbActivateServer_CheckedChanged);
      // 
      // label1
      // 
      this.label1.Location = new System.Drawing.Point(22, 53);
      this.label1.Name = "label1";
      this.label1.Size = new System.Drawing.Size(181, 55);
      this.label1.TabIndex = 2;
      this.label1.Text = "The implementation of this device is not finished yet; currently the root device " +
    "is simply empty.";
      // 
      // FormUPnPLightServer
      // 
      this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
      this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
      this.ClientSize = new System.Drawing.Size(366, 119);
      this.Controls.Add(this.gbServer);
      this.Controls.Add(this.panel1);
      this.Name = "FormUPnPLightServer";
      this.Text = "FormUPnPLightServer";
      this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.FormUPnPLightServer_FormClosing);
      this.Shown += new System.EventHandler(this.FormUPnPLightServer_Shown);
      this.gbServer.ResumeLayout(false);
      this.gbServer.PerformLayout();
      this.panel1.ResumeLayout(false);
      this.panel1.PerformLayout();
      this.ResumeLayout(false);

    }

    #endregion

    private System.Windows.Forms.GroupBox gbServer;
    private System.Windows.Forms.Label lLightState;
    private System.Windows.Forms.Label lTextLightState;
    private System.Windows.Forms.Panel panel1;
    private System.Windows.Forms.CheckBox cbActivateServer;
    private System.Windows.Forms.Label label1;
  }
}

