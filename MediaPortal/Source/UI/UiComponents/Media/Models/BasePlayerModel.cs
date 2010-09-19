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
using System.Collections.Generic;
using MediaPortal.Core;
using MediaPortal.Core.General;
using MediaPortal.Core.Messaging;
using MediaPortal.UI.Presentation.Models;
using MediaPortal.UI.Presentation.Players;
using MediaPortal.UI.Presentation.Screens;
using MediaPortal.UI.Presentation.Workflow;

namespace MediaPortal.UiComponents.Media.Models
{
  public abstract class BasePlayerModel : BaseTimerControlledModel, IWorkflowModel
  {
    protected Guid _currentlyPlayingWorkflowStateId;
    protected Guid _fullscreenContentWorkflowStateId;
    protected AbstractProperty _currentPlayerIndexProperty;
    protected object _syncObj = new object();
    protected MediaWorkflowStateType _mediaWorkflowStateType = MediaWorkflowStateType.None;
    protected IPlayerUIContributor _playerUIContributor = null;
    protected bool _inactive = false;

    protected BasePlayerModel(Guid currentlyPlayingWorkflowStateId, Guid fullscreenContentWorkflowStateId) : base(300)
    {
      _currentlyPlayingWorkflowStateId = currentlyPlayingWorkflowStateId;
      _fullscreenContentWorkflowStateId = fullscreenContentWorkflowStateId;
      _currentPlayerIndexProperty = new WProperty(typeof(int), 0);
      _messageQueue.SubscribeToMessageChannel(PlayerManagerMessaging.CHANNEL);
      _messageQueue.SubscribeToMessageChannel(PlayerContextManagerMessaging.CHANNEL);
      _messageQueue.MessageReceived += OnMessageReceived;
      // Don't call StartTimer() and _messageQueue.Start() here, since that will be done in method EnterModelContext
    }

    void OnMessageReceived(AsynchronousMessageQueue queue, SystemMessage message)
    {
      switch (message.ChannelName)
      {
        case PlayerManagerMessaging.CHANNEL:
          UpdatePlayerContributor();
          break;
        case PlayerContextManagerMessaging.CHANNEL:
          UpdatePlayerContributor();
          break;
      }
    }

    protected override void Update()
    {
      IPlayerContextManager playerContextManager = ServiceRegistration.Get<IPlayerContextManager>();
      CurrentPlayerIndex = playerContextManager.CurrentPlayerIndex;
    }

    protected void UpdatePlayerContributor()
    {
      MediaWorkflowStateType stateType = _mediaWorkflowStateType;
      if (stateType == MediaWorkflowStateType.None)
        SetPlayerUIContributor(null, MediaWorkflowStateType.None, null);
      else
      {
        IPlayerContext pc = GetPlayerContext(_mediaWorkflowStateType);
        IPlayer player = pc == null ? null : pc.CurrentPlayer;
        Type playerUIContributorType = GetPlayerUIContributorType(player, stateType);
        SetPlayerUIContributor(playerUIContributorType, stateType, player);
      }

      Update();
    }

    /// <summary>
    /// Returns the type of the player UI contributor implementation to be used for the given <paramref name="player"/> and
    /// media workflow <paramref name="stateType"/>.
    /// Actually, the code to determine the correct player UI contributor should be implemented to be generic and extendible
    /// (and we should avoid to hard code the determination in sub classes). But we need a piece of code to execute specific
    /// checks. I don't want to make them too generic because the checks could be very specific. For example, it might be that
    /// the single cases are not disjoint. For example if a player supports two specific player interfaces.
    ///  In such a case, we might need intelligent code here.
    /// </summary>
    /// <param name="player">The player for that the player UI contributor should be instantiated.</param>
    /// <param name="stateType">The media workflow state type, we need the UI contributor for.</param>
    /// <returns>Type of the player UI contributor to use.</returns>
    protected abstract Type GetPlayerUIContributorType(IPlayer player, MediaWorkflowStateType stateType);

    public AbstractProperty CurrentPlayerIndexProperty
    {
      get { return _currentPlayerIndexProperty; }
    }

    public int CurrentPlayerIndex
    {
      get { return (int) _currentPlayerIndexProperty.GetValue(); }
      set { _currentPlayerIndexProperty.SetValue(value); }
    }

    protected void SetPlayerUIContributor(Type playerUIContributorType, MediaWorkflowStateType stateType, IPlayer player)
    {
      IPlayerUIContributor oldPlayerUIContributor;
      lock (_syncObj)
        oldPlayerUIContributor = _playerUIContributor;
      bool backgroundDisabled = false;
      try
      {
        if (oldPlayerUIContributor != null && playerUIContributorType == oldPlayerUIContributor.GetType())
        { // Player UI contributor is already correct, but maybe must be initialized
          if (oldPlayerUIContributor.MediaWorkflowStateType != stateType)
            oldPlayerUIContributor.Initialize(stateType, player);
          backgroundDisabled = oldPlayerUIContributor.BackgroundDisabled;
          return;
        }
        IPlayerUIContributor playerUIContributor = InstantiatePlayerUIContributor(playerUIContributorType);
        if (playerUIContributor != null)
        {
          playerUIContributor.Initialize(stateType, player);
          backgroundDisabled = playerUIContributor.BackgroundDisabled;
        }
        lock (_syncObj)
          _playerUIContributor = playerUIContributor;
        if (oldPlayerUIContributor != null)
          oldPlayerUIContributor.Dispose();
      }
      finally
      {
        IScreenManager screenManager = ServiceRegistration.Get<IScreenManager>();
        screenManager.BackgroundDisabled = backgroundDisabled;
      }
    }

    protected IPlayerUIContributor InstantiatePlayerUIContributor(Type playerUIContributorType)
    {
      return playerUIContributorType == null ? null : Activator.CreateInstance(playerUIContributorType) as IPlayerUIContributor;
    }

    protected MediaWorkflowStateType GetMediaWorkflowStateType(NavigationContext context)
    {
      if (context.WorkflowState.StateId == _currentlyPlayingWorkflowStateId)
        return MediaWorkflowStateType.CurrentlyPlaying;
      if (context.WorkflowState.StateId == _fullscreenContentWorkflowStateId)
        return MediaWorkflowStateType.FullscreenContent;
      return MediaWorkflowStateType.None;
    }

    protected IPlayerContext GetPlayerContext(MediaWorkflowStateType stateType)
    {
      IPlayerContextManager playerContextManager = ServiceRegistration.Get<IPlayerContextManager>();
      if (stateType == MediaWorkflowStateType.CurrentlyPlaying)
        // The "currently playing" screen is always bound to the "current player"
        return playerContextManager.CurrentPlayerContext;
      if (stateType == MediaWorkflowStateType.FullscreenContent)
        // The "fullscreen content" screen is always bound to the "primary player"
        return playerContextManager.GetPlayerContext(PlayerManagerConsts.PRIMARY_SLOT);
      return null;
    }

    public abstract Guid ModelId { get; }

    public bool CanEnterState(NavigationContext oldContext, NavigationContext newContext)
    {
      MediaWorkflowStateType stateType = GetMediaWorkflowStateType(newContext);
      IPlayerContext pc = GetPlayerContext(stateType);
      Type playerUIContributorType = pc == null ? null : GetPlayerUIContributorType(pc.CurrentPlayer, stateType);
      return playerUIContributorType != null;
    }

    public void EnterModelContext(NavigationContext oldContext, NavigationContext newContext)
    {
      _mediaWorkflowStateType = GetMediaWorkflowStateType(newContext);
      StartTimer();
      _messageQueue.Start();
      UpdatePlayerContributor();
    }

    public void ExitModelContext(NavigationContext oldContext, NavigationContext newContext)
    {
      _mediaWorkflowStateType = MediaWorkflowStateType.None;
      StopTimer();
      _messageQueue.Shutdown();
      UpdatePlayerContributor();
    }

    public void ChangeModelContext(NavigationContext oldContext, NavigationContext newContext, bool push)
    {
      _mediaWorkflowStateType = GetMediaWorkflowStateType(newContext);
      UpdatePlayerContributor();
    }

    public void Deactivate(NavigationContext oldContext, NavigationContext newContext)
    {
      _inactive = true;
    }

    public void ReActivate(NavigationContext oldContext, NavigationContext newContext)
    {
      _inactive = false;
    }

    public void UpdateMenuActions(NavigationContext context, IDictionary<Guid, WorkflowAction> actions)
    {
      // Nothing to do
    }

    public ScreenUpdateMode UpdateScreen(NavigationContext context, ref string screen)
    {
      IPlayerUIContributor playerUIContributor;
      lock (_syncObj)
        playerUIContributor = _playerUIContributor;
      if (playerUIContributor != null)
        screen = playerUIContributor.Screen;
      return ScreenUpdateMode.AutoWorkflowManager;
    }
  }
}