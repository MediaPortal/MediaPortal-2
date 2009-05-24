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
using MediaPortal.Core;
using MediaPortal.Core.Messaging;

namespace MediaPortal.Presentation.Models
{
  /// <summary>
  /// Base class for UI models which are registered to messages from the system.
  /// This class provides virtual initialization and disposal methods for system message queue registrations.
  /// </summary>
  public abstract class BaseMessageControlledUIModel : IDisposable
  {
    /// <summary>
    /// Initializes the message subscribtions.
    /// </summary>
    protected BaseMessageControlledUIModel()
    {
      SubscribeToMessages();
    }

    /// <summary>
    /// Stops the timer and unsubscribes from messages.
    /// </summary>
    public virtual void Dispose()
    {
      UnsubscribeFromMessages();
    }

    /// <summary>
    /// Initializes message queue registrations.
    /// </summary>
    void SubscribeToMessages()
    {
      IMessageBroker broker = ServiceScope.Get<IMessageBroker>();
      broker.Register_Async(SystemMessaging.QUEUE, OnSystemMessageReceived);
    }

    /// <summary>
    /// Removes message queue registrations.
    /// </summary>
    protected virtual void UnsubscribeFromMessages()
    {
      IMessageBroker broker = ServiceScope.Get<IMessageBroker>();
      broker.Unregister_Async(SystemMessaging.QUEUE, OnSystemMessageReceived);
    }

    /// <summary>
    /// Will be called when a system message is received. Can be overridden to provide additional functionality. When
    /// overridden in subclasses, this method has to be called also.
    /// </summary>
    /// <param name="message">The system message which was recieved.</param>
    protected virtual void OnSystemMessageReceived(QueueMessage message)
    {
      SystemMessaging.MessageType messageType =
          (SystemMessaging.MessageType) message.MessageData[SystemMessaging.MESSAGE_TYPE];
      if (messageType == SystemMessaging.MessageType.SystemStateChanged)
      {
        SystemState state = (SystemState) message.MessageData[SystemMessaging.PARAM];
        switch (state)
        {
          case SystemState.ShuttingDown:
            Dispose();
            break;
        }
      }
    }

    /// <summary>
    /// Provides the id of this model. This property has to be implemented in subclasses.
    /// </summary>
    public abstract Guid ModelId { get; }
  }
}
