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
using System.Threading;
using System.Threading.Tasks;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Common.PathManager;
using MediaPortal.Common.PluginManager;
using MediaPortal.Common.ResourceAccess;
using MediaPortal.Common.Services.Logging;
using MediaPortal.Common.Settings;
using MediaPortal.Common.MediaManagement.Helpers;
using MediaPortal.Extensions.MetadataExtractors.StubMetadataExtractors.Settings;
using MediaPortal.Extensions.MetadataExtractors.StubMetadataExtractors.StubReaders;
using MediaPortal.Extensions.MetadataExtractors.StubMetadataExtractors.Stubs;

namespace MediaPortal.Extensions.MetadataExtractors.StubMetadataExtractors
{
  /// <summary>
  /// MediaPortal 2 metadata extractor for series reading from local stub-files.
  /// </summary>
  public class StubSeriesMetadataExtractor : IMetadataExtractor, IDisposable
  {
    #region Constants / Static fields

    /// <summary>
    /// GUID of the NfoMetadataExtractors plugin
    /// </summary>
    public const string PLUGIN_ID_STR = "A33319F7-D311-44A9-BE7E-2F0E88AC4EEF";
    public static readonly Guid PLUGIN_ID = new Guid(PLUGIN_ID_STR);

    /// <summary>
    /// GUID for the NfoSeriesMetadataExtractor
    /// </summary>
    public const string METADATAEXTRACTOR_ID_STR = "CCE300F5-755B-4744-B5B4-084BA2999374";
    public static readonly Guid METADATAEXTRACTOR_ID = new Guid(METADATAEXTRACTOR_ID_STR);

    /// <summary>
    /// MediaCategories this MetadataExtractor is applied to
    /// </summary>
    private const string MEDIA_CATEGORY_NAME_SERIES = "Series";
    private readonly static ICollection<MediaCategory> MEDIA_CATEGORIES = new List<MediaCategory>();

    #endregion

    #region Private fields

    /// <summary>
    /// Metadata of this MetadataExtractor
    /// </summary>
    private readonly MetadataExtractorMetadata _metadata;

    /// <summary>
    /// Settings of the <see cref="StubSeriesMetadataExtractor"/>
    /// </summary>
    private readonly StubSeriesMetadataExtractorSettings _settings;
    
    /// <summary>
    /// Debug logger
    /// </summary>
    /// <remarks>
    /// NoLogger if _settings.EnableDebugLogging == <c>false</c>"/>
    /// FileLogger if _settings.EnableDebugLogging == <c>true</c>"/>
    /// </remarks>
    private readonly ILogger _debugLogger;

    /// <summary>
    /// Unique number of the last MediaItem for which this MetadataExtractor was called
    /// </summary>
    private long _lastMediaItemNumber = 1;

    #endregion

    #region Ctor

    /// <summary>
    /// Initializes <see cref="MEDIA_CATEGORIES"/> and, if necessary, registers the "Series" <see cref="MediaCategory"/>
    /// </summary>
    static StubSeriesMetadataExtractor()
    {
      MediaCategory seriesCategory;
      var mediaAccessor = ServiceRegistration.Get<IMediaAccessor>();
      if (!mediaAccessor.MediaCategories.TryGetValue(MEDIA_CATEGORY_NAME_SERIES, out seriesCategory))
        seriesCategory = mediaAccessor.RegisterMediaCategory(MEDIA_CATEGORY_NAME_SERIES, new List<MediaCategory> { DefaultMediaCategories.Video });
      MEDIA_CATEGORIES.Add(seriesCategory);
    }

    /// <summary>
    /// Instantiates a new <see cref="NfoSeriesMetadataExtractor"/> object
    /// </summary>
    public StubSeriesMetadataExtractor()
    {
      // The metadataExtractorPriority is intentionally set wrong to "Extended" although, depending on the
      // content of the nfo-file, it may download thumbs from the internet (and should therefore be
      // "External"). This is a temporary workaround for performance purposes. It ensures that this 
      // MetadataExtractor is applied before the VideoThumbnailer (which is intentionally set to "External"
      // although it only uses local files). Creating thumbs with the VideoThumbnailer takes much longer
      // than downloading them from the internet.
      // ToDo: Correct this once we have a better priority system
      _metadata = new MetadataExtractorMetadata(
        metadataExtractorId: METADATAEXTRACTOR_ID,
        name: "Stub series metadata extractor",
        metadataExtractorPriority: MetadataExtractorPriority.Stub,
        processesNonFiles: true,
        shareCategories: MEDIA_CATEGORIES,
        extractedAspectTypes: new MediaItemAspectMetadata[]
        {
          MediaAspect.Metadata,
          EpisodeAspect.Metadata,
          StubAspect.Metadata
        });

      _settings = ServiceRegistration.Get<ISettingsManager>().Load<StubSeriesMetadataExtractorSettings>();

      if (_settings.EnableDebugLogging)
      {
        _debugLogger = FileLogger.CreateFileLogger(ServiceRegistration.Get<IPathManager>().GetPath(@"<LOG>\StubSeriesMetadataExtractorDebug.log"), LogLevel.Debug, false, true);
        LogSettings();
      }
      else
        _debugLogger = new NoLogger();
    }

    #endregion

    #region Logging helpers

    /// <summary>
    /// Logs version and setting information into <see cref="_debugLogger"/>
    /// </summary>
    private void LogSettings()
    {
      _debugLogger.Info("-------------------------------------------------------------");
      _debugLogger.Info("StubSeriesMetadataExtractor v{0} instantiated", ServiceRegistration.Get<IPluginManager>().AvailablePlugins[PLUGIN_ID].Metadata.PluginVersion);
      _debugLogger.Info("Setttings:");
      _debugLogger.Info("   EnableDebugLogging: {0}", _settings.EnableDebugLogging);
      _debugLogger.Info("   WriteRawNfoFileIntoDebugLog: {0}", _settings.WriteRawStubFileIntoDebugLog);
      _debugLogger.Info("   WriteStubObjectIntoDebugLog: {0}", _settings.WriteStubObjectIntoDebugLog);
      _debugLogger.Info("   SeriesBlurayStubFileExtensions: {0}", String.Join(" ", _settings.SeriesBlurayStubFileExtensions));
      _debugLogger.Info("   SeriesDvdStubFileExtensions: {0}", String.Join(" ", _settings.SeriesDvdStubFileExtensions));
      _debugLogger.Info("   SeriesHddvdStubFileExtensions: {0}", String.Join(" ", _settings.SeriesHddvdStubFileExtensions));
      _debugLogger.Info("   SeriesTvStubFileExtensions: {0}", String.Join(" ", _settings.SeriesTvStubFileExtensions));
      _debugLogger.Info("   SeriesVhsStubFileExtensions: {0}", String.Join(" ", _settings.SeriesVhsStubFileExtensions));
      _debugLogger.Info("   SeparatorCharacters: {0}", String.Join(" ", _settings.SeparatorCharacters));
      _debugLogger.Info("   IgnoreStrings: {0}", String.Join(";", _settings.IgnoreStrings));
      _debugLogger.Info("-------------------------------------------------------------");
    }

    #endregion

    #region IDisposable implementation

    public void Dispose()
    {
    }

    #endregion

    #region IMetadataExtractor implementation

    public MetadataExtractorMetadata Metadata
    {
      get { return _metadata; }
    }

    public Task<bool> TryExtractMetadataAsync(IResourceAccessor mediaItemAccessor, IDictionary<Guid, IList<MediaItemAspect>> extractedAspectData, bool forceQuickMode)
    {
      return Task.FromResult(false);
    }

    public bool IsDirectorySingleResource(IResourceAccessor mediaItemAccessor)
    {
      return false;
    }

    public Task<IList<MediaItemSearchResult>> SearchForMatchesAsync(IDictionary<Guid, IList<MediaItemAspect>> searchAspectData, ICollection<string> searchCategories)
    {
      return Task.FromResult<IList<MediaItemSearchResult>>(null);
    }

    public Task<bool> AddMatchedAspectDetailsAsync(IDictionary<Guid, IList<MediaItemAspect>> matchedAspectData)
    {
      return Task.FromResult(false);
    }

    public bool IsStubResource(IResourceAccessor mediaItemAccessor)
    {
      if (IsDvd(mediaItemAccessor))
        return true;
      if (IsBluray(mediaItemAccessor))
        return true;
      if (IsHdDvd(mediaItemAccessor))
        return true;
      if (IsTv(mediaItemAccessor))
        return true;
      if (IsVhs(mediaItemAccessor))
        return true;
      return false;
    }

    public bool TryExtractStubItems(IResourceAccessor mediaItemAccessor, ICollection<IDictionary<Guid, IList<MediaItemAspect>>> extractedStubAspectData)
    {
      // The following is bad practice as it wastes one ThreadPool thread.
      // ToDo: Once the IMetadataExtractor interface is updated to support async operations, call TryExtractMetadataAsync directly
      return TryExtractStubItemsAsync(mediaItemAccessor, extractedStubAspectData).Result;
    }

    private async Task<bool> TryExtractStubItemsAsync(IResourceAccessor mediaItemAccessor, ICollection<IDictionary<Guid, IList<MediaItemAspect>>> extractedStubAspectData)
    {
      // Get a unique number for this call to TryExtractMetadataAsync. We use this to make reading the debug log easier.
      // This MetadataExtractor is called in parallel for multiple MediaItems so that the respective debug log entries
      // for one call are not contained one after another in debug log. We therefore prepend this number before every log entry.
      var miNumber = Interlocked.Increment(ref _lastMediaItemNumber);
      try
      {
        _debugLogger.Info("[#{0}]: Start extracting stubs for resource '{1}'", miNumber, mediaItemAccessor);

        if (!IsStubResource(mediaItemAccessor))
        {
          _debugLogger.Info("[#{0}]: Cannot extract stubs; file does not have a supported extension", miNumber);
          return false;
        }

        // This MetadataExtractor only works for MediaItems accessible by an IFileSystemResourceAccessor.
        // Otherwise it is not possible to find a stub-file in the MediaItem's directory.
        if (!(mediaItemAccessor is IFileSystemResourceAccessor))
        {
          _debugLogger.Info("[#{0}]: Cannot extract stubs; mediaItemAccessor is not an IFileSystemResourceAccessor", miNumber);
          return false;
        }

        var fsra = mediaItemAccessor as IFileSystemResourceAccessor;
        var seriesStubReader = new StubSeriesReader(_debugLogger, miNumber, true, _settings);
        if (fsra != null && await seriesStubReader.TryReadMetadataAsync(fsra).ConfigureAwait(false))
        {
          SeriesStub series = seriesStubReader.GetSeriesStubs().FirstOrDefault();
          if (series != null && series.Episodes != null && series.Episodes.Count > 0)
          {
            Dictionary<Guid, IList<MediaItemAspect>> extractedAspectData = new Dictionary<Guid, IList<MediaItemAspect>>();
            string title = string.Format("{0} S{1:00}{2}", series.Title, series.Season.Value, string.Join("", series.Episodes.Select(e => "E"+ e.ToString("00"))));

            MultipleMediaItemAspect providerResourceAspect = MediaItemAspect.CreateAspect(extractedAspectData, ProviderResourceAspect.Metadata);
            providerResourceAspect.SetAttribute(ProviderResourceAspect.ATTR_RESOURCE_INDEX, 0);
            providerResourceAspect.SetAttribute(ProviderResourceAspect.ATTR_TYPE, ProviderResourceAspect.TYPE_STUB);
            providerResourceAspect.SetAttribute(ProviderResourceAspect.ATTR_RESOURCE_ACCESSOR_PATH, fsra.CanonicalLocalResourcePath.Serialize());
            if (IsVhs(mediaItemAccessor))
            {
              providerResourceAspect.SetAttribute(ProviderResourceAspect.ATTR_MIME_TYPE, "video/unknown");

              MultipleMediaItemAspect videoStreamAspects = MediaItemAspect.CreateAspect(extractedAspectData, VideoStreamAspect.Metadata);
              videoStreamAspects.SetAttribute(VideoStreamAspect.ATTR_RESOURCE_INDEX, 0);
              videoStreamAspects.SetAttribute(VideoStreamAspect.ATTR_STREAM_INDEX, 0);
              videoStreamAspects.SetAttribute(VideoStreamAspect.ATTR_VIDEO_TYPE, VideoStreamAspect.TYPE_SD);
              videoStreamAspects.SetAttribute(VideoStreamAspect.ATTR_ASPECTRATIO, Convert.ToSingle(4.0 / 3.0));
              videoStreamAspects.SetAttribute(VideoStreamAspect.ATTR_FPS, 25);
              videoStreamAspects.SetAttribute(VideoStreamAspect.ATTR_WIDTH, 720);
              videoStreamAspects.SetAttribute(VideoStreamAspect.ATTR_HEIGHT, 576);
              videoStreamAspects.SetAttribute(VideoStreamAspect.ATTR_AUDIOSTREAMCOUNT, 1);
              videoStreamAspects.SetAttribute(VideoStreamAspect.ATTR_VIDEO_PART, 1);

              MultipleMediaItemAspect audioAspect = MediaItemAspect.CreateAspect(extractedAspectData, VideoAudioStreamAspect.Metadata);
              audioAspect.SetAttribute(VideoAudioStreamAspect.ATTR_RESOURCE_INDEX, 0);
              audioAspect.SetAttribute(VideoAudioStreamAspect.ATTR_STREAM_INDEX, 1);
              audioAspect.SetAttribute(VideoAudioStreamAspect.ATTR_AUDIOCHANNELS, 2);
            }
            else if (IsTv(mediaItemAccessor))
            {
              providerResourceAspect.SetAttribute(ProviderResourceAspect.ATTR_MIME_TYPE, "video/unknown");

              MultipleMediaItemAspect videoStreamAspects = MediaItemAspect.CreateAspect(extractedAspectData, VideoStreamAspect.Metadata);
              videoStreamAspects.SetAttribute(VideoStreamAspect.ATTR_RESOURCE_INDEX, 0);
              videoStreamAspects.SetAttribute(VideoStreamAspect.ATTR_STREAM_INDEX, 0);
              videoStreamAspects.SetAttribute(VideoStreamAspect.ATTR_VIDEO_TYPE, VideoStreamAspect.TYPE_HD);
              videoStreamAspects.SetAttribute(VideoStreamAspect.ATTR_ASPECTRATIO, Convert.ToSingle(16.0 / 9.0));
              videoStreamAspects.SetAttribute(VideoStreamAspect.ATTR_FPS, 25F);
              videoStreamAspects.SetAttribute(VideoStreamAspect.ATTR_WIDTH, 1920);
              videoStreamAspects.SetAttribute(VideoStreamAspect.ATTR_HEIGHT, 1080);
              videoStreamAspects.SetAttribute(VideoStreamAspect.ATTR_AUDIOSTREAMCOUNT, 1);
              videoStreamAspects.SetAttribute(VideoStreamAspect.ATTR_VIDEO_PART, 1);

              MultipleMediaItemAspect audioAspect = MediaItemAspect.CreateAspect(extractedAspectData, VideoAudioStreamAspect.Metadata);
              audioAspect.SetAttribute(VideoAudioStreamAspect.ATTR_RESOURCE_INDEX, 0);
              audioAspect.SetAttribute(VideoAudioStreamAspect.ATTR_STREAM_INDEX, 1);
              audioAspect.SetAttribute(VideoAudioStreamAspect.ATTR_AUDIOCHANNELS, 2);
            }
            else if (IsDvd(mediaItemAccessor))
            {
              providerResourceAspect.SetAttribute(ProviderResourceAspect.ATTR_MIME_TYPE, "video/mp2t");

              MultipleMediaItemAspect videoStreamAspects = MediaItemAspect.CreateAspect(extractedAspectData, VideoStreamAspect.Metadata);
              videoStreamAspects.SetAttribute(VideoStreamAspect.ATTR_RESOURCE_INDEX, 0);
              videoStreamAspects.SetAttribute(VideoStreamAspect.ATTR_STREAM_INDEX, 0);
              videoStreamAspects.SetAttribute(VideoStreamAspect.ATTR_VIDEO_TYPE, VideoStreamAspect.TYPE_SD);
              videoStreamAspects.SetAttribute(VideoStreamAspect.ATTR_ASPECTRATIO, Convert.ToSingle(16.0 / 9.0));
              videoStreamAspects.SetAttribute(VideoStreamAspect.ATTR_FPS, 25F);
              videoStreamAspects.SetAttribute(VideoStreamAspect.ATTR_WIDTH, 720);
              videoStreamAspects.SetAttribute(VideoStreamAspect.ATTR_HEIGHT, 576);
              videoStreamAspects.SetAttribute(VideoStreamAspect.ATTR_VIDEOENCODING, "MPEG-2 Video");
              videoStreamAspects.SetAttribute(VideoStreamAspect.ATTR_AUDIOSTREAMCOUNT, 1);
              videoStreamAspects.SetAttribute(VideoStreamAspect.ATTR_VIDEO_PART, 1);

              MultipleMediaItemAspect audioAspect = MediaItemAspect.CreateAspect(extractedAspectData, VideoAudioStreamAspect.Metadata);
              audioAspect.SetAttribute(VideoAudioStreamAspect.ATTR_RESOURCE_INDEX, 0);
              audioAspect.SetAttribute(VideoAudioStreamAspect.ATTR_STREAM_INDEX, 1);
              audioAspect.SetAttribute(VideoAudioStreamAspect.ATTR_AUDIOENCODING, "AC3");
              audioAspect.SetAttribute(VideoAudioStreamAspect.ATTR_AUDIOCHANNELS, 6);
              audioAspect.SetAttribute(VideoAudioStreamAspect.ATTR_AUDIOSAMPLERATE, 48000L);
            }
            else if (IsBluray(mediaItemAccessor))
            {
              providerResourceAspect.SetAttribute(ProviderResourceAspect.ATTR_MIME_TYPE, "video/mp4");

              MultipleMediaItemAspect videoStreamAspects = MediaItemAspect.CreateAspect(extractedAspectData, VideoStreamAspect.Metadata);
              videoStreamAspects.SetAttribute(VideoStreamAspect.ATTR_RESOURCE_INDEX, 0);
              videoStreamAspects.SetAttribute(VideoStreamAspect.ATTR_STREAM_INDEX, 0);
              videoStreamAspects.SetAttribute(VideoStreamAspect.ATTR_VIDEO_TYPE, VideoStreamAspect.TYPE_HD);
              videoStreamAspects.SetAttribute(VideoStreamAspect.ATTR_ASPECTRATIO, Convert.ToSingle(16.0 / 9.0));
              videoStreamAspects.SetAttribute(VideoStreamAspect.ATTR_FPS, 24F);
              videoStreamAspects.SetAttribute(VideoStreamAspect.ATTR_WIDTH, 1920);
              videoStreamAspects.SetAttribute(VideoStreamAspect.ATTR_HEIGHT, 1080);
              videoStreamAspects.SetAttribute(VideoStreamAspect.ATTR_VIDEOENCODING, "AVC");
              videoStreamAspects.SetAttribute(VideoStreamAspect.ATTR_AUDIOSTREAMCOUNT, 1);
              videoStreamAspects.SetAttribute(VideoStreamAspect.ATTR_VIDEO_PART, 1);

              MultipleMediaItemAspect audioAspect = MediaItemAspect.CreateAspect(extractedAspectData, VideoAudioStreamAspect.Metadata);
              audioAspect.SetAttribute(VideoAudioStreamAspect.ATTR_RESOURCE_INDEX, 0);
              audioAspect.SetAttribute(VideoAudioStreamAspect.ATTR_STREAM_INDEX, 1);
              audioAspect.SetAttribute(VideoAudioStreamAspect.ATTR_AUDIOENCODING, "AC3");
              audioAspect.SetAttribute(VideoAudioStreamAspect.ATTR_AUDIOCHANNELS, 6);
              audioAspect.SetAttribute(VideoAudioStreamAspect.ATTR_AUDIOSAMPLERATE, 48000L);
            }
            else if (IsHdDvd(mediaItemAccessor))
            {
              providerResourceAspect.SetAttribute(ProviderResourceAspect.ATTR_MIME_TYPE, "video/wvc1");

              MultipleMediaItemAspect videoStreamAspects = MediaItemAspect.CreateAspect(extractedAspectData, VideoStreamAspect.Metadata);
              videoStreamAspects.SetAttribute(VideoStreamAspect.ATTR_RESOURCE_INDEX, 0);
              videoStreamAspects.SetAttribute(VideoStreamAspect.ATTR_STREAM_INDEX, 0);
              videoStreamAspects.SetAttribute(VideoStreamAspect.ATTR_VIDEO_TYPE, VideoStreamAspect.TYPE_HD);
              videoStreamAspects.SetAttribute(VideoStreamAspect.ATTR_ASPECTRATIO, Convert.ToSingle(16.0 / 9.0));
              videoStreamAspects.SetAttribute(VideoStreamAspect.ATTR_FPS, 24F);
              videoStreamAspects.SetAttribute(VideoStreamAspect.ATTR_WIDTH, 1920);
              videoStreamAspects.SetAttribute(VideoStreamAspect.ATTR_HEIGHT, 1080);
              videoStreamAspects.SetAttribute(VideoStreamAspect.ATTR_VIDEOENCODING, "VC1");
              videoStreamAspects.SetAttribute(VideoStreamAspect.ATTR_AUDIOSTREAMCOUNT, 1);
              videoStreamAspects.SetAttribute(VideoStreamAspect.ATTR_VIDEO_PART, 1);

              MultipleMediaItemAspect audioAspect = MediaItemAspect.CreateAspect(extractedAspectData, VideoAudioStreamAspect.Metadata);
              audioAspect.SetAttribute(VideoAudioStreamAspect.ATTR_RESOURCE_INDEX, 0);
              audioAspect.SetAttribute(VideoAudioStreamAspect.ATTR_STREAM_INDEX, 1);
              audioAspect.SetAttribute(VideoAudioStreamAspect.ATTR_AUDIOENCODING, "AC3");
              audioAspect.SetAttribute(VideoAudioStreamAspect.ATTR_AUDIOCHANNELS, 6);
              audioAspect.SetAttribute(VideoAudioStreamAspect.ATTR_AUDIOSAMPLERATE, 48000L);
            }

            SingleMediaItemAspect videoAspect = MediaItemAspect.GetOrCreateAspect(extractedAspectData, VideoAspect.Metadata);
            videoAspect.SetAttribute(VideoAspect.ATTR_ISDVD, true);

            SingleMediaItemAspect movieAspect = MediaItemAspect.GetOrCreateAspect(extractedAspectData, EpisodeAspect.Metadata);
            movieAspect.SetCollectionAttribute(EpisodeAspect.ATTR_EPISODE, series.Episodes);
            movieAspect.SetAttribute(EpisodeAspect.ATTR_SEASON, series.Season.Value);
            movieAspect.SetAttribute(EpisodeAspect.ATTR_EPISODE_NAME, string.Format("{0} {1}", "Episode", string.Join(", ", series.Episodes)));
            movieAspect.SetAttribute(EpisodeAspect.ATTR_SERIES_NAME, series.Title);
            movieAspect.SetAttribute(EpisodeAspect.ATTR_SERIES_SEASON, string.Format("{0} S{1:00}", series.Title, series.Season.Value));

            SingleMediaItemAspect stubAspect = MediaItemAspect.GetOrCreateAspect(extractedAspectData, StubAspect.Metadata);
            stubAspect.SetAttribute(StubAspect.ATTR_DISC_NAME, series.DiscName);
            stubAspect.SetAttribute(StubAspect.ATTR_MESSAGE, series.Message);

            MediaItemAspect.SetAttribute(extractedAspectData, MediaAspect.ATTR_TITLE, series.Title);
            MediaItemAspect.SetAttribute(extractedAspectData, MediaAspect.ATTR_SORT_TITLE, BaseInfo.GetSortTitle(series.Title));
            MediaItemAspect.SetAttribute(extractedAspectData, MediaAspect.ATTR_ISVIRTUAL, false);
            MediaItemAspect.SetAttribute(extractedAspectData, MediaAspect.ATTR_ISSTUB, true);
            MediaItemAspect.SetAttribute(extractedAspectData, MediaAspect.ATTR_RECORDINGTIME, fsra.LastChanged);

            extractedStubAspectData.Add(extractedAspectData);
          }
        }
        else
          _debugLogger.Warn("[#{0}]: No valid metadata found in movie stub file", miNumber);


        _debugLogger.Info("[#{0}]: Successfully finished extracting stubs", miNumber);
        return true;
      }
      catch (Exception e)
      {
        ServiceRegistration.Get<ILogger>().Warn("StubMovieMetadataExtractor: Exception while extracting stubs for resource '{0}'; enable debug logging for more details.", mediaItemAccessor);
        _debugLogger.Error("[#{0}]: Exception while extracting stubs", e, miNumber);
        return false;
      }
    }

    private bool IsDvd(IResourceAccessor mediaItemAccessor)
    {
      if (_settings.SeriesDvdStubFileExtensions.Where(e => mediaItemAccessor.Path.ToString().EndsWith("." + e, StringComparison.InvariantCultureIgnoreCase)).Any())
      {
        return true;
      }
      return false;
    }

    private bool IsBluray(IResourceAccessor mediaItemAccessor)
    {
      if (_settings.SeriesBlurayStubFileExtensions.Where(e => mediaItemAccessor.Path.ToString().EndsWith("." + e, StringComparison.InvariantCultureIgnoreCase)).Any())
      {
        return true;
      }
      return false;
    }

    private bool IsHdDvd(IResourceAccessor mediaItemAccessor)
    {
      if (_settings.SeriesHddvdStubFileExtensions.Where(e => mediaItemAccessor.Path.ToString().EndsWith("." + e, StringComparison.InvariantCultureIgnoreCase)).Any())
      {
        return true;
      }
      return false;
    }

    private bool IsTv(IResourceAccessor mediaItemAccessor)
    {
      if (_settings.SeriesTvStubFileExtensions.Where(e => mediaItemAccessor.Path.ToString().EndsWith("." + e, StringComparison.InvariantCultureIgnoreCase)).Any())
      {
        return true;
      }
      return false;
    }

    private bool IsVhs(IResourceAccessor mediaItemAccessor)
    {
      if (_settings.SeriesVhsStubFileExtensions.Where(e => mediaItemAccessor.Path.ToString().EndsWith("." + e, StringComparison.InvariantCultureIgnoreCase)).Any())
      {
        return true;
      }
      return false;
    }

    #endregion
  }
}
