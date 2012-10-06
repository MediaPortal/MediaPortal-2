#region Copyright (C) 2007-2012 Team MediaPortal

/*
    Copyright (C) 2007-2012 Team MediaPortal
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
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using DirectShowLib;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Common.Messaging;
using MediaPortal.Common.ResourceAccess;
using MediaPortal.Common.Settings;
using MediaPortal.UI.General;
using MediaPortal.UI.Players.Video.Interfaces;
using MediaPortal.UI.Players.Video.Settings;
using MediaPortal.UI.Players.Video.Tools;
using MediaPortal.UI.Presentation.Players;
using MediaPortal.UI.SkinEngine.SkinManagement;
using MediaPortal.Utilities.Exceptions;

namespace MediaPortal.UI.Players.Video
{
  /// <summary>
  /// <see cref="BaseDXPlayer"/> provides a base player for all DirectShow based players, which can be both IVideoPlayer and IAudioPlayer.
  /// </summary>
  public abstract class BaseDXPlayer : IPlayer, IDisposable, IPlayerEvents, IInitializablePlayer, IMediaPlaybackControl
  {
    #region Consts

    protected const int WM_GRAPHNOTIFY = 0x4000 + 123;
    public const string AUDIO_STREAM_NAME = "Audio1";

    protected static string[] DEFAULT_AUDIO_STREAM_NAMES = new[] { AUDIO_STREAM_NAME };
    protected static string[] EMPTY_STRING_ARRAY = new string[] { };

    protected const double PLAYBACK_RATE_PLAY_THRESHOLD = 0.05;

    public enum StreamGroup
    {
      Video = 0,
      Audio = 1,
      Subtitle = 2,
      MatroskaEdition = 18,
      DirectVobSubtitle = 6590033,
    }

    #endregion

    #region Protected Properties

    public virtual String PlayerTitle { get; protected set; }

    #endregion

    #region Variables

    // DirectShow objects
    protected IGraphBuilder _graphBuilder;
    protected DsROTEntry _rot;

    // Managed Direct3D Resources
    protected IntPtr _instancePtr;

    protected uint _streamCount = 1;

    // Internal state and variables
    protected PlayerState _state;
    protected bool _isPaused = false;
    protected int _volume = 100;
    protected bool _isMuted = false;
    protected bool _initialized = false;
    protected IResourceLocator _resourceLocator;
    protected IResourceAccessor _resourceAccessor;
    protected string _mediaItemTitle = null;
    protected AsynchronousMessageQueue _messageQueue = null;

    // Player event delegates
    protected PlayerEventDlgt _started = null;
    protected PlayerEventDlgt _stateReady = null;
    protected PlayerEventDlgt _stopped = null;
    protected PlayerEventDlgt _ended = null;
    protected PlayerEventDlgt _playbackStateChanged = null;
    protected PlayerEventDlgt _playbackError = null;

    private readonly object _syncObj = new object();

    #endregion

    #region Ctor & dtor

    protected BaseDXPlayer()
    {
      SubscribeToMessages();
    }

    public virtual void Dispose()
    {
      FilterGraphTools.TryDispose(ref _resourceAccessor);
      FilterGraphTools.TryDispose(ref _resourceLocator);
      UnsubscribeFromMessages();
    }

    public object SyncObj
    {
      get { return _syncObj; }
    }

    public abstract Guid PlayerId { get; }
    public abstract String Name { get; }

    #endregion

    #region Message handling

    protected void SubscribeToMessages()
    {
      _messageQueue = new AsynchronousMessageQueue(this, new [] { WindowsMessaging.CHANNEL });
      _messageQueue.MessageReceived += OnMessageReceived;
      _messageQueue.Start();
    }

    protected virtual void UnsubscribeFromMessages()
    {
      if (_messageQueue == null)
        return;
      _messageQueue.Shutdown();
      _messageQueue = null;
    }

    protected virtual void OnMessageReceived(AsynchronousMessageQueue queue, SystemMessage message)
    {
      if (message.ChannelName == WindowsMessaging.CHANNEL)
      {
        Message m = (Message) message.MessageData[WindowsMessaging.MESSAGE];
        if (m.LParam.Equals(_instancePtr))
        {
          if (m.Msg == WM_GRAPHNOTIFY)
          {
            IMediaEventEx eventEx = (IMediaEventEx) _graphBuilder;

            EventCode evCode;
            IntPtr param1, param2;

            while (eventEx.GetEvent(out evCode, out param1, out param2, 0) == 0)
            {
              eventEx.FreeEventParams(evCode, param1, param2);
              if (evCode == EventCode.Complete)
              {
                _state = PlayerState.Ended;
                ServiceRegistration.Get<ILogger>().Debug("{0}: Playback ended", PlayerTitle);
                // TODO: RemoveResumeData();
                FireEnded();
                return;
              }
            }
          }
        }
      }
    }

    #endregion
    
    #region IInitializablePlayer implementation

    public void SetMediaItem(IResourceLocator locator, string mediaItemTitle)
    {
      // free previous opened resource
      FilterGraphTools.TryDispose(ref _resourceAccessor);
      FilterGraphTools.TryDispose(ref _rot);

      _state = PlayerState.Active;
      _isPaused = true;
      try
      {
        _resourceLocator = locator;
        _mediaItemTitle = mediaItemTitle;
        if (_resourceLocator.NativeResourcePath.IsNetworkResource)
        {
          _resourceAccessor = _resourceLocator.CreateAccessor() as INetworkResourceAccessor;
          if (_resourceAccessor == null)
            throw new IllegalCallException("The VideoPlayer can only play network resources of type INetworkResourceAccessor");

          ServiceRegistration.Get<ILogger>().Debug("{0}: Initializing for network media item '{1}'", PlayerTitle, SourcePathOrUrl);
        }
        else
        {
          ILocalFsResourceAccessor lfsr;
          if (!_resourceLocator.TryCreateLocalFsAccessor(out lfsr))
            throw new IllegalCallException("The VideoPlayer can only play local file system resources");

          _resourceAccessor = lfsr;
          ServiceRegistration.Get<ILogger>().Debug("{0}: Initializing for media item '{1}'", PlayerTitle, SourcePathOrUrl);
        }

        // Create a DirectShow FilterGraph
        CreateGraphBuilder();

        // Add it in ROT (Running Object Table) for debug purpose, it allows to view the Graph from outside (i.e. graphedit)
        _rot = new DsROTEntry(_graphBuilder);

        // Add a notification handler (see WndProc)
        _instancePtr = Marshal.AllocCoTaskMem(4);
        IMediaEventEx mee = _graphBuilder as IMediaEventEx;
        if (mee != null)
          mee.SetNotifyWindow(SkinContext.Form.Handle, WM_GRAPHNOTIFY, _instancePtr);

        // Create the Allocator / Presenter object
        AddPresenter();

        ServiceRegistration.Get<ILogger>().Debug("{0}: Adding audio renderer", PlayerTitle);
        AddAudioRenderer();

        ServiceRegistration.Get<ILogger>().Debug("{0}: Adding preferred codecs", PlayerTitle);
        AddPreferredCodecs();

        ServiceRegistration.Get<ILogger>().Debug("{0}: Adding file source", PlayerTitle);
        AddFileSource();

        ServiceRegistration.Get<ILogger>().Debug("{0}: Run graph", PlayerTitle);

        //This needs to be done here before we check if the evr pins are connected
        //since this method gives players the chance to render the last bits of the graph
        OnBeforeGraphRunning();

        // Now run the graph, i.e. the DVD player needs a running graph before getting informations from dvd filter.
        IMediaControl mc = (IMediaControl) _graphBuilder;
        int hr = mc.Run();
        DsError.ThrowExceptionForHR(hr);

        _initialized = true;
        OnGraphRunning();
      }
      catch (Exception)
      {
        Shutdown();
        throw;
      }
    }

    /// <summary>
    /// Indicates if the current resource is a network resource, accessed by a <seealso cref="INetworkResourceAccessor"/>.
    /// </summary>
    public bool IsNetworkResource
    {
      get { return _resourceAccessor is INetworkResourceAccessor; }
    }

    /// <summary>
    /// Indicates if the current resource is a local filesystem resource, accessed by a <seealso cref="ILocalFsResourceAccessor"/>.
    /// </summary>
    public bool IsLocalFilesystemResource
    {
      get { return _resourceAccessor is ILocalFsResourceAccessor; }
    }

    /// <summary>
    /// Gets the current resource path or URL depending on the type (<see cref="IsNetworkResource"/> and <see cref="IsLocalFilesystemResource"/>).
    /// </summary>
    public string SourcePathOrUrl
    {
      get { return IsNetworkResource ? ((INetworkResourceAccessor)_resourceAccessor).URL : 
        IsLocalFilesystemResource ? ((ILocalFsResourceAccessor)_resourceAccessor).LocalFileSystemPath : null; }
    }

    /// <summary>
    /// Add presenter can be used by derived classes to add and configure EVR presenter, which is only needed for video players.
    /// </summary>
    protected virtual void AddPresenter () {}

    #endregion

    #region IPlayerEvents implementation

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
      _stopped = null;
      _ended = null;
      _playbackStateChanged = null;
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

    /// <summary>
    /// Callback executed when video size if present.
    /// </summary>
    /// <param name="evrCallback">evrCallback</param>
    protected virtual void OnVideoSizePresent(EVRCallback evrCallback)
    {
      FireStateReady();
    }

    /// <summary>
    /// Called just before starting the graph.
    /// </summary>
    protected virtual void OnBeforeGraphRunning() { }

    /// <summary>
    /// Called when graph is started.
    /// </summary>
    protected virtual void OnGraphRunning()
    { }

    #endregion

    #region Graph building

    /// <summary>
    /// Creates a new IFilterGraph2 interface.
    /// </summary>
    protected virtual void CreateGraphBuilder()
    {
      _graphBuilder = (IFilterGraph2) new FilterGraph();
    }

    /// <summary>
    /// Try to add filter by name to graph.
    /// </summary>
    /// <param name="codecInfo">Filter name to add</param>
    /// <returns>true if successful</returns>
    protected bool TryAdd(CodecInfo codecInfo)
    {
      return TryAdd(codecInfo, FilterCategory.LegacyAmFilterCategory);
    }

    /// <summary>
    /// Try to add filter by name to graph.
    /// </summary>
    /// <param name="codecInfo">Filter name to add</param>
    /// <param name="filterCategory">GUID of filter category (<see cref="DirectShowLib.FilterCategory"/> members)></param>
    /// <returns>true if successful</returns>
    protected bool TryAdd(CodecInfo codecInfo, Guid filterCategory)
    {
      if (codecInfo == null)
        return false;
      IBaseFilter tempFilter = FilterGraphTools.AddFilterByName(_graphBuilder, filterCategory, codecInfo.Name);
      return tempFilter != null;
    }

    /// <summary>
    /// Adds the file source filter to the graph.
    /// </summary>
    protected virtual void AddFileSource()
    {
      // Render the file
      int hr = _graphBuilder.RenderFile(SourcePathOrUrl, null);
      DsError.ThrowExceptionForHR(hr);
    }

    /// <summary>
    /// Adds preferred audio renderer.
    /// </summary>
    protected virtual void AddAudioRenderer()
    {
      VideoSettings settings = ServiceRegistration.Get<ISettingsManager>().Load<VideoSettings>();
      if (settings == null)
        return;
      TryAdd(settings.AudioRenderer, FilterCategory.AudioRendererCategory);
    }

    /// <summary>
    /// Adds preferred audio/video codecs.
    /// </summary>
    protected virtual void AddPreferredCodecs()
    {
      VideoSettings settings = ServiceRegistration.Get<ISettingsManager>().Load<VideoSettings>();
      if (settings == null)
        return;

      //IAMPluginControl is supported in Win7 and later only.
      try
      {
        IAMPluginControl pc = new DirectShowPluginControl() as IAMPluginControl;
        if (pc != null)
        {
          if (settings.Mpeg2Codec != null)
            pc.SetPreferredClsid(MediaSubType.Mpeg2Video, settings.Mpeg2Codec.GetCLSID());

          if (settings.H264Codec != null)
            pc.SetPreferredClsid(MediaSubType.H264, settings.H264Codec.GetCLSID());

          if (settings.AVCCodec != null)
            pc.SetPreferredClsid(CodecHandler.MEDIASUBTYPE_AVC, settings.AVCCodec.GetCLSID());

          if (settings.AudioCodecLATMAAC != null)
            pc.SetPreferredClsid(CodecHandler.MEDIASUBTYPE_LATM_AAC_AUDIO, settings.AudioCodecLATMAAC.GetCLSID());

          if (settings.AudioCodecAAC != null)
            pc.SetPreferredClsid(CodecHandler.MEDIASUBTYPE_AAC_AUDIO, settings.AudioCodecAAC.GetCLSID());

          if (settings.AudioCodec != null)
          {
            foreach (Guid guid in new[]
                                    {
                                      MediaSubType.Mpeg2Audio,
                                      MediaSubType.MPEG1AudioPayload,
                                      CodecHandler.WMMEDIASUBTYPE_MP3,
                                      CodecHandler.MEDIASUBTYPE_MPEG1_AUDIO,
                                      CodecHandler.MEDIASUBTYPE_MPEG2_AUDIO
                                    })
              pc.SetPreferredClsid(guid, settings.AudioCodec.GetCLSID());
          }
        }
      }
      catch (Exception ex)
      {
        ServiceRegistration.Get<ILogger>().Debug("{0}: Exception in IAMPluginControl: {1}", PlayerTitle, ex.ToString());
      }
    }

    #endregion

    #region Graph shutdown

    /// <summary>
    /// Frees the audio/video codecs.
    /// </summary>
    protected abstract void FreeCodecs ();

    protected void Shutdown(bool keepResourceAccessor = false)
    {
      StopSeeking();
      _initialized = false;
      ServiceRegistration.Get<ILogger>().Debug("{0}: Stop playing", PlayerTitle);

      try
      {
        if (_graphBuilder != null)
        {
          FilterState state;
          IMediaEventEx me = (IMediaEventEx) _graphBuilder;
          IMediaControl mc = (IMediaControl) _graphBuilder;

          me.SetNotifyWindow(IntPtr.Zero, 0, IntPtr.Zero);

          mc.GetState(10, out state);
          if (state != FilterState.Stopped)
          {
            mc.Stop();
            mc.GetState(10, out state);
            ServiceRegistration.Get<ILogger>().Debug("{0}: Graph state after stop command: {1}", PlayerTitle, state);
          }
        }
      }
      catch (Exception ex)
      {
        ServiceRegistration.Get<ILogger>().Debug("{0}: Exception when stopping graph: {1}", PlayerTitle, ex.ToString());
      }
      finally
      {
        if (_instancePtr != IntPtr.Zero)
        {
          Marshal.FreeCoTaskMem(_instancePtr);
          _instancePtr = IntPtr.Zero;
        }

        FreeCodecs();
      }
      // Dispose resource locator and accessor
      if (!keepResourceAccessor)
      {
        FilterGraphTools.TryDispose(ref _resourceAccessor);
        FilterGraphTools.TryDispose(ref _resourceLocator);
      }
    }

    #endregion

    #region Audio

    /// <summary>
    /// Helper method for calculating the hundredth decibel value, needed by the <see cref="IBasicAudio"/>
    /// interface (in the range from -10000 to 0), which is logarithmic, from our volume (in the range from 0 to 100),
    /// which is linear.
    /// </summary>
    /// <param name="volume">Volume in the range from 0 to 100, in a linear scale.</param>
    /// <returns>Volume in the range from -10000 to 0, in a logarithmic scale.</returns>
    protected static int VolumeToHundredthDeciBel(int volume)
    {
      return (int) ((Math.Log10(volume * 99f / 100f + 1) - 2) * 5000);
    }

    protected void CheckAudio()
    {
      int volume = _isMuted ? 0 : _volume;
      IBasicAudio audio = _graphBuilder as IBasicAudio;
      if (audio != null)
        // Our volume range is from 0 to 100, IBasicAudio volume range is from -10000 to 0 (in hundredth decibel).
        // See http://msdn.microsoft.com/en-us/library/dd389538(VS.85).aspx (IBasicAudio::put_Volume method)
        audio.put_Volume(VolumeToHundredthDeciBel(volume));
    }

    #endregion

    public virtual TimeSpan CurrentTime
    {
      get
      {
        if (!_initialized || !(_graphBuilder is IMediaSeeking))
          return new TimeSpan();
        IMediaSeeking mediaSeeking = (IMediaSeeking) _graphBuilder;
        long lStreamPos;

        mediaSeeking.GetCurrentPosition(out lStreamPos); // stream position
        double fCurrentPos = lStreamPos;
        fCurrentPos /= 10000000d;

        long lContentStart, lContentEnd;
        mediaSeeking.GetAvailable(out lContentStart, out lContentEnd);
        double fContentStart = lContentStart;
        fContentStart /= 10000000d;
        fCurrentPos -= fContentStart;
        return new TimeSpan(0, 0, 0, 0, (int) (fCurrentPos * 1000.0f));
      }
      set
      {
        ServiceRegistration.Get<ILogger>().Debug("{0}: Seek to {1} seconds", PlayerTitle, value.TotalSeconds);

        if (_state != PlayerState.Active)
          // If the player isn't active when setting its position, we will switch to pause mode to prevent the
          // player from run.
          Pause();

        IMediaSeeking mediaSeeking = _graphBuilder as IMediaSeeking;
        if (mediaSeeking == null)
          return;
        double dTimeInSecs = value.TotalSeconds;
        dTimeInSecs *= 10000000d;

        long lContentStart, lContentEnd;

        mediaSeeking.GetAvailable(out lContentStart, out lContentEnd);
        double dContentStart = lContentStart;
        double dContentEnd = lContentEnd;

        dTimeInSecs += dContentStart;
        if (dTimeInSecs > dContentEnd)
          dTimeInSecs = dContentEnd;

        DsLong seekPos = new DsLong((long) dTimeInSecs);
        DsLong stopPos = new DsLong(0);

        int hr = mediaSeeking.SetPositions(seekPos, AMSeekingSeekingFlags.AbsolutePositioning, stopPos, AMSeekingSeekingFlags.NoPositioning);
        if (hr != 0)
          ServiceRegistration.Get<ILogger>().Warn("{0}: Failed to seek, hr: {1}", PlayerTitle, hr);
      }
    }

    public virtual TimeSpan Duration
    {
      get
      {
        if (!_initialized || !(_graphBuilder is IMediaSeeking))
          return new TimeSpan();
        IMediaSeeking mediaSeeking = (IMediaSeeking) _graphBuilder;
        long lContentStart, lContentEnd;
        mediaSeeking.GetAvailable(out lContentStart, out lContentEnd);
        double fContentStart = lContentStart;
        double fContentEnd = lContentEnd;
        fContentStart /= 10000000d;
        fContentEnd /= 10000000d;
        fContentEnd -= fContentStart;
        return new TimeSpan(0, 0, 0, 0, (int) (fContentEnd * 1000.0f));
      }
    }

    public virtual double PlaybackRate
    {
      get
      {
        IMediaSeeking mediaSeeking = _graphBuilder as IMediaSeeking;
        double rate;
        if (mediaSeeking == null || mediaSeeking.GetRate(out rate) != 0)
          return 1.0;
        return rate;
      }
    }

    public virtual bool SetPlaybackRate(double value)
    {
      if (_graphBuilder == null)
        return false;
      IMediaSeeking mediaSeeking = _graphBuilder as IMediaSeeking;
      if (mediaSeeking == null)
        return false;
      double currentRate;
      if (mediaSeeking.GetRate(out currentRate) == 0 && currentRate != value)
      {
        bool result = mediaSeeking.SetRate(value) == 0;
        if (result)
          FirePlaybackStateChanged();
        return result;
      }
      return false;
    }

    public virtual bool IsPlayingAtNormalRate
    {
      get { return Math.Abs(PlaybackRate - 1) < PLAYBACK_RATE_PLAY_THRESHOLD; }
    }

    public virtual bool IsSeeking
    {
      get { return _state == PlayerState.Active && !IsPlayingAtNormalRate; }
    }

    protected void StopSeeking()
    {
      SetPlaybackRate(1);
    }

    public virtual bool CanSeekForwards
    {
      get
      {
        IMediaSeeking mediaSeeking = _graphBuilder as IMediaSeeking;
        AMSeekingSeekingCapabilities capabilities;
        if (mediaSeeking == null || mediaSeeking.GetCapabilities(out capabilities) != 0)
          return false;
        return (capabilities & AMSeekingSeekingCapabilities.CanSeekForwards) != 0;
      }
    }

    public virtual bool CanSeekBackwards
    {
      get
      {
        IMediaSeeking mediaSeeking = _graphBuilder as IMediaSeeking;
        AMSeekingSeekingCapabilities capabilities;
        if (mediaSeeking == null || mediaSeeking.GetCapabilities(out capabilities) != 0)
          return false;
        return (capabilities & AMSeekingSeekingCapabilities.CanSeekBackwards) != 0;
      }
    }

    public bool IsPaused
    {
      get { return _isPaused; }
    }

    public virtual void Stop()
    {
      if (_state == PlayerState.Stopped)
        return;

      ServiceRegistration.Get<ILogger>().Debug("{0}: Stop", PlayerTitle);
      // FIXME
      //        ResetRefreshRate();
      // TODO: WriteResumeData();
      StopSeeking();
      _isPaused = false;
      Shutdown();
      FireStopped();
    }

    public void Pause()
    {
      if (_isPaused)
        return;

      ServiceRegistration.Get<ILogger>().Debug("{0}: Pause", PlayerTitle);
      IMediaControl mc = _graphBuilder as IMediaControl;
      if (mc != null)
        mc.Pause();
      StopSeeking();
      _isPaused = true;
      _state = PlayerState.Active;
      FirePlaybackStateChanged();
    }

    public void Resume()
    {
      if (!_isPaused && !IsSeeking)
        return;

      ServiceRegistration.Get<ILogger>().Debug("{0}: Resume", PlayerTitle);
      IMediaControl mc = _graphBuilder as IMediaControl;
      if (mc != null)
      {
        int hr = mc.Run();
        if (hr != 0 && hr != 1)
        {
          ServiceRegistration.Get<ILogger>().Error("{0}: Resume Failed to start: {0:X}", PlayerTitle, hr);
          Shutdown();
          FireStopped();
          return;
        }
      }
      StopSeeking();
      _isPaused = false;
      _state = PlayerState.Active;
      FirePlaybackStateChanged();
    }

    public void Restart()
    {
      CurrentTime = new TimeSpan(0, 0, 0);
      IMediaControl mc = (IMediaControl) _graphBuilder;
      mc.Run();
      StopSeeking();
      _isPaused = false;
      _state = PlayerState.Active;
      FireStarted();
    }

    public string MediaItemTitle
    {
      get { return _mediaItemTitle; }
    }

    #region Audio streams

    /// <summary>    /// Helper method to try a lookup of missing LCID from stream names. It compares the given <paramref name="streamName"/> 
    /// with the available <see cref="CultureInfo.ThreeLetterISOLanguageName"/> and <see cref="CultureInfo.TwoLetterISOLanguageName"/>.
    /// </summary>
    /// <param name="streamName">Stream name to check.</param>
    /// <returns>Found LCID or <c>0</c></returns>
    protected int LookupLcidFromName(string streamName)
    {
      if (string.IsNullOrEmpty(streamName))
        return 0;

      int len = streamName.Length;
      if (len < 2 || len > 3)
        return 0;

      streamName = streamName.ToLowerInvariant();
      CultureInfo culture = CultureInfo.GetCultures(CultureTypes.SpecificCultures).FirstOrDefault
        (c =>
          len == 3 && c.ThreeLetterISOLanguageName == streamName ||
          len == 2 && c.TwoLetterISOLanguageName == streamName
        );
      return culture == null ? 0 : culture.LCID;
    }

    #endregion

    public PlayerState State
    {
      get { return _state; }
      set { _state = value; }
    }

    public int Volume
    {
      get { return _volume; }
      set
      {
        if (_volume == value)
          return;
        _volume = value;
        CheckAudio();
      }
    }

    public bool Mute
    {
      get { return (_isMuted); }
      set
      {
        if (_isMuted == value)
          return;
        _isMuted = value;
        CheckAudio();
      }
    }

    #region Base overrides

    public override string ToString()
    {
      return string.Format("{0}: {1}", GetType().Name, _resourceAccessor != null ? _resourceAccessor.ResourceName : "no resource");
    }

    #endregion
  }
}
