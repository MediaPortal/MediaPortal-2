#region Copyright (C) 2007-2018 Team MediaPortal

/*
    Copyright (C) 2007-2018 Team MediaPortal
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
using MediaPortal.Common.Messaging;
using MediaPortal.Common;
using MediaPortal.Common.MediaManagement;
using System.Collections.Generic;

namespace MediaPortal.Backend.Services.MediaLibrary
{
  /// <summary>
  /// This class provides an interface for all messages of the media accessor.
  /// </summary>
  public class MediaLibraryMessaging
  {
    // Message channel name
    public const string CHANNEL = "MediaLibrary";

    /// <summary>
    /// Messages of this type are sent by the media manager and its components.
    /// </summary>
    public enum MessageType
    {
      // This message will be sent by the media library when a media item has been added to the database or the aspects
      // have been updated.
      // The param will contain an enumeration of the media items that was added or updated.
      MediaItemsAddedOrUpdated,

      // This message will be sent by the media library when 1 or more media items have been deleted from the database.
      MediaItemsDeleted
    }

    // Message data
    public const string PARAM = "Param"; // Parameter depends on the message type, see the docs in MessageType enum

    /// <summary>
    /// Sends a media item added message.
    /// </summary>
    public static void SendMediaItemsAddedOrUpdatedMessage(MediaItem mediaItem)
    {
      SendMediaItemsAddedOrUpdatedMessage(new[] { mediaItem });
    }

    /// <summary>
    /// Sends a media item added message.
    /// </summary>
    public static void SendMediaItemsAddedOrUpdatedMessage(IEnumerable<MediaItem> mediaItems)
    {
      SystemMessage msg = new SystemMessage(MessageType.MediaItemsAddedOrUpdated);
      msg.MessageData[PARAM] = new List<MediaItem>(mediaItems);
      ServiceRegistration.Get<IMessageBroker>().Send(CHANNEL, msg);
    }

    /// <summary>
    /// Sends a media items deleted message.
    /// </summary>
    public static void SendMediaItemsDeletedMessage()
    {
      SystemMessage msg = new SystemMessage(MessageType.MediaItemsDeleted);
      ServiceRegistration.Get<IMessageBroker>().Send(CHANNEL, msg);
    }
  }
}
