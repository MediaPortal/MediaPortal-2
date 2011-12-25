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
using System.Drawing;
using System.IO;
using System.Threading;
using MediaPortal.Common;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Common.ResourceAccess;
using MediaPortal.Common.Settings;
using MediaPortal.UI.Players.Picture.Animation;
using MediaPortal.UI.Players.Picture.Settings;
using MediaPortal.UI.Presentation.Players;
using MediaPortal.UI.SkinEngine.Players;
using MediaPortal.UI.SkinEngine.SkinManagement;
using MediaPortal.Utilities;
using SlimDX.Direct3D9;
using RightAngledRotation = MediaPortal.UI.Presentation.Players.RightAngledRotation;

namespace MediaPortal.UI.Players.Picture
{
  public class PicturePlayer : IDisposable, ISlimDXPicturePlayer, IPlayerEvents, IReusablePlayer, IMediaPlaybackControl
  {
    #region Consts

    public const string STR_PLAYER_ID = "9B1B6861-1757-40b2-9227-98A36D6CC9D7";
    public static readonly Guid PLAYER_ID = new Guid(STR_PLAYER_ID);

    protected static readonly TimeSpan TS_INFINITE = TimeSpan.FromMilliseconds(-1);

    protected static readonly IPictureAnimator STILL_IMAGE_ANIMATION = new StillImageAnimator();

    #endregion

    #region Protected fields

    protected readonly object _syncObj = new object();

    protected PlayerState _state;
    protected DateTime? _pauseTime = null;
    protected string _mediaItemTitle = string.Empty;
    protected RightAngledRotation _rotation = RightAngledRotation.Zero;
    protected bool _flipX = false;
    protected bool _flipY = false;

    protected IResourceLocator _currentLocator = null;
    protected Texture _texture = null;
    protected SizeF _textureMaxUV = new SizeF(1, 1);
    protected TimeSpan _slideShowImageDuration = TimeSpan.FromSeconds(10);
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

    // Picture animation effect
    protected IPictureAnimator _animator;

    #endregion

    public PicturePlayer()
    {
      _state = PlayerState.Stopped;
    }

    public void Dispose()
    {
      DisposeTimer();
      DisposeTexture();
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

      _animator = settings.UseKenBurns ? new KenBurnsAnimator() : STILL_IMAGE_ANIMATION;
    }

    protected void DisposeTimer()
    {
      lock (_syncObj)
        if (_slideShowTimer != null)
        {
          WaitHandle notifyObject = new ManualResetEvent(false);
          _slideShowTimer.Dispose(notifyObject);
          notifyObject.WaitOne();
          notifyObject.Close();
          _slideShowTimer = null;
        }
    }

    protected void DisposeTexture()
    {
      if (_texture == null)
        return;
      _texture.Dispose();
      _texture = null;
      _textureMaxUV = SizeF.Empty;
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
    /// Sets the data of the new picture to be played.
    /// </summary>
    /// <param name="locator">Resource locator of the picture item.</param>
    /// <param name="mediaItemTitle">Title of the picture item.</param>
    /// <param name="rotation">Rotation of the picture.</param>
    /// <param name="flipX">Flipping in horizontal direction.</param>
    /// <param name="flipY">Flipping in vertical direction.</param>
    public void SetMediaItemData(IResourceLocator locator, string mediaItemTitle, RightAngledRotation rotation, bool flipX, bool flipY)
    {
      if (locator == null)
        lock (_syncObj)
        {
          DisposeTexture();
          _currentLocator = null;
          return;
        }

      Texture texture;
      ImageInformation imageInformation;
      using (IResourceAccessor ra = locator.CreateAccessor())
      using (Stream stream = ra.OpenRead())
        texture = Texture.FromStream(SkinContext.Device, stream, (int) stream.Length, 0, 0, 1, Usage.None,
            Format.A8R8G8B8, Pool.Default, Filter.None, Filter.None, 0, out imageInformation);
      lock (_syncObj)
      {
        ReloadSettings();
        _state = PlayerState.Active;

        DisposeTexture();
        _currentLocator = locator;
        _mediaItemTitle = mediaItemTitle;
        _texture = texture;
        _rotation = rotation;
        _flipX = flipX;
        _flipY = flipY;
        SurfaceDescription desc = _texture.GetLevelDescription(0);
        _textureMaxUV = new SizeF(imageInformation.Width / (float) desc.Width, imageInformation.Height / (float) desc.Height);

        // Reset animation
        _animator.Initialize();

        if (_slideShowTimer != null)
          _slideShowTimer.Change(_slideShowImageDuration, TS_INFINITE);
        else
          CheckTimer();
        _playbackStartTime = DateTime.Now;
        if (_pauseTime.HasValue)
          _pauseTime = _playbackStartTime;
      }
    }

    public static bool CanPlay(IResourceLocator locator, string mimeType)
    {
      // First check the mime type
      if (!string.IsNullOrEmpty(mimeType) && !mimeType.StartsWith("image"))
        return false;

      IResourceAccessor accessor = locator.CreateAccessor();
      string ext = StringUtils.TrimToEmpty(DosPathHelper.GetExtension(accessor.ResourcePathName)).ToLowerInvariant();

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

    public bool NextItem(MediaItem mediaItem, StartTime startTime)
    {
      string mimeType;
      string title;
      if (!mediaItem.GetPlayData(out mimeType, out title))
        return false;
      IResourceLocator locator = mediaItem.GetResourceLocator();
      if (locator == null)
        return false;
      if (!CanPlay(locator, mimeType))
        return false;
      RightAngledRotation rotation = RightAngledRotation.Zero;
      bool flipX = false;
      bool flipY = false;
      MediaItemAspect pictureAspect = mediaItem[PictureAspect.ASPECT_ID];
      if (pictureAspect != null)
      {
        int orientationInfo = (int) pictureAspect[PictureAspect.ATTR_ORIENTATION];
        PictureRotation pictureRotation;
        PictureAspect.OrientationToRotation(orientationInfo, out pictureRotation);
        rotation = PlayerRotationTranslator.TranslateToRightAngledRotation(pictureRotation);
        PictureAspect.OrientationToFlip(orientationInfo, out flipX, out flipY);
      }
      SetMediaItemData(locator, title, rotation, flipX, flipY);
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

    public Size PictureSize
    {
      get
      {
        SurfaceDescription sd = _texture.GetLevelDescription(0);
        return new Size((int) (sd.Width * _textureMaxUV.Width), (int) (sd.Height * _textureMaxUV.Height));
      }
    }

    public RightAngledRotation Rotation
    {
      get
      {
        lock (_syncObj)
          return _rotation;
      }
    }

    public bool FlipX
    {
      get
      {
        lock (_syncObj)
          return _flipX;
      }
    }

    public bool FlipY
    {
      get
      {
        lock (_syncObj)
          return _flipY;
      }
    }

    #endregion

    #region ISlimDXPicturePlayer implementation

    public object PicturesLock
    {
      get { return _syncObj; }
    }

    public Texture CurrentPicture
    {
      get
      {
        lock (_syncObj)
          return _texture;
      }
    }

    public RectangleF GetTextureClip(Size outputSize)
    {
      // TODO: Execute animation in own timer
      lock (_syncObj)
      {
        TimeSpan displayTime = (_pauseTime.HasValue ? _pauseTime.Value : DateTime.Now) - _playbackStartTime;
        float animationProgress = (float) displayTime.TotalMilliseconds / (float) _slideShowImageDuration.TotalMilliseconds;
        // Flatten progress function to be in the range 0-1
        if (animationProgress < 0)
          animationProgress = 0;
        animationProgress = 1-1/(5*animationProgress*animationProgress+1);
        RectangleF textureClip = _animator.GetZoomRect(animationProgress, PictureSize, outputSize);
        return new RectangleF(textureClip.X * _textureMaxUV.Width, textureClip.Y * _textureMaxUV.Height, textureClip.Width * _textureMaxUV.Width, textureClip.Height * _textureMaxUV.Height);
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
          if (IsPaused)
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
          return _pauseTime.HasValue;
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
        _pauseTime = DateTime.Now;
        DisposeTimer();
      }
    }

    public void Resume()
    {
      lock (_syncObj)
      {
        _pauseTime = null;
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
