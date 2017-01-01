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

using MediaPortal.Common;
using MediaPortal.Common.Localization;
using MediaPortal.Common.Messaging;
using MediaPortal.UI.Presentation.Players;
using MediaPortal.UI.Presentation.Workflow;
using MediaPortal.UiComponents.Media.General;
using MediaPortal.UiComponents.Media.Models;

namespace MediaPortal.UiComponents.Media.Actions
{
  public class SavePlaylistAction : IWorkflowContributor
  {
    #region Consts

    public const string SAVE_PLAYLISTS_ACTION_CONTRIBUTOR_MODEL_ID_STR = "02848CDD-34F0-4719-9A52-DA959E848409";

    #endregion

    #region Protected fields

    protected AsynchronousMessageQueue _messageQueue = null;
    protected readonly object _syncObj = new object();

    protected volatile bool _isVisible;
    protected volatile string _displayTitleResource;

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
          case PlayerManagerMessaging.MessageType.PlayerSlotStarted:
          case PlayerManagerMessaging.MessageType.PlayerSlotClosed:
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

    protected void FireStateChanged()
    {
      ContributorStateChangeDelegate d = StateChanged;
      if (d != null) d();
    }

    protected void Update()
    {
      IWorkflowManager workflowManager = ServiceRegistration.Get<IWorkflowManager>();
      bool showSavePL = workflowManager.CurrentNavigationContext.WorkflowState.StateId == Consts.WF_STATE_ID_SHOW_PLAYLIST ||
          workflowManager.CurrentNavigationContext.WorkflowState.StateId == Consts.WF_STATE_ID_EDIT_PLAYLIST;
      bool showSaveCurrentPL = workflowManager.CurrentNavigationContext.WorkflowState.StateId == Consts.WF_STATE_ID_PLAYLISTS_OVERVIEW;
      string displayTitleResource = showSavePL ? Consts.RES_SAVE_PLAYLIST : Consts.RES_SAVE_CURRENT_PLAYLIST;
      bool isVisible = (showSavePL || showSaveCurrentPL) &&
          ServiceRegistration.Get<IPlayerContextManager>().CurrentPlayerIndex > -1;
      if (isVisible == _isVisible && displayTitleResource == _displayTitleResource)
        return;
      _isVisible = isVisible;
      _displayTitleResource = displayTitleResource;
      FireStateChanged();
    }

    #region IWorkflowContributor implementation

    public event ContributorStateChangeDelegate StateChanged;

    public IResourceString DisplayTitle
    {
      get { return LocalizationHelper.CreateResourceString(_displayTitleResource); }
    }

    public void Initialize()
    {
      SubscribeToMessages();
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
      ManagePlaylistsModel.SaveCurrentPlaylist();
    }

    #endregion
  }
}