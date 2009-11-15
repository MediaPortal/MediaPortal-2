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
using MediaPortal.Shares;

namespace MediaPortal.Services.Shares
{
  /// <summary>
  /// This class provides an interface for all shares related messages.
  /// </summary>
  public class SharesMessaging
  {
    // Message channel name
    public const string CHANNEL = "Shares";

    /// <summary>
    /// Messages of this type are sent by the media manager and its components.
    /// </summary>
    public enum MessageType
    {
      // Share related messages. The SHARE_ID will contain the id of the share.
      ShareAdded,
      ShareRemoved,
      ShareChanged, // Parameter RELOCATION_MODE will be set
    }

    // Message data
    public const string SHARE_ID = "ShareId"; // Guid of the affected share
    public const string RELOCATION_MODE = "RelocationMode"; // Contains a variable of type RelocationMode

    /// <summary>
    /// Sends a message concerning a share.
    /// </summary>
    /// <param name="messageType">Type of the message to send.</param>
    /// <param name="shareId">Share which is affected.</param>
    public static void SendShareMessage(MessageType messageType, Guid shareId)
    {
      QueueMessage msg = new QueueMessage(messageType);
      msg.MessageData[SHARE_ID] = shareId;
      ServiceScope.Get<IMessageBroker>().Send(CHANNEL, msg);
    }

    public static void SendShareChangedMessage(Guid shareId, RelocationMode relocationMode)
    {
      QueueMessage msg = new QueueMessage(MessageType.ShareChanged);
      msg.MessageData[SHARE_ID] = shareId;
      msg.MessageData[RELOCATION_MODE] = relocationMode;
      ServiceScope.Get<IMessageBroker>().Send(CHANNEL, msg);
    }
  }
}