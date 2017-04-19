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
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using MediaPortal.Common;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Common.ResourceAccess;
using MediaPortal.Common.Services.ThumbnailGenerator;
using MediaPortal.Common.Settings;
using MediaPortal.Extensions.MetadataExtractors.AudioMetadataExtractor.Settings;
using MediaPortal.Utilities;
using TagLib;
using File = TagLib.File;
using MediaPortal.Common.Logging;
using MediaPortal.Common.MediaManagement.Helpers;
using MediaPortal.Extensions.MetadataExtractors.AudioMetadataExtractor.Matchers;
using System.Globalization;
using MediaPortal.Extensions.OnlineLibraries;
using MediaPortal.Common.Services.Settings;
using MediaPortal.Common.Messaging;
using System.Threading;

namespace MediaPortal.Extensions.MetadataExtractors.AudioMetadataExtractor
{
  /// <summary>
  /// MediaPortal 2 metadata extractor implementation for audio files. Supports several formats.
  /// </summary>
  public class AudioMetadataExtractor : IMetadataExtractor, IDisposable
  {
    #region Constants

    /// <summary>
    /// GUID string for the audio metadata extractor.
    /// </summary>
    public const string METADATAEXTRACTOR_ID_STR = "CC8B703D-054C-4EB8-A49D-AD92B64EBF62";

    /// <summary>
    /// Audio metadata extractor GUID.
    /// </summary>
    public static Guid METADATAEXTRACTOR_ID = new Guid(METADATAEXTRACTOR_ID_STR);

    public const double MINIMUM_HOUR_AGE_BEFORE_UPDATE = 0.5;

    #endregion

    #region Fields and classes

    protected static ICollection<MediaCategory> MEDIA_CATEGORIES = new List<MediaCategory>();
    protected static ICollection<string> AUDIO_EXTENSIONS = new List<string>();
    protected static ICollection<string> UNSPLITTABLE_ID3V23_VALUES = new List<string>();
    protected static bool USE_ADDITIONAL_SEPARATOR;
    protected static char ADDITIONAL_SEPARATOR;
    protected static ICollection<string> UNSPLITTABLE_ADDITIONAL_SEPARATOR_VALUES = new List<string>();

    protected SettingsChangeWatcher<AudioMetadataExtractorSettings> _settingWatcher;
    protected AsynchronousMessageQueue _messageQueue;
    protected int _importerCount;

    /// <summary>
    /// Audio file accessor class needed for our tag library implementation. This class maps
    /// the TagLib#'s <see cref="File.IFileAbstraction"/> view to an MP2 file from a resource provider.
    /// </summary>
    internal class ResourceProviderFileAbstraction : File.IFileAbstraction
    {
      protected IFileSystemResourceAccessor _resourceAccessor;

      public ResourceProviderFileAbstraction(IFileSystemResourceAccessor resourceAccessor)
      {
        _resourceAccessor = resourceAccessor;
      }

      #region IFileAbstraction implementation

      public void CloseStream(Stream stream)
      {
        stream.Close();
      }

      public string Name
      {
        get { return _resourceAccessor.ResourcePathName; }
      }

      public Stream ReadStream
      {
        get { return _resourceAccessor.OpenRead(); }
      }

      public Stream WriteStream
      {
        get { return _resourceAccessor.OpenWrite(); }
      }

      #endregion
    }

    protected MetadataExtractorMetadata _metadata;

    #endregion

    #region Ctor

    static AudioMetadataExtractor()
    {
      MEDIA_CATEGORIES.Add(DefaultMediaCategories.Audio);

      AudioMetadataExtractorSettings settings = ServiceRegistration.Get<ISettingsManager>().Load<AudioMetadataExtractorSettings>();
      InitializeExtensions(settings);
      InitializeUnsplittableID3v23Values(settings);
      InitializeAdditionalSeparatorBehaviour(settings);
    }

    /// <summary>
    /// (Re)initializes the audio extensions for which this <see cref="AudioMetadataExtractor"/> used.
    /// </summary>
    /// <param name="settings">Settings object to read the data from.</param>
    internal static void InitializeExtensions(AudioMetadataExtractorSettings settings)
    {
      AUDIO_EXTENSIONS = new List<string>(settings.AudioExtensions.Select(e => e.ToLowerInvariant()));
    }

    /// <summary>
    /// (Re)initializes the unsplittable values collection for ID3v2.3 tags.
    /// </summary>
    /// <param name="settings">Settings object to read the data from.</param>
    internal static void InitializeUnsplittableID3v23Values(AudioMetadataExtractorSettings settings)
    {
      UNSPLITTABLE_ID3V23_VALUES = new List<string>(settings.UnsplittableID3v23Values.Select(v => v.ToLowerInvariant()));
    }

    /// <summary>
    /// (Re)initializes the behaviour of this <see cref="AudioMetadataExtractor"/> regarding multiple values in single fields.
    /// </summary>
    /// <param name="settings">Settings object to read the data from.</param>
    internal static void InitializeAdditionalSeparatorBehaviour(AudioMetadataExtractorSettings settings)
    {
      USE_ADDITIONAL_SEPARATOR = settings.UseAdditionalSeparator;
      ADDITIONAL_SEPARATOR = settings.AdditionalSeparator;
      UNSPLITTABLE_ADDITIONAL_SEPARATOR_VALUES = new List<string>(settings.UnsplittableAddditionalSeparatorValues.Select(e => e.ToLowerInvariant()));
    }

    public AudioMetadataExtractor()
    {
      _metadata = new MetadataExtractorMetadata(METADATAEXTRACTOR_ID, "Audio metadata extractor", MetadataExtractorPriority.Core, false,
          MEDIA_CATEGORIES, new[]
              {
                MediaAspect.Metadata,
                AudioAspect.Metadata,
                ThumbnailLargeAspect.Metadata
              });

      _messageQueue = new AsynchronousMessageQueue(this, new string[]
        {
            ImporterWorkerMessaging.CHANNEL,
        });
      _messageQueue.MessageReceived += OnMessageReceived;
      _messageQueue.Start();

      _settingWatcher = new SettingsChangeWatcher<AudioMetadataExtractorSettings>();
      _settingWatcher.SettingsChanged += SettingsChanged;

      LoadSettings();
    }

    public void Dispose()
    {
      _messageQueue.Shutdown();
    }

    private void OnMessageReceived(AsynchronousMessageQueue queue, SystemMessage message)
    {
      if (message.ChannelName == ImporterWorkerMessaging.CHANNEL)
      {
        ImporterWorkerMessaging.MessageType messageType = (ImporterWorkerMessaging.MessageType)message.MessageType;
        switch (messageType)
        {
          case ImporterWorkerMessaging.MessageType.ImportStarted:
            if(Interlocked.Increment(ref _importerCount) == 1)
            {
              IMediaFanArtHandler fanartHandler;
              if (ServiceRegistration.Get<IMediaAccessor>().LocalFanArtHandlers.TryGetValue(AudioFanArtHandler.FANARTHANDLER_ID, out fanartHandler))
                fanartHandler.ClearCache();
            }
            break;
          case ImporterWorkerMessaging.MessageType.ImportCompleted:
            if (Interlocked.Decrement(ref _importerCount) == 0)
            {
              IRelationshipExtractor relationshipExtractor;
              if (ServiceRegistration.Get<IMediaAccessor>().LocalRelationshipExtractors.TryGetValue(AudioRelationshipExtractor.METADATAEXTRACTOR_ID, out relationshipExtractor))
                foreach (IAudioRelationshipExtractor extractor in relationshipExtractor.RoleExtractors)
                  extractor.ClearCache();
            }
            break;
        }
      }
    }

    #endregion

    #region Settings

    public static bool SkipOnlineSearches { get; private set; }
    public static bool SkipFanArtDownload { get; private set; }
    public static bool CacheOfflineFanArt { get; private set; }
    public static bool CacheLocalFanArt { get; private set; }
    public static bool IncludeArtistDetails { get; private set; }
    public static bool IncludeComposerDetails { get; private set; }
    public static bool IncludeMusicLabelDetails { get; private set; }
    public static bool OnlyLocalMedia { get; private set; }

    private void LoadSettings()
    {
      SkipOnlineSearches = _settingWatcher.Settings.SkipOnlineSearches;
      SkipFanArtDownload = _settingWatcher.Settings.SkipFanArtDownload;
      CacheOfflineFanArt = _settingWatcher.Settings.CacheOfflineFanArt;
      CacheLocalFanArt = _settingWatcher.Settings.CacheLocalFanArt;
      IncludeArtistDetails = _settingWatcher.Settings.IncludeArtistDetails;
      IncludeComposerDetails = _settingWatcher.Settings.IncludeComposerDetails;
      IncludeMusicLabelDetails = _settingWatcher.Settings.IncludeMusicLabelDetails;
      OnlyLocalMedia = _settingWatcher.Settings.OnlyLocalMedia;
    }

    private void SettingsChanged(object sender, EventArgs e)
    {
      LoadSettings();
    }

    #endregion

    #region Protected methods

    /// <summary>
    /// Returns the information if the specified file name (or path) has a file extension which is
    /// supposed to be supported by this metadata extractor.
    /// </summary>
    /// <param name="fileName">Relative or absolute file path to check.</param>
    /// <returns><c>true</c>, if the file's extension is supposed to be supported, else <c>false</c>.</returns>
    protected static bool HasAudioExtension(string fileName)
    {
      string ext = DosPathHelper.GetExtension(fileName).ToLowerInvariant();
      return AUDIO_EXTENSIONS.Contains(ext);
    }

    protected static readonly Regex TRACKNO_FORMAT = new Regex(@"\(?([0-9]+)\)?\.? *-? *(.*)", RegexOptions.IgnoreCase);
    protected static readonly Regex TITLE_ARTIST_FORMAT1 = new Regex(@"(.*) *- *(.*)", RegexOptions.IgnoreCase);
    protected static readonly Regex TITLE_ARTIST_FORMAT2 = new Regex(@"(.*) *\((.*)\)", RegexOptions.IgnoreCase);
    protected static readonly Regex SPLIT_MULTIPLE_ARTISTS_REGEX = new Regex(@"(?<artist>.+)(?:ft\.|feat\.|featuring)(?<artist2>.+)", RegexOptions.IgnoreCase);

    /// <summary>
    /// Given an audio file name, this method tries to guess title, artist and track number.
    /// </summary>
    /// <param name="fileNameWithoutExtension">Audio file name (no file path and extension!).</param>
    /// <param name="title">Guessed title.</param>
    /// <param name="artist">Guessed artist.</param>
    /// <param name="trackNo">Guessed track number.</param>
    protected static void GuessMetadataFromFileName(string fileNameWithoutExtension, out string title, out string artist, out uint? trackNo)
    {
      fileNameWithoutExtension = fileNameWithoutExtension.Replace('_', ' ');
      Match match = TRACKNO_FORMAT.Match(fileNameWithoutExtension);
      string titleArtist;
      if (match.Success)
      { // (Track) - TitleArtist
        GroupCollection groups = match.Groups;
        uint trackNoInt;
        trackNo = uint.TryParse(groups[1].Value.Trim(), out trackNoInt) ? (uint?)trackNoInt : null;
        titleArtist = groups[2].Value.Trim();
      }
      else
      {
        trackNo = null;
        titleArtist = fileNameWithoutExtension.Trim();
      }
      match = TITLE_ARTIST_FORMAT1.Match(titleArtist);
      if (match.Success)
      { // Artist - Track
        GroupCollection groups = match.Groups;
        artist = groups[1].Value.Trim();
        title = groups[2].Value.Trim();
        return;
      }
      match = TITLE_ARTIST_FORMAT2.Match(titleArtist);
      if (match.Success)
      { // Track (Artist)
        GroupCollection groups = match.Groups;
        title = groups[1].Value.Trim();
        artist = groups[2].Value.Trim();
        return;
      }
      title = fileNameWithoutExtension;
      artist = null;
    }

    /// <summary>
    /// Patches an enumeration of artists or other values that have been potentially been separated
    /// although the artist name or other value contains one or more separators in its name and thus should not
    /// be treated as different artists or separated other values.
    /// </summary>
    /// <param name="valuesList">List of artists or other values, which have potentially been separated.</param>
    /// <param name="unsplittableValue">Artist or other value containing at least one separator character.</param>
    /// <param name="separator">Character, which was used as separator.</param>
    protected static void JoinUnsplittableValue(IList<string> valuesList, string unsplittableValue, char separator)
    {
      IList<string> parts = unsplittableValue.Split(separator);
      int index = CollectionUtils.IndexOf<string, string>(valuesList, parts, StringComparer.InvariantCultureIgnoreCase);
      if (index == -1)
        return;
      string[] origParts = new string[parts.Count];
      for (int i = 0; i < parts.Count; i++)
      {
        origParts[i] = valuesList[index];
        valuesList.RemoveAt(index);
      }
      valuesList.Insert(index, StringUtils.Join(separator.ToString(), origParts));
    }

    /// <summary>
    /// Patches an enumeration of artists or other values that have been potentially been separated
    /// although the artist names or other values each contain one or more separators in their name and thus should not
    /// be treated as different artists or separated other values.
    /// </summary>
    /// <param name="valuesEnumer">Enumerable of artists or other values, which have potentially been separated.</param>
    /// <param name="unsplittableValues">Artists or other values each containing at least one separator character.</param>
    /// <param name="separator">Character, which was used as separator.</param>
    protected static IEnumerable<string> JoinUnsplittableValues(IEnumerable<string> valuesEnumer, ICollection<string> unsplittableValues, char separator)
    {
      if (valuesEnumer == null)
        return null;
      IList<string> values = new List<string>(valuesEnumer);
      if (values.Count == 0)
        return null;
      foreach (string unsplittableValue in unsplittableValues)
        JoinUnsplittableValue(values, unsplittableValue, separator);
      return values;
    }

    public static bool IsDiscFolder(string album, string albumFolder)
    {
      int discNo = 0;
      int albumNo = 0;
      if (album != null &&
        (albumFolder.StartsWith("CD", StringComparison.InvariantCultureIgnoreCase) && !album.StartsWith("CD", StringComparison.InvariantCultureIgnoreCase)) ||
        (int.TryParse(albumFolder, out discNo) && int.TryParse(album, out albumNo) && discNo != albumNo))
      {
        return true;
      }
      return false;
    }

    /// <summary>
    /// We have to cope with a very stupid problem; The ID3Tag specification v2.3 (http://www.id3.org/d3v2.3.0, search for TPE1)
    /// uses the '/' character as separator for multiple values in some fields such as TPEE1 (=artist), but what to do if an artist name contains
    /// that character? We'll do a hack for the most common artists and other values of that kind.
    /// </summary>
    /// <remarks>
    /// We call this for Artists, Albumartists, Composers and Genres if the tag format is ID3v2.3.
    /// For more information see this thread in the MediaPortal forum: http://forum.team-mediaportal.com/submit-bug-reports-532/multiple-music-genres-not-handled-correctly-103169/
    /// </remarks>
    /// <param name="valuesEnumer">Enumeration of values, which were potentially wrongly splitted by TagLib#.</param>
    protected static IEnumerable<string> PatchID3v23Enumeration(IEnumerable<string> valuesEnumer)
    {
      return JoinUnsplittableValues(valuesEnumer, UNSPLITTABLE_ID3V23_VALUES, '/');
    }

    /// <summary>
    /// If USE_ADDITIONAL_SEPARATOR is true, valuesEnumer are splitted by ADDITIONAL_SEPARATOR and wrongly
    /// splitted values contained in UNSPLITTABLE_ADDITIONAL_SEPARATOR_VALUES are corrected.
    /// </summary>
    /// <param name="valuesEnumer">Enumeration of values, to which the additional separator behaviour shall be applied.</param>
    protected static IEnumerable<string> ApplyAdditionalSeparator(IEnumerable<string> valuesEnumer)
    {
      List<String> result = new List<String>();
      if (valuesEnumer == null || !valuesEnumer.Any())
        return result;
      if (USE_ADDITIONAL_SEPARATOR)
      {
        foreach (String value in valuesEnumer)
          result.AddRange(value.Split(ADDITIONAL_SEPARATOR));
        result = new List<String>(JoinUnsplittableValues(result, UNSPLITTABLE_ADDITIONAL_SEPARATOR_VALUES, ADDITIONAL_SEPARATOR));
      }
      else
        result = new List<String>(valuesEnumer);
      return result;
    }

    #endregion

    #region IMetadataExtractor implementation

    public MetadataExtractorMetadata Metadata
    {
      get { return _metadata; }
    }

    public virtual bool TryExtractMetadata(IResourceAccessor mediaItemAccessor, IDictionary<Guid, IList<MediaItemAspect>> extractedAspectData, bool importOnly)
    {
      IFileSystemResourceAccessor fsra = mediaItemAccessor as IFileSystemResourceAccessor;
      if (fsra == null)
        return false;
      if (!fsra.IsFile)
        return false;
      string fileName = fsra.ResourceName;
      if (!HasAudioExtension(fileName))
        return false;

      bool refresh = false;
      if (extractedAspectData.ContainsKey(AudioAspect.ASPECT_ID))
        refresh = true;

      try
      {
        TrackInfo trackInfo = new TrackInfo();
        if (refresh)
        {
          trackInfo.FromMetadata(extractedAspectData);
        }
        if (!trackInfo.IsBaseInfoPresent)
        {
          File tag = null;
          try
          {
            ByteVector.UseBrokenLatin1Behavior = true;  // Otherwise we have problems retrieving non-latin1 chars
            tag = File.Create(new ResourceProviderFileAbstraction(fsra));

          }
          catch (CorruptFileException)
          {
            // Only log at the info level here - And simply return false. This makes the importer know that we
            // couldn't perform our task here.
            ServiceRegistration.Get<ILogger>().Info("AudioMetadataExtractor: Audio file '{0}' seems to be broken", fsra.CanonicalLocalResourcePath);
            return false;
          }

          using (tag)
          {
            // Some file extensions like .mp4 can contain audio and video. Do not handle files with video content here.
            if (tag.Properties.VideoHeight > 0 && tag.Properties.VideoWidth > 0)
              return false;

            fileName = ProviderPathHelper.GetFileNameWithoutExtension(fileName) ?? string.Empty;
            string title;
            string sortTitle;
            string artist;
            uint? trackNo;
            GuessMetadataFromFileName(fileName, out title, out artist, out trackNo);
            if (!string.IsNullOrEmpty(title))
              title = CultureInfo.InvariantCulture.TextInfo.ToTitleCase(title.ToLowerInvariant());
            if (!string.IsNullOrEmpty(artist))
              artist = CultureInfo.InvariantCulture.TextInfo.ToTitleCase(artist.ToLowerInvariant());

            if (!string.IsNullOrEmpty(tag.Tag.Title))
              title = tag.Tag.Title.Trim();

            sortTitle = BaseInfo.GetSortTitle(title);
            if (!string.IsNullOrEmpty(tag.Tag.TitleSort))
              sortTitle = tag.Tag.TitleSort.Trim();
              
            IEnumerable<string> artists;
            if (tag.Tag.Performers.Length > 0)
            {
              artists = tag.Tag.Performers;
              if ((tag.TagTypes & TagTypes.Id3v2) != 0)
                artists = PatchID3v23Enumeration(artists);
            }
            else
              artists = artist == null ? null : new string[] { artist.Trim() };
            if (tag.Tag.Track != 0)
              trackNo = tag.Tag.Track;

            if (importOnly)
            {
              MultipleMediaItemAspect providerResourceAspect = MediaItemAspect.CreateAspect(extractedAspectData, ProviderResourceAspect.Metadata);
              providerResourceAspect.SetAttribute(ProviderResourceAspect.ATTR_RESOURCE_INDEX, 0);
              providerResourceAspect.SetAttribute(ProviderResourceAspect.ATTR_PRIMARY, true);
              providerResourceAspect.SetAttribute(ProviderResourceAspect.ATTR_SIZE, fsra.Size);
              providerResourceAspect.SetAttribute(ProviderResourceAspect.ATTR_RESOURCE_ACCESSOR_PATH, fsra.CanonicalLocalResourcePath.Serialize());
              // FIXME Albert: tag.MimeType returns taglib/mp3 for an MP3 file. This is not what we want and collides with the
              // mimetype handling in the BASS player, which expects audio/xxx.
              if (!string.IsNullOrWhiteSpace(tag.MimeType))
                providerResourceAspect.SetAttribute(ProviderResourceAspect.ATTR_MIME_TYPE, tag.MimeType.Replace("taglib/", "audio/"));

              MediaItemAspect.SetAttribute(extractedAspectData, MediaAspect.ATTR_TITLE, title);
              MediaItemAspect.SetAttribute(extractedAspectData, MediaAspect.ATTR_SORT_TITLE, sortTitle);
              MediaItemAspect.SetAttribute(extractedAspectData, MediaAspect.ATTR_ISVIRTUAL, false);
              MediaItemAspect.SetAttribute(extractedAspectData, MediaAspect.ATTR_COMMENT, StringUtils.TrimToNull(tag.Tag.Comment));
              MediaItemAspect.SetAttribute(extractedAspectData, MediaAspect.ATTR_RECORDINGTIME, fsra.LastChanged);
            }

            trackInfo.TrackName = title;
            trackInfo.TrackNameSort = sortTitle;
            if (tag.Properties.Codecs.Count() > 0)
              trackInfo.Encoding = tag.Properties.Codecs.First().Description;
            if (tag.Properties.Duration.TotalSeconds != 0)
              trackInfo.Duration = (long)tag.Properties.Duration.TotalSeconds;
            if (tag.Properties.AudioBitrate != 0)
              trackInfo.BitRate = (int)tag.Properties.AudioBitrate;
            if (tag.Properties.AudioChannels != 0)
              trackInfo.Channels = (int)tag.Properties.AudioChannels;
            if (tag.Properties.AudioSampleRate != 0)
              trackInfo.SampleRate = (int)tag.Properties.AudioSampleRate;

            TagLib.Id3v2.Tag id3Tag = (TagLib.Id3v2.Tag)tag.GetTag(TagTypes.Id3v2, false);
            if (id3Tag != null && !id3Tag.IsEmpty)
              trackInfo.Compilation = id3Tag.IsCompilation;

            trackInfo.Album = !string.IsNullOrEmpty(tag.Tag.Album) ? tag.Tag.Album.Trim() : null;
            if(!string.IsNullOrEmpty(tag.Tag.AlbumSort))
            {
              IAudioRelationshipExtractor.StoreAlbum(extractedAspectData, tag.Tag.Album, tag.Tag.AlbumSort.Trim());
            }

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

            if (tag.Tag.TrackCount != 0)
              trackInfo.TotalTracks = (int)tag.Tag.TrackCount;

            if (!string.IsNullOrEmpty(tag.Tag.MusicBrainzTrackId))
              trackInfo.MusicBrainzId = tag.Tag.MusicBrainzTrackId;
            if (!string.IsNullOrEmpty(tag.Tag.MusicBrainzReleaseId))
              trackInfo.AlbumMusicBrainzId = tag.Tag.MusicBrainzReleaseId;
            if (!string.IsNullOrEmpty(tag.Tag.MusicBrainzDiscId))
              trackInfo.AlbumMusicBrainzDiscId = tag.Tag.MusicBrainzDiscId;
            if (!string.IsNullOrEmpty(tag.Tag.AmazonId))
              trackInfo.AlbumAmazonId = tag.Tag.AmazonId;
            if (!string.IsNullOrEmpty(tag.Tag.MusicIpId))
              trackInfo.MusicIpId = tag.Tag.MusicIpId;

            trackInfo.Artists = new List<PersonInfo>();
            if (artists != null)
            {
              foreach (string artistName in ApplyAdditionalSeparator(artists))
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

            //Save id if possible
            if (trackInfo.Artists.Count == 1 && !string.IsNullOrEmpty(tag.Tag.MusicBrainzArtistId))
            {
              trackInfo.Artists[0].MusicBrainzId = tag.Tag.MusicBrainzArtistId;
            }

            IEnumerable<string> albumArtists = tag.Tag.AlbumArtists;
            if ((tag.TagTypes & TagTypes.Id3v2) != 0)
              albumArtists = PatchID3v23Enumeration(albumArtists);
            trackInfo.AlbumArtists = new List<PersonInfo>();
            if (albumArtists != null)
            {
              foreach (string artistName in ApplyAdditionalSeparator(albumArtists))
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

            //Save id if possible
            if (trackInfo.AlbumArtists.Count == 1 && !string.IsNullOrEmpty(tag.Tag.MusicBrainzReleaseArtistId))
            {
              trackInfo.AlbumArtists[0].MusicBrainzId = tag.Tag.MusicBrainzReleaseArtistId;
            }

            IEnumerable<string> composers = tag.Tag.Composers;
            if ((tag.TagTypes & TagTypes.Id3v2) != 0)
              composers = PatchID3v23Enumeration(composers);
            trackInfo.Composers = new List<PersonInfo>();
            if (composers != null)
            {
              foreach (string composerName in ApplyAdditionalSeparator(composers))
              {
                trackInfo.Composers.Add(new PersonInfo()
                {
                  Name = composerName.Trim(),
                  Occupation = PersonAspect.OCCUPATION_COMPOSER,
                  ParentMediaName = trackInfo.Album,
                  MediaName = trackInfo.TrackName
                });
              }
            }

            if (tag.Tag.Genres.Length > 0)
            {
              IEnumerable<string> genres = tag.Tag.Genres;
              if ((tag.TagTypes & TagTypes.Id3v2) != 0)
                genres = PatchID3v23Enumeration(genres);
              trackInfo.Genres = ApplyAdditionalSeparator(genres).Select(s => new GenreInfo { Name = s.Trim() }).ToList();
              OnlineMatcherService.Instance.AssignMissingMusicGenreIds(trackInfo.Genres);
            }

            int year = (int)tag.Tag.Year;
            if (year >= 30 && year <= 99)
              year += 1900;
            if (year >= 1930 && year <= 2030)
              trackInfo.ReleaseDate = new DateTime(year, 1, 1);

            if (!trackInfo.HasThumbnail)
            {
              // The following code gets cover art images from file (embedded) or from windows explorer cache (supports folder.jpg).
              IPicture[] pics = tag.Tag.Pictures;
              if (pics.Length > 0)
              {
                try
                {
                  using (MemoryStream stream = new MemoryStream(pics[0].Data.Data))
                  {
                    trackInfo.Thumbnail = stream.ToArray();
                    trackInfo.HasChanged = true;
                  }
                }
                // Decoding of invalid image data can fail, but main MediaItem is correct.
                catch { }
              }
              else
              {
                // In quick mode only allow thumbs taken from cache.
                bool cachedOnly = importOnly;

                // Thumbnail extraction
                fileName = mediaItemAccessor.ResourcePathName;
                IThumbnailGenerator generator = ServiceRegistration.Get<IThumbnailGenerator>();
                byte[] thumbData;
                ImageType imageType;
                if (generator.GetThumbnail(fileName, cachedOnly, out thumbData, out imageType))
                {
                  trackInfo.Thumbnail = thumbData;
                  trackInfo.HasChanged = true;
                }
              }
            }
          }

          if (string.IsNullOrEmpty(trackInfo.Album) || trackInfo.Artists.Count == 0)
          {
            MusicNameMatcher.MatchTrack(fileName, trackInfo);
          }
        }

        //Determine compilation
        if (importOnly && !trackInfo.Compilation)
        {
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
            var albumMediaItemDirectoryPath = ResourcePathHelper.Combine(mediaItemPath, "../");
            var artistMediaItemDirectoryPath = ResourcePathHelper.Combine(mediaItemPath, "../../");

            if (IsDiscFolder(trackInfo.Album, albumMediaItemDirectoryPath.FileName))
            {
              //Probably a CD folder so try next parent
              artistMediaItemDirectoryPath = ResourcePathHelper.Combine(mediaItemPath, "../../../");
            }
            if (artistMediaItemDirectoryPath.FileName.IndexOf("Compilation", StringComparison.InvariantCultureIgnoreCase) >= 0)
            {
              trackInfo.Compilation = true;
            }
          }
        }

        if (!refresh)
        {
          //Check artists
          trackInfo.Artists = GetCorrectedArtistsList(trackInfo, trackInfo.Artists);
          trackInfo.AlbumArtists = GetCorrectedArtistsList(trackInfo, trackInfo.AlbumArtists);
        }

        trackInfo.AssignNameId();

        AudioCDMatcher.GetDiscMatchAndUpdate(mediaItemAccessor.ResourcePathName, trackInfo);

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

        if (refresh)
        {
          if ((IncludeArtistDetails && !BaseInfo.HasRelationship(extractedAspectData, PersonAspect.ROLE_ARTIST) && trackInfo.Artists.Count > 0) ||
            (IncludeArtistDetails && !BaseInfo.HasRelationship(extractedAspectData, PersonAspect.ROLE_ALBUMARTIST) && trackInfo.AlbumArtists.Count > 0) ||
            (IncludeComposerDetails && !BaseInfo.HasRelationship(extractedAspectData, PersonAspect.ROLE_COMPOSER) && trackInfo.Composers.Count > 0))
          {
            trackInfo.HasChanged = true;
          }
        }

        if (!trackInfo.HasChanged && !importOnly)
          return false;

        trackInfo.SetMetadata(extractedAspectData);

        if (importOnly)
        {
          //Store metadata for the Relationship Extractors
          if (IncludeArtistDetails)
          {
            IAudioRelationshipExtractor.StorePersons(extractedAspectData, trackInfo.Artists, false);
            IAudioRelationshipExtractor.StorePersons(extractedAspectData, trackInfo.AlbumArtists, true);
          }
          if (IncludeComposerDetails)
          {
            IAudioRelationshipExtractor.StorePersons(extractedAspectData, trackInfo.Composers, false);
          }
        }

        return trackInfo.IsBaseInfoPresent;
      }
      catch (UnsupportedFormatException)
      {
        ServiceRegistration.Get<ILogger>().Info("AudioMetadataExtractor: Unsupported audio file '{0}'", fsra.CanonicalLocalResourcePath);
        return false;
      }
      catch (Exception e)
      {
        // Only log at the info level here - And simply return false. This makes the importer know that we
        // couldn't perform our task here
        ServiceRegistration.Get<ILogger>().Info("AudioMetadataExtractor: Exception reading resource '{0}' (Text: '{1}')", fsra.CanonicalLocalResourcePath, e.Message);
      }
      return false;
    }

    protected List<PersonInfo> GetCorrectedArtistsList(TrackInfo trackInfo, List<PersonInfo> persons)
    {
      List<PersonInfo> resolvedList = new List<PersonInfo>();

      //Try to find correct artist names
      foreach (PersonInfo person in persons)
      {
        Match match = SPLIT_MULTIPLE_ARTISTS_REGEX.Match(person.Name);
        if (match.Success)
        {
          if (!string.IsNullOrEmpty(match.Groups["artist"].Value.Trim()) && !string.IsNullOrEmpty(match.Groups["artist2"].Value.Trim()))
          {
            PersonInfo tempPerson1 = new PersonInfo()
            {
              Name = match.Groups["artist"].Value.Trim(),
              Occupation = PersonAspect.OCCUPATION_ARTIST,
              ParentMediaName = trackInfo.Album,
              MediaName = trackInfo.TrackName
            };
            resolvedList.Add(tempPerson1);

            PersonInfo tempPerson2 = new PersonInfo()
            {
              Name = match.Groups["artist2"].Value.Trim(),
              Occupation = PersonAspect.OCCUPATION_ARTIST,
              ParentMediaName = trackInfo.Album,
              MediaName = trackInfo.TrackName
            };
            resolvedList.Add(tempPerson2);
          }
          else
          {
            resolvedList.Add(person);
          }
        }
        else
        {
          resolvedList.Add(person);
        }
      }

      return resolvedList;
    }

    #endregion
  }
}
