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

using System.Threading;
using MediaPortal.Common;
using MediaPortal.Common.Messaging;
using MediaPortal.Common.Runtime;

namespace MediaPortal.UI.Presentation.Models
{
  /// <summary>
  /// Base class for models which use a timer to update their properties periodically. Provides a timer-controlled
  /// virtual <see cref="Update"/> method which will be called automatically in a configurable interval.
  /// This class provides initialization and virtual disposal methods for the timer and for system message queue registrations.
  /// </summary>
  public abstract class BaseTimerControlledModel : BaseMessageControlledModel
  {
    protected object _syncObj = new object();
    protected Timer _timer = null;
    protected long _updateInterval = 0;
    protected volatile bool _duringUpdate = false;
    protected bool _autoStart;

    /// <summary>
    /// Creates a new <see cref="BaseTimerControlledModel"/> instance and initializes the internal timer
    /// and message queue registrations.
    /// </summary>
    /// <param name="startAtOnce">If set to <c>true</c>, the underlaying timer is started at once. Else, <see cref="StartTimer"/>
    /// must be called to start the timer.</param>
    /// <param name="updateInterval">Timer update interval in milliseconds.</param>
    /// <remarks>
    /// Subclasses might need to call method <see cref="Update"/> in their constructor to initialize the initial model state,
    /// if appropriate.
    /// </remarks>
    protected BaseTimerControlledModel(bool startAtOnce, long updateInterval)
    {
      _autoStart = startAtOnce;
      _updateInterval = updateInterval;
      ISystemStateService systemStateService = ServiceRegistration.Get<ISystemStateService>();
      if (startAtOnce)
      {
        if (systemStateService.CurrentState == SystemState.Running)
          StartTimer();
        SubscribeToMessages();
      }
    }

    /// <summary>
    /// Stops the timer and unsubscribes from messages.
    /// </summary>
    public override void Dispose()
    {
      base.Dispose();
      StopTimer();
    }

    void SubscribeToMessages()
    {
      _messageQueue.PreviewMessage += OnMessageReceived;
      if (_autoStart)
        _messageQueue.SubscribeToMessageChannel(SystemMessaging.CHANNEL);
    }

    protected void OnTimerElapsed(object sender)
    {
      if (_duringUpdate)
        return;
      _duringUpdate = true;
      try
      {
        Update();
      }
      finally
      {
        _duringUpdate = false;
      }
    }

    /// <summary>
    /// Sets the timer up to be called periodically.
    /// </summary>
    protected void StartTimer()
    {
      lock (_syncObj)
      {
        if (_timer != null)
          return;
        _timer = new Timer(OnTimerElapsed);
        ChangeInterval(_updateInterval);
      }
    }

    /// <summary>
    /// Changes the timer interval.
    /// </summary>
    /// <param name="updateInterval">Interval in ms</param>
    protected void ChangeInterval(long updateInterval)
    {
      lock (_syncObj)
      {
        if (_timer == null)
          return;
        _updateInterval = updateInterval;
        _timer.Change(updateInterval, updateInterval);
      }
    }

    /// <summary>
    /// Disables the timer and blocks until the last timer event has executed.
    /// </summary>
    protected void StopTimer()
    {
      WaitHandle notifyObject;
      lock (_syncObj)
      {
        if (_timer == null)
          return;
        notifyObject = new ManualResetEvent(false);
        _timer.Dispose(notifyObject);
        _timer = null;
      }
      notifyObject.WaitOne();
      notifyObject.Close();
    }

    void OnMessageReceived(AsynchronousMessageQueue queue, SystemMessage message)
    {
      if (message.ChannelName == SystemMessaging.CHANNEL)
      {
        SystemMessaging.MessageType messageType = (SystemMessaging.MessageType) message.MessageType;
        if (messageType == SystemMessaging.MessageType.SystemStateChanged)
        {
          if (!_autoStart)
            return;
          SystemState state = (SystemState) message.MessageData[SystemMessaging.NEW_STATE];
          switch (state)
          {
            case SystemState.Running:
              StartTimer();
              break;
            case SystemState.ShuttingDown:
              StopTimer();
              break;
          }
        }
      }
    }

    /// <summary>
    /// Called periodically to update properties. This methd has to be implemented in concrete subclasses.
    /// </summary>
    protected abstract void Update();
  }
}
