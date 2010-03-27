#region Copyright (C) 2007-2010 Team MediaPortal

/*
    Copyright (C) 2007-2010 Team MediaPortal
    http://www.team-mediaportal.com
 
    This file is part of MediaPortal 2

    MediaPortal 2 is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    MediaPortal 2 is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with MediaPortal 2.  If not, see <http://www.gnu.org/licenses/>.
*/

#endregion

using MediaPortal.Core;
using MediaPortal.Core.Messaging;

namespace MediaPortal.UI.Presentation.Players
{
  /// <summary>
  /// This class provides an interface for the messages sent by the player manager.
  /// This class is part of the player manager API.
  /// </summary>
  public class PlayerManagerMessaging
  {
    // Message channel name
    public const string CHANNEL = "PlayerManager";

    // Message type
    public enum MessageType
    {
      #region Player messages. Parameters: PLAYER_SLOT_CONTROLLER, ACTIVATION_SEQUENCE

      /// <summary>
      /// A player started playing a media item.
      /// </summary>
      PlayerStarted,

      /// <summary>
      /// The player state is ready. This message will be sent after the <see cref="PlayerStarted"/> message.
      /// </summary>
      PlayerStateReady,

      /// <summary>
      /// A player was stopped.
      /// </summary>
      PlayerStopped,

      /// <summary>
      /// A player has ended with playing the current media item.
      /// </summary>
      PlayerEnded,

      /// <summary>
      /// The playback state of a player changed. This can be a change in the "paused" state or a change in the seeking state.
      /// </summary>
      PlaybackStateChanged,

      /// <summary>
      /// A player error has occured. The player cannot play.
      /// </summary>
      PlayerError,

      /// <summary>
      /// The next item is requested by the player - this enables the player for gapless playback or to crossfade the
      /// next item, if possible.
      /// The PlayerManager/PlayerSlotController don't process this event theirselves because they are not aware of the
      /// playlist, which is managed by the player context.
      /// </summary>
      RequestNextItem,

      #endregion

      #region PlayerManager messages concerning a special player slot. Parameters: PLAYER_SLOT_CONTROLLER

      /// <summary>
      /// The slot playing audio changed to a new slot index.
      /// </summary>
      AudioSlotChanged,

      /// <summary>
      /// The slot was activated.
      /// </summary>
      PlayerSlotActivated,

      /// <summary>
      /// The slot was deactivated.
      /// </summary>
      PlayerSlotDeactivated,

      /// <summary>
      /// The slot was started.
      /// </summary>
      PlayerSlotStarted,

      #endregion

      #region General messages which don't concern a special player. No parameters.

      /// <summary>
      /// The primary and secondary players were exchanged.
      /// </summary>
      PlayerSlotsChanged,

      /// <summary>
      /// The players were muted.
      /// </summary>
      PlayersMuted,

      /// <summary>
      /// The mute state was removed.
      /// </summary>
      PlayersResetMute,

      #endregion
    }

    // Message data
    public const string PLAYER_SLOT_CONTROLLER = "PlayerSlotController"; // Holds the player slot controller (IPlayerSlotController)
    public const string ACTIVATION_SEQUENCE = "ActivationSequence"; // Holds the activation sequence number of the player slot controller at the time when the message was sent (uint)

    /// <summary>
    /// Sends a message which announces a change in a specific player. This method handles all
    /// the "player messages" message types.
    /// </summary>
    /// <param name="type">Type of the message.</param>
    /// <param name="psc">Player slot controller of the player which was changed.</param>
    public static void SendPlayerMessage(MessageType type, IPlayerSlotController psc)
    {
      SystemMessage msg = new SystemMessage(type);
      msg.MessageData[PLAYER_SLOT_CONTROLLER] = psc;
      msg.MessageData[ACTIVATION_SEQUENCE] = psc.ActivationSequence;
      ServiceScope.Get<IMessageBroker>().Send(CHANNEL, msg);
    }

    /// <summary>
    /// Sends a message which announces a change in the player manager. The change concerns a specific player
    /// slot. This method handles the "player manager messages concerning a special player" message types.
    /// </summary>
    /// <param name="type">Type of the message.</param>
    /// <param name="psc">Player slot controller of the player which is involved.</param>
    public static void SendPlayerManagerPlayerMessage(MessageType type, IPlayerSlotController psc)
    {
      SystemMessage msg = new SystemMessage(type);
      msg.MessageData[PLAYER_SLOT_CONTROLLER] = psc;
      ServiceScope.Get<IMessageBroker>().Send(CHANNEL, msg);
    }

    /// <summary>
    /// Sends a message which announces a change in the player manager. The change doesn't concern a specific player
    /// slot. This method handles the "general messages which don't concern a special player" message types.
    /// </summary>
    /// <param name="type">Type of the message.</param>
    public static void SendPlayerManagerPlayerMessage(MessageType type)
    {
      SystemMessage msg = new SystemMessage(type);
      ServiceScope.Get<IMessageBroker>().Send(CHANNEL, msg);
    }
  }
}
