#region Copyright (C) 2007-2012 Team MediaPortal

/*
    Copyright (C) 2007-2012 Team MediaPortal
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
      PlaylistsChanged,
      MIATypesChanged,
    }

    public static void SendPlaylistsChangedMessage()
    {
      SystemMessage msg = new SystemMessage(MessageType.PlaylistsChanged);
      ServiceRegistration.Get<IMessageBroker>().Send(CHANNEL, msg);
    }

    public static void SendMIATypesChangedMessage()
    {
      SystemMessage msg = new SystemMessage(MessageType.PlaylistsChanged);
      ServiceRegistration.Get<IMessageBroker>().Send(CHANNEL, msg);
    }
  }
}
