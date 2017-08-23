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
using MediaPortal.Common.Localization;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.ResourceAccess;
using MediaPortal.Common.Messaging;
using MediaPortal.UI.Presentation.UiNotifications;
using MediaPortal.UiComponents.SkinBase.General;

namespace MediaPortal.UiComponents.SkinBase.Services
{
  /// <summary>
  /// Listens for common messages and other system events and sends UI notifications for them.
  /// </summary>
  public class CommonNotificationService : IDisposable
  {
    protected AsynchronousMessageQueue _messageQueue;
    protected object _syncObj = new object();

    public CommonNotificationService()
    {
      _messageQueue = new AsynchronousMessageQueue(this, new string[]
          {
            ImporterWorkerMessaging.CHANNEL,
          });
      _messageQueue.MessageReceived += OnMessageReceived;
      _messageQueue.Start();
    }

    public void Dispose()
    {
      _messageQueue.Shutdown();
    }

    private static void OnMessageReceived(AsynchronousMessageQueue queue, SystemMessage message)
    {
      if (message.ChannelName == ImporterWorkerMessaging.CHANNEL)
      {
        INotificationService notificationService = ServiceRegistration.Get<INotificationService>();
        ImporterWorkerMessaging.MessageType messageType = (ImporterWorkerMessaging.MessageType) message.MessageType;
        ResourcePath resourcePath;
        if (messageType == ImporterWorkerMessaging.MessageType.ImportStarted)
        {
          resourcePath = (ResourcePath) message.MessageData[ImporterWorkerMessaging.RESOURCE_PATH];
          notificationService.EnqueueNotification(NotificationType.Info,
            LocalizationHelper.Translate(Consts.RES_IMPORT_STARTED_TITLE),
            LocalizationHelper.Translate(Consts.RES_IMPORT_STARTED_TEXT, resourcePath), false);
        }
        else if (messageType == ImporterWorkerMessaging.MessageType.ImportCompleted)
        {
          resourcePath = (ResourcePath) message.MessageData[ImporterWorkerMessaging.RESOURCE_PATH];
          notificationService.EnqueueNotification(NotificationType.Info,
            LocalizationHelper.Translate(Consts.RES_IMPORT_COMPLETED_TITLE),
            LocalizationHelper.Translate(Consts.RES_IMPORT_COMPLETED_TEXT, resourcePath), false);
        }
      }
    }
  }
}
