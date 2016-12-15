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
using System.IO;
using System.Threading;
using MediaPortal.Common;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Common.ResourceAccess;
using MediaPortal.Common.Settings;
using MediaPortal.UI.Players.Image.Settings;
using MediaPortal.UI.Presentation.Players;
using MediaPortal.UI.SkinEngine;
using MediaPortal.UI.SkinEngine.ContentManagement;
using MediaPortal.UI.SkinEngine.Controls.Brushes.Animation;
using MediaPortal.UI.SkinEngine.Players;
using MediaPortal.Utilities;
using SharpDX;
using SharpDX.Direct3D9;
using RightAngledRotation = MediaPortal.UI.Presentation.Players.RightAngledRotation;
using Size = SharpDX.Size2;
using SizeF = SharpDX.Size2F;
using PointF = SharpDX.Vector2;

namespace MediaPortal.UI.Players.Image
{
  public class ImagePlayer : IDisposable, ISharpDXImagePlayer, IPlayerEvents, IReusablePlayer, IMediaPlaybackControl
  {
    #region Consts

    protected static readonly TimeSpan TS_INFINITE = TimeSpan.FromMilliseconds(-1);

    protected static readonly IImageAnimator STILL_IMAGE_ANIMATION = new StillImageAnimator();

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
    protected TextureAsset _texture = null;
    protected SizeF _textureMaxUV = new SizeF(1, 1);
    protected TimeSpan _slideShowImageDuration = TimeSpan.FromSeconds(10);
    protected Timer _slideShowTimer = null;
    protected bool _slideShowEnabled = false;
    protected bool _isInitalResume = true;
    protected DateTime _playbackStartTime = DateTime.MinValue;

    // Data and events for the communication with the player manager.
    protected PlayerEventDlgt _started = null;
    protected PlayerEventDlgt _stateReady = null;
    protected PlayerEventDlgt _stopped = null;
    protected PlayerEventDlgt _ended = null;
    protected PlayerEventDlgt _playbackStateChanged = null;
    protected PlayerEventDlgt _playbackError = null;

    // Image animation effect
    protected IImageAnimator _animator;

    #endregion

    public ImagePlayer()
    {
      _state = PlayerState.Stopped;
    }

    public void Dispose()
    {
      DisposeTimer();
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
      ImagePlayerSettings settings = ServiceRegistration.Get<ISettingsManager>().Load<ImagePlayerSettings>() ?? new ImagePlayerSettings();
      double durationSec = settings.SlideShowImageDuration;
      _slideShowImageDuration = durationSec == 0 ? TS_INFINITE : TimeSpan.FromSeconds(durationSec);

      // Use animation only in slideshow mode
      var newAnimator = _slideShowEnabled && settings.UseKenBurns ? new KenBurnsAnimator() : STILL_IMAGE_ANIMATION;
      bool reInit = newAnimator != _animator;
      _animator = newAnimator;
      // Reset animation if the animator has been changed (i.e. toggling between single image/slideshow)
      if (reInit)
        _animator.Initialize();

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

    protected void CheckTimer()
    {
      lock (_syncObj)
      {
        if (_state == PlayerState.Active && !IsPaused)
        {
          if (_slideShowTimer == null && _slideShowEnabled)
            _slideShowTimer = new Timer(OnSlideShowNewImage, null, _slideShowImageDuration, TS_INFINITE);
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

    void OnSlideShowNewImage(object state)
    {
      FireEnded();
    }

    #endregion

    #region Public members

    public bool SlideShowEnabled
    {
      get { return _slideShowEnabled; }
      set
      {
        _slideShowEnabled = value;
        CheckTimer();
      }
    }

    /// <summary>
    /// Sets the data of the new image to be played.
    /// </summary>
    /// <param name="locator">Resource locator of the image item.</param>
    /// <param name="mediaItemTitle">Title of the image item.</param>
    /// <param name="rotation">Rotation of the image.</param>
    /// <param name="flipX">Flipping in horizontal direction.</param>
    /// <param name="flipY">Flipping in vertical direction.</param>
    public void SetMediaItemData(IResourceLocator locator, string mediaItemTitle, RightAngledRotation rotation, bool flipX, bool flipY)
    {
      if (locator == null)
        lock (_syncObj)
        {
          _currentLocator = null;
          return;
        }

      using (IResourceAccessor ra = locator.CreateAccessor())
      {
        IFileSystemResourceAccessor fsra = ra as IFileSystemResourceAccessor;
        if (fsra == null)
          return;
        using (Stream stream = fsra.OpenRead())
        {
          // Avoid caching of stream based textures
          string key = Guid.NewGuid().ToString(); // fsra.CanonicalLocalResourcePath.Serialize();
          _texture = ContentManager.Instance.GetTexture(stream, key, true);
          if (_texture == null)
            return;
          if (!_texture.IsAllocated)
            _texture.Allocate();
          if (!_texture.IsAllocated)
            return;
        }
      }
      lock (_syncObj)
      {
        ReloadSettings();
        _state = PlayerState.Active;

        _currentLocator = locator;
        _mediaItemTitle = mediaItemTitle;
        _rotation = rotation;
        _flipX = flipX;
        _flipY = flipY;
        SurfaceDescription desc = _texture.Texture.GetLevelDescription(0);
        _textureMaxUV = new SizeF(_texture.Width / (float) desc.Width, _texture.Height / (float) desc.Height);

        // Reset animation
        _animator.Initialize();

        if (_slideShowTimer != null)
          _slideShowTimer.Change(_slideShowImageDuration, TS_INFINITE);
        else
          CheckTimer();
        _playbackStartTime = DateTime.Now;
        if (!_slideShowEnabled || _pauseTime.HasValue)
          _pauseTime = _playbackStartTime;
      }
    }

    public static bool CanPlay(IResourceLocator locator, string mimeType)
    {
      // First check the mime type
      if (!string.IsNullOrEmpty(mimeType) && !mimeType.StartsWith("image"))
        return false;

      using (IResourceAccessor accessor = locator.CreateAccessor())
      {
        if (!(accessor is IFileSystemResourceAccessor))
          return false;
        string ext = StringUtils.TrimToEmpty(DosPathHelper.GetExtension(accessor.ResourcePathName)).ToLowerInvariant();

        ImagePlayerSettings settings = ServiceRegistration.Get<ISettingsManager>().Load<ImagePlayerSettings>();
        return settings.SupportedExtensions.IndexOf(ext) > -1;
      }
    }

    #endregion

    #region IPlayer implementation

    public string Name
    {
      get { return "Image"; }
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
     SingleMediaItemAspect imageAspect;
      MediaItemAspect.TryGetAspect(mediaItem.Aspects, ImageAspect.Metadata, out imageAspect);
      if (imageAspect != null)
      {
        int orientationInfo = (int) imageAspect[ImageAspect.ATTR_ORIENTATION];
        ImageRotation imageRotation;
        ImageAspect.OrientationToRotation(orientationInfo, out imageRotation);
        rotation = PlayerRotationTranslator.TranslateToRightAngledRotation(imageRotation);
        ImageAspect.OrientationToFlip(orientationInfo, out flipX, out flipY);
      }
      SetMediaItemData(locator, title, rotation, flipX, flipY);
      return true;
    }

    #endregion

    #region IImagePlayer implementation

    public IResourceLocator CurrentImageResourceLocator
    {
      get
      {
        lock (_syncObj)
          return _currentLocator;
      }
    }

    public System.Drawing.Size ImageSize
    {
      get
      {
        return _texture != null ? new System.Drawing.Size(_texture.Width, _texture.Height) : new System.Drawing.Size();
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

    #region ISharpDXImagePlayer implementation

    public object ImagesLock
    {
      get { return _syncObj; }
    }

    public Texture CurrentImage
    {
      get
      {
        lock (_syncObj)
          return _texture != null ? _texture.Texture : null;
      }
    }

    public RectangleF GetTextureClip(Size outputSize)
    {
      // TODO: Execute animation in own timer
      lock (_syncObj)
      {
        _animator = _animator ?? STILL_IMAGE_ANIMATION;
        DateTime displayTime = _pauseTime ?? DateTime.Now;
        RectangleF textureClip = _animator.GetZoomRect(ImageSize.ToSize2(), outputSize, displayTime);
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
        SlideShowEnabled = false;
        ReloadSettings();
        DisposeTimer();
      }
    }

    public void Resume()
    {
      lock (_syncObj)
      {
        if (_isInitalResume)
        {
          _isInitalResume = false;
          return;
        }
        _pauseTime = null;
        SlideShowEnabled = true;
        ReloadSettings();
        CurrentTime = TimeSpan.Zero;
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
