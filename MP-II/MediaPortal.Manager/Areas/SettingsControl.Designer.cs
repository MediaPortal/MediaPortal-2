#region Copyright (C) 2007-2008 Team MediaPortal

/*
 *  Copyright (C) 2007-2008 Team MediaPortal
 *  http://www.team-mediaportal.com
 *
 *  This file is part of MediaPortal II
 *
 *  MediaPortal II is free software: you can redistribute it and/or modify
 *  it under the terms of the GNU General Public License as published by
 *  the Free Software Foundation, either version 3 of the License, or
 *  (at your option) any later version.
 *
 *  MediaPortal II is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *  GNU General Public License for more details.
 *
 *  You should have received a copy of the GNU General Public License
 *  along with MediaPortal II.  If not, see <http://www.gnu.org/licenses/>.
 * 
 */

#endregion

namespace MediaPortal.Manager
{
  partial class SettingsControl
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

    #region Component Designer generated code

    /// <summary> 
    /// Required method for Designer support - do not modify 
    /// the contents of this method with the code editor.
    /// </summary>
    private void InitializeComponent()
    {
      this.components = new System.ComponentModel.Container();
      this.sections = new System.Windows.Forms.TreeView();
      this.sectionHeader = new System.Windows.Forms.Panel();
      this.sectionIcon = new System.Windows.Forms.PictureBox();
      this.sectionTitle = new System.Windows.Forms.Label();
      this.buttonReset = new System.Windows.Forms.Button();
      this.buttonSave = new System.Windows.Forms.Button();
      this.buttonApply = new System.Windows.Forms.Button();
      this.sectionSettings = new System.Windows.Forms.Panel();
      this.buttonResetAll = new System.Windows.Forms.Button();
      this.help = new System.Windows.Forms.ToolTip(this.components);
      this.sectionHeader.SuspendLayout();
      ((System.ComponentModel.ISupportInitialize)(this.sectionIcon)).BeginInit();
      this.SuspendLayout();
      // 
      // sections
      // 
      this.sections.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                  | System.Windows.Forms.AnchorStyles.Left)));
      this.sections.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
      this.sections.FullRowSelect = true;
      this.sections.HideSelection = false;
      this.sections.ItemHeight = 26;
      this.sections.LineColor = System.Drawing.Color.White;
      this.sections.Location = new System.Drawing.Point(0, 3);
      this.sections.Name = "sections";
      this.sections.ShowLines = false;
      this.sections.Size = new System.Drawing.Size(221, 467);
      this.sections.TabIndex = 3;
      this.sections.AfterSelect += new System.Windows.Forms.TreeViewEventHandler(this.sections_AfterSelect);
      // 
      // sectionHeader
      // 
      this.sectionHeader.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                  | System.Windows.Forms.AnchorStyles.Right)));
      this.sectionHeader.BackColor = System.Drawing.SystemColors.Highlight;
      this.sectionHeader.Controls.Add(this.sectionIcon);
      this.sectionHeader.Controls.Add(this.sectionTitle);
      this.sectionHeader.Location = new System.Drawing.Point(238, 3);
      this.sectionHeader.Name = "sectionHeader";
      this.sectionHeader.Size = new System.Drawing.Size(367, 59);
      this.sectionHeader.TabIndex = 4;
      // 
      // sectionIcon
      // 
      this.sectionIcon.Anchor = System.Windows.Forms.AnchorStyles.Right;
      this.sectionIcon.Location = new System.Drawing.Point(312, 5);
      this.sectionIcon.Name = "sectionIcon";
      this.sectionIcon.Size = new System.Drawing.Size(48, 48);
      this.sectionIcon.TabIndex = 1;
      this.sectionIcon.TabStop = false;
      // 
      // sectionTitle
      // 
      this.sectionTitle.AutoSize = true;
      this.sectionTitle.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
      this.sectionTitle.ForeColor = System.Drawing.SystemColors.ControlLight;
      this.sectionTitle.Location = new System.Drawing.Point(3, 22);
      this.sectionTitle.Name = "sectionTitle";
      this.sectionTitle.Size = new System.Drawing.Size(39, 16);
      this.sectionTitle.TabIndex = 0;
      this.sectionTitle.Text = "Test";
      // 
      // buttonReset
      // 
      this.buttonReset.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
      this.buttonReset.Enabled = false;
      this.buttonReset.Font = new System.Drawing.Font("Arial", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
      this.buttonReset.Location = new System.Drawing.Point(338, 487);
      this.buttonReset.Name = "buttonReset";
      this.buttonReset.Size = new System.Drawing.Size(86, 26);
      this.buttonReset.TabIndex = 8;
      this.buttonReset.Text = "Reset";
      this.buttonReset.UseVisualStyleBackColor = true;
      // 
      // buttonSave
      // 
      this.buttonSave.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
      this.buttonSave.Font = new System.Drawing.Font("Arial", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
      this.buttonSave.Location = new System.Drawing.Point(430, 487);
      this.buttonSave.Name = "buttonSave";
      this.buttonSave.Size = new System.Drawing.Size(86, 26);
      this.buttonSave.TabIndex = 7;
      this.buttonSave.Text = "Save";
      this.buttonSave.UseVisualStyleBackColor = true;
      this.buttonSave.Click += new System.EventHandler(this.buttonSave_Click);
      // 
      // buttonApply
      // 
      this.buttonApply.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
      this.buttonApply.Font = new System.Drawing.Font("Arial", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
      this.buttonApply.Location = new System.Drawing.Point(522, 487);
      this.buttonApply.Name = "buttonApply";
      this.buttonApply.Size = new System.Drawing.Size(86, 26);
      this.buttonApply.TabIndex = 6;
      this.buttonApply.Text = "Apply";
      this.buttonApply.UseVisualStyleBackColor = true;
      this.buttonApply.Click += new System.EventHandler(this.buttonApply_Click);
      // 
      // sectionSettings
      // 
      this.sectionSettings.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                  | System.Windows.Forms.AnchorStyles.Left)
                  | System.Windows.Forms.AnchorStyles.Right)));
      this.sectionSettings.AutoScroll = true;
      this.sectionSettings.Location = new System.Drawing.Point(238, 68);
      this.sectionSettings.Name = "sectionSettings";
      this.sectionSettings.Size = new System.Drawing.Size(367, 402);
      this.sectionSettings.TabIndex = 9;
      // 
      // buttonResetAll
      // 
      this.buttonResetAll.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
      this.buttonResetAll.Enabled = false;
      this.buttonResetAll.Font = new System.Drawing.Font("Arial", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
      this.buttonResetAll.Location = new System.Drawing.Point(0, 487);
      this.buttonResetAll.Name = "buttonResetAll";
      this.buttonResetAll.Size = new System.Drawing.Size(86, 26);
      this.buttonResetAll.TabIndex = 11;
      this.buttonResetAll.Text = "Reset All";
      this.buttonResetAll.UseVisualStyleBackColor = true;
      // 
      // help
      // 
      this.help.AutoPopDelay = 5000;
      this.help.InitialDelay = 1000;
      this.help.ReshowDelay = 100;
      // 
      // SettingsControl
      // 
      this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
      this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
      this.Controls.Add(this.buttonResetAll);
      this.Controls.Add(this.sectionSettings);
      this.Controls.Add(this.buttonReset);
      this.Controls.Add(this.buttonSave);
      this.Controls.Add(this.buttonApply);
      this.Controls.Add(this.sectionHeader);
      this.Controls.Add(this.sections);
      this.Name = "SettingsControl";
      this.RightToLeft = System.Windows.Forms.RightToLeft.No;
      this.Size = new System.Drawing.Size(608, 513);
      this.sectionHeader.ResumeLayout(false);
      this.sectionHeader.PerformLayout();
      ((System.ComponentModel.ISupportInitialize)(this.sectionIcon)).EndInit();
      this.ResumeLayout(false);

    }

    #endregion

    private System.Windows.Forms.TreeView sections;
    private System.Windows.Forms.Panel sectionHeader;
    private System.Windows.Forms.Label sectionTitle;
    private System.Windows.Forms.Button buttonReset;
    private System.Windows.Forms.Button buttonSave;
    private System.Windows.Forms.Button buttonApply;
    private System.Windows.Forms.Panel sectionSettings;
    private System.Windows.Forms.Button buttonResetAll;
    private System.Windows.Forms.ToolTip help;
    private System.Windows.Forms.PictureBox sectionIcon;
  }
}
