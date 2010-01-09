#region Copyright (C) 2007-2009 Team MediaPortal

/*
    Copyright (C) 2007-2009 Team MediaPortal
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

using System;
using System.Collections.Generic;
using MediaPortal.Core;
using MediaPortal.Core.Messaging;
using MediaPortal.Core.Settings;
using MediaPortal.UI.Presentation.Workflow;
using MediaPortal.UI.ServerCommunication;
using UiComponents.SkinBase.Models;
using UiComponents.SkinBase.Settings;

namespace UiComponents.SkinBase
{
  /// <summary>
  /// Service listening for MP server connections and showing the connection screen.
  /// </summary>
  public class ConnectionListenerService : IConnectionListenerService, IDisposable
  {
    public const string ATTACH_TO_SERVER_STATE_ID_STR = "E834D0E0-BC35-4397-86F8-AC78C152E693";
    public static Guid ATTACH_TO_SERVER_STATE_ID = new Guid(ATTACH_TO_SERVER_STATE_ID_STR);

    protected AsynchronousMessageQueue _messageQueue;
    protected object _syncObj = new object();

    public ConnectionListenerService()
    {
      _messageQueue = new AsynchronousMessageQueue(this, new string[]
          {
            ServerConnectionMessaging.CHANNEL
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
      IWorkflowManager workflowManager = ServiceScope.Get<IWorkflowManager>();
      if (workflowManager.CurrentNavigationContext.WorkflowState.StateId == ATTACH_TO_SERVER_STATE_ID)
        // If we are already in the AttachToServer state, don't navigate there again
        return;
      // Check setting which prevents the listener service to pop up server availability messages
      ISettingsManager settingsManager = ServiceScope.Get<ISettingsManager>();
      SkinBaseSettings settings = settingsManager.Load<SkinBaseSettings>();
      if (settings.DisableServerListener)
        return;
      if (message.ChannelName == ServerConnectionMessaging.CHANNEL)
      {
        ServerConnectionMessaging.MessageType messageType =
            (ServerConnectionMessaging.MessageType) message.MessageType;
        switch (messageType)
        {
          case ServerConnectionMessaging.MessageType.AvailableServersChanged:
            bool serversWereAdded = (bool) message.MessageData[ServerConnectionMessaging.SERVERS_WERE_ADDED];
            if (!serversWereAdded)
              // Don't bother the user with connection messages if servers were only removed from the network
              return;
            workflowManager.NavigatePush(ATTACH_TO_SERVER_STATE_ID, new Dictionary<string, object>
                {
                  {ServerAttachmentModel.AUTO_CLOSE_ON_NO_SERVER_KEY, true}
                });
            break;
        }
      }
    }

    #region IConnectionListenerService implementation

    #endregion
  }
}
