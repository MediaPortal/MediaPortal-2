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
using MediaPortal.Common.MediaManagement.Helpers;
using MediaPortal.Common.PluginManager;
using MediaPortal.Common.ResourceAccess;
using MediaPortal.Common.Services.GenreConverter;
using MediaPortal.Extensions.MetadataExtractors.NfoMetadataExtractors.Extractors;
using MediaPortal.Extensions.MetadataExtractors.NfoMetadataExtractors.NfoReaders;
using MediaPortal.Utilities.SystemAPI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MediaPortal.Extensions.MetadataExtractors.NfoMetadataExtractors
{
  /// <summary>
  /// MediaPortal 2 metadata extractor for album/artist reading from local nfo-files.
  /// </summary>
  public class NfoAudioMetadataExtractor : NfoAudioExtractorBase, IMetadataExtractor
  {
    #region Constants / Static fields

    /// <summary>
    /// GUID of the NfoMetadataExtractors plugin
    /// </summary>
    public const string PLUGIN_ID_STR = "2505C495-28AA-4D1C-BDEE-CA4A3A89B0D5";
    public static readonly Guid PLUGIN_ID = new Guid(PLUGIN_ID_STR);

    /// <summary>
    /// GUID for the NfoAudioMetadataExtractor
    /// </summary>
    public const string METADATAEXTRACTOR_ID_STR = "62D257D4-2A19-495F-9668-0EF777A4F16F";
    public static readonly Guid METADATAEXTRACTOR_ID = new Guid(METADATAEXTRACTOR_ID_STR);

    /// <summary>
    /// MediaCategories this MetadataExtractor is applied to
    /// </summary>
    private const string MEDIA_CATEGORY_NAME_AUDIO = "Audio";
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
    static NfoAudioMetadataExtractor()
    {
      MediaCategory audioCategory;
      var mediaAccessor = ServiceRegistration.Get<IMediaAccessor>();
      if (!mediaAccessor.MediaCategories.TryGetValue(MEDIA_CATEGORY_NAME_AUDIO, out audioCategory))
        audioCategory = mediaAccessor.RegisterMediaCategory(MEDIA_CATEGORY_NAME_AUDIO, new List<MediaCategory> { DefaultMediaCategories.Audio });
      MEDIA_CATEGORIES.Add(audioCategory);
    }

    /// <summary>
    /// Instantiates a new <see cref="NfoMovieMetadataExtractor"/> object
    /// </summary>
    public NfoAudioMetadataExtractor()
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
        name: "Nfo audio metadata extractor",
        metadataExtractorPriority: MetadataExtractorPriority.Extended,
        processesNonFiles: true,
        shareCategories: MEDIA_CATEGORIES,
        extractedAspectTypes: new MediaItemAspectMetadata[]
        {
          MediaAspect.Metadata,
          AudioAspect.Metadata,
          ThumbnailLargeAspect.Metadata
        });
    }

    #endregion

    #region Settings

    public static bool IncludeArtistDetails { get; private set; }
    public static bool IncludeAlbumDetails { get; private set; }

    protected override void LoadSettings()
    {
      IncludeArtistDetails = _settingWatcher.Settings.IncludeArtistDetails;
      IncludeAlbumDetails = _settingWatcher.Settings.IncludeAlbumDetails;
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
    private async Task<bool> TryExtractAudioMetadataAsync(IResourceAccessor mediaItemAccessor, IDictionary<Guid, IList<MediaItemAspect>> extractedAspectData, bool forceQuickMode)
    {
      // Get a unique number for this call to TryExtractMetadataAsync. We use this to make reading the debug log easier.
      // This MetadataExtractor is called in parallel for multiple MediaItems so that the respective debug log entries
      // for one call are not contained one after another in debug log. We therefore prepend this number before every log entry.
      var miNumber = Interlocked.Increment(ref _lastMediaItemNumber);
      bool isStub = extractedAspectData.ContainsKey(StubAspect.ASPECT_ID);
      if (!isStub)
      {
        _debugLogger.Info("[#{0}]: Ignoring non-stub track", miNumber);
        return false;
      }
      try
      {
        _debugLogger.Info("[#{0}]: Start extracting metadata for resource '{1}' (forceQuickMode: {2})", miNumber, mediaItemAccessor, forceQuickMode);

        // We only extract metadata with this MetadataExtractor, if another MetadataExtractor that was applied before
        // has identified this MediaItem as a video and therefore added a VideoAspect.
        if (!extractedAspectData.ContainsKey(AudioAspect.ASPECT_ID))
        {
          _debugLogger.Info("[#{0}]: Cannot extract metadata; this resource is not audio", miNumber);
          return false;
        }

        // This MetadataExtractor only works for MediaItems accessible by an IFileSystemResourceAccessor.
        // Otherwise it is not possible to find a nfo-file in the MediaItem's directory.
        if (!(mediaItemAccessor is IFileSystemResourceAccessor))
        {
          _debugLogger.Info("[#{0}]: Cannot extract metadata; mediaItemAccessor is not an IFileSystemResourceAccessor", miNumber);
          return false;
        }

        // First we try to find an IFileSystemResourceAccessor pointing to the album nfo-file.
        IFileSystemResourceAccessor albumNfoFsra;
        if (TryGetAlbumNfoSResourceAccessor(miNumber, mediaItemAccessor as IFileSystemResourceAccessor, out albumNfoFsra))
        {
          // If we found one, we (asynchronously) extract the metadata into a stub object and, if metadata was found,
          // we store it into the MediaItemAspects.
          var albumNfoReader = new NfoAlbumReader(_debugLogger, miNumber, forceQuickMode, isStub, _httpClient, _settings);
          using (albumNfoFsra)
          {
            if (await albumNfoReader.TryReadMetadataAsync(albumNfoFsra).ConfigureAwait(false))
            {
              //Check reimport
              if (extractedAspectData.ContainsKey(ReimportAspect.ASPECT_ID))
              {
                AlbumInfo reimport = new AlbumInfo();
                reimport.FromMetadata(extractedAspectData);
                if (!VerifyAlbumReimport(albumNfoReader, reimport))
                {
                  ServiceRegistration.Get<ILogger>().Info("NfoMovieMetadataExtractor: Nfo album metadata from resource '{0}' ignored because it does not match reimport {1}", mediaItemAccessor, reimport);
                  return false;
                }
              }

              Stubs.AlbumStub album = albumNfoReader.GetAlbumStubs().FirstOrDefault();
              if (album != null)
              {
                int trackNo = 0;
                if (album.Tracks != null && album.Tracks.Count > 0 && MediaItemAspect.TryGetAttribute(extractedAspectData, AudioAspect.ATTR_TRACK, out trackNo))
                {
                  var track = album.Tracks.FirstOrDefault(t => t.TrackNumber.HasValue && trackNo == t.TrackNumber.Value);
                  if (track != null)
                  {
                    TrackInfo trackInfo = new TrackInfo();
                    string title;
                    string sortTitle;

                    title = track.Title.Trim();
                    sortTitle = BaseInfo.GetSortTitle(title);

                    IEnumerable<string> artists;
                    if (track.Artists.Count > 0)
                      artists = track.Artists;

                    IList<MultipleMediaItemAspect> providerResourceAspects;
                    if (MediaItemAspect.TryGetAspects(extractedAspectData, ProviderResourceAspect.Metadata, out providerResourceAspects))
                    {
                      MultipleMediaItemAspect providerResourceAspect = providerResourceAspects.First(pa => pa.GetAttributeValue<int>(ProviderResourceAspect.ATTR_TYPE) == ProviderResourceAspect.TYPE_STUB);
                      string mime = null;
                      if (track.FileInfo != null && track.FileInfo.Count > 0)
                        mime = MimeTypeDetector.GetMimeTypeFromExtension("file" + track.FileInfo.First().Container);
                      if (mime != null)
                        providerResourceAspect.SetAttribute(ProviderResourceAspect.ATTR_MIME_TYPE, mime);
                    }

                    trackInfo.TrackName = title;
                    trackInfo.TrackNameSort = sortTitle;
                    trackInfo.Duration = track.Duration.HasValue ? Convert.ToInt64(track.Duration.Value.TotalSeconds) : 0;
                    trackInfo.Album = !string.IsNullOrEmpty(album.Title) ? album.Title.Trim() : null;
                    trackInfo.TrackNum = track.TrackNumber.HasValue ? track.TrackNumber.Value : 0;
                    trackInfo.TotalTracks = album.Tracks.Count;
                    trackInfo.MusicBrainzId = track.MusicBrainzId;
                    trackInfo.IsrcId = track.Isrc;
                    trackInfo.AudioDbId = track.AudioDbId.HasValue ? track.AudioDbId.Value : 0;
                    trackInfo.AlbumMusicBrainzId = album.MusicBrainzAlbumId;
                    trackInfo.AlbumMusicBrainzGroupId = album.MusicBrainzReleaseGroupId;
                    trackInfo.ReleaseDate = album.ReleaseDate;
                    if (track.FileInfo != null && track.FileInfo.Count > 0 && track.FileInfo.First().AudioStreams != null && track.FileInfo.First().AudioStreams.Count > 0)
                    {
                      var audio = track.FileInfo.First().AudioStreams.First();
                      trackInfo.Encoding = audio.Codec;
                      trackInfo.BitRate = audio.Bitrate != null ? Convert.ToInt32(audio.Bitrate / 1000) : 0;
                      trackInfo.Channels = audio.Channels != null ? audio.Channels.Value : 0;
                    }
                    trackInfo.Artists = new List<PersonInfo>();
                    if (track.Artists != null && track.Artists.Count > 0)
                    {
                      foreach (string artistName in track.Artists)
                      {
                        trackInfo.Artists.Add(new PersonInfo()
                        {
                          Name = artistName.Trim(),
                          Occupation = PersonAspect.OCCUPATION_ARTIST,
                          ParentMediaName = trackInfo.Album,
                          MediaName = trackInfo.TrackName
                        });
                      }
                    }
                    trackInfo.AlbumArtists = new List<PersonInfo>();
                    if (album.Artists != null && album.Artists.Count > 0)
                    {
                      foreach (string artistName in album.Artists)
                      {
                        trackInfo.AlbumArtists.Add(new PersonInfo()
                        {
                          Name = artistName.Trim(),
                          Occupation = PersonAspect.OCCUPATION_ARTIST,
                          ParentMediaName = trackInfo.Album,
                          MediaName = trackInfo.TrackName
                        });
                      }
                    }
                    if (album.Genres != null && album.Genres.Count > 0)
                    {
                      trackInfo.Genres = album.Genres.Where(s => !string.IsNullOrEmpty(s?.Trim())).Select(s => new GenreInfo { Name = s.Trim() }).ToList();
                      IGenreConverter converter = ServiceRegistration.Get<IGenreConverter>();
                      foreach (var genre in trackInfo.Genres)
                      {
                        if (!genre.Id.HasValue && converter.GetGenreId(genre.Name, GenreCategory.Music, null, out int genreId))
                          genre.Id = genreId;
                      }
                    }

                    if (album.Thumb != null && album.Thumb.Length > 0)
                    {
                      try
                      {
                        using (MemoryStream stream = new MemoryStream(album.Thumb))
                        {
                          trackInfo.Thumbnail = stream.ToArray();
                          trackInfo.HasChanged = true;
                        }
                      }
                      // Decoding of invalid image data can fail, but main MediaItem is correct.
                      catch { }
                    }

                    //Determine compilation
                    if (trackInfo.AlbumArtists.Count > 0 &&
                          (trackInfo.AlbumArtists[0].Name.IndexOf("Various", StringComparison.InvariantCultureIgnoreCase) >= 0 ||
                          trackInfo.AlbumArtists[0].Name.Equals("VA", StringComparison.InvariantCultureIgnoreCase)))
                    {
                      trackInfo.Compilation = true;
                    }
                    else
                    {
                      //Look for itunes compilation folder
                      var mediaItemPath = mediaItemAccessor.CanonicalLocalResourcePath;
                      var artistMediaItemDirectoryPath = ResourcePathHelper.Combine(mediaItemPath, "../../");
                      if (artistMediaItemDirectoryPath.FileName.IndexOf("Compilation", StringComparison.InvariantCultureIgnoreCase) >= 0)
                      {
                        trackInfo.Compilation = true;
                      }
                    }
                    trackInfo.SetMetadata(extractedAspectData);
                  }
                }
              }
            }
            else
              _debugLogger.Warn("[#{0}]: No valid metadata found in album nfo-file", miNumber);
          }
        }

        _debugLogger.Info("[#{0}]: Successfully finished extracting metadata", miNumber);
        ServiceRegistration.Get<ILogger>().Debug("NfoAudioMetadataExtractor: Assigned nfo audio metadata for resource '{0}'", mediaItemAccessor);
        return true;
      }
      catch (Exception e)
      {
        ServiceRegistration.Get<ILogger>().Warn("NfoAudioMetadataExtractor: Exception while extracting metadata for resource '{0}'; enable debug logging for more details.", mediaItemAccessor);
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
      _debugLogger.Info("NfoAudioMetadataExtractor v{0} instantiated", ServiceRegistration.Get<IPluginManager>().AvailablePlugins[PLUGIN_ID].Metadata.PluginVersion);
      _debugLogger.Info("Setttings:");
      _debugLogger.Info("   EnableDebugLogging: {0}", _settings.EnableDebugLogging);
      _debugLogger.Info("   WriteRawNfoFileIntoDebugLog: {0}", _settings.WriteRawNfoFileIntoDebugLog);
      _debugLogger.Info("   WriteStubObjectIntoDebugLog: {0}", _settings.WriteStubObjectIntoDebugLog);
      _debugLogger.Info("   AlbumNfoFileNames: {0}", String.Join(";", _settings.AlbumNfoFileNames));
      _debugLogger.Info("   ArtistNfoFileNames: {0}", String.Join(";", _settings.ArtistNfoFileNames));
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
      //if (extractedAspectData.ContainsKey(AudioAspect.ASPECT_ID))
      //  return false;

      return TryExtractAudioMetadataAsync(mediaItemAccessor, extractedAspectData, forceQuickMode);
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
