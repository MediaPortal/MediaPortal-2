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
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using System.Text.RegularExpressions;
using System.Collections;

namespace MediaPortal.Common.MediaManagement.Helpers
{
  /// <summary>
  /// <see cref="TrackInfo"/> contains information about a track. It's used as an interface structure for external 
  /// online data scrapers to fill in metadata.
  /// </summary>
  public class TrackInfo : BaseInfo, IComparable<TrackInfo>
  {
    /// <summary>
    /// Contains the ids of the minimum aspects that need to be present in order to test the equality of instances of this item.
    /// </summary>
    public static Guid[] EQUALITY_ASPECTS = new[] { AudioAspect.ASPECT_ID, ExternalIdentifierAspect.ASPECT_ID, MediaAspect.ASPECT_ID };
    /// <summary>
    /// Returns the index for "Album" used in <see cref="FormatString"/>.
    /// </summary>
    public static int ALBUM_INDEX = 0;
    /// <summary>
    /// Returns the index for "Track Number" used in <see cref="FormatString"/>.
    /// </summary>
    public static int TRACK_NO_INDEX = 1;
    /// <summary>
    /// Returns the index for "Track Name" used in <see cref="FormatString"/>.
    /// </summary>
    public static int TRACK_INDEX = 2;
    /// <summary>
    /// Format string that holds album name, album year, track number and name.
    /// </summary>
    public static string TRACK_FORMAT_STR = "{0}: {1} - {2}";
    /// <summary>
    /// Short format string that holds track number and name.
    /// </summary>
    public static string SHORT_FORMAT_STR = "{0} - {1}";

    protected static Regex _fromName = new Regex(@"(?<album>.*): (?<trackNum>\d+) - (?<track>.*)", RegexOptions.IgnoreCase);
    protected static Regex _fromAlbumName = new Regex(@"(?<album>.*) \((?<year>\d+)\)", RegexOptions.IgnoreCase);

    public string MusicBrainzId = null;
    public long AudioDbId = 0;
    public string IsrcId = null;
    public string MusicIpId = null;
    public long LyricId = 0;
    public long MvDbId = 0;
    public string NameId = null;

    public string Album = null;
    public string AlbumMusicBrainzId = null;
    public string AlbumMusicBrainzGroupId = null;
    public string AlbumMusicBrainzDiscId = null;
    public string AlbumCdDdId = null;
    public string AlbumUpcEanId = null;
    public long AlbumAudioDbId = 0;
    public string AlbumAmazonId = null;
    public string AlbumItunesId = null;
    public string AlbumNameId = null;

    public string TrackName = null;
    public string TrackNameSort = null;
    public string TrackLyrics = null;
    public DateTime? ReleaseDate = null;
    public int TrackNum = 0;
    public int TotalTracks = 0;
    public int DiscNum = 0;
    public int TotalDiscs = 0;
    public SimpleRating Rating = new SimpleRating();
    public bool Compilation = false;
    public int BitRate = 0;
    public long SampleRate = 0;
    public int Channels = 0;
    public long Duration = 0;
    public string Encoding = null;
    public bool AlbumHasOnlineCover = false;
    public bool AlbumHasBarcode = false;

    public List<PersonInfo> Artists = new List<PersonInfo>();
    public List<PersonInfo> AlbumArtists = new List<PersonInfo>();
    public List<PersonInfo> Composers = new List<PersonInfo>();
    public List<CompanyInfo> MusicLabels = new List<CompanyInfo>();
    public List<GenreInfo> Genres = new List<GenreInfo>();
    public List<string> Languages = new List<string>();

    public override bool IsBaseInfoPresent
    {
      get
      {
        if (string.IsNullOrEmpty(TrackName))
          return false;

        return true;
      }
    }

    public override bool HasExternalId
    {
      get
      {
        if (AudioDbId > 0)
          return true;
        if (LyricId > 0)
          return true;
        if (MvDbId > 0)
          return true;
        if (!string.IsNullOrEmpty(MusicBrainzId))
          return true;
        if (!string.IsNullOrEmpty(IsrcId))
          return true;
        if (!string.IsNullOrEmpty(MusicIpId))
          return true;

        if (AlbumAudioDbId > 0)
          return true;
        if (!string.IsNullOrEmpty(AlbumMusicBrainzId))
          return true;
        if (!string.IsNullOrEmpty(AlbumMusicBrainzGroupId))
          return true;
        if (!string.IsNullOrEmpty(AlbumCdDdId))
          return true;
        if (!string.IsNullOrEmpty(AlbumUpcEanId))
          return true;
        if (!string.IsNullOrEmpty(AlbumAmazonId))
          return true;
        if (!string.IsNullOrEmpty(AlbumItunesId))
          return true;

        return false;
      }
    }

    public override void AssignNameId()
    {
      if (!string.IsNullOrEmpty(Album))
      {
        if (AlbumArtists.Count > 0)
          AlbumNameId = AlbumArtists[0].Name + ":" + Album;
        else
          AlbumNameId = Album;
        AlbumNameId = GetNameId(AlbumNameId);

        if (!string.IsNullOrEmpty(TrackName))
        {
          NameId = Album + ":" + TrackName;
          NameId = GetNameId(NameId);
        }
      }
      else
      {
        if (!string.IsNullOrEmpty(TrackName))
        {
          NameId = TrackName;
          if (TrackNum > 0)
            NameId = TrackNum.ToString() + "_" + NameId;
          NameId = GetNameId(NameId);
        }
      }
    }

    public TrackInfo Clone()
    {
      return CloneProperties(this);
    }

    #region Members

    /// <summary>
    /// Copies the contained track information into MediaItemAspect.
    /// </summary>
    /// <param name="aspectData">Dictionary with extracted aspects.</param>
    public override bool SetMetadata(IDictionary<Guid, IList<MediaItemAspect>> aspectData)
    {
      if (string.IsNullOrEmpty(TrackName)) return false;

      SetMetadataChanged(aspectData);

      MediaItemAspect.SetAttribute(aspectData, MediaAspect.ATTR_TITLE, ToString());
      if (!string.IsNullOrEmpty(TrackNameSort))
        MediaItemAspect.SetAttribute(aspectData, MediaAspect.ATTR_SORT_TITLE, TrackNameSort);
      else
        MediaItemAspect.SetAttribute(aspectData, MediaAspect.ATTR_SORT_TITLE, GetSortTitle(TrackName));
      MediaItemAspect.SetAttribute(aspectData, MediaAspect.ATTR_ISVIRTUAL, IsVirtualResource(aspectData));
      MediaItemAspect.SetAttribute(aspectData, AudioAspect.ATTR_TRACKNAME, TrackName);
      MediaItemAspect.SetAttribute(aspectData, AudioAspect.ATTR_COMPILATION, Compilation);
      if (!string.IsNullOrEmpty(TrackLyrics)) MediaItemAspect.SetAttribute(aspectData, AudioAspect.ATTR_LYRICS, TrackLyrics);
      if (ReleaseDate.HasValue) MediaItemAspect.SetAttribute(aspectData, MediaAspect.ATTR_RECORDINGTIME, ReleaseDate.Value);
      if (TrackNum > 0) MediaItemAspect.SetAttribute(aspectData, AudioAspect.ATTR_TRACK, TrackNum);
      if (TotalTracks > 0) MediaItemAspect.SetAttribute(aspectData, AudioAspect.ATTR_NUMTRACKS, TotalTracks);
      if (Duration > 0) MediaItemAspect.SetAttribute(aspectData, AudioAspect.ATTR_DURATION, Duration);
      MediaItemAspect.SetAttribute(aspectData, AudioAspect.ATTR_ISCD, false);

      if (!string.IsNullOrEmpty(MusicBrainzId)) MediaItemAspect.AddOrUpdateExternalIdentifier(aspectData, ExternalIdentifierAspect.SOURCE_MUSICBRAINZ, ExternalIdentifierAspect.TYPE_TRACK, MusicBrainzId);
      if (!string.IsNullOrEmpty(IsrcId)) MediaItemAspect.AddOrUpdateExternalIdentifier(aspectData, ExternalIdentifierAspect.SOURCE_ISRC, ExternalIdentifierAspect.TYPE_TRACK, IsrcId);
      if (!string.IsNullOrEmpty(MusicIpId)) MediaItemAspect.AddOrUpdateExternalIdentifier(aspectData, ExternalIdentifierAspect.SOURCE_MUSIC_IP, ExternalIdentifierAspect.TYPE_TRACK, MusicIpId);
      if (AudioDbId > 0) MediaItemAspect.AddOrUpdateExternalIdentifier(aspectData, ExternalIdentifierAspect.SOURCE_AUDIODB, ExternalIdentifierAspect.TYPE_TRACK, AudioDbId.ToString());
      if (LyricId > 0) MediaItemAspect.AddOrUpdateExternalIdentifier(aspectData, ExternalIdentifierAspect.SOURCE_LYRIC, ExternalIdentifierAspect.TYPE_TRACK, LyricId.ToString());
      if (MvDbId > 0) MediaItemAspect.AddOrUpdateExternalIdentifier(aspectData, ExternalIdentifierAspect.SOURCE_MVDB, ExternalIdentifierAspect.TYPE_TRACK, MvDbId.ToString());
      if (!string.IsNullOrEmpty(NameId)) MediaItemAspect.AddOrUpdateExternalIdentifier(aspectData, ExternalIdentifierAspect.SOURCE_NAME, ExternalIdentifierAspect.TYPE_TRACK, NameId);

      if (!string.IsNullOrEmpty(Album)) MediaItemAspect.SetAttribute(aspectData, AudioAspect.ATTR_ALBUM, Album);
      if (DiscNum > 0) MediaItemAspect.SetAttribute(aspectData, AudioAspect.ATTR_DISCID, DiscNum);
      if (TotalDiscs > 0) MediaItemAspect.SetAttribute(aspectData, AudioAspect.ATTR_NUMDISCS, TotalDiscs);
      if (BitRate > 0) MediaItemAspect.SetAttribute(aspectData, AudioAspect.ATTR_BITRATE, BitRate);
      if (Channels > 0) MediaItemAspect.SetAttribute(aspectData, AudioAspect.ATTR_CHANNELS, Channels);
      if (SampleRate > 0) MediaItemAspect.SetAttribute(aspectData, AudioAspect.ATTR_SAMPLERATE, SampleRate);
      if (!string.IsNullOrEmpty(Encoding)) MediaItemAspect.SetAttribute(aspectData, AudioAspect.ATTR_ENCODING, Encoding);

      if (!Rating.IsEmpty)
      {
        MediaItemAspect.SetAttribute(aspectData, AudioAspect.ATTR_TOTAL_RATING, Rating.RatingValue.Value);
        if (Rating.VoteCount.HasValue) MediaItemAspect.SetAttribute(aspectData, AudioAspect.ATTR_RATING_COUNT, Rating.VoteCount.Value);
      }

      if (!string.IsNullOrEmpty(AlbumUpcEanId)) MediaItemAspect.AddOrUpdateExternalIdentifier(aspectData, ExternalIdentifierAspect.SOURCE_UPCEAN, ExternalIdentifierAspect.TYPE_ALBUM, AlbumUpcEanId);
      if (!string.IsNullOrEmpty(AlbumCdDdId)) MediaItemAspect.AddOrUpdateExternalIdentifier(aspectData, ExternalIdentifierAspect.SOURCE_CDDB, ExternalIdentifierAspect.TYPE_ALBUM, AlbumCdDdId);
      if (!string.IsNullOrEmpty(AlbumMusicBrainzId)) MediaItemAspect.AddOrUpdateExternalIdentifier(aspectData, ExternalIdentifierAspect.SOURCE_MUSICBRAINZ, ExternalIdentifierAspect.TYPE_ALBUM, AlbumMusicBrainzId);
      if (!string.IsNullOrEmpty(AlbumMusicBrainzGroupId)) MediaItemAspect.AddOrUpdateExternalIdentifier(aspectData, ExternalIdentifierAspect.SOURCE_MUSICBRAINZ_GROUP, ExternalIdentifierAspect.TYPE_ALBUM, AlbumMusicBrainzGroupId);
      if (AlbumAudioDbId > 0) MediaItemAspect.AddOrUpdateExternalIdentifier(aspectData, ExternalIdentifierAspect.SOURCE_AUDIODB, ExternalIdentifierAspect.TYPE_ALBUM, AlbumAudioDbId.ToString());
      if (!string.IsNullOrEmpty(AlbumAmazonId)) MediaItemAspect.AddOrUpdateExternalIdentifier(aspectData, ExternalIdentifierAspect.SOURCE_AMAZON, ExternalIdentifierAspect.TYPE_ALBUM, AlbumAmazonId);
      if (!string.IsNullOrEmpty(AlbumItunesId)) MediaItemAspect.AddOrUpdateExternalIdentifier(aspectData, ExternalIdentifierAspect.SOURCE_ITUNES, ExternalIdentifierAspect.TYPE_ALBUM, AlbumItunesId);
      if (!string.IsNullOrEmpty(AlbumNameId)) MediaItemAspect.AddOrUpdateExternalIdentifier(aspectData, ExternalIdentifierAspect.SOURCE_NAME, ExternalIdentifierAspect.TYPE_ALBUM, AlbumNameId);

      if (Artists.Count > 0) MediaItemAspect.SetCollectionAttribute(aspectData, AudioAspect.ATTR_ARTISTS, Artists.Where(p => !string.IsNullOrEmpty(p.Name)).Select(p => p.Name).ToList());
      if (AlbumArtists.Count > 0) MediaItemAspect.SetCollectionAttribute(aspectData, AudioAspect.ATTR_ALBUMARTISTS, AlbumArtists.Where(p => !string.IsNullOrEmpty(p.Name)).Select(p => p.Name).ToList());
      if (Composers.Count > 0) MediaItemAspect.SetCollectionAttribute(aspectData, AudioAspect.ATTR_COMPOSERS, Composers.Where(p => !string.IsNullOrEmpty(p.Name)).Select(p => p.Name).ToList());

      aspectData.Remove(GenreAspect.ASPECT_ID);
      foreach (GenreInfo genre in Genres.Distinct())
      {
        MultipleMediaItemAspect genreAspect = MediaItemAspect.CreateAspect(aspectData, GenreAspect.Metadata);
        genreAspect.SetAttribute(GenreAspect.ATTR_ID, genre.Id);
        genreAspect.SetAttribute(GenreAspect.ATTR_GENRE, genre.Name);
      }

      SetThumbnailMetadata(aspectData);

      return true;
    }

    public override bool FromMetadata(IDictionary<Guid, IList<MediaItemAspect>> aspectData)
    {
      if (!aspectData.ContainsKey(AudioAspect.ASPECT_ID))
        return false;

      GetMetadataChanged(aspectData);

      MediaItemAspect.TryGetAttribute(aspectData, AudioAspect.ATTR_TRACKNAME, out TrackName);
      MediaItemAspect.TryGetAttribute(aspectData, MediaAspect.ATTR_SORT_TITLE, out TrackNameSort);
      MediaItemAspect.TryGetAttribute(aspectData, AudioAspect.ATTR_LYRICS, out TrackLyrics);
      MediaItemAspect.TryGetAttribute(aspectData, MediaAspect.ATTR_RECORDINGTIME, out ReleaseDate);
      MediaItemAspect.TryGetAttribute(aspectData, AudioAspect.ATTR_TRACK, out TrackNum);
      MediaItemAspect.TryGetAttribute(aspectData, AudioAspect.ATTR_NUMTRACKS, out TotalTracks);
      MediaItemAspect.TryGetAttribute(aspectData, AudioAspect.ATTR_COMPILATION, out Compilation);
      MediaItemAspect.TryGetAttribute(aspectData, AudioAspect.ATTR_BITRATE, out BitRate);
      MediaItemAspect.TryGetAttribute(aspectData, AudioAspect.ATTR_SAMPLERATE, out SampleRate);
      MediaItemAspect.TryGetAttribute(aspectData, AudioAspect.ATTR_CHANNELS, out Channels);
      MediaItemAspect.TryGetAttribute(aspectData, AudioAspect.ATTR_ALBUM, out Album);
      MediaItemAspect.TryGetAttribute(aspectData, AudioAspect.ATTR_DISCID, out DiscNum);
      MediaItemAspect.TryGetAttribute(aspectData, AudioAspect.ATTR_NUMDISCS, out TotalDiscs);
      MediaItemAspect.TryGetAttribute(aspectData, AudioAspect.ATTR_DURATION, out Duration);
      MediaItemAspect.TryGetAttribute(aspectData, AudioAspect.ATTR_ENCODING, out Encoding);

      double? rating;
      MediaItemAspect.TryGetAttribute(aspectData, AudioAspect.ATTR_TOTAL_RATING, out rating);
      int? voteCount;
      MediaItemAspect.TryGetAttribute(aspectData, AudioAspect.ATTR_RATING_COUNT, out voteCount);
      Rating = new SimpleRating(rating, voteCount);

      string id;
      if (MediaItemAspect.TryGetExternalAttribute(aspectData, ExternalIdentifierAspect.SOURCE_AUDIODB, ExternalIdentifierAspect.TYPE_TRACK, out id))
        AudioDbId = Convert.ToInt64(id);
      if (MediaItemAspect.TryGetExternalAttribute(aspectData, ExternalIdentifierAspect.SOURCE_LYRIC, ExternalIdentifierAspect.TYPE_TRACK, out id))
        LyricId = Convert.ToInt64(id);
      if (MediaItemAspect.TryGetExternalAttribute(aspectData, ExternalIdentifierAspect.SOURCE_MVDB, ExternalIdentifierAspect.TYPE_TRACK, out id))
        MvDbId = Convert.ToInt64(id);
      MediaItemAspect.TryGetExternalAttribute(aspectData, ExternalIdentifierAspect.SOURCE_MUSICBRAINZ, ExternalIdentifierAspect.TYPE_TRACK, out MusicBrainzId);
      MediaItemAspect.TryGetExternalAttribute(aspectData, ExternalIdentifierAspect.SOURCE_ISRC, ExternalIdentifierAspect.TYPE_TRACK, out IsrcId);
      MediaItemAspect.TryGetExternalAttribute(aspectData, ExternalIdentifierAspect.SOURCE_MUSIC_IP, ExternalIdentifierAspect.TYPE_TRACK, out MusicIpId);
      MediaItemAspect.TryGetExternalAttribute(aspectData, ExternalIdentifierAspect.SOURCE_NAME, ExternalIdentifierAspect.TYPE_TRACK, out NameId);

      if (MediaItemAspect.TryGetExternalAttribute(aspectData, ExternalIdentifierAspect.SOURCE_AUDIODB, ExternalIdentifierAspect.TYPE_ALBUM, out id))
        AlbumAudioDbId = Convert.ToInt64(id);
      MediaItemAspect.TryGetExternalAttribute(aspectData, ExternalIdentifierAspect.SOURCE_MUSICBRAINZ, ExternalIdentifierAspect.TYPE_ALBUM, out AlbumMusicBrainzId);
      MediaItemAspect.TryGetExternalAttribute(aspectData, ExternalIdentifierAspect.SOURCE_MUSICBRAINZ_GROUP, ExternalIdentifierAspect.TYPE_ALBUM, out AlbumMusicBrainzGroupId);
      MediaItemAspect.TryGetExternalAttribute(aspectData, ExternalIdentifierAspect.SOURCE_CDDB, ExternalIdentifierAspect.TYPE_ALBUM, out AlbumCdDdId);
      MediaItemAspect.TryGetExternalAttribute(aspectData, ExternalIdentifierAspect.SOURCE_UPCEAN, ExternalIdentifierAspect.TYPE_ALBUM, out AlbumUpcEanId);
      MediaItemAspect.TryGetExternalAttribute(aspectData, ExternalIdentifierAspect.SOURCE_AMAZON, ExternalIdentifierAspect.TYPE_ALBUM, out AlbumAmazonId);
      MediaItemAspect.TryGetExternalAttribute(aspectData, ExternalIdentifierAspect.SOURCE_ITUNES, ExternalIdentifierAspect.TYPE_ALBUM, out AlbumItunesId);
      MediaItemAspect.TryGetExternalAttribute(aspectData, ExternalIdentifierAspect.SOURCE_NAME, ExternalIdentifierAspect.TYPE_ALBUM, out AlbumNameId);

      //Brownard 17.06.2016
      //The returned type of the collection differs on the server and client.
      //On the server it's an object collection but on the client it's a string collection due to [de]serialization.
      //Use the non generic Ienumerable to allow for both types.
      IEnumerable collection;
      Artists.Clear();
      if (MediaItemAspect.TryGetAttribute(aspectData, AudioAspect.ATTR_ARTISTS, out collection))
        Artists.AddRange(collection.Cast<string>().Select(s => new PersonInfo { Name = s, Occupation = PersonAspect.OCCUPATION_ARTIST }));

      AlbumArtists.Clear();
      if (MediaItemAspect.TryGetAttribute(aspectData, AudioAspect.ATTR_ALBUMARTISTS, out collection))
        AlbumArtists.AddRange(collection.Cast<string>().Select(s => new PersonInfo { Name = s, Occupation = PersonAspect.OCCUPATION_ARTIST }));

      Composers.Clear();
      if (MediaItemAspect.TryGetAttribute(aspectData, AudioAspect.ATTR_COMPOSERS, out collection))
        Composers.AddRange(collection.Cast<string>().Select(s => new PersonInfo { Name = s, Occupation = PersonAspect.OCCUPATION_COMPOSER }));

      Genres.Clear();
      IList<MultipleMediaItemAspect> genreAspects;
      if (MediaItemAspect.TryGetAspects(aspectData, GenreAspect.Metadata, out genreAspects))
      {
        foreach (MultipleMediaItemAspect genre in genreAspects)
        {
          Genres.Add(new GenreInfo
          {
            Id = genre.GetAttributeValue<int?>(GenreAspect.ATTR_ID),
            Name = genre.GetAttributeValue<string>(GenreAspect.ATTR_GENRE)
          });
        }
      }

      byte[] data;
      if (MediaItemAspect.TryGetAttribute(aspectData, ThumbnailLargeAspect.ATTR_THUMBNAIL, out data))
        HasThumbnail = true;

      return true;
    }

    public string ToShortString()
    {
      return string.Format(SHORT_FORMAT_STR, 0, TrackNum, TrackName);
    }

    public override bool FromString(string name)
    {
      if (name.Contains(":"))
      {
        Match match = _fromName.Match(name);
        if (match.Success)
        {
          Album = match.Groups["album"].Value;
          Match albumMatch = _fromAlbumName.Match(Album);
          if (albumMatch.Success)
          {
            Album = albumMatch.Groups["album"].Value;
            ReleaseDate = new DateTime(Convert.ToInt32(albumMatch.Groups["year"].Value), 1, 1);
          }
          TrackNum = Convert.ToInt32(match.Groups["trackNum"].Value);
          TrackName = match.Groups["track"].Value;
          return true;
        }
        return false;
      }
      TrackName = name;
      return true;
    }

    public override bool CopyIdsFrom<T>(T otherInstance)
    {
      if (otherInstance == null)
        return false;

      if (otherInstance is TrackInfo)
      {
        TrackInfo otherTrack = otherInstance as TrackInfo;
        AlbumAudioDbId = otherTrack.AlbumAudioDbId;
        AlbumCdDdId = otherTrack.AlbumCdDdId;
        AlbumMusicBrainzDiscId = otherTrack.AlbumMusicBrainzDiscId;
        AlbumMusicBrainzGroupId = otherTrack.AlbumMusicBrainzGroupId;
        AlbumMusicBrainzId = otherTrack.AlbumMusicBrainzId;
        AlbumAmazonId = otherTrack.AlbumAmazonId;
        AlbumItunesId = otherTrack.AlbumItunesId;
        AlbumNameId = otherTrack.AlbumNameId;

        AudioDbId = otherTrack.AudioDbId;
        MusicBrainzId = otherTrack.MusicBrainzId;
        MusicIpId = otherTrack.MusicIpId;
        MvDbId = otherTrack.MvDbId;
        LyricId = otherTrack.LyricId;
        NameId = otherTrack.NameId;
        return true;
      }
      return false;
    }

    public override T CloneBasicInstance<T>()
    {
      if (typeof(T) == typeof(AlbumInfo))
      {
        AlbumInfo info = new AlbumInfo();
        info.AudioDbId = AlbumAudioDbId;
        info.CdDdId = AlbumCdDdId;
        info.MusicBrainzDiscId = AlbumMusicBrainzDiscId;
        info.MusicBrainzGroupId = AlbumMusicBrainzGroupId;
        info.MusicBrainzId = AlbumMusicBrainzId;
        info.AmazonId = AlbumAmazonId;
        info.ItunesId = AlbumItunesId;
        info.NameId = AlbumNameId;

        info.Album = Album;
        info.DiscNum = DiscNum;
        info.TotalDiscs = TotalDiscs;
        info.TotalTracks = TotalTracks;
        info.Compilation = Compilation;
        info.ReleaseDate = ReleaseDate;
        info.Genres.AddRange(Genres);
        info.Languages.AddRange(Languages);
        info.Artists.AddRange(AlbumArtists);
        info.LastChanged = LastChanged;
        info.DateAdded = DateAdded;
        return (T)(object)info;
      }
      return default(T);
    }

    #endregion

    #region Overrides

    public override string ToString()
    {
      if (!string.IsNullOrEmpty(Album) && TrackNum > 0)
        return string.Format(TRACK_FORMAT_STR, Album, TrackNum, string.IsNullOrEmpty(TrackName) ? "[Unnamed Track]" : TrackName);

      if (TrackNum > 0)
        return string.Format(SHORT_FORMAT_STR, TrackNum, string.IsNullOrEmpty(TrackName) ? "[Unnamed Track]" : TrackName);

      return string.IsNullOrEmpty(TrackName) ? "[Unnamed Track]" : TrackName;
    }

    public override int GetHashCode()
    {
      //TODO: Check if this is functional
      if (string.IsNullOrEmpty(NameId))
        AssignNameId();
      return string.IsNullOrEmpty(NameId) ? "[Unnamed Track]".GetHashCode() : NameId.GetHashCode();
    }

    public override bool Equals(object obj)
    {
      TrackInfo other = obj as TrackInfo;
      if (other == null) return false;

      if (AudioDbId > 0 && other.AudioDbId > 0)
        return AudioDbId == other.AudioDbId;
      if (MvDbId > 0 && other.MvDbId > 0)
        return MvDbId == other.MvDbId;
      if (LyricId > 0 && other.LyricId > 0)
        return LyricId == other.LyricId;
      if (!string.IsNullOrEmpty(MusicBrainzId) && !string.IsNullOrEmpty(other.MusicBrainzId))
        return string.Equals(MusicBrainzId, other.MusicBrainzId, StringComparison.InvariantCultureIgnoreCase);
      if (!string.IsNullOrEmpty(MusicIpId) && !string.IsNullOrEmpty(other.MusicIpId))
        return string.Equals(MusicIpId, other.MusicIpId, StringComparison.InvariantCultureIgnoreCase);
      if (!string.IsNullOrEmpty(IsrcId) && !string.IsNullOrEmpty(other.IsrcId))
        return string.Equals(IsrcId, other.IsrcId, StringComparison.InvariantCultureIgnoreCase);
      if (!string.IsNullOrEmpty(NameId) && !string.IsNullOrEmpty(other.NameId))
        return string.Equals(NameId, other.NameId, StringComparison.InvariantCultureIgnoreCase);

      if (TrackNum > 0 && other.TrackNum > 0 && TrackNum == other.TrackNum)
      {
        if (AlbumAudioDbId > 0 && other.AlbumAudioDbId > 0)
          return AlbumAudioDbId == other.AlbumAudioDbId;
        if (!string.IsNullOrEmpty(AlbumMusicBrainzDiscId) && !string.IsNullOrEmpty(other.AlbumMusicBrainzDiscId))
          return string.Equals(AlbumMusicBrainzDiscId, other.AlbumMusicBrainzDiscId, StringComparison.InvariantCultureIgnoreCase);
        if (!string.IsNullOrEmpty(AlbumMusicBrainzGroupId) && !string.IsNullOrEmpty(other.AlbumMusicBrainzGroupId))
          return string.Equals(AlbumMusicBrainzGroupId, other.AlbumMusicBrainzGroupId, StringComparison.InvariantCultureIgnoreCase);
        if (!string.IsNullOrEmpty(AlbumMusicBrainzId) && !string.IsNullOrEmpty(other.AlbumMusicBrainzId))
          return string.Equals(AlbumMusicBrainzId, other.AlbumMusicBrainzId, StringComparison.InvariantCultureIgnoreCase);
        if (!string.IsNullOrEmpty(AlbumCdDdId) && !string.IsNullOrEmpty(other.AlbumCdDdId))
          return string.Equals(AlbumCdDdId, other.AlbumCdDdId, StringComparison.InvariantCultureIgnoreCase);
        if (!string.IsNullOrEmpty(AlbumAmazonId) && !string.IsNullOrEmpty(other.AlbumAmazonId))
          return string.Equals(AlbumAmazonId, other.AlbumAmazonId, StringComparison.InvariantCultureIgnoreCase);
        if (!string.IsNullOrEmpty(AlbumItunesId) && !string.IsNullOrEmpty(other.AlbumItunesId))
          return string.Equals(AlbumItunesId, other.AlbumItunesId, StringComparison.InvariantCultureIgnoreCase);
        if (!string.IsNullOrEmpty(AlbumNameId) && !string.IsNullOrEmpty(other.AlbumNameId))
          return string.Equals(AlbumNameId, other.AlbumNameId, StringComparison.InvariantCultureIgnoreCase);

        if (!string.IsNullOrEmpty(Album) && !string.IsNullOrEmpty(other.Album) &&
          ReleaseDate.HasValue && other.ReleaseDate.HasValue)
          return Album == other.Album && ReleaseDate.Value == other.ReleaseDate.Value;
      }
      if ((!string.IsNullOrEmpty(Album) || !string.IsNullOrEmpty(other.Album)) && Album != other.Album)
        return false;

      if (!string.IsNullOrEmpty(TrackName) && !string.IsNullOrEmpty(other.TrackName) && MatchNames(TrackName, other.TrackName))
      {
        if (Artists.Count > 0 && other.Artists.Count > 0 && ReleaseDate.HasValue && other.ReleaseDate.HasValue)
          return Artists.SequenceEqual(other.Artists) && ReleaseDate.Value == other.ReleaseDate.Value;
        if (AlbumArtists.Count > 0 && other.AlbumArtists.Count > 0 && ReleaseDate.HasValue && other.ReleaseDate.HasValue)
          return AlbumArtists.SequenceEqual(other.AlbumArtists) && ReleaseDate.Value == other.ReleaseDate.Value;
        if (Artists.Count > 0 && other.Artists.Count > 0)
          return Artists.SequenceEqual(other.Artists);
        if (AlbumArtists.Count > 0 && other.AlbumArtists.Count > 0)
          return AlbumArtists.SequenceEqual(other.AlbumArtists);
        if (ReleaseDate.HasValue && other.ReleaseDate.HasValue)
          return ReleaseDate.Value == other.ReleaseDate.Value;
      }

      return false;
    }

    public int CompareTo(TrackInfo other)
    {
      if (!string.IsNullOrEmpty(Album) && !string.IsNullOrEmpty(other.Album) && Album == other.Album &&
        TrackNum > 0 && other.TrackNum > 0 && TrackNum != other.TrackNum)
        return TrackNum.CompareTo(other.TrackNum);
      if (string.IsNullOrEmpty(TrackName) || string.IsNullOrEmpty(other.TrackName))
        return 1;

      return TrackName.CompareTo(other.TrackName);
    }

    #endregion
  }
}
