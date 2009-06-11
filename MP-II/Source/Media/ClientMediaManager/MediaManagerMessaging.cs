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

namespace MediaPortal.Media.ClientMediaManager
{
  /// <summary>
  /// This class provides an interface for the messages sent by the media manager, which includes all routed
  /// messages from the server.
  /// </summary>
  public class MediaManagerMessaging
  {
    // Message channel name
    public const string CHANNEL = "MediaManager";

    /// <summary>
    /// Messages of this type are sent by the media manager and its components.
    /// </summary>
    public enum MessageType
    {
      // Messages concerning a share. The parameter will denote the share id.

      ShareAdded,
      ShareRemoved,
      ShareChanged,
    }

    // Message data
    public const string PARAM = "Param"; // Parameter depends on the message type, see the docs in MessageType enum

    /// <summary>
    /// Sends a message concerning a share.
    /// </summary>
    /// <param name="messageType">Type of the message to send.</param>
    /// <param name="shareId">Share which is affected.</param>
    public static void SendShareMessage(MessageType messageType, Guid shareId)
    {
      QueueMessage msg = new QueueMessage(messageType);
      msg.MessageData[PARAM] = shareId;
      ServiceScope.Get<IMessageBroker>().Send(CHANNEL, msg);
    }
  }
}
