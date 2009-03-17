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
    // Message Queue name
    public const string QUEUE = "PlayerManager";

    public enum MessageType
    {
      #region Player messages. The param will denote the player slot (int).

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

      #endregion

      #region PlayerManager messages concerning a special player slot. The param will denote the player slot (int).

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

      #endregion

      #region General messages which don't concern a special player. The param doesn't have a special meaning for these messages.

      /// <summary>
      /// The primary player changed to a new slot.
      /// </summary>
      PlayerSlotsChanged,

      #endregion
    }

    // Message data
    public const string MESSAGE_TYPE = "MessagType"; // Message type stored as MessageType
    public const string PARAM = "Param"; // Parameter depends on the message type, see the docs in MessageType enum

    /// <summary>
    /// Sends a message which announces a change in a specific player. This method handles all
    /// the "player messages" message types.
    /// </summary>
    /// <param name="type">Type of the message.</param>
    /// <param name="slot">Slot of the specific player which was changed.</param>
    public static void SendPlayerMessage(MessageType type, int slot)
    {
      IMessageQueue queue = ServiceScope.Get<IMessageBroker>().GetOrCreate(QUEUE);
      QueueMessage msg = new QueueMessage();
      msg.MessageData[MESSAGE_TYPE] = type;
      msg.MessageData[PARAM] = slot;
      queue.Send(msg);
    }

    /// <summary>
    /// Sends a message which announces a change in the player manager. The change concerns a specific player
    /// slot. This method handles the "player manager messages concerning a special player" message types.
    /// </summary>
    /// <param name="type">Type of the message.</param>
    /// <param name="slot">Slot of the player which is involved.</param>
    public static void SendPlayerManagerPlayerMessage(MessageType type, int slot)
    {
      IMessageQueue queue = ServiceScope.Get<IMessageBroker>().GetOrCreate(QUEUE);
      QueueMessage msg = new QueueMessage();
      msg.MessageData[MESSAGE_TYPE] = type;
      msg.MessageData[PARAM] = slot;
      queue.Send(msg);
    }

    /// <summary>
    /// Sends a message which announces a change in the player manager. The change doesn't concern a specific player
    /// slot. This method handles the "general messages which don't concern a special player" message types.
    /// </summary>
    /// <param name="type">Type of the message.</param>
    /// <param name="slot">Slot of the player which is involved.</param>
    public static void SendPlayerManagerPlayerMessage(MessageType type)
    {
      IMessageQueue queue = ServiceScope.Get<IMessageBroker>().GetOrCreate(QUEUE);
      QueueMessage msg = new QueueMessage();
      msg.MessageData[MESSAGE_TYPE] = type;
      queue.Send(msg);
    }
  }
}
