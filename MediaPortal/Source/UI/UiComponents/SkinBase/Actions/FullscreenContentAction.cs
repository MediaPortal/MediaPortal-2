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

namespace MediaPortal.UiComponents.SkinBase.Actions
{
  public class FullscreenContentAction : IWorkflowContributor
  {
    #region Consts

    public const string FULLSCREEN_CONTENT_CONTRIBUTOR_MODEL_ID_STR = "08E19EDA-7BB3-4e74-8079-FFB0D52F3838";
    public static readonly Guid FULLSCREEN_CONTENT_CONTRIBUTOR_MODEL_ID = new Guid(FULLSCREEN_CONTENT_CONTRIBUTOR_MODEL_ID_STR);

    #endregion

    #region Protected fields

    protected AsynchronousMessageQueue _messageQueue = null;
    protected readonly object _syncObj = new object();

    protected bool _isVisible;
    protected IResourceString _displayTitleResource; // TODO: Listen for language changes; update display title

    #endregion

    void SubscribeToMessages()
    {
      _messageQueue = new AsynchronousMessageQueue(this, new string[]
        {
           PlayerManagerMessaging.CHANNEL,
           PlayerContextManagerMessaging.CHANNEL,
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
          case PlayerManagerMessaging.MessageType.PlayerStarted:
          case PlayerManagerMessaging.MessageType.PlayerStopped:
          case PlayerManagerMessaging.MessageType.PlayerEnded:
            Update();
            break;
        }
      }
      else if (message.ChannelName == PlayerContextManagerMessaging.CHANNEL)
      {
        PlayerContextManagerMessaging.MessageType messageType = (PlayerContextManagerMessaging.MessageType) message.MessageType;
        switch (messageType)
        {
          case PlayerContextManagerMessaging.MessageType.CurrentPlayerChanged:
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
      IPlayerContextManager playerContextManager = ServiceRegistration.Get<IPlayerContextManager>();
      IWorkflowManager workflowManager = ServiceRegistration.Get<IWorkflowManager>();
      IPlayerContext pcPrimary = playerContextManager.PrimaryPlayerContext;
      IPlayer primaryPlayer = pcPrimary == null ? null : pcPrimary.CurrentPlayer;
      IImagePlayer pp = primaryPlayer as IImagePlayer;
      IVideoPlayer vp = primaryPlayer as IVideoPlayer;
      IAudioPlayer ap = primaryPlayer as IAudioPlayer;
      bool visible = (pp != null || vp != null || ap != null) &&
          !workflowManager.IsStateContainedInNavigationStack(pcPrimary.FullscreenContentWorkflowStateId);
      IResourceString displayTitleRes;
      if (ap != null)
        displayTitleRes = LocalizationHelper.CreateResourceString(Consts.RES_AUDIO_VISUALIZATION);
      else if (vp != null)
        displayTitleRes = LocalizationHelper.CreateStaticString(
            LocalizationHelper.CreateResourceString(Consts.RES_FULLSCREEN_VIDEO).Evaluate(vp.Name));
      else
        displayTitleRes = LocalizationHelper.CreateResourceString(Consts.RES_FULLSCREEN_IMAGE);
      lock (_syncObj)
      {
        _isVisible = visible;
        _displayTitleResource = displayTitleRes;
      }
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
      get
      {
        lock (_syncObj)
          return _displayTitleResource;
      }
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
      lock (_syncObj)
        return _isVisible;
    }

    public bool IsActionEnabled(NavigationContext context)
    {
      return true;
    }

    public void Execute()
    {
      IPlayerContextManager playerContextManager = ServiceRegistration.Get<IPlayerContextManager>();
      IPlayerContext pc = playerContextManager.PrimaryPlayerContext;
      if (pc == null)
        return;
      IWorkflowManager workflowManager = ServiceRegistration.Get<IWorkflowManager>();
      workflowManager.NavigatePush(pc.FullscreenContentWorkflowStateId);
    }

    #endregion
  }
}
