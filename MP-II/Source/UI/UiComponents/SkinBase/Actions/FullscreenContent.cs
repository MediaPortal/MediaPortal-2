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
using MediaPortal.Presentation.DataObjects;
using MediaPortal.Presentation.Localization;
using MediaPortal.Presentation.Players;
using MediaPortal.Presentation.Workflow;

namespace UiComponents.SkinBase.Actions
{
  public class FullscreenContent : IWorkflowContributor, IDisposable
  {
    #region Consts

    public const string FULLSCREEN_CONTENT_CONTRIBUTOR_MODEL_ID_STR = "08E19EDA-7BB3-4e74-8079-FFB0D52F3838";

    public static Guid FULLSCREEN_CONTENT_CONTRIBUTOR_MODEL_ID = new Guid(FULLSCREEN_CONTENT_CONTRIBUTOR_MODEL_ID_STR);

    public const string FULLSCREEN_CONTENT_RESOURCE = "[Players.Fullscreen]";

    #endregion

    #region Protected fields

    protected bool _isVisible;
    protected IResourceString _displayTitle;

    #endregion

    protected void SubscribeToMessages()
    {
      IMessageBroker messageBroker = ServiceScope.Get<IMessageBroker>();
      messageBroker.Register_Async(PlayerManagerMessaging.QUEUE, OnPlayerManagerMessageReceived);
    }

    protected void UnsubscribeFromMessages()
    {
      IMessageBroker messageBroker = ServiceScope.Get<IMessageBroker>();
      messageBroker.Unregister_Async(PlayerManagerMessaging.QUEUE, OnPlayerManagerMessageReceived);
    }

    protected void OnPlayerManagerMessageReceived(QueueMessage message)
    {
      PlayerManagerMessaging.MessageType messageType =
          (PlayerManagerMessaging.MessageType) message.MessageData[PlayerManagerMessaging.MESSAGE_TYPE];
      switch (messageType)
      {
        case PlayerManagerMessaging.MessageType.PlayerStarted:
        case PlayerManagerMessaging.MessageType.PlayerStopped:
        case PlayerManagerMessaging.MessageType.PlayerEnded:
          Update();
          break;
      }
    }

    protected void Update()
    {
      IPlayerContextManager playerContextManager = ServiceScope.Get<IPlayerContextManager>();
      IPlayerContext pcPrimary = playerContextManager.GetPlayerContext(PlayerManagerConsts.PRIMARY_SLOT);
      IVideoPlayer vp = pcPrimary == null ? null : pcPrimary.CurrentPlayer as IVideoPlayer;
      _isVisible = vp != null;
      if (vp == null)
        _displayTitle = null;
      else
        _displayTitle =
            LocalizationHelper.CreateStaticString(
                LocalizationHelper.CreateResourceString(FULLSCREEN_CONTENT_RESOURCE).Evaluate(vp.Name));
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
      IPlayerContextManager playerContextManager = ServiceScope.Get<IPlayerContextManager>();
      IPlayerContext pc = playerContextManager.GetPlayerContext(PlayerManagerConsts.PRIMARY_SLOT);
      if (pc == null)
        return;
      IWorkflowManager workflowManager = ServiceScope.Get<IWorkflowManager>();
      workflowManager.NavigatePush(pc.FullscreenContentWorkflowStateId);
    }

    #endregion
  }
}