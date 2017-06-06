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
using MediaPortal.Common.Genres;
using System.IO;
using MediaPortal.Extensions.OnlineLibraries;
using MediaPortal.Utilities.SystemAPI;

namespace MediaPortal.Extensions.MetadataExtractors.NfoMetadataExtractors
{
  /// <summary>
  /// MediaPortal 2 metadata extractor for album/tracks reading from local stub files.
  /// </summary>
  public class StubAudioMetadataExtractor : IMetadataExtractor, IDisposable
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
    public const string METADATAEXTRACTOR_ID_STR = "CDE55F80-FEC7-4556-84CA-BC7236B3D323";
    public static readonly Guid METADATAEXTRACTOR_ID = new Guid(METADATAEXTRACTOR_ID_STR);

    /// <summary>
    /// MediaCategories this MetadataExtractor is applied to
    /// </summary>
    private const string MEDIA_CATEGORY_NAME_AUDIO = "Audio";
    private readonly static ICollection<MediaCategory> MEDIA_CATEGORIES = new List<MediaCategory>();

    /// <summary>
    /// Default mimetype is being used if actual mimetype detection fails.
    /// </summary>
    private const string DEFAULT_MIMETYPE = "audio/unknown";

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
    static StubAudioMetadataExtractor()
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
    public StubAudioMetadataExtractor()
    {
      _metadata = new MetadataExtractorMetadata(
        metadataExtractorId: METADATAEXTRACTOR_ID,
        name: "Stub audio metadata extractor",
        metadataExtractorPriority: MetadataExtractorPriority.Core,
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

      _settings = ServiceRegistration.Get<ISettingsManager>().Load<NfoAudioMetadataExtractorSettings>();

      if (_settings.EnableDebugLogging)
      {
        _debugLogger = FileLogger.CreateFileLogger(ServiceRegistration.Get<IPathManager>().GetPath(@"<LOG>\StubAudioMetadataExtractorDebug.log"), LogLevel.Debug, false, true);
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

    public static HashSet<string> AudioStubFileExtensions { get; private set; }
    public static bool SkipOnlineSearches { get; private set; }
    public static bool SkipFanArtDownload { get; private set; }
    public static bool IncludeArtistDetails { get; private set; }

    private void LoadSettings()
    {
      AudioStubFileExtensions = _settingWatcher.Settings.AudioStubFileExtensions;
      SkipOnlineSearches = _settingWatcher.Settings.SkipOnlineSearches;
      SkipFanArtDownload = _settingWatcher.Settings.SkipFanArtDownload;
      IncludeArtistDetails = _settingWatcher.Settings.IncludeArtistDetails;
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
      _debugLogger.Info("StubAudioMetadataExtractor v{0} instantiated", ServiceRegistration.Get<IPluginManager>().AvailablePlugins[PLUGIN_ID].Metadata.PluginVersion);
      _debugLogger.Info("Setttings:");
      _debugLogger.Info("   EnableDebugLogging: {0}", _settings.EnableDebugLogging);
      _debugLogger.Info("   WriteRawNfoFileIntoDebugLog: {0}", _settings.WriteRawNfoFileIntoDebugLog);
      _debugLogger.Info("   WriteStubObjectIntoDebugLog: {0}", _settings.WriteStubObjectIntoDebugLog);
      _debugLogger.Info("   AudioStubFileExtensions: {0}", String.Join(";", _settings.AudioStubFileExtensions));
      _debugLogger.Info("   SkipOnlineSearches: {0}",  _settings.SkipOnlineSearches);
      _debugLogger.Info("   SkipFanArtDownload: {0}", _settings.SkipFanArtDownload);
      _debugLogger.Info("   IncludeArtistDetails: {0}", _settings.IncludeArtistDetails);
      _debugLogger.Info("   SeparatorCharacters: {0}", _settings.SeparatorCharacters);
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
      if (!IsStubResource(mediaItemAccessor))
        return false;
      if (!extractedAspectData.ContainsKey(AudioAspect.ASPECT_ID))
        return false;

      try
      {
        TrackInfo trackInfo = new TrackInfo();
        trackInfo.FromMetadata(extractedAspectData);
        if (!forceQuickMode)
        {
          if (SkipOnlineSearches && !SkipFanArtDownload)
          {
            TrackInfo tempInfo = trackInfo.Clone();
            OnlineMatcherService.Instance.FindAndUpdateTrack(tempInfo, importOnly);
            trackInfo.CopyIdsFrom(tempInfo);
            trackInfo.HasChanged = tempInfo.HasChanged;
          }
          else if (!SkipOnlineSearches)
          {
            OnlineMatcherService.Instance.FindAndUpdateTrack(trackInfo, importOnly);
          }
        }

        if ((IncludeArtistDetails && !BaseInfo.HasRelationship(extractedAspectData, PersonAspect.ROLE_ARTIST) && trackInfo.Artists.Count > 0) ||
            (IncludeArtistDetails && !BaseInfo.HasRelationship(extractedAspectData, PersonAspect.ROLE_ALBUMARTIST) && trackInfo.AlbumArtists.Count > 0))
        {
          trackInfo.HasChanged = true;
        }

        if (!trackInfo.HasChanged && !importOnly)
          return false;

        trackInfo.SetMetadata(extractedAspectData);

        if (importOnly)
        {
          //Store metadata for the Relationship Extractors
          if (IncludeArtistDetails)
          {
            INfoRelationshipExtractor.StoreArtists(extractedAspectData, trackInfo.Artists, false);
            INfoRelationshipExtractor.StoreArtists(extractedAspectData, trackInfo.AlbumArtists, true);
          }
        }
        return trackInfo.IsBaseInfoPresent;
      }
      catch (Exception e)
      {
        // Only log at the info level here - And simply return false. This makes the importer know that we
        // couldn't perform our task here
        ServiceRegistration.Get<ILogger>().Info("StubAudioMetadataExtractor: Exception reading resource '{0}' (Text: '{1}')", mediaItemAccessor.CanonicalLocalResourcePath, e.Message);
      }
      return false;
    }

    public bool IsSingleResource(IResourceAccessor mediaItemAccessor)
    {
      return false;
    }

    public bool IsStubResource(IResourceAccessor mediaItemAccessor)
    {
      if (AudioStubFileExtensions.Where(e => string.Compare("." + e, ResourcePathHelper.GetExtension(mediaItemAccessor.Path.ToString()), true) == 0).Any())
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
        // Otherwise it is not possible to find a nfo-file in the MediaItem's directory.
        if (!(mediaItemAccessor is IFileSystemResourceAccessor))
        {
          _debugLogger.Info("[#{0}]: Cannot extract stubs; mediaItemAccessor is not an IFileSystemResourceAccessor", miNumber);
          return false;
        }

        var fsra = mediaItemAccessor as IFileSystemResourceAccessor;
        var albumNfoReader = new NfoAlbumReader(_debugLogger, miNumber, true, false, _httpClient, _settings);
        if (fsra != null && await albumNfoReader.TryReadMetadataAsync(fsra).ConfigureAwait(false))
        {
          Stubs.AlbumStub album = albumNfoReader.GetAlbumStubs().FirstOrDefault();
          if (album != null && album.Tracks != null && album.Tracks.Count > 0)
          {
            foreach (var track in album.Tracks)
            {
              Dictionary<Guid, IList<MediaItemAspect>> extractedAspectData = new Dictionary<Guid, IList<MediaItemAspect>>();
              TrackInfo trackInfo = new TrackInfo();
              string title;
              string sortTitle;

              title = track.Title.Trim();
              sortTitle = BaseInfo.GetSortTitle(title);

              IEnumerable<string> artists;
              if (track.Artists.Count > 0)
                artists = track.Artists;

              MultipleMediaItemAspect providerResourceAspect = MediaItemAspect.CreateAspect(extractedAspectData, ProviderResourceAspect.Metadata);
              providerResourceAspect.SetAttribute(ProviderResourceAspect.ATTR_RESOURCE_INDEX, 0);
              providerResourceAspect.SetAttribute(ProviderResourceAspect.ATTR_TYPE, ProviderResourceAspect.TYPE_STUB);
              providerResourceAspect.SetAttribute(ProviderResourceAspect.ATTR_RESOURCE_ACCESSOR_PATH, fsra.CanonicalLocalResourcePath.Serialize());
              if (track.FileInfo != null && track.FileInfo.Count > 0)
                providerResourceAspect.SetAttribute(ProviderResourceAspect.ATTR_MIME_TYPE, MimeTypeDetector.GetMimeTypeFromExtension("file" + track.FileInfo.First().Container) ?? DEFAULT_MIMETYPE);

              SingleMediaItemAspect audioAspect = MediaItemAspect.GetOrCreateAspect(extractedAspectData, AudioAspect.Metadata);
              audioAspect.SetAttribute(AudioAspect.ATTR_ISCD, true);

              MediaItemAspect.SetAttribute(extractedAspectData, MediaAspect.ATTR_TITLE, title);
              MediaItemAspect.SetAttribute(extractedAspectData, MediaAspect.ATTR_SORT_TITLE, sortTitle);
              MediaItemAspect.SetAttribute(extractedAspectData, MediaAspect.ATTR_ISVIRTUAL, false);
              MediaItemAspect.SetAttribute(extractedAspectData, MediaAspect.ATTR_ISSTUB, true);
              if (album.ReleaseDate.HasValue)
                MediaItemAspect.SetAttribute(extractedAspectData, MediaAspect.ATTR_RECORDINGTIME, album.ReleaseDate);

              trackInfo.TrackName = title;
              trackInfo.TrackNameSort = sortTitle;
              trackInfo.Duration = track.Duration.HasValue ? Convert.ToInt64(track.Duration.Value.TotalSeconds) : 0;
              trackInfo.Album = !string.IsNullOrEmpty(album.Title) ? album.Title.Trim() : null;
              trackInfo.TrackNum = track.TrackNumber.HasValue ? track.TrackNumber.Value : 0;
              trackInfo.TotalTracks = album.Tracks.Count;
              trackInfo.MusicBrainzId = track.MusicBrainzId;
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

              extractedStubAspectData.Add(extractedAspectData);
            }
          }
        }
        else
          _debugLogger.Warn("[#{0}]: No valid metadata found in album stub file", miNumber);


        _debugLogger.Info("[#{0}]: Successfully finished extracting stubs", miNumber);
        return true;
      }
      catch (Exception e)
      {
        ServiceRegistration.Get<ILogger>().Warn("StubAudioMetadataExtractor: Exception while extracting stubs for resource '{0}'; enable debug logging for more details.", mediaItemAccessor);
        _debugLogger.Error("[#{0}]: Exception while extracting stubs", e, miNumber);
        return false;
      }
    }

    #endregion
  }
}
