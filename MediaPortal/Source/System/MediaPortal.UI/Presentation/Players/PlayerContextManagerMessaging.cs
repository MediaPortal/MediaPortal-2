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
      CurrentPlayerChanged,
    }

    // Message data
    public const string PLAYER_SLOT = "PlayerSlot"; // Player slot index of type int

    public static void SendPlayerContextManagerMessage(MessageType type, int playerSlot)
    {
      SystemMessage msg = new SystemMessage(type);
      msg.MessageData[PLAYER_SLOT] = playerSlot;
      ServiceRegistration.Get<IMessageBroker>().Send(CHANNEL, msg);
    }
  }
}
