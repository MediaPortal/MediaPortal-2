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
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with MediaPortal 2. If not, see <http://www.gnu.org/licenses/>.
*/

#endregion

using System;
using System.Collections.Generic;
using MediaPortal.Core;
using MediaPortal.Core.Logging;
using MediaPortal.UI.Presentation.UiNotifications;
using MediaPortal.Utilities;

namespace MediaPortal.UI.Services.UiNotifications
{
  public class NotificationService : INotificationService
  {
    #region Classes

    #endregion

    #region Consts

    public const int PENDING_NOTIFICATIONS_THRESHOLD = 10;

    #endregion

    #region Protected fields

    protected object _syncObj = new object();
    protected IList<INotification> _normalQueue = new List<INotification>();
    protected IList<INotification> _urgentQueue = new List<INotification>();

    #endregion

    #region INotificationService implementation

    public IList<INotification> Notifications
    {
      get
      {
        IList<INotification> result = new List<INotification>(_normalQueue);
        CollectionUtils.AddAll(result, _urgentQueue);
        return result;
      }
    }

    public INotification EnqueueNotification(NotificationType type, string title, string text, bool urgent)
    {
      DefaultNotification notification = new DefaultNotification(type, title, text);
      EnqueueNotification(notification, urgent);
      return notification;
    }

    public INotification EnqueueNotification(NotificationType type, string title, string text,
        Guid handlerWorkflowState, bool urgent)
    {
      DefaultNotification notification = new DefaultNotification(type, title, text, handlerWorkflowState);
      EnqueueNotification(notification, urgent);
      return notification;
    }

    public void EnqueueNotification(INotification notification, bool urgent)
    {
      lock (_syncObj)
      {
        if (urgent)
          _urgentQueue.Insert(0, notification);
        else
          _normalQueue.Insert(0, notification);
        int numPendingNotifications = _urgentQueue.Count + _normalQueue.Count;
        if (numPendingNotifications > PENDING_NOTIFICATIONS_THRESHOLD)
          ServiceRegistration.Get<ILogger>().Warn("NotificationService: {0} pending notifications", numPendingNotifications);
      }
      NotificationServiceMessaging.SendMessage(NotificationServiceMessaging.MessageType.NotificationEnqueued, notification);
    }

    public void RemoveNotification(INotification notification)
    {
      lock (_syncObj)
      {
        _normalQueue.Remove(notification);
        _urgentQueue.Remove(notification);
      }
      NotificationServiceMessaging.SendMessage(NotificationServiceMessaging.MessageType.NotificationRemoved, notification);
    }

    public INotification PeekNotification()
    {
      lock (_syncObj)
      {
        if (_urgentQueue.Count > 0)
          return _urgentQueue[_urgentQueue.Count - 1];
        if (_normalQueue.Count > 0)
          return _normalQueue[_normalQueue.Count - 1];
      }
      return null;
    }

    public INotification DequeueNotification()
    {
      INotification notification = null;
      lock (_syncObj)
      {
        if (_urgentQueue.Count > 0)
        {
          notification = _urgentQueue[_urgentQueue.Count - 1];
          _urgentQueue.RemoveAt(_urgentQueue.Count - 1);
        }
        else if (_normalQueue.Count > 0)
        {
          notification = _normalQueue[_normalQueue.Count - 1];
          _normalQueue.RemoveAt(_normalQueue.Count - 1);
        }
      }
      NotificationServiceMessaging.SendMessage(NotificationServiceMessaging.MessageType.NotificationRemoved, notification);
      return notification;
    }

    #endregion
  }
}