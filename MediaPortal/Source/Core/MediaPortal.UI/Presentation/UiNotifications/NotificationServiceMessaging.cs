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

namespace MediaPortal.UI.Presentation.UiNotifications
{
  /// <summary>
  /// This class provides an interface for the messages sent by the notification service.
  /// This class is part of the notification service API.
  /// </summary>
  public class NotificationServiceMessaging
  {
    // Message channel name
    public const string CHANNEL = "NotificationService";

    /// <summary>
    /// Messages of this type are sent by the <see cref="INotificationService"/>.
    /// </summary>
    public enum MessageType
    {
      /// <summary>
      /// A new notification was added to the notifications queue.
      /// The parameter <see cref="NOTIFICATION"/> will contain the added notification.
      /// </summary>
      NotificationEnqueued,

      /// <summary>
      /// A new notification was dequeued from the notifications queue.
      /// The parameter <see cref="NOTIFICATION"/> will contain the dequeued notification.
      /// </summary>
      NotificationDequeued,

      /// <summary>
      /// A new notification was removed from the notifications queue. The notification
      /// has not been handled.
      /// The parameter <see cref="NOTIFICATION"/> will contain the removed notification.
      /// </summary>
      NotificationRemoved,
    }

    // Message data
    public const string NOTIFICATION = "Notification"; // Type: INotification

    public static void SendMessage(MessageType type, INotification notification)
    {
      SystemMessage msg = new SystemMessage(type);
      msg.MessageData[NOTIFICATION] = notification;
      ServiceRegistration.Get<IMessageBroker>().Send(CHANNEL, msg);
    }
  }
}
