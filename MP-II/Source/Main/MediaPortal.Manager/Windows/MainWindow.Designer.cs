#region Copyright (C) 2007-2009 Team MediaPortal

/*
 *  Copyright (C) 2007-2009 Team MediaPortal
 *  http://www.team-mediaportal.com
 *
 *  This file is part of MediaPortal 2
 *
 *  MediaPortal 2 is free software: you can redistribute it and/or modify
 *  it under the terms of the GNU General Public License as published by
 *  the Free Software Foundation, either version 3 of the License, or
 *  (at your option) any later version.
 *
 *  MediaPortal 2 is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *  GNU General Public License for more details.
 *
 *  You should have received a copy of the GNU General Public License
 *  along with MediaPortal 2.  If not, see <http://www.gnu.org/licenses/>.
 * 
 */

#endregion

namespace MediaPortal.Manager
{
  partial class MainWindow
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
      this.areaSettings = new System.Windows.Forms.Button();
      this.areaLogs = new System.Windows.Forms.Button();
      this.areaControls = new System.Windows.Forms.Panel();
      this.SuspendLayout();
      // 
      // areaSettings
      // 
      this.areaSettings.BackColor = System.Drawing.SystemColors.Highlight;
      this.areaSettings.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
      this.areaSettings.ForeColor = System.Drawing.SystemColors.ControlLightLight;
      this.areaSettings.Location = new System.Drawing.Point(12, 12);
      this.areaSettings.Name = "areaSettings";
      this.areaSettings.Size = new System.Drawing.Size(119, 23);
      this.areaSettings.TabIndex = 7;
      this.areaSettings.Text = "Settings";
      this.areaSettings.UseVisualStyleBackColor = false;
      // 
      // areaLogs
      // 
      this.areaLogs.BackColor = System.Drawing.SystemColors.Control;
      this.areaLogs.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
      this.areaLogs.ForeColor = System.Drawing.SystemColors.ControlText;
      this.areaLogs.Location = new System.Drawing.Point(137, 12);
      this.areaLogs.Name = "areaLogs";
      this.areaLogs.Size = new System.Drawing.Size(119, 23);
      this.areaLogs.TabIndex = 9;
      this.areaLogs.Text = "Logs";
      this.areaLogs.UseVisualStyleBackColor = false;
      // 
      // areaControls
      // 
      this.areaControls.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                  | System.Windows.Forms.AnchorStyles.Left)
                  | System.Windows.Forms.AnchorStyles.Right)));
      this.areaControls.Location = new System.Drawing.Point(12, 41);
      this.areaControls.Name = "areaControls";
      this.areaControls.Size = new System.Drawing.Size(608, 513);
      this.areaControls.TabIndex = 10;
      // 
      // MainWindow
      // 
      this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
      this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
      this.ClientSize = new System.Drawing.Size(632, 566);
      this.Controls.Add(this.areaControls);
      this.Controls.Add(this.areaLogs);
      this.Controls.Add(this.areaSettings);
      this.KeyPreview = true;
      this.MinimumSize = new System.Drawing.Size(600, 36);
      this.Name = "MainWindow";
      this.RightToLeft = System.Windows.Forms.RightToLeft.No;
      this.Text = "MediaPortal 2 - Manager";
      this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.MainWindow_FormClosing);
      this.KeyDown += new System.Windows.Forms.KeyEventHandler(this.MainWindow_KeyDown);
      this.ResumeLayout(false);

    }

    #endregion

    private System.Windows.Forms.Button areaSettings;
    private System.Windows.Forms.Button areaLogs;
    private System.Windows.Forms.Panel areaControls;
  }
}

