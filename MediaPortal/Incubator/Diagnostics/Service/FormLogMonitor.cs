#region Copyright (C) 2007-2015 Team MediaPortal

/*
    Copyright (C) 2007-2015 Team MediaPortal
    http://www.team-mediaportal.com

    This file is part of MediaPortal 2

    MediaPortal 2 is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    MediaPortal 2 is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with MediaPortal 2. If not, see <http://www.gnu.org/licenses/>.
*/

#endregion

using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using MediaPortal.UiComponents.Diagnostics.Service.UserControls;

namespace MediaPortal.UiComponents.Diagnostics.Service
{
  public partial class FormLogMonitor : Form
  {

    #region Public Constructors + Destructors

    public FormLogMonitor()
    {
      InitializeComponent();
    }

    #endregion Public Constructors + Destructors

    #region Public Methods

    /// <summary>
    /// Add logfile to watch: Create a new TAB with log watcher control
    /// </summary>
    /// <param name="logfile">logfile to watch</param>
    public void AddLog(string logfile)
    {
      string filename = new FileInfo(logfile).Name;
      TabPage page = new TabPage(filename);
      UserControlLogMonitor uc = new UserControlLogMonitor
      {
        FileName = logfile,
        Dock = DockStyle.Fill,
        Location = new Point(1, 1),
        Name = filename
      };
      page.Controls.Add(uc);

      tabControlContainer.TabPages.Add(page);
    }

    #endregion Public Methods

    #region Private Methods

    private void FormLogMonitor_Load(object sender, EventArgs e)
    {
      Location = new Point(1, 1);
      Height = Screen.FromControl(this).WorkingArea.Height;
      Width = Screen.FromControl(this).WorkingArea.Width / 3;
    }

    #endregion Private Methods
  }
}
