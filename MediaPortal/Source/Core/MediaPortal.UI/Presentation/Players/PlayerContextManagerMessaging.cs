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

using MediaPortal.Common;
using MediaPortal.Common.Messaging;

namespace MediaPortal.UI.Presentation.Players
{
  /// <summary>
  /// This class provides an interface for the messages sent by the player manager.
  /// This class is part of the player manager API.
  /// </summary>
  public class PlayerContextManagerMessaging
  {
    // Message Queue name
    public const string CHANNEL = "PlayerContextManager";

    // Message type
    public enum MessageType
    {
      /// <summary>
      /// The current player slot controller changed. The PLAYER_SLOT will contain the new player slot index.
      /// Note that this message is only sent when the current player slot controller changes, it isn't sent when
      /// the player slots were exchanged (for example when the primary video is exchanged with the PiP video) and thus
      /// the current player index was changed to the other player. So when tracking the current player and the
      /// player slot index is important, the message <see cref="PlayerManagerMessaging.MessageType.PlayerSlotsChanged"/>
      /// might also be interesting.
      /// </summary>
      /// <remarks>
      /// With this message, the <see cref="PLAYER_SLOT"/> property will be set.
      /// </remarks>
      CurrentPlayerChanged,

      /// <summary>
      /// Internal message used by the player context manager. This message is used to update the current player and the audio
      /// player index. It is used to enqueue that job after all other messages to avoid multithreading issues.
      /// </summary>
      /// <remarks>
      /// With this message, the <see cref="CURRENT_PLAYER_INDEX"/> <see cref="AUDIO_PLAYER_INDEX"/> properties will be set.
      /// </remarks>
      UpdatePlayerRolesInternal,
    }

    // Message data
    public const string PLAYER_SLOT = "PlayerSlot"; // Player slot index of type int
    public const string CURRENT_PLAYER_INDEX = "CurrentPlayer"; // Player slot index of type int
    public const string AUDIO_PLAYER_INDEX = "AudioPlayer"; // Player slot index of type int

    public static void SendPlayerContextManagerMessage(MessageType type, int playerSlot)
    {
      SystemMessage msg = new SystemMessage(type);
      msg.MessageData[PLAYER_SLOT] = playerSlot;
      ServiceRegistration.Get<IMessageBroker>().Send(CHANNEL, msg);
    }

    public static void SendUpdatePlayerRolesMessage(int currentPlayerIndex, int audioPlayerIndex)
    {
      SystemMessage msg = new SystemMessage(MessageType.UpdatePlayerRolesInternal);
      msg.MessageData[CURRENT_PLAYER_INDEX] = currentPlayerIndex;
      msg.MessageData[AUDIO_PLAYER_INDEX] = audioPlayerIndex;
      ServiceRegistration.Get<IMessageBroker>().Send(CHANNEL, msg);
    }
  }
}
