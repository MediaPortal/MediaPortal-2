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
using System.Timers;
using MediaPortal.Core;
using MediaPortal.Core.Messaging;
using MediaPortal.Presentation.DataObjects;
using MediaPortal.Presentation.Players;
using MediaPortal.Presentation.Screens;
using Timer=System.Timers.Timer;

namespace UiComponents.SkinBase
{
  /// <summary>
  /// This model provides skin data for the current primary player/playing state.
  /// </summary>
  public class PlayerModel : IDisposable
  {
    public const string PLAYER_MODEL_ID_STR = "A2F24149-B44C-498b-AE93-288213B87A1A";

    protected Timer _timer;

    protected Property _primaryPlayerProperty;
    protected Property _isPausedProperty;
    protected Property _isRunningProperty;
    protected Property _isMutedProperty;
    protected Property _isPlayerActiveProperty;
    protected Property _isPlayControlsVisibleProperty;

    public PlayerModel()
    {
      _primaryPlayerProperty = new Property(typeof(int), -1);
      _isPausedProperty = new Property(typeof(bool), false);
      _isRunningProperty = new Property(typeof(bool), false);
      _isPlayerActiveProperty = new Property(typeof(bool), false);
      _isMutedProperty = new Property(typeof(bool), false);
      _isPlayControlsVisibleProperty = new Property(typeof(bool), false);
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
      broker.GetOrCreate(PlayerManagerMessaging.QUEUE).OnMessageReceive += OnPlayerManagerMessageReceive;
    }

    protected void UnsubscribeFromMessages()
    {
      IMessageBroker broker = ServiceScope.Get<IMessageBroker>();
      broker.GetOrCreate(PlayerManagerMessaging.QUEUE).OnMessageReceive -= OnPlayerManagerMessageReceive;
    }

    protected void OnPlayerManagerMessageReceive(QueueMessage message)
    {
      PlayerManagerMessaging.MessageType messageType =
          (PlayerManagerMessaging.MessageType) message.MessageData[PlayerManagerMessaging.MESSAGE_TYPE];
      switch (messageType)
      {
        case PlayerManagerMessaging.MessageType.PrimaryPlayerChanged:
          PrimaryPlayer = (int) message.MessageData[PlayerManagerMessaging.PARAM];
          UpdatePlayControls();
          break;
        default:
          UpdatePlayControls();
          break;
      }
    }

    protected void OnTimerElapsed(object sender, ElapsedEventArgs e)
    {
      UpdatePlayControls();
    }

    protected void UpdatePlayControls()
    {
      IScreenControl screenControl = ServiceScope.Get<IScreenControl>();
      IPlayerManager playerManager = ServiceScope.Get<IPlayerManager>();
      IPlayer player = playerManager[playerManager.PrimaryPlayer];
      if (player == null)
      {
        IsPaused = false;
        IsRunning = false;
        IsPlayerActive = false;
        IsMuted = false;
        IsPlayControlsVisible = false;
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
      }
    }

    protected static IPlayer GetActivePlayer()
    {
      IPlayerManager playerManager = ServiceScope.Get<IPlayerManager>();
      return playerManager[playerManager.PrimaryPlayer];
    }

    public Property PrimaryPlayerProperty
    {
      get { return _primaryPlayerProperty; }
    }

    public int PrimaryPlayer
    {
      get { return (int) _primaryPlayerProperty.GetValue(); }
      set { _primaryPlayerProperty.SetValue(value); }
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

    public void TogglePause()
    {
      IPlayerManager playerManager = ServiceScope.Get<IPlayerManager>();
      IPlayer player = playerManager[playerManager.PrimaryPlayer];
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
      IPlayerManager playerManager = ServiceScope.Get<IPlayerManager>();
      IPlayer player = playerManager[playerManager.PrimaryPlayer];
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
      IPlayerManager playerManager = ServiceScope.Get<IPlayerManager>();
      IVolumeControl player = playerManager[playerManager.PrimaryPlayer] as IVolumeControl;
      if (player != null)
        player.Mute = !player.Mute;
    }
  }
}
