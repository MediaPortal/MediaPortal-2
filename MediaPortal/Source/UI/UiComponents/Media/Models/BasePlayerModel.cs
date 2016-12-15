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
using System.Collections.Generic;
using MediaPortal.Common;
using MediaPortal.Common.General;
using MediaPortal.Common.Messaging;
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
    protected MediaWorkflowStateType _mediaWorkflowStateType = MediaWorkflowStateType.None;
    protected AbstractProperty _playerUIContributorProperty = null;
    protected bool _inactive = false;
    protected string _screenName = null;
    protected bool _backgroundDisabled = false;
    protected IPlayer _oldPlayer;

    protected BasePlayerModel(Guid currentlyPlayingWorkflowStateId, Guid fullscreenContentWorkflowStateId) : base(false, 300)
    {
      _playerUIContributorProperty = new WProperty(typeof(IPlayerUIContributor));
      _currentlyPlayingWorkflowStateId = currentlyPlayingWorkflowStateId;
      _fullscreenContentWorkflowStateId = fullscreenContentWorkflowStateId;
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
          UpdatePlayerContributor(true);
          break;
        case PlayerContextManagerMessaging.CHANNEL:
          UpdatePlayerContributor(true);
          break;
      }
    }

    protected void UpdatePlayerContributor(bool doUpdateScreen)
    {
      MediaWorkflowStateType stateType = _mediaWorkflowStateType;
      if (stateType == MediaWorkflowStateType.None)
        SetPlayerUIContributor(null, MediaWorkflowStateType.None, null, doUpdateScreen);
      else
      {
        IPlayerContext pc = GetPlayerContext(_mediaWorkflowStateType);
        IPlayer player = pc == null ? null : pc.CurrentPlayer;
        Type playerUIContributorType = GetPlayerUIContributorType(player, stateType);
        SetPlayerUIContributor(playerUIContributorType, stateType, player, doUpdateScreen);
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

    public AbstractProperty PlayerUIContributorProperty
    {
      get { return _playerUIContributorProperty; }
    }

    public IPlayerUIContributor PlayerUIContributor
    {
      get { return (IPlayerUIContributor) _playerUIContributorProperty.GetValue(); }
      internal set { _playerUIContributorProperty.SetValue(value); }
    }

    protected void SetPlayerUIContributor(Type playerUIContributorType, MediaWorkflowStateType stateType, IPlayer player, bool doUpdateScreen)
    {
      IPlayerUIContributor oldPlayerUIContributor;
      lock (_syncObj)
        oldPlayerUIContributor = PlayerUIContributor;
      try
      {
        if (oldPlayerUIContributor != null && playerUIContributorType == oldPlayerUIContributor.GetType())
        { // Player UI contributor is already correct, but maybe must be initialized
          if (oldPlayerUIContributor.MediaWorkflowStateType != stateType || _oldPlayer != player)
            oldPlayerUIContributor.Initialize(stateType, player);
          _backgroundDisabled = oldPlayerUIContributor.BackgroundDisabled;
          _screenName = oldPlayerUIContributor.Screen;
          _oldPlayer = player;
          return;
        }
        IPlayerUIContributor playerUIContributor = InstantiatePlayerUIContributor(playerUIContributorType);
        if (playerUIContributor != null)
        {
          playerUIContributor.Initialize(stateType, player);
          _backgroundDisabled = playerUIContributor.BackgroundDisabled;
          _screenName = playerUIContributor.Screen;
        }
        else
        {
          _backgroundDisabled = false;
          _screenName = null;
        }
        lock (_syncObj)
          PlayerUIContributor = playerUIContributor;
        if (oldPlayerUIContributor != null)
          oldPlayerUIContributor.Dispose();
      }
      finally
      {
        if (doUpdateScreen)
          UpdateScreen();
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
        return playerContextManager.PrimaryPlayerContext;
      return null;
    }

    protected bool UpdateScreen()
    {
      IScreenManager screenManager = ServiceRegistration.Get<IScreenManager>();
      if (_screenName != null)
        if (!screenManager.CheckScreen(_screenName, !_backgroundDisabled).HasValue)
          // If the opened screen is not present or erroneous, we cannot update the screen
          return false;
      screenManager.BackgroundDisabled = _backgroundDisabled;
      return true;
    }

    protected void RestoreBackground()
    {
      IScreenManager screenManager = ServiceRegistration.Get<IScreenManager>();
      screenManager.BackgroundDisabled = false;
    }

    public abstract Guid ModelId { get; }

    public bool CanEnterState(NavigationContext oldContext, NavigationContext newContext)
    {
      return true;
    }

    public void EnterModelContext(NavigationContext oldContext, NavigationContext newContext)
    {
      _mediaWorkflowStateType = GetMediaWorkflowStateType(newContext);
      StartTimer();
      _messageQueue.Start();
      UpdatePlayerContributor(false);
    }

    public void ExitModelContext(NavigationContext oldContext, NavigationContext newContext)
    {
      _mediaWorkflowStateType = MediaWorkflowStateType.None;
      StopTimer();
      _messageQueue.Shutdown();
      UpdatePlayerContributor(false);
      RestoreBackground();
    }

    public void ChangeModelContext(NavigationContext oldContext, NavigationContext newContext, bool push)
    {
      _mediaWorkflowStateType = GetMediaWorkflowStateType(newContext);
      UpdatePlayerContributor(false);
    }

    public void Deactivate(NavigationContext oldContext, NavigationContext newContext)
    {
      _inactive = true;
      RestoreBackground();
    }

    public void Reactivate(NavigationContext oldContext, NavigationContext newContext)
    {
      _inactive = false;
    }

    public void UpdateMenuActions(NavigationContext context, IDictionary<Guid, WorkflowAction> actions)
    {
      // Nothing to do
    }

    public ScreenUpdateMode UpdateScreen(NavigationContext context, ref string screen)
    {
      return UpdateScreen() ? ScreenUpdateMode.ManualWorkflowModel : ScreenUpdateMode.AutoWorkflowManager;
    }
  }
}