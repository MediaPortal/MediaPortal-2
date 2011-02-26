#region Copyright (C) 2007-2011 Team MediaPortal

/*
    Copyright (C) 2007-2011 Team MediaPortal
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
using MediaPortal.Core;
using MediaPortal.Core.Messaging;
using MediaPortal.Core.Runtime;

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

    /// <summary>
    /// Creates a new <see cref="BaseTimerControlledModel"/> instance and initializes the internal timer
    /// and message queue registrations.
    /// </summary>
    /// <param name="updateInterval">Timer update interval in milliseconds.</param>
    /// <remarks>
    /// Subclasses might need to call method <see cref="Update"/> in their constructor to initialize the initial model state,
    /// if appropriate.
    /// </remarks>
    protected BaseTimerControlledModel(long updateInterval)
    {
      _updateInterval = updateInterval;
      ISystemStateService systemStateService = ServiceRegistration.Get<ISystemStateService>();
      if (systemStateService.CurrentState == SystemState.Running)
        StartTimer();
      SubscribeToMessages();
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
      _messageQueue.SubscribeToMessageChannel(SystemMessaging.CHANNEL);
    }

    protected void OnTimerElapsed(object sender)
    {
      Update();
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
        _timer.Change(_updateInterval, _updateInterval);
      }
    }

    /// <summary>
    /// Disables the timer and blocks until the last timer event has executed.
    /// </summary>
    protected void StopTimer()
    {
      lock (_syncObj)
      {
        if (_timer == null)
          return;
        WaitHandle notifyObject = new ManualResetEvent(false);
        _timer.Dispose(notifyObject);
        notifyObject.WaitOne();
        _timer = null;
      }
    }

    void OnMessageReceived(AsynchronousMessageQueue queue, SystemMessage message)
    {
      if (message.ChannelName == SystemMessaging.CHANNEL)
      {
        SystemMessaging.MessageType messageType = (SystemMessaging.MessageType) message.MessageType;
        if (messageType == SystemMessaging.MessageType.SystemStateChanged)
        {
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
