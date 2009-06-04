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
  public class PlayerContextManagerMessaging
  {
    // Message Queue name
    public const string CHANNEL = "PlayerContextManager";

    // Message type
    public enum MessageType
    {
      /// <summary>
      /// The current player was changed. The PARAM will contain the player slot.
      /// </summary>
      CurrentPlayerChanged,
    }

    // Message data
    public const string PARAM = "Param"; // Parameter depends on the message type, see the docs in MessageType enum

    public static void SendPlayerContextManagerMessage(MessageType type, int playerSlot)
    {
      QueueMessage msg = new QueueMessage(type);
      msg.MessageData[PARAM] = playerSlot;
      ServiceScope.Get<IMessageBroker>().Send(CHANNEL, msg);
    }
  }
}
