#region Copyright (C) 2007-2010 Team MediaPortal

/*
    Copyright (C) 2007-2010 Team MediaPortal
    http://www.team-mediaportal.com
 
    This file is part of MediaPortal 2

    MediaPortal 2 is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    MediaPortal 2 is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with MediaPortal 2.  If not, see <http://www.gnu.org/licenses/>.
*/

#endregion

using System;
using MediaPortal.Core;
using MediaPortal.Core.Messaging;
using MediaPortal.Core.Localization;
using MediaPortal.UI.Presentation.Players;
using MediaPortal.UI.Presentation.Workflow;

namespace UiComponents.SkinBase.Actions
{
  /// <summary>
  /// Action which is visible when player slots are open. Depending on the currently active players, this action will
  /// show the current player's content.
  /// </summary>
  public class CurrentMediaAction : IWorkflowContributor
  {
    #region Consts

    public const string CURRENT_MEDIA_CONTRIBUTOR_MODEL_ID_STR = "04854BDB-0933-4194-8AAE-DEC50062F37F";

    public static readonly Guid CURRENT_MEDIA_CONTRIBUTOR_MODEL_ID = new Guid(CURRENT_MEDIA_CONTRIBUTOR_MODEL_ID_STR);

    public const string CURRENT_MEDIA_RESOURCE = "[Players.CurrentMedia]";

    #endregion

    #region Protected fields

    protected AsynchronousMessageQueue _messageQueue = null;

    // This is the only attribute to be updated so we can optimize using volatile instead of using a lock
    protected volatile bool _isVisible;
    protected readonly IResourceString _displayTitle; // TODO: Listen for language changes; update display title

    #endregion

    public CurrentMediaAction()
    {
      _displayTitle = LocalizationHelper.CreateResourceString(CURRENT_MEDIA_RESOURCE);
    }

    void SubscribeToMessages()
    {
      _messageQueue = new AsynchronousMessageQueue(this, new string[]
        {
           PlayerManagerMessaging.CHANNEL,
           WorkflowManagerMessaging.CHANNEL,
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
      if (message.ChannelName == PlayerManagerMessaging.CHANNEL)
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
      IPlayerContextManager playerContextManager = ServiceScope.Get<IPlayerContextManager>();
      IWorkflowManager workflowManager = ServiceScope.Get<IWorkflowManager>();
      IPlayerContext pc = playerContextManager.CurrentPlayerContext;
      bool visible = pc == null ? false :
          workflowManager.CurrentNavigationContext.WorkflowState.StateId != pc.CurrentlyPlayingWorkflowStateId;
      if (visible == _isVisible)
        return;
      _isVisible = visible;
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

    public void Execute()
    {
      IPlayerContextManager playerContextManager = ServiceScope.Get<IPlayerContextManager>();
      IPlayerContext pc = playerContextManager.CurrentPlayerContext;
      if (pc == null)
        return;
      IWorkflowManager workflowManager = ServiceScope.Get<IWorkflowManager>();
      workflowManager.NavigatePush(pc.CurrentlyPlayingWorkflowStateId, null);
    }

    #endregion
  }
}
