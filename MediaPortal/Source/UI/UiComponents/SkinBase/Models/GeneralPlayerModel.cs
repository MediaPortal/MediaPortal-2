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
using MediaPortal.Common.General;
using MediaPortal.Common.Messaging;
using MediaPortal.UI.Presentation.Models;
using MediaPortal.UI.Presentation.Players;
using MediaPortal.UI.Presentation.Screens;
using MediaPortal.UiComponents.SkinBase.General;

namespace MediaPortal.UiComponents.SkinBase.Models
{
  /// <summary>
  /// This model provides properties for the global volume and mute state. It also provides methods for global player
  /// commands. For player slot specific commands, the SkinEngine's <c>PlayerControl</c> should be used.
  /// </summary>
  public class GeneralPlayerModel : BaseMessageControlledModel
  {
    #region Consts

    public const string PLAYER_MODEL_ID_STR = "A2F24149-B44C-498b-AE93-288213B87A1A";
    public static Guid PLAYER_MODEL_ID = new Guid(PLAYER_MODEL_ID_STR);

    public const int VOLUME_INCREMENT = 5;

    public static TimeSpan VOLUME_SUPERLAYER_TIME = TimeSpan.FromSeconds(2);

    #endregion

    protected AbstractProperty _isMutedProperty;
    protected AbstractProperty _volumeProperty;

    public GeneralPlayerModel()
    {
      _isMutedProperty = new WProperty(typeof(bool), false);
      _volumeProperty = new WProperty(typeof(int), 0);

      SubscribeToMessages();
      Update();
    }

    void SubscribeToMessages()
    {
      _messageQueue.SubscribeToMessageChannel(PlayerManagerMessaging.CHANNEL);
      _messageQueue.MessageReceived += OnMessageReceived;
    }

    void OnMessageReceived(AsynchronousMessageQueue queue, SystemMessage message)
    {
      if (message.ChannelName == PlayerManagerMessaging.CHANNEL)
        Update();
    }

    protected void Update()
    {
      IPlayerManager playerManager = ServiceRegistration.Get<IPlayerManager>();
      IsMuted = playerManager.Muted;
      Volume = playerManager.Volume;
    }

    protected static void ChangeVolume(int delta)
    {
      ISuperLayerManager superLayerManager = ServiceRegistration.Get<ISuperLayerManager>();
      superLayerManager.ShowSuperLayer(Consts.SCREEN_SUPERLAYER_VOLUME, VOLUME_SUPERLAYER_TIME);
      IPlayerManager playerManager = ServiceRegistration.Get<IPlayerManager>();
      playerManager.Volume += delta;
    }

    public Guid ModelId
    {
      get { return PLAYER_MODEL_ID; }
    }

    #region Members to be accessed from the GUI

    public AbstractProperty IsMutedProperty
    {
      get { return _isMutedProperty; }
    }

    public bool IsMuted
    {
      get { return (bool) _isMutedProperty.GetValue(); }
      internal set { _isMutedProperty.SetValue(value); }
    }

    public AbstractProperty VolumeProperty
    {
      get { return _volumeProperty; }
    }

    public int Volume
    {
      get { return (int) _volumeProperty.GetValue(); }
      internal set { _volumeProperty.SetValue(value); }
    }

    public void SetCurrentPlayer(int playerIndex)
    {
      IPlayerContextManager playerContextManager = ServiceRegistration.Get<IPlayerContextManager>();
      playerContextManager.CurrentPlayerIndex = playerIndex;
    }

    public void ClosePlayerContext(int playerIndex)
    {
      IPlayerContextManager playerContextManager = ServiceRegistration.Get<IPlayerContextManager>();
      IPlayerContext pc = playerContextManager.GetPlayerContext(playerIndex);
      if (pc != null)
        pc.Close();
    }

    public void PlayersMute()
    {
      IPlayerManager playerManager = ServiceRegistration.Get<IPlayerManager>();
      playerManager.Muted = true;
    }

    public void PlayersResetMute()
    {
      IPlayerManager playerManager = ServiceRegistration.Get<IPlayerManager>();
      playerManager.Muted = false;
    }

    public void SwitchPrimarySecondaryPlayer()
    {
      IPlayerContextManager playerContextManager = ServiceRegistration.Get<IPlayerContextManager>();
      playerContextManager.SwitchPipPlayers();
    }

    #region Methods for general play controls

    public static void Play()
    {
      IPlayerContextManager playerContextManager = ServiceRegistration.Get<IPlayerContextManager>();
      playerContextManager.Play();
    }

    public static void Pause()
    {
      IPlayerContextManager playerContextManager = ServiceRegistration.Get<IPlayerContextManager>();
      playerContextManager.Pause();
    }

    public static void TogglePause()
    {
      IPlayerContextManager playerContextManager = ServiceRegistration.Get<IPlayerContextManager>();
      playerContextManager.TogglePlayPause();
    }

    public static void Stop()
    {
      IPlayerContextManager playerContextManager = ServiceRegistration.Get<IPlayerContextManager>();
      playerContextManager.Stop();
    }

    public static void SeekForward()
    {
      IPlayerContextManager playerContextManager = ServiceRegistration.Get<IPlayerContextManager>();
      playerContextManager.SeekForward();
    }

    public static void SeekBackward()
    {
      IPlayerContextManager playerContextManager = ServiceRegistration.Get<IPlayerContextManager>();
      playerContextManager.SeekBackward();
    }

    public static void Previous()
    {
      IPlayerContextManager playerContextManager = ServiceRegistration.Get<IPlayerContextManager>();
      playerContextManager.PreviousItem();
    }

    public static void Next()
    {
      IPlayerContextManager playerContextManager = ServiceRegistration.Get<IPlayerContextManager>();
      playerContextManager.NextItem();
    }

    public static void VolumeUp()
    {
      ChangeVolume(VOLUME_INCREMENT);
    }

    public static void VolumeDown()
    {
      ChangeVolume(-VOLUME_INCREMENT);
    }

    public static void ToggleMute()
    {
      IPlayerManager playerManager = ServiceRegistration.Get<IPlayerManager>();
      playerManager.Muted ^= true;
    }

    public static void ToggleCurrentPlayer()
    {
      IPlayerContextManager playerContextManager = ServiceRegistration.Get<IPlayerContextManager>();
      playerContextManager.ToggleCurrentPlayer();
    }

    public static void SwitchPipPlayers()
    {
      IPlayerContextManager playerContextManager = ServiceRegistration.Get<IPlayerContextManager>();
      playerContextManager.SwitchPipPlayers();
    }

    #endregion

    #endregion
  }
}