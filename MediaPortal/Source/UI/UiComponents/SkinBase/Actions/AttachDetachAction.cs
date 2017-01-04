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
using MediaPortal.UI.Presentation.Workflow;
using MediaPortal.UI.ServerCommunication;
using MediaPortal.UiComponents.SkinBase.General;

namespace MediaPortal.UiComponents.SkinBase.Actions
{
  /// <summary>
  /// Action which triggers a switch to workflow state DetachFromServer if a server is currently connected.
  /// </summary>
  public class AttachDetachAction : IWorkflowContributor
  {
    #region Consts

    public const string ATTACH_DETACH_CONTRIBUTOR_MODEL_ID_STR = "793DAD9F-F64C-4c7a-86C0-F5AA222D0CDB";
    public static readonly Guid ATTACH_DETACH_CONTRIBUTOR_MODEL_ID = new Guid(ATTACH_DETACH_CONTRIBUTOR_MODEL_ID_STR);

    #endregion

    #region Protected fields

    protected AsynchronousMessageQueue _messageQueue = null;

    // This is the only attribute to be updated so we can optimize using volatile instead of using a lock
    protected volatile IResourceString _titleRes = null;

    #endregion

    void SubscribeToMessages()
    {
      _messageQueue = new AsynchronousMessageQueue(this, new string[]
        {
            ServerConnectionMessaging.CHANNEL
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
      if (message.ChannelName == ServerConnectionMessaging.CHANNEL)
      {
        ServerConnectionMessaging.MessageType messageType =
            (ServerConnectionMessaging.MessageType) message.MessageType;
        switch (messageType)
        {
          case ServerConnectionMessaging.MessageType.HomeServerAttached:
          case ServerConnectionMessaging.MessageType.HomeServerDetached:
            Update();
            break;
        }
      }
    }

    protected void Update()
    {
      IServerConnectionManager scm = ServiceRegistration.Get<IServerConnectionManager>();
      _titleRes = LocalizationHelper.CreateResourceString(string.IsNullOrEmpty(scm.HomeServerSystemId) ? Consts.RES_SEARCH_FOR_SERVERS : Consts.RES_DETACH_FROM_SERVER);
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
      get { return _titleRes; }
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
      return true;
    }

    public bool IsActionEnabled(NavigationContext context)
    {
      return true;
    }

    public void Execute()
    {
      IWorkflowManager workflowManager = ServiceRegistration.Get<IWorkflowManager>();
      IServerConnectionManager scm = ServiceRegistration.Get<IServerConnectionManager>();
      workflowManager.NavigatePush(string.IsNullOrEmpty(scm.HomeServerSystemId) ?
          Consts.WF_STATE_ID_ATTACH_TO_SERVER : Consts.WF_STATE_ID_DETACH_FROM_SERVER);
    }

    #endregion
  }
}
