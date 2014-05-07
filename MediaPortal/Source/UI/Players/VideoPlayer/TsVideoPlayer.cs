#region Copyright (C) 2007-2014 Team MediaPortal

/*
    Copyright (C) 2007-2014 Team MediaPortal
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
using System.Runtime.InteropServices;
using DirectShow;
using DirectShow.Helper;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Common.ResourceAccess;
using MediaPortal.Common.Settings;
using MediaPortal.UI.Players.Video.Interfaces;
using MediaPortal.UI.Players.Video.Settings;
using MediaPortal.UI.Players.Video.Subtitles;
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
    private const int NO_STREAM_INDEX = -1;

    #endregion

    #region Variables

    protected IBaseFilter _sourceFilter = null;
    protected SubtitleRenderer _subtitleRenderer;
    protected IBaseFilter _subtitleFilter;
    protected GraphRebuilder _graphRebuilder;
    protected int _selectedSubtitleIndex = NO_STREAM_INDEX;
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
      FilterGraphTools.TryRelease(ref _sourceFilter);
    }

    /// <summary>
    /// Adds the TsReader filter to the graph.
    /// </summary>
    protected override void AddSourceFilter()
    {
      // Render the file
      _sourceFilter = FilterLoader.LoadFilterFromDll("TsReader.ax", typeof(TsReader).GUID, true);

      IFileSourceFilter fileSourceFilter = (IFileSourceFilter)_sourceFilter;
      ITsReader tsReader = (ITsReader)_sourceFilter;
      tsReader.SetRelaxedMode(1);
      tsReader.SetTsReaderCallback(this);
      tsReader.SetRequestAudioChangeCallback(this);

      _graphBuilder.AddFilter(_sourceFilter, TSREADER_FILTER_NAME);

      _subtitleRenderer = new SubtitleRenderer(OnTextureInvalidated);
      _subtitleFilter = _subtitleRenderer.AddSubtitleFilter(_graphBuilder);
      if (_subtitleFilter != null)
      {
        _subtitleRenderer.RenderSubtitles = true;
        _subtitleRenderer.SetPlayer(this);
      }

      if (_resourceLocator.NativeResourcePath.IsNetworkResource)
      {
        // _resourceAccessor points to an rtsp:// stream or network file
        var sourcePathOrUrl = SourcePathOrUrl;

        if (sourcePathOrUrl == null)
          throw new IllegalCallException("The TsVideoPlayer can only play network resources of type INetworkResourceAccessor");

        ServiceRegistration.Get<ILogger>().Debug("{0}: Initializing for stream '{1}'", PlayerTitle, sourcePathOrUrl);

        int hr = fileSourceFilter.Load(SourcePathOrUrl, null);
        new HRESULT(hr).Throw();
      }
      else
      {
        // _resourceAccessor points to a local or remote mapped .ts file
        _localFsRaHelper = new LocalFsResourceAccessorHelper(_resourceAccessor);
        var localFileSystemResourceAccessor = _localFsRaHelper.LocalFsResourceAccessor;

        if (localFileSystemResourceAccessor == null)
          throw new IllegalCallException("The TsVideoPlayer can only play file resources of type ILocalFsResourceAccessor");

        ServiceRegistration.Get<ILogger>().Debug("{0}: Initializing for stream '{1}'", PlayerTitle, localFileSystemResourceAccessor.LocalFileSystemPath);

        int hr = fileSourceFilter.Load(localFileSystemResourceAccessor.LocalFileSystemPath, null);
        new HRESULT(hr).Throw();
      }
      // Init GraphRebuilder
      _graphRebuilder = new GraphRebuilder(_graphBuilder, _sourceFilter, OnAfterGraphRebuild) { PlayerName = PlayerTitle };
    }

    protected override void OnBeforeGraphRunning()
    {
      FilterGraphTools.RenderOutputPins(_graphBuilder, _sourceFilter);
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
      ITsReader tsReader = (ITsReader)_sourceFilter;
      tsReader.OnGraphRebuild(_changedMediaType);
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

      SetPreferredAudio(true);
      return 0;
    }

    #endregion

    #region Subtitles

    protected override bool EnumerateStreams(bool forceRefresh)
    {
      //FIXME: TSReader only offers Audio in IAMStreamSelect, it would be cleaner to expose subs as well.
      bool refreshed = base.EnumerateStreams(forceRefresh);
      if (refreshed)
      {
        // If base class has refreshed the stream infos, then update the subtitle streams.
        ISubtitleStream subtitleStream = _sourceFilter as ISubtitleStream;
        int count = 0;
        if (subtitleStream != null)
        {
          _streamInfoSubtitles = new StreamInfoHandler();
          subtitleStream.GetSubtitleStreamCount(ref count);
          if (count > 0)
          {
            StreamInfo subStream = new StreamInfo(null, NO_STREAM_INDEX, NO_SUBTITLES, 0);
            _streamInfoSubtitles.AddUnique(subStream);
          }
          for (int i = 0; i < count; ++i)
          {
            //FIXME: language should be passed back also as LCID
            SubtitleLanguage language = new SubtitleLanguage();
            subtitleStream.GetSubtitleStreamLanguage(i, ref language);
            int lcid = LookupLcidFromName(language.lang);
            // Note: the "type" is no longer considered in MP1 code as well, so I guess DVBSub3 only supports Bitmap subs at all.
            string name = language.lang;
            StreamInfo subStream = new StreamInfo(null, i, name, lcid);
            _streamInfoSubtitles.AddUnique(subStream);
          }
        }
      }
      return refreshed;
    }

    public override void SetSubtitle(string subtitle)
    {
      EnumerateStreams();
      ISubtitleStream subtitleStream = _sourceFilter as ISubtitleStream;
      if (_streamInfoSubtitles == null || subtitleStream == null)
        return;

      // First try to find a stream by it's exact LCID.
      StreamInfo streamInfo = _streamInfoSubtitles.FindStream(subtitle);
      if (streamInfo != null)
      {
        // Tell the renderer if it should render subtitles
        _selectedSubtitleIndex = streamInfo.StreamIndex;
        _subtitleRenderer.RenderSubtitles = _selectedSubtitleIndex != NO_STREAM_INDEX;
        if (_selectedSubtitleIndex != NO_STREAM_INDEX)
          subtitleStream.SetSubtitleStream(_selectedSubtitleIndex);
        SaveSubtitlePreference();
      }
    }

    protected override void SaveSubtitlePreference()
    {
      VideoSettings settings = ServiceRegistration.Get<ISettingsManager>().Load<VideoSettings>() ?? new VideoSettings();
      settings.PreferredSubtitleSteamName = _selectedSubtitleIndex != NO_STREAM_INDEX
        ? Subtitles[_selectedSubtitleIndex] : String.Empty;

      // If selected stream is "No subtitles", we disable the setting
      settings.EnableSubtitles = _selectedSubtitleIndex != NO_STREAM_INDEX;
      ServiceRegistration.Get<ISettingsManager>().Save(settings);
    }

    protected override void SetPreferredSubtitle()
    {
      EnumerateStreams();
      ISubtitleStream subtitleStream = _sourceFilter as ISubtitleStream;
      if (_streamInfoSubtitles == null || subtitleStream == null)
        return;

      VideoSettings settings = ServiceRegistration.Get<ISettingsManager>().Load<VideoSettings>() ?? new VideoSettings();

      // first try to find a stream by it's exact LCID.
      StreamInfo streamInfo = _streamInfoSubtitles.FindStream(settings.PreferredSubtitleLanguage) ?? _streamInfoSubtitles.FindSimilarStream(settings.PreferredSubtitleSteamName);
      if (streamInfo == null || !settings.EnableSubtitles)
        // Tell the renderer it should not render subtitles
        _subtitleRenderer.RenderSubtitles = false;
      else
        subtitleStream.SetSubtitleStream(streamInfo.StreamIndex);
    }

    #endregion

    /// <summary>
    /// Render subtitles on video texture if enabled and available.
    /// </summary>
    protected override void PostProcessTexture(Surface targetSurface)
    {
      _subtitleRenderer.DrawOverlay(targetSurface);
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
