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
using MediaPortal.Presentation.Players;
using MediaPortal.Presentation.Screens;
using MediaPortal.Presentation.Workflow;

namespace UiComponents.SkinBase.Actions
{
  /// <summary>
  /// Action which is visible when player slots are open. This action will show the dialog
  /// "dialogPlayerConfiguration" when executed.
  /// </summary>
  public class PlayerConfiguration : IWorkflowContributor, IDisposable
  {
    #region Consts

    public const string PLAYER_CONFIGURATION_CONTRIBUTOR_MODEL_ID_STR = "95DD6923-058A-4481-AF33-2455CEBB7A03";

    public const string PLAYER_CONFIGURATION_DIALOG_NAME = "DialogPlayerConfiguration";

    public static Guid PLAYER_CONFIGURATION_CONTRIBUTOR_MODEL_ID = new Guid(PLAYER_CONFIGURATION_CONTRIBUTOR_MODEL_ID_STR);

    #endregion

    #region Protected fields

    protected bool _isVisible;

    #endregion

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

    #region IDisposable implementation

    public void Dispose()
    {
      IMessageBroker messageBroker = ServiceScope.Get<IMessageBroker>();
      messageBroker.GetOrCreate(PlayerManagerMessaging.QUEUE).MessageReceived -= OnPlayerManagerMessageReceived;
    }

    #endregion

    #region IWorkflowContributor implementation

    public event ContributorStateChangeDelegate StateChanged;

    public bool IsActionVisible
    {
      get
      {
        IPlayerManager playerManager = ServiceScope.Get<IPlayerManager>();
        return playerManager.NumActiveSlots > 0;
      }
    }

    public bool IsActionEnabled
    {
      get { return true; }
    }

    public void Initialize()
    {
      IMessageBroker messageBroker = ServiceScope.Get<IMessageBroker>();
      messageBroker.GetOrCreate(PlayerManagerMessaging.QUEUE).MessageReceived += OnPlayerManagerMessageReceived;
      Update();
    }

    public void Execute()
    {
      IScreenManager screenManager = ServiceScope.Get<IScreenManager>();
      screenManager.ShowDialog(PLAYER_CONFIGURATION_DIALOG_NAME);
    }

    #endregion
  }
}
