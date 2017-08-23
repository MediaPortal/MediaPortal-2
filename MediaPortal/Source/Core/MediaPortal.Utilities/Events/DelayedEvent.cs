#region Copyright (C) 2007-2017 Team MediaPortal

/*
    Copyright (C) 2007-2017 Team MediaPortal
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
using System.Timers;

namespace MediaPortal.Utilities.Events
{
  /// <summary>
  /// <see cref="DelayedEvent"/> allows to queue up events and raise one single event after a given timeout value.
  /// A common use case for this behavior is to handle user inputs and fire a "finished" event after a second.
  /// </summary>
  public class DelayedEvent : IDisposable
  {
    #region Fields

    protected readonly object _syncObj = new object();
    protected Timer _timer;
    protected double _delayMilliSeconds;
    protected EventHandler _onEventHandler;
    protected object _sender;
    protected EventArgs _args;
    protected bool _eventPending = false;

    #endregion

    /// <summary>
    /// Constructs a <see cref="DelayedEvent"/> instance.
    /// </summary>
    /// <param name="delayMilliSeconds">Delay in ms when the <see cref="OnEventHandler"/> will be invoked</param>
    public DelayedEvent(double delayMilliSeconds)
    {
      _delayMilliSeconds = delayMilliSeconds;
    }

    public void Dispose()
    {
      lock (_syncObj)
      {
        if (_timer != null)
          _timer.Dispose();
        _timer = null;
      }
    }

    /// <summary>
    /// Signales the original event and resets the timer.
    /// </summary>
    /// <param name="sender">Sender</param>
    /// <param name="args">Arguments</param>
    public void EnqueueEvent(object sender, EventArgs args)
    {
      lock (_syncObj)
      {
        _eventPending = true;
        if (_timer == null)
        {
          _timer = new Timer(_delayMilliSeconds) { Enabled = true, AutoReset = false };
          _timer.Elapsed += TimerElapsed;
        }
        else
        {
          // In case of new user action, reset the timer.
          _timer.Stop();
          _timer.Start();
        }
        _sender = sender;
        _args = args;
      }
    }

    /// <summary>
    /// Stops the timer.
    /// </summary>
    public void Stop()
    {
      lock (_syncObj)
      {
        if (_timer != null)
          _timer.Stop();
      }
    }

    /// <summary>
    /// Eventhandler that is executed after the timeout happened.
    /// </summary>
    public EventHandler OnEventHandler
    {
      get
      {
        lock (_syncObj)
          return _onEventHandler;
      }
      set
      {
        lock (_syncObj)
          _onEventHandler = value;
      }
    }

    public bool IsEventPending
    {
      get { return _eventPending; }
    }

    #region Private members

    private void TimerElapsed(object sender, ElapsedEventArgs e)
    {
      EventHandler handler;
      lock (_syncObj)
        handler = _onEventHandler;

      if (handler == null)
        return;

      handler(_sender, _args);
      _eventPending = false;
    }

    #endregion
  }
}
