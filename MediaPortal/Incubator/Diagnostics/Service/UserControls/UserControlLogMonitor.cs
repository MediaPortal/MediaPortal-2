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
    public partial class UserControlLogMonitor : System.Windows.Forms.UserControl
    {

        #region Private Fields

        private LogMonitor _logMonitor;

        #endregion Private Fields

        #region Public Constructors

        public UserControlLogMonitor()
        {
            InitializeComponent();
            this.Load += UserControlLogMonitor_Load;
        }

        #endregion Public Constructors

        #region Public Properties

        public string FileName { get; set; }

        #endregion Public Properties

        #region Private Methods

        private void _logMonitor_OnNewLogs(object sender, LogMonitor.NewLogsEventArgs e)
        {
            this.Invoke(new Action(() =>
                                        {
                                            textBoxLog.SuspendLayout();
                                            if (toolStripButtonScroll2End.CheckState == CheckState.Checked)
                                                textBoxLog.AppendText(e.Logs);
                                            else
                                                textBoxLog.Text += e.Logs;

                                            textBoxLog.ResumeLayout();
                                            SetLastUpdate();
                                        }
                                        ));
        }

        private void _logMonitor_OnReseted(object sender, EventArgs e)
        {
            this.Invoke(new Action(() =>
            {
                textBoxLog.Text = string.Empty;
                SetLastUpdate();
            }
            ));
        }

        private void SetLastUpdate()
        {
            toolStripLabelLastUpdate.Text = "Last update: " + DateTime.Now.TimeOfDay.ToString();
        }

        private void SetState()
        {
            switch (_logMonitor.State)
            {
                case LogMonitor.LogHandlerState.Stopped:
                case LogMonitor.LogHandlerState.Pausing:
                    toolStripButtonToggleStart.Text = "Start";
                    break;

                case LogMonitor.LogHandlerState.Started:
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

        private void UserControlLogMonitor_Load(object sender, EventArgs e)
        {
            _logMonitor = new LogMonitor();
            _logMonitor.OnNewLogs += _logMonitor_OnNewLogs;
            _logMonitor.OnReseted += _logMonitor_OnReseted;
        }

        #endregion Private Methods

    }
}