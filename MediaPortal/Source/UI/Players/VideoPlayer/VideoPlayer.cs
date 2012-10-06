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
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security;
using DirectShowLib;
using MediaPortal.Common;
using MediaPortal.Common.Localization;
using MediaPortal.Common.Logging;
using MediaPortal.Common.Settings;
using MediaPortal.UI.Players.Video.Settings;
using MediaPortal.UI.Players.Video.Tools;
using MediaPortal.UI.Presentation.Geometries;
using MediaPortal.UI.Presentation.Players;
using MediaPortal.UI.SkinEngine.Players;
using MediaPortal.UI.SkinEngine.SkinManagement;
using MediaPortal.Utilities.Exceptions;
using SlimDX.Direct3D9;

namespace MediaPortal.UI.Players.Video
{
  public class VideoPlayer : BaseDXPlayer, ISlimDXVideoPlayer, ISubtitlePlayer, IChapterPlayer, ITitlePlayer
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

    [DllImport("EVRPresenter.dll", ExactSpelling = true, CharSet = CharSet.Auto, SetLastError = true)]
    private static extern int EvrInit(IEVRPresentCallback callback, uint dwD3DDevice, IBaseFilter evrFilter, IntPtr monitor, out IntPtr presenterInstance);

    [DllImport("EVRPresenter.dll", ExactSpelling = true, CharSet = CharSet.Auto, SetLastError = true)]
    private static extern void EvrDeinit(IntPtr presenterInstance);

    #endregion

    #region Consts

    protected const string EVR_FILTER_NAME = "Enhanced Video Renderer";
    protected IntPtr _presenterInstance;

    public const string PLAYER_ID_STR = "9EF8D975-575A-4c64-AA54-500C97745969";

    // The default name for "No subtitles available" or "Subtitles disabled".
    protected const string NO_SUBTITLES = "No subtitles";

    public const string RES_PLAYBACK_CHAPTER = "[Playback.Chapter]";

    #endregion

    #region Variables

    // DirectShow objects
    protected IBaseFilter _evr;
    protected EVRCallback _evrCallback;

    // Managed Direct3D Resources
    protected Size _displaySize = new Size(100, 100);

    protected Size _previousTextureSize;
    protected Size _previousVideoSize;
    protected Size _previousAspectRatio;
    protected Size _previousDisplaySize;
    protected SizeF _maxUV = new SizeF(1.0f, 1.0f);

    // Internal state and variables
    protected IGeometry _geometryOverride = null;
    protected string _effectOverride = null;
    protected CropSettings _cropSettings;

    protected readonly List<IPin> _evrConnectionPins = new List<IPin>();

    protected SkinEngine.Players.RenderDlgt _renderDlgt = null;

    protected StreamInfoHandler _streamInfoAudio = null;
    protected StreamInfoHandler _streamInfoSubtitles = null;
    protected StreamInfoHandler _streamInfoTitles = null; // Used mostly for MKV Editions
    protected List<IAMStreamSelect> _streamSelectors = null;
    private readonly object _syncObj = new object();

    /// <summary>
    /// List of chapter timestamps. Will be initialized lazily. <c>null</c> if not currently valid.
    /// </summary>
    protected double[] _chapterTimestamps = null;

    /// <summary>
    /// List of chapter names. Will be initialized lazily. <c>null</c> if not currently valid.
    /// </summary>
    protected string[] _chapterNames = null;

    protected bool _textureInvalid = true;

    #endregion

    #region Ctor & dtor

    public VideoPlayer ()
    {
      _cropSettings = ServiceRegistration.Get<IGeometryManager>().CropSettings;

      // EVR is available since Vista
      OperatingSystem osInfo = Environment.OSVersion;
      if (osInfo.Version.Major <= 5)
        throw new EnvironmentException("This video player can only run on Windows Vista or above");

      PlayerTitle = "VideoPlayer";
    }

    #endregion

    #region EVR Callback

    protected void RenderFrame()
    {
      SkinEngine.Players.RenderDlgt dlgt = _renderDlgt;
      if (dlgt != null)
        dlgt();
    }

    #endregion

    #region IInitializablePlayer implementation

    protected override void AddPresenter ()
    {
      // Create the Allocator / Presenter object
      FreeEvrCallback();
      CreateEvrCallback();

      AddEvr();
    }

    #endregion

    #region Graph building

    /// <summary>
    /// Adds the EVR to graph.
    /// </summary>
    protected virtual void AddEvr()
    {
      ServiceRegistration.Get<ILogger>().Debug("{0}: Initialize EVR", PlayerTitle);

      _evr = (IBaseFilter) new EnhancedVideoRenderer();

      IntPtr upDevice = SkinContext.Device.ComPointer;
      int hr = EvrInit(_evrCallback, (uint) upDevice.ToInt32(), _evr, SkinContext.Form.Handle, out _presenterInstance);
      if (hr != 0)
      {
        EvrDeinit(_presenterInstance);
        FilterGraphTools.TryRelease(ref _evr);
        throw new VideoPlayerException("Initializing of EVR failed");
      }

      // Set the number of video/subtitle/cc streams that are allowed to be connected to EVR. This has to be done after the custom presenter is initialized.
      IEVRFilterConfig config = (IEVRFilterConfig) _evr;
      config.SetNumberOfStreams(_streamCount);

      _graphBuilder.AddFilter(_evr, EVR_FILTER_NAME);
    }

    #endregion

    #region Graph shutdown

    /// <summary>
    /// Release all COM object references to IAMStreamSelect instances.
    /// </summary>
    protected virtual void ReleaseStreamSelectors()
    {
      // Release all existing stream selector references
      if (_streamSelectors != null)
        foreach (IAMStreamSelect streamSelector in _streamSelectors)
          Marshal.ReleaseComObject(streamSelector);
    }

    /// <summary>
    /// Frees the audio/video codecs.
    /// </summary>
    protected override void FreeCodecs()
    {
      // Release stream selectors
      ReleaseStreamSelectors();

      // Free all filters from graph
      if (_graphBuilder != null)
        FilterGraphTools.RemoveAllFilters(_graphBuilder, true);

      // Free EVR
      EvrDeinit(_presenterInstance);
      FreeEvrCallback();
      FilterGraphTools.TryRelease(ref _evr);

      FilterGraphTools.TryDispose(ref _rot);
      FilterGraphTools.TryRelease(ref _graphBuilder);
    }
    
    #endregion

    #region ISlimDXVideoPlayer implementation

    public override Guid PlayerId
    {
      get { return new Guid(PLAYER_ID_STR); }
    }

    public override string Name
    {
      get { return "Video"; }
    }

    public Size VideoSize
    {
      get { return (_evrCallback == null || !_initialized) ? new Size(0, 0) : _evrCallback.OriginalVideoSize; }
    }

    public SizeF VideoAspectRatio
    {
      get { return (_evrCallback == null) ? new Size(1, 1) : _evrCallback.AspectRatio; }
    }

    protected Surface RawVideoSurface
    {
      get { return (_initialized && _evrCallback != null) ? _evrCallback.Surface : null; }
    }

    public object SurfaceLock
    {
      get
      {
        EVRCallback callback = _evrCallback;
        return callback == null ? _syncObj : callback.SurfaceLock;
      }
    }

    public Surface Surface
    {
      get
      {
        lock (SurfaceLock)
        {
          Surface videoSurface = RawVideoSurface;
          if (!_textureInvalid)
            return videoSurface;

          if (videoSurface == null || videoSurface.Disposed)
            return null;

          PostProcessTexture(videoSurface);
          _textureInvalid = false;
          return videoSurface;
        }
      }
    }

    protected void OnTextureInvalidated()
    {
      _textureInvalid = true;
    }

    /// <summary>
    /// PostProcessTexture allows video players to post process the video frame texture,
    /// i.e. for overlaying subtitles or OSD menus.
    /// </summary>
    /// <param name="targetTexture"></param>
    protected virtual void PostProcessTexture(Surface targetTexture)
    { }

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

    #region Audio streams

    /// <summary>
    /// Sets the preferred audio stream. The stream is chosen either by LCID or the last used stream name.
    /// If there is no matching stream, the first available can be chosen if <paramref name="useFirstAsDefault"/> 
    /// is set to <c>true</c>. This is especially required for the <see cref="TsVideoPlayer"/>.
    /// </summary>
    /// <param name="useFirstAsDefault"><c>true</c> to enable the first stream as default, if no language match found</param>
    protected void SetPreferredAudio(bool useFirstAsDefault = false)
    {
      EnumerateStreams();
      StreamInfoHandler audioStreams;
      lock (SyncObj)
        audioStreams = _streamInfoAudio;

      if (audioStreams == null || audioStreams.Count == 0)
        return;

      VideoSettings settings = ServiceRegistration.Get<ISettingsManager>().Load<VideoSettings>();

      // When multiple streams are available, we select the stream by channel count preference (PreferMultiChannelAudio).
      Predicate<StreamInfo> channelCountPreference;
      if (settings.PreferMultiChannelAudio)
        channelCountPreference = (a => a.ChannelCount > 2); // Prefer more then stereo (usually 6ch)
      else
        channelCountPreference = (a => a.ChannelCount <= 2); // Stereo or even mono

      // Check if there are multiple audio streams for the PreferredAudioLanguage.
      int preferredAudioLCID = settings.PreferredAudioLanguage;

      List<StreamInfo> streamsForLCID = audioStreams.ToList().FindAll(a => a.LCID == preferredAudioLCID && a.LCID != 0);
      int count = streamsForLCID.Count;
      if (count > 0)
      {
        // If we have only one choice, select this stream.
        if (count == 1)
        {
          audioStreams.EnableStream(streamsForLCID[0].Name);
          return;
        }

        StreamInfo bestChannelStream = streamsForLCID.Find(channelCountPreference);
        if (bestChannelStream != null)
        {
          audioStreams.EnableStream(bestChannelStream.Name);
          return;
        }
      }

      // If we did not find matching languages by LCID no try to find them by name.
      StreamInfo streamInfo = null;
      if (preferredAudioLCID != 0)
      {
        CultureInfo ci = new CultureInfo(preferredAudioLCID);
        string languagePart = ci.EnglishName.Substring(0, ci.EnglishName.IndexOf("(") - 1);
        streamInfo = audioStreams.FindSimilarStream(languagePart);
      }

      // Still no matching languages? Then select the first that matches channelCountPreference.
      if (streamInfo == null)
        streamInfo = audioStreams.ToList().Find(channelCountPreference);

      if (streamInfo != null)
        audioStreams.EnableStream(streamInfo.Name);
      else
        if (useFirstAsDefault)
          audioStreams.EnableStream(audioStreams[0].Name);
    }

    public virtual void SetAudioStream(string audioStream)
    {
      StreamInfoHandler audioStreams;
      lock (SyncObj)
        audioStreams = _streamInfoAudio;

      if (audioStreams == null)
        return;

      if (audioStreams.EnableStream(audioStream))
      {
        int lcid = audioStreams.CurrentStream.LCID;
        if (lcid != 0)
        {
          VideoSettings settings = ServiceRegistration.Get<ISettingsManager>().Load<VideoSettings>() ?? new VideoSettings();
          settings.PreferredAudioLanguage = lcid;
          ServiceRegistration.Get<ISettingsManager>().Save(settings);
        }
      }
    }

    public virtual string CurrentAudioStream
    {
      get
      {
        StreamInfoHandler audioStreams;
        lock (SyncObj)
          audioStreams = _streamInfoAudio;

        return audioStreams == null ? null : audioStreams.CurrentStreamName;
      }
    }

    public virtual string[] AudioStreams
    {
      get
      {
        EnumerateStreams();
        StreamInfoHandler audioStreams;
        lock (SyncObj)
          audioStreams = _streamInfoAudio;

        return audioStreams == null ? DEFAULT_AUDIO_STREAM_NAMES : audioStreams.GetStreamNames();
      }
    }

    /// <summary>
    /// Enumerates streams from video (audio, subtitles).
    /// </summary>
    /// <returns><c>true</c> if information has been changed.</returns>
    protected virtual bool EnumerateStreams()
    {
      return EnumerateStreams(false);
    }

    /// <summary>
    /// Enumerates streams from video (audio, subtitles).
    /// </summary>
    /// <param name="forceRefresh">Force refresh</param>
    /// <returns><c>true</c> if information has been changed.</returns>
    protected virtual bool EnumerateStreams(bool forceRefresh)
    {
      if (_graphBuilder == null || !_initialized)
        return false;

      StreamInfoHandler audioStreams;
      StreamInfoHandler subtitleStreams;
      StreamInfoHandler titleStreams;
      lock (SyncObj)
      {
        audioStreams = _streamInfoAudio;
        subtitleStreams = _streamInfoSubtitles;
        titleStreams = _streamInfoTitles;
      }
      if (forceRefresh || audioStreams == null || subtitleStreams == null || titleStreams == null)
      {
        audioStreams = new StreamInfoHandler();
        subtitleStreams = new StreamInfoHandler();
        titleStreams = new StreamInfoHandler();

        // Release stream selectors
        ReleaseStreamSelectors();
        _streamSelectors = FilterGraphTools.FindFiltersByInterface<IAMStreamSelect>(_graphBuilder);
        foreach (IAMStreamSelect streamSelector in _streamSelectors)
        {
          FilterInfo fi = FilterGraphTools.QueryFilterInfoAndFree(((IBaseFilter) streamSelector));
          int streamCount;
          streamSelector.Count(out streamCount);

          for (int i = 0; i < streamCount; ++i)
          {
            AMMediaType mediaType;
            AMStreamSelectInfoFlags selectInfoFlags;
            int groupNumber, lcid;
            string name;
            object pppunk, ppobject;

            streamSelector.Info(i, out mediaType, out selectInfoFlags, out lcid, out groupNumber, out name, out pppunk, out ppobject);

            // If stream does not contain a LCID, try a lookup from stream name.
            if (lcid == 0)
              lcid = LookupLcidFromName(name);

            ServiceRegistration.Get<ILogger>().Debug("Stream {4}|{0}: MajorType {1}; Name {2}; PWDGroup: {3}; LCID: {5}",
              i, mediaType.majorType, name, groupNumber, fi.achName, lcid);

            StreamInfo currentStream = new StreamInfo(streamSelector, i, name, lcid);
            switch ((StreamGroup) groupNumber)
            {
              case StreamGroup.Video:
                break;
              case StreamGroup.Audio:
                if (mediaType.majorType == MediaType.AnalogAudio || mediaType.majorType == MediaType.Audio)
                {
                  String streamName = name.Trim();
                  String streamAppendix;
                  if (!CodecHandler.MediaSubTypes.TryGetValue(mediaType.subType, out streamAppendix))
                    streamAppendix = string.Empty;

                  // if audio information is available via WaveEx format, query the channel count
                  if (mediaType.formatType == FormatType.WaveEx && mediaType.formatPtr != IntPtr.Zero)
                  {
                    WaveFormatEx waveFormatEx = (WaveFormatEx) Marshal.PtrToStructure(mediaType.formatPtr, typeof(WaveFormatEx));
                    currentStream.ChannelCount = waveFormatEx.nChannels;
                    streamAppendix = String.Format("{0} {1}ch", streamAppendix, currentStream.ChannelCount);
                  }

                  if (!string.IsNullOrEmpty(streamAppendix))
                    currentStream.Name = String.Format("{0} ({1})", streamName, streamAppendix);

                  audioStreams.AddUnique(currentStream);
                }
                break;
              case StreamGroup.Subtitle:
              case StreamGroup.DirectVobSubtitle:
                subtitleStreams.AddUnique(currentStream, true);
                break;
              case StreamGroup.MatroskaEdition: // This is a MKV Edition handled by Haali splitter
                titleStreams.AddUnique(currentStream, true);
                break;
            }
            // Free MediaType and references
            FilterGraphTools.FreeAMMediaType(mediaType);
          }
        }

        lock (SyncObj)
        {
          _streamInfoAudio = audioStreams;
          _streamInfoSubtitles = subtitleStreams;
          _streamInfoTitles = titleStreams;
        }
        return true;
      }
      return false;
    }
    
    protected virtual void EnumerateChapters()
    {
      EnumerateChapters(false);
    }

    protected virtual void EnumerateChapters(bool forceRefresh)
    {
      if (_graphBuilder == null || !_initialized || !forceRefresh && _chapterTimestamps != null)
        return;

      // Try to find a filter implementing IAMExtendSeeking for chapter support
      IAMExtendedSeeking extendSeeking = FilterGraphTools.FindFilterByInterface<IAMExtendedSeeking>(_graphBuilder);
      if (extendSeeking == null)
        return;
      try
      {
        int markerCount;
        if (extendSeeking.get_MarkerCount(out markerCount) != 0 || markerCount <= 0)
          return;

        _chapterTimestamps = new double[markerCount];
        _chapterNames = new string[markerCount];
        for (int i = 1; i <= markerCount; i++)
        {
          double markerTime;
          string markerName;
          extendSeeking.GetMarkerTime(i, out markerTime);
          extendSeeking.GetMarkerName(i, out markerName);

          _chapterTimestamps[i - 1] = markerTime;
          _chapterNames[i - 1] = !string.IsNullOrEmpty(markerName) ? markerName : GetChapterName(i);
        }
      }
      finally
      {
        Marshal.ReleaseComObject(extendSeeking);
      }
    }

    #endregion
    
    public virtual void ReleaseGUIResources()
    {
      // Releases all Direct3D related resources
      _initialized = false;

      FilterState state;
      IMediaControl mc = (IMediaControl) _graphBuilder;
      mc.GetState(10, out state);
      if (state != FilterState.Stopped)
      {
        mc.StopWhenReady();
        mc.Stop();
      }

      if (_evr != null)
      {
        // Get the currently connected EVR Pins to restore the connections later
        FilterGraphTools.GetConnectedPins(_evr, PinDirection.Input, _evrConnectionPins);
        _graphBuilder.RemoveFilter(_evr);
        FilterGraphTools.TryRelease(ref _evr);
      }

      EvrDeinit(_presenterInstance);
      FreeEvrCallback();
    }

    protected virtual void CreateEvrCallback()
    {
      _evrCallback = new EVRCallback(RenderFrame, OnTextureInvalidated);
      _evrCallback.VideoSizePresent += OnVideoSizePresent;
    }

    protected virtual void FreeEvrCallback()
    {
      if (_evrCallback != null)
        _evrCallback.Dispose();
      _evrCallback = null;
    }

    public virtual void ReallocGUIResources()
    {
      if (_graphBuilder == null)
        return;

      CreateEvrCallback();
      AddEvr();
      FilterGraphTools.RestorePinConnections(_graphBuilder, _evr, PinDirection.Input, _evrConnectionPins);

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

    public bool SetRenderDelegate(SkinEngine.Players.RenderDlgt dlgt)
    {
      _renderDlgt = dlgt;
      return true;
    }

    public Rectangle CropVideoRect
    {
      get
      {
        Size videoSize = VideoSize;
        return _cropSettings == null ? new Rectangle(Point.Empty, videoSize) : _cropSettings.CropRect(videoSize);
      }
    }

    #endregion

    #region ISubtitlePlayer implementation

    protected virtual void SetPreferredSubtitle()
    {
      StreamInfoHandler subtitleStreams;
      lock (SyncObj)
        subtitleStreams = _streamInfoSubtitles;

      if (subtitleStreams == null)
        return;

      VideoSettings settings = ServiceRegistration.Get<ISettingsManager>().Load<VideoSettings>() ?? new VideoSettings();

      // first try to find a stream by it's exact LCID.
      StreamInfo streamInfo = subtitleStreams.FindStream(settings.PreferredSubtitleLanguage) ?? subtitleStreams.FindSimilarStream(settings.PreferredSubtitleSteamName);
      if (streamInfo == null || !settings.EnableSubtitles)
        subtitleStreams.EnableStream(NO_SUBTITLES);
      else
        subtitleStreams.EnableStream(streamInfo.Name);
    }

    /// <summary>
    /// Returns list of available subtitle streams.
    /// </summary>
    public virtual string[] Subtitles
    {
      get
      {
        EnumerateStreams();
        StreamInfoHandler subtitleStreams;
        lock (SyncObj)
          subtitleStreams = _streamInfoSubtitles;

        if (subtitleStreams == null)
          return EMPTY_STRING_ARRAY;

        // Check if there are real subtitle streams available. If not, the splitter only offers "No subtitles".
        string[] subtitleStreamNames = subtitleStreams.GetStreamNames();
        return subtitleStreamNames.Length == 1 && subtitleStreamNames[0] == NO_SUBTITLES
                 ? EMPTY_STRING_ARRAY
                 : subtitleStreamNames;
      }
    }

    /// <summary>
    /// Sets the current subtitle stream.
    /// </summary>
    /// <param name="subtitle">subtitle stream</param>
    public virtual void SetSubtitle(string subtitle)
    {
      StreamInfoHandler subtitleStreams;
      lock (SyncObj)
        subtitleStreams = _streamInfoSubtitles;

      if (subtitleStreams == null)
        return;

      if (subtitleStreams.EnableStream(subtitle))
        SaveSubtitlePreference();
    }

    protected virtual void SaveSubtitlePreference()
    {
      StreamInfoHandler subtitleStreams;
      lock (SyncObj)
        subtitleStreams = _streamInfoSubtitles;

      if (subtitleStreams == null)
        return;

      VideoSettings settings = ServiceRegistration.Get<ISettingsManager>().Load<VideoSettings>() ?? new VideoSettings();
      settings.PreferredSubtitleSteamName = subtitleStreams.CurrentStreamName;
      // if the subtitle stream has proper LCID, remember it.
      int lcid = subtitleStreams.CurrentStream.LCID;
      if (lcid != 0)
        settings.PreferredSubtitleLanguage = lcid;

      // if selected stream is "No subtitles", we disable the setting
      settings.EnableSubtitles = subtitleStreams.CurrentStreamName != NO_SUBTITLES;
      ServiceRegistration.Get<ISettingsManager>().Save(settings);
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
        StreamInfoHandler subtitleStreams;
        lock (SyncObj)
          subtitleStreams = _streamInfoSubtitles;
        return subtitleStreams == null ? String.Empty : subtitleStreams.CurrentStreamName;
      }
    }

    #endregion

    #region IChapterPlayer implementation

    /// <summary>
    /// Gets a list of available chapters.
    /// </summary>
    public virtual string[] Chapters
    {
      get
      {
        EnumerateChapters();

        string[] chapters;
        lock (SyncObj)
          chapters = _chapterNames;

        return chapters ?? EMPTY_STRING_ARRAY;
      }
    }

    /// <summary>
    /// Sets the chapter to play.
    /// </summary>
    /// <param name="chapter">Chapter name</param>
    public virtual void SetChapter(string chapter)
    {
      string[] chapters = Chapters;
      for (int i = 0; i < chapters.Length; i++)
      {
        if (chapter == chapters[i])
        {
          SetChapterByIndex(i);
          return;
        }
      }
    }

    /// <summary>
    /// Indicate if chapters are available.
    /// </summary>
    public virtual bool ChaptersAvailable
    {
      get { return Chapters.Length > 1; }
    }

    /// <summary>
    /// Skip to next chapter.
    /// </summary>
    public virtual void NextChapter()
    {
      Int32 currentChapter;
      if (GetCurrentChapter(out currentChapter))
        SetChapterByIndex(currentChapter + 1);
    }

    /// <summary>
    /// Skip to previous chapter.
    /// </summary>
    public virtual void PrevChapter()
    {
      Int32 currentChapter;
      if (GetCurrentChapter(out currentChapter))
        SetChapterByIndex(Math.Max(currentChapter - 1, 0));
    }

    /// <summary>
    /// Gets the current chapter.
    /// </summary>
    public virtual string CurrentChapter
    {
      get
      {
        Int32 currentChapter;
        return GetCurrentChapter(out currentChapter) ? _chapterNames[currentChapter] : null;
      }
    }

    /// <summary>
    /// Gets the current chapter.
    /// </summary>
    protected virtual bool GetCurrentChapter(out Int32 chapterIndex)
    {
      double[] chapterTimestamps;
      double currentTimestamp = CurrentTime.TotalSeconds;
      lock (SyncObj)
        chapterTimestamps = _chapterTimestamps;

      if (chapterTimestamps != null)
        for (int c = chapterTimestamps.Length - 1; c >= 0; c--)
        {
          if (currentTimestamp > chapterTimestamps[c])
          {
            chapterIndex = c;
            return true;
          }
        }
      chapterIndex = 0;
      return false;
    }

    /// <summary>
    /// Seek to the beginning of the chapter to play.
    /// </summary>
    /// <param name="chapterIndex">0 based chapter number.</param>
    protected virtual void SetChapterByIndex(Int32 chapterIndex)
    {
      double[] chapterTimestamps;
      lock (SyncObj)
        chapterTimestamps = _chapterTimestamps;

      if (chapterIndex >= chapterTimestamps.Length || chapterIndex < 0)
        return;

      TimeSpan seekTo = TimeSpan.FromSeconds(chapterTimestamps[chapterIndex]);
      CurrentTime = seekTo;
    }

    /// <summary>
    /// Returns a localized chapter name.
    /// </summary>
    /// <param name="chapterNumber">0 based chapter number.</param>
    /// <returns>Localized chapter name.</returns>
    protected virtual string GetChapterName(int chapterNumber)
    {
      // Idea: we could scrape chapter names and store them in MediaAspects. When they are available, return the full names here.
      return ServiceRegistration.Get<ILocalization>().ToString(RES_PLAYBACK_CHAPTER, chapterNumber);
    }

    #endregion

    #region ITitlePlayer implementation

    public virtual string[] Titles
    {
      get
      {
        EnumerateStreams();
        StreamInfoHandler titleStreams;
        lock (SyncObj)
          titleStreams = _streamInfoTitles;

        // Check if there are real title streams available.
        if (titleStreams == null || titleStreams.Count == 0)
          return EMPTY_STRING_ARRAY;

        return titleStreams.GetStreamNames();
      }
    }

    /// <summary>
    /// Sets the current title.
    /// </summary>
    /// <param name="title">Title</param>
    public virtual void SetTitle(string title)
    {
      StreamInfoHandler titleStreams;
      lock (SyncObj)
        titleStreams = _streamInfoTitles;

      // Check if there are real title streams available.
      if (titleStreams == null || titleStreams.Count == 0)
        return;

      if (!titleStreams.EnableStream(title))
        return;

      EnumerateStreams(true);
      EnumerateChapters(true);
    }

    public virtual string CurrentTitle
    {
      get
      {
        StreamInfoHandler titleStreams;
        lock (SyncObj)
          titleStreams = _streamInfoTitles;

        return titleStreams != null ? titleStreams.CurrentStreamName : String.Empty;
      }
    }

    #endregion

    #region Base overrides

    public override string ToString()
    {
      return string.Format("{0}: {1}", GetType().Name, _resourceAccessor != null ? _resourceAccessor.ResourceName : "no resource");
    }

    #endregion
  }
}
