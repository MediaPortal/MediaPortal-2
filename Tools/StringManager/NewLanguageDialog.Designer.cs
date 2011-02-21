#region Copyright (C) 2007-2008 Team MediaPortal

/*
    Copyright (C) 2007-2008 Team MediaPortal
    http://www.team-mediaportal.com
 
    This file is part of MediaPortal II

    MediaPortal II is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    MediaPortal II is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with MediaPortal II.  If not, see <http://www.gnu.org/licenses/>.
*/

#endregion

namespace MediaPortal.Tools.StringManager
{
  partial class NewLanguageDialog
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
      this.lbLanguageList = new System.Windows.Forms.ListBox();
      this.btnCancel = new System.Windows.Forms.Button();
      this.btnOk = new System.Windows.Forms.Button();
      this.cbRegional = new System.Windows.Forms.CheckBox();
      this.SuspendLayout();
      // 
      // lbLanguageList
      // 
      this.lbLanguageList.FormattingEnabled = true;
      this.lbLanguageList.Location = new System.Drawing.Point(22, 28);
      this.lbLanguageList.Name = "lbLanguageList";
      this.lbLanguageList.Size = new System.Drawing.Size(248, 160);
      this.lbLanguageList.TabIndex = 0;
      // 
      // btnCancel
      // 
      this.btnCancel.Location = new System.Drawing.Point(195, 231);
      this.btnCancel.Name = "btnCancel";
      this.btnCancel.Size = new System.Drawing.Size(75, 23);
      this.btnCancel.TabIndex = 1;
      this.btnCancel.Text = "Cancel";
      this.btnCancel.UseVisualStyleBackColor = true;
      this.btnCancel.Click += new System.EventHandler(this.btnCancel_Click);
      // 
      // btnOk
      // 
      this.btnOk.Location = new System.Drawing.Point(22, 231);
      this.btnOk.Name = "btnOk";
      this.btnOk.Size = new System.Drawing.Size(75, 23);
      this.btnOk.TabIndex = 3;
      this.btnOk.Text = "Ok";
      this.btnOk.UseVisualStyleBackColor = true;
      this.btnOk.Click += new System.EventHandler(this.btnOk_Click);
      // 
      // cbRegional
      // 
      this.cbRegional.AutoSize = true;
      this.cbRegional.Location = new System.Drawing.Point(22, 195);
      this.cbRegional.Name = "cbRegional";
      this.cbRegional.Size = new System.Drawing.Size(154, 17);
      this.cbRegional.TabIndex = 4;
      this.cbRegional.Text = "Show Regional Languages";
      this.cbRegional.UseVisualStyleBackColor = true;
      this.cbRegional.CheckStateChanged += new System.EventHandler(this.cbRegional_CheckStateChanged);
      // 
      // NewLanguageDialog
      // 
      this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
      this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
      this.ClientSize = new System.Drawing.Size(292, 266);
      this.Controls.Add(this.cbRegional);
      this.Controls.Add(this.btnOk);
      this.Controls.Add(this.btnCancel);
      this.Controls.Add(this.lbLanguageList);
      this.Name = "NewLanguageDialog";
      this.Text = "Select New Language";
      this.ResumeLayout(false);
      this.PerformLayout();

    }

    #endregion

    private System.Windows.Forms.ListBox lbLanguageList;
    private System.Windows.Forms.Button btnCancel;
    private System.Windows.Forms.Button btnOk;
    private System.Windows.Forms.CheckBox cbRegional;
  }
}
