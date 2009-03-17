using System;
using MediaPortal.Core;
using MediaPortal.Core.MediaManagement;
using MediaPortal.Core.MediaManagement.DefaultItemAspects;
using MediaPortal.Core.Settings;
using MediaPortal.Presentation.Players;
using MediaPortal.Services.Players.Settings;
using MediaPortal.Utilities.Exceptions;

namespace MediaPortal.Services.Players
{
    internal class PlayerSlotController : IPlayerSlotController
    {
      protected PlayerManager _playerManager;
      protected int _slotIndex;
      protected bool _isAudioSlot = false;
      protected PlayerBuilderRegistration _builderRegistration = null;
      protected IPlayer _player = null;
      protected Playlist _playlist = null;
      protected PlayerSlotState _playbackState = PlayerSlotState.Stopped;

      internal PlayerSlotController(PlayerManager parent, int slotIndex)
      {
        _playerManager = parent;
        _slotIndex = slotIndex;
        _playlist = new Playlist();
      }

      protected static bool GetItemData(MediaItem item, out IMediaItemLocator locator, out string mimeType)
      {
        IMediaManager mediaManager = ServiceScope.Get<IMediaManager>();
        locator = mediaManager.GetMediaItemLocator(item);
        MediaItemAspect mediaAspect = item[MediaAspect.ASPECT_ID];
        mimeType = (string) mediaAspect[MediaAspect.ATTR_MIME_TYPE];
        return locator != null;
      }

      protected bool DoPlay(MediaItem item)
      {
        IMediaItemLocator locator;
        string mimeType;
        if (!GetItemData(item, out locator, out mimeType))
          return false;
        return Play(locator, mimeType);
      }

      protected bool CreatePlayer(IMediaItemLocator locator, string mimeType)
      {
        ReleasePlayer();
        _playerManager.BuildPlayer(locator, mimeType, this);
        if (_player != null)
        {
          RegisterPlayerEvents();
          return true;
        }
        return false;
      }

      protected void ReleasePlayer()
      {
        if (_player != null)
        {
          ResetPlayerEvents();
          _playbackState = PlayerSlotState.Stopped;
          if (_player.State != Presentation.Players.PlaybackState.Stopped)
            _player.Stop();
          if (_player is IDisposable)
            ((IDisposable) _player).Dispose();
          _player = null;
        }
        _playerManager.RevokePlayer(this);
      }

      internal PlayerBuilderRegistration BuilderRegistration
      {
        get { return _builderRegistration; }
      }

      internal void AssignPlayerAndBuilderRegistration(IPlayer player, PlayerBuilderRegistration builderRegistration)
      {
        _player = player;
        _builderRegistration = builderRegistration;
        _builderRegistration.UsingSlots.Add(this);
      }

      internal void ResetPlayerAndBuilderRegistration()
      {
        _player = null;
        if (_builderRegistration != null)
        {
          _builderRegistration.UsingSlots.Remove(this);
          _builderRegistration = null;
        }
      }

      protected void CheckActive()
      {
        if (_playbackState == PlayerSlotState.Inactive)
          throw new InvalidStateException("PlayerSlotController: PSC is not active");
      }

      protected void RegisterPlayerEvents()
      {
        IPlayerEvents pe = (IPlayerEvents) _player;
        pe.InitializePlayerEvents(OnPlayerStarted, OnPlayerStopped, OnPlayerEnded,
            OnPlayerPaused, OnPlayerResumed, OnPlaybackError);
      }

      protected void ResetPlayerEvents()
      {
        IPlayerEvents pe = _player as IPlayerEvents;
        if (pe != null)
          pe.ResetPlayerEvents();
      }

      protected void OnPlayerStarted(IPlayer player)
      {
        PlayerManagerMessaging.SendPlayerMessage(PlayerManagerMessaging.MessageType.PlayerStarted, _slotIndex);
      }

      protected void OnPlayerStopped(IPlayer player)
      {
        PlayerManagerMessaging.SendPlayerMessage(PlayerManagerMessaging.MessageType.PlayerStopped, _slotIndex);
        // No automatic closing of slots - has to be done explicitly by the user as a result to the previous event
      }

      protected void OnPlayerEnded(IPlayer player)
      {
        PlayerManagerMessaging.SendPlayerMessage(PlayerManagerMessaging.MessageType.PlayerEnded, _slotIndex);
        NextItem();
      }

      protected void OnPlayerPaused(IPlayer player)
      {
        PlayerManagerMessaging.SendPlayerMessage(PlayerManagerMessaging.MessageType.PlayerPaused, _slotIndex);
      }

      protected void OnPlayerResumed(IPlayer player)
      {
        PlayerManagerMessaging.SendPlayerMessage(PlayerManagerMessaging.MessageType.PlayerResumed, _slotIndex);
      }

      protected void OnPlaybackError(IPlayer player)
      {
        // TODO: Log error
        NextItem();
      }

      #region IPlayerSlot implementation

      public IPlaylist PlayList
      {
        get
        {
          CheckActive();
          return _playlist;
        }
      }

      public bool IsAudioSlot
      {
        get
        {
          CheckActive();
          return _isAudioSlot;
        }
        internal set { _isAudioSlot = value; }
      }

      public bool IsActive
      {
        get { return _playbackState != PlayerSlotState.Inactive; }
        internal set
        {
          if (value)
            _playbackState = PlayerSlotState.Stopped;
          else
          {
            Reset();
            _isAudioSlot = false;
            _playbackState = PlayerSlotState.Inactive;
          }
        }
      }

      public bool CanPlay
      {
        get
        {
          CheckActive();
          return !_playlist.AllPlayed;
        }
      }

      public PlayerSlotState PlaybackState
      {
        get { return _playbackState; }
      }

      public IPlayer CurrentPlayer
      {
        get
        {
          CheckActive();
          return _player;
        }
      }

      public void Reset()
      {
        CheckActive();
        ReleasePlayer(); // Resets _player and _builderRegistration
        _playlist.Clear();
      }

      public void Stop()
      {
        CheckActive();
        _playbackState = PlayerSlotState.Stopped;
        ReleasePlayer();
        _playlist.ResetStatus();
      }

      public void Pause()
      {
        CheckActive();
        if (_player == null)
          return;
        _playbackState = PlayerSlotState.Paused;
        _player.Pause();
      }

      public void Play()
      {
        CheckActive();
        if (_player == null)
        {
          NextItem();
          return;
        }
        if (_playbackState != PlayerSlotState.Paused)
          return;
        _playbackState = PlayerSlotState.Playing;
        _player.Resume();
      }

      public bool Play(IMediaItemLocator locator, string mimeType)
      {
        CheckActive();
        PlayerSettings settings = ServiceScope.Get<ISettingsManager>().Load<PlayerSettings>();
        if (settings.CrossFading)
        {
          ICrossfadingEnabledPlayer cep = _player as ICrossfadingEnabledPlayer;
          if (cep != null)
            return cep.Crossfade(locator, mimeType, CrossFadeMode.FadeDuration, new TimeSpan((long) (10000000*settings.CrossFadeDuration)));
        }
        IReusablePlayer rp = _player as IReusablePlayer;
        if (rp != null)
          return rp.NextItem(locator, mimeType);
        if (CreatePlayer(locator, mimeType))
        {
          _player.Resume();
          return true;
        }
        return false;
      }

      public void Restart()
      {
        CheckActive();
        if (_player == null)
          return;
        _playbackState = PlayerSlotState.Playing;
        _player.Restart();
      }

      public bool PreviousItem()
      {
        CheckActive();
        return DoPlay(_playlist.Previous());
      }

      public bool NextItem()
      {
        CheckActive();
        return DoPlay(_playlist.Next());
      }

      #endregion
    }
}