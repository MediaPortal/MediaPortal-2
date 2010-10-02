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
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with MediaPortal 2.  If not, see <http://www.gnu.org/licenses/>.
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
using MediaPortal.Core.MediaManagement.ResourceAccess;
using MediaPortal.Core.Messaging;
using MediaPortal.Core.Settings;
using MediaPortal.Core.Logging;
using MediaPortal.UI.General;
using MediaPortal.UI.Presentation.Geometries;
using MediaPortal.UI.Presentation.Players;
using MediaPortal.UI.SkinEngine.ContentManagement;
using MediaPortal.UI.SkinEngine.Players;
using MediaPortal.Utilities.Exceptions;
using SlimDX.Direct3D9;
using MediaPortal.UI.SkinEngine.DirectX;
using MediaPortal.UI.SkinEngine.SkinManagement;
using SlimDX;
using Ui.Players.Video.Interfaces;

namespace Ui.Players.Video
{
  public class VideoPlayer : ISlimDXVideoPlayer, IDisposable, IPlayerEvents, IInitializablePlayer, IMediaPlaybackControl
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

    protected const double PLAYBACK_RATE_PLAY_THRESHOLD = 0.05;
    protected String PlayerTitle;

    #region Protected Properties

    /// <summary>
    /// MediaSubTypes lookup list.
    /// </summary>
    protected Dictionary<Guid, String> MediaSubTypes = new Dictionary<Guid, string>();

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

    // Filter graph related 
    protected CodecHandler.CodecCapabilities _graphCapabilities; // Currently to graph added capabilities
    protected CodecHandler.CodecCapabilities _requiredCapabilities; // Required capabilities to playback

    // Audio related
    protected int _currentAudioStream = 0;

    protected IGeometry _geometryOverride = null;

    protected PlayerState _state;
    protected bool _isPaused = false;
    protected int _volume = 100;
    protected bool _isMuted = false;
    protected bool _initialized = false;
    protected readonly List<IPin> _evrConnectionPins = new List<IPin>();
    protected IResourceLocator _resourceLocator;
    protected ILocalFsResourceAccessor _resourceAccessor;
    protected string _mediaItemTitle = null;
    protected PlayerEventDlgt _started = null;
    protected PlayerEventDlgt _stateReady = null;
    protected PlayerEventDlgt _stopped = null;
    protected PlayerEventDlgt _ended = null;
    protected PlayerEventDlgt _playbackStateChanged = null;
    protected PlayerEventDlgt _playbackError = null;
    protected AsynchronousMessageQueue _messageQueue = null;

    #endregion

    #region Ctor & dtor

    public VideoPlayer()
    {
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
      if (_resourceAccessor != null)
        _resourceAccessor.Dispose();
      _resourceAccessor = null;
      UnsubscribeFromMessages();
    }

    void InitMediaSubTypes()
    {
      MediaSubTypes[new Guid("00000130-0000-0010-8000-00AA00389B71")] = "ACELPnet"; //WMMEDIASUBTYPE_ACELPnet	
      MediaSubTypes[new Guid("00000000-0000-0010-8000-00AA00389B71")] = "Base"; //WMMEDIASUBTYPE_Base
      MediaSubTypes[new Guid("00000009-0000-0010-8000-00AA00389B71")] = "DRM"; //WMMEDIASUBTYPE_DRM
      MediaSubTypes[new Guid("00000055-0000-0010-8000-00AA00389B71")] = "MP3"; //WMMEDIASUBTYPE_MP3
      MediaSubTypes[new Guid("3334504D-0000-0010-8000-00AA00389B71")] = "MP43"; //WMMEDIASUBTYPE_MP43
      MediaSubTypes[new Guid("5334504D-0000-0010-8000-00AA00389B71")] = "MP4S"; //WMMEDIASUBTYPE_MP4S
      MediaSubTypes[new Guid("3253344D-0000-0010-8000-00AA00389B71")] = "M4S2"; //WMMEDIASUBTYPE_M4S2
      MediaSubTypes[new Guid("32323450-0000-0010-8000-00AA00389B71")] = "P422"; //WMMEDIASUBTYPE_P422
      MediaSubTypes[new Guid("e06d8026-db46-11cf-b4d1-00805f6cbbea")] = "MPEG2"; //WMMEDIASUBTYPE_MPEG2_VIDEO
      MediaSubTypes[new Guid("3153534D-0000-0010-8000-00AA00389B71")] = "MSS1"; //WMMEDIASUBTYPE_MSS1
      MediaSubTypes[new Guid("3253534D-0000-0010-8000-00AA00389B71")] = "MSS2"; //WMMEDIASUBTYPE_MSS2
      MediaSubTypes[new Guid("00000001-0000-0010-8000-00AA00389B71")] = "PCM"; //WMMEDIASUBTYPE_PCM
      MediaSubTypes[new Guid("776257d4-c627-41cb-8f81-7ac7ff1c40cc")] = "WebStream"; //WMMEDIASUBTYPE_WebStream
      MediaSubTypes[new Guid("00000163-0000-0010-8000-00AA00389B71")] = "WMA Lossless"; //WMMEDIASUBTYPE_WMAudio_Lossless
      MediaSubTypes[new Guid("00000161-0000-0010-8000-00AA00389B71")] = "WMA v2"; //WMMEDIASUBTYPE_WMAudioV2
      MediaSubTypes[new Guid("00000161-0000-0010-8000-00AA00389B71")] = "WMA v7"; //WMMEDIASUBTYPE_WMAudioV7
      MediaSubTypes[new Guid("00000161-0000-0010-8000-00AA00389B71")] = "WMA v8"; //WMMEDIASUBTYPE_WMAudioV8
      MediaSubTypes[new Guid("00000162-0000-0010-8000-00AA00389B71")] = "WMA v9"; //WMMEDIASUBTYPE_WMAudioV9
      MediaSubTypes[new Guid("0000000A-0000-0010-8000-00AA00389B71")] = "WMSP1"; //WMMEDIASUBTYPE_WMSP1
      MediaSubTypes[new Guid("31564D57-0000-0010-8000-00AA00389B71")] = "WMV1"; //WMMEDIASUBTYPE_WMV1
      MediaSubTypes[new Guid("32564D57-0000-0010-8000-00AA00389B71")] = "WMV2"; //WMMEDIASUBTYPE_WMV2
      MediaSubTypes[new Guid("33564D57-0000-0010-8000-00AA00389B71")] = "WMV3"; //WMMEDIASUBTYPE_WMV3
      MediaSubTypes[new Guid("41564D57-0000-0010-8000-00AA00389B71")] = "WMVA"; //WMMEDIASUBTYPE_WMVA
      MediaSubTypes[new Guid("50564D57-0000-0010-8000-00AA00389B71")] = "WMVP"; //WMMEDIASUBTYPE_WMVP
      MediaSubTypes[new Guid("32505657-0000-0010-8000-00AA00389B71")] = "WVP2"; //WMMEDIASUBTYPE_WVP2
      MediaSubTypes[new Guid("e06d802c-db46-11cf-b4d1-00805f6cbbea")] = "AC3"; //MEDIASUBTYPE_AC3_AUDIO
      MediaSubTypes[new Guid("00002000-0000-0010-8000-00aa00389b71")] = "AC3"; //MEDIASUBTYPE_ ???
      MediaSubTypes[new Guid("a7fb87af-2d02-42fb-a4d4-05cd93843bdd")] = "AC3+"; //MEDIASUBTYPE_DDPLUS_AUDIO
      MediaSubTypes[new Guid("e436eb81-524f-11ce-9f53-0020af0ba770")] = "MPEG1"; //MEDIASUBTYPE_MPEG1_PAYLOAD
      MediaSubTypes[new Guid("e436eb87-524f-11ce-9f53-0020af0ba770")] = "MPEG1"; //MEDIASUBTYPE_MPEG1_AUDIO
      MediaSubTypes[new Guid("e06d802b-db46-11cf-b4d1-00805f6cbbea")] = "MPEG2"; //MEDIASUBTYPE_MPEG2_AUDIO
      MediaSubTypes[new Guid("000001ff-0000-0010-8000-00aa00389b71")] = "LATM AAC"; //MEDIASUBTYPE_LATM_AAC_AUDIO
      MediaSubTypes[new Guid("000000ff-0000-0010-8000-00aa00389b71")] = "AAC"; //MEDIASUBTYPE_AAC_AUDIO
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
        Message m = (Message)message.MessageData[WindowsMessaging.MESSAGE];
        if (m.LParam.Equals(_instancePtr))
        {
          if (m.Msg == WM_GRAPHNOTIFY)
          {
            IMediaEventEx eventEx = (IMediaEventEx)_graphBuilder;

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
        _evrCallback = new EVRCallback(this);
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
        IMediaControl mc = (IMediaControl)_graphBuilder;
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
    protected virtual void OnGraphRunning() { }

    #endregion

    #region Graph building

    /// <summary>
    /// Creates a new IFilterGraph2 interface.
    /// </summary>
    protected virtual void CreateGraphBuilder()
    {
      _graphBuilder = (IFilterGraph2)new FilterGraph();
    }

    /// <summary>
    /// Adds the EVR to graph.
    /// </summary>
    protected virtual void AddEvr()
    {
      ServiceRegistration.Get<ILogger>().Debug("{0}: Initialize EVR", PlayerTitle);

      _evr = (IBaseFilter)new EnhancedVideoRenderer();

      IEVRFilterConfig config = (IEVRFilterConfig)_evr;
      
      //set the number of video/subtitle/cc streams that are allowed to be connected to EVR
      config.SetNumberOfStreams(_streamCount);

      int ordinal = GraphicsDevice.Device.Capabilities.AdapterOrdinal;
      AdapterInformation ai = MPDirect3D.Direct3D.Adapters[ordinal];
      IntPtr hMonitor = MPDirect3D.Direct3D.GetAdapterMonitor(ai.Adapter);
      IntPtr upDevice = GraphicsDevice.Device.ComPointer;
      _allocatorKey = EvrInit(_evrCallback, (uint)upDevice.ToInt32(), _evr, (uint)hMonitor.ToInt32());
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
    /// If it succeedes it gets added to _filtersList.
    /// </summary>
    /// <param name="codecName">Filter name to add</param>
    /// <returns>true if successful</returns>
    protected bool TryAdd(String codecName)
    {
      IBaseFilter tempFilter = FilterGraphTools.AddFilterByName(_graphBuilder, FilterCategory.LegacyAmFilterCategory, codecName);
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
      {
        _requiredCapabilities = CodecHandler.CodecCapabilities.VideoH264 | CodecHandler.CodecCapabilities.VideoMPEG2 | CodecHandler.CodecCapabilities.AudioMPEG;
      }
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
      // Init capabilities
      _graphCapabilities = CodecHandler.CodecCapabilities.None;

      CodecHandler codecHandler = new CodecHandler();
      VideoSettings settings = ServiceRegistration.Get<ISettingsManager>().Load<VideoSettings>();

      // set default codecs
      if (codecHandler.Supports(_requiredCapabilities, CodecHandler.CodecCapabilities.VideoH264) && !string.IsNullOrEmpty(settings.H264Codec))
        codecHandler.SetPreferred(settings.H264Codec, CodecHandler.CodecCapabilities.VideoH264);

      if (codecHandler.Supports(_requiredCapabilities, CodecHandler.CodecCapabilities.VideoMPEG2) && !string.IsNullOrEmpty(settings.Mpeg2Codec))
        codecHandler.SetPreferred(settings.Mpeg2Codec, CodecHandler.CodecCapabilities.VideoMPEG2);

      if (codecHandler.Supports(_requiredCapabilities, CodecHandler.CodecCapabilities.VideoDIVX) && !string.IsNullOrEmpty(settings.DivXCodec))
        codecHandler.SetPreferred(settings.DivXCodec, CodecHandler.CodecCapabilities.VideoDIVX);

      if (codecHandler.Supports(_requiredCapabilities, CodecHandler.CodecCapabilities.AudioMPEG) && !string.IsNullOrEmpty(settings.AudioCodec))
        codecHandler.SetPreferred(settings.AudioCodec, CodecHandler.CodecCapabilities.AudioMPEG);

      // Lookup all known codecs and add them to graph
      foreach (CodecInfo currentCodec in codecHandler.CodecList)
      {
        // Check H264 codec
        if (codecHandler.Supports(_requiredCapabilities, CodecHandler.CodecCapabilities.VideoH264))
          AddFilterByCapability(codecHandler, currentCodec, CodecHandler.CodecCapabilities.VideoH264);

        // Check MPEG2 codec
        if (codecHandler.Supports(_requiredCapabilities, CodecHandler.CodecCapabilities.VideoMPEG2))
          AddFilterByCapability(codecHandler, currentCodec, CodecHandler.CodecCapabilities.VideoMPEG2);

        // Check Audio codec
        if (codecHandler.Supports(_requiredCapabilities, CodecHandler.CodecCapabilities.AudioMPEG))
          AddFilterByCapability(codecHandler, currentCodec, CodecHandler.CodecCapabilities.AudioMPEG);

        // Exit if all needed capabilities exist
        if (codecHandler.Supports(_graphCapabilities, _requiredCapabilities))
          break;
      }
    }

    /// <summary>
    /// Adds a codec to graph for the requested capability.
    /// </summary>
    /// <param name="codecHandler">Current codec handler.</param>
    /// <param name="currentCodec">Current codec to check.</param>
    /// <param name="requestedCapability">The capability the codec has to support.</param>
    protected void AddFilterByCapability(CodecHandler codecHandler, CodecInfo currentCodec, CodecHandler.CodecCapabilities requestedCapability)
    {
      if (!codecHandler.Supports(_graphCapabilities, requestedCapability) &&
           codecHandler.Supports(currentCodec.Capabilities, requestedCapability))
      {
        if (TryAdd(currentCodec.Name))
        {
          _graphCapabilities |= currentCodec.Capabilities; // remember all capabilities
          ServiceRegistration.Get<ILogger>().Debug("{0}: Add {1} Codec {2} ", PlayerTitle, requestedCapability.ToString(), currentCodec.Name);
        }
      }
    }

    #endregion

    #region Graph shutdown

    /// <summary>
    /// Frees the audio/video codecs.
    /// </summary>
    protected virtual void FreeCodecs()
    {
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
      lock (_resourceAccessor)
      {
        ServiceRegistration.Get<ILogger>().Debug("{0}: Stop playing", PlayerTitle);

        try
        {
          if (_graphBuilder != null)
          {
            FilterState state;
            IMediaEventEx me = (IMediaEventEx)_graphBuilder;
            IMediaControl mc = (IMediaControl)_graphBuilder;

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

          // Dispose the resource accessor; i.e. to stop the Tve3 provider's timeshifting
          FilterGraphTools.TryDispose(ref _resourceAccessor);
        }
      }
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
      get
      {
        if (_evrCallback == null || !_initialized) return new Size(0, 0);
        return _evrCallback.VideoSize;
      }
    }

    public Size VideoAspectRatio
    {
      get
      {
        if (_evrCallback == null || !_initialized) return new Size(0, 0);
        return _evrCallback.AspectRatio;
      }
    }

    public IGeometry GeometryOverride
    {
      get { return _geometryOverride; }
      set { _geometryOverride = value; }
    }

    public virtual void BeginRender(EffectAsset effect, Matrix finalTransform)
    {
      if (!_initialized) return;
      if (_evrCallback == null) return;
      effect.StartRender(_evrCallback.Texture, finalTransform);
    }

    public virtual void EndRender(EffectAsset effect)
    {
      if (!_initialized) return;
      if (_evrCallback == null) return;
      effect.EndRender();
    }

    public virtual TimeSpan CurrentTime
    {
      get
      {
        lock (_resourceAccessor)
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
        lock (_resourceAccessor)
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
      IMediaControl mc = _graphBuilder as IMediaControl;
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

    /// <summary>
    /// Enumerates the graph for a filter that support IAMStreamSelect interface.
    /// </summary>
    protected IAMStreamSelect StreamSelector
    {
      get
      {
        return FilterGraphTools.FindFilterByInterface<IAMStreamSelect>(_graphBuilder);
      }
    }

    /// <summary>
    /// Sets the current audio stream.
    /// </summary>
    /// <param name="audioStream">audio stream</param>
    public virtual void SetAudioStream(string audioStream)
    {
      string[] streams = AudioStreams;
      for (int i = 0; i < streams.Length; i++)
      {
        if (audioStream == streams[i])
        {
          _currentAudioStream = i;
          // StreamSelector expects stream number starting with 1
          if (StreamSelector != null)
            StreamSelector.Enable(i + 1, AMStreamSelectEnableFlags.Enable);
          break;
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
        string[] streams = AudioStreams;
        if (_currentAudioStream >= 0 && _currentAudioStream < streams.Length)
        {
          return streams[_currentAudioStream];
        }
        return "";
      }
    }

    /// <summary>
    /// returns list of available audio streams
    /// </summary>
    /// <value></value>
    public virtual string[] AudioStreams
    {
      get
      {
        int streamCount = 0;
        if (StreamSelector != null)
          StreamSelector.Count(out streamCount);

        List<String> streams = new List<String>();
        for (int i = 0; i < streamCount; ++i)
        {
          AMMediaType sType; AMStreamSelectInfoFlags sFlag;
          int sPDWGroup, sPLCid; 
          string sName;
          object pppunk, ppobject;

          StreamSelector.Info(i, out sType, out sFlag, out sPLCid, out sPDWGroup, out sName, out pppunk, out ppobject);

          if (sType.majorType == MediaType.AnalogAudio || sType.majorType == MediaType.Audio)
          {
            String streamName = sName.Trim();
            String streamAppendix;
            if (MediaSubTypes.TryGetValue(sType.subType, out streamAppendix))
            {
              // if audio information is available via WaveEx format, query the channel count
              if (sType.formatType == FormatType.WaveEx && sType.formatPtr != IntPtr.Zero)
              {
                WaveFormatEx waveFormatEx = (WaveFormatEx)Marshal.PtrToStructure(sType.formatPtr, typeof(WaveFormatEx));
                streamAppendix = String.Format("{0} {1}ch", streamAppendix, waveFormatEx.nChannels);
              }
              streamName = String.Format("{0} ({1})", streamName, streamAppendix);
            }
            streams.Add(streamName);
          }
        }
        return streams.ToArray();
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
                      //Marshal.ReleaseComObject(pinConnect);
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
        IMediaControl mc = (IMediaControl)_graphBuilder;
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
        _evrCallback = new EVRCallback(this);
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
        _evrCallback.VideoSizePresent += OnVideoSizePresent;
        _initialized = true;
      }
    }

    #endregion
  }
}
