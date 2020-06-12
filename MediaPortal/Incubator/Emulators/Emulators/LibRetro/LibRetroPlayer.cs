using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Common.ResourceAccess;
using MediaPortal.Common.Threading;
using MediaPortal.UI.Presentation.Geometries;
using MediaPortal.UI.Presentation.Players;
using MediaPortal.UI.Presentation.Screens;
using MediaPortal.UI.SkinEngine;
using MediaPortal.UI.SkinEngine.Players;
using SharpDX.Direct3D9;
using SharpRetro.LibRetro;
using System;
using System.Drawing;

namespace Emulators.LibRetro
{
  public class LibRetroPlayer : ISharpDXVideoPlayer, IMediaPlaybackControl, IPlayerEvents, IUIContributorPlayer, IDisposable
  {
    #region Protected Members
    protected const string AUDIO_STREAM_NAME = "Audio1";
    protected static string[] DEFAULT_AUDIO_STREAM_NAMES = new[] { AUDIO_STREAM_NAME };

    protected readonly object _syncObj = new object();
    protected string _corePath;
    protected bool _isCoreLoaded;
    protected LibRetroFrontend _retro;
    protected bool _isLibretroInit;
    protected bool _isLibRetroRunning;
    protected PlayerState _state = PlayerState.Stopped;
    protected string _mediaItemTitle;
    protected bool _isMuted;
    protected int _volume;
    protected CropSettings _cropSettings;
    protected IGeometry _geometryOverride;
    protected ILocalFsResourceAccessor _accessor;
    
    // Player event delegates
    protected PlayerEventDlgt _started = null;
    protected PlayerEventDlgt _stateReady = null;
    protected PlayerEventDlgt _stopped = null;
    protected PlayerEventDlgt _ended = null;
    protected PlayerEventDlgt _playbackStateChanged = null;
    protected PlayerEventDlgt _playbackError = null;
    #endregion

    #region Ctor
    public LibRetroPlayer()
    {
      _cropSettings = ServiceRegistration.Get<IGeometryManager>().CropSettings;
    }
    #endregion

    #region LibRetroPlayer
    public LibRetroFrontend LibRetro
    {
      get { return _retro; }
    }

    public Type UIContributorType
    {
      get { return typeof(LibRetroPlayerUIContributor); }
    }

    public void SetMediaItem(LibRetroMediaItem mediaItem)
    {
      if (_retro != null)
        return;

      _state = PlayerState.Active;
      IResourceLocator locator = mediaItem.GetResourceLocator();

      string gamePath;
      if (!string.IsNullOrEmpty(mediaItem.ExtractedPath))
        gamePath = mediaItem.ExtractedPath;
      else if (locator.TryCreateLocalFsAccessor(out _accessor))
        gamePath = _accessor.LocalFileSystemPath;
      else
        return;

      string saveName = DosPathHelper.GetFileNameWithoutExtension(locator.NativeResourcePath.FileName);
      ServiceRegistration.Get<ILogger>().Debug("LibRetroPlayer: Creating LibRetroFrontend: Core Path '{0}', Game Path '{1}', Save Name '{2}'", mediaItem.LibRetroPath, gamePath, saveName);

      _corePath = mediaItem.LibRetroPath;
      _isCoreLoaded = ServiceRegistration.Get<ILibRetroCoreInstanceManager>().TrySetCoreLoading(_corePath);
      if (!_isCoreLoaded)
      {
        ShowLoadErrorDialog();
        return;
      }

      _retro = new LibRetroFrontend(mediaItem.LibRetroPath, gamePath, saveName);
      _isLibretroInit = _retro.Init();
      //if (_isLibretroInit)
      //{
      //  TimingInfo timingInfo = _retro.GetTimingInfo();
      //  if (timingInfo != null)
      //    MediaItemAspect.SetAttribute(mediaItem.Aspects, VideoAspect.ATTR_FPS, (int)timingInfo.FPS);
      //}
    }

    protected void ShowLoadErrorDialog()
    {
      ServiceRegistration.Get<IThreadPool>().Add(() => 
      {
        ServiceRegistration.Get<IDialogManager>().ShowDialog("[Emulators.Dialog.Error.Header]", "[Emulators.LibRetro.CoreAlreadyLoaded]", DialogType.OkDialog, false, DialogButtonType.Ok);
      });
    }

    protected void RunLibRetro()
    {
      if (_isLibretroInit)
      {
        _retro.Run();
        _isLibRetroRunning = true;
        FireStarted();
        FireStateReady();
        ServiceRegistration.Get<ILogger>().Debug("LibRetroPlayer: LibRetroFrontend started");
      }
      else
      {
        FireError();
      }
    }
    #endregion

    #region IPlayer
    public string MediaItemTitle
    {
      get { return _mediaItemTitle; }
    }

    public string Name
    {
      get { return "LibRetroPlayer"; }
    }

    public PlayerState State
    {
      get { return _state; }
    }

    public void Stop()
    {
      Dispose();
      _isLibRetroRunning = false;
      _state = PlayerState.Stopped;
      FireStopped();
    }
    #endregion

    #region IMediaPlaybackControl
    public bool SetPlaybackRate(double value)
    {
      return false;
    }

    public void Pause()
    {
      if (_retro == null || _retro.Paused)
        return;
      _retro.Pause();
      FirePlaybackStateChanged();
    }

    public void Resume()
    {
      if (_retro == null)
      {
        FireError();
        return;
      }

      if (!_isLibRetroRunning)
      {
        RunLibRetro();
        return;
      }

      if (_retro.Paused)
      {
        _retro.Unpause();
        FirePlaybackStateChanged();
      }
    }

    public void Restart()
    {

    }

    public TimeSpan CurrentTime
    {
      get { return TimeSpan.Zero; }
      set { }
    }

    public TimeSpan Duration
    {
      get { return TimeSpan.Zero; }
    }

    public double PlaybackRate
    {
      get { return 1; }
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
      get { return _retro != null ? _retro.Paused : false; }
    }

    public bool CanSeekForwards
    {
      get { return false; }
    }

    public bool CanSeekBackwards
    {
      get { return false; }
    }
    #endregion

    #region Video
    public CropSettings CropSettings
    {
      get { return _cropSettings; }
      set { _cropSettings = value; }
    }

    public SharpDX.Rectangle CropVideoRect
    {
      get
      {
        SharpDX.Size2 videoSize = VideoSize.ToSize2();
        return _cropSettings == null ? new SharpDX.Rectangle(0, 0, videoSize.Width, videoSize.Height) : _cropSettings.CropRect(videoSize.ToDrawingSize()).ToRect();
      }
    }

    public string EffectOverride
    {
      get { return null; }
      set { }
    }

    public IGeometry GeometryOverride
    {
      get { return _geometryOverride; }
      set { _geometryOverride = value; }
    }

    public object SurfaceLock
    {
      get { return _retro != null ? _retro.SurfaceLock : _syncObj; }
    }

    public Texture Texture
    {
      get { return _retro != null ? _retro.Texture : null; }
    }

    public SizeF VideoAspectRatio
    {
      get
      {
        if (_retro != null)
        {
          VideoInfo videoInfo = _retro.VideoInfo;
          if (videoInfo != null)
            return new SizeF(videoInfo.VirtualWidth, videoInfo.VirtualHeight);
        }
        return new SizeF(1, 1);
      }
    }

    public Size VideoSize
    {
      get
      {
        if (_retro != null)
        {
          VideoInfo videoInfo = _retro.VideoInfo;
          if (videoInfo != null)
            return new Size(videoInfo.Width, videoInfo.Height);
        }
        return new Size(0, 0);
      }
    }

    public void ReallocGUIResources()
    {
      if (_retro != null)
        _retro.ReallocGUIResources();
    }

    public void ReleaseGUIResources()
    {
      if (_retro != null)
        _retro.ReleaseGUIResources();
    }

    public bool SetRenderDelegate(RenderDlgt dlgt)
    {
      return _retro != null && _retro.SetRenderDelegate(dlgt);
    }
    #endregion

    #region Audio
    public string[] AudioStreams
    {
      get { return DEFAULT_AUDIO_STREAM_NAMES; }
    }

    public string CurrentAudioStream
    {
      get { return AudioStreams[0]; }
    }

    public bool Mute
    {
      get { return _isMuted; }
      set
      {
        if (value == _isMuted)
          return;
        _isMuted = value;
        CheckAudio();
      }
    }

    public int Volume
    {
      get { return _volume; }
      set
      {
        _volume = value;
        CheckAudio();
      }
    }

    protected void CheckAudio()
    {
      int volume = _isMuted ? 0 : _volume;
      if (_retro != null)
        _retro.SetVolume(VolumeToHundredthDeciBel(volume));
    }

    /// <summary>
    /// Helper method for calculating the hundredth decibel value, needed by DirectSound
    /// (in the range from -10000 to 0), which is logarithmic, from our volume (in the range from 0 to 100),
    /// which is linear.
    /// </summary>
    /// <param name="volume">Volume in the range from 0 to 100, in a linear scale.</param>
    /// <returns>Volume in the range from -10000 to 0, in a logarithmic scale.</returns>
    protected static int VolumeToHundredthDeciBel(int volume)
    {
      return (int)((Math.Log10(volume * 99f / 100f + 1) - 2) * 5000);
    }

    public void SetAudioStream(string audioStream) { }
    #endregion

    #region IPlayerEvents
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

    #region Event handling
    protected void FireStarted()
    {
      if (_started != null)
        _started(this);
    }

    protected void FireStateReady()
    {
      if (_stateReady != null)
        _stateReady(this);
    }

    protected void FireStopped()
    {
      if (_stopped != null)
        _stopped(this);
    }

    protected void FireEnded()
    {
      if (_ended != null)
        _ended(this);
    }

    protected void FirePlaybackStateChanged()
    {
      if (_playbackStateChanged != null)
        _playbackStateChanged(this);
    }

    protected void FireError()
    {
      if (_playbackError != null)
        _playbackError(this);
    }
    #endregion

    #region IDisposable
    public void Dispose()
    {
      if (_retro != null)
      {
        _retro.Dispose();
        _retro = null;
      }
      if (_accessor != null)
      {
        _accessor.Dispose();
        _accessor = null;
      }
      if (_isCoreLoaded)
      {
        ServiceRegistration.Get<ILibRetroCoreInstanceManager>().SetCoreUnloaded(_corePath);
        _isCoreLoaded = false;
      }
    }
    #endregion
  }
}
