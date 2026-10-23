#region Copyright (C) 2007-2025 Team MediaPortal

/*
    Copyright (C) 2007-2025 Team MediaPortal
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
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Common.MediaManagement.Helpers;
using MediaPortal.Common.MediaManagement.MLQueries;
using MediaPortal.Common.MediaManagement.TransientAspects;
using MediaPortal.Common.ResourceAccess;
using MediaPortal.Common.Services.GenreConverter;
using MediaPortal.Utilities;
using MediaPortal.Utilities.SystemAPI;
using File = TagLib.File;
using Tag = TagLib.Id3v2.Tag;

namespace MediaPortal.Extensions.MetadataExtractors.AudioTagMetadataExtractor
{
  /// <summary>
  /// Metadata extractor that reads an audio file's own tags and turns them into the
  /// <see cref="AudioAspect"/>, which is the aspect that marks a <see cref="MediaItem"/> as a track.
  /// </summary>
  /// <remarks>
  /// <para>
  /// This extractor exists because the one that normally provides that mark, the
  /// <c>AudioMetadataExtractor</c> of the Windows plugin, cannot be built for a portable server: it is
  /// written against MediaPortal.TagLib, which is only on MediaPortal's own feed and only carries
  /// .NET Framework assemblies, and it probes streams with BASS.NET. As long as no extractor assigns
  /// the <see cref="AudioAspect"/>, a music share is imported as nothing at all - the importer walks
  /// every file, no extractor claims it, and the library ends up with zero tracks. Worse, everything
  /// downstream stays blind as well: the NFO audio extractor refuses a resource that no earlier
  /// extractor has called audio, so even a folder with a well-written album.nfo imports nothing.
  /// </para>
  /// <para>
  /// It reads the tags that are already in the file, with a managed reader that runs on every platform
  /// the server runs on. Title, artist, album, track number, duration, bitrate and the like are taken
  /// from the tag; nothing is invented, and nothing is looked up online - an album whose tags name no
  /// artist gets no artist, and a file whose tags are empty gets a title from its file name, which is
  /// what the Windows extractor does too.
  /// </para>
  /// <para>
  /// It also notes the file as the item's primary resource, so that a track in the library has a path
  /// that the player can open. Upstream that is written by the same extractor together with the stream
  /// values.
  /// </para>
  /// <para>
  /// What it leaves out, and why: the online lookups (MusicBrainz, AudioDB, FanArt) live in
  /// <c>OnlineMatcherService</c>, which is not part of a portable layout, so a track is described by
  /// its tags alone. Cover art embedded in a tag is not written to the thumbnail aspects either, for
  /// the same reason the Windows extractor stopped doing it - album pictures are the fan art
  /// handler's business, not the track's.
  /// </para>
  /// </remarks>
  public class AudioTagMetadataExtractor : IMetadataExtractor
  {
    #region Constants

    /// <summary>
    /// GUID of the <see cref="AudioTagMetadataExtractor"/>.
    /// </summary>
    public const string METADATAEXTRACTOR_ID_STR = "46FBE1EA-7D5C-4E49-968A-C1D93417D756";
    public static readonly Guid METADATAEXTRACTOR_ID = new Guid(METADATAEXTRACTOR_ID_STR);

    /// <summary>
    /// MediaCategories this MetadataExtractor is applied to.
    /// </summary>
    private readonly static ICollection<MediaCategory> MEDIA_CATEGORIES = new List<MediaCategory>();

    /// <summary>
    /// A mime type is only useful to us if it says "audio/something".
    /// </summary>
    private const string AUDIO_MIME_TYPE_PREFIX = "audio/";

    /// <summary>
    /// TagLibSharp reports the codec of a file as "taglib/mp3". The player expects "audio/mp3", so the
    /// prefix is exchanged the same way the Windows extractor exchanges it.
    /// </summary>
    private const string TAGLIB_MIME_TYPE_PREFIX = "taglib/";

    /// <summary>
    /// Formats a leading track number can be written in: "03 - Title", "(3) Title", "3. Title".
    /// </summary>
    private static readonly Regex TRACKNO_FORMAT = new Regex(@"\(?([0-9]+)\)?\.? *-? *(.*)", RegexOptions.IgnoreCase);

    /// <summary>
    /// Formats a file name can hold title and artist in: "Artist - Title" and "Title (Artist)".
    /// </summary>
    private static readonly Regex TITLE_ARTIST_FORMAT1 = new Regex(@"(.*) *- *(.*)", RegexOptions.IgnoreCase);
    private static readonly Regex TITLE_ARTIST_FORMAT2 = new Regex(@"(.*) *\((.*)\)", RegexOptions.IgnoreCase);

    #endregion

    #region Private fields

    private readonly MetadataExtractorMetadata _metadata;

    #endregion

    #region Ctor

    static AudioTagMetadataExtractor()
    {
      MEDIA_CATEGORIES.Add(DefaultMediaCategories.Audio);
    }

    public AudioTagMetadataExtractor()
    {
      // "Core" for the same reason the video container extractor is: everything that builds on the
      // knowledge that a resource is audio - the NFO audio reader among them - runs at "Extended", and
      // on a server that happens to have the Windows extractor as well, both have to run in the same
      // round so that their results are merged instead of one of them being skipped.
      _metadata = new MetadataExtractorMetadata(
        metadataExtractorId: METADATAEXTRACTOR_ID,
        name: "Audio tag metadata extractor",
        metadataExtractorPriority: MetadataExtractorPriority.Core,
        // Only files carry tags. A folder of tracks is not a track itself, and saying otherwise would
        // hide the files inside it from the importer.
        processesNonFiles: false,
        shareCategories: MEDIA_CATEGORIES,
        extractedAspectTypes: new MediaItemAspectMetadata[]
        {
          MediaAspect.Metadata,
          AudioAspect.Metadata,
          GenreAspect.Metadata,
          ExternalIdentifierAspect.Metadata,
          ProviderResourceAspect.Metadata
        });
    }

    #endregion

    #region Public methods

    /// <inheritdoc/>
    public MetadataExtractorMetadata Metadata
    {
      get { return _metadata; }
    }

    /// <summary>
    /// Reads the tags of the given audio file and stores them as the aspects of a track.
    /// </summary>
    /// <param name="mediaItemAccessor">Points to the resource for which we try to extract metadata.</param>
    /// <param name="extractedAspectData">Dictionary of <see cref="MediaItemAspect"/>s with the extracted metadata.</param>
    /// <param name="forceQuickMode">Unused; this extractor never accesses the network.</param>
    /// <returns><c>true</c> if the resource was recognised as an audio file with something to say.</returns>
    public Task<bool> TryExtractMetadataAsync(IResourceAccessor mediaItemAccessor, IDictionary<Guid, IList<MediaItemAspect>> extractedAspectData, bool forceQuickMode)
    {
      return Task.FromResult(TryExtractMetadata(mediaItemAccessor, extractedAspectData));
    }

    /// <inheritdoc/>
    public bool IsDirectorySingleResource(IResourceAccessor mediaItemAccessor)
    {
      // A folder of tracks stays a folder of tracks, each of which becomes an item of its own.
      return false;
    }

    /// <inheritdoc/>
    public bool IsStubResource(IResourceAccessor mediaItemAccessor)
    {
      return false;
    }

    /// <inheritdoc/>
    public bool TryExtractStubItems(IResourceAccessor mediaItemAccessor, ICollection<IDictionary<Guid, IList<MediaItemAspect>>> extractedStubAspectData)
    {
      return false;
    }

    /// <inheritdoc/>
    public Task<IList<MediaItemSearchResult>> SearchForMatchesAsync(IDictionary<Guid, IList<MediaItemAspect>> searchAspectData, ICollection<string> searchCategories)
    {
      // No online lookups; see the remarks on this class.
      return Task.FromResult((IList<MediaItemSearchResult>)new List<MediaItemSearchResult>());
    }

    /// <inheritdoc/>
    public Task<bool> AddMatchedAspectDetailsAsync(IDictionary<Guid, IList<MediaItemAspect>> matchedAspectData)
    {
      return Task.FromResult(false);
    }

    /// <inheritdoc/>
    public Task<bool> DownloadMetadataAsync(Guid mediaItemId, IDictionary<Guid, IList<MediaItemAspect>> aspectData)
    {
      return Task.FromResult(false);
    }

    #endregion

    #region Private methods

    /// <summary>
    /// Reads the tags of the given resource and fills the aspect data with what they say.
    /// </summary>
    private static bool TryExtractMetadata(IResourceAccessor mediaItemAccessor, IDictionary<Guid, IList<MediaItemAspect>> extractedAspectData)
    {
      var fsra = mediaItemAccessor as IFileSystemResourceAccessor;
      if (fsra == null || !fsra.Exists || !fsra.IsFile)
        return false;
      if (!AudioTags.HasAudioExtension(fsra.ResourceName))
        return false;

      File tag = AudioTags.LoadTag(fsra);
      if (tag == null)
        return false;

      TrackInfo trackInfo = new TrackInfo();
      string fileName = ProviderPathHelper.GetFileNameWithoutExtension(fsra.ResourceName) ?? string.Empty;

      using (tag)
      {
        // .mp4 and .m4a containers can hold a picture slideshow or a film. Those are videos and the
        // video extractors know what to do with them; claiming them here would put a film into the
        // music library.
        if (tag.Properties.VideoHeight > 0 && tag.Properties.VideoWidth > 0)
          return false;

        string title;
        string guessedArtist;
        uint? trackNo;
        GuessMetadataFromFileName(fileName, out title, out guessedArtist, out trackNo);
        if (!string.IsNullOrEmpty(tag.Tag.Title))
          title = tag.Tag.Title.Trim();
        if (tag.Tag.Track != 0)
          trackNo = tag.Tag.Track;

        AddPrimaryResource(extractedAspectData, fsra, tag.MimeType);

        // The track is the file itself, not a placeholder for something else.
        MediaItemAspect.SetAttribute(extractedAspectData, MediaAspect.ATTR_ISVIRTUAL, false);
        MediaItemAspect.SetAttribute(extractedAspectData, MediaAspect.ATTR_COMMENT, StringUtils.TrimToNull(tag.Tag.Comment));
        // Until the tag contradicts it, the release date of a track is the date of its file. The
        // Windows extractor starts from the same value.
        MediaItemAspect.SetAttribute(extractedAspectData, MediaAspect.ATTR_RECORDINGTIME, fsra.LastChanged);

        trackInfo.TrackName = title;
        if (!string.IsNullOrEmpty(tag.Tag.TitleSort))
          trackInfo.TrackNameSort = tag.Tag.TitleSort.Trim();
        Tag id3v2Tag = tag.GetTag(TagLib.TagTypes.Id3v2, false) as Tag;
        if (id3v2Tag != null && !id3v2Tag.IsEmpty)
          trackInfo.Compilation = id3v2Tag.IsCompilation;
        if (!string.IsNullOrEmpty(tag.Tag.Album))
          trackInfo.Album = tag.Tag.Album.Trim();
        if (trackNo.HasValue)
          trackInfo.TrackNum = (int)trackNo.Value;
        if (tag.Tag.TrackCount != 0)
          trackInfo.TotalTracks = (int)tag.Tag.TrackCount;
        if (tag.Tag.Disc != 0)
          trackInfo.DiscNum = (int)tag.Tag.Disc;
        if (tag.Tag.DiscCount != 0)
          trackInfo.TotalDiscs = (int)tag.Tag.DiscCount;
        if (!string.IsNullOrEmpty(tag.Tag.Lyrics))
          trackInfo.TrackLyrics = tag.Tag.Lyrics;
        if (!string.IsNullOrEmpty(tag.Tag.Grouping))
          trackInfo.ContentGroup = tag.Tag.Grouping.Trim();
        if (!string.IsNullOrEmpty(tag.Tag.MusicBrainzTrackId))
          trackInfo.MusicBrainzId = tag.Tag.MusicBrainzTrackId.Trim();
        if (!string.IsNullOrEmpty(tag.Tag.MusicBrainzReleaseId))
          trackInfo.AlbumMusicBrainzId = tag.Tag.MusicBrainzReleaseId.Trim();
        if (!string.IsNullOrEmpty(tag.Tag.MusicBrainzReleaseGroupId))
          trackInfo.AlbumMusicBrainzGroupId = tag.Tag.MusicBrainzReleaseGroupId.Trim();
        if (!string.IsNullOrEmpty(tag.Tag.MusicBrainzDiscId))
          trackInfo.AlbumMusicBrainzDiscId = tag.Tag.MusicBrainzDiscId.Trim();
        if (!string.IsNullOrEmpty(tag.Tag.AmazonId))
          trackInfo.AlbumAmazonId = tag.Tag.AmazonId.Trim();
        if (!string.IsNullOrEmpty(tag.Tag.MusicIpId))
          trackInfo.MusicIpId = tag.Tag.MusicIpId.Trim();
        if (!string.IsNullOrEmpty(tag.Tag.ISRC))
          trackInfo.IsrcId = tag.Tag.ISRC.Trim();

        string parentMediaName = trackInfo.Album;
        trackInfo.Artists = CreatePersons(TrackArtistNames(tag, guessedArtist), PersonAspect.OCCUPATION_ARTIST, parentMediaName, trackInfo.TrackName);
        trackInfo.AlbumArtists = CreatePersons(AlbumArtistNames(tag, trackInfo.Artists), PersonAspect.OCCUPATION_ARTIST, parentMediaName, trackInfo.TrackName);
        trackInfo.Composers = CreatePersons(tag.Tag.Composers, PersonAspect.OCCUPATION_COMPOSER, parentMediaName, trackInfo.TrackName);
        trackInfo.Conductors = CreatePersons(
            string.IsNullOrEmpty(tag.Tag.Conductor) ? Enumerable.Empty<string>() : new[] { tag.Tag.Conductor },
            PersonAspect.OCCUPATION_CONDUCTOR, parentMediaName, trackInfo.TrackName);
        trackInfo.Genres = CreateGenres(tag.Tag.Genres);

        int year = (int)tag.Tag.Year;
        if (year >= 30 && year <= 99)
          year += 1900;
        if (year >= 1930 && year <= 2030)
          trackInfo.ReleaseDate = new DateTime(year, 1, 1);

        AddStreamProperties(trackInfo, tag);
      }

      // "Various Artists" is how compilations are written down in tags, and a folder named
      // "Compilation" is how they are kept on disk. Both mean the same to the library's filters.
      if (!trackInfo.Compilation)
        trackInfo.Compilation = IsCompilation(fsra, trackInfo);

      AssignGenreIds(trackInfo);
      AssignSortTitle(trackInfo);

      if (!trackInfo.SetMetadata(extractedAspectData))
        return false;
      if (!trackInfo.IsBaseInfoPresent)
        return false;

      ServiceRegistration.Get<ILogger>().Debug("AudioTagMetadataExtractor: Resource '{0}' is the track '{1}'",
        mediaItemAccessor, trackInfo.TrackName);
      return true;
    }

    /// <summary>
    /// Reads the sample rate, bitrate, channel count, duration and codec of the opened file.
    /// </summary>
    /// <remarks>
    /// These are the values the Windows extractor used BASS.NET for. They are in the container header,
    /// which TagLibSharp reads with managed code, so a portable server can report them too - and
    /// without them the UI has no length to show for a track.
    /// </remarks>
    private static void AddStreamProperties(TrackInfo trackInfo, File tag)
    {
      if (tag.Properties.Codecs.Any())
        trackInfo.Encoding = tag.Properties.Codecs.First().Description;
      if (tag.Properties.Duration.TotalSeconds != 0)
        trackInfo.Duration = (long)tag.Properties.Duration.TotalSeconds;
      if (tag.Properties.AudioBitrate != 0)
        trackInfo.BitRate = (int)tag.Properties.AudioBitrate;
      if (tag.Properties.AudioChannels != 0)
        trackInfo.Channels = (int)tag.Properties.AudioChannels;
      if (tag.Properties.AudioSampleRate != 0)
        trackInfo.SampleRate = (int)tag.Properties.AudioSampleRate;
    }

    /// <summary>
    /// Returns the artists of the track itself, falling back to the artist the file name suggests.
    /// </summary>
    private static IEnumerable<string> TrackArtistNames(File tag, string guessedArtist)
    {
      IEnumerable<string> performers = tag.Tag.Performers.Where(name => !string.IsNullOrEmpty(name));
      if (performers.Any())
        return performers;
      return string.IsNullOrEmpty(guessedArtist) ? Enumerable.Empty<string>() : new[] { guessedArtist.Trim() };
    }

    /// <summary>
    /// Returns the artists of the album the track is on, falling back to the track's own artists.
    /// </summary>
    /// <remarks>
    /// The album artist frame is optional, and most files do not carry it. Without the fallback every
    /// single album in the library would be an album of nobody, and the "Artists" view would be empty.
    /// </remarks>
    private static IEnumerable<string> AlbumArtistNames(File tag, IEnumerable<PersonInfo> trackArtists)
    {
      IEnumerable<string> albumArtists = tag.Tag.AlbumArtists.Where(name => !string.IsNullOrEmpty(name));
      if (albumArtists.Any())
        return albumArtists;
      return trackArtists.Select(artist => artist.Name).Where(name => !string.IsNullOrEmpty(name)).ToList();
    }

    /// <summary>
    /// Turns tag names into <see cref="PersonInfo"/>s.
    /// </summary>
    private static List<PersonInfo> CreatePersons(IEnumerable<string> names, string occupation, string parentMediaName, string mediaName)
    {
      return names
          .Where(name => !string.IsNullOrEmpty(name))
          .Select(name => name.Trim())
          .Where(name => name.Length > 0)
          .Distinct(StringComparer.InvariantCultureIgnoreCase)
          .Select(name => new PersonInfo
          {
            Name = name,
            Occupation = occupation,
            ParentMediaName = parentMediaName,
            MediaName = mediaName
          })
          .ToList();
    }

    /// <summary>
    /// Turns the genre names of a tag into <see cref="GenreInfo"/>s.
    /// </summary>
    private static List<GenreInfo> CreateGenres(IEnumerable<string> genres)
    {
      return genres
          .Where(genre => !string.IsNullOrEmpty(genre))
          .Select(genre => genre.Trim())
          .Where(genre => genre.Length > 0)
          .Select(genre => new GenreInfo { Name = genre })
          .ToList();
    }

    /// <summary>
    /// Looks up the id of every genre name, so that the library can group by genre instead of by the
    /// spelling a tag author happened to use.
    /// </summary>
    private static void AssignGenreIds(TrackInfo trackInfo)
    {
      if (trackInfo.Genres.Count == 0)
        return;
      IGenreConverter converter = ServiceRegistration.Get<IGenreConverter>();
      foreach (GenreInfo genre in trackInfo.Genres)
      {
        if (!genre.Id.HasValue && converter.GetGenreId(genre.Name, GenreCategory.Music, null, out int genreId))
          genre.Id = genreId;
      }
    }

    /// <summary>
    /// Builds the string a track is sorted by when its tags give no sort title.
    /// </summary>
    /// <remarks>
    /// Sorting by album and track number keeps a record in the right order even when two tracks on it
    /// have the same name, which is the reason the Windows extractor builds it the same way.
    /// </remarks>
    private static void AssignSortTitle(TrackInfo trackInfo)
    {
      if (!string.IsNullOrEmpty(trackInfo.TrackNameSort))
        return;

      if (!string.IsNullOrEmpty(trackInfo.Album) && trackInfo.ReleaseDate.HasValue && trackInfo.DiscNum > 0 && trackInfo.TrackNum > 0)
        trackInfo.TrackNameSort = string.Format(CultureInfo.InvariantCulture, "{0} {1}  D{2:00}T{3:00}",
          trackInfo.Album, trackInfo.ReleaseDate.Value.Year, trackInfo.DiscNum, trackInfo.TrackNum);
      else if (!string.IsNullOrEmpty(trackInfo.Album) && trackInfo.DiscNum > 0 && trackInfo.TrackNum > 0)
        trackInfo.TrackNameSort = string.Format(CultureInfo.InvariantCulture, "{0}  D{1:00}T{2:00}",
          trackInfo.Album, trackInfo.DiscNum, trackInfo.TrackNum);
      else if (!string.IsNullOrEmpty(trackInfo.Album) && trackInfo.TrackNum > 0)
        trackInfo.TrackNameSort = string.Format(CultureInfo.InvariantCulture, "{0}  D00T{1:00}",
          trackInfo.Album, trackInfo.TrackNum);
      else
        trackInfo.TrackNameSort = BaseInfo.GetSortTitle(trackInfo.TrackName);
    }

    /// <summary>
    /// Checks whether the track belongs to a compilation, by its album artists or by the folder it is in.
    /// </summary>
    private static bool IsCompilation(IFileSystemResourceAccessor fsra, TrackInfo trackInfo)
    {
      if (trackInfo.AlbumArtists.Any(artist =>
          artist.Name.IndexOf("Various", StringComparison.InvariantCultureIgnoreCase) >= 0 ||
          artist.Name.Equals("VA", StringComparison.InvariantCultureIgnoreCase)))
        return true;

      // Music is kept as "Music/Artist/Album/Track.mp3", so the artist is the folder two levels above
      // the file. A folder that says "Compilation" in its name is a compilation whatever the tags say.
      ResourcePath trackPath = fsra.CanonicalLocalResourcePath;
      if (trackPath == null)
        return false;
      ResourcePath artistPath = ResourcePathHelper.Combine(trackPath, "../../");
      return artistPath != null && artistPath.FileName != null &&
          artistPath.FileName.IndexOf("Compilation", StringComparison.InvariantCultureIgnoreCase) >= 0;
    }

    /// <summary>
    /// Notes that the resource of the item being extracted is the given file.
    /// </summary>
    private static void AddPrimaryResource(IDictionary<Guid, IList<MediaItemAspect>> extractedAspectData, IFileSystemResourceAccessor fsra, string mimeType)
    {
      MultipleMediaItemAspect providerResourceAspect = MediaItemAspect.CreateAspect(extractedAspectData, ProviderResourceAspect.Metadata);
      providerResourceAspect.SetAttribute(ProviderResourceAspect.ATTR_RESOURCE_INDEX, 0);
      providerResourceAspect.SetAttribute(ProviderResourceAspect.ATTR_TYPE, ProviderResourceAspect.TYPE_PRIMARY);
      providerResourceAspect.SetAttribute(ProviderResourceAspect.ATTR_SIZE, fsra.Size);
      providerResourceAspect.SetAttribute(ProviderResourceAspect.ATTR_RESOURCE_ACCESSOR_PATH, fsra.CanonicalLocalResourcePath.Serialize());

      string mime = string.IsNullOrWhiteSpace(mimeType) ? null : mimeType.Replace(TAGLIB_MIME_TYPE_PREFIX, AUDIO_MIME_TYPE_PREFIX);
      if (string.IsNullOrEmpty(mime))
      {
        // Not every file type TagLibSharp opens has a mime type of its own; the extension knows one.
        mime = MimeTypeDetector.GetMimeTypeFromExtension(fsra.ResourceName);
      }
      if (!string.IsNullOrWhiteSpace(mime))
        providerResourceAspect.SetAttribute(ProviderResourceAspect.ATTR_MIME_TYPE, mime);
    }

    /// <summary>
    /// Given an audio file name, tries to guess title, artist and track number from it.
    /// </summary>
    /// <param name="fileNameWithoutExtension">Audio file name (no file path and no extension!).</param>
    /// <param name="title">Guessed title.</param>
    /// <param name="artist">Guessed artist, or <c>null</c>.</param>
    /// <param name="trackNo">Guessed track number, or <c>null</c>.</param>
    /// <remarks>
    /// Tags beat the file name everywhere; this is only the last resort for the files whose tags are
    /// empty, and it is the same guess the Windows extractor makes, so a library does not look
    /// different depending on which extractor read it.
    /// </remarks>
    internal static void GuessMetadataFromFileName(string fileNameWithoutExtension, out string title, out string artist, out uint? trackNo)
    {
      title = fileNameWithoutExtension.Replace('_', ' ');
      artist = null;
      trackNo = null;

      Match trackNoMatch = TRACKNO_FORMAT.Match(title);
      string titleArtist;
      if (trackNoMatch.Success)
      {
        uint parsed;
        if (uint.TryParse(trackNoMatch.Groups[1].Value, NumberStyles.AllowLeadingWhite, CultureInfo.InvariantCulture, out parsed))
          trackNo = parsed;
        titleArtist = trackNoMatch.Groups[2].Value.Trim();
      }
      else
      {
        titleArtist = title;
      }

      Match match = TITLE_ARTIST_FORMAT1.Match(titleArtist);
      if (match.Success)
      {
        // "Artist - Title" is the way files exported by ripping tools are named.
        artist = match.Groups[1].Value.Trim();
        title = match.Groups[2].Value.Trim();
        return;
      }

      match = TITLE_ARTIST_FORMAT2.Match(titleArtist);
      if (match.Success)
      {
        title = match.Groups[1].Value.Trim();
        artist = match.Groups[2].Value.Trim();
        return;
      }

      title = titleArtist.Trim();
    }

    #endregion
  }
}
