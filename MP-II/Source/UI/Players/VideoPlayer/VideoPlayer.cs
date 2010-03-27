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
using MediaPortal.Core.MediaManagement;
using MediaPortal.Core.Messaging;
using MediaPortal.Core.Settings;
using MediaPortal.Core.Logging;
using MediaPortal.UI.General;
using MediaPortal.UI.Presentation.Geometries;
using MediaPortal.UI.Presentation.Players;
using MediaPortal.UI.SkinEngine;
using MediaPortal.UI.SkinEngine.ContentManagement;
using MediaPortal.UI.SkinEngine.Players;
using MediaPortal.Utilities.Exceptions;
using SlimDX.Direct3D9;
using MediaPortal.UI.SkinEngine.DirectX;
using MediaPortal.UI.SkinEngine.Effects;
using MediaPortal.UI.SkinEngine.SkinManagement;

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

    public const string PLAYER_ID_STR = "9EF8D975-575A-4c64-AA54-500C97745969";

    public const string AUDIO_STREAM_NAME = "Audio1";

    protected const double PLAYBACK_RATE_PLAY_THRESHOLD = 0.05;

    protected String PlayerTitle;

    #endregion

    #region Variables

    // DirectShow objects
    protected IGraphBuilder _graphBuilder;
    protected DsROTEntry _rot;
    protected IBaseFilter _evr;
    protected EVRCallback _evrCallback;

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
    protected uint _streamCount = 1;

    // Filter graph related 
    protected IBaseFilter _videoh264Codec;
    protected IBaseFilter _videoCodec;
    protected IBaseFilter _audioCodec;

    protected List<IBaseFilter> _filterList = new List<IBaseFilter>();
    protected CodecHandler.CodecCapabilities _graphCapabilities;

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
    }

    public void Dispose()
    {
      if (_resourceAccessor != null)
        _resourceAccessor.Dispose();
      _resourceAccessor = null;
      UnsubscribeFromMessages();
    }

    #endregion

    void SubscribeToMessages()
    {
      _messageQueue = new AsynchronousMessageQueue(this, new string[]
        {
           WindowsMessaging.CHANNEL
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

    void OnMessageReceived(AsynchronousMessageQueue queue, SystemMessage message)
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
                ServiceScope.Get<ILogger>().Debug("{0}: Playback ended", PlayerTitle);
                // TODO: RemoveResumeData();
                FireEnded();
                return;
              }
            }
          }
        }
      }
    }

    void OnVideoSizePresent(EVRCallback sender)
    {
      FireStateReady();
    }

    #region IInitializablePlayer implementation

    public void SetMediaItemLocator(IResourceLocator locator)
    {
      if (_resourceAccessor != null)
      {
        _resourceAccessor.Dispose();
        _resourceAccessor = null;
      }
      _resourceLocator = locator;
      _resourceAccessor = _resourceLocator.CreateLocalFsAccessor();
      _state = PlayerState.Active;
      _isPaused = true;
      _vertices = new PositionColored2Textured[4];
      ServiceScope.Get<ILogger>().Debug("{0}: Initializing for media file '{1}'", PlayerTitle, _resourceAccessor.LocalFileSystemPath);

      try
      {
        AllocateResources();

        // Create a DirectShow FilterGraph
        CreateGraphBuilder();

        // Add it in ROT (Running Object Table) for debug purpose 
        _rot = new DsROTEntry(_graphBuilder);

        // Add a notification handler (see WndProc)
        _instancePtr = Marshal.AllocCoTaskMem(4);
        IMediaEventEx mee = _graphBuilder as IMediaEventEx;
        int hr = mee.SetNotifyWindow(SkinContext.Form.Handle, WM_GRAPHNOTIFY, _instancePtr);

        // Create the Allocator / Presenter object
        _evrCallback = new EVRCallback(this);
        _evrCallback.VideoSizePresent += OnVideoSizePresent;

        AddEvr();

        ServiceScope.Get<ILogger>().Debug("{0}: Adding preferred codecs", PlayerTitle);
        AddPreferredCodecs();

        ServiceScope.Get<ILogger>().Debug("{0}: Adding file source", PlayerTitle);
        AddFileSource();

        ServiceScope.Get<ILogger>().Debug("{0}: Run graph", PlayerTitle);

        ///This needs to be done here before we check if the evr pins are connected
        ///since this method gives players the chance to render the last bits of the graph
        OnBeforeGraphRunning();

        bool success = false;
        IEnumPins enumer;
        _evr.EnumPins(out enumer);
        if (enumer != null)
        {
          IPin[] pins = new IPin[2];
          IntPtr ptrFetched = Marshal.AllocCoTaskMem(4);
          if (enumer.Next(1, pins, ptrFetched) == 0 && Marshal.ReadInt32(ptrFetched) == 1 && pins[0] != null)
          {
            IPin pinConnect;
            if (pins[0].ConnectedTo(out pinConnect) == 0)
              if (pinConnect != null)
              {
                success = true;
                Marshal.ReleaseComObject(pinConnect);
              }
            Marshal.ReleaseComObject(pins[0]);
          }
          Marshal.FreeCoTaskMem(ptrFetched);
          Marshal.ReleaseComObject(enumer);
        }
        if (!success)
          throw new VideoPlayerException("Cannot build graph");

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

    void AddEvr()
    {
      ServiceScope.Get<ILogger>().Debug("{0}: Initialize EVR", PlayerTitle);

      _evr = (IBaseFilter) new EnhancedVideoRenderer();

      IEVRFilterConfig config = (IEVRFilterConfig) _evr;
      int hr = config.SetNumberOfStreams(_streamCount);

      int ordinal = GraphicsDevice.Device.Capabilities.AdapterOrdinal;
      AdapterInformation ai = MPDirect3D.Direct3D.Adapters[ordinal];
      IntPtr hMonitor = MPDirect3D.Direct3D.GetAdapterMonitor(ai.Adapter);
      IntPtr upDevice = GraphicsDevice.Device.ComPointer;
      _allocatorKey = EvrInit(_evrCallback, (uint) upDevice.ToInt32(), _evr, (uint) hMonitor.ToInt32());
      if (_allocatorKey < 0)
      {
        EvrDeinit(_allocatorKey);
        Marshal.ReleaseComObject(_evr);
        _evrCallback.Dispose();
        _evrCallback = null;
        throw new VideoPlayerException("Initializing of EVR failed");
      }
      hr = _graphBuilder.AddFilter(_evr, "Enhanced Video Renderer");
    }

    /// <summary>
    /// Enables/disables frame skipping.
    /// </summary>
    /// <param name="onOff"><c>true</c> enables frame skipping, <c>false</c> disables it.</param>
    protected void EnableFrameSkipping(bool onOff)
    {
      EvrEnableFrameSkipping(_allocatorKey, onOff);
    }

    /// <summary>
    /// Called just before starting the graph
    /// </summary>
    protected virtual void OnBeforeGraphRunning()
    {
    }

    /// <summary>
    /// Called when graph is started
    /// </summary>
    protected virtual void OnGraphRunning() { }

    /// <summary>
    /// Creates a new IFilterGraph2 interface
    /// </summary>
    protected virtual void CreateGraphBuilder()
    {
      _graphBuilder = (IFilterGraph2) new FilterGraph();
    }

    /// <summary>
    /// Try to add filter by name to graph.
    /// If it succeedes it gets added to _filtersList.
    /// </summary>
    /// <param name="CodecName">Filter name to add</param>
    /// <returns>true if successful</returns>
    protected bool TryAdd(String CodecName)
    {
      IBaseFilter _tempFilter = FilterGraphTools.AddFilterByName(_graphBuilder, FilterCategory.LegacyAmFilterCategory, CodecName);
      if (_tempFilter != null)
      {
        _filterList.Add(_tempFilter);
        return true;
      }
      return false;
    }

    /// <summary>
    /// adds prefferred audio/video codecs
    /// </summary>
    protected virtual void AddPreferredCodecs()
    {
      // Init capabilities
      CodecHandler.CodecCapabilities requiredCapabilities = CodecHandler.CodecCapabilities.None;
      _graphCapabilities = CodecHandler.CodecCapabilities.None;

      CodecHandler codecHandler = new CodecHandler();
      VideoSettings settings = ServiceScope.Get<ISettingsManager>().Load<VideoSettings>();

      string ext = Path.GetExtension(_resourceAccessor.LocalFileSystemPath);
      if (ext.IndexOf(".mpg") >= 0 || ext.IndexOf(".ts") >= 0 || ext.IndexOf(".mpeg") >= 0)
      {
        requiredCapabilities = CodecHandler.CodecCapabilities.VideoH264 | CodecHandler.CodecCapabilities.VideoMPEG2 | CodecHandler.CodecCapabilities.AudioMPEG;
      }
      else if (ext.IndexOf(".avi") >= 0)
      {
        requiredCapabilities = CodecHandler.CodecCapabilities.VideoDIVX /* | CodecHandler.CodecCapabilities.AudioMPEG*/;
      }

      // set default codecs
      if (codecHandler.Supports(requiredCapabilities, CodecHandler.CodecCapabilities.VideoH264) && !string.IsNullOrEmpty(settings.H264Codec)) 
        codecHandler.SetPreferred(settings.H264Codec, CodecHandler.CodecCapabilities.VideoH264);

      if (codecHandler.Supports(requiredCapabilities, CodecHandler.CodecCapabilities.VideoMPEG2) && !string.IsNullOrEmpty(settings.Mpeg2Codec)) 
        codecHandler.SetPreferred(settings.Mpeg2Codec, CodecHandler.CodecCapabilities.VideoMPEG2);

      if (codecHandler.Supports(requiredCapabilities, CodecHandler.CodecCapabilities.VideoDIVX) && !string.IsNullOrEmpty(settings.DivXCodec)) 
          codecHandler.SetPreferred(settings.DivXCodec, CodecHandler.CodecCapabilities.VideoDIVX);

      if (codecHandler.Supports(requiredCapabilities, CodecHandler.CodecCapabilities.AudioMPEG) &&!string.IsNullOrEmpty(settings.AudioCodec)) 
        codecHandler.SetPreferred(settings.AudioCodec, CodecHandler.CodecCapabilities.AudioMPEG);

      // Lookup all known codecs and add them to graph
      foreach (CodecInfo currentCodec in codecHandler.CodecList)
      {
        // Check H264 codec
        if (codecHandler.Supports(requiredCapabilities, CodecHandler.CodecCapabilities.VideoH264))
          AddFilterByCapability(codecHandler, currentCodec, CodecHandler.CodecCapabilities.VideoH264);

        // Check MPEG2 codec
        if (codecHandler.Supports(requiredCapabilities, CodecHandler.CodecCapabilities.VideoMPEG2))
          AddFilterByCapability(codecHandler, currentCodec, CodecHandler.CodecCapabilities.VideoMPEG2);

        // Check Audio codec
        if (codecHandler.Supports(requiredCapabilities, CodecHandler.CodecCapabilities.AudioMPEG))
          AddFilterByCapability(codecHandler, currentCodec, CodecHandler.CodecCapabilities.AudioMPEG);

        // Exit if all needed capabilities exist
        if (codecHandler.Supports(_graphCapabilities, requiredCapabilities))
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
          ServiceScope.Get<ILogger>().Debug("{0}: Add {1} Codec {2} ", PlayerTitle, requestedCapability.ToString(), currentCodec.Name);
        }
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
    /// Frees the audio/video codecs.
    /// </summary>
    protected virtual void FreeCodecs()
    {
      int hr;

      // Iterate and free all manually added filters
      foreach (IBaseFilter currentFilter in _filterList)
      {
        _graphBuilder.RemoveFilter(currentFilter);
        while ((hr = Marshal.ReleaseComObject(currentFilter)) > 0)
        {
          ;
        }
      }
    }

    protected void Shutdown()
    {
      StopSeeking();
      _initialized = false;
      lock (_resourceAccessor)
      {
        ServiceScope.Get<ILogger>().Debug("{0}: Stop playing", PlayerTitle);
        int hr = 0;

        if (_graphBuilder != null)
        {
          IMediaEventEx mee = _graphBuilder as IMediaEventEx;
          IMediaControl mc = (IMediaControl) _graphBuilder;

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

          if (_evr != null)
          {
            if (_graphBuilder != null)
              _graphBuilder.RemoveFilter(_evr);
            //while (Marshal.ReleaseComObject(_evr) > 0) ;
            try
            {
              Marshal.ReleaseComObject(_evr);
            }
            catch (Exception)
            {
            }
          }
          _evr = null;

          if (_allocatorKey >= 0)
            // FIXME: From time to time, our DShowHelper crashes here
            EvrDeinit(_allocatorKey);

          _allocatorKey = -1;

          FreeCodecs();

          if (_evrCallback != null)
          {
            _evrCallback.Dispose();
            _evrCallback = null;
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
          // morpheus: do we really need to force GC ???
          //GC.Collect();
          //GC.Collect();
          //GC.Collect();
        }
      }
    }

    /// <summary>
    /// Allocates the vertex buffers
    /// </summary>
    protected void AllocateResources()
    {
      //Trace.WriteLine("videoplayer:alloc vertex");
      if (_vertexBuffer != null)
        FreeResources();
      // Alloc a Vertex buffer to draw the video (4 vertices -> a Quad)
      _vertexBuffer = PositionColored2Textured.Create(4);
      ContentManager.VertexReferences++;
    }

    /// <summary>
    /// Frees the vertext buffers.
    /// </summary>
    protected void FreeResources()
    {
      ServiceScope.Get<ILogger>().Info("{0}: FreeResources", PlayerTitle);
      // Free Managed Direct3D resources
      if (_vertexBuffer != null)
      {
        //Trace.WriteLine("videoplayer:free vertex");
        _vertexBuffer.Dispose();
        _vertexBuffer = null;
        ContentManager.VertexReferences--;
      }
      if (_vertices != null)
        _vertices = null;
    }

    /// <summary>
    /// Helper method for calculating the hundredth decibel value, needed by the <see cref="IBasicAudio"/>
    /// interface (in the range from -10000 to 0), which is logarithmic, from our volume (in the range from 0 to 100),
    /// which is linear.
    /// </summary>
    /// <param name="volume">Volume in the range from 0 to 100, in a linear scale.</param>
    /// <returns>Volume in the range from -10000 to 0, in a logarithmic scale.</returns>
    private static int VolumeToHundredthDeciBel(int volume)
    {
      return (int) ((Math.Log10(volume * 99f/100f + 1) - 2) * 5000);
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

    public virtual void BeginRender(EffectAsset effect)
    {
      if (!_initialized) return;
      if (_evrCallback == null) return;
      effect.StartRender(_evrCallback.Texture);
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
        ServiceScope.Get<ILogger>().Debug("{0}: Seek to {1} seconds", PlayerTitle, value.TotalSeconds);

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
        else
          return rate;
      }
    }

    public virtual bool SetPlaybackRate(double value)
    {
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
        ServiceScope.Get<ILogger>().Debug("{0}: Stop", PlayerTitle);
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
        ServiceScope.Get<ILogger>().Debug("{0}: Pause", PlayerTitle);
        IMediaControl mc = (IMediaControl) _graphBuilder;
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
        ServiceScope.Get<ILogger>().Debug("{0}: Resume", PlayerTitle);
        IMediaControl mc = (IMediaControl) _graphBuilder;
        if (mc != null)
        {
          int hr = mc.Run();
          if (hr != 0 && hr != 1)
          {
            ServiceScope.Get<ILogger>().Error("{0}: Resume Failed to start: {0:X}", PlayerTitle, hr);
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

    public virtual string[] AudioStreams
    {
      get { return new string[] { AUDIO_STREAM_NAME }; }
    }

    public virtual void SetAudioStream(string audioStream) { }

    public virtual string CurrentAudioStream
    {
      get { return AUDIO_STREAM_NAME; }
    }

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
        int hr;
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
        IMediaControl mc = (IMediaControl) _graphBuilder;
        mc.GetState(10, out state);
        if (state == FilterState.Running)
        {
          hr = mc.StopWhenReady();
          hr = mc.Stop();
        }

        if (_evrCallback != null)
        {
          _evrCallback.Dispose();
          _evrCallback = null;
        }

        if (_evr != null)
        {
          if (_graphBuilder != null)
            _graphBuilder.RemoveFilter(_evr);
          //while (Marshal.ReleaseComObject(_vmr9) > 0) ;
          Marshal.ReleaseComObject(_evr);
        }
        _evr = null;

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
        _vertices = new PositionColored2Textured[4];
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
