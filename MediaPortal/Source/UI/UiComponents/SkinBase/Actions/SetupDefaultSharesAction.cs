#region Copyright (C) 2007-2012 Team MediaPortal

/*
    Copyright (C) 2007-2012 Team MediaPortal
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
using MediaPortal.Common.General;
using MediaPortal.Common.Messaging;
using MediaPortal.Common.Localization;
using MediaPortal.UI.Presentation.Workflow;
using MediaPortal.UI.ServerCommunication;
using MediaPortal.UI.Shares;

namespace MediaPortal.UiComponents.SkinBase.Actions
{
  /// <summary>
  /// Action which sets up default client shares.
  /// </summary>
  public class SetupDefaultSharesAction : IWorkflowContributor
  {
    #region Consts

    public const string SETUP_DEFAULT_SHARES_CONTRIBUTOR_MODEL_ID_STR = "35907215-09EE-4886-9E8B-8A222B9B6DCA";
    public static readonly Guid SETUP_DEFAULT_SHARES_CONTRIBUTOR_MODEL_ID = new Guid(SETUP_DEFAULT_SHARES_CONTRIBUTOR_MODEL_ID_STR);

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
        IContentDirectory contentDirectory = serverConnectionManager.ContentDirectory;
        SystemName homeServerSystem = serverConnectionManager.LastHomeServerSystem;
        bool localHomeServer = homeServerSystem == null ? false : homeServerSystem.IsLocalSystem();
        bool homeServerConncted = contentDirectory != null;
        ILocalSharesManagement localSharesManagement = ServiceRegistration.Get<ILocalSharesManagement>();
        return localHomeServer ? (homeServerConncted && contentDirectory.GetShares(null, SharesFilter.All).Count == 0) :
            localSharesManagement.Shares.Count == 0;
      }
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

    public bool IsActionVisible(NavigationContext context)
    {
      return CanSetupDefaultShares;
    }

    public bool IsActionEnabled(NavigationContext context)
    {
      return true;
    }

    public void Execute()
    {
      IServerConnectionManager serverConnectionManager = ServiceRegistration.Get<IServerConnectionManager>();
      IContentDirectory contentDirectory = serverConnectionManager.ContentDirectory;
      SystemName homeServerSystem = serverConnectionManager.LastHomeServerSystem;
      bool localHomeServer = homeServerSystem == null ? false : homeServerSystem.IsLocalSystem();
      bool homeServerConncted = contentDirectory != null;

      ILocalSharesManagement localSharesManagement = ServiceRegistration.Get<ILocalSharesManagement>();
      if (localHomeServer)
      {
        if (homeServerConncted && contentDirectory.GetShares(null, SharesFilter.All).Count == 0)
          contentDirectory.SetupDefaultServerShares();
        FireStateChanged();
      }
      else
      {
        if (localSharesManagement.Shares.Count == 0)
          localSharesManagement.SetupDefaultShares();
      }
      // The shares config model listens to share update events from both the local shares management and the home server,
      // so we don't need to trigger an update of the shares lists here
    }

    #endregion
  }
}
