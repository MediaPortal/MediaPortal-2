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

using System.Timers;
using MediaPortal.Core;
using MediaPortal.Core.Messaging;

namespace MediaPortal.Presentation.Models
{
  /// <summary>
  /// Base class for UI models which use a timer to update their properties periodically. Provides a timer-controlled
  /// virtual <see cref="Update"/> method which will be called automatically in a configurable interval.
  /// This class provides initialization and virtual disposal methods for the timer and for system message queue registrations.
  /// </summary>
  public abstract class BaseTimerControlledUIModel : BaseMessageControlledUIModel
  {
    protected Timer _timer = null;

    /// <summary>
    /// Initializes the internal timer with the specified <paramref name="updateInterval"/>.
    /// </summary>
    /// <remarks>
    /// Subclasses need to call method <see cref="SubscribeToMessages"/>, and, if appropriate, <see cref="Update"/>.
    /// </remarks>
    /// <param name="updateInterval">Timer update interval in milliseconds.</param>
    protected BaseTimerControlledUIModel(long updateInterval)
    {
      _timer = new Timer(updateInterval);

      SubscribeToMessages();
    }

    protected void OnTimerElapsed(object sender, ElapsedEventArgs e)
    {
      Update();
    }

    /// <summary>
    /// Stops the timer and unsubscribes from messages.
    /// </summary>
    public override void Dispose()
    {
      StopListening();
      UnsubscribeFromMessages();
    }

    /// <summary>
    /// Initializes message queue registrations.
    /// </summary>
    void SubscribeToMessages()
    {
      ISystemStateService systemStateService = ServiceScope.Get<ISystemStateService>();
      if (systemStateService.CurrentState == SystemState.Started)
        StartListening();

      IMessageBroker broker = ServiceScope.Get<IMessageBroker>();
      broker.GetOrCreate(SystemMessaging.QUEUE).MessageReceived_Async += OnSystemMessageReceived;
    }

    /// <summary>
    /// Removes message queue registrations.
    /// </summary>
    protected override void UnsubscribeFromMessages()
    {
      IMessageBroker broker = ServiceScope.Get<IMessageBroker>();
      broker.GetOrCreate(SystemMessaging.QUEUE).MessageReceived_Async -= OnSystemMessageReceived;
    }

    /// <summary>
    /// Sets the timer up to be called periodically.
    /// </summary>
    protected void StartListening()
    {
      if (_timer.Enabled)
        return;
      // Setup timer to update the properties
      _timer.Elapsed += OnTimerElapsed;
      _timer.Enabled = true;
    }

    /// <summary>
    /// Disables the timer.
    /// </summary>
    protected virtual void StopListening()
    {
      if (!_timer.Enabled)
        return;
      _timer.Enabled = false;
      _timer.Elapsed -= OnTimerElapsed;
    }

    /// <summary>
    /// Will be called when a system message is received. Can be overridden to provide additional functionality. When
    /// overridden in subclasses, this method has to be called also.
    /// </summary>
    /// <param name="message">The system message which was recieved.</param>
    protected override void OnSystemMessageReceived(QueueMessage message)
    {
      SystemMessaging.MessageType messageType =
          (SystemMessaging.MessageType) message.MessageData[SystemMessaging.MESSAGE_TYPE];
      if (messageType == SystemMessaging.MessageType.SystemStateChanged)
      {
        SystemState state = (SystemState) message.MessageData[SystemMessaging.PARAM];
        switch (state)
        {
          case SystemState.Started:
            StartListening();
            break;
        }
      }
    }

    /// <summary>
    /// Called periodically to update properties. This methd has to be implemented in concrete subclasses.
    /// </summary>
    protected abstract void Update();
  }
}
