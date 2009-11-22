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

using System;
using System.Collections.Generic;
using System.Threading;
using MediaPortal.UI.Media.MediaManagement;
using MediaPortal.UI.Presentation.Players;

namespace Media.Players.BassPlayer
{
  public partial class BassPlayer
  {
    /// <summary>
    /// Player monitor. Keeps track of playback position and notifies controller if nessecary.
    /// </summary>
    class Monitor : IDisposable
    {
      #region Static members

      /// <summary>
      /// Creates and initializes an new instance.
      /// </summary>
      /// <param name="player">Reference to containing IPlayer object.</param>
      /// <returns>The new instance.</returns>
      public static Monitor Create(BassPlayer player)
      {
        Monitor monitor = new Monitor(player);
        monitor.Initialize();
        return monitor;
      }

      #endregion

      #region Fields

      // Reference to the containin IPlayer object.
      private BassPlayer _Player;

      // Monitorthread.
      private Thread _MonitorThread;
      private bool _MonitorThreadAbortFlag;
      private AutoResetEvent _MonitorThreadNotify;

      // Playback progress
      private TimeSpan _Duration;
      private TimeSpan _CurrentPosition;

      #endregion

      #region Public members

      /// <summary>
      /// Returns the duration of the currently played track.
      /// </summary>
      public TimeSpan Duration
      {
        get { return _Duration; }
      }

      /// <summary>
      /// Returns the playback position of the currently played track.
      /// </summary>
      public TimeSpan CurrentPosition
      {
        get { return _CurrentPosition; }
      }

      /// <summary>
      /// Terminates and waits for the monitor thread.
      /// </summary>
      public void TerminateThread()
      {
        if (_MonitorThread.IsAlive)
        {
          Log.Debug("Stopping monitor thread.");

          _MonitorThreadAbortFlag = true;
          _MonitorThreadNotify.Set();
          _MonitorThread.Join();
        }
      }

      #endregion

      #region Private members

      private Monitor(BassPlayer player)
      {
        _Player = player;
      }

      /// <summary>
      /// Initializes a new instance.
      /// </summary>
      private void Initialize()
      {
        _MonitorThreadAbortFlag = false;
        _MonitorThreadNotify = new AutoResetEvent(false);
        _MonitorThread = new Thread(new ThreadStart(ThreadMonitor));
        _MonitorThread.IsBackground = true;
        _MonitorThread.Start();
      }

      /// <summary>
      /// Monitor thread loop.
      /// </summary>
      private void ThreadMonitor()
      {
        try
        {
          while (!_MonitorThreadAbortFlag)
          {
            InternalPlayBackState internalState = _Player._Controller.InternalState;
            if (internalState == InternalPlayBackState.Playing || internalState == InternalPlayBackState.Paused)
            {
              _Duration = _Player._InputSourceSwitcher.CurrentInputSource.OutputStream.Length;
              _CurrentPosition = _Player._InputSourceSwitcher.CurrentInputSource.OutputStream.GetPosition();
            }
            else
            {
              _Duration = TimeSpan.Zero;
              _CurrentPosition = TimeSpan.Zero;
            }
            
            _MonitorThreadNotify.WaitOne(200, false);
          }
        }
        catch (Exception e)
        {
          throw new BassPlayerException("Exception in monitor thread.", e);
        }
      }

      #endregion

      #region IDisposable Members

      public void Dispose()
      {
        Log.Debug("PlaybackBuffer.Dispose()");

        TerminateThread();
      }

      #endregion
    }
  }
}
