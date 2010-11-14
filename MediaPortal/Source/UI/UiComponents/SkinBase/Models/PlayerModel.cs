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
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with MediaPortal 2. If not, see <http://www.gnu.org/licenses/>.
*/

#endregion

using System;
using System.Drawing;
using MediaPortal.Core;
using MediaPortal.Core.General;
using MediaPortal.Core.Logging;
using MediaPortal.Core.Messaging;
using MediaPortal.UI.Presentation.Models;
using MediaPortal.UI.Presentation.Players;
using MediaPortal.Core.MediaManagement.ResourceAccess;

namespace MediaPortal.UiComponents.SkinBase.Models
{
  /// <summary>
  /// This model attends some screens for Video, Audio and Image media players.
  /// It is also used as data model for media backgrounds.
  /// </summary>
  public class PlayerModel : BaseMessageControlledModel
  {
    public const string PLAYER_MODEL_ID_STR = "A2F24149-B44C-498b-AE93-288213B87A1A";
    public static Guid PLAYER_MODEL_ID = new Guid(PLAYER_MODEL_ID_STR);

    public const float DEFAULT_PIP_HEIGHT = 108;
    public const float DEFAULT_PIP_WIDTH = 192;

    protected AbstractProperty _isPipVisibleProperty;
    protected AbstractProperty _isPipVideoVisibleProperty;
    protected AbstractProperty _isPipPictureVisibleProperty;
    protected AbstractProperty _piPPictureSourcePathProperty;
    protected AbstractProperty _primaryPictureSourcePathProperty;
    protected AbstractProperty _pipWidthProperty;
    protected AbstractProperty _pipHeightProperty;
    protected AbstractProperty _isMutedProperty;
    protected AbstractProperty _volumeProperty;

    protected IResourceLocator[] _currentPictureSourceLocator = new IResourceLocator[] {null, null};
    protected ILocalFsResourceAccessor[] _currentPictureResourceAccessor = new ILocalFsResourceAccessor[] {null, null};

    public PlayerModel()
    {
      _isPipVisibleProperty = new WProperty(typeof(bool), false);
      _isPipVideoVisibleProperty = new WProperty(typeof(bool), false);
      _isPipPictureVisibleProperty = new WProperty(typeof(bool), false);
      _piPPictureSourcePathProperty = new WProperty(typeof(string), string.Empty);
      _primaryPictureSourcePathProperty = new WProperty(typeof(string), string.Empty);
      _pipWidthProperty = new WProperty(typeof(float), 0f);
      _pipHeightProperty = new WProperty(typeof(float), 0f);
      _isMutedProperty = new WProperty(typeof(bool), false);
      _volumeProperty = new WProperty(typeof(int), 0);

      SubscribeToMessages();
      Update();
    }

    public override void Dispose()
    {
      base.Dispose();
      DisposePictureResourceAccessor(PlayerManagerConsts.PRIMARY_SLOT);
      DisposePictureResourceAccessor(PlayerManagerConsts.SECONDARY_SLOT);
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
      IPlayerContextManager playerContextManager = ServiceRegistration.Get<IPlayerContextManager>();
      IPlayerManager playerManager = ServiceRegistration.Get<IPlayerManager>();
      IPlayerContext primaryPlayerContext = playerContextManager.GetPlayerContext(PlayerManagerConsts.PRIMARY_SLOT);
      IPlayerContext secondaryPlayerContext = playerContextManager.GetPlayerContext(PlayerManagerConsts.SECONDARY_SLOT);
      IPlayer primaryPlayer = primaryPlayerContext == null ? null : primaryPlayerContext.CurrentPlayer;
      IPlayer secondaryPlayer = secondaryPlayerContext == null ? null : secondaryPlayerContext.CurrentPlayer;
      IPicturePlayer primaryPicturePlayer = primaryPlayer as IPicturePlayer;
      IVideoPlayer pipVideoPlayer = secondaryPlayer as IVideoPlayer;
      IPicturePlayer pipPicturePlayer = secondaryPlayer as IPicturePlayer;
      Size videoAspectRatio = pipVideoPlayer == null ? new Size(4, 3) : pipVideoPlayer.VideoAspectRatio;
      IsPipVisible = playerContextManager.IsPipActive;
      IsPipVideoVisible = pipVideoPlayer != null;
      IsPipPictureVisible = pipPicturePlayer != null;
      PipPictureSourcePath = pipPicturePlayer == null ? string.Empty : CheckLocalResourcePath(PlayerManagerConsts.SECONDARY_SLOT, pipPicturePlayer.CurrentPictureResourceLocator);
      PrimaryPictureSourcePath = primaryPicturePlayer == null ? string.Empty : CheckLocalResourcePath(PlayerManagerConsts.PRIMARY_SLOT, primaryPicturePlayer.CurrentPictureResourceLocator);
      IsMuted = playerManager.Muted;
      PipWidth = DEFAULT_PIP_WIDTH;
      PipHeight = pipVideoPlayer == null ? DEFAULT_PIP_HEIGHT : PipWidth*videoAspectRatio.Height/videoAspectRatio.Width;
      Volume = playerManager.Volume;
    }

    protected string CheckLocalResourcePath(int slot, IResourceLocator resourceLocator)
    {
      if (_currentPictureSourceLocator[slot] != resourceLocator)
      {
        DisposePictureResourceAccessor(slot);
        _currentPictureSourceLocator[slot] = resourceLocator;
        try
        {
          _currentPictureResourceAccessor[slot] = resourceLocator.CreateLocalFsAccessor();
        }
        catch (Exception e)
        {
          ServiceRegistration.Get<ILogger>().Warn("PlayerModel: Error creating local filesystem accessor for picture '{0}'", e, resourceLocator);
          return string.Empty;
        }
      }
      return _currentPictureResourceAccessor[slot].LocalFileSystemPath;
    }

    protected void DisposePictureResourceAccessor(int slot)
    {
      if (_currentPictureResourceAccessor[slot] != null)
        _currentPictureResourceAccessor[slot].Dispose();
      _currentPictureResourceAccessor[slot] = null;
    }

    public Guid ModelId
    {
      get { return PLAYER_MODEL_ID; }
    }

    #region Members to be accessed from the GUI

    public AbstractProperty IsPipVisibleProperty
    {
      get { return _isPipVisibleProperty; }
    }

    public bool IsPipVisible
    {
      get { return (bool) _isPipVisibleProperty.GetValue(); }
      internal set { _isPipVisibleProperty.SetValue(value); }
    }

    public AbstractProperty IsPipVideoVisibleProperty
    {
      get { return _isPipVideoVisibleProperty; }
    }

    public bool IsPipVideoVisible
    {
      get { return (bool) _isPipVideoVisibleProperty.GetValue(); }
      internal set { _isPipVideoVisibleProperty.SetValue(value); }
    }

    public AbstractProperty IsPipPictureVisibleProperty
    {
      get { return _isPipPictureVisibleProperty; }
    }

    public bool IsPipPictureVisible
    {
      get { return (bool) _isPipPictureVisibleProperty.GetValue(); }
      internal set { _isPipPictureVisibleProperty.SetValue(value); }
    }

    public AbstractProperty PiPPictureSourcePathProperty
    {
      get { return _piPPictureSourcePathProperty; }
    }

    public string PipPictureSourcePath
    {
      get { return (string) _piPPictureSourcePathProperty.GetValue(); }
      internal set { _piPPictureSourcePathProperty.SetValue(value); }
    }

    public AbstractProperty PrimaryPictureSourcePathProperty
    {
      get { return _primaryPictureSourcePathProperty; }
    }

    public string PrimaryPictureSourcePath
    {
      get { return (string) _primaryPictureSourcePathProperty.GetValue(); }
      internal set { _primaryPictureSourcePathProperty.SetValue(value); }
    }

    public AbstractProperty PipWidthProperty
    {
      get { return _pipWidthProperty; }
    }

    public float PipWidth
    {
      get { return (float) _pipWidthProperty.GetValue(); }
      internal set { _pipWidthProperty.SetValue(value); }
    }

    public AbstractProperty PipHeightProperty
    {
      get { return _pipHeightProperty; }
    }

    public float PipHeight
    {
      get { return (float) _pipHeightProperty.GetValue(); }
      internal set { _pipHeightProperty.SetValue(value); }
    }

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
      IPlayerManager playerManager = ServiceRegistration.Get<IPlayerManager>();
      playerManager.CloseSlot(playerIndex);
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
      IPlayerManager playerManager = ServiceRegistration.Get<IPlayerManager>();
      playerManager.SwitchSlots();
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
      IPlayerManager playerManager = ServiceRegistration.Get<IPlayerManager>();
      playerManager.VolumeUp();
    }

    public static void VolumeDown()
    {
      IPlayerManager playerManager = ServiceRegistration.Get<IPlayerManager>();
      playerManager.VolumeDown();
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

    #endregion

    #endregion
  }
}