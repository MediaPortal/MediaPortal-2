#region Copyright (C) 2007-2010 Team MediaPortal

/*
    Copyright (C) 2007-2010 Team MediaPortal
    http://www.team-mediaportal.com
 
    This file is part of MediaPortal 2

    MediaPortal 2 is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    MediaPortal 2 is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with MediaPortal 2.  If not, see <http://www.gnu.org/licenses/>.
*/

#endregion

namespace MediaPortal.UI.SkinEngine.GUI
{
  partial class MainForm
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
      this.components = new System.ComponentModel.Container();
      this.timer = new System.Windows.Forms.Timer(this.components);
      this.SuspendLayout();
      // 
      // timer
      // 
      this.timer.Enabled = true;
      this.timer.Tick += new System.EventHandler(this.timer_Tick);
      // 
      // MainForm
      // 
      this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
      this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
      this.BackColor = System.Drawing.Color.Black;
      this.ClientSize = new System.Drawing.Size(292, 266);
      this.Name = "MainForm";
      this.Text = "MediaPortal 2";
      this.Icon = new System.Drawing.Icon(System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceStream("MediaPortal.UI.SkinEngine.GUI.MP2.ico"));
      this.Deactivate += new System.EventHandler(this.MainForm_Deactivate);
      this.MouseUp += new System.Windows.Forms.MouseEventHandler(this.MainForm_MouseUp);
      this.MouseClick += new System.Windows.Forms.MouseEventHandler(this.MainForm_MouseClick);
      this.Activated += new System.EventHandler(this.MainForm_Activated);
      this.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.MainForm_KeyPress);
      this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.MainForm_FormClosing);
      this.MouseMove += new System.Windows.Forms.MouseEventHandler(this.MainForm_MouseMove);
      this.KeyDown += new System.Windows.Forms.KeyEventHandler(this.MainForm_KeyDown);
      this.ResumeLayout(false);

    }

    #endregion

    private System.Windows.Forms.Timer timer;
  }
}

