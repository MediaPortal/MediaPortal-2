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
using MediaPortal.Core.Messaging;
using MediaPortal.Core.Localization;
using MediaPortal.UI.Presentation.Players;
using MediaPortal.UI.Presentation.Workflow;

namespace UiComponents.SkinBase.Actions
{
  /// <summary>
  /// Action which is visible when player slots are open. This action will show the dialog
  /// "DialogPlayerConfiguration" when executed.
  /// </summary>
  public class PlayerConfigurationAction : IWorkflowContributor
  {
    #region Consts

    public const string PLAYER_CONFIGURATION_CONTRIBUTOR_MODEL_ID_STR = "95DD6923-058A-4481-AF33-2455CEBB7A03";
    public const string PLAYER_CONFIGURATION_DIALOG_STATE_ID = "D0B79345-69DF-4870-B80E-39050434C8B3";

    public static readonly Guid PLAYER_CONFIGURATION_CONTRIBUTOR_MODEL_ID = new Guid(PLAYER_CONFIGURATION_CONTRIBUTOR_MODEL_ID_STR);
    public static readonly Guid PLAYER_CONFIGURATION_DIALOG_STATE = new Guid(PLAYER_CONFIGURATION_DIALOG_STATE_ID);

    public const string DISPLAY_TITLE_RESOURCE = "[Players.PlayerConfiguration]";

    #endregion

    #region Protected fields

    protected AsynchronousMessageQueue _messageQueue = null;
    protected volatile bool _isVisible;
    protected volatile IResourceString _displayTitle;

    #endregion

    public PlayerConfigurationAction()
    {
      _displayTitle = LocalizationHelper.CreateResourceString(DISPLAY_TITLE_RESOURCE);
    }

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

    protected void OnMessageReceived(AsynchronousMessageQueue queue, SystemMessage message)
    {
      if (message.ChannelName == PlayerManagerMessaging.CHANNEL)
      {
        PlayerManagerMessaging.MessageType messageType =
            (PlayerManagerMessaging.MessageType) message.MessageType;
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
      bool oldVisible = _isVisible;

      IPlayerManager playerManager = ServiceScope.Get<IPlayerManager>();
      _isVisible = playerManager.NumActiveSlots > 0;
      if (oldVisible != _isVisible)
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
      IWorkflowManager workflowManager = ServiceScope.Get<IWorkflowManager>();
      workflowManager.NavigatePush(PLAYER_CONFIGURATION_DIALOG_STATE, null);
    }

    #endregion
  }
}
