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
using System.Collections.Generic;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Common.Threading;
using MediaPortal.UI.Presentation.UiNotifications;
using MediaPortal.Utilities;

namespace MediaPortal.UI.Services.UiNotifications
{
  public class NotificationService : IDisposable, INotificationService
  {
    #region Consts

    public const int PENDING_NOTIFICATIONS_WARNING_THRESHOLD = 10;
    public const int MAX_NUM_PENDING_NOTIFICATIONS_THRESHOLD = 100;

    public static readonly TimeSpan TIMESPAN_CHECK_NOTIFICATION_TIMEOUTS = TimeSpan.FromSeconds(10);

    #endregion

    #region Protected fields

    protected readonly object _syncObj = new object();
    protected IList<INotification> _normalQueue = new List<INotification>();
    protected IList<INotification> _urgentQueue = new List<INotification>();
    protected volatile bool _timeoutCheckSuspended = false;
    protected IIntervalWork _checkTimeoutIntervalWork = null;

    #endregion

    public void Dispose()
    {
      CheckForTimeouts = false;
    }

    protected void CheckTimeouts()
    {
      if (_timeoutCheckSuspended)
        return;
      ICollection<INotification> notifications = new List<INotification>(_normalQueue);
      CollectionUtils.AddAll(notifications, _urgentQueue);
      DateTime now = DateTime.Now;
      foreach (INotification notification in notifications)
      {
        DateTime? timeout = notification.Timeout;
        if (!timeout.HasValue)
          continue;
        if (timeout.Value < now)
          RemoveNotification(notification);
      }
    }

    #region INotificationService implementation

    public IList<INotification> Notifications
    {
      get
      {
        IList<INotification> result = new List<INotification>(_normalQueue);
        CollectionUtils.AddAll(result, new List<INotification>(_urgentQueue));
        return result;
      }
    }

    public bool CheckForTimeouts
    {
      get { return !_timeoutCheckSuspended; }
      set { _timeoutCheckSuspended = !value; }
    }

    public void Startup()
    {
      if (_checkTimeoutIntervalWork != null)
        return;
      IThreadPool threadPool = ServiceRegistration.Get<IThreadPool>();
      _checkTimeoutIntervalWork = new IntervalWork(CheckTimeouts, TIMESPAN_CHECK_NOTIFICATION_TIMEOUTS);
      threadPool.AddIntervalWork(_checkTimeoutIntervalWork, true);
    }

    public void Shutdown()
    {
      if (_checkTimeoutIntervalWork == null)
        return;
      IThreadPool threadPool = ServiceRegistration.Get<IThreadPool>();
      threadPool.RemoveIntervalWork(_checkTimeoutIntervalWork);
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
        int numUrgentMessages = _urgentQueue.Count;
        int numNormalMessages = _normalQueue.Count;
        int numPendingNotifications = numUrgentMessages + numNormalMessages;
        if (numPendingNotifications > PENDING_NOTIFICATIONS_WARNING_THRESHOLD)
        {
          ServiceRegistration.Get<ILogger>().Warn("NotificationService: {0} pending notifications", numPendingNotifications);
          while (_urgentQueue.Count > MAX_NUM_PENDING_NOTIFICATIONS_THRESHOLD)
          {
            INotification prunedNotification = _urgentQueue[_urgentQueue.Count - 1];
            ServiceRegistration.Get<ILogger>().Warn("NotificationService: Removing unhandled urgent notification '{0}'", prunedNotification);
            _urgentQueue.RemoveAt(_urgentQueue.Count - 1);
          }
          while (_normalQueue.Count > MAX_NUM_PENDING_NOTIFICATIONS_THRESHOLD)
          {
            INotification prunedNotification = _normalQueue[_normalQueue.Count - 1];
            ServiceRegistration.Get<ILogger>().Warn("NotificationService: Removing unhandled normal notification '{0}'", prunedNotification);
            _normalQueue.RemoveAt(_normalQueue.Count - 1);
          }
        }
      }
      NotificationServiceMessaging.SendMessage(NotificationServiceMessaging.MessageType.NotificationEnqueued, notification);
    }

    public void RemoveNotification(INotification notification)
    {
      bool removed;
      lock (_syncObj)
        removed = _normalQueue.Remove(notification) || _urgentQueue.Remove(notification);
      if (removed)
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
      INotification notification;
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
        else
          return null;
      }
      NotificationServiceMessaging.SendMessage(NotificationServiceMessaging.MessageType.NotificationRemoved, notification);
      return notification;
    }

    #endregion
  }
}