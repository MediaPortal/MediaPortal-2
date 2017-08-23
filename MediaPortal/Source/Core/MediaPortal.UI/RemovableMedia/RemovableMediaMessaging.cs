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

using MediaPortal.Common;
using MediaPortal.Common.Messaging;

namespace MediaPortal.UI.RemovableMedia
{
  /// <summary>
  /// This class provides an interface for the messages sent by the removable media tracker.
  /// This class is part of the removable media tracker API.
  /// </summary>
  public class RemovableMediaMessaging
  {
    // Message channel name
    public const string CHANNEL = "RemovableMediaTracker";

    /// <summary>
    /// Messages of this type are sent by the <see cref="IRemovableMediaTracker"/>.
    /// </summary>
    public enum MessageType
    {
      /// <summary>
      /// A removable media disk/stick was inserted. The parameter <see cref="RemovableMediaMessaging.DRIVE_LETTER"/> contains the drive letter
      /// of the new device.
      /// </summary>
      MediaInserted,

      /// <summary>
      /// A removable media disk/stick was removed. The parameter <see cref="RemovableMediaMessaging.DRIVE_LETTER"/> contains the drive letter
      /// of the removed device.
      /// </summary>
      MediaRemoved,
    }

    // Message data
    public const string DRIVE_LETTER = "DriveLetter"; // Type: string. Format: "D:".

    /// <summary>
    /// Sends a <see cref="MessageType.MediaInserted"/> or <see cref="MessageType.MediaRemoved"/> message.
    /// </summary>
    /// <param name="messageType">One of the <see cref="MessageType.MediaInserted"/> or
    /// <see cref="MessageType.MediaRemoved"/> messages.</param>
    /// <param name="driveLetter">Drive letter of the inserted or removed drive. Format: <c>"D:"</c>.</param>
    public static void SendMediaChangedMessage(MessageType messageType, string driveLetter)
    {
      SystemMessage msg = new SystemMessage(messageType);
      msg.MessageData[DRIVE_LETTER] = driveLetter;
      ServiceRegistration.Get<IMessageBroker>().Send(CHANNEL, msg);
    }
  }
}
