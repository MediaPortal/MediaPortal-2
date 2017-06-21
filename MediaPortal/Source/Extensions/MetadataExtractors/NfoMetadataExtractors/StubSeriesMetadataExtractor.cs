#region Copyright (C) 2007-2017 Team MediaPortal

/*
    Copyright (C) 2007-2017 Team MediaPortal
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
using System.Net;
using System.Net.Http;
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
using MediaPortal.Extensions.MetadataExtractors.NfoMetadataExtractors.NfoReaders;
using MediaPortal.Extensions.MetadataExtractors.NfoMetadataExtractors.Settings;
using MediaPortal.Common.Services.Settings;
using MediaPortal.Common.MediaManagement.Helpers;
using MediaPortal.Utilities.SystemAPI;
using MediaPortal.Extensions.MetadataExtractors.NfoMetadataExtractors.Utilities;

namespace MediaPortal.Extensions.MetadataExtractors.NfoMetadataExtractors
{
  /// <summary>
  /// MediaPortal 2 metadata extractor for series reading from local nfo-files.
  /// </summary>
  public class StubSeriesMetadataExtractor : IMetadataExtractor, IDisposable
  {
    #region Constants / Static fields

    /// <summary>
    /// GUID of the NfoMetadataExtractors plugin
    /// </summary>
    public const string PLUGIN_ID_STR = "2505C495-28AA-4D1C-BDEE-CA4A3A89B0D5";
    public static readonly Guid PLUGIN_ID = new Guid(PLUGIN_ID_STR);

    /// <summary>
    /// GUID for the NfoSeriesMetadataExtractor
    /// </summary>
    public const string METADATAEXTRACTOR_ID_STR = "8BAB0AA1-A7B2-4F59-9286-2F4C1946BEF6";
    public static readonly Guid METADATAEXTRACTOR_ID = new Guid(METADATAEXTRACTOR_ID_STR);

    /// <summary>
    /// MediaCategories this MetadataExtractor is applied to
    /// </summary>
    private const string MEDIA_CATEGORY_NAME_SERIES = "Series";
    private readonly static ICollection<MediaCategory> MEDIA_CATEGORIES = new List<MediaCategory>();

    /// <summary>
    /// Default mimetype is being used if actual mimetype detection fails.
    /// </summary>
    private const string DEFAULT_MIMETYPE = "video/unknown";

    #endregion

    #region Private fields

    /// <summary>
    /// Metadata of this MetadataExtractor
    /// </summary>
    private readonly MetadataExtractorMetadata _metadata;

    /// <summary>
    /// Settings of the <see cref="NfoSeriesMetadataExtractor"/>
    /// </summary>
    private readonly NfoSeriesMetadataExtractorSettings _settings;
    
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

    /// <summary>
    /// <see cref="HttpClient"/> used to download from http URLs contained in nfo-files
    /// </summary>
    private HttpClient _httpClient;

    private SettingsChangeWatcher<NfoSeriesMetadataExtractorSettings> _settingWatcher;

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
      _metadata = new MetadataExtractorMetadata(
        metadataExtractorId: METADATAEXTRACTOR_ID,
        name: "Stub series metadata extractor",
        metadataExtractorPriority: MetadataExtractorPriority.Core,
        processesNonFiles: true,
        shareCategories: MEDIA_CATEGORIES,
        extractedAspectTypes: new MediaItemAspectMetadata[]
        {
          MediaAspect.Metadata,
          VideoStreamAspect.Metadata,
          VideoAudioStreamAspect.Metadata,
          SubtitleAspect.Metadata,
          ThumbnailLargeAspect.Metadata
        });

      _settingWatcher = new SettingsChangeWatcher<NfoSeriesMetadataExtractorSettings>();
      _settingWatcher.SettingsChanged += SettingsChanged;

      LoadSettings();

      _settings = ServiceRegistration.Get<ISettingsManager>().Load<NfoSeriesMetadataExtractorSettings>();

      if (_settings.EnableDebugLogging)
      {
        _debugLogger = FileLogger.CreateFileLogger(ServiceRegistration.Get<IPathManager>().GetPath(@"<LOG>\StubSeriesMetadataExtractorDebug.log"), LogLevel.Debug, false, true);
        LogSettings();
      }
      else
        _debugLogger = new NoLogger();

      var handler = new HttpClientHandler();
      if (handler.SupportsAutomaticDecompression)
        // This enables the automatic decompression of the content. It does not automatically send an "Accept-Encoding" header!
        // We therefore have to add the Accept-Encoding header(s) manually below.
        // Additionally, due to the automatic decompression, HttpResponseMessage.Content.Headers DOES NOT contain
        // a "Content-Encoding" header anymore when we try to access it. It is automatically removed when decompressing.
        handler.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
      else
        _debugLogger.Warn("HttpClient does not support compression");
      _httpClient = new HttpClient(handler);
      _httpClient.DefaultRequestHeaders.AcceptEncoding.Add(new System.Net.Http.Headers.StringWithQualityHeaderValue("gzip"));
      _httpClient.DefaultRequestHeaders.AcceptEncoding.Add(new System.Net.Http.Headers.StringWithQualityHeaderValue("deflate"));
    }

    #endregion

    #region Settings

    public static HashSet<string> SeriesStubFileExtensions { get; private set; }

    private void LoadSettings()
    {
      SeriesStubFileExtensions = _settingWatcher.Settings.SeriesStubFileExtensions;
    }

    private void SettingsChanged(object sender, EventArgs e)
    {
      LoadSettings();
    }

    #endregion

    #region Private methods

    #region Logging helpers

    /// <summary>
    /// Logs version and setting information into <see cref="_debugLogger"/>
    /// </summary>
    private void LogSettings()
    {
      _debugLogger.Info("-------------------------------------------------------------");
      _debugLogger.Info("NfoSeriesMetadataExtractor v{0} instantiated", ServiceRegistration.Get<IPluginManager>().AvailablePlugins[PLUGIN_ID].Metadata.PluginVersion);
      _debugLogger.Info("Setttings:");
      _debugLogger.Info("   EnableDebugLogging: {0}", _settings.EnableDebugLogging);
      _debugLogger.Info("   WriteRawNfoFileIntoDebugLog: {0}", _settings.WriteRawNfoFileIntoDebugLog);
      _debugLogger.Info("   WriteStubObjectIntoDebugLog: {0}", _settings.WriteStubObjectIntoDebugLog);
      _debugLogger.Info("   SeriesStubFileExtensions: {0}", String.Join(";", _settings.SeriesStubFileExtensions));
      _debugLogger.Info("   SkipFanArtDownload: {0}", _settings.SkipFanArtDownload);
      _debugLogger.Info("   SeparatorCharacters: {0}", String.Join(" ", _settings.SeparatorCharacters));
      _debugLogger.Info("   IgnoreStrings: {0}", String.Join(";", _settings.IgnoreStrings));
      _debugLogger.Info("-------------------------------------------------------------");
    }

    #endregion

    #endregion

    #region IDisposable implementation

    public void Dispose()
    {
      if (_httpClient == null)
        return;
      _httpClient.Dispose();
      _httpClient = null;
    }

    #endregion

    #region IMetadataExtractor implementation

    public MetadataExtractorMetadata Metadata
    {
      get { return _metadata; }
    }

    public bool TryExtractMetadata(IResourceAccessor mediaItemAccessor, IDictionary<Guid, IList<MediaItemAspect>> extractedAspectData, bool importOnly, bool forceQuickMode)
    {
      return false;
    }

    public bool IsSingleResource(IResourceAccessor mediaItemAccessor)
    {
      return false;
    }

    public bool IsStubResource(IResourceAccessor mediaItemAccessor)
    {
      if (SeriesStubFileExtensions.Where(e => string.Compare("." + e, ResourcePathHelper.GetExtension(mediaItemAccessor.Path.ToString()), true) == 0).Any())
      {
        return true;
      }
      return false;
    }

    public bool TryExtractStubItems(IResourceAccessor mediaItemAccessor, ICollection<IDictionary<Guid, IList<MediaItemAspect>>> extractedStubAspectData)
    {
      // The following is bad practice as it wastes one ThreadPool thread.
      // ToDo: Once the IMetadataExtractor interface is updated to support async operations, call TryExtractMetadataAsync directly
      return TryExtractStubItemsAsync(mediaItemAccessor, extractedStubAspectData).Result;
    }

    public async Task<bool> TryExtractStubItemsAsync(IResourceAccessor mediaItemAccessor, ICollection<IDictionary<Guid, IList<MediaItemAspect>>> extractedStubAspectData)
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
        // Otherwise it is not possible to find a nfo-file in the MediaItem's directory.
        if (!(mediaItemAccessor is IFileSystemResourceAccessor))
        {
          _debugLogger.Info("[#{0}]: Cannot extract stubs; mediaItemAccessor is not an IFileSystemResourceAccessor", miNumber);
          return false;
        }

        var fsra = mediaItemAccessor as IFileSystemResourceAccessor;
        var nfoReader = new NfoSeriesReader(_debugLogger, miNumber, true, false, _httpClient, _settings);
        if (fsra != null && await nfoReader.TryReadMetadataAsync(fsra).ConfigureAwait(false))
        {
          Stubs.SeriesStub series = nfoReader.GetSeriesStubs().FirstOrDefault();
          if (series != null && series.Episodes != null && series.Episodes.Count > 0)
          {
            foreach (var episode in series.Episodes)
            {
              Dictionary<Guid, IList<MediaItemAspect>> extractedAspectData = new Dictionary<Guid, IList<MediaItemAspect>>();

              //VideoAspect required to mark this media item as a video
              SingleMediaItemAspect videoAspect = MediaItemAspect.GetOrCreateAspect(extractedAspectData, VideoAspect.Metadata);
              videoAspect.SetAttribute(VideoAspect.ATTR_ISDVD, true);

              MediaItemAspect.SetAttribute(extractedAspectData, MediaAspect.ATTR_TITLE, string.Format("{0} S{1:00}E{2:00} {3}", series.ShowTitle, episode.Season, episode.Episode, episode.Title));
              MediaItemAspect.SetAttribute(extractedAspectData, MediaAspect.ATTR_SORT_TITLE, BaseInfo.GetSortTitle(episode.Title));
              MediaItemAspect.SetAttribute(extractedAspectData, MediaAspect.ATTR_ISSTUB, true);
              MediaItemAspect.SetAttribute(extractedAspectData, MediaAspect.ATTR_ISVIRTUAL, false);
              MediaItemAspect.SetAttribute(extractedAspectData, MediaAspect.ATTR_RECORDINGTIME, episode.Premiered.HasValue ? episode.Premiered.Value : episode.Year.HasValue ? episode.Year.Value : (DateTime?)null);
              MediaItemAspect.SetAttribute(extractedAspectData, MediaAspect.ATTR_STUB_LABEL, series.StubLabel);

              MultipleMediaItemAspect providerResourceAspect = MediaItemAspect.CreateAspect(extractedAspectData, ProviderResourceAspect.Metadata);
              providerResourceAspect.SetAttribute(ProviderResourceAspect.ATTR_RESOURCE_INDEX, 0);
              providerResourceAspect.SetAttribute(ProviderResourceAspect.ATTR_TYPE, ProviderResourceAspect.TYPE_STUB);
              providerResourceAspect.SetAttribute(ProviderResourceAspect.ATTR_RESOURCE_ACCESSOR_PATH, fsra.CanonicalLocalResourcePath.Serialize());
              if (episode.FileInfo != null && episode.FileInfo.Count > 0)
                providerResourceAspect.SetAttribute(ProviderResourceAspect.ATTR_MIME_TYPE, MimeTypeDetector.GetMimeTypeFromExtension("file" + episode.FileInfo.First().Container) ?? DEFAULT_MIMETYPE);

              StubParser.ParseFileInfo(extractedAspectData, episode.FileInfo, episode.Title);

              extractedStubAspectData.Add(extractedAspectData);
            }
          }
        }
        else
          _debugLogger.Warn("[#{0}]: No valid metadata found in movie stub file", miNumber);


        _debugLogger.Info("[#{0}]: Successfully finished extracting stubs", miNumber);
        return true;
      }
      catch (Exception e)
      {
        ServiceRegistration.Get<ILogger>().Warn("StubSeriesMetadataExtractor: Exception while extracting stubs for resource '{0}'; enable debug logging for more details.", mediaItemAccessor);
        _debugLogger.Error("[#{0}]: Exception while extracting stubs", e, miNumber);
        return false;
      }
    }

    #endregion
  }
}
