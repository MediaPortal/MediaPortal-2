#region Copyright (C) 2007-2010 Team MediaPortal

/*
    Copyright (C) 2007-2010 Team MediaPortal
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
  partial class BaseScreen
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
      this.tbTitle = new System.Windows.Forms.TextBox();
      this.tbDetails = new System.Windows.Forms.TextBox();
      this.pbIcon = new System.Windows.Forms.PictureBox();
      this.pbLogo = new System.Windows.Forms.PictureBox();
      ((System.ComponentModel.ISupportInitialize)(this.pbIcon)).BeginInit();
      ((System.ComponentModel.ISupportInitialize)(this.pbLogo)).BeginInit();
      this.SuspendLayout();
      // 
      // tbTitle
      // 
      this.tbTitle.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                  | System.Windows.Forms.AnchorStyles.Right)));
      this.tbTitle.BorderStyle = System.Windows.Forms.BorderStyle.None;
      this.tbTitle.Font = new System.Drawing.Font("Microsoft Sans Serif", 20.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
      this.tbTitle.Location = new System.Drawing.Point(81, 93);
      this.tbTitle.Multiline = true;
      this.tbTitle.Name = "tbTitle";
      this.tbTitle.Size = new System.Drawing.Size(382, 42);
      this.tbTitle.TabIndex = 2;
      this.tbTitle.Text = "Title";
      // 
      // tbDetails
      // 
      this.tbDetails.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                  | System.Windows.Forms.AnchorStyles.Left)
                  | System.Windows.Forms.AnchorStyles.Right)));
      this.tbDetails.BorderStyle = System.Windows.Forms.BorderStyle.None;
      this.tbDetails.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
      this.tbDetails.Location = new System.Drawing.Point(93, 141);
      this.tbDetails.Multiline = true;
      this.tbDetails.Name = "tbDetails";
      this.tbDetails.Size = new System.Drawing.Size(369, 116);
      this.tbDetails.TabIndex = 4;
      this.tbDetails.Text = "Details";
      // 
      // pbIcon
      // 
      this.pbIcon.BackgroundImageLayout = System.Windows.Forms.ImageLayout.None;
      this.pbIcon.Image = global::MediaPortal.Utilities.Properties.Resources.info;
      this.pbIcon.Location = new System.Drawing.Point(12, 93);
      this.pbIcon.Name = "pbIcon";
      this.pbIcon.Size = new System.Drawing.Size(63, 66);
      this.pbIcon.TabIndex = 3;
      this.pbIcon.TabStop = false;
      // 
      // pbLogo
      // 
      this.pbLogo.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                  | System.Windows.Forms.AnchorStyles.Right)));
      this.pbLogo.BackColor = System.Drawing.SystemColors.Window;
      this.pbLogo.Image = global::MediaPortal.Utilities.Properties.Resources.mplogo;
      this.pbLogo.Location = new System.Drawing.Point(12, 12);
      this.pbLogo.Name = "pbLogo";
      this.pbLogo.Size = new System.Drawing.Size(452, 73);
      this.pbLogo.TabIndex = 1;
      this.pbLogo.TabStop = false;
      // 
      // BaseScreen
      // 
      this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
      this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
      this.BackColor = System.Drawing.SystemColors.Window;
      this.ClientSize = new System.Drawing.Size(474, 303);
      this.Controls.Add(this.tbDetails);
      this.Controls.Add(this.pbIcon);
      this.Controls.Add(this.tbTitle);
      this.Controls.Add(this.pbLogo);
      this.Name = "BaseScreen";
      this.Text = "Info";
      ((System.ComponentModel.ISupportInitialize)(this.pbIcon)).EndInit();
      ((System.ComponentModel.ISupportInitialize)(this.pbLogo)).EndInit();
      this.ResumeLayout(false);
      this.PerformLayout();

    }

    #endregion

    private System.Windows.Forms.PictureBox pbLogo;
    private System.Windows.Forms.TextBox tbTitle;
    private System.Windows.Forms.PictureBox pbIcon;
    private System.Windows.Forms.TextBox tbDetails;
  }
}

