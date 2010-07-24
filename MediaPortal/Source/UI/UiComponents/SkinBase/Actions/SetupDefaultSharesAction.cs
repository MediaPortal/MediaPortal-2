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
using MediaPortal.Core.General;
using MediaPortal.Core.Messaging;
using MediaPortal.Core.Localization;
using MediaPortal.UI.Presentation.Workflow;
using MediaPortal.UI.ServerCommunication;
using MediaPortal.UI.Shares;

namespace MediaPortal.UiComponents.SkinBase.Actions
{
  /// <summary>
  /// Action which is visible when player slots are open. This action will show the dialog
  /// "DialogPlayerConfiguration" when executed.
  /// </summary>
  public class SetupDefaultSharesAction : IWorkflowContributor
  {
    #region Consts

    public const string SETUP_DEFAULT_SHARES_CONTRIBUTOR_MODEL_ID_STR = "35907215-09EE-4886-9E8B-8A222B9B6DCA";

    public static readonly Guid SETUP_DEFAULT_SHARES_CONTRIBUTOR_MODEL_ID = new Guid(SETUP_DEFAULT_SHARES_CONTRIBUTOR_MODEL_ID_STR);

    public const string SETUP_DEFAULT_SHARES_RES = "[SharesConfig.SetupDefaultShares]";

    #endregion

    #region Protected fields

    protected AsynchronousMessageQueue _messageQueue = null;

    #endregion

    void SubscribeToMessages()
    {
      _messageQueue = new AsynchronousMessageQueue(this, new string[]
        {
           ServerConnectionMessaging.CHANNEL,
           SharesMessaging.CHANNEL,
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
      if (message.ChannelName == ServerConnectionMessaging.CHANNEL)
      {
        ServerConnectionMessaging.MessageType messageType =
            (ServerConnectionMessaging.MessageType) message.MessageType;
        switch (messageType)
        {
          case ServerConnectionMessaging.MessageType.HomeServerAttached:
          case ServerConnectionMessaging.MessageType.HomeServerDetached:
            FireStateChanged();
            break;
        }
      }
      else if (message.ChannelName == SharesMessaging.CHANNEL)
      {
        SharesMessaging.MessageType messageType =
            (SharesMessaging.MessageType) message.MessageType;
        switch (messageType)
        {
          case SharesMessaging.MessageType.ShareAdded:
          case SharesMessaging.MessageType.ShareRemoved:
            FireStateChanged();
            break;
        }
      }
    }

    protected bool CanSetupDefaultShares
    {
      get
      {
        IServerConnectionManager serverConnectionManager = ServiceRegistration.Get<IServerConnectionManager>();
        SystemName homeServerSystem = serverConnectionManager.LastHomeServerSystem;
        bool localHomeServer = homeServerSystem == null ? false : homeServerSystem.IsLocalSystem();
        ILocalSharesManagement localSharesManagement = ServiceRegistration.Get<ILocalSharesManagement>();
        return !localHomeServer && localSharesManagement.Shares.Count == 0;
      }
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
      get { return CanSetupDefaultShares; }
    }

    public bool IsActionEnabled
    {
      get { return true; }
    }

    public IResourceString DisplayTitle
    {
      get { return null; }
    }

    public void Initialize()
    {
      SubscribeToMessages();
    }

    public void Uninitialize()
    {
      UnsubscribeFromMessages();
    }

    public void Execute()
    {
      ILocalSharesManagement localSharesManagement = ServiceRegistration.Get<ILocalSharesManagement>();
      if (CanSetupDefaultShares)
        localSharesManagement.SetupDefaultShares();
    }

    #endregion
  }
}
