#region Copyright (C) 2007-2010 Team MediaPortal

/*
    Copyright (C) 2007-2010 Team MediaPortal
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
using MediaPortal.Core.Localization;
using MediaPortal.Core.Messaging;
using MediaPortal.UI.Presentation.Workflow;
using MediaPortal.UI.ServerCommunication;

namespace UiComponents.Media.Actions
{
  public class TrackServerConnectionBaseAction : IWorkflowContributor
  {
    #region Protected fields

    protected bool _visibleOnServerConnect;
    protected Guid _targetWorkflowStateId;
    protected string _displayTitleResource;
    protected AsynchronousMessageQueue _messageQueue = null;
    protected volatile bool _isVisible;

    #endregion

    public TrackServerConnectionBaseAction(bool visibleOnServerConnect, Guid targetWorkflowStateId, string displayTitleResource)
    {
      _visibleOnServerConnect = visibleOnServerConnect;
      _targetWorkflowStateId = targetWorkflowStateId;
      _displayTitleResource = displayTitleResource;
    }

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

    void OnMessageReceived(AsynchronousMessageQueue queue, SystemMessage message)
    {
      if (message.ChannelName == ServerConnectionMessaging.CHANNEL)
      {
        ServerConnectionMessaging.MessageType messageType = (ServerConnectionMessaging.MessageType) message.MessageType;
        switch (messageType)
        {
          case ServerConnectionMessaging.MessageType.HomeServerConnected:
          case ServerConnectionMessaging.MessageType.HomeServerDisconnected:
            Update();
            break;
        }
      }
    }

    protected void Update()
    {
      IServerConnectionManager scm = ServiceScope.Get<IServerConnectionManager>();
      bool lastVisible = _isVisible;
      _isVisible = scm.IsHomeServerConnected ^ !_visibleOnServerConnect;
      if (lastVisible != _isVisible)
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
      get { return _isVisible; }
    }

    public bool IsActionEnabled
    {
      get { return true; }
    }

    public IResourceString DisplayTitle
    {
      get { return LocalizationHelper.CreateResourceString(_displayTitleResource); }
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
      workflowManager.NavigatePush(_targetWorkflowStateId, null);
    }

    #endregion
  }
}