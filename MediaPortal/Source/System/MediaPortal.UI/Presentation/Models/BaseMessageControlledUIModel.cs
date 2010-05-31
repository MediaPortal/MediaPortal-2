#region Copyright (C) 2007-2010 Team MediaPortal

/*
    Copyright (C) 2007-2010 Team MediaPortal
    http://www.team-mediaportal.com
 
    This file is part of MediaPortal 2

    MediaPortal 2 is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    MediaPortal 2 is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with MediaPortal 2.  If not, see <http://www.gnu.org/licenses/>.
*/

#endregion

using System;
using MediaPortal.Core.Messaging;

namespace MediaPortal.UI.Presentation.Models
{
  /// <summary>
  /// Base class for UI models which are registered to messages from the system and which are listening for
  /// messages all over their lifetime.
  /// </summary>
  /// <remarks>
  /// In general, workflow models should not be derived from this class as workflow models are normally receiving
  /// messages only during the time when they are active. The other time they normally will temporary shut down their
  /// message queue.
  /// </remarks>
  public abstract class BaseMessageControlledUIModel : IDisposable
  {
    protected AsynchronousMessageQueue _messageQueue;

    /// <summary>
    /// Creates a new <see cref="BaseMessageControlledUIModel"/> instance and starts the message queue.
    /// </summary>
    protected BaseMessageControlledUIModel()
    {
      SubscribeToMessages();
    }

    public virtual void Dispose()
    {
      _messageQueue.Shutdown();
    }

    /// <summary>
    /// Initializes message queue registrations.
    /// </summary>
    void SubscribeToMessages()
    {
      _messageQueue = new AsynchronousMessageQueue(this, new string[] {});
      _messageQueue.Start();
    }

    /// <summary>
    /// Provides the id of this model. This property has to be implemented in subclasses.
    /// </summary>
    public abstract Guid ModelId { get; }
  }
}
