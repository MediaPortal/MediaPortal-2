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

using System;
using MediaPortal.Core;
using MediaPortal.Core.Messaging;
using MediaPortal.Presentation.Workflow;
using MediaPortal.ServerCommunication;

namespace UiComponents.SkinBase
{
  /// <summary>
  /// Service listening for MP server connections and showing the connection screen.
  /// </summary>
  public class ConnectionListenerService : IConnectionListenerService
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

    private static void OnMessageReceived(AsynchronousMessageQueue queue, QueueMessage message)
    {
      IWorkflowManager workflowManager = ServiceScope.Get<IWorkflowManager>();
      if (workflowManager.CurrentNavigationContext.WorkflowState.StateId == ATTACH_TO_SERVER_STATE_ID)
        // If we are already in the AttachToServer state, don't navigate there again
        return;
      // TODO: Check setting which prevents the ConnectionListenerService to pop up server availability messages and
      // return from method, if configured to do so
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
            workflowManager.NavigatePush(ATTACH_TO_SERVER_STATE_ID);
            break;
        }
      }
    }

    #region IConnectionListenerService implementation

    #endregion
  }
}