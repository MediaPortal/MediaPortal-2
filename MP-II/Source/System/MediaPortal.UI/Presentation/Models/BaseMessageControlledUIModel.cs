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
using MediaPortal.Core.Messaging;

namespace MediaPortal.Presentation.Models
{
  /// <summary>
  /// Base class for UI models which are registered to messages from the system.
  /// This class provides virtual initialization and disposal methods for system message queue registrations.
  /// </summary>
  public abstract class BaseMessageControlledUIModel
  {
    protected AsynchronousMessageQueue _messageQueue;

    /// <summary>
    /// Creates a new <see cref="BaseMessageControlledUIModel"/> instance and initializes the message subscribtions.
    /// </summary>
    protected BaseMessageControlledUIModel()
    {
      SubscribeToMessages();
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
