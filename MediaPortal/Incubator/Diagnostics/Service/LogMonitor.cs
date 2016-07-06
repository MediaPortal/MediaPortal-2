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
using System.IO;

namespace MediaPortal.UiComponents.Diagnostics.Service
{
    public class LogMonitor : IDisposable
    {

        #region Private Fields

        private bool _disposedValue = false;

        private long _lastFileSize;

        private System.Threading.Thread _worker = null;

        #endregion Private Fields

        #region Private Delegates

        private delegate void dVoidMethod();

        #endregion Private Delegates

        #region Public Events

        public event EventHandler<NewLogsEventArgs> OnNewLogs;

        public event EventHandler OnReseted;

        #endregion Public Events

        #region Public Enums

        public enum LogHandlerState
        {
            Stopped = 0,
            Started = 1,
            Pausing = 2
        }

        #endregion Public Enums

        #region Public Properties

        public string FileName { get; set; }

        /// <summary>
        /// Gets or sets the interval, expressed in milliseconds.
        /// </summary>
        public int Interval { get; set; }

        public LogHandlerState State { get; private set; }

        #endregion Public Properties

        #region Public Methods

        public void Dispose()
        {
            Dispose(true);
        }

        /// <summary>
        /// Pause logs watching
        /// </summary>
        public void Pause()
        {
            if (State == LogHandlerState.Stopped) return;

            State = LogHandlerState.Pausing;
        }

        /// <summary>
        /// Start logs watching or resume from pause
        /// </summary>
        public void Start()
        {
            if (State == LogHandlerState.Started) return;
            if (_disposedValue) return;
            if (string.IsNullOrEmpty(FileName)) return;

            State = LogHandlerState.Started;

            if (_worker == null)
            {
                _worker = new System.Threading.Thread(new System.Threading.ThreadStart(MonitorFile))
                {
                    IsBackground = true,
                    Priority = System.Threading.ThreadPriority.BelowNormal
                };
                _worker.Start();
            }
        }

        /// <summary>
        /// Stop Logs watching
        /// </summary>
        public void Stop()
        {
            if (State == LogHandlerState.Stopped) return;
            if (_disposedValue) return;

            if (_worker != null)
            {
                try
                {
                    _worker.Abort();
                }
                catch (System.Threading.ThreadAbortException)
                { }
                _worker = null;
            }

            State = LogHandlerState.Stopped;
        }

        #endregion Public Methods

        #region Protected Methods

        protected virtual void Dispose(bool disposing)
        {
            if (State != LogHandlerState.Stopped)
                Stop();

            if (!_disposedValue)
            {
                _disposedValue = true;
            }
        }

        #endregion Protected Methods

        #region Private Methods

        private void MonitorFile()
        {
            while (State == LogHandlerState.Started || State == LogHandlerState.Pausing)
            {
                ScanFile();
                System.Threading.Thread.Sleep(1000);
            }
        }

        private void ScanFile()
        {
            if (State == LogHandlerState.Pausing) return;

            string newFileLines = null;
            bool reseted = false;

            // Open with the least amount of locking possible
            using (FileStream stream = File.Open(FileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                long newLength = stream.Length;
                if (newLength >= _lastFileSize)
                {
                    stream.Position = _lastFileSize;  // Only read in new lines added
                }
                else
                {
                    stream.Position = 0; //restart from beginning
                    reseted = true;
                }
                using (StreamReader reader = new StreamReader(stream))
                {
                    newFileLines = reader.ReadToEnd();
                }
                _lastFileSize = newLength;
            }

            if (reseted)
                OnReseted?.Invoke(this, EventArgs.Empty);

            if (!string.IsNullOrEmpty(newFileLines))
                OnNewLogs?.Invoke(this, new NewLogsEventArgs(newFileLines));
        }

        #endregion Private Methods

        #region Public Classes

        public class NewLogsEventArgs : EventArgs
        {

            #region Internal Constructors

            internal NewLogsEventArgs(string logs)
            {
                this.Logs = logs;
            }

            #endregion Internal Constructors

            #region Public Properties

            public string Logs { get; private set; }

            #endregion Public Properties

        }

        #endregion Public Classes

    }
}