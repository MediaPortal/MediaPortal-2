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
using System.Collections.Generic;
using System.Timers;
using MediaPortal.Core;
using MediaPortal.Core.Messaging;
using MediaPortal.Presentation.DataObjects;
using MediaPortal.Presentation.Models;
using MediaPortal.Presentation.Players;
using MediaPortal.Presentation.Screens;
using MediaPortal.Presentation.Workflow;
using Timer=System.Timers.Timer;

namespace UiComponents.SkinBase
{
  /// <summary>
  /// This model provides skin data for the current primary player/playing state.
  /// </summary>
  public class PlayerModel : IDisposable, IWorkflowModel
  {
    public const string PLAYER_MODEL_ID_STR = "A2F24149-B44C-498b-AE93-288213B87A1A";

    public const string CURRENTLY_PLAYING_STATE_ID_STR = "5764A810-F298-4a20-BF84-F03D16F775B1";

    public const string FULLSCREENVIDEO_SCREEN_NAME = "FullScreenVideo";
    public const string FULLSCREENAUDIO_SCREEN_NAME = "FullScreenAudio";
    public const string FULLSCREENPICTURE_SCREEN_NAME = "FullScreenPicture";

    public static Guid PLAYER_MODEL_ID = new Guid(PLAYER_MODEL_ID_STR);
    public static Guid CURRENTLY_PLAYING_STATE_ID = new Guid(CURRENTLY_PLAYING_STATE_ID_STR);

    protected Timer _timer;

    protected Property _isPausedProperty;
    protected Property _isRunningProperty;
    protected Property _isMutedProperty;
    protected Property _isPlayerActiveProperty;
    protected Property _isPlayControlsVisibleProperty;
    protected Property _isPipProperty;
    protected Property _isVideoInfoVisibleProperty;
    protected bool _inCurrentlyPlaying = false;

    public PlayerModel()
    {
      _isPausedProperty = new Property(typeof(bool), false);
      _isRunningProperty = new Property(typeof(bool), false);
      _isPlayerActiveProperty = new Property(typeof(bool), false);
      _isMutedProperty = new Property(typeof(bool), false);
      _isPlayControlsVisibleProperty = new Property(typeof(bool), false);
      _isPipProperty = new Property(typeof(bool), false);
      _isVideoInfoVisibleProperty = new Property(typeof(bool), false);
      SubscribeToMessages();

      // Setup timer to update the properties
      _timer = new Timer(500);
      _timer.Elapsed += OnTimerElapsed;
      _timer.Enabled = true;
    }

    public void Dispose()
    {
      _timer.Elapsed -= OnTimerElapsed;
      _timer.Enabled = false;
      UnsubscribeFromMessages();
    }

    protected void SubscribeToMessages()
    {
      IMessageBroker broker = ServiceScope.Get<IMessageBroker>();
      broker.GetOrCreate(PlayerManagerMessaging.QUEUE).MessageReceived += OnPlayerManagerMessageReceived;
    }

    protected void UnsubscribeFromMessages()
    {
      IMessageBroker broker = ServiceScope.Get<IMessageBroker>();
      broker.GetOrCreate(PlayerManagerMessaging.QUEUE).MessageReceived -= OnPlayerManagerMessageReceived;
    }

    protected void OnPlayerManagerMessageReceived(QueueMessage message)
    {
      PlayerManagerMessaging.MessageType messageType =
          (PlayerManagerMessaging.MessageType) message.MessageData[PlayerManagerMessaging.MESSAGE_TYPE];
      switch (messageType)
      {
        case PlayerManagerMessaging.MessageType.PlayerStopped:
          if (_inCurrentlyPlaying)
          {
            IWorkflowManager workflowManager = ServiceScope.Get<IWorkflowManager>();
            // Maybe we should do more handling in this case - show dialogs "do you want to delete"
            // etc.? At the moment we'll simply return to the last workflow state.
            workflowManager.NavigatePop(1);
          }
          break;
        case PlayerManagerMessaging.MessageType.PlayerEnded:
          if (_inCurrentlyPlaying)
          {
            // TODO: Leave currently playing state if no playlist is running
          }
          break;
        case PlayerManagerMessaging.MessageType.PlayerStarted:
        case PlayerManagerMessaging.MessageType.PlayerResumed:
          if (_inCurrentlyPlaying)
            // Automatically switch "currently playing" screen if another player is started. This will
            // ensure that the screen is correctly updated when the playlist progresses.
            UpdateScreenForPrimaryPlayer();
          break;
      }
      UpdatePlayControls();
    }

    protected void OnTimerElapsed(object sender, ElapsedEventArgs e)
    {
      UpdatePlayControls();
    }

    protected void UpdatePlayControls()
    {
      IScreenControl screenControl = ServiceScope.Get<IScreenControl>();
      IPlayerManager playerManager = ServiceScope.Get<IPlayerManager>();
      IPlayer player = playerManager[0];
      if (player == null)
      {
        IsPaused = false;
        IsRunning = false;
        IsPlayerActive = false;
        IsMuted = false;
        IsPlayControlsVisible = false;
        IsVideoInfoVisible = false;

        IsPip = false;
      }
      else
      {
        player.UpdateTime();
        IsPaused = player.State == PlaybackState.Paused;
        IsRunning = player.State == PlaybackState.Playing;
        IsPlayerActive = true;
        IVolumeControl volumeControl = player as IVolumeControl;
        IsMuted = volumeControl != null && volumeControl.Mute;
        IsPlayControlsVisible = screenControl.IsMouseUsed;
        // TODO: Trigger video info overlay by a button
        IsVideoInfoVisible = screenControl.IsMouseUsed;

        // TODO: PIP configuration
        IsPip = false;
      }
    }

    protected static IPlayer GetPrimaryPlayer()
    {
      IPlayerManager playerManager = ServiceScope.Get<IPlayerManager>();
      return playerManager[0];
    }

    protected static bool CanHandlePlayer(IPlayer player)
    {
      return player is IVideoPlayer || player is IAudioPlayer || player is IPicturePlayer;
    }

    protected static void UpdateScreenForPrimaryPlayer()
    {
      IScreenManager screenManager = ServiceScope.Get<IScreenManager>();
      IPlayer player = GetPrimaryPlayer();
      string targetScreen;
      if (!CanHandlePlayer(player))
        return;
      if (player is IVideoPlayer)
        targetScreen = FULLSCREENVIDEO_SCREEN_NAME;
      else if (player is IAudioPlayer)
        targetScreen = FULLSCREENAUDIO_SCREEN_NAME;
      else if (player is IPicturePlayer)
        targetScreen = FULLSCREENPICTURE_SCREEN_NAME;
      else
          // Error case: The current player isn't recognized - its none of our supported players
        targetScreen = FULLSCREENVIDEO_SCREEN_NAME;
      if (screenManager.CurrentScreenName != targetScreen)
        screenManager.ShowScreen(targetScreen);
    }

    public Property IsPipProperty
    {
      get { return _isPipProperty; }
    }

    public bool IsPip
    {
      get { return (bool) _isPipProperty.GetValue(); }
      set { _isPipProperty.SetValue(value); }
    }

    public Property IsPausedProperty
    {
      get { return _isPausedProperty; }
    }

    public bool IsPaused
    {
      get { return (bool) _isPausedProperty.GetValue(); }
      set { _isPausedProperty.SetValue(value); }
    }

    public Property IsRunningProperty
    {
      get { return _isRunningProperty; }
    }

    public bool IsRunning
    {
      get { return (bool) _isRunningProperty.GetValue(); }
      set { _isRunningProperty.SetValue(value); }
    }

    public Property IsMutedProperty
    {
      get { return _isMutedProperty; }
    }

    public bool IsMuted
    {
      get { return (bool) _isMutedProperty.GetValue(); }
      set { _isMutedProperty.SetValue(value); }
    }

    public Property IsPlayControlsVisibleProperty
    {
      get { return _isPlayControlsVisibleProperty; }
    }

    public bool IsPlayControlsVisible
    {
      get { return (bool) _isPlayControlsVisibleProperty.GetValue(); }
      set { _isPlayControlsVisibleProperty.SetValue(value); }
    }

    public Property IsPlayerActiveProperty
    {
      get { return _isPlayerActiveProperty; }
    }

    public bool IsPlayerActive
    {
      get { return (bool) _isPlayerActiveProperty.GetValue(); }
      set { _isPlayerActiveProperty.SetValue(value); }
    }

    public Property IsVideoInfoVisibleProperty
    {
      get { return _isVideoInfoVisibleProperty; }
    }

    public bool IsVideoInfoVisible
    {
      get { return (bool) _isVideoInfoVisibleProperty.GetValue(); }
      set { _isVideoInfoVisibleProperty.SetValue(value); }
    }

    public void TogglePause()
    {
      IPlayer player = GetPrimaryPlayer();
      if (player != null)
        switch (player.State) {
          case PlaybackState.Playing:
            player.Pause();
            break;
          case PlaybackState.Paused:
            player.Resume();
            break;
          default:
            player.Restart();
            break;
        }
    }

    public void Stop()
    {
      IPlayer player = GetPrimaryPlayer();
      if (player != null)
        player.Stop();
    }

    public void Rewind()
    {
      // TODO
      IDialogManager dialogManager = ServiceScope.Get<IDialogManager>();
      dialogManager.ShowDialog("Not implemented", "The REWD function is not implemented yet", DialogType.OkDialog, false);
    }

    public void Forward()
    {
      // TODO
      IDialogManager dialogManager = ServiceScope.Get<IDialogManager>();
      dialogManager.ShowDialog("Not implemented", "The FWD function is not implemented yet", DialogType.OkDialog, false);
    }

    public void ToggleMute()
    {
      IVolumeControl player = GetPrimaryPlayer() as IVolumeControl;
      if (player != null)
        player.Mute = !player.Mute;
    }

    public void ShowCurrentlyPlaying()
    {
      IWorkflowManager workflowManager = ServiceScope.Get<IWorkflowManager>();
      workflowManager.NavigatePush(new Guid(CURRENTLY_PLAYING_STATE_ID_STR));
    }

    #region IWorkflowModel implementation

    public Guid ModelId
    {
      get { return PLAYER_MODEL_ID; }
    }

    public bool CanEnterState(NavigationContext oldContext, NavigationContext newContext)
    {
      return CanHandlePlayer(GetPrimaryPlayer());
    }

    public void EnterModelContext(NavigationContext oldContext, NavigationContext newContext)
    {
      if (newContext.WorkflowState.StateId == CURRENTLY_PLAYING_STATE_ID)
      {
        _inCurrentlyPlaying = true;
        UpdateScreenForPrimaryPlayer();
      }
    }

    public void ExitModelContext(NavigationContext oldContext, NavigationContext newContext)
    {
      if (oldContext.WorkflowState.StateId == CURRENTLY_PLAYING_STATE_ID)
        _inCurrentlyPlaying = false;
    }

    public void ChangeModelContext(NavigationContext oldContext, NavigationContext newContext, bool push)
    {
      // Not implemented
    }

    public void Deactivate(NavigationContext oldContext, NavigationContext newContext)
    {
      // Not implemented
    }

    public void ReActivate(NavigationContext oldContext, NavigationContext newContext)
    {
      // Not implemented
    }

    public void UpdateMenuActions(NavigationContext context, ICollection<WorkflowStateAction> actions)
    {
      // Not implemented yet
    }

    #endregion
  }
}
