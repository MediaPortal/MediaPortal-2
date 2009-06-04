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
using MediaPortal.Presentation.Geometries;
using MediaPortal.Presentation.Players;
using MediaPortal.SkinEngine;
using MediaPortal.SkinEngine.ContentManagement;
using MediaPortal.SkinEngine.Players;
using MediaPortal.Utilities.Exceptions;
using SlimDX.Direct3D9;
using MediaPortal.SkinEngine.DirectX;
using MediaPortal.SkinEngine.Effects;
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

    #endregion

    #region Variables

    // DirectShow objects
    protected IGraphBuilder _graphBuilder;
    protected DsROTEntry _rot;
    protected IBaseFilter _evr;
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
    protected uint _streamCount = 1;

    protected IBaseFilter _videoh264Codec;
    protected IBaseFilter _videoCodec;
    protected IBaseFilter _audioCodec;
    protected IGeometry _geometryOverride = null;

    protected PlaybackState _state;
    protected int _volume = 100;
    protected bool _isMuted = false;
    protected bool _isAudioEnabled = true;
    protected bool _initialized = false;
    protected readonly List<IPin> _evrConnectionPins = new List<IPin>();
    protected IMediaItemLocator _mediaItemLocator;
    protected IMediaItemLocalFsAccessor _mediaItemAccessor;
    protected string _mediaItemTitle = null;
    protected PlayerEventDlgt _started = null;
    protected PlayerEventDlgt _stopped = null;
    protected PlayerEventDlgt _ended = null;
    protected PlayerEventDlgt _paused = null;
    protected PlayerEventDlgt _resumed = null;
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
    }

    public void Dispose()
    {
      if (_mediaItemAccessor != null)
        _mediaItemAccessor.Dispose();
      _mediaItemAccessor = null;
      UnsubscribeFromMessages();
    }

    #endregion

    void SubscribeToMessages()
    {
      _messageQueue = new AsynchronousMessageQueue(string.Format("Message queue of class '{0}'", GetType().Name), new string[]
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

    void OnMessageReceived(AsynchronousMessageQueue queue, QueueMessage message)
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
                _state = PlaybackState.Ended;
                ServiceScope.Get<ILogger>().Debug("VideoPlayer: Playback ended");
                // TODO: RemoveResumeData();
                FireEnded();
                return;
              }
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
      ServiceScope.Get<ILogger>().Debug("VideoPlayer: Initializing for media file '{0}'", _mediaItemAccessor.LocalFileSystemPath);

      try
      {
        AllocateResources();

        // Create a DirectShow FilterGraph
        CreateGraphBuilder();

        // Add it in ROT for debug purpose FIXME Albert -> what is ROT?
        _rot = new DsROTEntry(_graphBuilder);

        // Add a notification handler (see WndProc)
        _instancePtr = Marshal.AllocCoTaskMem(4);
        IMediaEventEx mee = _graphBuilder as IMediaEventEx;
        int hr = mee.SetNotifyWindow(SkinContext.Form.Handle, WM_GRAPHNOTIFY, _instancePtr);

        // Create the Allocator / Presenter object
        _allocator = new Allocator(this);

        AddEvr();

        ServiceScope.Get<ILogger>().Debug("VideoPlayer: Adding preferred codecs");
        AddPreferredCodecs();

        ServiceScope.Get<ILogger>().Debug("VideoPlayer: Adding file source");
        AddFileSource();

        ServiceScope.Get<ILogger>().Debug("VideoPlayer: Run graph");

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

    public void InitializePlayerEvents(PlayerEventDlgt started, PlayerEventDlgt stopped,
        PlayerEventDlgt ended, PlayerEventDlgt paused, PlayerEventDlgt resumed, PlayerEventDlgt playbackError)
    {
      _started = started;
      _stopped = stopped;
      _ended = ended;
      _paused = paused;
      _resumed = resumed;
      _playbackError = playbackError;
    }

    public void ResetPlayerEvents()
    {
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
        _started(this);
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

    protected void FirePaused()
    {
      if (_paused != null)
        _paused(this);
    }

    protected void FireResumed()
    {
      if (_resumed != null)
        _resumed(this);
    }

    void AddEvr()
    {
      ServiceScope.Get<ILogger>().Debug("VideoPlayer: Initialize EVR");

      _evr = (IBaseFilter) new EnhancedVideoRenderer();

      IEVRFilterConfig config = (IEVRFilterConfig) _evr;
      int hr = config.SetNumberOfStreams(_streamCount);

      int ordinal = GraphicsDevice.Device.Capabilities.AdapterOrdinal;
      AdapterInformation ai = MPDirect3D.Direct3D.Adapters[ordinal];
      IntPtr hMonitor = MPDirect3D.Direct3D.GetAdapterMonitor(ai.Adapter);
      IntPtr upDevice = GraphicsDevice.Device.ComPointer;
      _allocatorKey = EvrInit(_allocator, (uint) upDevice.ToInt32(), _evr, (uint) hMonitor.ToInt32());
      if (_allocatorKey < 0)
      {
        EvrDeinit(_allocatorKey);
        Marshal.ReleaseComObject(_evr);
        _allocator.Dispose();
        _allocator = null;
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
    /// adds prefferred audio/video codecs
    /// </summary>
    protected virtual void AddPreferredCodecs()
    {
      VideoSettings settings = ServiceScope.Get<ISettingsManager>().Load<VideoSettings>();
      string ext = Path.GetExtension(_mediaItemAccessor.LocalFileSystemPath);
      if (ext.IndexOf(".mpg") >= 0 || ext.IndexOf(".ts") >= 0 || ext.IndexOf(".mpeg") >= 0)
      {
        //_videoh264Codec = FilterGraphTools.AddFilterByName(_graphBuilder, FilterCategory.LegacyAmFilterCategory, "CoreAVC Video Decoder");

        if (!string.IsNullOrEmpty(settings.H264Codec))
        {
          _videoh264Codec = FilterGraphTools.AddFilterByName(_graphBuilder,
              FilterCategory.LegacyAmFilterCategory, settings.H264Codec);
        }

        if (_videoh264Codec == null)
        {
          _videoh264Codec = FilterGraphTools.AddFilterByName(_graphBuilder,
              FilterCategory.LegacyAmFilterCategory, "CyberLink H.264/AVC Decoder (PDVD7.X)");
        }
        if (!string.IsNullOrEmpty(settings.Mpeg2Codec))
        {
          _videoCodec = FilterGraphTools.AddFilterByName(_graphBuilder,
              FilterCategory.LegacyAmFilterCategory, settings.Mpeg2Codec);
        }

        if (_videoCodec == null)
        {
          _videoCodec = FilterGraphTools.AddFilterByName(_graphBuilder,
              FilterCategory.LegacyAmFilterCategory, "CyberLink Video/SP Decoder");
        }
        if (_videoCodec == null)
        {
          _videoCodec = FilterGraphTools.AddFilterByName(_graphBuilder,
              FilterCategory.LegacyAmFilterCategory, "Microsoft MPEG-2 Video Decoder");
        }
        if (_videoCodec == null)
        {
          _videoCodec = FilterGraphTools.AddFilterByName(_graphBuilder,
              FilterCategory.LegacyAmFilterCategory, "NVIDIA Video Decoder");
        }
        if (_videoCodec == null)
        {
          _videoCodec = FilterGraphTools.AddFilterByName(_graphBuilder,
              FilterCategory.LegacyAmFilterCategory, "MPV Decoder Filter");
        }

        if (!string.IsNullOrEmpty(settings.AudioCodec))
        {
          _audioCodec = FilterGraphTools.AddFilterByName(_graphBuilder,
              FilterCategory.LegacyAmFilterCategory, settings.AudioCodec);
        }
        if (_audioCodec == null)
        {
          _audioCodec = FilterGraphTools.AddFilterByName(_graphBuilder,
              FilterCategory.LegacyAmFilterCategory, "Microsoft MPEG-1/DD Audio Decoder");
        }
        if (_audioCodec == null)
        {
          _audioCodec = FilterGraphTools.AddFilterByName(_graphBuilder,
              FilterCategory.LegacyAmFilterCategory, "MPA Decoder Filter");
        }
      }
      else if (ext.IndexOf(".avi") >= 0)
      {
        if (!string.IsNullOrEmpty(settings.DivXCodec))
        {
          _videoCodec = FilterGraphTools.AddFilterByName(_graphBuilder,
              FilterCategory.LegacyAmFilterCategory, settings.DivXCodec);
        }
        if (_videoCodec == null)
        {
          _videoCodec = FilterGraphTools.AddFilterByName(_graphBuilder,
              FilterCategory.LegacyAmFilterCategory, "ffdshow Video Decoder");
        }
        if (!string.IsNullOrEmpty(settings.AudioCodec))
        {
          _audioCodec = FilterGraphTools.AddFilterByName(_graphBuilder,
              FilterCategory.LegacyAmFilterCategory, settings.AudioCodec);
        }
        if (_audioCodec == null)
        {
          _audioCodec = FilterGraphTools.AddFilterByName(_graphBuilder,
              FilterCategory.LegacyAmFilterCategory, "ffdshow Audio Decoder");
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
      _initialized = false;
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
            EvrDeinit(_allocatorKey);

          _allocatorKey = -1;

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
      int volume = 0;
      if (!_isMuted && _isAudioEnabled)
        volume = _volume;
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

    public IGeometry GeometryOverride
    {
      get { return _geometryOverride; }
      set { _geometryOverride = value; }
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

    public virtual void Stop()
    {
      if (_state != PlaybackState.Stopped)
      {
        ServiceScope.Get<ILogger>().Debug("VideoPlayer: Stop");
        // FIXME
//        ResetRefreshRate();
        // TODO: WriteResumeData();
        Shutdown();
        FireStopped();
      }
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

    public void SetMediaItemTitleHint(string title)
    {
      // We don't extract the title by ourselves, we just use the title hint
      _mediaItemTitle = title;
    }

    public string MediaItemTitle
    {
      get { return _mediaItemTitle; }
    }

    public virtual TimeSpan CurrentTime
    {
      get
      {
        lock (_mediaItemAccessor)
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
        ServiceScope.Get<ILogger>().Debug("VideoPlayer: Seek to {0} seconds", value.TotalSeconds);

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
        lock (_mediaItemAccessor)
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

    public virtual string[] AudioStreams
    {
      get { return new string[] { AUDIO_STREAM_NAME }; }
    }

    public virtual void SetAudioStream(string audioStream) { }

    public virtual string CurrentAudioStream
    {
      get { return AUDIO_STREAM_NAME; }
    }

    public PlaybackState State
    {
      get { return _state; }
      set { _state = value; }
    }

    public bool IsAudioEnabled
    {
      get { return _isAudioEnabled; }
      set
      {
        if (_isAudioEnabled == value)
          return;
        _isAudioEnabled = value;
        CheckAudio();
      }
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
      lock (_allocator)
      {
        FreeResources();
        _allocator.ReleaseResources();
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
        _allocator = new Allocator(this);
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
        _allocator.ReallocResources();

        if (State == PlaybackState.Playing)
        {
          IMediaControl mc = _graphBuilder as IMediaControl;
          mc.Run();
        }
        _initialized = true;
      }
    }

    #endregion
  }
}
