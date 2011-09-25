#region Copyright (C) 2007-2011 Team MediaPortal

/*
    Copyright (C) 2007-2011 Team MediaPortal
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
using System.IO;
using System.Threading;
using MediaPortal.Common;
using MediaPortal.Common.MediaManagement.ResourceAccess;
using MediaPortal.Common.Settings;
using MediaPortal.UI.Players.Picture.Settings;
using MediaPortal.UI.Presentation.Players;
using MediaPortal.Utilities;

namespace MediaPortal.UI.Players.Picture
{
  public class PicturePlayer : IDisposable, IPicturePlayer, IPlayerEvents, IReusablePlayer, IMediaPlaybackControl
  {
    #region Consts

    public const string STR_PLAYER_ID = "9B1B6861-1757-40b2-9227-98A36D6CC9D7";
    public static readonly Guid PLAYER_ID = new Guid(STR_PLAYER_ID);

    protected TimeSpan TS_INFINITE = TimeSpan.FromMilliseconds(-1);

    #endregion

    #region Protected fields

    protected readonly object _syncObj = new object();

    protected PlayerState _state;
    protected bool _isPaused = false;
    protected string _mediaItemTitle = string.Empty;

    protected IResourceLocator _currentLocator;
    protected TimeSpan _slideShowImageDuration;
    protected Timer _slideShowTimer = null;
    protected bool _slideShowEnabled = true;
    protected DateTime _playbackStartTime = DateTime.MinValue;

    // Data and events for the communication with the player manager.
    protected PlayerEventDlgt _started = null;
    protected PlayerEventDlgt _stateReady = null;
    protected PlayerEventDlgt _stopped = null;
    protected PlayerEventDlgt _ended = null;
    protected PlayerEventDlgt _playbackStateChanged = null;
    protected PlayerEventDlgt _playbackError = null;

    #endregion

    public PicturePlayer()
    {
      _state = PlayerState.Stopped;
    }

    public void Dispose()
    {
      if (_slideShowTimer != null)
      {
        _slideShowTimer.Dispose();
        _slideShowTimer = null;
      }
    }

    #region Protected members

    protected void PlaybackEnded()
    {
      lock (_syncObj)
      {
        if (_state != PlayerState.Active)
          return;
        _state = PlayerState.Ended;
      }
      FireEnded();
    }

    internal void FireStarted()
    {
      // The delegate is final so we can invoke it without the need of a local copy
      if (_started != null)
        _started(this);
    }

    internal void FireStateReady()
    {
      // The delegate is final so we can invoke it without the need of a local copy
      if (_stateReady != null)
        _stateReady(this);
    }

    internal void FireStopped()
    {
      // The delegate is final so we can invoke it without the need of a local copy
      if (_stopped != null)
        _stopped(this);
    }

    internal void FireEnded()
    {
      // The delegate is final so we can invoke it without the need of a local copy
      if (_ended != null)
        _ended(this);
    }

    internal void FirePlaybackStateChanged()
    {
      // The delegate is final so we can invoke it without the need of a local copy
      if (_playbackStateChanged != null)
        _playbackStateChanged(this);
    }

    protected void FireNextItemRequest()
    {
      RequestNextItemDlgt dlgt = NextItemRequest;
      if (dlgt != null)
        dlgt(this);
    }

    protected void ReloadSettings()
    {
      PicturePlayerSettings settings = ServiceRegistration.Get<ISettingsManager>().Load<PicturePlayerSettings>() ?? new PicturePlayerSettings();
      double durationSec = settings.SlideShowImageDuration;
      _slideShowImageDuration = durationSec == 0 ? TS_INFINITE : TimeSpan.FromSeconds(durationSec);
    }

    protected void DisposeTimer()
    {
      lock (_syncObj)
        if (_slideShowTimer != null)
        {
          _slideShowTimer.Dispose();
          _slideShowTimer = null;
        }
    }

    protected void CheckTimer()
    {
      lock (_syncObj)
      {
        if (_state == PlayerState.Active && !IsPaused)
        {
          if (_slideShowTimer == null && _slideShowEnabled)
            _slideShowTimer = new Timer(OnSlideShowNewPicture, null, _slideShowImageDuration, TS_INFINITE);
          if (_slideShowTimer != null && !_slideShowEnabled)
            DisposeTimer();
        }
        else
        {
          if (_slideShowTimer != null)
            DisposeTimer();
        }
      }
    }

    void OnSlideShowNewPicture(object state)
    {
      FireEnded();
    }

    #endregion

    #region Public methods

    /// <summary>
    /// Sets the new picture to be played.
    /// </summary>
    /// <param name="locator">Resource locator of the picture item.</param>
    public void SetMediaItemLocator(IResourceLocator locator)
    {
      ReloadSettings();
      lock (_syncObj)
      {
        _state = PlayerState.Active;
        _currentLocator = locator;
        if (_slideShowTimer != null)
          _slideShowTimer.Change(_slideShowImageDuration, TS_INFINITE);
        else
          CheckTimer();
        _playbackStartTime = DateTime.Now;
      }
    }

    public static bool CanPlay(IResourceLocator locator, string mimeType)
    {
      // First check the Mime Type
      if (!string.IsNullOrEmpty(mimeType) && !mimeType.StartsWith("image"))
        return false;

      IResourceAccessor accessor = locator.CreateAccessor();
      string ext = StringUtils.TrimToEmpty(Path.GetExtension(accessor.ResourcePathName)).ToLowerInvariant();

      PicturePlayerSettings settings = ServiceRegistration.Get<ISettingsManager>().Load<PicturePlayerSettings>();
      return settings.SupportedExtensions.IndexOf(ext) > -1;
    }

    #endregion

    #region IPlayer implementation

    public Guid PlayerId
    {
      get { return PLAYER_ID; }
    }

    public string Name
    {
      get { return "Picture"; }
    }

    public PlayerState State
    {
      get
      {
        lock (_syncObj)
          return _state;
      }
    }

    public string MediaItemTitle
    {
      get
      {
        lock (_syncObj)
          return _mediaItemTitle;
      }
    }

    public void SetMediaItemTitleHint(string title)
    {
      lock (_syncObj)
        _mediaItemTitle = title;
    }

    public void Stop()
    {
      lock (_syncObj)
      {
        if (_state != PlayerState.Active)
          return;
        _state = PlayerState.Stopped;
        _currentLocator = null;
      }
      FireStopped();
    }

    #endregion

    #region IPlayerEvents implementation

    // Not implemented thread-safe by design
    public void InitializePlayerEvents(PlayerEventDlgt started, PlayerEventDlgt stateReady, PlayerEventDlgt stopped,
        PlayerEventDlgt ended, PlayerEventDlgt playbackStateChanged, PlayerEventDlgt playbackError)
    {
      _started = started;
      _stateReady = stateReady;
      _stopped = stopped;
      _ended = ended;
      _playbackStateChanged = playbackStateChanged;
      _playbackError = playbackError;
    }

    // Not implemented thread-safe by design
    public void ResetPlayerEvents()
    {
      _started = null;
      _stateReady = null;
      _stopped = null;
      _ended = null;
      _playbackStateChanged = null;
      _playbackError = null;
    }

    #endregion

    #region IReusablePlayer implementation

    public event RequestNextItemDlgt NextItemRequest;

    public bool NextItem(IResourceLocator locator, string mimeType, StartTime startTime)
    {
      if (!CanPlay(locator, mimeType))
        return false;
      SetMediaItemLocator(locator);
      return true;
    }

    #endregion

    #region IPicturePlayer implementation

    public bool SlideShowEnabled
    {
      get { return _slideShowEnabled; }
      set
      {
        _slideShowEnabled = value;
        CheckTimer();
      }
    }

    public IResourceLocator CurrentPictureResourceLocator
    {
      get
      {
        lock (_syncObj)
          return _currentLocator;
      }
    }

    #endregion

    #region IMediaPlaybackControl implementation

    public TimeSpan CurrentTime
    {
      get
      {
        lock (_syncObj)
        {
          if (_isPaused)
            return TimeSpan.Zero;
          return DateTime.Now - _playbackStartTime;
        }
      }
      set
      {
        lock (_syncObj)
          _playbackStartTime = DateTime.Now - value;
      }
    }

    public TimeSpan Duration
    {
      get { return _slideShowImageDuration; }
    }

    public double PlaybackRate
    {
      get { return 1.0; }
    }

    public bool SetPlaybackRate(double value)
    {
      return false;
    }

    public bool IsPlayingAtNormalRate
    {
      get { return true; }
    }

    public bool IsSeeking
    {
      get { return false; }
    }

    public bool IsPaused
    {
      get
      {
        lock (_syncObj)
          return _isPaused;
      }
    }

    public bool CanSeekForwards
    {
      get { return false; }
    }

    public bool CanSeekBackwards
    {
      get { return false; }
    }

    public void Pause()
    {
      lock (_syncObj)
      {
        _isPaused = true;
        DisposeTimer();
      }
    }

    public void Resume()
    {
      lock (_syncObj)
      {
        _isPaused = false;
        CurrentTime = TimeSpan.Zero;
        CheckTimer();
      }
    }

    public void Restart()
    {
      lock (_syncObj)
      {
        DisposeTimer();
        CheckTimer();
      }
    }

    #endregion
  }
}
