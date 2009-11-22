#region Copyright (C) 2007-2009 Team MediaPortal

/*
    Copyright (C) 2007-2009 Team MediaPortal
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

namespace MediaPortal.Utilities.Screens
{
  partial class YesNoDialogScreen
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
      this.btnYes = new System.Windows.Forms.Button();
      this.btnNo = new System.Windows.Forms.Button();
      this.SuspendLayout();
      // 
      // btnYes
      // 
      this.btnYes.Location = new System.Drawing.Point(306, 268);
      this.btnYes.Name = "btnYes";
      this.btnYes.Size = new System.Drawing.Size(75, 23);
      this.btnYes.TabIndex = 5;
      this.btnYes.Text = "&Yes";
      this.btnYes.UseVisualStyleBackColor = true;
      this.btnYes.Click += new System.EventHandler(this.btnYes_Click);
      // 
      // btnNo
      // 
      this.btnNo.DialogResult = System.Windows.Forms.DialogResult.Cancel;
      this.btnNo.Location = new System.Drawing.Point(387, 268);
      this.btnNo.Name = "btnNo";
      this.btnNo.Size = new System.Drawing.Size(75, 23);
      this.btnNo.TabIndex = 6;
      this.btnNo.Text = "&No";
      this.btnNo.UseVisualStyleBackColor = true;
      this.btnNo.Click += new System.EventHandler(this.btnNo_Click);
      // 
      // YesNoDialogScreen
      // 
      this.AcceptButton = this.btnYes;
      this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
      this.CancelButton = this.btnNo;
      this.ClientSize = new System.Drawing.Size(474, 303);
      this.Controls.Add(this.btnNo);
      this.Controls.Add(this.btnYes);
      this.Name = "YesNoDialogScreen";
      this.Controls.SetChildIndex(this.btnYes, 0);
      this.Controls.SetChildIndex(this.btnNo, 0);
      this.ResumeLayout(false);
      this.PerformLayout();

    }

    #endregion

    private System.Windows.Forms.Button btnYes;
    private System.Windows.Forms.Button btnNo;
  }
}
