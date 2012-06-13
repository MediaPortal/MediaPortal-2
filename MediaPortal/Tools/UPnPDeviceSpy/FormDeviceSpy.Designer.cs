namespace UPnPDeviceSpy
{
  partial class FormDeviceSpy
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
      System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FormDeviceSpy));
      this.menuStripMainMenu = new System.Windows.Forms.MenuStrip();
      this.fileToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
      this.exitToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
      this.splitContainerMain = new System.Windows.Forms.SplitContainer();
      this.tvDeviceTree = new System.Windows.Forms.TreeView();
      this.tbDetails = new System.Windows.Forms.TextBox();
      this.toolStrip = new System.Windows.Forms.ToolStrip();
      this.bRefresh = new System.Windows.Forms.ToolStripButton();
      this.bUPnPSearch = new System.Windows.Forms.ToolStripButton();
      this.menuStripMainMenu.SuspendLayout();
      this.splitContainerMain.Panel1.SuspendLayout();
      this.splitContainerMain.Panel2.SuspendLayout();
      this.splitContainerMain.SuspendLayout();
      this.toolStrip.SuspendLayout();
      this.SuspendLayout();
      // 
      // menuStripMainMenu
      // 
      this.menuStripMainMenu.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.fileToolStripMenuItem});
      this.menuStripMainMenu.Location = new System.Drawing.Point(0, 0);
      this.menuStripMainMenu.Name = "menuStripMainMenu";
      this.menuStripMainMenu.Size = new System.Drawing.Size(907, 24);
      this.menuStripMainMenu.TabIndex = 0;
      // 
      // fileToolStripMenuItem
      // 
      this.fileToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.exitToolStripMenuItem});
      this.fileToolStripMenuItem.Name = "fileToolStripMenuItem";
      this.fileToolStripMenuItem.Size = new System.Drawing.Size(65, 20);
      this.fileToolStripMenuItem.Text = "&Program";
      // 
      // exitToolStripMenuItem
      // 
      this.exitToolStripMenuItem.Name = "exitToolStripMenuItem";
      this.exitToolStripMenuItem.Size = new System.Drawing.Size(92, 22);
      this.exitToolStripMenuItem.Text = "&Exit";
      this.exitToolStripMenuItem.Click += new System.EventHandler(this.exitToolStripMenuItem_Click);
      // 
      // splitContainerMain
      // 
      this.splitContainerMain.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
      this.splitContainerMain.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
      this.splitContainerMain.FixedPanel = System.Windows.Forms.FixedPanel.Panel1;
      this.splitContainerMain.Location = new System.Drawing.Point(0, 52);
      this.splitContainerMain.Name = "splitContainerMain";
      // 
      // splitContainerMain.Panel1
      // 
      this.splitContainerMain.Panel1.AutoScroll = true;
      this.splitContainerMain.Panel1.Controls.Add(this.tvDeviceTree);
      // 
      // splitContainerMain.Panel2
      // 
      this.splitContainerMain.Panel2.Controls.Add(this.tbDetails);
      this.splitContainerMain.Size = new System.Drawing.Size(907, 615);
      this.splitContainerMain.SplitterDistance = 250;
      this.splitContainerMain.TabIndex = 2;
      // 
      // tvDeviceTree
      // 
      this.tvDeviceTree.Dock = System.Windows.Forms.DockStyle.Fill;
      this.tvDeviceTree.HideSelection = false;
      this.tvDeviceTree.HotTracking = true;
      this.tvDeviceTree.Location = new System.Drawing.Point(0, 0);
      this.tvDeviceTree.Name = "tvDeviceTree";
      this.tvDeviceTree.Size = new System.Drawing.Size(246, 611);
      this.tvDeviceTree.TabIndex = 2;
      this.tvDeviceTree.AfterSelect += new System.Windows.Forms.TreeViewEventHandler(this.tvDeviceTree_AfterSelect);
      // 
      // tbDetails
      // 
      this.tbDetails.Dock = System.Windows.Forms.DockStyle.Fill;
      this.tbDetails.Location = new System.Drawing.Point(0, 0);
      this.tbDetails.Multiline = true;
      this.tbDetails.Name = "tbDetails";
      this.tbDetails.ReadOnly = true;
      this.tbDetails.ScrollBars = System.Windows.Forms.ScrollBars.Both;
      this.tbDetails.Size = new System.Drawing.Size(649, 611);
      this.tbDetails.TabIndex = 0;
      // 
      // toolStrip
      // 
      this.toolStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.bRefresh,
            this.bUPnPSearch});
      this.toolStrip.LayoutStyle = System.Windows.Forms.ToolStripLayoutStyle.HorizontalStackWithOverflow;
      this.toolStrip.Location = new System.Drawing.Point(0, 24);
      this.toolStrip.Name = "toolStrip";
      this.toolStrip.Size = new System.Drawing.Size(907, 25);
      this.toolStrip.TabIndex = 3;
      this.toolStrip.Text = "toolStrip1";
      // 
      // bRefresh
      // 
      this.bRefresh.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
      this.bRefresh.Image = ((System.Drawing.Image)(resources.GetObject("bRefresh.Image")));
      this.bRefresh.ImageTransparentColor = System.Drawing.Color.Magenta;
      this.bRefresh.Name = "bRefresh";
      this.bRefresh.Size = new System.Drawing.Size(23, 22);
      this.bRefresh.Text = "&Refresh";
      this.bRefresh.Click += new System.EventHandler(this.bRefresh_Click);
      // 
      // bUPnPSearch
      // 
      this.bUPnPSearch.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
      this.bUPnPSearch.Image = ((System.Drawing.Image)(resources.GetObject("bUPnPSearch.Image")));
      this.bUPnPSearch.ImageTransparentColor = System.Drawing.Color.Magenta;
      this.bUPnPSearch.Name = "bUPnPSearch";
      this.bUPnPSearch.Size = new System.Drawing.Size(23, 22);
      this.bUPnPSearch.Text = "UPnP &search";
      this.bUPnPSearch.Click += new System.EventHandler(this.bUPnPSearch_Click);
      // 
      // FormDeviceSpy
      // 
      this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
      this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
      this.ClientSize = new System.Drawing.Size(907, 666);
      this.Controls.Add(this.toolStrip);
      this.Controls.Add(this.splitContainerMain);
      this.Controls.Add(this.menuStripMainMenu);
      this.MainMenuStrip = this.menuStripMainMenu;
      this.Name = "FormDeviceSpy";
      this.Text = "Device Spy";
      this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.FormDeviceSpy_FormClosing);
      this.Shown += new System.EventHandler(this.FormDeviceSpy_Shown);
      this.menuStripMainMenu.ResumeLayout(false);
      this.menuStripMainMenu.PerformLayout();
      this.splitContainerMain.Panel1.ResumeLayout(false);
      this.splitContainerMain.Panel2.ResumeLayout(false);
      this.splitContainerMain.Panel2.PerformLayout();
      this.splitContainerMain.ResumeLayout(false);
      this.toolStrip.ResumeLayout(false);
      this.toolStrip.PerformLayout();
      this.ResumeLayout(false);
      this.PerformLayout();

    }

    #endregion

    private System.Windows.Forms.MenuStrip menuStripMainMenu;
    private System.Windows.Forms.ToolStripMenuItem fileToolStripMenuItem;
    private System.Windows.Forms.ToolStripMenuItem exitToolStripMenuItem;
    private System.Windows.Forms.SplitContainer splitContainerMain;
    private System.Windows.Forms.TreeView tvDeviceTree;
    private System.Windows.Forms.ToolStrip toolStrip;
    private System.Windows.Forms.ToolStripButton bRefresh;
    private System.Windows.Forms.TextBox tbDetails;
    private System.Windows.Forms.ToolStripButton bUPnPSearch;
  }
}

