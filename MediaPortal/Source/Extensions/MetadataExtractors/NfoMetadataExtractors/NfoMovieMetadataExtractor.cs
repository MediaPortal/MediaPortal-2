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
using System.Collections;
using MediaPortal.Common.Services.Settings;

namespace MediaPortal.Extensions.MetadataExtractors.NfoMetadataExtractors
{
  /// <summary>
  /// MediaPortal 2 metadata extractor for movies reading from local nfo-files.
  /// </summary>
  public class NfoMovieMetadataExtractor : IMetadataExtractor, IDisposable
  {
    #region Constants / Static fields

    /// <summary>
    /// GUID of the NfoMetadataExtractors plugin
    /// </summary>
    public const string PLUGIN_ID_STR = "2505C495-28AA-4D1C-BDEE-CA4A3A89B0D5";
    public static readonly Guid PLUGIN_ID = new Guid(PLUGIN_ID_STR);

    /// <summary>
    /// GUID for the NfoMovieMetadataExtractor
    /// </summary>
    public const string METADATAEXTRACTOR_ID_STR = "F1028D66-6E60-4EB6-9987-1C34D4B7813C";
    public static readonly Guid METADATAEXTRACTOR_ID = new Guid(METADATAEXTRACTOR_ID_STR);

    /// <summary>
    /// MediaCategories this MetadataExtractor is applied to
    /// </summary>
    private const string MEDIA_CATEGORY_NAME_MOVIE = "Movie";
    private readonly static ICollection<MediaCategory> MEDIA_CATEGORIES = new List<MediaCategory>();

    #endregion

    #region Private fields

    /// <summary>
    /// Metadata of this MetadataExtractor
    /// </summary>
    private readonly MetadataExtractorMetadata _metadata;

    /// <summary>
    /// Settings of the <see cref="NfoMovieMetadataExtractor"/>
    /// </summary>
    private readonly NfoMovieMetadataExtractorSettings _settings;
    
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

    private SettingsChangeWatcher<NfoMovieMetadataExtractorSettings> _settingWatcher;

    #endregion

    #region Ctor

    /// <summary>
    /// Initializes <see cref="MEDIA_CATEGORIES"/> and, if necessary, registers the "Movie" <see cref="MediaCategory"/>
    /// </summary>
    static NfoMovieMetadataExtractor()
    {
      MediaCategory movieCategory;
      var mediaAccessor = ServiceRegistration.Get<IMediaAccessor>();
      if (!mediaAccessor.MediaCategories.TryGetValue(MEDIA_CATEGORY_NAME_MOVIE, out movieCategory))
        movieCategory = mediaAccessor.RegisterMediaCategory(MEDIA_CATEGORY_NAME_MOVIE, new List<MediaCategory> { DefaultMediaCategories.Video });
      MEDIA_CATEGORIES.Add(movieCategory);
    }

    /// <summary>
    /// Instantiates a new <see cref="NfoMovieMetadataExtractor"/> object
    /// </summary>
    public NfoMovieMetadataExtractor()
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
        name: "Nfo movie metadata extractor",
        metadataExtractorPriority: MetadataExtractorPriority.Extended,
        processesNonFiles: true,
        shareCategories: MEDIA_CATEGORIES,
        extractedAspectTypes: new MediaItemAspectMetadata[]
        {
          MediaAspect.Metadata,
          MovieAspect.Metadata,
          ThumbnailLargeAspect.Metadata
        });

      _settingWatcher = new SettingsChangeWatcher<NfoMovieMetadataExtractorSettings>();
      _settingWatcher.SettingsChanged += SettingsChanged;

      LoadSettings();

      _settings = ServiceRegistration.Get<ISettingsManager>().Load<NfoMovieMetadataExtractorSettings>();

      if (_settings.EnableDebugLogging)
      {
        _debugLogger = FileLogger.CreateFileLogger(ServiceRegistration.Get<IPathManager>().GetPath(@"<LOG>\NfoMovieMetadataExtractorDebug.log"), LogLevel.Debug, false, true);
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
    public static bool IncludeDirectorDetails { get; private set; }

    private void LoadSettings()
    {
      IncludeActorDetails = _settingWatcher.Settings.IncludeActorDetails;
      IncludeCharacterDetails = _settingWatcher.Settings.IncludeCharacterDetails;
      IncludeDirectorDetails = _settingWatcher.Settings.IncludeDirectorDetails;
    }

    private void SettingsChanged(object sender, EventArgs e)
    {
      LoadSettings();
    }

    #endregion

    #region Private methods

    #region Metadata extraction

    /// <summary>
    /// Asynchronously tries to extract metadata for the given <param name="mediaItemAccessor"></param>
    /// </summary>
    /// <param name="mediaItemAccessor">Points to the resource for which we try to extract metadata</param>
    /// <param name="extractedAspectData">Dictionary of <see cref="MediaItemAspect"/>s with the extracted metadata</param>
    /// <param name="importOnly">If <c>true</c>, nothing is downloaded from the internet</param>
    /// <returns><c>true</c> if metadata was found and stored into <param name="extractedAspectData"></param>, else <c>false</c></returns>
    private async Task<bool> TryExtractMetadataAsync(IResourceAccessor mediaItemAccessor, IDictionary<Guid, IList<MediaItemAspect>> extractedAspectData, bool importOnly)
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
        if (!extractedAspectData.ContainsKey(VideoStreamAspect.ASPECT_ID))
        {
          _debugLogger.Info("[#{0}]: Cannot extract metadata; this resource is not a video", miNumber);
          return false;
        }

        // This MetadataExtractor only works for MediaItems accessible by an IFileSystemResourceAccessor.
        // Otherwise it is not possible to find a nfo-file in the MediaItem's directory.
        if (!(mediaItemAccessor is IFileSystemResourceAccessor))
        {
          _debugLogger.Info("[#{0}]: Cannot extract metadata; mediaItemAccessor is not an IFileSystemResourceAccessor", miNumber);
          return false;
        }

        // Here we try to find an IFileSystemResourceAccessor pointing to the nfo-file.
        // If we don't find one, we cannot extract any metadata.
        IFileSystemResourceAccessor nfoFsra;
        if (!TryGetNfoSResourceAccessor(miNumber, mediaItemAccessor as IFileSystemResourceAccessor, out nfoFsra))
          return false;

        // Now we (asynchronously) extract the metadata into a stub object.
        // If there is an error parsing the nfo-file with XmlNfoReader, we at least try to parse for a valid IMDB-ID.
        // If no metadata was found, nothing can be stored in the MediaItemAspects.
        var nfoReader = new NfoMovieReader(_debugLogger, miNumber, importOnly, _httpClient, _settings);
        using (nfoFsra)
        {
          if (!await nfoReader.TryReadMetadataAsync(nfoFsra).ConfigureAwait(false) &&
              !await nfoReader.TryParseForImdbId(nfoFsra).ConfigureAwait(false))
          {
            _debugLogger.Warn("[#{0}]: No valid metadata found", miNumber);
            return false;
          }
        }

        // Then we store the found metadata in the MediaItemAspects. If we only found metadata that is
        // not (yet) supported by our MediaItemAspects, this MetadataExtractor returns false.
        if (!nfoReader.TryWriteMetadata(extractedAspectData))
        {
          _debugLogger.Warn("[#{0}]: No metadata was written into MediaItemsAspects", miNumber);
          return false;
        }

        _debugLogger.Info("[#{0}]: Successfully finished extracting metadata", miNumber);
        return true;
      }
      catch (Exception e)
      {
        ServiceRegistration.Get<ILogger>().Warn("NfoMovieMetadataExtractor: Exception while extracting metadata for resource '{0}'; enable debug logging for more details.", mediaItemAccessor);
        _debugLogger.Error("[#{0}]: Exception while extracting metadata", e, miNumber);
        return false;
      }
    }

    #endregion

    #region Resource helpers

    /// <summary>
    /// Tries to find a nfo-file for the given <param name="mediaFsra"></param>
    /// </summary>
    /// <param name="miNumber">Unique number for logging purposes</param>
    /// <param name="mediaFsra">FileSystemResourceAccessor for which we search a nfo-file</param>
    /// <param name="nfoFsra">FileSystemResourceAccessor of the nfo-file or <c>null</c> if no nfo-file was found</param>
    /// <returns><c>true</c> if a nfo-file was found, otherwise <c>false</c></returns>
    private bool TryGetNfoSResourceAccessor(long miNumber, IFileSystemResourceAccessor mediaFsra, out IFileSystemResourceAccessor nfoFsra)
    {
      nfoFsra = null;

      // Determine the directory, in which we look for the nfo-file
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
      var nfoDirectoryResourcePath = ResourcePathHelper.Combine(mediaFsra.CanonicalLocalResourcePath, "../");
      _debugLogger.Info("[#{0}]: nfo-directory: '{1}'", miNumber, nfoDirectoryResourcePath);

      // Then try to create an IFileSystemResourceAccessor for this directory
      IResourceAccessor nfoDirectoryRa;
      nfoDirectoryResourcePath.TryCreateLocalResourceAccessor(out nfoDirectoryRa);
      var nfoDirectoryFsra = nfoDirectoryRa as IFileSystemResourceAccessor;
      if (nfoDirectoryFsra == null)
      {
        _debugLogger.Info("[#{0}]: Cannot extract metadata; nfo-directory not accessible'", miNumber, nfoDirectoryResourcePath);
        if (nfoDirectoryRa != null)
          nfoDirectoryRa.Dispose();
        return false;
      }

      // Finally try to find a nfo-file in that directory
      using (nfoDirectoryFsra)
      {
        var nfoFileNames = GetNfoFileNames(mediaFsra);
        foreach (var nfoFileName in nfoFileNames)
          if (nfoDirectoryFsra.ResourceExists(nfoFileName))
          {
            _debugLogger.Info("[#{0}]: nfo-file found: '{1}'", miNumber, nfoFileName);
            nfoFsra = nfoDirectoryFsra.GetResource(nfoFileName);
            return true;
          }
          else
            _debugLogger.Info("[#{0}]: nfo-file '{1}' not found; checking next possible file...", miNumber, nfoFileName);
      }

      _debugLogger.Info("[#{0}]: Cannot extract metadata; No nfo-file found", miNumber);
      return false;
    }

    /// <summary>
    /// Determines all possible file names for the nfo-file based on the respective NfoMovieMetadataExtractorSettings
    /// </summary>
    /// <param name="mediaFsra">IFilesystemResourceAccessor to the media file for which we search an nfo-file</param>
    /// <returns>IEnumerable of strings containing the possible nfo-file names</returns>
    IEnumerable<string> GetNfoFileNames(IFileSystemResourceAccessor mediaFsra)
    {
      var result = new List<string>();
      
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

      // Combine the mediaFileOrDirectoryName and potentially further MovieNfoFileNames from the settings with
      // the NfoFileNameExtensions from the settings
      foreach (var extension in _settings.NfoFileNameExtensions)
      {
        result.Add(mediaFileOrDirectoryName + extension);
        result.AddRange(_settings.MovieNfoFileNames.Select(movieNfoFileName => movieNfoFileName + extension));
      }
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
      _debugLogger.Info("NfoMovieMetadataExtractor v{0} instantiated", ServiceRegistration.Get<IPluginManager>().AvailablePlugins[PLUGIN_ID].Metadata.PluginVersion);
      _debugLogger.Info("Setttings:");
      _debugLogger.Info("   EnableDebugLogging: {0}", _settings.EnableDebugLogging);
      _debugLogger.Info("   WriteRawNfoFileIntoDebugLog: {0}", _settings.WriteRawNfoFileIntoDebugLog);
      _debugLogger.Info("   WriteStubObjectIntoDebugLog: {0}", _settings.WriteStubObjectIntoDebugLog);
      _debugLogger.Info("   MovieNfoFileNames: {0}", String.Join(";", _settings.MovieNfoFileNames));
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
      if (extractedAspectData.ContainsKey(MovieAspect.ASPECT_ID))
        return false;

      // The following is bad practice as it wastes one ThreadPool thread.
      // ToDo: Once the IMetadataExtractor interface is updated to support async operations, call TryExtractMetadataAsync directly
      return TryExtractMetadataAsync(mediaItemAccessor, extractedAspectData, importOnly).Result;
    }

    #endregion
  }
}
