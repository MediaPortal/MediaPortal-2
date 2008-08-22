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
      System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(SettingsControl));
      this._treeSections = new System.Windows.Forms.TreeView();
      this._sectionHeader = new System.Windows.Forms.Panel();
      this._picSectionIcon = new System.Windows.Forms.PictureBox();
      this._lblSectionTitle = new System.Windows.Forms.Label();
      this._btnReset = new System.Windows.Forms.Button();
      this._btnSave = new System.Windows.Forms.Button();
      this._btnApply = new System.Windows.Forms.Button();
      this._panelSectionSettings = new System.Windows.Forms.Panel();
      this._btnResetAll = new System.Windows.Forms.Button();
      this._help = new System.Windows.Forms.ToolTip(this.components);
      this._btnSearch = new System.Windows.Forms.Button();
      this._txtSearch = new System.Windows.Forms.TextBox();
      this._sectionHeader.SuspendLayout();
      ((System.ComponentModel.ISupportInitialize)(this._picSectionIcon)).BeginInit();
      this.SuspendLayout();
      // 
      // _treeSections
      // 
      this._treeSections.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                  | System.Windows.Forms.AnchorStyles.Left)));
      this._treeSections.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
      this._treeSections.FullRowSelect = true;
      this._treeSections.HideSelection = false;
      this._treeSections.ItemHeight = 26;
      this._treeSections.LineColor = System.Drawing.Color.White;
      this._treeSections.Location = new System.Drawing.Point(0, 0);
      this._treeSections.Name = "_treeSections";
      this._treeSections.ShowLines = false;
      this._treeSections.Size = new System.Drawing.Size(221, 444);
      this._treeSections.TabIndex = 3;
      this._treeSections.AfterSelect += new System.Windows.Forms.TreeViewEventHandler(this.sections_AfterSelect);
      // 
      // _sectionHeader
      // 
      this._sectionHeader.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                  | System.Windows.Forms.AnchorStyles.Right)));
      this._sectionHeader.BackColor = System.Drawing.SystemColors.Highlight;
      this._sectionHeader.Controls.Add(this._picSectionIcon);
      this._sectionHeader.Controls.Add(this._lblSectionTitle);
      this._sectionHeader.Location = new System.Drawing.Point(238, 3);
      this._sectionHeader.Name = "_sectionHeader";
      this._sectionHeader.Size = new System.Drawing.Size(367, 59);
      this._sectionHeader.TabIndex = 4;
      // 
      // _picSectionIcon
      // 
      this._picSectionIcon.Anchor = System.Windows.Forms.AnchorStyles.Right;
      this._picSectionIcon.Location = new System.Drawing.Point(312, 5);
      this._picSectionIcon.Name = "_picSectionIcon";
      this._picSectionIcon.Size = new System.Drawing.Size(48, 48);
      this._picSectionIcon.TabIndex = 1;
      this._picSectionIcon.TabStop = false;
      // 
      // _lblSectionTitle
      // 
      this._lblSectionTitle.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
      this._lblSectionTitle.ForeColor = System.Drawing.SystemColors.ControlLight;
      this._lblSectionTitle.Location = new System.Drawing.Point(3, 22);
      this._lblSectionTitle.Name = "_lblSectionTitle";
      this._lblSectionTitle.Size = new System.Drawing.Size(280, 16);
      this._lblSectionTitle.TabIndex = 0;
      this._lblSectionTitle.Text = "Project";
      // 
      // _btnReset
      // 
      this._btnReset.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
      this._btnReset.Enabled = false;
      this._btnReset.Font = new System.Drawing.Font("Arial", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
      this._btnReset.Location = new System.Drawing.Point(338, 487);
      this._btnReset.Name = "_btnReset";
      this._btnReset.Size = new System.Drawing.Size(86, 26);
      this._btnReset.TabIndex = 8;
      this._btnReset.Text = "Reset";
      this._btnReset.UseVisualStyleBackColor = true;
      // 
      // _btnSave
      // 
      this._btnSave.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
      this._btnSave.Font = new System.Drawing.Font("Arial", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
      this._btnSave.Location = new System.Drawing.Point(430, 487);
      this._btnSave.Name = "_btnSave";
      this._btnSave.Size = new System.Drawing.Size(86, 26);
      this._btnSave.TabIndex = 7;
      this._btnSave.Text = "Save";
      this._btnSave.UseVisualStyleBackColor = true;
      this._btnSave.Click += new System.EventHandler(this.buttonSave_Click);
      // 
      // _btnApply
      // 
      this._btnApply.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
      this._btnApply.Font = new System.Drawing.Font("Arial", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
      this._btnApply.Location = new System.Drawing.Point(522, 487);
      this._btnApply.Name = "_btnApply";
      this._btnApply.Size = new System.Drawing.Size(86, 26);
      this._btnApply.TabIndex = 6;
      this._btnApply.Text = "Apply";
      this._btnApply.UseVisualStyleBackColor = true;
      this._btnApply.Click += new System.EventHandler(this.buttonApply_Click);
      // 
      // _panelSectionSettings
      // 
      this._panelSectionSettings.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                  | System.Windows.Forms.AnchorStyles.Left)
                  | System.Windows.Forms.AnchorStyles.Right)));
      this._panelSectionSettings.AutoScroll = true;
      this._panelSectionSettings.Location = new System.Drawing.Point(238, 81);
      this._panelSectionSettings.Name = "_panelSectionSettings";
      this._panelSectionSettings.Size = new System.Drawing.Size(367, 389);
      this._panelSectionSettings.TabIndex = 9;
      this._panelSectionSettings.SizeChanged += new System.EventHandler(this.panelSectionSettings_SizeChanged);
      // 
      // _btnResetAll
      // 
      this._btnResetAll.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
      this._btnResetAll.Enabled = false;
      this._btnResetAll.Font = new System.Drawing.Font("Arial", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
      this._btnResetAll.Location = new System.Drawing.Point(0, 487);
      this._btnResetAll.Name = "_btnResetAll";
      this._btnResetAll.Size = new System.Drawing.Size(86, 26);
      this._btnResetAll.TabIndex = 11;
      this._btnResetAll.Text = "Reset All";
      this._btnResetAll.UseVisualStyleBackColor = true;
      // 
      // _help
      // 
      this._help.AutoPopDelay = 5000;
      this._help.InitialDelay = 1000;
      this._help.ReshowDelay = 100;
      this._help.ShowAlways = true;
      // 
      // _btnSearch
      // 
      this._btnSearch.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
      this._btnSearch.FlatStyle = System.Windows.Forms.FlatStyle.Popup;
      this._btnSearch.ForeColor = System.Drawing.SystemColors.ButtonFace;
      this._btnSearch.Image = ((System.Drawing.Image)(resources.GetObject("_btnSearch.Image")));
      this._btnSearch.Location = new System.Drawing.Point(201, 450);
      this._btnSearch.Name = "_btnSearch";
      this._btnSearch.Size = new System.Drawing.Size(20, 20);
      this._btnSearch.TabIndex = 13;
      this._btnSearch.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageAboveText;
      this._help.SetToolTip(this._btnSearch, "Find match");
      this._btnSearch.UseVisualStyleBackColor = true;
      this._btnSearch.Click += new System.EventHandler(this.btnSearch_Click);
      // 
      // _txtSearch
      // 
      this._txtSearch.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
      this._txtSearch.Location = new System.Drawing.Point(0, 450);
      this._txtSearch.Name = "_txtSearch";
      this._txtSearch.Size = new System.Drawing.Size(204, 20);
      this._txtSearch.TabIndex = 12;
      this._txtSearch.TextChanged += new System.EventHandler(this.txtSearch_TextChanged);
      this._txtSearch.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.txtSearch_KeyPress);
      // 
      // SettingsControl
      // 
      this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
      this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
      this.Controls.Add(this._btnSearch);
      this.Controls.Add(this._btnResetAll);
      this.Controls.Add(this._txtSearch);
      this.Controls.Add(this._panelSectionSettings);
      this.Controls.Add(this._btnReset);
      this.Controls.Add(this._btnSave);
      this.Controls.Add(this._btnApply);
      this.Controls.Add(this._sectionHeader);
      this.Controls.Add(this._treeSections);
      this.Name = "SettingsControl";
      this.RightToLeft = System.Windows.Forms.RightToLeft.No;
      this.Size = new System.Drawing.Size(608, 513);
      this._sectionHeader.ResumeLayout(false);
      ((System.ComponentModel.ISupportInitialize)(this._picSectionIcon)).EndInit();
      this.ResumeLayout(false);
      this.PerformLayout();

    }

    #endregion

    private System.Windows.Forms.TreeView _treeSections;
    private System.Windows.Forms.Panel _sectionHeader;
    private System.Windows.Forms.Label _lblSectionTitle;
    private System.Windows.Forms.Button _btnReset;
    private System.Windows.Forms.Button _btnSave;
    private System.Windows.Forms.Button _btnApply;
    private System.Windows.Forms.Panel _panelSectionSettings;
    private System.Windows.Forms.Button _btnResetAll;
    private System.Windows.Forms.ToolTip _help;
    private System.Windows.Forms.PictureBox _picSectionIcon;
    private System.Windows.Forms.TextBox _txtSearch;
    private System.Windows.Forms.Button _btnSearch;
  }
}
