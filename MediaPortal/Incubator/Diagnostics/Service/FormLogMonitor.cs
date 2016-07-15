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

using MediaPortal.UiComponents.Diagnostics.Service.UserControls;
using System;
using System.Windows.Forms;

namespace MediaPortal.UiComponents.Diagnostics.Service
{
    public partial class FormLogMonitor : Form
    {

        #region Public Constructors

        public FormLogMonitor()
        {
            InitializeComponent();
        }

        #endregion Public Constructors

        #region Public Methods

        /// <summary>
        /// Add logfile to watch: Create a new TAB with log watcher control
        /// </summary>
        /// <param name="logfile">logfile to watch</param>
        public void AddLog(string logfile)
        {
            string filename = new System.IO.FileInfo(logfile).Name;
            TabPage page = new TabPage(filename);
            UserControlLogMonitor uc = new UserControlLogMonitor();
            uc.FileName = logfile;
            uc.Dock = DockStyle.Fill;
            uc.Location = new System.Drawing.Point(1, 1);
            uc.Name = filename;
            page.Controls.Add(uc);

            this.tabControlContainer.TabPages.Add(page);
        }

        #endregion Public Methods

        private void FormLogMonitor_Load(object sender, EventArgs e)
        {
            this.Location = new System.Drawing.Point(1,1);
            this.Height = Screen.FromControl(this).WorkingArea.Height;
            this.Width = Screen.FromControl(this).WorkingArea.Width / 3;
        }
    }
}