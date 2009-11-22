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

    public static Guid CURRENT_MEDIA_CONTRIBUTOR_MODEL_ID = new Guid(CURRENT_MEDIA_CONTRIBUTOR_MODEL_ID_STR);

    public const string CURRENT_MEDIA_RESOURCE = "[Players.CurrentMedia]";

    #endregion

    #region Protected fields

    protected AsynchronousMessageQueue _messageQueue = null;
    protected bool _isVisible;

    #endregion

    void SubscribeToMessages()
    {
      _messageQueue = new AsynchronousMessageQueue(this, new string[]
        {
           PlayerManagerMessaging.CHANNEL
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
    }

    protected void Update()
    {
      IPlayerManager playerManager = ServiceScope.Get<IPlayerManager>();
      _isVisible = playerManager.NumActiveSlots > 0;
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
      get { return LocalizationHelper.CreateResourceString(CURRENT_MEDIA_RESOURCE); }
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
      workflowManager.NavigatePush(pc.CurrentlyPlayingWorkflowStateId);
    }

    #endregion
  }
}
