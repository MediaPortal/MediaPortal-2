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

using System;
using MediaPortal.Common.Messaging;

namespace MediaPortal.Common.Services.MediaManagement
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
      // Resource provider related messages. The param will contain the id of the resource provider.
      ResourceProviderAdded,
      ResourceProviderRemoved,

      // Metadata extractor related messages. The param will contain the id of the metadata extractor.
      MetadataExtractorAdded,
      MetadataExtractorRemoved,

      // Relationship extractor related messages. The param will contain the id of the metadata extractor.
      RelationshipExtractorAdded,
      RelationshipExtractorRemoved,

      // Merge handler related messages. The param will contain the id of the merge handler.
      MergeHandlerAdded,
      MergeHandlerRemoved,

      // FanArt handler related messages. The param will contain the id of the FanArt handler.
      FanArtHandlerAdded,
      FanArtHandlerRemoved,
    }

    // Message data
    public const string PARAM = "Param"; // Parameter depends on the message type, see the docs in MessageType enum

    /// <summary>
    /// Sends a message concerning a resource provider.
    /// </summary>
    /// <param name="messageType">Type of the message to send.</param>
    /// <param name="resourceProviderId">Resource provider which is affected.</param>
    public static void SendResourceProviderMessage(MessageType messageType, Guid resourceProviderId)
    {
      SystemMessage msg = new SystemMessage(messageType);
      msg.MessageData[PARAM] = resourceProviderId;
      ServiceRegistration.Get<IMessageBroker>().Send(CHANNEL, msg);
    }

    /// <summary>
    /// Sends a message concerning a metadata extractor.
    /// </summary>
    /// <param name="messageType">Type of the message to send.</param>
    /// <param name="metadataExtractorId">Metadata extractor which is affected.</param>
    public static void SendMetadataExtractorMessage(MessageType messageType, Guid metadataExtractorId)
    {
      SystemMessage msg = new SystemMessage(messageType);
      msg.MessageData[PARAM] = metadataExtractorId;
      ServiceRegistration.Get<IMessageBroker>().Send(CHANNEL, msg);
    }

    /// <summary>
    /// Sends a message concerning a metadata extractor.
    /// </summary>
    /// <param name="messageType">Type of the message to send.</param>
    /// <param name="relationshipExtractorId">Relationship extractor which is affected.</param>
    public static void SendRelationshipExtractorMessage(MessageType messageType, Guid relationshipExtractorId)
    {
        SystemMessage msg = new SystemMessage(messageType);
        msg.MessageData[PARAM] = relationshipExtractorId;
        ServiceRegistration.Get<IMessageBroker>().Send(CHANNEL, msg);
    }

    /// <summary>
    /// Sends a message concerning a merge handler.
    /// </summary>
    /// <param name="messageType">Type of the message to send.</param>
    /// <param name="relationshipExtractorId">Merge handler which is affected.</param>
    public static void SendMergeHandlerMessage(MessageType messageType, Guid mergeHandlerId)
    {
        SystemMessage msg = new SystemMessage(messageType);
        msg.MessageData[PARAM] = mergeHandlerId;
        ServiceRegistration.Get<IMessageBroker>().Send(CHANNEL, msg);
    }
  }
}
