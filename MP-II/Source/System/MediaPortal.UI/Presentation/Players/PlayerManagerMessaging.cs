#region Copyright (C) 2007-2008 Team MediaPortal

/*
    Copyright (C) 2007-2008 Team MediaPortal
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

using MediaPortal.Core;
using MediaPortal.Core.Messaging;

namespace MediaPortal.Presentation.Players
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
      #region Player messages. The param will denote the player slot controller (IPlayerSlotController).

      /// <summary>
      /// A player started.
      /// </summary>
      PlayerStarted,

      /// <summary>
      /// A player was stopped.
      /// </summary>
      PlayerStopped,

      /// <summary>
      /// A player has ended.
      /// </summary>
      PlayerEnded,

      /// <summary>
      /// A player was paused.
      /// </summary>
      PlayerPaused,

      /// <summary>
      /// A player error has occured. The player cannot play.
      /// </summary>
      PlayerError,

      #endregion

      #region PlayerManager messages concerning a special player slot. The param will denote the player slot controller (IPlayerSlotController).

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

      #region General messages which don't concern a special player. The param doesn't have a special meaning for these messages.

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
    public const string PARAM = "Param"; // Parameter depends on the message type, see the docs in MessageType enum

    /// <summary>
    /// Sends a message which announces a change in a specific player. This method handles all
    /// the "player messages" message types.
    /// </summary>
    /// <param name="type">Type of the message.</param>
    /// <param name="psc">Player slot controller of the player which was changed.</param>
    public static void SendPlayerMessage(MessageType type, IPlayerSlotController psc)
    {
      QueueMessage msg = new QueueMessage(type);
      msg.MessageData[PARAM] = psc;
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
      QueueMessage msg = new QueueMessage(type);
      msg.MessageData[PARAM] = psc;
      ServiceScope.Get<IMessageBroker>().Send(CHANNEL, msg);
    }

    /// <summary>
    /// Sends a message which announces a change in the player manager. The change doesn't concern a specific player
    /// slot. This method handles the "general messages which don't concern a special player" message types.
    /// </summary>
    /// <param name="type">Type of the message.</param>
    public static void SendPlayerManagerPlayerMessage(MessageType type)
    {
      QueueMessage msg = new QueueMessage(type);
      ServiceScope.Get<IMessageBroker>().Send(CHANNEL, msg);
    }
  }
}
