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
using MediaPortal.Utilities;
using MediaPortal.Common.Services.Settings;
using MediaPortal.Common.MediaManagement.Helpers;

namespace MediaPortal.Extensions.MetadataExtractors.NfoMetadataExtractors
{
  /// <summary>
  /// MediaPortal 2 metadata extractor for series reading from local nfo-files.
  /// </summary>
  public class NfoSeriesMetadataExtractor : IMetadataExtractor, IDisposable
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
    public const string METADATAEXTRACTOR_ID_STR = "45070E52-7CA1-473C-AE10-B08FB8243CC3";
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
    static NfoSeriesMetadataExtractor()
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
    public NfoSeriesMetadataExtractor()
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
        name: "Nfo series metadata extractor",
        metadataExtractorPriority: MetadataExtractorPriority.Extended,
        processesNonFiles: true,
        shareCategories: MEDIA_CATEGORIES,
        extractedAspectTypes: new MediaItemAspectMetadata[]
        {
          MediaAspect.Metadata,
          EpisodeAspect.Metadata,
          ThumbnailLargeAspect.Metadata
        });

      _settingWatcher = new SettingsChangeWatcher<NfoSeriesMetadataExtractorSettings>();
      _settingWatcher.SettingsChanged += SettingsChanged;

      LoadSettings();

      _settings = ServiceRegistration.Get<ISettingsManager>().Load<NfoSeriesMetadataExtractorSettings>();

      if (_settings.EnableDebugLogging)
      {
        _debugLogger = FileLogger.CreateFileLogger(ServiceRegistration.Get<IPathManager>().GetPath(@"<LOG>\NfoSeriesMetadataExtractorDebug.log"), LogLevel.Debug, false, true);
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

    public static bool IncludeActorDetails { get; private set; }
    public static bool IncludeCharacterDetails { get; private set; }

    private void LoadSettings()
    {
      IncludeActorDetails = _settingWatcher.Settings.IncludeActorDetails;
      IncludeCharacterDetails = _settingWatcher.Settings.IncludeCharacterDetails;
    }

    private void SettingsChanged(object sender, EventArgs e)
    {
      LoadSettings();
    }

    #endregion

    #region Private methods

    #region Metadata extraction

    /// <summary>
    /// Asynchronously tries to extract episode metadata for the given <param name="mediaItemAccessor"></param>
    /// </summary>
    /// <param name="mediaItemAccessor">Points to the resource for which we try to extract metadata</param>
    /// <param name="extractedAspectData">Dictionary of <see cref="MediaItemAspect"/>s with the extracted metadata</param>
    /// <param name="importOnly">If <c>true</c>, nothing is downloaded from the internet</param>
    /// <returns><c>true</c> if metadata was found and stored into <param name="extractedAspectData"></param>, else <c>false</c></returns>
    private async Task<bool> TryExtractEpsiodeMetadataAsync(IResourceAccessor mediaItemAccessor, IDictionary<Guid, IList<MediaItemAspect>> extractedAspectData, bool importOnly)
    {
      // Get a unique number for this call to TryExtractMetadataAsync. We use this to make reading the debug log easier.
      // This MetadataExtractor is called in parallel for multiple MediaItems so that the respective debug log entries
      // for one call are not contained one after another in debug log. We therefore prepend this number before every log entry.
      var miNumber = Interlocked.Increment(ref _lastMediaItemNumber);
      try
      {
        _debugLogger.Info("[#{0}]: Start extracting metadata for resource '{1}' (importOnly: {2})", miNumber, mediaItemAccessor, importOnly);

        // We only extract metadata with this MetadataExtractor, if another MetadataExtractor that was applied before
        // has identified this MediaItem as a video and therefore added a VideoAspect.
        if (!extractedAspectData.ContainsKey(VideoAspect.ASPECT_ID))
        {
          _debugLogger.Info("[#{0}]: Cannot extract metadata; this resource is not a video", miNumber);
          return false;
        }

        // This MetadataExtractor only works for MediaItems accessible by an IFileSystemResourceAccessor.
        // Otherwise it is not possible to find a nfo-file in the MediaItem's directory or parent directory.
        if (!(mediaItemAccessor is IFileSystemResourceAccessor))
        {
          _debugLogger.Info("[#{0}]: Cannot extract metadata; mediaItemAccessor is not an IFileSystemResourceAccessor", miNumber);
          return false;
        }

        // Here we try to find an IFileSystemResourceAccessor pointing to the episode nfo-file.
        // If we don't find one, we cannot extract any metadata.
        IFileSystemResourceAccessor episodeNfoFsra;
        NfoSeriesEpisodeReader episodeNfoReader = null;
        if (TryGetEpisodeNfoSResourceAccessor(miNumber, mediaItemAccessor as IFileSystemResourceAccessor, out episodeNfoFsra))
        {
          // Now we (asynchronously) extract the metadata into a stub object.
          // If no metadata was found, nothing can be stored in the MediaItemAspects.
          episodeNfoReader = new NfoSeriesEpisodeReader(_debugLogger, miNumber, importOnly, _httpClient, _settings);
          using (episodeNfoFsra)
          {
            if (!await episodeNfoReader.TryReadMetadataAsync(episodeNfoFsra).ConfigureAwait(false))
            {
              _debugLogger.Warn("[#{0}]: No valid metadata found in episode nfo-file", miNumber);
              return false;
            }
          }
        }

        // Then we try to find an IFileSystemResourceAccessor pointing to the series nfo-file.
        IFileSystemResourceAccessor seriesNfoFsra;
        if (TryGetSeriesNfoSResourceAccessor(miNumber, mediaItemAccessor as IFileSystemResourceAccessor, out seriesNfoFsra))
        {
          // If we found one, we (asynchronously) extract the metadata into a stub object and, if metadata was found,
          // we store it into the episodeNfoReader so that the latter can store metadata from series and episode level into the MediaItemAspects.
          var seriesNfoReader = new NfoSeriesReader(_debugLogger, miNumber, importOnly, _httpClient, _settings);
          using (seriesNfoFsra)
          {
            if (await seriesNfoReader.TryReadMetadataAsync(seriesNfoFsra).ConfigureAwait(false))
            {
              Stubs.SeriesStub series = seriesNfoReader.GetSeriesStubs().First();
              if (episodeNfoReader != null)
              {
                episodeNfoReader.SetSeriesStubs(new List<Stubs.SeriesStub> { series });

                // Then we store the found metadata in the MediaItemAspects. If we only found metadata that is
                // not (yet) supported by our MediaItemAspects, this MetadataExtractor returns false.
                if (!episodeNfoReader.TryWriteMetadata(extractedAspectData))
                {
                  _debugLogger.Warn("[#{0}]: No metadata was written into MediaItemsAspects", miNumber);
                  return false;
                }
              }
              else
              {
                EpisodeInfo episode = new EpisodeInfo();
                if (series.Id.HasValue)
                  episode.SeriesTvdbId = series.Id.Value;
                if (series.Premiered.HasValue)
                  episode.SeriesFirstAired = series.Premiered.Value;
                episode.SeriesName = series.ShowTitle;
                episode.SetMetadata(extractedAspectData);
              }
              INfoRelationshipExtractor.StoreSeries(extractedAspectData, series);
            }
            else
              _debugLogger.Warn("[#{0}]: No valid metadata found in series nfo-file", miNumber);
          }
        }

        _debugLogger.Info("[#{0}]: Successfully finished extracting metadata", miNumber);
        return true;
      }
      catch (Exception e)
      {
        ServiceRegistration.Get<ILogger>().Warn("NfoSeriesMetadataExtractor: Exception while extracting metadata for resource '{0}'; enable debug logging for more details.", mediaItemAccessor);
        _debugLogger.Error("[#{0}]: Exception while extracting metadata", e, miNumber);
        return false;
      }
    }

    /// <summary>
    /// Asynchronously tries to extract series metadata for the given <param name="mediaItemAccessor"></param>
    /// </summary>
    /// <param name="mediaItemAccessor">Points to the resource for which we try to extract metadata</param>
    /// <param name="extractedAspectData">Dictionary of <see cref="MediaItemAspect"/>s with the extracted metadata</param>
    /// <param name="importOnly">If <c>true</c>, nothing is downloaded from the internet</param>
    /// <returns><c>true</c> if metadata was found and stored into <param name="extractedAspectData"></param>, else <c>false</c></returns>
    private async Task<bool> TryExtractSeriesMetadataAsync(IResourceAccessor mediaItemAccessor, IDictionary<Guid, IList<MediaItemAspect>> extractedAspectData, bool importOnly)
    {
      // Get a unique number for this call to TryExtractMetadataAsync. We use this to make reading the debug log easier.
      // This MetadataExtractor is called in parallel for multiple MediaItems so that the respective debug log entries
      // for one call are not contained one after another in debug log. We therefore prepend this number before every log entry.
      var miNumber = Interlocked.Increment(ref _lastMediaItemNumber);
      try
      {
        _debugLogger.Info("[#{0}]: Start extracting metadata for resource '{1}' (importOnly: {2})", miNumber, mediaItemAccessor, importOnly);

        // This MetadataExtractor only works for MediaItems accessible by an IFileSystemResourceAccessor.
        // Otherwise it is not possible to find a nfo-file in the MediaItem's directory or parent directory.
        if (!(mediaItemAccessor is IFileSystemResourceAccessor))
        {
          _debugLogger.Info("[#{0}]: Cannot extract metadata; mediaItemAccessor is not an IFileSystemResourceAccessor", miNumber);
          return false;
        }

        // Then we try to find an IFileSystemResourceAccessor pointing to the series nfo-file.
        IFileSystemResourceAccessor seriesNfoFsra;
        if (TryGetSeriesNfoSResourceAccessor(miNumber, mediaItemAccessor as IFileSystemResourceAccessor, out seriesNfoFsra))
        {
          // If we found one, we (asynchronously) extract the metadata into a stub object and, if metadata was found,
          // we store it into the episodeNfoReader so that the latter can store metadata from series and episode level into the MediaItemAspects.
          var seriesNfoReader = new NfoSeriesReader(_debugLogger, miNumber, importOnly, _httpClient, _settings);
          using (seriesNfoFsra)
          {
            if (await seriesNfoReader.TryReadMetadataAsync(seriesNfoFsra).ConfigureAwait(false))
            {
              // Then we store the found metadata in the MediaItemAspects. If we only found metadata that is
              // not (yet) supported by our MediaItemAspects, this MetadataExtractor returns false.
              if (!seriesNfoReader.TryWriteMetadata(extractedAspectData))
              {
                _debugLogger.Warn("[#{0}]: No metadata was written into series MediaItemsAspects", miNumber);
                return false;
              }
              else
              {
                _debugLogger.Warn("[#{0}]: No valid metadata found in series nfo-file", miNumber);
              }
            }
          }
        }

        _debugLogger.Info("[#{0}]: Successfully finished extracting series metadata", miNumber);
        return true;
      }
      catch (Exception e)
      {
        ServiceRegistration.Get<ILogger>().Warn("NfoSeriesMetadataExtractor: Exception while extracting series metadata for resource '{0}'; enable debug logging for more details.", mediaItemAccessor);
        _debugLogger.Error("[#{0}]: Exception while extracting metadata", e, miNumber);
        return false;
      }
    }

    #endregion

    #region Resource helpers

    /// <summary>
    /// Tries to find an episode nfo-file for the given <param name="mediaFsra"></param>
    /// </summary>
    /// <param name="miNumber">Unique number for logging purposes</param>
    /// <param name="mediaFsra">FileSystemResourceAccessor for which we search an episode nfo-file</param>
    /// <param name="episodeNfoFsra">FileSystemResourceAccessor of the episode nfo-file or <c>null</c> if no epsiode nfo-file was found</param>
    /// <returns><c>true</c> if an episode nfo-file was found, otherwise <c>false</c></returns>
    private bool TryGetEpisodeNfoSResourceAccessor(long miNumber, IFileSystemResourceAccessor mediaFsra, out IFileSystemResourceAccessor episodeNfoFsra)
    {
      episodeNfoFsra = null;

      // Determine the directory, in which we look for the episode nfo-file
      // We cannot use mediaFsra.GetResource, because for ChainedResourceProviders the parent directory
      // may be located in the ParentResourceProvider. For details see the comments for the ResourcePathHelper class.
      
      // First get the ResourcePath of the parent directory
      // The parent directory is
      // - for an IFilesystemResourceAcessor pointing to a file:
      //   the directory in which the file is located;
      // - for an IFilesystemResourceAcessor pointing to a root directory of a ChainedResourceProvider (e.g. in case of a DVD iso-file):
      //   the directory in which the file that was unfolded by the ChainedResourceProvider is located;
      // - for an IFilesystemResourceAcessor pointing to any other directory (e.g. DVD directories):
      //   the parent directory of such directory.
      var episodeNfoDirectoryResourcePath = ResourcePathHelper.Combine(mediaFsra.CanonicalLocalResourcePath, "../");
      _debugLogger.Info("[#{0}]: episode nfo-directory: '{1}'", miNumber, episodeNfoDirectoryResourcePath);

      // Then try to create an IFileSystemResourceAccessor for this directory
      IResourceAccessor episodeNfoDirectoryRa;
      episodeNfoDirectoryResourcePath.TryCreateLocalResourceAccessor(out episodeNfoDirectoryRa);
      var episodeNfoDirectoryFsra = episodeNfoDirectoryRa as IFileSystemResourceAccessor;
      if (episodeNfoDirectoryFsra == null)
      {
        _debugLogger.Info("[#{0}]: Cannot extract metadata; episode nfo-directory not accessible'", miNumber, episodeNfoDirectoryResourcePath);
        if (episodeNfoDirectoryRa != null)
          episodeNfoDirectoryRa.Dispose();
        return false;
      }

      // Finally try to find an episode nfo-file in that directory
      using (episodeNfoDirectoryFsra)
      {
        var episodeNfoFileNames = GetEpisodeNfoFileNames(mediaFsra);
        foreach (var episodeNfoFileName in episodeNfoFileNames)
          if (episodeNfoDirectoryFsra.ResourceExists(episodeNfoFileName))
          {
            _debugLogger.Info("[#{0}]: episode nfo-file found: '{1}'", miNumber, episodeNfoFileName);
            episodeNfoFsra = episodeNfoDirectoryFsra.GetResource(episodeNfoFileName);
            return true;
          }
          else
            _debugLogger.Info("[#{0}]: episode nfo-file '{1}' not found; checking next possible file...", miNumber, episodeNfoFileName);
      }

      _debugLogger.Info("[#{0}]: Cannot extract metadata; No episode nfo-file found", miNumber);
      return false;
    }

    /// <summary>
    /// Tries to find a series nfo-file for the given <param name="mediaFsra"></param>
    /// </summary>
    /// <param name="miNumber">Unique number for logging purposes</param>
    /// <param name="mediaFsra">FileSystemResourceAccessor for which we search a series nfo-file</param>
    /// <param name="seriesNfoFsra">FileSystemResourceAccessor of the series nfo-file or <c>null</c> if no series nfo-file was found</param>
    /// <returns><c>true</c> if a series nfo-file was found, otherwise <c>false</c></returns>
    private bool TryGetSeriesNfoSResourceAccessor(long miNumber, IFileSystemResourceAccessor mediaFsra, out IFileSystemResourceAccessor seriesNfoFsra)
    {
      seriesNfoFsra = null;

      // Determine the first directory, in which we look for the series nfo-file
      // We cannot use mediaFsra.GetResource, because for ChainedResourceProviders the parent directory
      // may be located in the ParentResourceProvider. For details see the comments for the ResourcePathHelper class.

      // First get the ResourcePath of the parent directory
      // The parent directory is
      // - for an IFilesystemResourceAcessor pointing to a file:
      //   the directory in which the file is located;
      // - for an IFilesystemResourceAcessor pointing to a root directory of a ChainedResourceProvider (e.g. in case of a DVD iso-file):
      //   the directory in which the file that was unfolded by the ChainedResourceProvider is located;
      // - for an IFilesystemResourceAcessor pointing to any other directory (e.g. DVD directories):
      //   the parent directory of such directory.
      var firstSeriesNfoDirectoryResourcePath = ResourcePathHelper.Combine(mediaFsra.CanonicalLocalResourcePath, "../");
      _debugLogger.Info("[#{0}]: first series nfo-directory: '{1}'", miNumber, firstSeriesNfoDirectoryResourcePath);

      // Then try to create an IFileSystemResourceAccessor for this directory
      IResourceAccessor seriesNfoDirectoryRa;
      firstSeriesNfoDirectoryResourcePath.TryCreateLocalResourceAccessor(out seriesNfoDirectoryRa);
      var seriesNfoDirectoryFsra = seriesNfoDirectoryRa as IFileSystemResourceAccessor;
      if (seriesNfoDirectoryFsra == null)
      {
        _debugLogger.Info("[#{0}]: first series nfo-directory not accessible'", miNumber, firstSeriesNfoDirectoryResourcePath);
        if (seriesNfoDirectoryRa != null)
          seriesNfoDirectoryRa.Dispose();
      }
      else
      {
        // Try to find a series nfo-file in the that directory
        using (seriesNfoDirectoryFsra)
        {
          var seriesNfoFileNames = GetSeriesNfoFileNames();
          foreach (var seriesNfoFileName in seriesNfoFileNames)
            if (seriesNfoDirectoryFsra.ResourceExists(seriesNfoFileName))
            {
              _debugLogger.Info("[#{0}]: series nfo-file found: '{1}'", miNumber, seriesNfoFileName);
              seriesNfoFsra = seriesNfoDirectoryFsra.GetResource(seriesNfoFileName);
              return true;
            }
            else
              _debugLogger.Info("[#{0}]: series nfo-file '{1}' not found; checking next possible file...", miNumber, seriesNfoFileName);
        }
      }

      // Determine the second directory, in which we look for the series nfo-file

      // First get the ResourcePath of the parent directory's parent directory
      var secondSeriesNfoDirectoryResourcePath = ResourcePathHelper.Combine(firstSeriesNfoDirectoryResourcePath, "../");
      _debugLogger.Info("[#{0}]: second series nfo-directory: '{1}'", miNumber, secondSeriesNfoDirectoryResourcePath);

      // Then try to create an IFileSystemResourceAccessor for this directory
      secondSeriesNfoDirectoryResourcePath.TryCreateLocalResourceAccessor(out seriesNfoDirectoryRa);
      seriesNfoDirectoryFsra = seriesNfoDirectoryRa as IFileSystemResourceAccessor;
      if (seriesNfoDirectoryFsra == null)
      {
        _debugLogger.Info("[#{0}]: second series nfo-directory not accessible'", miNumber, secondSeriesNfoDirectoryResourcePath);
        if (seriesNfoDirectoryRa != null)
          seriesNfoDirectoryRa.Dispose();
        return false;
      }

      // Finally try to find a series nfo-file in the that second directory
      using (seriesNfoDirectoryFsra)
      {
        var seriesNfoFileNames = GetSeriesNfoFileNames();
        foreach (var seriesNfoFileName in seriesNfoFileNames)
          if (seriesNfoDirectoryFsra.ResourceExists(seriesNfoFileName))
          {
            _debugLogger.Info("[#{0}]: series nfo-file found: '{1}'", miNumber, seriesNfoFileName);
            seriesNfoFsra = seriesNfoDirectoryFsra.GetResource(seriesNfoFileName);
            return true;
          }
          else
            _debugLogger.Info("[#{0}]: series nfo-file '{1}' not found; checking next possible file...", miNumber, seriesNfoFileName);
      }

      _debugLogger.Info("[#{0}]: No series nfo-file found", miNumber);
      return false;
    }

    /// <summary>
    /// Determines all possible file names for the episode nfo-file based on the respective NfoSeriesMetadataExtractorSettings
    /// </summary>
    /// <param name="mediaFsra">IFilesystemResourceAccessor to the media file for which we search an episode nfo-file</param>
    /// <returns>IEnumerable of strings containing the possible episode nfo-file names</returns>
    IEnumerable<string> GetEpisodeNfoFileNames(IFileSystemResourceAccessor mediaFsra)
    {
      // Always consider the file or directory name of the media item
      string mediaFileOrDirectoryName;
      
      // If the MediaItem is a file, we simply take the filename without extension
      if (mediaFsra.IsFile)
        mediaFileOrDirectoryName = ResourcePathHelper.GetFileNameWithoutExtension(mediaFsra.CanonicalLocalResourcePath.Serialize());
      else
      {
        // if the media is a directory (such as a DVD or BluRay) we start with the ResourcePath
        mediaFileOrDirectoryName = mediaFsra.CanonicalLocalResourcePath.Serialize();
        
        // In case of the root path of a ChainedResourceProvider (such as for DVD- or BluRay-Iso-Files), we remove the last
        // ChainedResourceProvider, leaving us with the full path of the file, the ChainedResourceProvider has unfolded
        if (mediaFileOrDirectoryName.EndsWith(":///") && mediaFileOrDirectoryName.Contains(">"))
          mediaFileOrDirectoryName = mediaFileOrDirectoryName.Substring(0, mediaFileOrDirectoryName.LastIndexOf(">", StringComparison.Ordinal) - 1);

        // If it's a directory in a BaseResourceProvider, we just remove the last "/" so that the following
        // GetFileNameWithoutExtension considers the directory as a file.
        else
          mediaFileOrDirectoryName = StringUtils.RemoveSuffixIfPresent(mediaFileOrDirectoryName, "/");

        // Finally we get the file name without extension
        mediaFileOrDirectoryName = ResourcePathHelper.GetFileNameWithoutExtension(mediaFileOrDirectoryName);
      }

      // Combine the mediaFileOrDirectoryName with the NfoFileNameExtensions from the settings
      return _settings.NfoFileNameExtensions.Select(extension => mediaFileOrDirectoryName + extension).ToList();
    }

    /// <summary>
    /// Determines all possible file names for the series nfo-file based on the respective NfoSeriesMetadataExtractorSettings
    /// </summary>
    /// <returns>IEnumerable of strings containing the possible series nfo-file names</returns>
    IEnumerable<string> GetSeriesNfoFileNames()
    {
      var result = new List<string>();

      // Combine the SeriesNfoFileNames from the settings with the NfoFileNameExtensions from the settings
      foreach (var extension in _settings.NfoFileNameExtensions)
        result.AddRange(_settings.SeriesNfoFileNames.Select(seriesNfoFileName => seriesNfoFileName + extension));
      return result;
    }

    #endregion

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
      _debugLogger.Info("   SeriesNfoFileNames: {0}", String.Join(";", _settings.SeriesNfoFileNames));
      _debugLogger.Info("   NfoFileNameExtensions: {0}", String.Join(" ", _settings.NfoFileNameExtensions));
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

    public bool TryExtractMetadata(IResourceAccessor mediaItemAccessor, IDictionary<Guid, IList<MediaItemAspect>> extractedAspectData, bool importOnly)
    {
      if (extractedAspectData.ContainsKey(EpisodeAspect.ASPECT_ID))
        return false;

      // The following is bad practice as it wastes one ThreadPool thread.
      // ToDo: Once the IMetadataExtractor interface is updated to support async operations, call TryExtractMetadataAsync directly
      return TryExtractEpsiodeMetadataAsync(mediaItemAccessor, extractedAspectData, importOnly).Result;
    }

    #endregion
  }
}
