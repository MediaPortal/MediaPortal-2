#region Copyright (C) 2007-2012 Team MediaPortal

/*
    Copyright (C) 2007-2012 Team MediaPortal
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
using MediaPortal.Common.Messaging;
using MediaPortal.UI.Presentation.Workflow;
using MediaPortal.UI.ServerCommunication;

namespace MediaPortal.UiComponents.Media.Actions
{
  public class TrackServerConnectionBaseAction : IWorkflowContributor
  {
    #region Protected fields

    protected AsynchronousMessageQueue _messageQueue = null;

    protected readonly bool _visibleOnServerConnect;
    protected readonly Guid _targetWorkflowStateId;
    protected readonly IResourceString _displayTitle;

    // This is the only attribute to be updated so we can optimize using volatile instead of using a lock
    protected volatile bool _isVisible;

    #endregion

    public TrackServerConnectionBaseAction(bool visibleOnServerConnect, Guid targetWorkflowStateId, string displayTitleResource)
    {
      _visibleOnServerConnect = visibleOnServerConnect;
      _targetWorkflowStateId = targetWorkflowStateId;
      _displayTitle = LocalizationHelper.CreateResourceString(displayTitleResource);
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
      IServerConnectionManager scm = ServiceRegistration.Get<IServerConnectionManager>();
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

    public IResourceString DisplayTitle
    {
      get { return _displayTitle; }
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

    public bool IsActionVisible(NavigationContext context)
    {
      return _isVisible;
    }

    public bool IsActionEnabled(NavigationContext context)
    {
      return true;
    }

    public void Execute()
    {
      IWorkflowManager workflowManager = ServiceRegistration.Get<IWorkflowManager>();
      workflowManager.NavigatePush(_targetWorkflowStateId);
    }

    #endregion
  }
}
