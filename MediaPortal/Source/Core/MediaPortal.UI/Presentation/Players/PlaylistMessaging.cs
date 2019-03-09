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

using MediaPortal.Common;
using MediaPortal.Common.Messaging;

namespace MediaPortal.UI.Presentation.Players
{
  /// <summary>
  /// This class provides an interface for the messages sent by the playlist.
  /// This class is part of the playlist API.
  /// </summary>
  public class PlaylistMessaging
  {
    // Message channel name
    public const string CHANNEL = "Playlist";

    // Message type
    public enum MessageType
    {
      /// <summary>
      /// Gets fired when the playlist was changed, i.e. items were added, deleted or moved.
      /// </summary>
      PlaylistUpdate,

      /// <summary>
      /// Gets fired when the item, which is currently played, changes.
      /// </summary>
      CurrentItemChange,

      /// <summary>
      /// Gets fired when some properties of the playlist, e.g. <see cref="IPlaylist.PlayMode"/>, change.
      /// </summary>
      PropertiesChange,
    }

    // Message data
    public const string PLAYER_CONTEXT = "PlayerContext"; // Holds the player context which contains the playlist (IPlayerContext)

    /// <summary>
    /// Sends a playlist message.
    /// </summary>
    /// <param name="type">Type of the message.</param>
    /// <param name="playerContext">Player context which contains the playlist.</param>
    public static void SendPlaylistMessage(MessageType type, IPlayerContext playerContext)
    {
      SystemMessage msg = new SystemMessage(type);
      msg.MessageData[PLAYER_CONTEXT] = playerContext;
      ServiceRegistration.Get<IMessageBroker>().Send(CHANNEL, msg);
    }
  }
}
