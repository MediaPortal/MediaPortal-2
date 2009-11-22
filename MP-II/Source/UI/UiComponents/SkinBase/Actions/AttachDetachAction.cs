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
using MediaPortal.Core.Localization;
using MediaPortal.UI.Presentation.Workflow;
using MediaPortal.UI.ServerCommunication;

namespace UiComponents.SkinBase.Actions
{
  /// <summary>
  /// Action which triggers a switch to workflow state DetachFromServer if a server is currently connected.
  /// </summary>
  public class AttachDetachAction : IWorkflowContributor
  {
    #region Consts

    public const string ATTACH_DETACH_CONTRIBUTOR_MODEL_ID_STR = "793DAD9F-F64C-4c7a-86C0-F5AA222D0CDB";

    protected const string ATTACH_TO_SERVER_STATE_STR = "E834D0E0-BC35-4397-86F8-AC78C152E693";
    protected const string DETACH_FROM_SERVER_STATE_STR = "BAC42991-5AB6-471f-A185-673D2E3B1EBA";

    public static Guid ATTACH_DETACH_CONTRIBUTOR_MODEL_ID = new Guid(ATTACH_DETACH_CONTRIBUTOR_MODEL_ID_STR);

    public static Guid ATTACH_TO_SERVER_STATE = new Guid(ATTACH_TO_SERVER_STATE_STR);
    public static Guid DETACH_FROM_SERVER_STATE = new Guid(DETACH_FROM_SERVER_STATE_STR);

    public const string SEARCH_FOR_SERVERS_RES = "[ServerConnection.SearchForServers]";
    public const string DETACH_FROM_SERVER_RES = "[ServerConnection.DetachFromServer]";

    #endregion

    #region Protected fields

    protected AsynchronousMessageQueue _messageQueue = null;
    protected bool _isEnabled = true;
    protected IResourceString _titleRes = null;

    #endregion

    void SubscribeToMessages()
    {
      _messageQueue = new AsynchronousMessageQueue(this, new string[]
        {
            ServerConnectionMessaging.CHANNEL
        });
      _messageQueue.MessageReceived += OnMessageReceived;
      _messageQueue.Start();
    }

    void UnsubscribeFromMessages()
    {
      if (_messageQueue == null)
        return;
      _messageQueue.Shutdown();
      _messageQueue = null;
    }

    void OnMessageReceived(AsynchronousMessageQueue queue, QueueMessage message)
    {
      if (message.ChannelName == ServerConnectionMessaging.CHANNEL)
      {
        ServerConnectionMessaging.MessageType messageType =
            (ServerConnectionMessaging.MessageType) message.MessageType;
        switch (messageType)
        {
          case ServerConnectionMessaging.MessageType.HomeServerAttached:
          case ServerConnectionMessaging.MessageType.HomeServerDetached:
            Update();
            break;
        }
      }
    }

    protected void Update()
    {
      IServerConnectionManager scm = ServiceScope.Get<IServerConnectionManager>();
      _titleRes = LocalizationHelper.CreateResourceString(string.IsNullOrEmpty(scm.HomeServerUUID) ? SEARCH_FOR_SERVERS_RES : DETACH_FROM_SERVER_RES);
      FireStateChanged();
    }

    protected void FireStateChanged()
    {
      ContributorStateChangeDelegate d = StateChanged;
      if (d != null) d();
    }

    #region IWorkflowContributor implementation

    public event ContributorStateChangeDelegate StateChanged;

    public bool IsActionVisible
    {
      get { return true; }
    }

    public bool IsActionEnabled
    {
      get { return true; }
    }

    public IResourceString DisplayTitle
    {
      get { return _titleRes; }
    }

    public void Initialize()
    {
      SubscribeToMessages();
      Update();
    }

    public void Uninitialize()
    {
      UnsubscribeFromMessages();
    }

    public void Execute()
    {
      IWorkflowManager workflowManager = ServiceScope.Get<IWorkflowManager>();
      IServerConnectionManager scm = ServiceScope.Get<IServerConnectionManager>();
      if (string.IsNullOrEmpty(scm.HomeServerUUID))
        workflowManager.NavigatePush(ATTACH_TO_SERVER_STATE);
      else
        workflowManager.NavigatePush(DETACH_FROM_SERVER_STATE);
    }

    #endregion
  }
}
