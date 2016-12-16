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
using MediaPortal.Common.Messaging;
using MediaPortal.Common.Settings;
using MediaPortal.Common.SystemCommunication;
using MediaPortal.UI.Presentation.UiNotifications;
using MediaPortal.UI.Presentation.Workflow;
using MediaPortal.UI.ServerCommunication;
using MediaPortal.UiComponents.SkinBase.General;
using MediaPortal.UiComponents.SkinBase.Settings;

namespace MediaPortal.UiComponents.SkinBase.Services
{
  /// <summary>
  /// Service listening for MP server connections and showing the connection screen.
  /// </summary>
  public class ConnectionListenerService : IConnectionListenerService, IDisposable
  {
    protected AsynchronousMessageQueue _messageQueue;
    protected object _syncObj = new object();

    protected INotification _queuedNotification = null;

    public ConnectionListenerService()
    {
      _messageQueue = new AsynchronousMessageQueue(this, new string[]
          {
            ServerConnectionMessaging.CHANNEL,
            NotificationServiceMessaging.CHANNEL,
          });
      _messageQueue.MessageReceived += OnMessageReceived;
      _messageQueue.Start();
    }

    public void Dispose()
    {
      _messageQueue.Shutdown();
    }

    private void OnMessageReceived(AsynchronousMessageQueue queue, SystemMessage message)
    {
      if (message.ChannelName == NotificationServiceMessaging.CHANNEL)
      {
        INotification notification = (INotification) message.MessageData[NotificationServiceMessaging.NOTIFICATION];
        NotificationServiceMessaging.MessageType messageType = (NotificationServiceMessaging.MessageType) message.MessageType;
        if (messageType == NotificationServiceMessaging.MessageType.NotificationDequeued ||
            messageType == NotificationServiceMessaging.MessageType.NotificationRemoved)
          lock (_syncObj)
            if (notification == _queuedNotification)
              _queuedNotification = null;
      }
      // Check setting which prevents the listener service to pop up server availability messages
      ISettingsManager settingsManager = ServiceRegistration.Get<ISettingsManager>();
      SkinBaseSettings settings = settingsManager.Load<SkinBaseSettings>();
      if (!settings.EnableServerListener)
      {
        RemoveNotification();
        return;
      }
      if (message.ChannelName == ServerConnectionMessaging.CHANNEL)
      {
        ServerConnectionMessaging.MessageType messageType =
            (ServerConnectionMessaging.MessageType) message.MessageType;
        switch (messageType)
        {
          case ServerConnectionMessaging.MessageType.AvailableServersChanged:
            bool serversWereAdded = (bool) message.MessageData[ServerConnectionMessaging.SERVERS_WERE_ADDED];
            if (serversWereAdded)
              QueueNotification();
            else
            {
              ICollection<ServerDescriptor> servers = (ICollection<ServerDescriptor>) message.MessageData[ServerConnectionMessaging.AVAILABLE_SERVERS];
              if (servers.Count == 0)
                // No servers available any more
                RemoveNotification();
            }
            break;
          case ServerConnectionMessaging.MessageType.HomeServerAttached:
            // Home server was attached outside the notification handler
            RemoveNotification();
            break;
        }
      }
    }

    protected void QueueNotification()
    {
      if (_queuedNotification != null)
        return;
      IWorkflowManager workflowManager = ServiceRegistration.Get<IWorkflowManager>();
      if (workflowManager.CurrentNavigationContext.WorkflowState.StateId == Consts.WF_STATE_ID_ATTACH_TO_SERVER)
        // If we are already in the AttachToServer state, don't queue message again
        return;
      INotificationService notificationService = ServiceRegistration.Get<INotificationService>();
      _queuedNotification = notificationService.EnqueueNotification(NotificationType.UserInteractionRequired,
          Consts.RES_NOTIFICATION_HOME_SERVER_AVAILABLE_IN_NETWORK_TITLE,
          Consts.RES_NOTIFICATION_HOME_SERVER_AVAILABLE_IN_NETWORK_TEXT, Consts.WF_STATE_ID_ATTACH_TO_SERVER, false);
    }

    protected void RemoveNotification()
    {
      if (_queuedNotification == null)
        return;
      INotificationService notificationService = ServiceRegistration.Get<INotificationService>();
      notificationService.RemoveNotification(_queuedNotification);
    }

    #region IConnectionListenerService implementation

    #endregion
  }
}
