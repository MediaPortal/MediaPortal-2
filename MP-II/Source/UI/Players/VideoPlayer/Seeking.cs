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
using System.Windows.Forms;
using MediaPortal.Core;
using MediaPortal.Control.InputManager;
using MediaPortal.Presentation.Localization;
using MediaPortal.Presentation.Players;
using MediaPortal.Presentation.DataObjects;
using MediaPortal.SkinEngine.SkinManagement;

namespace Ui.Players.VideoPlayer
{
  public class Seeking
  {
    #region enums

    private enum SeekDirection
    {
      Unknown,
      Past,
      Future
    }

    #endregion

    #region variables

    private List<int> _seekSteps = new List<int>();
    private int _currentSeekStep = 0;
    private bool _reachedEnd = false;
    private bool _reachedStart = false;
    private SeekDirection _seekDirection = SeekDirection.Unknown;
    private DateTime _seekTimeoutTimer;
    private bool _seekTimerRunning;
    private StringId _startLabel = new StringId("playback", "5");
    private StringId _endLabel = new StringId("playback", "6");
    private StringId _hoursLabel = new StringId("system", "2");
    private StringId _minsLabel = new StringId("system", "3");
    private StringId _secsLabel = new StringId("system", "4");
    private Property _seekTime = new Property(typeof(string), "");

    #endregion

    /// <summary>
    /// Initializes a new instance of the <see cref="Seeking"/> class.
    /// </summary>
    public Seeking()
    {
      Application.Idle += new EventHandler(Application_Idle);
      _seekSteps.Add(0);
      _seekSteps.Add(15);
      _seekSteps.Add(30);
      _seekSteps.Add(60);
      _seekSteps.Add(180);
      _seekSteps.Add(300);
      _seekSteps.Add(600);
      _seekSteps.Add(900);
      _seekSteps.Add(1800);
      _seekSteps.Add(3600);
      _seekSteps.Add(7200);
    }


    /// <summary>
    /// Gets a value indicating whether this we're seeking.
    /// </summary>
    /// <value>
    /// 	<c>true</c> if this w're seeking; otherwise, <c>false</c>.
    /// </value>
    public bool IsSeeking
    {
      get { return (_seekDirection != SeekDirection.Unknown); }
    }

    /// <summary>
    /// returns the current seek time
    /// </summary>
    /// <value>The seek time.</value>
    public string SeekTime
    {
      get { return (string) _seekTime.GetValue(); }
      set { _seekTime.SetValue(value); }
    }

    public Property SeekTimeProperty
    {
      get { return _seekTime; }
      set { _seekTime = value; }
    }

    private void UpdateSeekTime()
    {
      string labelState = "";
      if (_reachedEnd)
      {
        labelState = _endLabel.ToString();
      }
      else if (_reachedStart)
      {
        labelState = _startLabel.ToString();
      }
      else if (_currentSeekStep > 0)
      {
        TimeSpan ts = new TimeSpan(0, 0, _seekSteps[_currentSeekStep]);
        string secs = ts.Seconds.ToString();
        string mins = ts.Minutes.ToString();
        string hours = ts.Hours.ToString();
        string label = "";
        if (ts.Hours > 0)
        {
          label = String.Format("{0} {1}", hours, _hoursLabel.ToString());
        }
        else if (ts.Minutes > 0)
        {
          label = String.Format("{0} {1}", mins, _minsLabel.ToString());
        }
        else
        {
          label = String.Format("{0} {1}", secs, _secsLabel.ToString());
        }
        if (_seekDirection == SeekDirection.Past)
        {
          label = "-" + label;
        }
        labelState = label;
      }
      SeekTime = labelState;
    }

    /// <summary>
    /// Determines whether we can seek the specified timespan.
    /// </summary>
    /// <param name="ts">The timespan we want to seek.</param>
    /// <param name="reachedStart">if set to <c>true</c> we reached the start.</param>
    /// <param name="reachedEnd">if set to <c>true</c> we reached the end (livepoint).</param>
    /// <returns>
    /// 	<c>true</c> if this instance can seek the specified timespan; otherwise, <c>false</c>.
    /// </returns>
    public bool CanSeek(TimeSpan ts, ref bool reachedStart, ref bool reachedEnd)
    {
      _seekTimeoutTimer = SkinContext.Now;
      _seekTimerRunning = true;
      IPlayerCollection collection = ServiceScope.Get<IPlayerCollection>();
      if (collection.Count == 0)
      {
        return false;
      }
      IPlayer player = collection[0];
      TimeSpan newPosition = ts + player.CurrentTime;
      if (newPosition.TotalSeconds > player.Duration.TotalSeconds)
      {
        reachedEnd = true;
        reachedStart = false;
        return false;
      }
      else if (newPosition.TotalSeconds < 0)
      {
        reachedEnd = false;
        reachedStart = true;
        return false;
      }
      reachedEnd = false;
      reachedStart = false;
      return true;
    }

    /// <summary>
    /// Handles the seeking commands forward/backward
    /// </summary>
    /// <param name="key">The key.</param>
    public void OnSeek(Key key)
    {
      if (_seekDirection == SeekDirection.Unknown)
      {
        if (key == Key.Right)
        {
          _seekDirection = SeekDirection.Future;
        }
        if (key == Key.Left)
        {
          _seekDirection = SeekDirection.Past;
        }
      }
      switch (_seekDirection)
      {
        case SeekDirection.Past:
          if (key == Key.Left)
          {
            if (_currentSeekStep + 1 < _seekSteps.Count)
            {
              TimeSpan ts = new TimeSpan(0, 0, -_seekSteps[_currentSeekStep + 1]);
              if (CanSeek(ts, ref _reachedStart, ref _reachedEnd))
              {
                _currentSeekStep++;
              }
            }
          }
          if (key == Key.Right)
          {
            if (_currentSeekStep - 1 > 0)
            {
              TimeSpan ts = new TimeSpan(0, 0, -_seekSteps[_currentSeekStep - 1]);
              if (CanSeek(ts, ref _reachedStart, ref _reachedEnd))
              {
                _currentSeekStep--;
              }
            }
            else
            {
              _seekDirection = SeekDirection.Unknown;
              _currentSeekStep = 0;
              _reachedEnd = _reachedStart = false;
            }
          }
          break;
        case SeekDirection.Future:
          if (key == Key.Right)
          {
            if (_currentSeekStep + 1 < _seekSteps.Count)
            {
              TimeSpan ts = new TimeSpan(0, 0, _seekSteps[_currentSeekStep + 1]);
              if (CanSeek(ts, ref _reachedStart, ref _reachedEnd))
              {
                _currentSeekStep++;
              }
            }
          }
          if (key == Key.Left)
          {
            if (_currentSeekStep - 1 > 0)
            {
              TimeSpan ts = new TimeSpan(0, 0, _seekSteps[_currentSeekStep - 1]);
              if (CanSeek(ts, ref _reachedStart, ref _reachedEnd))
              {
                _currentSeekStep--;
              }
            }
            else
            {
              _seekDirection = SeekDirection.Unknown;
              _currentSeekStep = 0;
              _reachedEnd = _reachedStart = false;
            }
          }
          break;
      }
      UpdateSeekTime();
    }

    /// <summary>
    /// called when application is idle
    /// Will perform the actual seeking after a timeout of 1 second
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
    private void Application_Idle(object sender, EventArgs e)
    {
      if (_seekTimerRunning == false)
      {
        return;
      }
      TimeSpan ts = SkinContext.Now - _seekTimeoutTimer;
      if (ts.TotalSeconds < 1)
      {
        return;
      }
      _seekTimerRunning = false;
      IPlayerCollection collection = ServiceScope.Get<IPlayerCollection>();
      if (collection.Count != 0)
      {
        IPlayer player = collection[0];
        if (_reachedStart)
        {
          player.CurrentTime = new TimeSpan(0, 0, 0);
        }
        else if (_reachedEnd)
        {
          TimeSpan newPos = player.Duration + new TimeSpan(0, 0, 0, 0, -100);
          player.CurrentTime = newPos;
        }
        else
        {
          if (_seekDirection == SeekDirection.Past)
          {
            ts = new TimeSpan(0, 0, -_seekSteps[_currentSeekStep]);
          }
          else
          {
            ts = new TimeSpan(0, 0, _seekSteps[_currentSeekStep]);
          }

          TimeSpan newPosition = ts + player.CurrentTime;
          player.CurrentTime = newPosition;
        }
      }
      _seekDirection = SeekDirection.Unknown;
      _currentSeekStep = 0;
      _reachedEnd = _reachedStart = false;
      UpdateSeekTime();
    }
  }
}
