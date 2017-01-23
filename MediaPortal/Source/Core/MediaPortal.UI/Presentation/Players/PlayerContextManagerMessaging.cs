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

namespace MediaPortal.UI.Presentation.Players
{
  /// <summary>
  /// This class provides an interface for the messages sent by the player context manager.
  /// This class is part of the player context manager API.
  /// </summary>
  public class PlayerContextManagerMessaging
  {
    // Message Queue name
    public const string CHANNEL = "PlayerContextManager";

    // Message type
    public enum MessageType
    {
      /// <summary>
      /// The primary and secondary players were exchanged.
      /// </summary>
      PlayerSlotsChanged,

      /// <summary>
      /// The current player slot controller changed. The <see cref="PLAYER_CONTEXT"/> message property will contain the
      /// new player context.
      /// Note that this message is only sent when the current player slot controller changes, it isn't sent when
      /// the player slots were exchanged (for example when the primary video is exchanged with the PiP video) and thus
      /// the current player index was changed to the other player. So when tracking the current player and the
      /// player slot index is important, the message <see cref="PlayerSlotsChanged"/> might also be interesting.
      /// </summary>
      /// <remarks>
      /// With this message, the <see cref="PLAYER_CONTEXT"/> property will be set.
      /// </remarks>
      CurrentPlayerChanged,

      /// <summary>
      /// Internal message used by the player context manager. This message is used to update the current player and the audio
      /// player index. It is used to enqueue that job after all other messages to avoid multithreading issues.
      /// </summary>
      /// <remarks>
      /// With this message, the <see cref="NEW_CURRENT_PLAYER_CONTEXT"/> <see cref="NEW_AUDIO_PLAYER_CONTEXT"/> properties will be set.
      /// </remarks>
      UpdatePlayerRolesInternal,
    }

    // Message data
    public const string PLAYER_CONTEXT = "PlayerContext"; // Type IPlayerContext
    public const string NEW_CURRENT_PLAYER_CONTEXT = "CurrentPlayer"; // Type IPlayerContext
    public const string NEW_AUDIO_PLAYER_CONTEXT = "AudioPlayer"; // Type IPlayerContext

    public static void SendPlayerContextManagerMessage(MessageType type)
    {
      SystemMessage msg = new SystemMessage(type);
      ServiceRegistration.Get<IMessageBroker>().Send(CHANNEL, msg);
    }

    public static void SendPlayerContextManagerMessage(MessageType type, IPlayerContext playerContext)
    {
      SystemMessage msg = new SystemMessage(type);
      msg.MessageData[PLAYER_CONTEXT] = playerContext;
      ServiceRegistration.Get<IMessageBroker>().Send(CHANNEL, msg);
    }

    internal static void SendUpdatePlayerRolesMessage(IPlayerContext newCurrentPlayerContext, IPlayerContext newAudioPlayerContext)
    {
      SystemMessage msg = new SystemMessage(MessageType.UpdatePlayerRolesInternal);
      msg.MessageData[NEW_CURRENT_PLAYER_CONTEXT] = newCurrentPlayerContext;
      msg.MessageData[NEW_AUDIO_PLAYER_CONTEXT] = newAudioPlayerContext;
      ServiceRegistration.Get<IMessageBroker>().Send(CHANNEL, msg);
    }
  }
}
