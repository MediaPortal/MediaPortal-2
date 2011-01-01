#region Copyright (C) 2007-2011 Team MediaPortal

/*
    Copyright (C) 2007-2011 Team MediaPortal
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
using MediaPortal.Core;
using MediaPortal.Core.Localization;
using MediaPortal.Core.Messaging;
using MediaPortal.UI.Presentation.Players;
using MediaPortal.UI.Presentation.Workflow;

namespace MediaPortal.UiComponents.Media.Actions
{
  public class ShowPlaylistAction : IWorkflowContributor
  {
    #region Consts

    public const string SHOW_PLAYLIST_WORKFLOW_STATE_ID_STR = "95E38A80-234C-4494-9F7A-006D8E4D6FDA";
    public static readonly Guid SHOW_PLAYLIST_WORKFLOW_STATE_ID = new Guid(SHOW_PLAYLIST_WORKFLOW_STATE_ID_STR);

    public const string SHOW_AUDIO_PLAYLIST_RES = "[Media.ShowAudioPlaylistAction]";
    public const string SHOW_VIDEO_PLAYLIST_RES = "[Media.ShowVideoPlaylistAction]";
    public const string SHOW_PIP_PLAYLIST_RES = "[Media.ShowPiPPlaylistAction]";

    #endregion

    #region Protected fields

    protected AsynchronousMessageQueue _messageQueue = null;
    protected readonly object _syncObj = new object();

    protected bool _isVisible;
    protected string _displayTitleResource = null;

    #endregion

    private void SubscribeToMessages()
    {
      _messageQueue = new AsynchronousMessageQueue(this, new string[]
        {
            PlayerManagerMessaging.CHANNEL,
            PlayerContextManagerMessaging.CHANNEL,
            WorkflowManagerMessaging.CHANNEL,
        });
      _messageQueue.MessageReceived += OnMessageReceived;
      _messageQueue.Start();
    }

    private void UnsubscribeFromMessages()
    {
      if (_messageQueue == null)
        return;
      _messageQueue.Shutdown();
      _messageQueue = null;
    }

    private void OnMessageReceived(AsynchronousMessageQueue queue, SystemMessage message)
    {
      if (message.ChannelName == PlayerContextManagerMessaging.CHANNEL)
      {
        PlayerContextManagerMessaging.MessageType messageType = (PlayerContextManagerMessaging.MessageType) message.MessageType;
        switch (messageType)
        {
          case PlayerContextManagerMessaging.MessageType.CurrentPlayerChanged:
            Update();
            break;
        }
      }
      else if (message.ChannelName == PlayerManagerMessaging.CHANNEL)
      {
        PlayerManagerMessaging.MessageType messageType = (PlayerManagerMessaging.MessageType) message.MessageType;
        switch (messageType)
        {
          case PlayerManagerMessaging.MessageType.PlayerSlotActivated:
          case PlayerManagerMessaging.MessageType.PlayerSlotDeactivated:
            Update();
            break;
        }
      }
      else if (message.ChannelName == WorkflowManagerMessaging.CHANNEL)
      {
        WorkflowManagerMessaging.MessageType messageType = (WorkflowManagerMessaging.MessageType) message.MessageType;
        switch (messageType)
        {
          case WorkflowManagerMessaging.MessageType.NavigationComplete:
            Update();
            break;
        }
      }
    }

    protected void Update()
    {
      IWorkflowManager workflowManager = ServiceRegistration.Get<IWorkflowManager>();
      IPlayerContextManager playerContextManager = ServiceRegistration.Get<IPlayerContextManager>();
      IPlayerContext pc = playerContextManager.GetPlayerContext(PlayerChoice.CurrentPlayer);
      bool visible = pc != null;
      string displayTitleRes = null;
      if (pc != null)
        switch (pc.AVType)
        {
          case AVType.Audio:
            displayTitleRes = SHOW_AUDIO_PLAYLIST_RES;
            break;
          case AVType.Video:
            displayTitleRes = pc.PlayerSlotController.SlotIndex == PlayerManagerConsts.PRIMARY_SLOT ?
                SHOW_VIDEO_PLAYLIST_RES : SHOW_PIP_PLAYLIST_RES;
            break;
          default:
            // Unknown player context type
            visible = false;
            break;
        }
      visible = visible && !workflowManager.IsStateContainedInNavigationStack(SHOW_PLAYLIST_WORKFLOW_STATE_ID);
      lock (_syncObj)
      {
        if (visible == _isVisible && displayTitleRes == _displayTitleResource)
          return;
        _isVisible = visible;
        _displayTitleResource = displayTitleRes;
      }
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
      get
      {
        lock (_syncObj)
          return LocalizationHelper.CreateResourceString(_displayTitleResource);
      }
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
      lock (_syncObj)
        return _isVisible;
    }

    public bool IsActionEnabled(NavigationContext context)
    {
      return true;
    }

    public void Execute()
    {
      IWorkflowManager workflowManager = ServiceRegistration.Get<IWorkflowManager>();
      workflowManager.NavigatePush(SHOW_PLAYLIST_WORKFLOW_STATE_ID);
    }

    #endregion
  }
}
