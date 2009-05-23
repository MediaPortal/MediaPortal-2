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

namespace MediaPortal.Presentation.Geometries
{
  /// <summary>
  /// This class provides an interface for the messages sent by the plugin manager.
  /// This class is part of the plugin manager API.
  /// </summary>
  public static class PlayerGeometryMessaging
  {
    // Message Queue name
    public const string QUEUE = "PlayerGeometry";

    public enum NotificationType
    {
      /// <summary>
      /// Will be sent when the default geometry changed or when the geometry of a special player slot changed.
      /// The parameter PLAYER_SLOT denotes the slot for which the change occured. If this parameter is equal to
      /// <see cref="PlayerGeometryMessaging.ALL_PLAYERS"/>, all players are affected.
      /// </summary>
      GeometryChanged,
    }

    // Message data
    public const string NOTIFICATION_TYPE = "Notification"; // Notification stored as NotificationType
    public const string PLAYER_SLOT = "PlayerSlot"; // Player slot which changed its geometry

    public const int ALL_PLAYERS = -1;

    /// <summary>
    /// Sends a <see cref="NotificationType.GeometryChanged"/> message.
    /// </summary>
    /// <param name="playerSlot">The player slot which is affected from the change. If this parameter is equal to
    /// <see cref="PlayerGeometryMessaging.ALL_PLAYERS"/>, all player slots are affected.</param>
    public static void SendGeometryChangedMessage(int playerSlot)
    {
      IMessageQueue queue = ServiceScope.Get<IMessageBroker>().GetOrCreate(QUEUE);
      QueueMessage msg = new QueueMessage();
      msg.MessageData[NOTIFICATION_TYPE] = NotificationType.GeometryChanged;
      msg.MessageData[PLAYER_SLOT] = playerSlot;
      queue.Send(msg);
    }
  }
}