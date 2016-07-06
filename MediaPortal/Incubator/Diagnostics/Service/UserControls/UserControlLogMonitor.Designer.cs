namespace MediaPortal.UiComponents.Diagnostics.Service.UserControls
{
    partial class UserControlLogMonitor
    {
        /// <summary> 
        /// Variable nécessaire au concepteur.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary> 
        /// Nettoyage des ressources utilisées.
        /// </summary>
        /// <param name="disposing">true si les ressources managées doivent être supprimées ; sinon, false.</param>
        protected override void Dispose(bool disposing)
        {
            _logMonitor?.Dispose();
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Code généré par le Concepteur de composants

        /// <summary> 
        /// Méthode requise pour la prise en charge du concepteur - ne modifiez pas 
        /// le contenu de cette méthode avec l'éditeur de code.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(UserControlLogMonitor));
            this.toolStrip1 = new System.Windows.Forms.ToolStrip();
            this.toolStripButtonToggleStart = new System.Windows.Forms.ToolStripButton();
            this.toolStripButtonScroll2End = new System.Windows.Forms.ToolStripButton();
            this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
            this.toolStripLabelLastUpdate = new System.Windows.Forms.ToolStripLabel();
            this.textBoxLog = new System.Windows.Forms.TextBox();
            this.toolStrip1.SuspendLayout();
            this.SuspendLayout();
            // 
            // toolStrip1
            // 
            this.toolStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripButtonToggleStart,
            this.toolStripButtonScroll2End,
            this.toolStripSeparator1,
            this.toolStripLabelLastUpdate});
            this.toolStrip1.Location = new System.Drawing.Point(0, 0);
            this.toolStrip1.Name = "toolStrip1";
            this.toolStrip1.Size = new System.Drawing.Size(390, 25);
            this.toolStrip1.TabIndex = 0;
            this.toolStrip1.Text = "toolStrip1";
            // 
            // toolStripButtonToggleStart
            // 
            this.toolStripButtonToggleStart.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.toolStripButtonToggleStart.Image = ((System.Drawing.Image)(resources.GetObject("toolStripButtonToggleStart.Image")));
            this.toolStripButtonToggleStart.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.toolStripButtonToggleStart.Name = "toolStripButtonToggleStart";
            this.toolStripButtonToggleStart.Size = new System.Drawing.Size(35, 22);
            this.toolStripButtonToggleStart.Text = "Start";
            this.toolStripButtonToggleStart.Click += new System.EventHandler(this.toolStripButton1_Click);
            // 
            // toolStripButtonScroll2End
            // 
            this.toolStripButtonScroll2End.Checked = true;
            this.toolStripButtonScroll2End.CheckState = System.Windows.Forms.CheckState.Checked;
            this.toolStripButtonScroll2End.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.toolStripButtonScroll2End.Image = ((System.Drawing.Image)(resources.GetObject("toolStripButtonScroll2End.Image")));
            this.toolStripButtonScroll2End.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.toolStripButtonScroll2End.Name = "toolStripButtonScroll2End";
            this.toolStripButtonScroll2End.Size = new System.Drawing.Size(77, 22);
            this.toolStripButtonScroll2End.Text = "Scroll to end";
            this.toolStripButtonScroll2End.ToolTipText = "Scroll to End";
            // 
            // toolStripSeparator1
            // 
            this.toolStripSeparator1.Name = "toolStripSeparator1";
            this.toolStripSeparator1.Size = new System.Drawing.Size(6, 25);
            // 
            // toolStripLabelLastUpdate
            // 
            this.toolStripLabelLastUpdate.Name = "toolStripLabelLastUpdate";
            this.toolStripLabelLastUpdate.Size = new System.Drawing.Size(71, 22);
            this.toolStripLabelLastUpdate.Text = "Last update:";
            // 
            // textBoxLog
            // 
            this.textBoxLog.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.textBoxLog.Location = new System.Drawing.Point(3, 28);
            this.textBoxLog.Multiline = true;
            this.textBoxLog.Name = "textBoxLog";
            this.textBoxLog.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.textBoxLog.Size = new System.Drawing.Size(384, 303);
            this.textBoxLog.TabIndex = 1;
            // 
            // UserControlLogMonitor
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.textBoxLog);
            this.Controls.Add(this.toolStrip1);
            this.Name = "UserControlLogMonitor";
            this.Size = new System.Drawing.Size(390, 351);
            this.toolStrip1.ResumeLayout(false);
            this.toolStrip1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.ToolStrip toolStrip1;
        private System.Windows.Forms.ToolStripButton toolStripButtonToggleStart;
        private System.Windows.Forms.ToolStripButton toolStripButtonScroll2End;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator1;
        private System.Windows.Forms.ToolStripLabel toolStripLabelLastUpdate;
        private System.Windows.Forms.TextBox textBoxLog;
    }
}
