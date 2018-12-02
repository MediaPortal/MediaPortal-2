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

using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Common.PluginManager;
using MediaPortal.Common.ResourceAccess;
using MediaPortal.Extensions.MetadataExtractors.NfoMetadataExtractors.Extractors;
using MediaPortal.Extensions.MetadataExtractors.NfoMetadataExtractors.NfoReaders;
using MediaPortal.Extensions.MetadataExtractors.NfoMetadataExtractors.Settings;
using MediaPortal.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MediaPortal.Extensions.MetadataExtractors.NfoMetadataExtractors
{
  /// <summary>
  /// MediaPortal 2 metadata extractor for video reading from local nfo-files.
  /// </summary>
  public class NfoVideoMetadataExtractor : NfoExtractorBase<NfoMovieMetadataExtractorSettings>, IDisposable
  {
    #region Constants / Static fields

    /// <summary>
    /// GUID of the NfoMetadataExtractors plugin
    /// </summary>
    public const string PLUGIN_ID_STR = "2505C495-28AA-4D1C-BDEE-CA4A3A89B0D5";
    public static readonly Guid PLUGIN_ID = new Guid(PLUGIN_ID_STR);

    /// <summary>
    /// GUID for the NfoVideoMetadataExtractor
    /// </summary>
    public const string METADATAEXTRACTOR_ID_STR = "183DBA7C-666A-4BBD-BCE8-AD0924B4FEF1";
    public static readonly Guid METADATAEXTRACTOR_ID = new Guid(METADATAEXTRACTOR_ID_STR);

    /// <summary>
    /// MediaCategories this MetadataExtractor is applied to
    /// </summary>
    private const string MEDIA_CATEGORY_NAME_VIDEO = "Video";
    private readonly static ICollection<MediaCategory> MEDIA_CATEGORIES = new List<MediaCategory>();

    #endregion

    #region Private fields

    /// <summary>
    /// Metadata of this MetadataExtractor
    /// </summary>
    private readonly MetadataExtractorMetadata _metadata;

    #endregion

    #region Ctor

    /// <summary>
    /// Initializes <see cref="MEDIA_CATEGORIES"/> and, if necessary, registers the "Movie" <see cref="MediaCategory"/>
    /// </summary>
    static NfoVideoMetadataExtractor()
    {
      MediaCategory videoCategory;
      var mediaAccessor = ServiceRegistration.Get<IMediaAccessor>();
      if (!mediaAccessor.MediaCategories.TryGetValue(MEDIA_CATEGORY_NAME_VIDEO, out videoCategory))
        videoCategory = mediaAccessor.RegisterMediaCategory(MEDIA_CATEGORY_NAME_VIDEO, new List<MediaCategory> { DefaultMediaCategories.Video });
      MEDIA_CATEGORIES.Add(videoCategory);
    }

    /// <summary>
    /// Instantiates a new <see cref="NfoVideoMetadataExtractor"/> object
    /// </summary>
    public NfoVideoMetadataExtractor()
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
        name: "Nfo video metadata extractor",
        metadataExtractorPriority: MetadataExtractorPriority.Extended,
        processesNonFiles: true,
        shareCategories: MEDIA_CATEGORIES,
        extractedAspectTypes: new MediaItemAspectMetadata[]
        {
          MediaAspect.Metadata,
          VideoAspect.Metadata,
          ThumbnailLargeAspect.Metadata
        });
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
    private async Task<bool> TryExtractVideoMetadataAsync(IResourceAccessor mediaItemAccessor, IDictionary<Guid, IList<MediaItemAspect>> extractedAspectData, bool forceQuickMode)
    {
      // Get a unique number for this call to TryExtractMetadataAsync. We use this to make reading the debug log easier.
      // This MetadataExtractor is called in parallel for multiple MediaItems so that the respective debug log entries
      // for one call are not contained one after another in debug log. We therefore prepend this number before every log entry.
      var miNumber = Interlocked.Increment(ref _lastMediaItemNumber);
      try
      {
        _debugLogger.Info("[#{0}]: Start extracting metadata for resource '{1}' (forceQuickMode: {2})", miNumber, mediaItemAccessor, forceQuickMode);

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
        // If no metadata was found, nothing can be stored in the MediaItemAspects.
        var nfoReader = new NfoMovieReader(_debugLogger, miNumber, true, forceQuickMode, false, _httpClient, _settings);
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
        ServiceRegistration.Get<ILogger>().Debug("NfoVideoMetadataExtractor: Assigned nfo video metadata for resource '{0}'", mediaItemAccessor);
        return true;
      }
      catch (Exception e)
      {
        ServiceRegistration.Get<ILogger>().Warn("NfoVideoMetadataExtractor: Exception while extracting metadata for resource '{0}'; enable debug logging for more details.", mediaItemAccessor);
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
    protected override void LogSettings()
    {
      _debugLogger.Info("-------------------------------------------------------------");
      _debugLogger.Info("NfoVideoMetadataExtractor v{0} instantiated", ServiceRegistration.Get<IPluginManager>().AvailablePlugins[PLUGIN_ID].Metadata.PluginVersion);
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

    #region IMetadataExtractor implementation

    public MetadataExtractorMetadata Metadata
    {
      get { return _metadata; }
    }

    public Task<bool> TryExtractMetadataAsync(IResourceAccessor mediaItemAccessor, IDictionary<Guid, IList<MediaItemAspect>> extractedAspectData, bool forceQuickMode)
    {
      if (extractedAspectData.ContainsKey(MovieAspect.ASPECT_ID) || extractedAspectData.ContainsKey(EpisodeAspect.ASPECT_ID))
        return Task.FromResult(false);
      if (extractedAspectData.ContainsKey(ReimportAspect.ASPECT_ID)) //Ignore for reimports because they are handled by movie or series MDE
        return Task.FromResult(false);

      return TryExtractVideoMetadataAsync(mediaItemAccessor, extractedAspectData, forceQuickMode);
    }

    public bool IsDirectorySingleResource(IResourceAccessor mediaItemAccessor)
    {
      return false;
    }

    public bool IsStubResource(IResourceAccessor mediaItemAccessor)
    {
      return false;
    }

    public bool TryExtractStubItems(IResourceAccessor mediaItemAccessor, ICollection<IDictionary<Guid, IList<MediaItemAspect>>> extractedStubAspectData)
    {
      return false;
    }

    public Task<IList<MediaItemSearchResult>> SearchForMatchesAsync(IDictionary<Guid, IList<MediaItemAspect>> searchAspectData)
    {
      return Task.FromResult<IList<MediaItemSearchResult>>(null);
    }

    public Task<bool> AddMatchedAspectDetailsAsync(IDictionary<Guid, IList<MediaItemAspect>> matchedAspectData)
    {
      return Task.FromResult(false);
    }

    #endregion
  }
}
