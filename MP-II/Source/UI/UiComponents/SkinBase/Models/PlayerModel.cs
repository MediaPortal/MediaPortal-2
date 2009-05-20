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
using MediaPortal.Presentation.Models;
using MediaPortal.Presentation.Players;
using MediaPortal.Presentation.Screens;

namespace UiComponents.SkinBase.Models
{
  /// <summary>
  /// This model attends the currently-playing and fullscreen-content workflow states for
  /// Video, Audio and Image media players. It is also used as data model for some dialogs to configure
  /// the players like "DialogPlayerConfiguration" and "DialogChooseAudioStream".
  /// </summary>
  public class PlayerModel : BaseMessageControlledUIModel
  {
    public const string PLAYER_MODEL_ID_STR = "A2F24149-B44C-498b-AE93-288213B87A1A";
    public static Guid PLAYER_MODEL_ID = new Guid(PLAYER_MODEL_ID_STR);

    public static float DEFAULT_PIP_HEIGHT = 108;
    public static float DEFAULT_PIP_WIDTH = 192;

    protected Property _isPipVisibleProperty;
    protected Property _pipWidthProperty;
    protected Property _pipHeightProperty;
    protected Property _isMutedProperty;

    public PlayerModel()
    {
      _isPipVisibleProperty = new Property(typeof(bool), false);
      _pipWidthProperty = new Property(typeof(float), 0f);
      _pipHeightProperty = new Property(typeof(float), 0f);
      _isMutedProperty = new Property(typeof(bool), false);

      SubscribeToMessages();
    }

    void SubscribeToMessages()
    {
      IMessageBroker broker = ServiceScope.Get<IMessageBroker>();
      broker.GetOrCreate(PlayerManagerMessaging.QUEUE).MessageReceived_Async += OnPlayerManagerMessageReceived;
    }

    protected override void UnsubscribeFromMessages()
    {
      base.UnsubscribeFromMessages();
      IMessageBroker broker = ServiceScope.Get<IMessageBroker>();
      broker.GetOrCreate(PlayerManagerMessaging.QUEUE).MessageReceived_Async -= OnPlayerManagerMessageReceived;
    }

    protected void OnPlayerManagerMessageReceived(QueueMessage message)
    {
      PlayerManagerMessaging.MessageType messageType =
          (PlayerManagerMessaging.MessageType) message.MessageData[PlayerManagerMessaging.MESSAGE_TYPE];
      switch (messageType)
      {
        case PlayerManagerMessaging.MessageType.PlayerSlotActivated:
        case PlayerManagerMessaging.MessageType.PlayerSlotDeactivated:
        case PlayerManagerMessaging.MessageType.PlayersMuted:
        case PlayerManagerMessaging.MessageType.PlayersResetMute:
          Update();
          break;
      }
    }

    protected void Update()
    {
      IPlayerContextManager playerContextManager = ServiceScope.Get<IPlayerContextManager>();
      IPlayerManager playerManager = ServiceScope.Get<IPlayerManager>();
      IPlayerContext secondaryPlayerContext = playerContextManager.GetPlayerContext(PlayerManagerConsts.SECONDARY_SLOT);
      IVideoPlayer pipPlayer = secondaryPlayerContext == null ? null : secondaryPlayerContext.CurrentPlayer as IVideoPlayer;
      IsPipVisible = playerContextManager.IsPipActive;
      IsMuted = playerManager.Muted;
      PipHeight = DEFAULT_PIP_HEIGHT;
      PipWidth = pipPlayer == null ? DEFAULT_PIP_WIDTH :
          PipHeight*pipPlayer.VideoAspectRatio.Width/pipPlayer.VideoAspectRatio.Height;
    }

    /// <summary>
    /// Returns the player context for the current focused player. The current player governs which
    /// "currently playing" screen is shown.
    /// </summary>
    /// <returns>Player context for the current player or <c>null</c>, if there is no current player.</returns>
    protected static IPlayerContext GetCurrentPlayerContext()
    {
      IPlayerContextManager pcm = ServiceScope.Get<IPlayerContextManager>();
      int currentPlayerSlot = pcm.CurrentPlayerIndex;
      if (currentPlayerSlot == -1)
        currentPlayerSlot = PlayerManagerConsts.PRIMARY_SLOT;
      return pcm.GetPlayerContext(currentPlayerSlot);
    }

    public override Guid ModelId
    {
      get { return PLAYER_MODEL_ID; }
    }

    #region Members to be accessed from the GUI

    public Property IsPipVisibleProperty
    {
      get { return _isPipVisibleProperty; }
    }

    public bool IsPipVisible
    {
      get { return (bool) _isPipVisibleProperty.GetValue(); }
      set { _isPipVisibleProperty.SetValue(value); }
    }

    public Property PipWidthProperty
    {
      get { return _pipWidthProperty; }
    }

    public float PipWidth
    {
      get { return (float) _pipWidthProperty.GetValue(); }
      set { _pipWidthProperty.SetValue(value); }
    }

    public Property PipHeightProperty
    {
      get { return _pipHeightProperty; }
    }

    public float PipHeight
    {
      get { return (float) _pipHeightProperty.GetValue(); }
      set { _pipHeightProperty.SetValue(value); }
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

    public void SetCurrentPlayer(int playerIndex)
    {
      IPlayerContextManager playerContextManager = ServiceScope.Get<IPlayerContextManager>();
      playerContextManager.CurrentPlayerIndex = playerIndex;
    }

    public void ClosePlayerContext(int playerIndex)
    {
      IPlayerManager playerManager = ServiceScope.Get<IPlayerManager>();
      playerManager.CloseSlot(playerIndex);
    }

    public void PlayersMute()
    {
      IPlayerManager playerManager = ServiceScope.Get<IPlayerManager>();
      playerManager.Muted = true;
    }

    public void PlayersResetMute()
    {
      IPlayerManager playerManager = ServiceScope.Get<IPlayerManager>();
      playerManager.Muted = false;
    }

    public void SwitchPrimarySecondaryPlayer()
    {
      IPlayerManager playerManager = ServiceScope.Get<IPlayerManager>();
      playerManager.SwitchSlots();
    }

    #endregion

    #region Methods for general play controls

    public static void Play()
    {
      IPlayerContext pc = GetCurrentPlayerContext();
      if (pc == null)
        return;
      if (pc.PlayerState == PlaybackState.Paused)
        pc.Pause();
      else
        pc.Restart();
    }

    public static void Pause()
    {
      IPlayerContext pc = GetCurrentPlayerContext();
      if (pc == null)
        return;
      pc.Pause();
    }

    public static void TogglePause()
    {
      IPlayerContextManager playerContextManager = ServiceScope.Get<IPlayerContextManager>();
      playerContextManager.TogglePlayPause();
    }

    public static void Stop()
    {
      IPlayerContext pc = GetCurrentPlayerContext();
      if (pc == null)
        return;
      pc.Stop();
    }

    public static void SeekBackward()
    {
      // TODO
      IDialogManager dialogManager = ServiceScope.Get<IDialogManager>();
      dialogManager.ShowDialog("Not implemented", "The BKWD command is not implemented yet", DialogType.OkDialog, false);
    }

    public static void SeekForward()
    {
      // TODO
      IDialogManager dialogManager = ServiceScope.Get<IDialogManager>();
      dialogManager.ShowDialog("Not implemented", "The FWD command is not implemented yet", DialogType.OkDialog, false);
    }

    public static void Previous()
    {
      IPlayerContext pc = GetCurrentPlayerContext();
      if (pc == null)
        return;
      pc.PreviousItem();
    }

    public static void Next()
    {
      IPlayerContext pc = GetCurrentPlayerContext();
      if (pc == null)
        return;
      pc.NextItem();
    }

    public static void VolumeUp()
    {
      IPlayerManager playerManager = ServiceScope.Get<IPlayerManager>();
      playerManager.VolumeUp();
    }

    public static void VolumeDown()
    {
      IPlayerManager playerManager = ServiceScope.Get<IPlayerManager>();
      playerManager.VolumeDown();
    }

    public static void ToggleMute()
    {
      IPlayerManager playerManager = ServiceScope.Get<IPlayerManager>();
      playerManager.Muted ^= true;
    }

    public static void ToggleCurrentPlayer()
    {
      IPlayerContextManager playerContextManager = ServiceScope.Get<IPlayerContextManager>();
      playerContextManager.ToggleCurrentPlayer();
    }

    #endregion
  }
}