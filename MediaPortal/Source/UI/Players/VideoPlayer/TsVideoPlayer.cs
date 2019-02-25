#region Copyright (C) 2007-2018 Team MediaPortal

/*
    Copyright (C) 2007-2018 Team MediaPortal
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
using System.Linq;
using System.Runtime.InteropServices;
using DirectShow;
using DirectShow.Helper;
using MediaPortal.Common;
using MediaPortal.Common.Localization;
using MediaPortal.Common.Logging;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Common.ResourceAccess;
using MediaPortal.Common.Settings;
using MediaPortal.UI.Players.Video.Interfaces;
using MediaPortal.UI.Players.Video.Settings;
using MediaPortal.UI.Players.Video.Subtitles;
using MediaPortal.UI.Players.Video.Teletext;
using MediaPortal.UI.Players.Video.Tools;
using MediaPortal.Utilities.Exceptions;
using SharpDX.Direct3D9;

namespace MediaPortal.UI.Players.Video
{
  public class TsVideoPlayer : VideoPlayer, ITsReaderCallback, ITsReaderCallbackAudioChange
  {
    #region Imports

    [ComImport, Guid(TSREADER_CLSID)]
    protected class TsReader { }

    #endregion

    #region Constants and structs

    public const string TSREADER_CLSID = "b9559486-E1BB-45D3-A2A2-9A7AFE49B23F";
    private const string TSREADER_FILTER_NAME = "TsReader";

    #endregion

    #region Variables

    protected FilterFileWrapper _sourceFilter = null;
    protected ISubtitleRenderer _subtitleRenderer;
    protected IBaseFilter _subtitleFilter;
    protected ITsReader _tsReader;
    protected ChangedMediaType _changedMediaType;
    protected string _oldVideoFormat;
    protected LocalFsResourceAccessorHelper _localFsRaHelper;

    #endregion

    #region Constructor
    /// <summary>
    /// Constructs a TsReader player object.
    /// </summary>
    public TsVideoPlayer()
    {
      PlayerTitle = "TsVideoPlayer"; // for logging
    }
    #endregion

    #region Graph building

    /// <summary>
    /// Frees the audio/video codecs.
    /// </summary>
    protected override void FreeCodecs()
    {
      // Free subtitle filter
      FilterGraphTools.TryDispose(ref _subtitleRenderer);
      FilterGraphTools.TryRelease(ref _subtitleFilter);

      // Free locally mounted remote resources
      FilterGraphTools.TryDispose(ref _localFsRaHelper);

      // Free base class
      base.FreeCodecs();

      // Free file source
      FilterGraphTools.TryDispose(ref _sourceFilter);
    }

    /// <summary>
    /// Adds the TsReader filter to the graph.
    /// </summary>
    protected override void AddSourceFilter()
    {
      _sourceFilter = FilterLoader.LoadFilterFromDll("TsReader.ax", typeof(TsReader).GUID, true);
      var baseFilter = _sourceFilter.GetFilter();

      IFileSourceFilter fileSourceFilter = (IFileSourceFilter)baseFilter;
      _tsReader = (ITsReader)baseFilter;
      _tsReader.SetRelaxedMode(1);
      _tsReader.SetTsReaderCallback(this);
      _tsReader.SetRequestAudioChangeCallback(this);

      _graphBuilder.AddFilter(baseFilter, TSREADER_FILTER_NAME);

      if (_resourceLocator.NativeResourcePath.IsNetworkResource)
      {
        // _resourceAccessor points to an rtsp:// stream or network file
        var sourcePathOrUrl = SourcePathOrUrl;

        if (sourcePathOrUrl == null)
          throw new IllegalCallException("The TsVideoPlayer can only play network resources of type INetworkResourceAccessor");

        ServiceRegistration.Get<ILogger>().Debug("{0}: Initializing for stream '{1}'", PlayerTitle, sourcePathOrUrl);

        IDisposable accessEnsurer = null;
        if (IsLocalFilesystemResource)
          accessEnsurer = ((ILocalFsResourceAccessor)_resourceAccessor).EnsureLocalFileSystemAccess();
        using (accessEnsurer)
        {
          int hr = fileSourceFilter.Load(SourcePathOrUrl, null);
          new HRESULT(hr).Throw();
        }
      }
      else
      {
        // _resourceAccessor points to a local or remote mapped .ts file
        _localFsRaHelper = new LocalFsResourceAccessorHelper(_resourceAccessor);
        var localFileSystemResourceAccessor = _localFsRaHelper.LocalFsResourceAccessor;

        if (localFileSystemResourceAccessor == null)
          throw new IllegalCallException("The TsVideoPlayer can only play file resources of type ILocalFsResourceAccessor");

        using (localFileSystemResourceAccessor.EnsureLocalFileSystemAccess())
        {
          ServiceRegistration.Get<ILogger>().Debug("{0}: Initializing for stream '{1}'", PlayerTitle, localFileSystemResourceAccessor.LocalFileSystemPath);
          int hr = fileSourceFilter.Load(localFileSystemResourceAccessor.LocalFileSystemPath, null);
          new HRESULT(hr).Throw();
        }
      }
      // Init GraphRebuilder
      _graphRebuilder = new GraphRebuilder(_graphBuilder, baseFilter, OnAfterGraphRebuild) { PlayerName = PlayerTitle };
    }

    protected override void AddSubtitleFilter(bool isSourceFilterPresent)
    {
      VideoSettings settings = ServiceRegistration.Get<ISettingsManager>().Load<VideoSettings>() ?? new VideoSettings();

      ISubtitleStream subtitleStream = _tsReader as ISubtitleStream;
      int subtitleStreamCount = 0;
      subtitleStream?.GetSubtitleStreamCount(ref subtitleStreamCount);

      ITeletextSource teletextSource = _tsReader as ITeletextSource;
      int teletextStreamCount = 0;
      teletextSource?.GetTeletextStreamCount(ref teletextStreamCount);

      bool shouldAddDvbFilter = isSourceFilterPresent && subtitleStreamCount >= 1 && settings.EnableDvbSubtitles;
      bool shouldRenderTeletextSubtitles = isSourceFilterPresent && teletextStreamCount >= 1 && settings.EnableTeletextSubtitles;
      bool shouldAddClosedCaptionsFilter = settings.EnableAtscClosedCaptions && (subtitleStreamCount == 0 || teletextStreamCount == 0);
      if (shouldAddDvbFilter)
      {
        _subtitleRenderer.SetPlayer(this);
        _subtitleFilter = _subtitleRenderer.AddDvbSubtitleFilter(_graphBuilder);
        if (_subtitleFilter != null)
        {
          _subtitleRenderer.RenderSubtitles = true;
        }
      }
      else if (shouldRenderTeletextSubtitles)
      {
        _subtitleRenderer.AddTeletextSubtitleDecoder(teletextSource);
        _subtitleRenderer.SetPlayer(this);
        _subtitleRenderer.RenderSubtitles = true;
      }
      else if (shouldAddClosedCaptionsFilter)
      {
        _subtitleRenderer.AddClosedCaptionsFilter(_graphBuilder);
        _closedCaptionsFilterAdded = true;
      }
    }

    protected override void SetSubtitleRenderer()
    {
      _subtitleRenderer = new SubtitleRenderer(OnTextureInvalidated);
    }

    protected override void OnBeforeGraphRunning()
    {
      FilterGraphTools.RenderOutputPins(_graphBuilder, _sourceFilter.GetFilter());
      UpdateVideoFps();
    }

    /// <summary>
    /// Checks if the current MediaItem contains fps information, if not it tries to get it from
    /// the source filter.
    /// </summary>
    protected virtual void UpdateVideoFps()
    {
      IList<MultipleMediaItemAspect> videoAspects;
      // If there are VideoStreamAspects we don't need to fill it.
      if (_mediaItem == null || MediaItemAspect.TryGetAspects(_mediaItem.Aspects, VideoStreamAspect.Metadata, out videoAspects))
        return;

      using (DSFilter d = new DSFilter((IBaseFilter)_tsReader))
      {
        // Would release the filter which causes errors in later access (like stream enumeration)
        d.ReleaseOnDestroy = false;
        var videoOutPin = d.Pins.FirstOrDefault(p => p.Direction == PinDirection.Output && p.ConnectionMediaType?.majorType == MediaType.Video);
        if (videoOutPin != null)
        {
          const long nTenMillion = 10000000;
          long avgTimePerFrameHns = videoOutPin.ConnectionMediaType.GetFrameRate();
          if (avgTimePerFrameHns == 0)
            return;

          float fps = (float)nTenMillion / avgTimePerFrameHns;

          MultipleMediaItemAspect videoStreamAspects = MediaItemAspect.CreateAspect(_mediaItem.Aspects, VideoStreamAspect.Metadata);
          videoStreamAspects.SetAttribute(VideoStreamAspect.ATTR_RESOURCE_INDEX, 0);
          videoStreamAspects.SetAttribute(VideoStreamAspect.ATTR_STREAM_INDEX, 0);
          videoStreamAspects.SetAttribute(VideoStreamAspect.ATTR_FPS, fps);
        }
      }
    }

    #endregion

    #region ITSReaderCallback members

    /// <summary>
    /// Callback when MediaType has changed.
    /// </summary>
    /// <param name="mediaType">new MediaType</param>
    /// <returns>0</returns>
    public int OnMediaTypeChanged(ChangedMediaType mediaType)
    {
      // Graph cannot be rebuilt inside this callback, it would lead to deadlocking when accessing the graphbuilder for rebuild.
      _graphRebuilder.DoAsynchRebuild();
      _changedMediaType = mediaType;
      return 0;
    }

    /// <summary>
    /// Informs the ITsReader that the graph rebuild was done.
    /// </summary>
    protected void OnAfterGraphRebuild()
    {
      _tsReader.OnGraphRebuild(_changedMediaType);
    }

    /// <summary>
    /// Callback when VideoFormat changed.
    /// </summary>
    /// <param name="streamType"></param>
    /// <param name="width"></param>
    /// <param name="height"></param>
    /// <param name="aspectRatioX"></param>
    /// <param name="aspectRatioY"></param>
    /// <param name="bitrate"></param>
    /// <param name="isInterlaced"></param>
    /// <returns></returns>
    public int OnVideoFormatChanged(int streamType, int width, int height, int aspectRatioX, int aspectRatioY, int bitrate, int isInterlaced)
    {
      string newFormat = string.Format("StreamType: {0} {1}x{2} [{3}/{4} @ {5} interlaced: {6}]",
                                       streamType, width, height, aspectRatioX, aspectRatioY, bitrate, isInterlaced);

      ServiceRegistration.Get<ILogger>().Debug("{0}: OnVideoFormatChanged: {1}", PlayerTitle, newFormat);

      if (!string.IsNullOrEmpty(_oldVideoFormat) && newFormat != _oldVideoFormat)
      {
        // Check for new audio/subtitle streams.
        EnumerateStreams(true);
      }
      SetPreferredSubtitle();
      _oldVideoFormat = newFormat;
      return 0;
    }

    public int OnBitRateChanged(int bitrate)
    {
      return 0;
    }

    #endregion

    #region ITSReaderAudioCallback members

    /// <summary>
    /// Callback when Audio change is requested.
    /// </summary>
    /// <returns></returns>
    public int OnRequestAudioChange()
    {
      // This is a special workaround for enumerating streams the first time: the callback happens before _initialized is set usually set to true (in AddFileSource).
      _initialized = true;

      EnumerateStreams(true); // Force re-enumerating of audio streams before selecting new stream
      SetPreferredAudio(true);
      SetPreferredSubtitle();
      return 0;
    }

    #endregion

    #region Subtitles

    protected override bool EnumerateStreams(bool forceRefresh)
    {
      bool refreshed = base.EnumerateStreams(forceRefresh);
      if (refreshed)
      {
        ISubtitleStream subtitleStream = _tsReader as ISubtitleStream;
        int subtitleStreamCount = 0;
        subtitleStream?.GetSubtitleStreamCount(ref subtitleStreamCount);

        if (subtitleStreamCount >= 1)
        {
          _streamInfoSubtitles = new TsReaderStreamInfoHandler(subtitleStream);
          SetPreferredSubtitle();
          return true;
        }
        ITeletextSource teletextSource = _tsReader as ITeletextSource;
        int teletextStreamCount = 0;
        teletextSource?.GetTeletextStreamCount(ref teletextStreamCount);
        if (teletextStreamCount >= 1)
        {
          _streamInfoSubtitles = new TsReaderTeletextInfoHandler(teletextSource);
          SetPreferredSubtitle();
          return true;
        }
      }
      return false;
    }

    public override void SetSubtitle(string subtitle)
    {
      EnumerateStreams();
      if (_streamInfoSubtitles is TsReaderStreamInfoHandler tsStreamInfoHandler)
      {
        _streamInfoSubtitles.EnableStream(subtitle);
        _subtitleRenderer.RenderSubtitles = !tsStreamInfoHandler.DisableSubs;
        SaveSubtitlePreference();
      }
      else if (_streamInfoSubtitles is TsReaderTeletextInfoHandler tsTeletextInfoHandler)
      {
        _streamInfoSubtitles.EnableStream(subtitle);
        _subtitleRenderer.RenderSubtitles = !tsTeletextInfoHandler.DisableSubs;
        SaveSubtitlePreference();
      }
    }

    protected override void SaveSubtitlePreference()
    {
      BaseStreamInfoHandler subtitleStreams;
      lock (SyncObj)
      {
        subtitleStreams = _streamInfoSubtitles;
      }
      if (subtitleStreams == null)
      {
        return;
      }

      VideoSettings settings = ServiceRegistration.Get<ISettingsManager>().Load<VideoSettings>() ?? new VideoSettings();
      settings.PreferredSubtitleStreamName = subtitleStreams.CurrentStreamName;
      // if the subtitle stream has proper LCID, remember it.
      int lcid = subtitleStreams.CurrentStream.IsAutoSubtitle ? 0 : subtitleStreams.CurrentStream.LCID;
      if (lcid != 0)
      {
        settings.PreferredSubtitleLanguage = lcid;
      }

      if (subtitleStreams is TsReaderStreamInfoHandler)
      {
        // if selected stream is "No subtitles" or "forced subtitle", we disable the setting
        settings.EnableDvbSubtitles = subtitleStreams.CurrentStreamName.ToLowerInvariant().Contains(GetNoSubsName().ToLowerInvariant()) == false &&
                                      subtitleStreams.CurrentStreamName.ToLowerInvariant().Contains(FORCED_SUBTITLES.ToLowerInvariant()) == false;
      }
      else if(subtitleStreams is TsReaderTeletextInfoHandler)
      {
        // if selected stream is "No subtitles" or "forced subtitle", we disable the setting
        settings.EnableTeletextSubtitles = subtitleStreams.CurrentStreamName.ToLowerInvariant().Contains(GetNoSubsName().ToLowerInvariant()) == false &&
                                      subtitleStreams.CurrentStreamName.ToLowerInvariant().Contains(FORCED_SUBTITLES.ToLowerInvariant()) == false;
      }
      ServiceRegistration.Get<ISettingsManager>().Save(settings);
    }

    protected override void SetPreferredSubtitle()
    {
      ISubtitleStream subtitleStream = _tsReader as ISubtitleStream;
      ITeletextSource teletextSource = _tsReader as ITeletextSource;
      if (_streamInfoSubtitles == null || (subtitleStream == null && teletextSource == null))
      {
        return;
      }

      VideoSettings settings = ServiceRegistration.Get<ISettingsManager>().Load<VideoSettings>() ?? new VideoSettings();

      // first try to find a stream by it's exact LCID.
      StreamInfo streamInfo = _streamInfoSubtitles.FindStream(settings.PreferredSubtitleLanguage) ?? _streamInfoSubtitles.FindSimilarStream(settings.PreferredSubtitleStreamName);
      if (streamInfo == null || !settings.EnableDvbSubtitles || !settings.EnableTeletextSubtitles)
      {
        // Tell the renderer it should not render subtitles
        if (_subtitleRenderer != null)
          _subtitleRenderer.RenderSubtitles = false;
      }
      else
      {
        _streamInfoSubtitles.EnableStream(streamInfo.Name);
      }
    }

    #endregion

    /// <summary>
    /// Render subtitles on video texture if enabled and available.
    /// </summary>
    protected override void PostProcessTexture(Texture targetTexture)
    {
      _subtitleRenderer.DrawOverlay(targetTexture);
    }

    public override TimeSpan CurrentTime
    {
      get { return base.CurrentTime; }
      set
      {
        base.CurrentTime = value;
        if (_subtitleRenderer != null)
          _subtitleRenderer.OnSeek(CurrentTime.TotalSeconds);
      }
    }
  }
}
