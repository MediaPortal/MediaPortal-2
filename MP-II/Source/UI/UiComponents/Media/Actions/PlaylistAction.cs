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
using MediaPortal.UI.Presentation.Players;
using MediaPortal.UI.Presentation.Workflow;

namespace UiComponents.Media.Actions
{
  public class PlaylistAction : IWorkflowContributor
  {
    #region Consts

    public const string SHOW_PLAYLIST_WORKFLOW_STATE_ID_STR = "95E38A80-234C-4494-9F7A-006D8E4D6FDA";
    public static readonly Guid SHOW_PLAYLIST_WORKFLOW_STATE_ID = new Guid(SHOW_PLAYLIST_WORKFLOW_STATE_ID_STR);

    public const string SHOW_AUDIO_PLAYLIST_RES = "[Media.ShowAudioPlaylist]";
    public const string SHOW_VIDEO_PLAYLIST_RES = "[Media.ShowVideoPlaylist]";
    public const string SHOW_PIP_PLAYLIST_RES = "[Media.ShowPiPPlaylist]";

    #endregion

    #region Protected fields

    protected AsynchronousMessageQueue _messageQueue = null;
    protected volatile bool _isVisible;

    protected string _displayTitleResource = null;

    #endregion

    public PlaylistAction()
    {
      Update();
    }

    void SubscribeToMessages()
    {
      _messageQueue = new AsynchronousMessageQueue(this, new string[]
        {
           PlaylistMessaging.CHANNEL,
           PlayerContextManagerMessaging.CHANNEL,
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
      if (message.ChannelName == PlaylistMessaging.CHANNEL)
      {
        PlaylistMessaging.MessageType messageType = (PlaylistMessaging.MessageType) message.MessageType;
        switch (messageType)
        {
          case PlaylistMessaging.MessageType.PlaylistAdvance:
          case PlaylistMessaging.MessageType.PropertiesChanged:
            Update();
            break;
        }
      }
      else if (message.ChannelName == PlayerContextManagerMessaging.CHANNEL)
      {
        PlayerContextManagerMessaging.MessageType messageType = (PlayerContextManagerMessaging.MessageType) message.MessageType;
        switch (messageType)
        {
          case PlayerContextManagerMessaging.MessageType.CurrentPlayerChanged:
            Update();
            break;
        }
      }
    }

    protected void Update()
    {
      IWorkflowManager workflowManager = ServiceScope.Get<IWorkflowManager>();
      IPlayerContextManager playerContextManager = ServiceScope.Get<IPlayerContextManager>();
      bool lastVisible = _isVisible;
      string lastDisplayTitleResource = _displayTitleResource;
      IPlayerContext pc = playerContextManager.GetPlayerContext(PlayerChoice.CurrentPlayer);
      _isVisible = pc != null;
      if (pc != null)
        switch (pc.MediaType)
        {
          case PlayerContextType.Audio:
            _displayTitleResource = SHOW_AUDIO_PLAYLIST_RES;
            break;
          case PlayerContextType.Video:
            _displayTitleResource = pc.PlayerSlotController.SlotIndex == PlayerManagerConsts.PRIMARY_SLOT ?
                SHOW_VIDEO_PLAYLIST_RES : SHOW_PIP_PLAYLIST_RES;
            break;
          default:
            // Unknown player context type
            _isVisible = false;
            _displayTitleResource = null;
            break;
        }
      _isVisible &= workflowManager.CurrentNavigationContext.WorkflowState.StateId != SHOW_PLAYLIST_WORKFLOW_STATE_ID;
      if (lastVisible != _isVisible || lastDisplayTitleResource != _displayTitleResource)
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
      workflowManager.NavigatePush(SHOW_PLAYLIST_WORKFLOW_STATE_ID, null);
    }

    #endregion
  }
}