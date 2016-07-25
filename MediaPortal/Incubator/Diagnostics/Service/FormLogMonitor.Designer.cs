namespace MediaPortal.UiComponents.Diagnostics.Service
{
    partial class FormLogMonitor
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
      System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FormLogMonitor));
      this.tabControlContainer = new System.Windows.Forms.TabControl();
      this.SuspendLayout();
      // 
      // tabControlContainer
      // 
      this.tabControlContainer.Dock = System.Windows.Forms.DockStyle.Fill;
      this.tabControlContainer.Location = new System.Drawing.Point(0, 0);
      this.tabControlContainer.Name = "tabControlContainer";
      this.tabControlContainer.SelectedIndex = 0;
      this.tabControlContainer.Size = new System.Drawing.Size(394, 350);
      this.tabControlContainer.TabIndex = 1;
      // 
      // FormLogMonitor
      // 
      this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
      this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
      this.ClientSize = new System.Drawing.Size(394, 350);
      this.Controls.Add(this.tabControlContainer);
      this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
      this.Name = "FormLogMonitor";
      this.Text = "Logs";
      this.Load += new System.EventHandler(this.FormLogMonitor_Load);
      this.ResumeLayout(false);

        }

        #endregion
        private System.Windows.Forms.TabControl tabControlContainer;
    }
}