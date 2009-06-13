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
using System.Globalization;
using System.Timers;
using MediaPortal.Control.InputManager;
using MediaPortal.Core;
using MediaPortal.Core.MediaManagement;
using MediaPortal.Core.MediaManagement.DefaultItemAspects;
using MediaPortal.Core.Messaging;
using MediaPortal.Presentation.DataObjects;
using MediaPortal.Presentation.Localization;
using MediaPortal.Presentation.Players;
using MediaPortal.Presentation.Workflow;
using MediaPortal.SkinEngine.Xaml;
using MediaPortal.Utilities.DeepCopy;

namespace MediaPortal.SkinEngine.SpecialElements.Controls
{
  /// <summary>
  /// Visible Control providing the overview data for one player slot. This control can be decorated by different
  /// templates providing the player data.
  /// </summary>
  public class PlayerControl : SkinEngine.Controls.Visuals.Control
  {
    #region Consts

    public const string NO_MEDIA_ITEM_RESOURCE = "[PlayerControl.NoMediaItem]";
    public const string NO_PLAYER_RESOURCE = "[PlayerControl.NoPlayer]";
    public const string PLAYER_PLAYING_RESOURCE = "[PlayerControl.Playing]";
    public const string PLAYER_PAUSED_RESOURCE = "[PlayerControl.Paused]";
    public const string PLAYER_SEEKING_FORWARD_RESOURCE = "[PlayerControl.SeekingForward]";
    public const string PLAYER_SEEKING_BACKWARD_RESOURCE = "[PlayerControl.SeekingBackward]";
    public const string PLAYER_SLOWMOTION_BACKWARD_RESOURCE = "[PlayerControl.SlowMotionBackward]";
    public const string PLAYER_SLOWMOTION_FORWARD_RESOURCE = "[PlayerControl.SlowMotionForward]";
    public const string PLAYER_STOPPED_RESOURCE = "[PlayerControl.Stopped]";
    public const string PLAYER_ENDED_RESOURCE = "[PlayerControl.Ended]";
    public const string PLAYER_ACTIVE_RESOURCE = "[PlayerControl.Active]";
    public const string UNKNOWN_MEDIA_ITEM_RESOURCE = "[PlayerControl.UnknownMediaItem]";
    public const string UNKNOWN_PLAYER_CONTEXT_NAME_RESOURCE = "[PlayerControl.UnknownPlayerContextName]";
    public const string HEADER_NORMAL_RESOURCE = "[PlayerControl.HeaderNormal]";
    public const string HEADER_PIP_RESOURCE = "[PlayerControl.HeaderPip]";
    public const string PLAYBACK_RATE_HINT_RESOURCE = "[PlayerControl.PlaybackRateHint]";

    public const string PLAYER_SLOT_AUDIO_MENU_DIALOG_STATE_ID_STR = "428326CE-9DE1-41ff-A33B-BBB80C8AFAC5";
    public static Guid PLAYER_SLOT_AUDIO_MENU_DIALOG_STATE_ID = new Guid(PLAYER_SLOT_AUDIO_MENU_DIALOG_STATE_ID_STR);

    public const string KEY_PLAYER_SLOT = "PlayerSlot";
    public const string KEY_SHOW_MUTE = "ShowMute";

    #endregion

    #region Protected fields

    // Direct properties/fields
    protected Property _slotIndexProperty;
    protected Property _autoVisibilityProperty;
    protected bool _stickToPlayerContext;
    protected float _fixedVideoWidth;
    protected float _fixedVideoHeight;
    protected Timer _timer;
    protected bool _initialized = false;
    protected bool _updating = false;
    protected AsynchronousMessageQueue _messageQueue = null;

    // Derived properties/fields
    protected Property _isPlayerPresentProperty;
    protected Property _titleProperty;
    protected Property _mediaItemTitleProperty;
    protected Property _isAudioProperty;
    protected Property _isMutedProperty;
    protected Property _isPlayingProperty;
    protected Property _isPausedProperty;
    protected Property _isSeekingForwardProperty;
    protected Property _isSeekingBackwardProperty;
    protected Property _seekHintProperty;
    protected Property _isCurrentPlayerProperty;
    protected Property _percentPlayedProperty;
    protected Property _currentTimeProperty;
    protected Property _durationProperty;
    protected Property _playerStateTextProperty;
    protected Property _showMouseControlsProperty;
    protected Property _canPlayProperty;
    protected Property _canPauseProperty;
    protected Property _canStopProperty;
    protected Property _canSkipForwardProperty;
    protected Property _canSkipBackProperty;
    protected Property _canSeekForwardProperty;
    protected Property _canSeekBackwardProperty;
    protected Property _isPlayerActiveProperty;
    protected Property _isPipProperty;
    protected Property _videoWidthProperty;
    protected Property _videoHeightProperty;

    protected IResourceString _headerNormalResource;
    protected IResourceString _headerPipResource;
    protected IResourceString _playbackRateHintResource;

    #endregion

    #region Ctor

    public PlayerControl()
    {
      Init();
      Attach();
      SubscribeToMessages();
      UpdateProperties();
    }

    void Init()
    {
      _slotIndexProperty = new Property(typeof(int), 0);
      _autoVisibilityProperty = new Property(typeof(bool), false);
      _isPlayerPresentProperty = new Property(typeof(bool), false);
      _titleProperty = new Property(typeof(string), null);
      _mediaItemTitleProperty = new Property(typeof(string), null);
      _isAudioProperty = new Property(typeof(bool), false);
      _isMutedProperty = new Property(typeof(bool), false);
      _isPlayingProperty = new Property(typeof(bool), false);
      _isPausedProperty = new Property(typeof(bool), false);
      _isSeekingForwardProperty = new Property(typeof(bool), false);
      _isSeekingBackwardProperty = new Property(typeof(bool), false);
      _seekHintProperty = new Property(typeof(string), string.Empty);
      _isCurrentPlayerProperty = new Property(typeof(bool), false);
      _percentPlayedProperty = new Property(typeof(float), 0f);
      _currentTimeProperty = new Property(typeof(string), string.Empty);
      _durationProperty = new Property(typeof(string), string.Empty);
      _playerStateTextProperty = new Property(typeof(string), string.Empty);
      _showMouseControlsProperty = new Property(typeof(bool), false);
      _canPlayProperty = new Property(typeof(bool), false);
      _canPauseProperty = new Property(typeof(bool), false);
      _canStopProperty = new Property(typeof(bool), false);
      _canSkipForwardProperty = new Property(typeof(bool), false);
      _canSkipBackProperty = new Property(typeof(bool), false);
      _canSeekForwardProperty = new Property(typeof(bool), false);
      _canSeekBackwardProperty = new Property(typeof(bool), false);
      _isPlayerActiveProperty = new Property(typeof(bool), false);
      _isPipProperty = new Property(typeof(bool), false);
      _stickToPlayerContext = false;
      _fixedVideoWidth = 0f;
      _fixedVideoHeight = 0f;
      _videoWidthProperty = new Property(typeof(float), 0f);
      _videoHeightProperty = new Property(typeof(float), 0f);

      _timer = new Timer(200);
      _timer.Enabled = false;
      _timer.Elapsed += OnTimerElapsed;

      _headerNormalResource = LocalizationHelper.CreateResourceString(HEADER_NORMAL_RESOURCE);
      _headerPipResource = LocalizationHelper.CreateResourceString(HEADER_PIP_RESOURCE);
      _playbackRateHintResource = LocalizationHelper.CreateResourceString(PLAYBACK_RATE_HINT_RESOURCE);
    }

    void Attach()
    {
      _slotIndexProperty.Attach(OnPropertyChanged);
      _autoVisibilityProperty.Attach(OnPropertyChanged);
      _isMutedProperty.Attach(OnMuteChanged);
    }

    void Detach()
    {
      _slotIndexProperty.Detach(OnPropertyChanged);
      _autoVisibilityProperty.Detach(OnPropertyChanged);
    }

    public override void DeepCopy(IDeepCopyable source, ICopyManager copyManager)
    {
      Detach();
      base.DeepCopy(source, copyManager);
      PlayerControl pc = (PlayerControl) source;

      SlotIndex = copyManager.GetCopy(pc.SlotIndex);
      AutoVisibility = copyManager.GetCopy(pc.AutoVisibility);
      Attach();
      UpdateProperties();
    }

    public override void Dispose()
    {
      base.Dispose();
      UnsubscribeFromMessages();
      StopTimer();
    }

    #endregion

    #region Private & protected methods

    void OnPropertyChanged(Property prop, object oldValue)
    {
      UpdateProperties();
    }

    void OnMuteChanged(Property prop, object oldValue)
    {
      if (!_initialized)
        // Avoid changing the player manager's mute state in the initialization phase
        return;
      IPlayerManager playerManager = ServiceScope.Get<IPlayerManager>();
      playerManager.Muted = IsMuted;
    }

    void OnTimerElapsed(object sender, ElapsedEventArgs e)
    {
      CheckShowMouseControls();
      UpdateProperties();
    }

    void SubscribeToMessages()
    {
      _messageQueue = new AsynchronousMessageQueue(this, new string[]
        {
           PlayerManagerMessaging.CHANNEL,
           PlayerContextManagerMessaging.CHANNEL,
           SystemMessaging.CHANNEL,
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

    protected void StartTimer()
    {
      _timer.Enabled = true;
    }

    protected void StopTimer()
    {
      _timer.Enabled = false;
    }

    protected void OnMessageReceived(AsynchronousMessageQueue queue, QueueMessage message)
    {
      if (message.ChannelName == PlayerManagerMessaging.CHANNEL)
      {
        PlayerManagerMessaging.MessageType messageType = (PlayerManagerMessaging.MessageType) message.MessageType;
        if (messageType == PlayerManagerMessaging.MessageType.PlayerSlotsChanged && _stickToPlayerContext)
          SlotIndex = 1 - SlotIndex; // Will trigger an Update
        else
          UpdateProperties();
      }
      else if (message.ChannelName == PlayerContextManagerMessaging.CHANNEL)
        UpdateProperties();
      else if (message.ChannelName == SystemMessaging.CHANNEL)
      {
        SystemMessaging.MessageType messageType = (SystemMessaging.MessageType) message.MessageType;
        if (messageType == SystemMessaging.MessageType.SystemStateChanged)
        {
          ISystemStateService sss = ServiceScope.Get<ISystemStateService>();
          if (sss.CurrentState == SystemState.ShuttingDown)
            UnsubscribeFromMessages();
          StopTimer();
        }
      }
    }

    protected IPlayerContext GetPlayerContext()
    {
      IPlayerContextManager playerContextManager = ServiceScope.Get<IPlayerContextManager>();
      return playerContextManager.GetPlayerContext(SlotIndex);
    }

    protected void CheckShowMouseControls()
    {
      IInputManager inputManager = ServiceScope.Get<IInputManager>();
      ShowMouseControls = inputManager.IsMouseUsed && Screen != null && Screen.HasInputFocus;
    }

    protected void UpdateProperties()
    {
      if (_updating)
        return;
      _updating = true;
      try
      {
        IPlayerManager playerManager = ServiceScope.Get<IPlayerManager>();
        IPlayerContextManager playerContextManager = ServiceScope.Get<IPlayerContextManager>();
        IPlayer player = playerManager[SlotIndex];
        IPlayerContext playerContext = GetPlayerContext();
        IPlayerSlotController playerSlotController = playerManager.GetPlayerSlotController(SlotIndex);
        
        IsPlayerPresent = player != null;
        IVideoPlayer vp = player as IVideoPlayer;
        if (vp == null)
        {
          VideoWidth = 0f;
          VideoHeight = 0f;
        }
        else
        {
          if (FixedVideoWidth > 0f && FixedVideoHeight > 0f)
          {
            VideoWidth = FixedVideoWidth;
            VideoHeight = FixedVideoHeight;
          }
          else if (FixedVideoWidth > 0f)
          { // Calculate the video height from the width
            VideoWidth = FixedVideoWidth;
            VideoHeight = FixedVideoWidth*vp.VideoAspectRatio.Height/vp.VideoAspectRatio.Width;
          }
          else
          { // FixedVideoHeight > 0f
            VideoHeight = FixedVideoHeight;
            VideoWidth = FixedVideoHeight*vp.VideoAspectRatio.Width/vp.VideoAspectRatio.Height;
          }
        }
        if (player == null)
        {
          Title = playerContext == null ? NO_PLAYER_RESOURCE : playerContext.Name;
          MediaItemTitle = NO_MEDIA_ITEM_RESOURCE;
          IsAudio = false;
          IsPlaying = false;
          IsPaused = false;
          IsSeekingForward = false;
          IsSeekingBackward = false;
          SeekHint = string.Empty;
          IsCurrentPlayer = false;
          PercentPlayed = 0f;
          CurrentTime = string.Empty;
          Duration = string.Empty;
          PlayerStateText = string.Empty;
          CanPlay = false;
          CanPause = false;
          CanStop = false;
          CanSkipBack = false;
          CanSkipForward = false;
          CanSeekBackward = false;
          CanSeekForward = false;
          IsPlayerActive = false;
          IsPip = false;
        }
        else
        {
          IsPip = SlotIndex == PlayerManagerConsts.SECONDARY_SLOT && player is IVideoPlayer;
          string pcName = LocalizationHelper.CreateResourceString(playerContext.Name).Evaluate();
          Title = IsPip ? _headerPipResource.Evaluate(pcName) : _headerNormalResource.Evaluate(pcName);
          string mit = player.MediaItemTitle;
          if (mit == null)
          {
            MediaItem mediaItem = playerContext.Playlist.Current;
            if (mediaItem != null)
              mit = mediaItem.Aspects[MediaAspect.ASPECT_ID][MediaAspect.ATTR_TITLE] as string;
            if (mit == null)
              mit = UNKNOWN_MEDIA_ITEM_RESOURCE;
          }
          MediaItemTitle = mit;
          IMediaPlaybackControl mediaPlaybackControl = player as IMediaPlaybackControl;
          IsAudio = playerSlotController.IsAudioSlot;
          IsCurrentPlayer = playerContextManager.CurrentPlayerIndex == SlotIndex;
          TimeSpan currentTime = mediaPlaybackControl == null ? new TimeSpan() : mediaPlaybackControl.CurrentTime;
          TimeSpan duration = mediaPlaybackControl == null ? new TimeSpan() : mediaPlaybackControl.Duration;
          if (duration.TotalMilliseconds == 0)
          {
            PercentPlayed = 0;
            CurrentTime = string.Empty;
            Duration = string.Empty;
          }
          else
          {
            ILocalization localization = ServiceScope.Get<ILocalization>();
            CultureInfo culture = localization.CurrentCulture;
            PercentPlayed = (float) (100*currentTime.TotalMilliseconds/duration.TotalMilliseconds);
            CurrentTime = new DateTime().Add(currentTime).ToString("T", culture);
            Duration = new DateTime().Add(duration).ToString("T", culture);
          }
          string seekHint = string.Empty;
          bool playing = false;
          bool paused = false;
          bool seekingForward = false;
          bool seekingBackward = false;
          switch (player.State)
          {
            case PlayerState.Active:
              if (mediaPlaybackControl == null)
              {
                playing = true;
                PlayerStateText = PLAYER_ACTIVE_RESOURCE;
              }
              else
              {
                if (mediaPlaybackControl.IsPaused)
                {
                  paused = true;
                  PlayerStateText = PLAYER_PAUSED_RESOURCE;
                }
                else if (mediaPlaybackControl.IsPlayingAtNormalRate)
                {
                  playing = true;
                  PlayerStateText = PLAYER_PLAYING_RESOURCE;
                }
                else
                {
                  string playerStateTextResource;
                  double playbackRate = mediaPlaybackControl.PlaybackRate;
                  string format = "#";
                  if (playbackRate > 1.0)
                  {
                    seekingForward = true;
                    playerStateTextResource = PLAYER_SEEKING_FORWARD_RESOURCE;
                  }
                  else if (playbackRate < -1.0)
                  {
                    seekingBackward = true;
                    playerStateTextResource = PLAYER_SEEKING_BACKWARD_RESOURCE;
                  }
                  else if (playbackRate > 0.0)
                  {
                    seekingForward = true;
                    playerStateTextResource = PLAYER_SLOWMOTION_FORWARD_RESOURCE;
                    format = "0.#";
                  }
                  else // playbackRate < 0.0
                  {
                    seekingBackward = true;
                    playerStateTextResource = PLAYER_SLOWMOTION_BACKWARD_RESOURCE;
                    format = "0.#";
                  }
                  seekHint = _playbackRateHintResource.Evaluate(string.Format("{0:" + format + "}", Math.Abs(playbackRate)));
                  PlayerStateText = LocalizationHelper.CreateResourceString(playerStateTextResource).Evaluate(seekHint);
                }
              }
              break;
            case PlayerState.Stopped:
              PlayerStateText = PLAYER_STOPPED_RESOURCE;
              break;
            case PlayerState.Ended:
              PlayerStateText = PLAYER_ENDED_RESOURCE;
              break;
          }
          IsPlaying = playing;
          IsPaused = paused;
          IsSeekingForward = seekingForward;
          IsSeekingBackward = seekingBackward;
          SeekHint = seekHint;
          IsPlayerActive = player.State == PlayerState.Active;
          CanPlay = mediaPlaybackControl == null || mediaPlaybackControl.IsPaused || seekingForward || seekingBackward;
          CanPause = mediaPlaybackControl != null && !mediaPlaybackControl.IsPaused && !mediaPlaybackControl.IsSeeking;
          CanStop = true;
          CanSkipBack = playerContext.Playlist.HasPrevious;
          CanSkipForward = playerContext.Playlist.HasNext;
          CanSeekBackward = mediaPlaybackControl != null && mediaPlaybackControl.CanSeekBackwards;
          CanSeekForward = mediaPlaybackControl != null && mediaPlaybackControl.CanSeekForwards;
        }
        IsMuted = playerManager.Muted;
        CheckShowMouseControls();
        if (AutoVisibility)
        {
          bool isVisible = playerSlotController.IsActive;
          SimplePropertyDataDescriptor dd;
          if (SimplePropertyDataDescriptor.CreateSimplePropertyDataDescriptor(this, "IsVisible", out dd))
            SetValueInRenderThread(dd, isVisible);
          else
            IsVisible = isVisible;
        }
      }
      finally
      {
        _initialized = true;
        _updating = false;
      }
    }

    #endregion

    public override void Allocate()
    {
      base.Allocate();
      StartTimer();
    }

    public override void Deallocate()
    {
      base.Deallocate();
      StopTimer();
    }

    #region Public menbers, to be accessed via the GUI

    #region Configuration properties, to be set from the outside

    public Property SlotIndexProperty
    {
      get { return _slotIndexProperty; }
    }

    /// <summary>
    /// Index of the underlaying player slot. Will be updated automatically if <see cref="StickToPlayerContext"/> is
    /// set to <c>true</c> and the player manager changes its player slots.
    /// </summary>
    public int SlotIndex
    {
      get { return (int) _slotIndexProperty.GetValue(); }
      set { _slotIndexProperty.SetValue(value); }
    }

    public Property AutoVisibilityProperty
    {
      get { return _autoVisibilityProperty; }
    }

    /// <summary>
    /// If set to <c>true</c>, this <see cref="PlayerControl"/> will automatically show up when the underlaying
    /// player slot is active and will automatically hide when it is deactivated.
    /// </summary>
    public bool AutoVisibility
    {
      get { return (bool) _autoVisibilityProperty.GetValue(); }
      set { _autoVisibilityProperty.SetValue(value); }
    }

    /// <summary>
    /// If set to <c>true</c>, this <see cref="PlayerControl"/> will automatically change its <see cref="SlotIndex"/> when
    /// the player slots get exchanged by the <see cref="IPlayerManager"/>.
    /// </summary>
    public bool StickToPlayerContext
    {
      get { return _stickToPlayerContext; }
      set { _stickToPlayerContext = value; }
    }

    /// <summary>
    /// Gets or sets a fixed width for the <see cref="VideoWidth"/> property. If <see cref="FixedVideoHeight"/> is set to
    /// <c>0</c>, the <see cref="VideoHeight"/> will be calculated automatically using the current player's aspect ratio.
    /// If both <see cref="FixedVideoWidth"/> and <see cref="FixedVideoHeight"/> are set, the player's aspect ratio will be
    /// ignored.
    /// </summary>
    public float FixedVideoWidth
    {
      get { return _fixedVideoWidth; }
      set { _fixedVideoWidth = value; }
    }

    /// <summary>
    /// Gets or sets a fixed height for the <see cref="VideoHeight"/> property. If <see cref="FixedVideoWidth"/> is set to
    /// <c>0</c>, the <see cref="VideoWidth"/> will be calculated automatically using the current player's aspect ratio.
    /// If both <see cref="FixedVideoWidth"/> and <see cref="FixedVideoHeight"/> are set, the player's aspect ratio will be
    /// ignored.
    /// </summary>
    public float FixedVideoHeight
    {
      get { return _fixedVideoHeight; }
      set { _fixedVideoHeight = value; }
    }

    #endregion

    #region Derived properties to update the GUI

    public Property IsPlayerPresentProperty
    {
      get { return _isPlayerPresentProperty; }
    }

    /// <summary>
    /// Returns the information if the underlaying player slot currently has a player.
    /// </summary>
    public bool IsPlayerPresent
    {
      get { return (bool) _isPlayerPresentProperty.GetValue(); }
      internal set { _isPlayerPresentProperty.SetValue(value); }
    }

    public Property TitleProperty
    {
      get { return _titleProperty; }
    }

    /// <summary>
    /// Returns the title of this player control, i.e. the name of the player, like "Video (PiP)".
    /// </summary>
    public string Title
    {
      get { return (string) _titleProperty.GetValue(); }
      internal set { _titleProperty.SetValue(value); }
    }

    public Property MediaItemTitleProperty
    {
      get { return _mediaItemTitleProperty; }
    }

    /// <summary>
    /// Returns the title of the current media item.
    /// </summary>
    public string MediaItemTitle
    {
      get { return (string) _mediaItemTitleProperty.GetValue(); }
      internal set { _mediaItemTitleProperty.SetValue(value); }
    }

    public Property IsAudioProperty
    {
      get { return _isAudioProperty; }
    }

    /// <summary>
    /// Returns the information if the slot with the <see cref="SlotIndex"/> is the audio slot.
    /// </summary>
    public bool IsAudio
    {
      get { return (bool) _isAudioProperty.GetValue(); }
      internal set { _isAudioProperty.SetValue(value); }
    }

    public Property IsMutedProperty
    {
      get { return _isMutedProperty; }
    }

    /// <summary>
    /// Returns the information if the underlaying player is the audio player but is muted.
    /// </summary>
    public bool IsMuted
    {
      get { return (bool) _isMutedProperty.GetValue(); }
      internal set { _isMutedProperty.SetValue(value); }
    }

    public Property IsCurrentPlayerProperty
    {
      get { return _isCurrentPlayerProperty; }
    }

    /// <summary>
    /// Returns the information if the underlaying player is currently playing.
    /// </summary>
    public bool IsPlaying
    {
      get { return (bool) _isPlayingProperty.GetValue(); }
      internal set { _isPlayingProperty.SetValue(value); }
    }

    public Property IsPlayingProperty
    {
      get { return _isPlayingProperty; }
    }

    /// <summary>
    /// Returns the information if the underlaying player is currently paused.
    /// </summary>
    public bool IsPaused
    {
      get { return (bool) _isPausedProperty.GetValue(); }
      internal set { _isPausedProperty.SetValue(value); }
    }

    public Property IsPausedProperty
    {
      get { return _isPausedProperty; }
    }

    /// <summary>
    /// Returns the information if the underlaying player is currently seeking forward.
    /// </summary>
    public bool IsSeekingForward
    {
      get { return (bool) _isSeekingForwardProperty.GetValue(); }
      internal set { _isSeekingForwardProperty.SetValue(value); }
    }

    public Property IsSeekingForwardProperty
    {
      get { return _isSeekingForwardProperty; }
    }

    /// <summary>
    /// Returns the information if the underlaying player is currently seeking backward.
    /// </summary>
    public bool IsSeekingBackward
    {
      get { return (bool) _isSeekingBackwardProperty.GetValue(); }
      internal set { _isSeekingBackwardProperty.SetValue(value); }
    }

    public Property IsSeekingBackwardProperty
    {
      get { return _isSeekingBackwardProperty; }
    }

    /// <summary>
    /// Returns a string which contains the current seeking rate (for example: "2x").
    /// </summary>
    public string SeekHint
    {
      get { return (string) _seekHintProperty.GetValue(); }
      internal set { _seekHintProperty.SetValue(value); }
    }

    public Property SeekHintProperty
    {
      get { return _seekHintProperty; }
    }

    /// <summary>
    /// Returns the information if the underlaying player is currently focused for remote or keyboard input.
    /// </summary>
    public bool IsCurrentPlayer
    {
      get { return (bool) _isCurrentPlayerProperty.GetValue(); }
      internal set { _isCurrentPlayerProperty.SetValue(value); }
    }

    public Property PercentPlayedProperty
    {
      get { return _percentPlayedProperty; }
    }

    /// <summary>
    /// Returns a value (range 0 to 100) which denotes the current fraction of played content.
    /// </summary>
    public float PercentPlayed
    {
      get { return (float) _percentPlayedProperty.GetValue(); }
      internal set { _percentPlayedProperty.SetValue(value); }
    }

    public Property CurrentTimeProperty
    {
      get { return _currentTimeProperty; }
    }

    /// <summary>
    /// Returns the current play time (or empty).
    /// </summary>
    public string CurrentTime
    {
      get { return (string) _currentTimeProperty.GetValue(); }
      internal set { _currentTimeProperty.SetValue(value); }
    }

    public Property DurationProperty
    {
      get { return _durationProperty; }
    }

    /// <summary>
    /// Returns the duration of the current media item (or empty).
    /// </summary>
    public string Duration
    {
      get { return (string) _durationProperty.GetValue(); }
      internal set { _durationProperty.SetValue(value); }
    }

    public Property PlayerStateTextProperty
    {
      get { return _playerStateTextProperty; }
    }

    /// <summary>
    /// Returns a string which denotes the current playing state, for example "Playing" or "Seeking forward (2x)".
    /// </summary>
    public string PlayerStateText
    {
      get { return (string) _playerStateTextProperty.GetValue(); }
      internal set { _playerStateTextProperty.SetValue(value); }
    }

    public Property ShowMouseControlsProperty
    {
      get { return _showMouseControlsProperty; }
    }

    /// <summary>
    /// Returns the information if the mouse is being used, i.e. if mouse controls should be shown, if appropriate.
    /// </summary>
    public bool ShowMouseControls
    {
      get { return (bool) _showMouseControlsProperty.GetValue(); }
      internal set { _showMouseControlsProperty.SetValue(value); }
    }

    public Property CanPlayProperty
    {
      get { return _canPlayProperty; }
    }

    /// <summary>
    /// Returns the information if the underlaying player is able to play in the current state, i.e. if the "Play" control
    /// should be shown, if appropriate.
    /// </summary>
    public bool CanPlay
    {
      get { return (bool) _canPlayProperty.GetValue(); }
      internal set { _canPlayProperty.SetValue(value); }
    }

    public Property CanPauseProperty
    {
      get { return _canPauseProperty; }
    }

    /// <summary>
    /// Returns the information if the underlaying player is able to pause in the current state, i.e. if the "Pause" control
    /// should be shown, if appropriate.
    /// </summary>
    public bool CanPause
    {
      get { return (bool) _canPauseProperty.GetValue(); }
      internal set { _canPauseProperty.SetValue(value); }
    }

    public Property CanStopProperty
    {
      get { return _canStopProperty; }
    }

    /// <summary>
    /// Returns the information if the underlaying player is able to stop in the current state, i.e. if the "Stop" control
    /// should be shown, if appropriate.
    /// </summary>
    public bool CanStop
    {
      get { return (bool) _canStopProperty.GetValue(); }
      internal set { _canStopProperty.SetValue(value); }
    }

    public Property CanSkipForwardProperty
    {
      get { return _canSkipForwardProperty; }
    }

    /// <summary>
    /// Returns the information if the underlaying player is able to skip forward in the current state, i.e. if the "SkipForward" control
    /// should be shown, if appropriate.
    /// </summary>
    public bool CanSkipForward
    {
      get { return (bool) _canSkipForwardProperty.GetValue(); }
      internal set { _canSkipForwardProperty.SetValue(value); }
    }

    public Property CanSkipBackProperty
    {
      get { return _canSkipBackProperty; }
    }

    /// <summary>
    /// Returns the information if the underlaying player is able to skip backward in the current state, i.e. if the "SkipBackward" control
    /// should be shown, if appropriate.
    /// </summary>
    public bool CanSkipBack
    {
      get { return (bool) _canSkipBackProperty.GetValue(); }
      internal set { _canSkipBackProperty.SetValue(value); }
    }

    public Property CanSeekForwardProperty
    {
      get { return _canSeekForwardProperty; }
    }

    /// <summary>
    /// Returns the information if the underlaying player is able to seek forward in the current state, i.e. if the "SeekForward" control
    /// should be shown, if appropriate.
    /// </summary>
    public bool CanSeekForward
    {
      get { return (bool) _canSeekForwardProperty.GetValue(); }
      internal set { _canSeekForwardProperty.SetValue(value); }
    }

    public Property CanSeekBackwardProperty
    {
      get { return _canSeekBackwardProperty; }
    }

    /// <summary>
    /// Returns the information if the underlaying player is able to seek backward in the current state, i.e. if the "SeekBackward" control
    /// should be shown, if appropriate.
    /// </summary>
    public bool CanSeekBackward
    {
      get { return (bool) _canSeekBackwardProperty.GetValue(); }
      internal set { _canSeekBackwardProperty.SetValue(value); }
    }

    public Property IsPlayerActiveProperty
    {
      get { return _isPlayerActiveProperty; }
    }

    /// <summary>
    /// Returns the activity state of the underlaying player. When <see cref="IsPlayerActive"/> is <c>true</c>,
    /// the underlaying player is either playing or paused or seeking. Else, it is stopped or ended.
    /// </summary>
    public bool IsPlayerActive
    {
      get { return (bool) _isPlayerActiveProperty.GetValue(); }
      internal set { _isPlayerActiveProperty.SetValue(value); }
    }

    public Property IsPipProperty
    {
      get { return _isPipProperty; }
    }

    /// <summary>
    /// Returns the information whether a picture-in-picture player is playing.
    /// </summary>
    public bool IsPip
    {
      get { return (bool) _isPipProperty.GetValue(); }
      internal set { _isPipProperty.SetValue(value); }
    }

    public Property VideoWidthProperty
    {
      get { return _videoWidthProperty; }
    }

    /// <summary>
    /// Returns the fixed or calculated video width. Can be used for picture-in-picture players, for example.
    /// </summary>
    public float VideoWidth
    {
      get { return (float) _videoWidthProperty.GetValue(); }
      internal set { _videoWidthProperty.SetValue(value); }
    }

    public Property VideoHeightProperty
    {
      get { return _videoHeightProperty; }
    }

    /// <summary>
    /// Returns the fixed or calculated video height. Can be used for picture-in-picture players, for example.
    /// </summary>
    public float VideoHeight
    {
      get { return (float) _videoHeightProperty.GetValue(); }
      internal set { _videoHeightProperty.SetValue(value); }
    }

    #endregion

    /// <summary>
    /// Called from the skin if the user presses the audio button. This will move the audio to the current player slot,
    /// mute the player or show up the audio menu, depending on the available audio streams.
    /// </summary>
    public void AudioButtonPressed()
    {
      IPlayerManager playerManager = ServiceScope.Get<IPlayerManager>();
      IPlayerContext playerContext = GetPlayerContext();
      IList<AudioStreamDescriptor> audioStreamDescriptors =
          new List<AudioStreamDescriptor>(playerContext.GetAudioStreamDescriptors());
      if (audioStreamDescriptors.Count <= 1)
        if (IsAudio)
          playerManager.Muted ^= true;
        else
        {
          playerManager.AudioSlotIndex = SlotIndex;
          playerManager.Muted = false;
        }
      else
      {
        IWorkflowManager workflowManager = ServiceScope.Get<IWorkflowManager>();
        workflowManager.NavigatePush(PLAYER_SLOT_AUDIO_MENU_DIALOG_STATE_ID, new Dictionary<string, object>
          {
              {KEY_PLAYER_SLOT, SlotIndex},
              {KEY_SHOW_MUTE, !IsAudio}
          });
      }
    }

    public void Play()
    {
      IPlayerContext pc = GetPlayerContext();
      if (pc == null)
        return;
      if (pc.PlayerState == PlaybackState.Paused || pc.PlayerState == PlaybackState.Seeking)
        pc.Play();
      else
        pc.Restart();
    }

    public void Pause()
    {
      IPlayerContext pc = GetPlayerContext();
      if (pc == null)
        return;
      pc.Pause();
    }

    public void TogglePause()
    {
      IPlayerContext pc = GetPlayerContext();
      if (pc == null)
        return;
      switch (pc.PlayerState) {
        case PlaybackState.Playing:
          pc.Pause();
          break;
        case PlaybackState.Paused:
          pc.Play();
          break;
        default:
          pc.Restart();
          break;
      }
    }

    public void Stop()
    {
      IPlayerContext pc = GetPlayerContext();
      if (pc == null)
        return;
      pc.Stop();
    }

    public void SeekBackward()
    {
      IPlayerContext pc = GetPlayerContext();
      if (pc == null)
        return;
      pc.SeekBackward();
    }

    public void SeekForward()
    {
      IPlayerContext pc = GetPlayerContext();
      if (pc == null)
        return;
      pc.SeekForward();
    }

    public void Previous()
    {
      IPlayerContext pc = GetPlayerContext();
      if (pc == null)
        return;
      pc.PreviousItem();
    }

    public void Next()
    {
      IPlayerContext pc = GetPlayerContext();
      if (pc == null)
        return;
      pc.NextItem();
    }

    public void ToggleMute()
    {
      IPlayerManager playerManager = ServiceScope.Get<IPlayerManager>();
      playerManager.Muted ^= true;
      UpdateProperties();
    }

    public void MakeCurrent()
    {
      IPlayerContextManager playerContextManager = ServiceScope.Get<IPlayerContextManager>();
      playerContextManager.CurrentPlayerIndex = SlotIndex;
    }

    public void SwitchPip()
    {
      IPlayerManager playerManager = ServiceScope.Get<IPlayerManager>();
      playerManager.SwitchSlots();
      // The workflow state will be changed to the new primary player's FSC- or CP-state automatically by the PCM,
      // if necessary
    }

    #endregion
  }
}
