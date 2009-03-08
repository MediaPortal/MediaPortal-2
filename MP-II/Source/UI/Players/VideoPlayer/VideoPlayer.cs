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
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Security;
using System.Windows.Forms;
using DirectShowLib;
using MediaPortal.Core;
using MediaPortal.Core.MediaManagement;
using MediaPortal.Core.Messaging;
using MediaPortal.Core.Settings;
using MediaPortal.Core.Logging;
using MediaPortal.General;
using MediaPortal.Presentation.Players;
using MediaPortal.SkinEngine;
using MediaPortal.SkinEngine.ContentManagement;
using MediaPortal.SkinEngine.Players;
using SlimDX.Direct3D9;
using MediaPortal.SkinEngine.DirectX;
using MediaPortal.SkinEngine.Effects;
using Ui.Players.Video.Vmr9;
using MediaPortal.SkinEngine.SkinManagement;

namespace Ui.Players.Video
{
  public class VideoPlayer : ISlimDXVideoPlayer, IDisposable, IPlayerEvents, IInitializablePlayer
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

    [DllImport("vmr9Helper.dll", ExactSpelling = true, CharSet = CharSet.Auto, SetLastError = true)]
    private static extern unsafe int Vmr9Init(IVMR9PresentCallback callback, uint dwD3DDevice,
        IBaseFilter vmr9Filter, uint monitor);

    [DllImport("vmr9Helper.dll", ExactSpelling = true, CharSet = CharSet.Auto, SetLastError = true)]
    private static extern unsafe void Vmr9DeInit(int handle);

    [DllImport("vmr9Helper.dll", ExactSpelling = true, CharSet = CharSet.Auto, SetLastError = true)]
    private static extern unsafe int EvrInit(IVMR9PresentCallback callback, uint dwD3DDevice,
        IBaseFilter vmr9Filter, uint monitor);

    [DllImport("vmr9Helper.dll", ExactSpelling = true, CharSet = CharSet.Auto, SetLastError = true)]
    private static extern unsafe void Vmr9FreeResources(int handle);

    [DllImport("vmr9Helper.dll", ExactSpelling = true, CharSet = CharSet.Auto, SetLastError = true)]
    private static extern unsafe void Vmr9ReAllocResources(int handle);

    [DllImport("vmr9Helper.dll", ExactSpelling = true, CharSet = CharSet.Auto, SetLastError = true)]
    private static extern unsafe void EvrDeinit(int handle);

    [DllImport("vmr9Helper.dll", ExactSpelling = true, CharSet = CharSet.Auto, SetLastError = true)]
    private static extern unsafe void EvrEnableFrameSkipping(int handle, bool onOff);

    [DllImport("vmr9Helper.dll", ExactSpelling = true, CharSet = CharSet.Auto, SetLastError = true)]
    private static extern unsafe void EvrFreeResources(int handle);

    [DllImport("vmr9Helper.dll", ExactSpelling = true, CharSet = CharSet.Auto, SetLastError = true)]
    private static extern unsafe void EvrReAllocResources(int handle);

    #endregion

    #region Consts

    protected const int WM_GRAPHNOTIFY = 0x4000 + 123;

    public const string PLAYER_ID_STR = "9EF8D975-575A-4c64-AA54-500C97745969";

    #endregion

    #region variables

    private enum EvrResult
    {
      NotTried,
      Failed,
      Ok
    }

    // DirectShow objects
    protected IGraphBuilder _graphBuilder;
    protected DsROTEntry _rot;
    protected IBaseFilter _vmr9;
    protected Allocator _allocator;

    // Managed Direct3D Resources
    protected VertexBuffer _vertexBuffer = null;
    protected PositionColored2Textured[] _vertices;
    protected Size _displaySize = new Size(100, 100);

    protected IntPtr _instancePtr;
    protected int _allocatorKey = -1;

    protected Size _previousTextureSize;
    protected Size _previousVideoSize;
    protected Size _previousAspectRatio;
    protected Size _previousDisplaySize;
    protected EffectAsset _effect;
    protected bool _useEvr = true;
    protected uint _streamCount = 1;

    protected IBaseFilter _videoh264Codec;
    protected IBaseFilter _videoCodec;
    protected IBaseFilter _audioCodec;
    protected Rectangle _destinationRect;
    private TimeSpan _duration;
    private TimeSpan _currentTime;
    private static EvrResult _evrResult = EvrResult.NotTried;
    protected PlaybackState _state;
    protected int _volume = 100;
    protected bool _isMuted = false;
    protected bool _initialized = false;
    List<IPin> _vmr9ConnectionPins = new List<IPin>();
    protected string _resumeFile;
    protected IMediaItemLocator _mediaItemLocator;
    protected IMediaItemLocalFsAccessor _mediaItemAccessor;
    protected int _playerSlot = -1;
    protected PlayerEventDlgt _started;
    protected PlayerEventDlgt _stopped;
    protected PlayerEventDlgt _ended;
    protected PlayerEventDlgt _paused;
    protected PlayerEventDlgt _resumed;


    #endregion

    #region Ctor & dtor

    public VideoPlayer()
    {
      SubscribeWindowsMessages();
    }

    public void Dispose()
    {
      if (_mediaItemAccessor != null)
        _mediaItemAccessor.Dispose();
      _mediaItemAccessor = null;
      UnsubscribeWindowsMessages();
    }

    #endregion

    protected void SubscribeWindowsMessages()
    {
      ServiceScope.Get<IMessageBroker>().GetOrCreate(WindowsMessaging.QUEUE).MessageReceived += OnWindowsMessageReceived;
    }

    protected void UnsubscribeWindowsMessages()
    {
      ServiceScope.Get<IMessageBroker>().GetOrCreate(WindowsMessaging.QUEUE).MessageReceived -= OnWindowsMessageReceived;
    }

    protected virtual void OnWindowsMessageReceived(QueueMessage message)
    {
      Message m = (Message) message.MessageData[WindowsMessaging.MESSAGE];
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
              _state = PlaybackState.Ended;
              ServiceScope.Get<ILogger>().Debug("VideoPlayer:Playback ended");
              // TODO: RemoveResumeData();
              FireEnded();
              return;
            }
          }
        }
      }
    }

    #region IInitializablePlayer implementation

    public void SetMediaItemLocator(IMediaItemLocator locator)
    {
      _mediaItemLocator = locator;
      _mediaItemAccessor = _mediaItemLocator.CreateLocalFsAccessor();
      _state = PlaybackState.Paused;
      _vertices = new PositionColored2Textured[4];
      ServiceScope.Get<ILogger>().Debug("VideoPlayer: Playing '{0}'", _mediaItemAccessor.LocalFileSystemPath);

      int hr = 0;

      // FIXME: When to call this?
      //SetRefreshRate(mediaItem);

      AllocateResources();

      // Create a DirectShow FilterGraph
      GetGraphBuilder();

      // Add it in ROT for debug purpose
      _rot = new DsROTEntry(_graphBuilder);

      // Add a notification handler (see WndProc)
      _instancePtr = Marshal.AllocCoTaskMem(4);
      IMediaEventEx mee = _graphBuilder as IMediaEventEx;
      hr = mee.SetNotifyWindow(SkinContext.Form.Handle, WM_GRAPHNOTIFY, _instancePtr);

      //only use EVR if we're running Vista
      OperatingSystem osInfo = Environment.OSVersion;
      if (osInfo.Version.Major <= 5 || _evrResult == EvrResult.Failed)
        _useEvr = false;

      // Create the Allocator / Presenter object
      _allocator = new Allocator(this, _useEvr);

      AddEvrOrVmr9();

      ServiceScope.Get<ILogger>().Debug("VideoPlayer add preferred codecs");
      AddPreferredCodecs();

      ServiceScope.Get<ILogger>().Debug("VideoPlayer add file source");
      // Here an exception might be raised - TODO: Handle that exception, tidy up already allocated resources
      AddFileSource();

      ServiceScope.Get<ILogger>().Debug("VideoPlayer run graph");

      ///This needs to be done here before we check if the evr pins are connected
      ///since this method gives players the chance to render the last bits of the graph
      OnBeforeGraphRunning();

      //check if EVR is connected!
      if (_useEvr)
      {
        bool success = false;
        IEnumPins enumer;
        _vmr9.EnumPins(out enumer);
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
                IPin pinConnect;
                if (0 == pins[0].ConnectedTo(out pinConnect))
                {
                  if (pinConnect != null)
                  {
                    success = true;
                    Marshal.ReleaseComObject(pinConnect);
                  }
                }
                Marshal.ReleaseComObject(pins[0]);
              }
            }
          }
          Marshal.FreeCoTaskMem(ptrFetched);
          Marshal.ReleaseComObject(enumer);
        }
        if (!success)
        {
          Shutdown();
          _useEvr = false;
          _evrResult = EvrResult.Failed;
          SetMediaItemLocator(locator);
          return;
        }
      }

      OnGraphRunning();
      _initialized = true;
    }

    #endregion

    #region IPlayerEvents implementation

    public void InitializePlayerEvents(int playerSlot, PlayerEventDlgt started, PlayerEventDlgt stopped,
        PlayerEventDlgt ended, PlayerEventDlgt paused, PlayerEventDlgt resumed)
    {
      _playerSlot = playerSlot;
      _started = started;
      _stopped = stopped;
      _ended = ended;
      _paused = paused;
      _resumed = resumed;
    }

    public void ResetPlayerEvents()
    {
      _playerSlot = -1;
      _started = null;
      _stopped = null;
      _ended = null;
      _paused = null;
      _resumed = null;
    }

    #endregion

    protected void FireStarted()
    {
      if (_started != null)
        _started(this, _playerSlot);
    }

    protected void FireStopped()
    {
      if (_stopped != null)
        _stopped(this, _playerSlot);
    }

    protected void FireEnded()
    {
      if (_ended != null)
        _ended(this, _playerSlot);
    }

    protected void FirePaused()
    {
      if (_paused != null)
        _paused(this, _playerSlot);
    }

    protected void FireResumed()
    {
      if (_resumed != null)
        _resumed(this, _playerSlot);
    }

    public virtual Guid PlayerId
    {
      get { return new Guid(PLAYER_ID_STR); }
    }

    public virtual string Name
    {
      get { return "VideoPlayer"; }

    }

    public EffectAsset Effect
    {
      get { return _effect; }
      set { _effect = value; }
    }

    public Size DisplaySize
    {
      get { return _displaySize; }
      set { _displaySize = value; }
    }

    public Size VideoSize
    {
      get
      {
        if (_allocator == null || !_initialized) return new Size(0, 0);
        return _allocator.VideoSize;
      }
    }

    public Size VideoAspectRatio
    {
      get
      {
        if (_allocator == null || !_initialized) return new Size(0, 0);
        return _allocator.AspectRatio;
      }
    }

    // FIXME Albert78: When should we change the refresh rate? In which interface should we put this method?
    //void SetRefreshRate(IMediaItem mediaItem)
    //{
    //  IPlayerCollection players = ServiceScope.Get<IPlayerCollection>();
    //  float fps;
    //  if (players.Count != 0)
    //    return;
    //  if (!mediaItem.MetaData.ContainsKey("FPS"))
    //    return;
    //  if (!float.TryParse((mediaItem.MetaData["FPS"] as string), System.Globalization.NumberStyles.Any, CultureInfo.InvariantCulture, out fps))
    //    return;
    //  if (fps > 1000.0f)
    //    fps /= 1000.0f;

    //  IScreenControl sc = ServiceScope.Get<IScreenControl>();

    //  if (sc.RefreshRateControlEnabled && sc.IsFullScreen)
    //  {
    //    if (fps == 24.0f)
    //      sc.SwitchMode(ScreenMode.ExclusiveMode, FPS.FPS_24);
    //    else if (fps == 25.0f)
    //      sc.SwitchMode(ScreenMode.ExclusiveMode, FPS.FPS_25);
    //    else if (fps == 30.0f)
    //      sc.SwitchMode(ScreenMode.ExclusiveMode, FPS.FPS_30);
    //    else
    //      sc.SwitchMode(ScreenMode.ExclusiveMode, FPS.Default);
    //  }
    //}


    // FIXME Albert78: When should we change the refresh rate? In which interface should we put this method?
    //void ResetRefreshRate()
    //{
    //  IPlayerCollection players = ServiceScope.Get<IPlayerCollection>();

    //  if (players[0] != this)
    //    return;

    //  IScreenControl sc = ServiceScope.Get<IScreenControl>();

    //  if (!(sc.RefreshRateControlEnabled && sc.IsFullScreen))
    //    return;
    //  sc.SwitchMode(ScreenMode.FullScreenWindowed, FPS.None);
    //}

    void AddEvrOrVmr9()
    {
      int hr;
      if (_useEvr)
      {
        ServiceScope.Get<ILogger>().Debug("VideoPlayer initialize EVR");

        _vmr9 = (IBaseFilter)new EnhancedVideoRenderer();

        IEVRFilterConfig config = (IEVRFilterConfig)_vmr9;
        hr = config.SetNumberOfStreams(_streamCount);

        IntPtr hMonitor;
        int ordinal = GraphicsDevice.Device.Capabilities.AdapterOrdinal;
        AdapterInformation ai = MPDirect3D.Direct3D.Adapters[ordinal];
        hMonitor = MPDirect3D.Direct3D.GetAdapterMonitor(ai.Adapter);
        IntPtr upDevice = GraphicsDevice.Device.ComPointer;//.GetObjectByValue(-759872593);
        _allocatorKey = EvrInit(_allocator, (uint)upDevice.ToInt32(), _vmr9, (uint) hMonitor.ToInt32());
        if (_allocatorKey >= 0)
        {
          hr = _graphBuilder.AddFilter(_vmr9, "Enhanced Video Renderer");
          _evrResult = EvrResult.Ok;
        }
        else
        {
          ServiceScope.Get<ILogger>().Debug("VideoPlayer initialize evr failed, fallback to vmr9");
          EvrDeinit(_allocatorKey);
          Marshal.ReleaseComObject(_vmr9);
          _allocator.Dispose();
          _allocator = null;
          _useEvr = false;
          _evrResult = EvrResult.Failed;
        }
      }

      if (!_useEvr)
      {
        ServiceScope.Get<ILogger>().Debug("VideoPlayer initialize vmr9");
        _vmr9 = (IBaseFilter)new VideoMixingRenderer9();
        IVMRFilterConfig9 filterConfig = (IVMRFilterConfig9)_vmr9;

        // We want the Renderless mode!
        hr = filterConfig.SetRenderingMode(VMR9Mode.Renderless);
        DsError.ThrowExceptionForHR(hr);

        // One stream is enough for this sample
        hr = filterConfig.SetNumberOfStreams((int)_streamCount);
        DsError.ThrowExceptionForHR(hr);

        // Create the Allocator / Presenter object
        _allocator = new Allocator(this, _useEvr);

        IntPtr hMonitor;
        int ordinal = GraphicsDevice.Device.Capabilities.AdapterOrdinal;
        AdapterInformation ai = MPDirect3D.Direct3D.Adapters[ordinal];
        hMonitor = MPDirect3D.Direct3D.GetAdapterMonitor(ai.Adapter);
        IntPtr upDevice = GraphicsDevice.Device.ComPointer;//.GetObjectByValue(-759872593);
        _allocatorKey = Vmr9Init(_allocator, (uint)upDevice.ToInt32(), _vmr9, (uint)hMonitor.ToInt32());

        IVMRMixerControl9 mixerControl = (IVMRMixerControl9)_vmr9;

        // Select the mixer mode : YUV or RGB
        //hr = mixerControl.SetMixingPrefs(VMR9MixerPrefs.None);
        hr =
          mixerControl.SetMixingPrefs(VMR9MixerPrefs.RenderTargetYUV | VMR9MixerPrefs.NonSquareMixing |
                                      VMR9MixerPrefs.AnisotropicFiltering);
        //hr = mixerControl.SetMixingPrefs(VMR9MixerPrefs.RenderTargetYUV | VMR9MixerPrefs.NoDecimation | VMR9MixerPrefs.ARAdjustXorY | VMR9MixerPrefs.BiLinearFiltering);
        //hr = mixerControl.SetMixingPrefs(VMR9MixerPrefs.RenderTargetRGB | VMR9MixerPrefs.NoDecimation | VMR9MixerPrefs.ARAdjustXorY | VMR9MixerPrefs.BiLinearFiltering);

        // Add the filter to the graph
        hr = _graphBuilder.AddFilter(_vmr9, "Video Mixing Renderer 9");
        DsError.ThrowExceptionForHR(hr);
      }
    }

    /// <summary>
    /// Enables/disables frame skipping.
    /// </summary>
    /// <param name="onOff">if set to <c>true</c> [on off].</param>
    protected void EnableFrameSkipping(bool onOff)
    {
      if (_useEvr)
      {
        EvrEnableFrameSkipping(_allocatorKey, onOff);
      }
    }

    /// <summary>
    /// Called just before starting the graph
    /// </summary>
    protected virtual void OnBeforeGraphRunning()
    {
    }

    /// <summary>
    /// called when graph is started
    /// </summary>
    protected virtual void OnGraphRunning() { }

    /// <summary>
    /// returns a new IFilterGraph2 interface
    /// </summary>
    protected virtual void GetGraphBuilder()
    {
      _graphBuilder = (IFilterGraph2) new FilterGraph();
    }

    /// <summary>
    /// adds prefferred audio/video codecs
    /// </summary>
    protected virtual void AddPreferredCodecs()
    {
      VideoSettings settings = ServiceScope.Get<ISettingsManager>().Load<VideoSettings>();
      string ext = Path.GetExtension(_mediaItemAccessor.LocalFileSystemPath);
      if (ext.IndexOf(".mpg") >= 0 || ext.IndexOf(".ts") >= 0 || ext.IndexOf(".mpeg") >= 0)
      {
        //_videoh264Codec = FilterGraphTools.AddFilterByName(_graphBuilder, FilterCategory.LegacyAmFilterCategory, "CoreAVC Video Decoder");

        if (settings.H264Codec != null && settings.H264Codec.Length > 0)
        {
          _videoh264Codec =
            FilterGraphTools.AddFilterByName(_graphBuilder, FilterCategory.LegacyAmFilterCategory,
                                             settings.H264Codec);
        }

        if (_videoh264Codec == null)
        {
          _videoh264Codec =
            FilterGraphTools.AddFilterByName(_graphBuilder, FilterCategory.LegacyAmFilterCategory,
                                             "CyberLink H.264/AVC Decoder (PDVD7.X)");
        }
        if (settings.Mpeg2Codec != null && settings.Mpeg2Codec.Length > 0)
        {
          _videoCodec =
            FilterGraphTools.AddFilterByName(_graphBuilder, FilterCategory.LegacyAmFilterCategory,
                                             settings.Mpeg2Codec);
        }

        if (_videoCodec == null)
        {
          _videoCodec =
            FilterGraphTools.AddFilterByName(_graphBuilder, FilterCategory.LegacyAmFilterCategory,
                                             "CyberLink Video/SP Decoder");
        }
        if (_videoCodec == null)
        {
          _videoCodec =
            FilterGraphTools.AddFilterByName(_graphBuilder, FilterCategory.LegacyAmFilterCategory,
                                             "Microsoft MPEG-2 Video Decoder");
        }
        if (_videoCodec == null)
        {
          _videoCodec =
            FilterGraphTools.AddFilterByName(_graphBuilder, FilterCategory.LegacyAmFilterCategory,
                                             "NVIDIA Video Decoder");
        }
        if (_videoCodec == null)
        {
          _videoCodec =
            FilterGraphTools.AddFilterByName(_graphBuilder, FilterCategory.LegacyAmFilterCategory, "MPV Decoder Filter");
        }

        if (settings.AudioCodec != null && settings.AudioCodec.Length > 0)
        {
          _audioCodec =
            FilterGraphTools.AddFilterByName(_graphBuilder, FilterCategory.LegacyAmFilterCategory,
                                             settings.AudioCodec);
        }
        if (_audioCodec == null)
        {
          _audioCodec =
            FilterGraphTools.AddFilterByName(_graphBuilder, FilterCategory.LegacyAmFilterCategory,
                                             "Microsoft MPEG-1/DD Audio Decoder");
        }
        if (_audioCodec == null)
        {
          _audioCodec =
            FilterGraphTools.AddFilterByName(_graphBuilder, FilterCategory.LegacyAmFilterCategory, "MPA Decoder Filter");
        }
      }
      else if (ext.IndexOf(".avi") >= 0)
      {
        if (settings.DivXCodec != null && settings.DivXCodec.Length > 0)
        {
          _videoCodec =
            FilterGraphTools.AddFilterByName(_graphBuilder, FilterCategory.LegacyAmFilterCategory,
                                             settings.DivXCodec);
        }
        if (_videoCodec == null)
        {
          _videoCodec =
            FilterGraphTools.AddFilterByName(_graphBuilder, FilterCategory.LegacyAmFilterCategory, "ffdshow Video Decoder");
        }
        if (settings.AudioCodec != null && settings.AudioCodec.Length > 0)
        {
          _audioCodec =
            FilterGraphTools.AddFilterByName(_graphBuilder, FilterCategory.LegacyAmFilterCategory,
                                             settings.AudioCodec);
        }
        if (_audioCodec == null)
        {
          _audioCodec =
            FilterGraphTools.AddFilterByName(_graphBuilder, FilterCategory.LegacyAmFilterCategory, "ffdshow Audio Decoder");
        }
      }
    }

    /// <summary>
    /// Adds the file source filter to the graph.
    /// </summary>
    protected virtual void AddFileSource()
    {
      // Render the file
      int hr = _graphBuilder.RenderFile(_mediaItemAccessor.LocalFileSystemPath, null);
      DsError.ThrowExceptionForHR(hr);
    }

    /// <summary>
    /// Frees the audio/video codecs.
    /// </summary>
    protected virtual void FreeCodecs()
    {
      int hr;
      if (_videoh264Codec != null)
      {
        _graphBuilder.RemoveFilter(_videoh264Codec);
        while ((hr = Marshal.ReleaseComObject(_videoh264Codec)) > 0)
        {
          ;
        }
        _videoh264Codec = null;
      }
      if (_videoCodec != null)
      {
        _graphBuilder.RemoveFilter(_videoCodec);
        while ((hr = Marshal.ReleaseComObject(_videoCodec)) > 0)
        {
          ;
        }
        _videoCodec = null;
      }
      if (_audioCodec != null)
      {
        _graphBuilder.RemoveFilter(_audioCodec);
        while ((hr = Marshal.ReleaseComObject(_audioCodec)) > 0)
        {
          ;
        }
        _audioCodec = null;
      }
    }

    protected void Shutdown()
    {
      _initialized = false; ;
      lock (_mediaItemAccessor)
      {
        ServiceScope.Get<ILogger>().Debug("VideoPlayer: Stop playing");
        int hr = 0;

        if (_graphBuilder != null)
        {
          IMediaEventEx mee = _graphBuilder as IMediaEventEx;
          IMediaControl mc = _graphBuilder as IMediaControl;

          // Stop DirectShow notifications
          hr = mee.SetNotifyWindow(IntPtr.Zero, 0, IntPtr.Zero);

          // Stop the graph

          FilterState state;
          mc.GetState(10, out state);
          if (state != FilterState.Stopped)
          {
            hr = mc.StopWhenReady();
            hr = mc.Stop();
            mc.GetState(10, out state);
            ServiceScope.Get<ILogger>().Info("state:{0}", state);
          }

          if (_vmr9 != null)
          {
            if (_graphBuilder != null) //&& !_useEvr)
            {
              _graphBuilder.RemoveFilter(_vmr9);
            }
            //while (Marshal.ReleaseComObject(_vmr9) > 0) ;
            try
            {
              Marshal.ReleaseComObject(_vmr9);
            }
            catch (Exception)
            {
            }
          }
          _vmr9 = null;

          if (_allocatorKey >= 0)
          {
            if (!_useEvr)
            {
              Vmr9DeInit(_allocatorKey);
            }
            else
            {
              EvrDeinit(_allocatorKey);
            }
          }

          _allocatorKey = -1;

          // Dispose the allocator
          FreeCodecs();

          if (_allocator != null)
          {
            _allocator.Dispose();
            _allocator = null;
          }

          if (_instancePtr != IntPtr.Zero)
          {
            Marshal.FreeCoTaskMem(_instancePtr);
            _instancePtr = IntPtr.Zero;
          }

          if (_rot != null)
          {
            _rot.Dispose();
            _rot = null;
          }
          if (_graphBuilder != null)
          {
            while (Marshal.ReleaseComObject(_graphBuilder) > 0)
            {
              ;
            }
          }
          _graphBuilder = null;

          FreeResources();
          GC.Collect();
          GC.Collect();
          GC.Collect();
        }
      }
    }

    /// <summary>
    /// Allocates the vertex buffers
    /// </summary>
    protected void AllocateResources()
    {
      //      Trace.WriteLine("videoplayer:alloc vertex");
      if (_vertexBuffer != null)
      {
        FreeResources();
      }
      // Alloc a Vertex buffer to draw the video (4 vertices -> a Quad)
      _vertexBuffer = PositionColored2Textured.Create(4);
      ContentManager.VertexReferences++;
    }

    /// <summary>
    /// Frees the vertext buffers.
    /// </summary>
    protected void FreeResources()
    {
      ServiceScope.Get<ILogger>().Info("VideoPlayer: FreeResources");
      // Free Managed Direct3D resources
      if (_vertexBuffer != null)
      {
        //Trace.WriteLine("videoplayer:free vertex");
        _vertexBuffer.Dispose();
        _vertexBuffer = null;
        ContentManager.VertexReferences--;
      }
      if (_vertices != null)
      {
        _vertices = null;
      }
    }

#if NOTUSED
    /// <summary>
    /// Gets a value indicating whether render attributes (like position/size) have been changed
    /// </summary>
    /// <value><c>true</c> if changed; otherwise, <c>false</c>.</value>
    private bool Changed(bool usePIP)
    {
      if (usePIP)
      {
        _sourceRect = new Rectangle(0, 0, _allocator.TextureSize.Width, _allocator.TextureSize.Height);
        _destinationRect = new Rectangle(0, 0, DisplaySize.Width, DisplaySize.Height);
        return true;
      }
      Rectangle sourceRect;
      Rectangle destinationRect;
      if (_allocator.TextureSize == _previousTextureSize &&
          _allocator.VideoSize == _previousVideoSize &&
          _allocator.AspectRatio == _previousAspectRatio &&
          DisplaySize == _previousDisplaySize)
      {
        SkinContext.Geometry.ImageWidth = (int)_allocator.VideoSize.Width;
        SkinContext.Geometry.ImageHeight = (int)_allocator.VideoSize.Height;
        SkinContext.Geometry.ScreenWidth = (int)_displaySize.Width;
        SkinContext.Geometry.ScreenHeight = (int)_displaySize.Height;

        SkinContext.Geometry.GetWindow(_allocator.AspectRatio.Width, _allocator.AspectRatio.Height, out sourceRect,
                                       out destinationRect, SkinContext.CropSettings);
        if (sourceRect == _sourceRect && _destinationRect == destinationRect)
        {
          return false;
        }
      }

      SkinContext.Geometry.ImageWidth = (int)_allocator.VideoSize.Width;
      SkinContext.Geometry.ImageHeight = (int)_allocator.VideoSize.Height;
      SkinContext.Geometry.ScreenWidth = (int)_displaySize.Width;
      SkinContext.Geometry.ScreenHeight = (int)_displaySize.Height;
      SkinContext.Geometry.GetWindow(_allocator.AspectRatio.Width, _allocator.AspectRatio.Height, out sourceRect,
                                     out destinationRect, SkinContext.CropSettings);
      string shaderName = SkinContext.Geometry.Current.Shader;
      if (!string.IsNullOrEmpty(shaderName))
      {
        Effect = null;
        EffectAsset effect = ContentManager.GetEffect(shaderName);
        if (effect != null)
        {
          Effect = effect;
        }
      }
      _sourceRect = sourceRect;
      _destinationRect = destinationRect;

      _previousTextureSize = _allocator.TextureSize;
      _previousVideoSize = _allocator.VideoSize;
      _previousAspectRatio = _allocator.AspectRatio;
      _previousDisplaySize = DisplaySize;
      return true;
    }
    /// <summary>
    /// Updates the vertex buffers with the new rendering attributes like size/position.
    /// </summary>
    protected void UpdateVertex()
    {
      Size vmrTexSize = _allocator.TextureSize;
      Size vmrVidSize = _allocator.VideoSize;

      float uValue = (float)vmrVidSize.Width / (float)vmrTexSize.Width;
      float vValue = (float)vmrVidSize.Height / (float)vmrTexSize.Height;

      float uoffs = ((float)_sourceRect.X) / ((float)(vmrVidSize.Width));
      float voffs = ((float)_sourceRect.Y) / ((float)(vmrVidSize.Height));
      float u = ((float)_sourceRect.Width) / ((float)(vmrVidSize.Width));
      float v = ((float)_sourceRect.Height) / ((float)(vmrVidSize.Height));

      //take in account that the texture might be larger
      //then the video size
      uoffs *= uValue;
      u *= uValue;
      voffs *= vValue;
      v *= vValue;

      /*
      Size vmrAspectRatio = _allocator.AspectRatio;
      RectangleF videoClientRectangle = Rectangle.Empty;
      float videoAR = (float)vmrAspectRatio.Width / (float)vmrAspectRatio.Height;

      if (vmrVidSize.Width >= vmrVidSize.Height)
      {
        // Compute the video aspect-ratio for a landscape proportioned image
        videoClientRectangle.X = 0.0f;
        videoClientRectangle.Width = (float)displaySize.Width;
        videoClientRectangle.Height = ((float)displaySize.Width) / videoAR;
        videoClientRectangle.Y = ((float)displaySize.Height - videoClientRectangle.Height) / 2;
      }
      else
      {
        // Compute the video aspect-ratio for a portrait proportioned image
        videoClientRectangle.Y = 0.0f;
        videoClientRectangle.Width = (float)displaySize.Height * videoAR;
        videoClientRectangle.Height = (float)displaySize.Height;
        videoClientRectangle.X = ((float)displaySize.Width - videoClientRectangle.Width) / 2;
      }*/

      RectangleF videoClientRectangle =
        new RectangleF(_destinationRect.X, _destinationRect.Y, _destinationRect.Width, _destinationRect.Height);

      // The Quad is built using a triangle fan of 2 triangles : 0,1,2 and 0, 2, 3
      // 0 *-------------------* 1
      //   |\                  |
      //   |   \               |
      //   |      \            |
      //   |         \         |
      //   |            \      |
      //   |               \   |
      //   |                  \|
      // 3 *-------------------* 2

      float alphaUpperLeft = AlphaMask.X;
      if (alphaUpperLeft < 0)
      {
        alphaUpperLeft = 0;
      }
      if (alphaUpperLeft > 255)
      {
        alphaUpperLeft = 255;
      }

      float alphaBottomLeft = AlphaMask.Width;
      if (alphaBottomLeft < 0)
      {
        alphaBottomLeft = 0;
      }
      if (alphaBottomLeft > 255)
      {
        alphaBottomLeft = 255;
      }

      float alphaBottomRight = AlphaMask.Height;
      if (alphaBottomRight < 0)
      {
        alphaBottomRight = 0;
      }
      if (alphaBottomRight > 255)
      {
        alphaBottomRight = 255;
      }

      float alphaUpperRight = AlphaMask.Y;
      if (alphaUpperRight < 0)
      {
        alphaUpperRight = 0;
      }
      if (alphaUpperRight > 255)
      {
        alphaUpperRight = 255;
      }


      long colorUpperLeft = (long)alphaUpperLeft;
      colorUpperLeft <<= 24;
      colorUpperLeft += 0xffffff;
      long colorBottomLeft = (long)alphaBottomLeft;
      colorBottomLeft <<= 24;
      colorBottomLeft += 0xffffff;
      long colorBottomRight = (long)alphaBottomRight;
      colorBottomRight <<= 24;
      colorBottomRight += 0xffffff;
      long colorUpperRight = (long)alphaUpperRight;
      colorUpperRight <<= 24;
      colorUpperRight += 0xffffff;

      _movieRect.X = (int)(videoClientRectangle.X + Position.X);
      _movieRect.Y = (int)(videoClientRectangle.Y + Position.Y);
      _movieRect.Height = (int)(videoClientRectangle.Height);
      _movieRect.Width = (int)(videoClientRectangle.Width);

      //upperleft
      _vertices[0].X = videoClientRectangle.X + Position.X;
      _vertices[0].Y = videoClientRectangle.Y + Position.Y;
      _vertices[0].Color = (int)colorUpperLeft;

      //upperright
      _vertices[1].X = videoClientRectangle.Width + videoClientRectangle.X + Position.X;
      _vertices[1].Y = videoClientRectangle.Y + Position.Y;
      _vertices[1].Color = (int)colorUpperRight;

      //bottomright
      _vertices[2].X = videoClientRectangle.Width + videoClientRectangle.X + Position.X;
      _vertices[2].Y = videoClientRectangle.Height + videoClientRectangle.Y + Position.Y;
      _vertices[2].Color = (int)colorBottomRight;

      //bottomleft
      _vertices[3].X = videoClientRectangle.X + Position.X;
      _vertices[3].Y = videoClientRectangle.Height + videoClientRectangle.Y + Position.Y;
      _vertices[3].Color = (int)colorBottomLeft;

      //upperleft
      float tu2, tv2;
      SkinContext.GetAlphaGradientUV(new Vector3(_vertices[0].X, _vertices[0].Y, 0), out tu2, out tv2);
      _vertices[0].Tu1 = uoffs;
      _vertices[0].Tv1 = voffs;
      _vertices[0].Tu2 = tu2;
      _vertices[0].Tv2 = tv2;

      //upperright
      SkinContext.GetAlphaGradientUV(new Vector3(_vertices[1].X, _vertices[1].Y, 0), out tu2, out tv2);
      _vertices[1].Tu1 = uoffs + u;
      _vertices[1].Tv1 = voffs;
      _vertices[1].Tu2 = tu2;
      _vertices[1].Tv2 = tv2;

      //bottomright
      SkinContext.GetAlphaGradientUV(new Vector3(_vertices[2].X, _vertices[2].Y, 0), out tu2, out tv2);
      _vertices[2].Tu1 = uoffs + u;
      _vertices[2].Tv1 = voffs + v;
      _vertices[2].Tu2 = tu2;
      _vertices[2].Tv2 = tv2;

      //bottomleft
      SkinContext.GetAlphaGradientUV(new Vector3(_vertices[3].X, _vertices[3].Y, 0), out tu2, out tv2);
      _vertices[3].Tu1 = uoffs;
      _vertices[3].Tv1 = voffs + v;
      _vertices[3].Tu2 = tu2;
      _vertices[3].Tv2 = tv2;

      // Fill the vertex buffer
      PositionColored2Textured.Set(_vertexBuffer, ref _vertices);
    }
#endif
    protected void RenderTexture()
    {
      // Attach the vertex buffer to the Direct3D Device
      GraphicsDevice.Device.SetStreamSource(0, _vertexBuffer, 0, PositionColored2Textured.StrideSize);
      //GraphicsDevice.Device.VertexFormat = PositionColored2Textured.Format;

      _allocator.Render(_effect);

    }

    public virtual void BeginRender(EffectAsset effect)
    {
      if (!_initialized) return;
      if (_allocator == null) return;
      effect.StartRender(_allocator.Texture);
    }

    public virtual void EndRender(EffectAsset effect)
    {
      if (!_initialized) return;
      if (_allocator == null) return;
      effect.EndRender();
    }

    // Not used
    public virtual void Render()
    {
      /*
      if (_initialized == false) return;
      if (_vertexBuffer == null) return;
      lock (_fileName)
      {
        bool usePip = false;
        if (_players.Count > 0 && _players[0] != this)
        {
          //PIP.. use stretch mode..
          usePip = true;
        }
        if (Changed(usePip))
        {
          UpdateVertex();
        }
        RenderTexture();
      }*/
    }

    public virtual void Stop()
    {
      ServiceScope.Get<ILogger>().Debug("VideoPlayer: Stop");
      // FIXME
//      ResetRefreshRate();
      // TODO: WriteResumeData();
      Shutdown();
      FireStopped();
    }

    public void Pause()
    {
      if (_state != PlaybackState.Paused)
      {
        ServiceScope.Get<ILogger>().Debug("VideoPlayer: Pause");
        IMediaControl mc = _graphBuilder as IMediaControl;
        if (mc != null)
          mc.Pause();
        _state = PlaybackState.Paused;
        FirePaused();
      }
    }

    public void Resume()
    {
      if (_state == PlaybackState.Paused)
      {
        ServiceScope.Get<ILogger>().Debug("VideoPlayer: Resume");
        IMediaControl mc = _graphBuilder as IMediaControl;
        if (mc != null)
        {
          int hr = mc.Run();
          if (hr != 0 && hr != 1)
          {
            ServiceScope.Get<ILogger>().Error("VideoPlayer: Resume Failed to start: {0:X}", hr);
            Shutdown();
            FireStopped();
            return;
          }
        }
        _state = PlaybackState.Playing;
        FireResumed();
      }
    }

    public void Restart()
    {
      CurrentTime = new TimeSpan(0, 0, 0);
      IMediaControl mc = _graphBuilder as IMediaControl;
      mc.Run();
      _state = PlaybackState.Playing;
      FireStarted();
    }

    public virtual void UpdateTime()
    {
      if (!_initialized)
      {
        _currentTime = new TimeSpan();
        _duration = new TimeSpan();
        return;
      }
      if (_graphBuilder == null) return;
      lock (_mediaItemAccessor)
      {
        IMediaSeeking mediaSeeking = _graphBuilder as IMediaSeeking;
        if (mediaSeeking != null)
        {
          long lStreamPos;
          double fCurrentPos;
          mediaSeeking.GetCurrentPosition(out lStreamPos); // stream position
          fCurrentPos = lStreamPos;
          fCurrentPos /= 10000000d;

          long lContentStart, lContentEnd;
          double fContentStart, fContentEnd;
          mediaSeeking.GetAvailable(out lContentStart, out lContentEnd);
          fContentStart = lContentStart;
          fContentEnd = lContentEnd;
          fContentStart /= 10000000d;
          fContentEnd /= 10000000d;
          fContentEnd -= fContentStart;
          fCurrentPos -= fContentStart;
          _currentTime = new TimeSpan(0, 0, 0, 0, (int)(fCurrentPos * 1000.0f));
          _duration = new TimeSpan(0, 0, 0, 0, (int)(fContentEnd * 1000.0f));
        }
      }
    }

    public virtual TimeSpan CurrentTime
    {
      get { return _currentTime; }
      set
      {
        ServiceScope.Get<ILogger>().Debug("VideoPlayer: Seek {0} / {1}", value.TotalSeconds, _duration.TotalSeconds);


        lock (_graphBuilder)
        {
          IMediaSeeking mediaSeeking = _graphBuilder as IMediaSeeking;
          double dTimeInSecs = value.TotalSeconds;
          dTimeInSecs *= 10000000d;

          long lContentStart, lContentEnd;
          double dContentStart, dContentEnd;
          mediaSeeking.GetAvailable(out lContentStart, out lContentEnd);
          dContentStart = lContentStart;
          dContentEnd = lContentEnd;

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
      get { return _duration; }
    }

    public virtual string[] AudioStreams
    {
      get { return new string[0]; }
    }

    public virtual void SetAudioStream(string audioStream) { }

    public virtual string CurrentAudioStream
    {
      get { return ""; }
    }

    public PlaybackState State
    {
      get { return _state; }
      set { _state = value; }
    }

    public int Volume
    {
      get { return _volume; }
      set
      {
        if (_volume != value)
        {
          _volume = value;
          IBasicAudio audio = _graphBuilder as IBasicAudio;
          if (audio != null)
          {
            // Divide by 100 to get equivalent decibel value. For example, �10,000 is �100 dB. 
            float fPercent = _volume / 100.0f;
            int iVolume = (int) (5000 * fPercent);
            audio.put_Volume(iVolume - 5000);
          }
        }
      }
    }

    public bool Mute
    {
      get { return (_isMuted); }
      set
      {
        if (_isMuted != value)
        {
          _isMuted = value;
          int volume = 0;
          if (!_isMuted)
            volume = _volume;
          IBasicAudio audio = _graphBuilder as IBasicAudio;
          if (audio != null)
          {
            // Divide by 100 to get equivalent decibel value. For example, �10,000 is �100 dB. 
            float fPercent = (float)volume / 100.0f;
            int iVolume = (int)(5000.0f * fPercent);
            audio.put_Volume((iVolume - 5000));
          }
        }
      }
    }

    public virtual void ReleaseGUIResources()
    {
      //stops the renderer threads all of it's own.
      //this could be split into two parts, but we would need
      //EvrSuspend(_allocatorKey) for that.
      lock (_allocator)
      {
        FreeResources();
        _allocator.ReleaseResources();
        int hr;
        IEnumPins enumer;
        _vmr9.EnumPins(out enumer);
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
                      _vmr9ConnectionPins.Add(pinConnect);
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
        IMediaControl mc = _graphBuilder as IMediaControl;
        mc.GetState(10, out state);
        if (state == FilterState.Running)
        {
          hr = mc.StopWhenReady();
          hr = mc.Stop();
        }

        if (_allocator != null)
        {
          _allocator.Dispose();
          _allocator = null;
        }

        if (_vmr9 != null)
        {
          if (_graphBuilder != null) //&& !_useEvr)
          {
            _graphBuilder.RemoveFilter(_vmr9);
          }
          //while (Marshal.ReleaseComObject(_vmr9) > 0) ;
          Marshal.ReleaseComObject(_vmr9);
        }
        _vmr9 = null;


        if (_allocatorKey >= 0)
        {
          if (!_useEvr)
          {
            Vmr9DeInit(_allocatorKey);
          }
          else
          {
            EvrDeinit(_allocatorKey);
          }
        }

        _allocatorKey = -1;
        _initialized = false;
      }
    }

    public virtual void ReallocGUIResources()
    {
      if (_graphBuilder != null)
      {
        _vertices = new PositionColored2Textured[4];
        _allocator = new Allocator(this, _useEvr);
        AddEvrOrVmr9();
        IEnumPins enumer;
        _vmr9.EnumPins(out enumer);
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
                  if (_vmr9ConnectionPins.Count > 0)
                  {
                    _graphBuilder.Connect(_vmr9ConnectionPins[0], pins[0]);
                    Marshal.ReleaseComObject(_vmr9ConnectionPins[0]);
                    _vmr9ConnectionPins.RemoveAt(0);
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
        _allocator.ReallocResources();

        IMediaControl mc = _graphBuilder as IMediaControl;
        mc.Run();
        _initialized = true;
      }
    }
  }
}
