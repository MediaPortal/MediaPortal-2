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
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Security;
using System.Windows.Forms;
using DirectShowLib;
using MediaPortal.Core;
using MediaPortal.Core.Logging;
using MediaPortal.Core.MediaManagement.ResourceAccess;
using MediaPortal.Core.Messaging;
using MediaPortal.Core.Settings;
using MediaPortal.UI.General;
using MediaPortal.UI.Players.Video.Interfaces;
using MediaPortal.UI.Players.Video.Settings;
using MediaPortal.UI.Players.Video.Tools;
using MediaPortal.UI.Presentation.Geometries;
using MediaPortal.UI.Presentation.Players;
using MediaPortal.UI.SkinEngine.DirectX;
using MediaPortal.UI.SkinEngine.Players;
using MediaPortal.UI.SkinEngine.SkinManagement;
using MediaPortal.Utilities.Exceptions;
using SlimDX.Direct3D9;
using System.Globalization;

namespace MediaPortal.UI.Players.Video
{
  public class VideoPlayer : ISlimDXVideoPlayer, IDisposable, IPlayerEvents, IInitializablePlayer, IMediaPlaybackControl, ISubtitlePlayer
  {
    #region Classes & interfaces

    [ComImport, Guid("fa10746c-9b63-4b6c-bc49-fc300ea5f256")]
    public class EnhancedVideoRenderer { }

    [ComImport, SuppressUnmanagedCodeSecurity,
     Guid("83E91E85-82C1-4ea7-801D-85DC50B75086"),
     InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IEVRFilterConfig
    {
      int SetNumberOfStreams(uint dwMaxStreams);
      int GetNumberOfStreams(ref uint pdwMaxStreams);
    }

    #endregion

    #region DLL imports

    [DllImport("DShowHelper.dll", ExactSpelling = true, CharSet = CharSet.Auto, SetLastError = true)]
    private static extern int EvrInit(IEVRPresentCallback callback, uint dwD3DDevice,
        IBaseFilter evrFilter, uint monitor);

    [DllImport("DShowHelper.dll", ExactSpelling = true, CharSet = CharSet.Auto, SetLastError = true)]
    private static extern void EvrDeinit(int handle);

    [DllImport("DShowHelper.dll", ExactSpelling = true, CharSet = CharSet.Auto, SetLastError = true)]
    private static extern void EvrEnableFrameSkipping(int handle, bool onOff);

    [DllImport("DShowHelper.dll", ExactSpelling = true, CharSet = CharSet.Auto, SetLastError = true)]
    private static extern void EvrFreeResources(int handle);

    [DllImport("DShowHelper.dll", ExactSpelling = true, CharSet = CharSet.Auto, SetLastError = true)]
    private static extern void EvrReAllocResources(int handle);

    #endregion

    #region Consts

    protected const int WM_GRAPHNOTIFY = 0x4000 + 123;
    private const string EVR_FILTER_NAME = "Enhanced Video Renderer";

    public const string PLAYER_ID_STR = "9EF8D975-575A-4c64-AA54-500C97745969";
    public const string AUDIO_STREAM_NAME = "Audio1";
    
    protected static string[] DEFAULT_AUDIO_STREAM_NAMES = new string[] { AUDIO_STREAM_NAME };
    protected static string[] EMPTY_STRING_ARRAY = new string[] { };

    // The default name for "No subtitles available" or "Subtitles disabled".
    private const string NO_SUBTITLES = "No subtitles";

    protected const double PLAYBACK_RATE_PLAY_THRESHOLD = 0.05;

    #region Protected Properties

    /// <summary>
    /// MediaSubTypes lookup list.
    /// </summary>
    protected Dictionary<Guid, String> MediaSubTypes = new Dictionary<Guid, string>();
    protected String PlayerTitle = "VideoPlayer";

    #endregion

    #endregion

    #region Variables

    // DirectShow objects
    protected IGraphBuilder _graphBuilder;
    protected DsROTEntry _rot;
    protected IBaseFilter _evr;
    protected EVRCallback _evrCallback;

    // Managed Direct3D Resources
    protected Size _displaySize = new Size(100, 100);

    protected IntPtr _instancePtr;
    protected int _allocatorKey = -1;

    protected Size _previousTextureSize;
    protected Size _previousVideoSize;
    protected Size _previousAspectRatio;
    protected Size _previousDisplaySize;
    protected uint _streamCount = 1;
    protected SizeF _maxUV = new SizeF(1.0f, 1.0f);

    // Filter graph related 
    protected CodecHandler.CodecCapabilities _graphCapabilities; // Capabilities which are currently added to graph
    protected CodecHandler.CodecCapabilities _requiredCapabilities; // Required capabilities for playback

    // Internal state and variables
    protected IGeometry _geometryOverride = null;
    protected string _effectOverride = null;
    protected CropSettings _cropSettings;

    protected PlayerState _state;
    protected bool _isPaused = false;
    protected int _volume = 100;
    protected bool _isMuted = false;
    protected bool _initialized = false;
    protected readonly List<IPin> _evrConnectionPins = new List<IPin>();
    protected IResourceLocator _resourceLocator;
    protected ILocalFsResourceAccessor _resourceAccessor;
    protected string _mediaItemTitle = null;
    protected AsynchronousMessageQueue _messageQueue = null;

    // Player event delegates
    protected PlayerEventDlgt _started = null;
    protected PlayerEventDlgt _stateReady = null;
    protected PlayerEventDlgt _stopped = null;
    protected PlayerEventDlgt _ended = null;
    protected PlayerEventDlgt _playbackStateChanged = null;
    protected PlayerEventDlgt _playbackError = null;

    protected StreamInfoHandler _streamInfoAudio = null;
    protected StreamInfoHandler _streamInfoSubtitles = null;

    #endregion

    #region Ctor & dtor

    public VideoPlayer()
    {
      _cropSettings = ServiceRegistration.Get<IGeometryManager>().CropSettings;

      // EVR is available since Vista
      OperatingSystem osInfo = Environment.OSVersion;
      if (osInfo.Version.Major <= 5)
        throw new EnvironmentException("This video player can only run on Windows Vista or above");

      SubscribeToMessages();
      PlayerTitle = "VideoPlayer";

      // Default video player capabilities
      _requiredCapabilities = CodecHandler.CodecCapabilities.VideoDIVX | CodecHandler.CodecCapabilities.AudioMPEG;

      // Init the MediaSubTypes dictionary
      InitMediaSubTypes();
    }

    public void Dispose()
    {
      FilterGraphTools.TryDispose(ref _resourceAccessor);
      FilterGraphTools.TryDispose(ref _resourceLocator);
      UnsubscribeFromMessages();
    }

    public object SyncObj
    {
      get
      {
        IPlayerManager playerManager = ServiceRegistration.Get<IPlayerManager>();
        return playerManager.SyncObj;
      }
    }

    void InitMediaSubTypes()
    {
      MediaSubTypes[CodecHandler.WMMEDIASUBTYPE_ACELPnet] = "ACELPnet"; //WMMEDIASUBTYPE_ACELPnet	
      MediaSubTypes[CodecHandler.WMMEDIASUBTYPE_Base] = "Base"; //WMMEDIASUBTYPE_Base
      MediaSubTypes[CodecHandler.WMMEDIASUBTYPE_DRM] = "DRM"; //WMMEDIASUBTYPE_DRM
      MediaSubTypes[CodecHandler.WMMEDIASUBTYPE_MP3] = "MP3"; //WMMEDIASUBTYPE_MP3
      MediaSubTypes[CodecHandler.WMMEDIASUBTYPE_MP43] = "MP43"; //WMMEDIASUBTYPE_MP43
      MediaSubTypes[CodecHandler.WMMEDIASUBTYPE_MP4S] = "MP4S"; //WMMEDIASUBTYPE_MP4S
      MediaSubTypes[CodecHandler.WMMEDIASUBTYPE_M4S2] = "M4S2"; //WMMEDIASUBTYPE_M4S2
      MediaSubTypes[CodecHandler.WMMEDIASUBTYPE_P422] = "P422"; //WMMEDIASUBTYPE_P422
      MediaSubTypes[CodecHandler.WMMEDIASUBTYPE_MPEG2_VIDEO] = "MPEG2"; //WMMEDIASUBTYPE_MPEG2_VIDEO
      MediaSubTypes[CodecHandler.WMMEDIASUBTYPE_MSS1] = "MSS1"; //WMMEDIASUBTYPE_MSS1
      MediaSubTypes[CodecHandler.WMMEDIASUBTYPE_MSS2] = "MSS2"; //WMMEDIASUBTYPE_MSS2
      MediaSubTypes[CodecHandler.WMMEDIASUBTYPE_PCM] = "PCM"; //WMMEDIASUBTYPE_PCM
      MediaSubTypes[CodecHandler.WMMEDIASUBTYPE_WebStream] = "WebStream"; //WMMEDIASUBTYPE_WebStream
      MediaSubTypes[CodecHandler.WMMEDIASUBTYPE_WMAudio_Lossless] = "WMA Lossless"; //WMMEDIASUBTYPE_WMAudio_Lossless
      MediaSubTypes[CodecHandler.WMMEDIASUBTYPE_WMAudioV2] = "WMA v2"; //WMMEDIASUBTYPE_WMAudioV2
      MediaSubTypes[CodecHandler.WMMEDIASUBTYPE_WMAudioV7] = "WMA v7"; //WMMEDIASUBTYPE_WMAudioV7
      MediaSubTypes[CodecHandler.WMMEDIASUBTYPE_WMAudioV8] = "WMA v8"; //WMMEDIASUBTYPE_WMAudioV8
      MediaSubTypes[CodecHandler.WMMEDIASUBTYPE_WMAudioV9] = "WMA v9"; //WMMEDIASUBTYPE_WMAudioV9
      MediaSubTypes[CodecHandler.WMMEDIASUBTYPE_WMSP1] = "WMSP1"; //WMMEDIASUBTYPE_WMSP1
      MediaSubTypes[CodecHandler.WMMEDIASUBTYPE_WMV1] = "WMV1"; //WMMEDIASUBTYPE_WMV1
      MediaSubTypes[CodecHandler.WMMEDIASUBTYPE_WMV2] = "WMV2"; //WMMEDIASUBTYPE_WMV2
      MediaSubTypes[CodecHandler.WMMEDIASUBTYPE_WMV3] = "WMV3"; //WMMEDIASUBTYPE_WMV3
      MediaSubTypes[CodecHandler.WMMEDIASUBTYPE_WMVA] = "WMVA"; //WMMEDIASUBTYPE_WMVA
      MediaSubTypes[CodecHandler.WMMEDIASUBTYPE_WMVP] = "WMVP"; //WMMEDIASUBTYPE_WMVP
      MediaSubTypes[CodecHandler.WMMEDIASUBTYPE_WVP2] = "WVP2"; //WMMEDIASUBTYPE_WVP2
      MediaSubTypes[CodecHandler.MEDIASUBTYPE_AC3_AUDIO] = "AC3"; //MEDIASUBTYPE_AC3_AUDIO
      MediaSubTypes[CodecHandler.MEDIASUBTYPE_AC3_AUDIO_OTHER] = "AC3"; //MEDIASUBTYPE_ ???
      MediaSubTypes[CodecHandler.MEDIASUBTYPE_DDPLUS_AUDIO] = "AC3+"; //MEDIASUBTYPE_DDPLUS_AUDIO
      MediaSubTypes[CodecHandler.MEDIASUBTYPE_MPEG1_PAYLOAD] = "MPEG1"; //MEDIASUBTYPE_MPEG1_PAYLOAD
      MediaSubTypes[CodecHandler.MEDIASUBTYPE_MPEG1_AUDIO] = "MPEG1"; //MEDIASUBTYPE_MPEG1_AUDIO
      MediaSubTypes[CodecHandler.MEDIASUBTYPE_MPEG2_AUDIO] = "MPEG2"; //MEDIASUBTYPE_MPEG2_AUDIO
      MediaSubTypes[CodecHandler.MEDIASUBTYPE_LATM_AAC_AUDIO] = "LATM AAC"; //MEDIASUBTYPE_LATM_AAC_AUDIO
      MediaSubTypes[CodecHandler.MEDIASUBTYPE_AAC_AUDIO] = "AAC"; //MEDIASUBTYPE_AAC_AUDIO
    }
    #endregion

    #region Message handling

    protected void SubscribeToMessages()
    {
      _messageQueue = new AsynchronousMessageQueue(this, new string[]
        {
           WindowsMessaging.CHANNEL
        });
      _messageQueue.MessageReceived += OnMessageReceived;
      _messageQueue.Start();
    }

    protected void UnsubscribeFromMessages()
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

    public void SetMediaItemLocator(IResourceLocator locator)
    {
      // free previous opened resource
      FilterGraphTools.TryDispose(ref _resourceAccessor);
      FilterGraphTools.TryDispose(ref _rot);

      _resourceLocator = locator;
      _resourceAccessor = _resourceLocator.CreateLocalFsAccessor();
      _state = PlayerState.Active;
      _isPaused = true;
      ServiceRegistration.Get<ILogger>().Debug("{0}: Initializing for media file '{1}'", PlayerTitle, _resourceAccessor.LocalFileSystemPath);

      try
      {
        int hr;
        AllocateResources();

        // Create a DirectShow FilterGraph
        CreateGraphBuilder();

        // Add it in ROT (Running Object Table) for debug purpose, it allows to view the Graph from outside (i.e. graphedit)
        _rot = new DsROTEntry(_graphBuilder);

        // Add a notification handler (see WndProc)
        _instancePtr = Marshal.AllocCoTaskMem(4);
        IMediaEventEx mee = _graphBuilder as IMediaEventEx;
        if (mee != null)
        {
          hr = mee.SetNotifyWindow(SkinContext.Form.Handle, WM_GRAPHNOTIFY, _instancePtr);
        }

        // Create the Allocator / Presenter object
        _evrCallback = new EVRCallback {CropSettings = _cropSettings};
        _evrCallback.VideoSizePresent += OnVideoSizePresent;

        AddEvr();

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
        hr = mc.Run();
        DsError.ThrowExceptionForHR(hr);

        OnGraphRunning();
        _initialized = true;
      }
      catch (Exception)
      {
        Shutdown();
        throw;
      }
    }

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
    {
      SetPreferredSubtitle();
      SetPreferredAudio();
    }

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
    /// Adds the EVR to graph.
    /// </summary>
    protected virtual void AddEvr()
    {
      ServiceRegistration.Get<ILogger>().Debug("{0}: Initialize EVR", PlayerTitle);

      _evr = (IBaseFilter) new EnhancedVideoRenderer();

      IEVRFilterConfig config = (IEVRFilterConfig) _evr;

      //set the number of video/subtitle/cc streams that are allowed to be connected to EVR
      config.SetNumberOfStreams(_streamCount);

      int ordinal = GraphicsDevice.Device.Capabilities.AdapterOrdinal;
      AdapterInformation ai = MPDirect3D.Direct3D.Adapters[ordinal];
      IntPtr hMonitor = MPDirect3D.Direct3D.GetAdapterMonitor(ai.Adapter);
      IntPtr upDevice = GraphicsDevice.Device.ComPointer;
      _allocatorKey = EvrInit(_evrCallback, (uint) upDevice.ToInt32(), _evr, (uint) hMonitor.ToInt32());
      if (_allocatorKey < 0)
      {
        throw new VideoPlayerException("Initializing of EVR failed");
      }
      _graphBuilder.AddFilter(_evr, EVR_FILTER_NAME);
    }

    /// <summary>
    /// Enables/disables frame skipping. Used for DVD Player.
    /// </summary>
    /// <param name="onOff"><c>true</c> enables frame skipping, <c>false</c> disables it.</param>
    protected void EnableFrameSkipping(bool onOff)
    {
      EvrEnableFrameSkipping(_allocatorKey, onOff);
    }

    /// <summary>
    /// Try to add filter by name to graph.
    /// </summary>
    /// <param name="codecInfo">Filter name to add</param>
    /// <returns>true if successful</returns>
    protected bool TryAdd(CodecInfo codecInfo)
    {
      if (codecInfo == null)
        return false;
      IBaseFilter tempFilter = FilterGraphTools.AddFilterByName(_graphBuilder, FilterCategory.LegacyAmFilterCategory, codecInfo.Name);
      return tempFilter != null;
    }

    /// <summary>
    /// Used to override requiredCapabilities by file extension.
    /// </summary>
    protected virtual void SetCapabilitiesByExtension()
    {
      if (_resourceAccessor == null) return;
      string ext = Path.GetExtension(_resourceAccessor.LocalFileSystemPath);
      if (ext.IndexOf(".mpg") >= 0 || ext.IndexOf(".ts") >= 0 || ext.IndexOf(".mpeg") >= 0)
        _requiredCapabilities = CodecHandler.CodecCapabilities.VideoH264 | CodecHandler.CodecCapabilities.VideoMPEG2 | CodecHandler.CodecCapabilities.AudioMPEG;
      else
        _requiredCapabilities = CodecHandler.CodecCapabilities.VideoDIVX | CodecHandler.CodecCapabilities.AudioMPEG;
    }

    /// <summary>
    /// Adds the file source filter to the graph.
    /// </summary>
    protected virtual void AddFileSource()
    {
      // Render the file
      int hr = _graphBuilder.RenderFile(_resourceAccessor.LocalFileSystemPath, null);
      DsError.ThrowExceptionForHR(hr);
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
      IAMPluginControl pc = null;
      try
      {
        pc = new DirectShowPluginControl() as IAMPluginControl;
        if (pc != null)
        {
          if (settings.Mpeg2Codec != null)
            pc.SetPreferredClsid(MediaSubType.Mpeg2Video, settings.Mpeg2Codec.GetCLSID());

          if (settings.H264Codec != null)
            pc.SetPreferredClsid(MediaSubType.H264, settings.H264Codec.GetCLSID());

          if (settings.AudioCodec != null)
          {
            foreach (Guid guid in new Guid[]
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

      // if IAMPluginControl is not supported.
      if (pc == null)
      {
        if ((_requiredCapabilities & CodecHandler.CodecCapabilities.VideoMPEG2) != 0)
          TryAdd(settings.Mpeg2Codec);

        if ((_requiredCapabilities & CodecHandler.CodecCapabilities.VideoH264) != 0)
          TryAdd(settings.H264Codec);

        if ((_requiredCapabilities & CodecHandler.CodecCapabilities.VideoDIVX) != 0)
          TryAdd(settings.DivXCodec);

        if ((_requiredCapabilities & CodecHandler.CodecCapabilities.AudioMPEG) != 0)
          TryAdd(settings.AudioCodec);
      }
    }

    #endregion

    #region Graph shutdown

    /// <summary>
    /// Frees the audio/video codecs.
    /// </summary>
    protected virtual void FreeCodecs()
    {
      FilterGraphTools.TryDispose(ref _streamInfoAudio);
      FilterGraphTools.TryDispose(ref _streamInfoSubtitles);

      FilterGraphTools.RemoveAllFilters(_graphBuilder);
      FilterGraphTools.TryRelease(ref _evr);

      if (_allocatorKey >= 0)
      {
        EvrDeinit(_allocatorKey);
        _allocatorKey = -1;
      }
      FilterGraphTools.TryDispose(ref _evrCallback);
      FilterGraphTools.TryDispose(ref _rot);
      FilterGraphTools.TryRelease(ref _graphBuilder);
    }

    protected void Shutdown()
    {
      StopSeeking();
      _initialized = false;
      lock (SyncObj)
      {
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

          FreeResources();
        }
      }
      // Dispose resource locator and accessor
      FilterGraphTools.TryDispose(ref _resourceAccessor);
      FilterGraphTools.TryDispose(ref _resourceLocator);
    }

    #endregion

    #region Resources Handling

    /// <summary>
    /// Allocates the vertex buffers
    /// </summary>
    protected void AllocateResources()
    {
      //Trace.WriteLine("{0}: Alloc vertex", PlayerTitle);
    }

    /// <summary>
    /// Frees the vertext buffers.
    /// </summary>
    protected void FreeResources()
    {
      ServiceRegistration.Get<ILogger>().Info("{0}: FreeResources", PlayerTitle);
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
    private static int VolumeToHundredthDeciBel(int volume)
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

    #region ISlimDXVideoPlayer implementation

    public virtual Guid PlayerId
    {
      get { return new Guid(PLAYER_ID_STR); }
    }

    public virtual string Name
    {
      get { return "Video"; }
    }

    public Size VideoSize
    {
      get { return (_evrCallback == null || !_initialized) ? new Size(0, 0) : _evrCallback.OriginalVideoSize; }
    }

    public Size VideoAspectRatio
    {
      get { return (_evrCallback == null) ? new Size(1, 1) : _evrCallback.AspectRatio; }
    }

    public SizeF SurfaceMaxUV
    {
      get { return (_evrCallback == null) ? new SizeF(1.0f, 1.0f) : _evrCallback.SurfaceMaxUV; }
    }

    public IGeometry GeometryOverride
    {
      get { return _geometryOverride; }
      set { _geometryOverride = value; }
    }

    public string EffectOverride
    {
      get { return _effectOverride; }
      set { _effectOverride = value; }
    }

    public CropSettings CropSettings
    {
      get { return _cropSettings; }
      set { _cropSettings = value; }
    }

    public virtual TimeSpan CurrentTime
    {
      get
      {
        lock (SyncObj)
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
      }
      set
      {
        ServiceRegistration.Get<ILogger>().Debug("{0}: Seek to {1} seconds", PlayerTitle, value.TotalSeconds);

        if (_state != PlayerState.Active)
          // If the player isn't active when setting its position, we will switch to pause mode to prevent the
          // player from run.
          Pause();
        lock (_graphBuilder)
        {
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

          int hr = mediaSeeking.SetPositions(seekPos, AMSeekingSeekingFlags.AbsolutePositioning, stopPos,
              AMSeekingSeekingFlags.NoPositioning);
        }
      }
    }

    public virtual TimeSpan Duration
    {
      get
      {
        lock (SyncObj)
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
      if (_state != PlayerState.Stopped)
      {
        ServiceRegistration.Get<ILogger>().Debug("{0}: Stop", PlayerTitle);
        // FIXME
        //        ResetRefreshRate();
        // TODO: WriteResumeData();
        StopSeeking();
        _isPaused = false;
        Shutdown();
        FireStopped();
      }
    }

    public void Pause()
    {
      if (!_isPaused)
      {
        ServiceRegistration.Get<ILogger>().Debug("{0}: Pause", PlayerTitle);
        IMediaControl mc = _graphBuilder as IMediaControl;
        if (mc != null)
          mc.Pause();
        StopSeeking();
        _isPaused = true;
        _state = PlayerState.Active;
        FirePlaybackStateChanged();
      }
    }

    public void Resume()
    {
      if (_isPaused || IsSeeking)
      {
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

    public void SetMediaItemTitleHint(string title)
    {
      // We don't extract the title by ourselves, we just use the title hint
      _mediaItemTitle = title;
    }

    public string MediaItemTitle
    {
      get { return _mediaItemTitle; }
    }

    #region audio streams


    private void SetPreferredAudio()
    {
      VideoSettings settings = ServiceRegistration.Get<ISettingsManager>().Load<VideoSettings>();
      if (_streamInfoAudio == null)
        EnumerateStreams();

      // First try to find a stream by it's exact LCID...
      StreamInfo streamInfo = _streamInfoAudio.FindStream(settings.PreferredAudioLanguage);
      if (streamInfo == null && settings.PreferredAudioLanguage != 0)
      {
        // ... then try to find a stream by it's name part.
        CultureInfo ci = new CultureInfo(settings.PreferredAudioLanguage);
        string languagePart = ci.EnglishName.Substring(0, ci.EnglishName.IndexOf("(") - 1);
        streamInfo = _streamInfoAudio.FindSimilarStream(languagePart);
      }
      if (streamInfo != null)
        _streamInfoAudio.EnableStream(streamInfo.Name);
    }


    /// <summary>
    /// Sets the current audio stream.
    /// </summary>
    /// <param name="audioStream">audio stream</param>
    public virtual void SetAudioStream(string audioStream)
    {
      lock (SyncObj)
      {
        if (_streamInfoAudio != null && _streamInfoAudio.EnableStream(audioStream))
        {
          VideoSettings settings = ServiceRegistration.Get<ISettingsManager>().Load<VideoSettings>() ?? new VideoSettings();
          int lcid = _streamInfoAudio.CurrentStream.LCID;
          if (lcid != 0)
          {
            settings.PreferredAudioLanguage = lcid;
            ServiceRegistration.Get<ISettingsManager>().Save(settings);
          }
        }
      }
    }

    /// <summary>
    /// Gets the current audio stream.
    /// </summary>
    /// <value>The current audio stream.</value>
    public virtual string CurrentAudioStream
    {
      get
      {
        lock (SyncObj)
        {
          return _streamInfoAudio != null ? _streamInfoAudio.CurrentStreamName : String.Empty;
        }
      }
    }

    /// <summary>
    /// Returns list of available audio streams.
    /// </summary>
    public virtual string[] AudioStreams
    {
      get
      {
        lock (SyncObj)
        {
          if (_streamInfoAudio == null)
            EnumerateStreams();

          return _streamInfoAudio.Count == 0 ? DEFAULT_AUDIO_STREAM_NAMES : _streamInfoAudio.GetStreamNames();
        }
      }
    }

    private void EnumerateStreams()
    {
      _streamInfoAudio = new StreamInfoHandler();
      _streamInfoSubtitles = new StreamInfoHandler();
      if (_graphBuilder == null)
        return;

      foreach (IAMStreamSelect streamSelector in FilterGraphTools.FindFiltersByInterface<IAMStreamSelect>(_graphBuilder))
      {
        FilterInfo fi;
        ((IBaseFilter) streamSelector).QueryFilterInfo(out fi);

        int streamCount;
        streamSelector.Count(out streamCount);

        for (int i = 0; i < streamCount; ++i)
        {
          AMMediaType mediaType;
          AMStreamSelectInfoFlags selectInfoFlags;
          int groupNumber, LCID;
          string name;
          object pppunk, ppobject;

          streamSelector.Info(i, out mediaType, out selectInfoFlags, out LCID, out groupNumber, out name,
              out pppunk, out ppobject);
          ServiceRegistration.Get<ILogger>().Debug("Stream {4}|{0}: MajorType {1}; Name {2}; PWDGroup: {3}; LCID: {5}", i,
              mediaType.majorType, name, groupNumber, fi.achName, LCID);

          StreamInfo currentStream = new StreamInfo(streamSelector, i, name, LCID);

          if (groupNumber == 0)
          {
            // video streams
          }
          if (groupNumber == 1)
          {
            if (mediaType.majorType == MediaType.AnalogAudio || mediaType.majorType == MediaType.Audio)
            {
              String streamName = name.Trim();
              String streamAppendix;
              if (MediaSubTypes.TryGetValue(mediaType.subType, out streamAppendix))
              {
                // if audio information is available via WaveEx format, query the channel count
                if (mediaType.formatType == FormatType.WaveEx && mediaType.formatPtr != IntPtr.Zero)
                {
                  WaveFormatEx waveFormatEx =
                    (WaveFormatEx) Marshal.PtrToStructure(mediaType.formatPtr, typeof (WaveFormatEx));
                  streamAppendix = String.Format("{0} {1}ch", streamAppendix, waveFormatEx.nChannels);
                }
                currentStream.Name = String.Format("{0} ({1})", streamName, streamAppendix);
              }
              _streamInfoAudio.AddUnique(currentStream);
            }
          }
          if (groupNumber == 2 || groupNumber == 6590033 /*DirectVobSub*/)
          {
            // subtitles
            _streamInfoSubtitles.AddUnique(currentStream, true);
          }
        }
      }
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

    public virtual void ReleaseGUIResources()
    {
      //stops the renderer threads all of it's own.
      //this could be split into two parts, but we would need
      //EvrSuspend(_allocatorKey) for that.
      lock (_evrCallback)
      {
        FreeResources();
        _evrCallback.ReleaseResources();
        IEnumPins enumer;
        _evr.EnumPins(out enumer);
        if (enumer != null)
        {
          IPin[] pins = new IPin[2];
          IntPtr ptrFetched = Marshal.AllocCoTaskMem(4);
          if (0 == enumer.Next(1, pins, ptrFetched))
          {
            if (Marshal.ReadInt32(ptrFetched) == 1)
            {
              if (pins[0] != null)
              {
                PinDirection pinDir;
                pins[0].QueryDirection(out pinDir);
                if (pinDir == PinDirection.Input)
                {
                  IPin pinConnect;
                  if (0 == pins[0].ConnectedTo(out pinConnect))
                  {
                    if (pinConnect != null)
                    {
                      _evrConnectionPins.Add(pinConnect);
                    }
                  }
                  Marshal.ReleaseComObject(pins[0]);
                }
              }
            }
          }
          Marshal.FreeCoTaskMem(ptrFetched);
          Marshal.ReleaseComObject(enumer);
        }

        FilterState state;
        IMediaControl mc = (IMediaControl) _graphBuilder;
        mc.GetState(10, out state);
        if (state != FilterState.Stopped)
        {
          mc.StopWhenReady();
          mc.Stop();
        }

        if (_evr != null)
          _graphBuilder.RemoveFilter(_evr);

        FilterGraphTools.TryDispose(ref _evrCallback);
        FilterGraphTools.TryRelease(ref _evr);

        if (_allocatorKey >= 0)
          EvrDeinit(_allocatorKey);

        _allocatorKey = -1;
        _initialized = false;
      }
    }

    public virtual void ReallocGUIResources()
    {
      if (_graphBuilder != null)
      {
        _evrCallback = new EVRCallback {CropSettings = _cropSettings};
        _evrCallback.VideoSizePresent += OnVideoSizePresent;
        AddEvr();
        IEnumPins enumer;
        _evr.EnumPins(out enumer);
        if (enumer != null)
        {
          IPin[] pins = new IPin[2];
          IntPtr ptrFetched = Marshal.AllocCoTaskMem(4);
          if (0 == enumer.Next(1, pins, ptrFetched))
          {
            if (Marshal.ReadInt32(ptrFetched) == 1)
            {
              if (pins[0] != null)
              {
                PinDirection pinDir;
                pins[0].QueryDirection(out pinDir);
                if (pinDir == PinDirection.Input)
                {
                  if (_evrConnectionPins.Count > 0)
                  {
                    _graphBuilder.Connect(_evrConnectionPins[0], pins[0]);
                    Marshal.ReleaseComObject(_evrConnectionPins[0]);
                    _evrConnectionPins.RemoveAt(0);
                  }
                  Marshal.ReleaseComObject(pins[0]);
                }
              }
            }
          }

          Marshal.FreeCoTaskMem(ptrFetched);
          Marshal.ReleaseComObject(enumer);
        }
        AllocateResources();
        _evrCallback.ReallocResources();

        if (State == PlayerState.Active)
        {
          IMediaControl mc = (IMediaControl) _graphBuilder;
          if (_isPaused)
            mc.Pause();
          else
            mc.Run();
        }
        _initialized = true;
      }
    }

    public Texture Texture 
    {
      get { return (_initialized && _evrCallback != null) ? _evrCallback.Texture : null; }
    } 

    #endregion

    #region ISubtitlePlayer Member

    private void SetPreferredSubtitle()
    {
      VideoSettings settings = ServiceRegistration.Get<ISettingsManager>().Load<VideoSettings>() ?? new VideoSettings();
      if (_streamInfoSubtitles == null)
        EnumerateStreams();

      // first try to find a stream by it's exact LCID.
      StreamInfo streamInfo = _streamInfoSubtitles.FindStream(settings.PreferredSubtitleLanguage) ??
          _streamInfoSubtitles.FindSimilarStream(settings.PreferredSubtitleSteamName);

      if (streamInfo == null || !settings.EnableSubtitles)
        _streamInfoSubtitles.EnableStream(NO_SUBTITLES);
      else
        _streamInfoSubtitles.EnableStream(streamInfo.Name);
    }

    /// <summary>
    /// Returns list of available subtitle streams.
    /// </summary>
    public virtual string[] Subtitles
    {
      get
      {
        lock (SyncObj)
        {
          if (_streamInfoSubtitles == null)
            EnumerateStreams();

          if (_streamInfoSubtitles == null)
            return EMPTY_STRING_ARRAY;
          // Check if there are real subtitle streams available. If not, the splitter only offers "No subtitles".
          string[] subtitleStreamNames = _streamInfoSubtitles.GetStreamNames();
          if (subtitleStreamNames.Length == 1 && subtitleStreamNames[0] == NO_SUBTITLES)
            return EMPTY_STRING_ARRAY;
          return subtitleStreamNames;
        }
      }
    }

    /// <summary>
    /// Sets the current subtitle stream.
    /// </summary>
    /// <param name="subtitle">subtitle stream</param>
    public virtual void SetSubtitle(string subtitle)
    {
      lock (SyncObj)
      {
        if (_streamInfoSubtitles != null && _streamInfoSubtitles.EnableStream(subtitle))
        {
          VideoSettings settings = ServiceRegistration.Get<ISettingsManager>().Load<VideoSettings>() ?? new VideoSettings();
          settings.PreferredSubtitleSteamName = _streamInfoSubtitles.CurrentStreamName;
          // if the subtitle stream has proper LCID, remember it.
          int lcid = _streamInfoAudio.CurrentStream.LCID;
          if (lcid != 0)
            settings.PreferredAudioLanguage = lcid;

          // if selected stream is "No subtitles", we disable the setting
          settings.EnableSubtitles = _streamInfoSubtitles.CurrentStreamName != NO_SUBTITLES;
          ServiceRegistration.Get<ISettingsManager>().Save(settings);
        }
      }
    }

    public virtual void DisableSubtitle()
    {
    }

    /// <summary>
    /// Gets the current subtitle stream name.
    /// </summary>
    public virtual string CurrentSubtitle
    {
      get
      {
        lock (SyncObj)
        {
          return _streamInfoSubtitles != null ? _streamInfoSubtitles.CurrentStreamName : String.Empty;
        }
      }
    }

    #endregion
  }
}
