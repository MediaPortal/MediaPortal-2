#region Copyright (C) 2007-2009 Team MediaPortal

/*
    Copyright (C) 2007-2009 Team MediaPortal
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

namespace MediaPortal.Core.Services.MediaManagement
{
  /// <summary>
  /// This class provides an interface for all messages of the media accessor.
  /// </summary>
  public class MediaAccessorMessaging
  {
    // Message channel name
    public const string CHANNEL = "MediaAccessor";

    /// <summary>
    /// Messages of this type are sent by the media manager and its components.
    /// </summary>
    public enum MessageType
    {
      // Media provider related messages. The param will contain the id of the media provider.
      MediaProviderAdded,
      MediaProviderRemoved,

      // Metadata extractor related messages. The param will contain the id of the metadata extractor.
      MetadataExtractorAdded,
      MetadataExtractorRemoved,
    }

    // Message data
    public const string PARAM = "Param"; // Parameter depends on the message type, see the docs in MessageType enum

    /// <summary>
    /// Sends a message concerning a media provider.
    /// </summary>
    /// <param name="messageType">Type of the message to send.</param>
    /// <param name="mediaProviderId">Media provider which is affected.</param>
    public static void SendMediaProviderMessage(MessageType messageType, Guid mediaProviderId)
    {
      QueueMessage msg = new QueueMessage(messageType);
      msg.MessageData[PARAM] = mediaProviderId;
      ServiceScope.Get<IMessageBroker>().Send(CHANNEL, msg);
    }

    /// <summary>
    /// Sends a message concerning a metadata extractor.
    /// </summary>
    /// <param name="messageType">Type of the message to send.</param>
    /// <param name="metadataExtractorId">Metadata extractor which is affected.</param>
    public static void SendMetadataExtractorMessage(MessageType messageType, Guid metadataExtractorId)
    {
      QueueMessage msg = new QueueMessage(messageType);
      msg.MessageData[PARAM] = metadataExtractorId;
      ServiceScope.Get<IMessageBroker>().Send(CHANNEL, msg);
    }
  }
}