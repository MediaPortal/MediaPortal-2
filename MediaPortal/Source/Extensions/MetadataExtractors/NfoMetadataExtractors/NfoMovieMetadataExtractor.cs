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
using MediaPortal.Common.MediaManagement.TransientAspects;
using MediaPortal.Utilities.SystemAPI;
using MediaPortal.Extensions.MetadataExtractors.NfoMetadataExtractors.Utilities;
using MediaPortal.Common.MediaManagement.Helpers;
using MediaPortal.Extensions.MetadataExtractors.NfoMetadataExtractors.Extractors;

namespace MediaPortal.Extensions.MetadataExtractors.NfoMetadataExtractors
{
  /// <summary>
  /// MediaPortal 2 metadata extractor for movies reading from local nfo-files.
  /// </summary>
  public class NfoMovieMetadataExtractor : NfoMovieExtractorBase, IMetadataExtractor
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

      // All non-default media item aspects must be registered
      IMediaItemAspectTypeRegistration miatr = ServiceRegistration.Get<IMediaItemAspectTypeRegistration>();
      miatr.RegisterLocallyKnownMediaItemAspectTypeAsync(TempActorAspect.Metadata);
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
    }

    #endregion

    #region Settings

    public static bool IncludeActorDetails { get; private set; }
    public static bool IncludeCharacterDetails { get; private set; }
    public static bool IncludeDirectorDetails { get; private set; }

    protected override void LoadSettings()
    {
      IncludeActorDetails = _settingWatcher.Settings.IncludeActorDetails;
      IncludeCharacterDetails = _settingWatcher.Settings.IncludeCharacterDetails;
      IncludeDirectorDetails = _settingWatcher.Settings.IncludeDirectorDetails;
    }

    #endregion

    #region Private methods

    #region Metadata extraction

    /// <summary>
    /// Asynchronously tries to extract metadata for the given <param name="mediaItemAccessor"></param>
    /// </summary>
    /// <param name="mediaItemAccessor">Points to the resource for which we try to extract metadata</param>
    /// <param name="extractedAspectData">Dictionary of <see cref="MediaItemAspect"/>s with the extracted metadata</param>
    /// <param name="forceQuickMode">If <c>true</c>, nothing is downloaded from the internet</param>
    /// <returns><c>true</c> if metadata was found and stored into <param name="extractedAspectData"></param>, else <c>false</c></returns>
    private async Task<bool> TryExtractMovieMetadataAsync(IResourceAccessor mediaItemAccessor, IDictionary<Guid, IList<MediaItemAspect>> extractedAspectData, bool forceQuickMode)
    {
      // Get a unique number for this call to TryExtractMetadataAsync. We use this to make reading the debug log easier.
      // This MetadataExtractor is called in parallel for multiple MediaItems so that the respective debug log entries
      // for one call are not contained one after another in debug log. We therefore prepend this number before every log entry.
      var miNumber = Interlocked.Increment(ref _lastMediaItemNumber);
      bool isStub = extractedAspectData.ContainsKey(StubAspect.ASPECT_ID);
      try
      {
        _debugLogger.Info("[#{0}]: Start extracting metadata for resource '{1}' (forceQuickMode: {2})", miNumber, mediaItemAccessor, forceQuickMode);

        // This MetadataExtractor only works for MediaItems accessible by an IFileSystemResourceAccessor.
        // Otherwise it is not possible to find a nfo-file in the MediaItem's directory.
        if (!(mediaItemAccessor is IFileSystemResourceAccessor))
        {
          _debugLogger.Info("[#{0}]: Cannot extract metadata; mediaItemAccessor is not an IFileSystemResourceAccessor", miNumber);
          return false;
        }

        // We only extract metadata with this MetadataExtractor, if another MetadataExtractor that was applied before
        // has identified this MediaItem as a video and therefore added a VideoAspect.
        if (!extractedAspectData.ContainsKey(VideoAspect.ASPECT_ID))
        {
          _debugLogger.Info("[#{0}]: Cannot extract metadata; this resource is not a video", miNumber);
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
        NfoMovieReader nfoReader = new NfoMovieReader(_debugLogger, miNumber, false, forceQuickMode, isStub, _httpClient, _settings);
        using (nfoFsra)
        {
          if (!await nfoReader.TryReadMetadataAsync(nfoFsra).ConfigureAwait(false) &&
              !await nfoReader.TryParseForImdbId(nfoFsra).ConfigureAwait(false))
          {
            _debugLogger.Warn("[#{0}]: No valid metadata found", miNumber);
            return false;
          }
          else if (isStub)
          {
            Stubs.MovieStub movie = nfoReader.GetMovieStubs().FirstOrDefault();
            if (movie != null)
            {
              IList<MultipleMediaItemAspect> providerResourceAspects;
              if (MediaItemAspect.TryGetAspects(extractedAspectData, ProviderResourceAspect.Metadata, out providerResourceAspects))
              {
                MultipleMediaItemAspect providerResourceAspect = providerResourceAspects.First(pa => pa.GetAttributeValue<int>(ProviderResourceAspect.ATTR_TYPE) == ProviderResourceAspect.TYPE_STUB);
                string mime = null;
                if (movie.FileInfo != null && movie.FileInfo.Count > 0)
                  mime = MimeTypeDetector.GetMimeTypeFromExtension("file" + movie.FileInfo.First().Container);
                if (mime != null)
                  providerResourceAspect.SetAttribute(ProviderResourceAspect.ATTR_MIME_TYPE, mime);
              }

              MediaItemAspect.SetAttribute(extractedAspectData, MediaAspect.ATTR_TITLE, movie.Title);
              MediaItemAspect.SetAttribute(extractedAspectData, MediaAspect.ATTR_SORT_TITLE, movie.SortTitle != null ? movie.SortTitle : BaseInfo.GetSortTitle(movie.Title));
              MediaItemAspect.SetAttribute(extractedAspectData, MediaAspect.ATTR_RECORDINGTIME, movie.Premiered.HasValue ? movie.Premiered.Value : movie.Year.HasValue ? movie.Year.Value : (DateTime?)null);

              if (movie.FileInfo != null && movie.FileInfo.Count > 0)
              {
                extractedAspectData.Remove(VideoStreamAspect.ASPECT_ID);
                extractedAspectData.Remove(VideoAudioStreamAspect.ASPECT_ID);
                extractedAspectData.Remove(SubtitleAspect.ASPECT_ID);
                StubParser.ParseFileInfo(extractedAspectData, movie.FileInfo, movie.Title, movie.Fps);
              }
            }
          }
        }

        //Check reimport
        if (extractedAspectData.ContainsKey(ReimportAspect.ASPECT_ID))
        {
          MovieInfo reimport = new MovieInfo();
          reimport.FromMetadata(extractedAspectData);
          if (!VerifyMovieReimport(nfoReader, reimport))
          {
            ServiceRegistration.Get<ILogger>().Info("NfoMovieMetadataExtractor: Nfo movie metadata from resource '{0}' ignored because it does not match reimport {1}", mediaItemAccessor, reimport);
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
        ServiceRegistration.Get<ILogger>().Debug("NfoMovieMetadataExtractor: Assigned nfo movie metadata for resource '{0}'", mediaItemAccessor);
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

    #region Logging helpers

    /// <summary>
    /// Logs version and setting information into <see cref="_debugLogger"/>
    /// </summary>
    protected override void LogSettings()
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

    #region IMetadataExtractor implementation

    public MetadataExtractorMetadata Metadata
    {
      get { return _metadata; }
    }

    public Task<bool> TryExtractMetadataAsync(IResourceAccessor mediaItemAccessor, IDictionary<Guid, IList<MediaItemAspect>> extractedAspectData, bool forceQuickMode)
    {
      //if (extractedAspectData.ContainsKey(MovieAspect.ASPECT_ID))
      //  return false;

      return TryExtractMovieMetadataAsync(mediaItemAccessor, extractedAspectData, forceQuickMode);
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

    public Task<IList<MediaItemSearchResult>> SearchForMatchesAsync(IDictionary<Guid, IList<MediaItemAspect>> searchAspectData, ICollection<string> searchCategories)
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
