#region Copyright (C) 2007-2013 Team MediaPortal

/*
    Copyright (C) 2007-2013 Team MediaPortal
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

namespace MediaPortal.Common.MediaManagement
{
  /// <summary>
  /// This class provides an interface for the messages sent by the content directory.
  /// This messaging class is used for the server as well as for the client.
  /// At the MP2 client, messages are sent by the server connection manager class,
  /// at the server, messages are sent directly by the media library.
  /// </summary>
  public class ContentDirectoryMessaging
  {
    // Message channel name
    public const string CHANNEL = "ContentDirectory";

    /// <summary>
    /// Messages of this type are sent by the content directory.
    /// </summary>
    public enum MessageType
    {
      /// <summary>
      /// This message will be sent from the media library when a playlist registration changed.
      /// It is sent if a playlist was registered or removed as well as if one was changed.
      /// This message doesn't have any parameters.
      /// </summary>
      PlaylistsChanged,

      /// <summary>
      /// This message will be sent from the media library when the registration of MIA types changed.
      /// This message doesn't have any parameters.
      /// </summary>
      MIATypesChanged,

      /// <summary>
      /// This message will be sent from the media library when the registration of shares changed.
      /// This message doesn't have any parameters.
      /// </summary>
      RegisteredSharesChanged,

      /// <summary>
      /// This message will be sent from the media library when it is notified that a client started a share import and when
      /// the local importer starts an import.
      /// This message has a parameter <see cref="SHARE_ID"/>.
      /// </summary>
      ShareImportStarted,

      /// <summary>
      /// This message will be sent from the media library when it is notified that a client completed a share import and when
      /// the local importer completes an import.
      /// This message has a parameter <see cref="SHARE_ID"/>.
      /// </summary>
      ShareImportCompleted,
    }

    public const string SHARE_ID = "ShareId"; // Parameter type: Guid

    public static void SendPlaylistsChangedMessage()
    {
      SystemMessage msg = new SystemMessage(MessageType.PlaylistsChanged);
      ServiceRegistration.Get<IMessageBroker>().Send(CHANNEL, msg);
    }

    public static void SendMIATypesChangedMessage()
    {
      SystemMessage msg = new SystemMessage(MessageType.MIATypesChanged);
      ServiceRegistration.Get<IMessageBroker>().Send(CHANNEL, msg);
    }

    public static void SendRegisteredSharesChangedMessage()
    {
      SystemMessage msg = new SystemMessage(MessageType.RegisteredSharesChanged);
      ServiceRegistration.Get<IMessageBroker>().Send(CHANNEL, msg);
    }

    public static void SendShareImportMessage(MessageType messageType, Guid shareId)
    {
      SystemMessage msg = new SystemMessage(messageType);
      msg.MessageData[SHARE_ID] = shareId;
      ServiceRegistration.Get<IMessageBroker>().Send(CHANNEL, msg);
    }
  }
}
