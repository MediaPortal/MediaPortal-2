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

using System;
using MediaPortal.Common;
using MediaPortal.Common.Messaging;
using MediaPortal.Common.Localization;
using MediaPortal.UI.Presentation.Players;
using MediaPortal.UI.Presentation.Workflow;
using MediaPortal.UiComponents.SkinBase.General;
using MediaPortal.UiComponents.SkinBase.Models;

namespace MediaPortal.UiComponents.SkinBase.Actions
{
  /// <summary>
  /// Action which is visible when player slots are open. This action will show the dialog
  /// "DialogPlayerConfiguration" when executed.
  /// </summary>
  public class PlayerConfigurationAction : IWorkflowContributor
  {
    #region Consts

    public const string PLAYER_CONFIGURATION_CONTRIBUTOR_MODEL_ID_STR = "95DD6923-058A-4481-AF33-2455CEBB7A03";
    public static readonly Guid PLAYER_CONFIGURATION_CONTRIBUTOR_MODEL_ID = new Guid(PLAYER_CONFIGURATION_CONTRIBUTOR_MODEL_ID_STR);

    #endregion

    #region Protected fields

    protected AsynchronousMessageQueue _messageQueue = null;

    // This is the only attribute to be updated so we can optimize using volatile instead of using a lock
    protected volatile bool _isVisible;
    protected readonly IResourceString _displayTitle;

    #endregion

    public PlayerConfigurationAction()
    {
      _displayTitle = LocalizationHelper.CreateResourceString(Consts.RES_PLAYER_CONFIGURATION);
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
          case PlayerManagerMessaging.MessageType.PlayerSlotStarted:
          case PlayerManagerMessaging.MessageType.PlayerSlotClosed:
            Update();
            break;
        }
      }
    }

    protected void Update()
    {
      bool oldVisible = _isVisible;

      IPlayerContextManager playerContextManager = ServiceRegistration.Get<IPlayerContextManager>();
      _isVisible = playerContextManager.NumActivePlayerContexts > 0;
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
      PlayerConfigurationDialogModel.OpenPlayerConfigurationDialog();
    }

    #endregion
  }
}
