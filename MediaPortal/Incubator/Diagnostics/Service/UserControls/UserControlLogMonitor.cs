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

using System;
using System.Windows.Forms;

namespace MediaPortal.UiComponents.Diagnostics.Service.UserControls
{
    public partial class UserControlLogMonitor : UserControl
    {

        #region Private Fields

        private LogMonitor _logMonitor;
        private bool _scrollToend = true;

        #endregion Private Fields

        #region Public Constructors + Destructors

        public UserControlLogMonitor()
        {
            InitializeComponent();
            this.Load += UserControlLogMonitor_Load;
        }

        #endregion Public Constructors + Destructors

        #region Public Properties

        /// <summary>
        /// LogFile to watch
        /// </summary>
        public string FileName { get; set; }

        #endregion Public Properties

        #region Private Methods

        private void _logMonitor_OnNewLogs(object sender, LogMonitor.NewLogsEventArgs e)
        {
            this.Invoke(new Action(() =>
            {
                treeViewLog.SuspendLayout();
                SetLogToGUI(e.Logs);
                treeViewLog.ResumeLayout();
                SetLastUpdateToGUI();
            }
                                        ));
        }

        private void _logMonitor_OnReseted(object sender, EventArgs e)
        {
            this.Invoke(new Action(() =>
            {
                try
                {
                    treeViewLog.Nodes.Clear();
                    SetLastUpdateToGUI();
                }
                catch { }
            }
            ));
        }

        private void SetLastUpdateToGUI()
        {
            try
            {
                toolStripLabelLastUpdate.Text = "Last update: " + DateTime.Now.TimeOfDay.ToString();
            }
            catch { }
        }

        private void SetLogToGUI(string logs)
        {
            try
            {
                string[] tlines = logs.Split('\n');
                int idx = 0;
                string line = tlines[0];
                TreeNode parentNode = null;
                while (!string.IsNullOrEmpty(line) && idx < tlines.Length - 1)
                {
                    line = tlines[idx];

                    if (line.Contains("["))
                    {
                        parentNode = treeViewLog.Nodes.Add(line);
                        if (line.Contains("[ERROR")) parentNode.ForeColor = System.Drawing.Color.Red;
                        else if (line.Contains("[WARN")) parentNode.ForeColor = System.Drawing.Color.Orange;
                    }
                    else if (parentNode != null)
                    {
                        TreeNode subNode = parentNode.Nodes.Add(line);
                        subNode.ForeColor = parentNode.ForeColor;
                    }
                    idx++;
                }
                parentNode = null;

                tlines = null;

                if (_scrollToend)
                    treeViewLog.Nodes[treeViewLog.Nodes.Count - 1].EnsureVisible();
            }
            catch (Exception ex)
            {
            }
        }

        private void SetState()
        {
            switch (_logMonitor.State)
            {
                case LogMonitor.LogHandlerState.Stopped:
                case LogMonitor.LogHandlerState.Pausing:
                    toolStripButtonToggleStart.Text = "Start";
                    this.toolStripButtonToggleStart.Image = global::MediaPortal.UiComponents.Diagnostics.Properties.Resources.play;
                    break;

                case LogMonitor.LogHandlerState.Started:
                    this.toolStripButtonToggleStart.Image = global::MediaPortal.UiComponents.Diagnostics.Properties.Resources.pause;
                    toolStripButtonToggleStart.Text = "Pause";
                    break;
            }
        }

        private void toolStripButton1_Click(object sender, EventArgs e)
        {
            if (!string.IsNullOrEmpty(FileName)) _logMonitor.FileName = FileName;

            switch (_logMonitor.State)
            {
                case LogMonitor.LogHandlerState.Stopped:
                case LogMonitor.LogHandlerState.Pausing:
                    _logMonitor.Start();
                    break;

                case LogMonitor.LogHandlerState.Started:
                    _logMonitor.Pause();
                    break;
            }

            SetState();
        }

        private void toolStripButtonScroll2End_Click(object sender, EventArgs e)
        {
            _scrollToend = !_scrollToend;

            if (_scrollToend)
                this.toolStripButtonScroll2End.Image = global::MediaPortal.UiComponents.Diagnostics.Properties.Resources.scroll;
            else
                this.toolStripButtonScroll2End.Image = global::MediaPortal.UiComponents.Diagnostics.Properties.Resources.dontscroll;
        }

        private void UserControlLogMonitor_Load(object sender, EventArgs e)
        {
            _logMonitor = new LogMonitor();
            _logMonitor.OnNewLogs += _logMonitor_OnNewLogs;
            _logMonitor.OnReseted += _logMonitor_OnReseted;
        }

        #endregion Private Methods
    }
}