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
using MediaPortal.Utilities.SystemAPI;
using MediaPortal.Common.Genres;
using MediaPortal.Common.MediaManagement.Helpers;
using System.IO;

namespace MediaPortal.Extensions.MetadataExtractors.NfoMetadataExtractors
{
  /// <summary>
  /// MediaPortal 2 metadata extractor for album/artist reading from local nfo-files.
  /// </summary>
  public class NfoAudioMetadataExtractor : IMetadataExtractor, IDisposable
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

    /// <summary>
    /// Settings of the <see cref="NfoMovieMetadataExtractor"/>
    /// </summary>
    private readonly NfoAudioMetadataExtractorSettings _settings;
    
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

    private SettingsChangeWatcher<NfoAudioMetadataExtractorSettings> _settingWatcher;

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

      _settingWatcher = new SettingsChangeWatcher<NfoAudioMetadataExtractorSettings>();
      _settingWatcher.SettingsChanged += SettingsChanged;

      LoadSettings();

      _settings = _settingWatcher.Settings;

      if (_settings.EnableDebugLogging)
      {
        _debugLogger = FileLogger.CreateFileLogger(ServiceRegistration.Get<IPathManager>().GetPath(@"<LOG>\NfoAudioMetadataExtractorDebug.log"), LogLevel.Debug, false, true);
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

    public static bool IncludeArtistDetails { get; private set; }
    public static bool IncludeAlbumDetails { get; private set; }

    private void LoadSettings()
    {
      IncludeArtistDetails = _settingWatcher.Settings.IncludeArtistDetails;
      IncludeAlbumDetails = _settingWatcher.Settings.IncludeAlbumDetails;
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
    private async Task<bool> TryExtractMetadataAsync(IResourceAccessor mediaItemAccessor, IDictionary<Guid, IList<MediaItemAspect>> extractedAspectData, bool importOnly, bool forceQuickMode)
    {
      // Get a unique number for this call to TryExtractMetadataAsync. We use this to make reading the debug log easier.
      // This MetadataExtractor is called in parallel for multiple MediaItems so that the respective debug log entries
      // for one call are not contained one after another in debug log. We therefore prepend this number before every log entry.
      var miNumber = Interlocked.Increment(ref _lastMediaItemNumber);
      bool isStub = extractedAspectData.ContainsKey(StubAspect.ASPECT_ID);
      try
      {
        _debugLogger.Info("[#{0}]: Start extracting metadata for resource '{1}' (importOnly: {2}, forceQuickMode: {3})", miNumber, mediaItemAccessor, importOnly, forceQuickMode);

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
          var albumNfoReader = new NfoAlbumReader(_debugLogger, miNumber, importOnly, forceQuickMode, isStub, _httpClient, _settings);
          using (albumNfoFsra)
          {
            if (await albumNfoReader.TryReadMetadataAsync(albumNfoFsra).ConfigureAwait(false))
            {
              Stubs.AlbumStub album = albumNfoReader.GetAlbumStubs().FirstOrDefault();
              if (album != null)
              {
                INfoRelationshipExtractor.StoreAlbum(extractedAspectData, album);

                // Check if stub
                if (isStub)
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
                        trackInfo.Genres = album.Genres.Select(s => new GenreInfo { Name = s.Trim() }).ToList();
                        GenreMapper.AssignMissingMusicGenreIds(trackInfo.Genres);
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
                      trackInfo.AssignNameId();
                      trackInfo.SetMetadata(extractedAspectData);
                    }
                  }
                }
              }
            }
            else
              _debugLogger.Warn("[#{0}]: No valid metadata found in album nfo-file", miNumber);
          }
        }

        // Then we try to find an IFileSystemResourceAccessor pointing to the artist nfo-file.
        IFileSystemResourceAccessor artistNfoFsra;
        if (TryGetArtistNfoSResourceAccessor(miNumber, mediaItemAccessor as IFileSystemResourceAccessor, out artistNfoFsra))
        {
          // If we found one, we (asynchronously) extract the metadata into a stub object and, if metadata was found,
          // we store it into the MediaItemAspects.
          var artistNfoReader = new NfoArtistReader(_debugLogger, miNumber, importOnly, forceQuickMode, _httpClient, _settings);
          using (artistNfoFsra)
          {
            if (await artistNfoReader.TryReadMetadataAsync(artistNfoFsra).ConfigureAwait(false))
            {
              Stubs.ArtistStub artist = artistNfoReader.GetArtistStubs().First();
              INfoRelationshipExtractor.StoreArtist(extractedAspectData, artist, true);
            }
            else
              _debugLogger.Warn("[#{0}]: No valid metadata found in artist nfo-file", miNumber);
          }
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
    /// Tries to find an album nfo-file for the given <param name="mediaFsra"></param>
    /// </summary>
    /// <param name="miNumber">Unique number for logging purposes</param>
    /// <param name="mediaFsra">FileSystemResourceAccessor for which we search an album nfo-file</param>
    /// <param name="albumNfoFsra">FileSystemResourceAccessor of the album nfo-file or <c>null</c> if no album nfo-file was found</param>
    /// <returns><c>true</c> if an album nfo-file was found, otherwise <c>false</c></returns>
    private bool TryGetAlbumNfoSResourceAccessor(long miNumber, IFileSystemResourceAccessor mediaFsra, out IFileSystemResourceAccessor albumNfoFsra)
    {
      albumNfoFsra = null;

      // Determine the directory, in which we look for the album nfo-file
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
      var albumNfoDirectoryResourcePath = ResourcePathHelper.Combine(mediaFsra.CanonicalLocalResourcePath, "../");
      _debugLogger.Info("[#{0}]: album nfo-directory: '{1}'", miNumber, albumNfoDirectoryResourcePath);

      // Then try to create an IFileSystemResourceAccessor for this directory
      IResourceAccessor albumNfoDirectoryRa;
      albumNfoDirectoryResourcePath.TryCreateLocalResourceAccessor(out albumNfoDirectoryRa);
      var albumNfoDirectoryFsra = albumNfoDirectoryRa as IFileSystemResourceAccessor;
      if (albumNfoDirectoryFsra == null)
      {
        _debugLogger.Info("[#{0}]: Cannot extract metadata; album nfo-directory not accessible'", miNumber, albumNfoDirectoryResourcePath);
        if (albumNfoDirectoryRa != null)
          albumNfoDirectoryRa.Dispose();
        return false;
      }

      // Finally try to find an episode nfo-file in that directory
      using (albumNfoDirectoryFsra)
      {
        var albumNfoFileNames = GetAlbumNfoFileNames();
        foreach (var albumNfoFileName in albumNfoFileNames)
          if (albumNfoDirectoryFsra.ResourceExists(albumNfoFileName))
          {
            _debugLogger.Info("[#{0}]: album nfo-file found: '{1}'", miNumber, albumNfoFileName);
            albumNfoFsra = albumNfoDirectoryFsra.GetResource(albumNfoFileName);
            return true;
          }
          else
            _debugLogger.Info("[#{0}]: album nfo-file '{1}' not found; checking next possible file...", miNumber, albumNfoFileName);
      }

      _debugLogger.Info("[#{0}]: Cannot extract metadata; No album nfo-file found", miNumber);
      return false;
    }

    /// <summary>
    /// Tries to find a artist nfo-file for the given <param name="mediaFsra"></param>
    /// </summary>
    /// <param name="miNumber">Unique number for logging purposes</param>
    /// <param name="mediaFsra">FileSystemResourceAccessor for which we search a artist nfo-file</param>
    /// <param name="artistNfoFsra">FileSystemResourceAccessor of the artist nfo-file or <c>null</c> if no artist nfo-file was found</param>
    /// <returns><c>true</c> if a artist nfo-file was found, otherwise <c>false</c></returns>
    private bool TryGetArtistNfoSResourceAccessor(long miNumber, IFileSystemResourceAccessor mediaFsra, out IFileSystemResourceAccessor artistNfoFsra)
    {
      artistNfoFsra = null;

      // Determine the first directory, in which we look for the artist nfo-file
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
      var firstArtistNfoDirectoryResourcePath = ResourcePathHelper.Combine(mediaFsra.CanonicalLocalResourcePath, "../");
      _debugLogger.Info("[#{0}]: first artist nfo-directory: '{1}'", miNumber, firstArtistNfoDirectoryResourcePath);

      // Then try to create an IFileSystemResourceAccessor for this directory
      IResourceAccessor artistNfoDirectoryRa;
      firstArtistNfoDirectoryResourcePath.TryCreateLocalResourceAccessor(out artistNfoDirectoryRa);
      var artistNfoDirectoryFsra = artistNfoDirectoryRa as IFileSystemResourceAccessor;
      if (artistNfoDirectoryFsra == null)
      {
        _debugLogger.Info("[#{0}]: first artist nfo-directory not accessible'", miNumber, firstArtistNfoDirectoryResourcePath);
        if (artistNfoDirectoryRa != null)
          artistNfoDirectoryRa.Dispose();
      }
      else
      {
        // Try to find a artist nfo-file in the that directory
        using (artistNfoDirectoryFsra)
        {
          var artistNfoFileNames = GetArtistNfoFileNames();
          foreach (var artistNfoFileName in artistNfoFileNames)
            if (artistNfoDirectoryFsra.ResourceExists(artistNfoFileName))
            {
              _debugLogger.Info("[#{0}]: artist nfo-file found: '{1}'", miNumber, artistNfoFileName);
              artistNfoFsra = artistNfoDirectoryFsra.GetResource(artistNfoFileName);
              return true;
            }
            else
              _debugLogger.Info("[#{0}]: artist nfo-file '{1}' not found; checking next possible file...", miNumber, artistNfoFileName);
        }
      }

      // Determine the second directory, in which we look for the series nfo-file

      // First get the ResourcePath of the parent directory's parent directory
      var secondArtistNfoDirectoryResourcePath = ResourcePathHelper.Combine(firstArtistNfoDirectoryResourcePath, "../");
      _debugLogger.Info("[#{0}]: second artist nfo-directory: '{1}'", miNumber, secondArtistNfoDirectoryResourcePath);

      // Then try to create an IFileSystemResourceAccessor for this directory
      secondArtistNfoDirectoryResourcePath.TryCreateLocalResourceAccessor(out artistNfoDirectoryRa);
      artistNfoDirectoryFsra = artistNfoDirectoryRa as IFileSystemResourceAccessor;
      if (artistNfoDirectoryFsra == null)
      {
        _debugLogger.Info("[#{0}]: second artist nfo-directory not accessible'", miNumber, secondArtistNfoDirectoryResourcePath);
        if (artistNfoDirectoryRa != null)
          artistNfoDirectoryRa.Dispose();
        return false;
      }

      // Finally try to find a artist nfo-file in the that second directory
      using (artistNfoDirectoryFsra)
      {
        var artistNfoFileNames = GetArtistNfoFileNames();
        foreach (var artistNfoFileName in artistNfoFileNames)
          if (artistNfoDirectoryFsra.ResourceExists(artistNfoFileName))
          {
            _debugLogger.Info("[#{0}]: artist nfo-file found: '{1}'", miNumber, artistNfoFileName);
            artistNfoFsra = artistNfoDirectoryFsra.GetResource(artistNfoFileName);
            return true;
          }
          else
            _debugLogger.Info("[#{0}]: artist nfo-file '{1}' not found; checking next possible file...", miNumber, artistNfoFileName);
      }

      _debugLogger.Info("[#{0}]: No artist nfo-file found", miNumber);
      return false;
    }

    /// <summary>
    /// Determines all possible file names for the album nfo-file based on the respective NfoSeriesMetadataExtractorSettings
    /// </summary>
    /// <returns>IEnumerable of strings containing the possible album nfo-file names</returns>
    IEnumerable<string> GetAlbumNfoFileNames()
    {
      var result = new List<string>();

      // Combine the SeriesNfoFileNames from the settings with the NfoFileNameExtensions from the settings
      foreach (var extension in _settings.NfoFileNameExtensions)
        result.AddRange(_settings.AlbumNfoFileNames.Select(albumNfoFileName => albumNfoFileName + extension));
      return result;
    }

    /// <summary>
    /// Determines all possible file names for the artist nfo-file based on the respective NfoSeriesMetadataExtractorSettings
    /// </summary>
    /// <returns>IEnumerable of strings containing the possible artist nfo-file names</returns>
    IEnumerable<string> GetArtistNfoFileNames()
    {
      var result = new List<string>();

      // Combine the SeriesNfoFileNames from the settings with the NfoFileNameExtensions from the settings
      foreach (var extension in _settings.NfoFileNameExtensions)
        result.AddRange(_settings.ArtistNfoFileNames.Select(seriesNfoFileName => seriesNfoFileName + extension));
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
      //if (extractedAspectData.ContainsKey(AudioAspect.ASPECT_ID))
      //  return false;

      // The following is bad practice as it wastes one ThreadPool thread.
      // ToDo: Once the IMetadataExtractor interface is updated to support async operations, call TryExtractMetadataAsync directly
      return TryExtractMetadataAsync(mediaItemAccessor, extractedAspectData, importOnly, forceQuickMode).Result;
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

    #endregion
  }
}
