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
using MediaPortal.Core.Logging;
using MediaPortal.Core.Messaging;
using MediaPortal.Presentation.DataObjects;
using MediaPortal.Presentation.Localization;
using MediaPortal.Presentation.Players;
using MediaPortal.Presentation.Workflow;

namespace UiComponents.SkinBase.Actions
{
  /// <summary>
  /// Action which is visible when player slots are open. Depending on the currently active players, this action will
  /// show the current player's content.
  /// </summary>
  public class ShowPlayerContent : IWorkflowContributor, IDisposable
  {
    #region Consts

    public const string SHOW_CONTENT_CONTRIBUTOR_MODEL_ID_STR = "04854BDB-0933-4194-8AAE-DEC50062F37F";

    public static Guid SHOW_CONTENT_CONTRIBUTOR_MODEL_ID = new Guid(SHOW_CONTENT_CONTRIBUTOR_MODEL_ID_STR);

    public const string SHOW_FULLSCREEN_VIDEO_RESOURCE = "[Players.ShowFullscreenVideo]";
    public const string SHOW_CURRENTLY_PLAYING_RESOURCE = "[Players.ShowCurrentlyPlaying]";

    #endregion

    #region Protected fields

    protected bool _isVisible;
    protected IResourceString _displayTitle;

    #endregion

    protected void SubscribeToMessages()
    {
      IMessageBroker messageBroker = ServiceScope.Get<IMessageBroker>();
      messageBroker.GetOrCreate(PlayerManagerMessaging.QUEUE).MessageReceived_Async += OnPlayerManagerMessageReceived;
    }

    protected void UnsubscribeFromMessages()
    {
      IMessageBroker messageBroker = ServiceScope.Get<IMessageBroker>();
      messageBroker.GetOrCreate(PlayerManagerMessaging.QUEUE).MessageReceived_Async -= OnPlayerManagerMessageReceived;
    }

    protected void OnPlayerManagerMessageReceived(QueueMessage message)
    {
      PlayerManagerMessaging.MessageType messageType =
          (PlayerManagerMessaging.MessageType) message.MessageData[PlayerManagerMessaging.MESSAGE_TYPE];
      switch (messageType)
      {
        case PlayerManagerMessaging.MessageType.PlayerSlotActivated:
        case PlayerManagerMessaging.MessageType.PlayerSlotDeactivated:
          Update();
          break;
      }
    }

    protected void Update()
    {
      IPlayerManager playerManager = ServiceScope.Get<IPlayerManager>();
      _isVisible = playerManager.NumActiveSlots > 0;
      IPlayerSlotController psc = playerManager.GetPlayerSlotController(PlayerManagerConsts.PRIMARY_SLOT);
      IPlayer player = psc.IsActive ? psc.CurrentPlayer : null;
      if (player is IVideoPlayer)
        _displayTitle = LocalizationHelper.CreateResourceString(SHOW_FULLSCREEN_VIDEO_RESOURCE);
      else
        _displayTitle = LocalizationHelper.CreateResourceString(SHOW_CURRENTLY_PLAYING_RESOURCE);
      FireStateChanged();
    }

    protected void FireStateChanged()
    {
      ContributorStateChangeDelegate d = StateChanged;
      if (d != null) d();
    }

    #region IDisposable implementation

    public void Dispose()
    {
      UnsubscribeFromMessages();
    }

    #endregion

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

    public void Execute()
    {
      IPlayerManager playerManager = ServiceScope.Get<IPlayerManager>();
      IPlayerSlotController psc = playerManager.GetPlayerSlotController(PlayerManagerConsts.PRIMARY_SLOT);
      IPlayer player = psc.IsActive ? psc.CurrentPlayer : null;
      Guid? workflowState = null;
      if (player is IVideoPlayer)
        workflowState = player.FullscreenContentWorkflowStateId;
      else if (player != null)
        workflowState = player.CurrentlyPlayingWorkflowStateId;
      if (!workflowState.HasValue)
      {
        ServiceScope.Get<ILogger>().Warn("ShowPlayerContent: No workflow state present to show player content");
        return;
      }
      IWorkflowManager workflowManager = ServiceScope.Get<IWorkflowManager>();
      workflowManager.NavigatePush(workflowState.Value);
    }

    #endregion
  }
}
