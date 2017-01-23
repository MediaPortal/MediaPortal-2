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
using MediaPortal.Common;
using MediaPortal.Common.Messaging;
using MediaPortal.Common.MediaManagement;

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

    public const string KEY_RESUME_STATE = "PlayerResumeState";
    public const string KEY_MEDIAITEM = "MediaItem";

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
      /// The player is about to be released (after beeing stopped or ended). This message is immediately sent before <see cref="PlayerStopped"/>
      /// or <see cref="PlayerEnded"/> and contains resume information as parameter <see cref="PlayerManagerMessaging.KEY_RESUME_STATE"/>.
      /// </summary>
      PlayerResumeState,

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
      /// The audio signal is now provided by the player slot controller given in the message parameter.
      /// </summary>
      AudioSlotChanged,

      /// <summary>
      /// The slot given by the message parameter was started.
      /// </summary>
      PlayerSlotStarted,

      /// <summary>
      /// The slot given by the message parameter was closed.
      /// </summary>
      PlayerSlotClosed,

      #endregion

      #region General messages which don't concern a special player. No parameters.

      /// <summary>
      /// The players were muted.
      /// </summary>
      PlayersMuted,

      /// <summary>
      /// The mute state was removed.
      /// </summary>
      PlayersResetMute,

      /// <summary>
      /// The global volume was changed.
      /// </summary>
      VolumeChanged,

      #endregion
    }

    // Message data
    public const string PLAYER_SLOT_CONTROLLER = "PlayerSlotController"; // Holds the player slot controller (IPlayerSlotController)

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
      ServiceRegistration.Get<IMessageBroker>().Send(CHANNEL, msg);
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
      ServiceRegistration.Get<IMessageBroker>().Send(CHANNEL, msg);
    }

    /// <summary>
    /// Sends a message which contains information for resuming playback. The contained data can be specific for each player (can be position or some binary data).
    /// </summary>
    /// <param name="psc">Player slot controller of the player which is involved.</param>
    /// <param name="mediaItemId">ID of media item that was played.</param>
    /// <param name="resumeState">Resume state.</param>
    public static void SendPlayerResumeStateMessage(IPlayerSlotController psc, MediaItem mediaItem, IResumeState resumeState)
    {
      SystemMessage msg = new SystemMessage(MessageType.PlayerResumeState);
      msg.MessageData[PLAYER_SLOT_CONTROLLER] = psc;
      msg.MessageData[KEY_MEDIAITEM] = mediaItem;
      msg.MessageData[KEY_RESUME_STATE] = resumeState;
      ServiceRegistration.Get<IMessageBroker>().Send(CHANNEL, msg);
    }

    /// <summary>
    /// Sends a message which announces a change in the player manager. The change doesn't concern a specific player
    /// slot. This method handles the "general messages which don't concern a special player" message types.
    /// </summary>
    /// <param name="type">Type of the message.</param>
    public static void SendPlayerManagerPlayerMessage(MessageType type)
    {
      SystemMessage msg = new SystemMessage(type);
      ServiceRegistration.Get<IMessageBroker>().Send(CHANNEL, msg);
    }
  }
}
