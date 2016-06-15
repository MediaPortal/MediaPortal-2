#region Copyright (C) 2007-2015 Team MediaPortal

/*
    Copyright (C) 2007-2015 Team MediaPortal
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
using System.Collections.ObjectModel;

namespace MediaPortal.Common.MediaManagement.Helpers
{
  /// <summary>
  /// <see cref="TrackInfo"/> contains information about a track. It's used as an interface structure for external 
  /// online data scrapers to fill in metadata.
  /// </summary>
  public class TrackInfo : BaseInfo
  {
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
    public static string SHORT_FORMAT_STR = "{1} - {2}";
    /// <summary>
    /// Format string that holds album name and album year.
    /// </summary>
    public static string ALBUM_FORMAT_STR = "{0} ({1})";

    protected static Regex _fromName = new Regex(@"(?<album>.*): (?<trackNum>\d+) - (?<track>.*)", RegexOptions.IgnoreCase);
    protected static Regex _fromAlbumName = new Regex(@"(?<album>.*) \((?<year>\d+)\)", RegexOptions.IgnoreCase);

    public string MusicBrainzId = null;
    public long AudioDbId = 0;
    public string IsrcId = null;

    public string Album = null;
    public string AlbumMusicBrainzId = null;
    public string AlbumMusicBrainzGroupId = null;
    public string AlbumMusicBrainzDiscId = null;
    public string AlbumCdDdId = null;
    public string AlbumUpcEanId = null;
    public long AlbumAudioDbId = 0;

    public string TrackName = null;
    public string TrackLyrics = null;
    public DateTime? ReleaseDate = null;
    public int TrackNum = 0;
    public int TotalTracks = 0;
    public int DiscNum = 0;
    public int TotalDiscs = 0;
    public double TotalRating = 0;
    public int RatingCount = 0;
    public bool Compilation = false;
    public int BitRate = 0;
    public long Duration = 0;

    public List<PersonInfo> Artists = new List<PersonInfo>();
    public List<PersonInfo> AlbumArtists = new List<PersonInfo>();
    public List<PersonInfo> Composers = new List<PersonInfo>();
    public List<CompanyInfo> MusicLabels = new List<CompanyInfo>();
    public List<string> Genres = new List<string>();
    public List<string> Languages = new List<string>();

    #region Members

    /// <summary>
    /// Copies the contained track information into MediaItemAspect.
    /// </summary>
    /// <param name="aspectData">Dictionary with extracted aspects.</param>
    public bool SetMetadata(IDictionary<Guid, IList<MediaItemAspect>> aspectData)
    {
      if (string.IsNullOrEmpty(TrackName)) return false;

      MediaItemAspect.SetAttribute(aspectData, MediaAspect.ATTR_TITLE, ToString());
      MediaItemAspect.SetAttribute(aspectData, AudioAspect.ATTR_TRACKNAME, TrackName);
      MediaItemAspect.SetAttribute(aspectData, AudioAspect.ATTR_COMPILATION, Compilation);
      if (!string.IsNullOrEmpty(TrackLyrics)) MediaItemAspect.SetAttribute(aspectData, AudioAspect.ATTR_LYRICS, TrackLyrics);
      if (ReleaseDate.HasValue) MediaItemAspect.SetAttribute(aspectData, MediaAspect.ATTR_RECORDINGTIME, ReleaseDate.Value);
      if (TrackNum > 0) MediaItemAspect.SetAttribute(aspectData, AudioAspect.ATTR_TRACK, TrackNum);
      if (TotalTracks > 0) MediaItemAspect.SetAttribute(aspectData, AudioAspect.ATTR_NUMTRACKS, TotalTracks);
      if (Duration > 0) MediaItemAspect.SetAttribute(aspectData, AudioAspect.ATTR_DURATION, Duration);

      if (!string.IsNullOrEmpty(MusicBrainzId)) MediaItemAspect.AddOrUpdateExternalIdentifier(aspectData, ExternalIdentifierAspect.SOURCE_MUSICBRAINZ, ExternalIdentifierAspect.TYPE_TRACK, MusicBrainzId);
      if (!string.IsNullOrEmpty(IsrcId)) MediaItemAspect.AddOrUpdateExternalIdentifier(aspectData, ExternalIdentifierAspect.SOURCE_ISRC, ExternalIdentifierAspect.TYPE_TRACK, IsrcId);
      if (AudioDbId > 0) MediaItemAspect.AddOrUpdateExternalIdentifier(aspectData, ExternalIdentifierAspect.SOURCE_AUDIODB, ExternalIdentifierAspect.TYPE_TRACK, AudioDbId.ToString());

      if (!string.IsNullOrEmpty(Album)) MediaItemAspect.SetAttribute(aspectData, AudioAspect.ATTR_ALBUM, Album);
      if (DiscNum > 0) MediaItemAspect.SetAttribute(aspectData, AudioAspect.ATTR_DISCID, DiscNum);
      if (TotalDiscs > 0) MediaItemAspect.SetAttribute(aspectData, AudioAspect.ATTR_NUMDISCS, TotalDiscs);

      if (TotalRating > 0d) MediaItemAspect.SetAttribute(aspectData, AudioAspect.ATTR_TOTAL_RATING, TotalRating);
      if (RatingCount > 0) MediaItemAspect.SetAttribute(aspectData, AudioAspect.ATTR_RATING_COUNT, RatingCount);
      if(BitRate > 0) MediaItemAspect.SetAttribute(aspectData, AudioAspect.ATTR_BITRATE, BitRate);

      if (!string.IsNullOrEmpty(AlbumUpcEanId)) MediaItemAspect.AddOrUpdateExternalIdentifier(aspectData, ExternalIdentifierAspect.SOURCE_UPCEAN, ExternalIdentifierAspect.TYPE_ALBUM, AlbumUpcEanId);
      if (!string.IsNullOrEmpty(AlbumCdDdId)) MediaItemAspect.AddOrUpdateExternalIdentifier(aspectData, ExternalIdentifierAspect.SOURCE_CDDB, ExternalIdentifierAspect.TYPE_ALBUM, AlbumCdDdId);
      if (!string.IsNullOrEmpty(AlbumMusicBrainzId)) MediaItemAspect.AddOrUpdateExternalIdentifier(aspectData, ExternalIdentifierAspect.SOURCE_MUSICBRAINZ, ExternalIdentifierAspect.TYPE_ALBUM, AlbumMusicBrainzId);
      if (!string.IsNullOrEmpty(AlbumMusicBrainzGroupId)) MediaItemAspect.AddOrUpdateExternalIdentifier(aspectData, ExternalIdentifierAspect.SOURCE_MUSICBRAINZ_GROUP, ExternalIdentifierAspect.TYPE_ALBUM, AlbumMusicBrainzGroupId);
      if (AlbumAudioDbId > 0) MediaItemAspect.AddOrUpdateExternalIdentifier(aspectData, ExternalIdentifierAspect.SOURCE_AUDIODB, ExternalIdentifierAspect.TYPE_ALBUM, AlbumAudioDbId.ToString());

      if (Artists.Count > 0) MediaItemAspect.SetCollectionAttribute(aspectData, AudioAspect.ATTR_ARTISTS, Artists.Select(p => p.Name).ToList<object>());
      if (AlbumArtists.Count > 0) MediaItemAspect.SetCollectionAttribute(aspectData, AudioAspect.ATTR_ALBUMARTISTS, AlbumArtists.Select(p => p.Name).ToList<object>());
      if (Composers.Count > 0) MediaItemAspect.SetCollectionAttribute(aspectData, AudioAspect.ATTR_COMPOSERS, Composers.Select(p => p.Name).ToList<object>());

      if (Genres.Count > 0) MediaItemAspect.SetCollectionAttribute(aspectData, AudioAspect.ATTR_GENRES, Genres.ToList<object>());

      SetThumbnailMetadata(aspectData);

      return true;
    }

    public bool FromMetadata(IDictionary<Guid, IList<MediaItemAspect>> aspectData)
    {
      if (!aspectData.ContainsKey(AudioAspect.ASPECT_ID))
        return false;

      MediaItemAspect.TryGetAttribute(aspectData, AudioAspect.ATTR_TRACKNAME, out TrackName);
      MediaItemAspect.TryGetAttribute(aspectData, AudioAspect.ATTR_LYRICS, out TrackLyrics);
      MediaItemAspect.TryGetAttribute(aspectData, MediaAspect.ATTR_RECORDINGTIME, out ReleaseDate);
      MediaItemAspect.TryGetAttribute(aspectData, AudioAspect.ATTR_TRACK, out TrackNum);
      MediaItemAspect.TryGetAttribute(aspectData, AudioAspect.ATTR_NUMTRACKS, out TotalTracks);
      MediaItemAspect.TryGetAttribute(aspectData, AudioAspect.ATTR_COMPILATION, out Compilation);
      MediaItemAspect.TryGetAttribute(aspectData, AudioAspect.ATTR_BITRATE, out BitRate);
      MediaItemAspect.TryGetAttribute(aspectData, AudioAspect.ATTR_ALBUM, out Album);
      MediaItemAspect.TryGetAttribute(aspectData, AudioAspect.ATTR_DISCID, out DiscNum);
      MediaItemAspect.TryGetAttribute(aspectData, AudioAspect.ATTR_NUMDISCS, out TotalDiscs);
      MediaItemAspect.TryGetAttribute(aspectData, AudioAspect.ATTR_DURATION, out Duration);

      MediaItemAspect.TryGetAttribute(aspectData, AudioAspect.ATTR_TOTAL_RATING, out TotalRating);
      MediaItemAspect.TryGetAttribute(aspectData, AudioAspect.ATTR_RATING_COUNT, out RatingCount);

      string id;
      if (MediaItemAspect.TryGetExternalAttribute(aspectData, ExternalIdentifierAspect.SOURCE_AUDIODB, ExternalIdentifierAspect.TYPE_TRACK, out id))
        AudioDbId = Convert.ToInt32(id);
      MediaItemAspect.TryGetExternalAttribute(aspectData, ExternalIdentifierAspect.SOURCE_MUSICBRAINZ, ExternalIdentifierAspect.TYPE_TRACK, out MusicBrainzId);
      MediaItemAspect.TryGetExternalAttribute(aspectData, ExternalIdentifierAspect.SOURCE_ISRC, ExternalIdentifierAspect.TYPE_TRACK, out IsrcId);

      if (MediaItemAspect.TryGetExternalAttribute(aspectData, ExternalIdentifierAspect.SOURCE_AUDIODB, ExternalIdentifierAspect.TYPE_ALBUM, out id))
        AlbumAudioDbId = Convert.ToInt32(id);
      MediaItemAspect.TryGetExternalAttribute(aspectData, ExternalIdentifierAspect.SOURCE_MUSICBRAINZ, ExternalIdentifierAspect.TYPE_ALBUM, out AlbumMusicBrainzId);
      MediaItemAspect.TryGetExternalAttribute(aspectData, ExternalIdentifierAspect.SOURCE_MUSICBRAINZ_GROUP, ExternalIdentifierAspect.TYPE_ALBUM, out AlbumMusicBrainzGroupId);
      MediaItemAspect.TryGetExternalAttribute(aspectData, ExternalIdentifierAspect.SOURCE_CDDB, ExternalIdentifierAspect.TYPE_ALBUM, out AlbumCdDdId);
      MediaItemAspect.TryGetExternalAttribute(aspectData, ExternalIdentifierAspect.SOURCE_UPCEAN, ExternalIdentifierAspect.TYPE_ALBUM, out AlbumUpcEanId);

      ICollection<object> collection;
      Artists.Clear();
      if (MediaItemAspect.TryGetAttribute(aspectData, AudioAspect.ATTR_ARTISTS, out collection))
        Artists.AddRange(collection.Select(s => new PersonInfo() { Name = s.ToString(), Occupation = PersonAspect.OCCUPATION_ARTIST }));

      AlbumArtists.Clear();
      if (MediaItemAspect.TryGetAttribute(aspectData, AudioAspect.ATTR_ALBUMARTISTS, out collection))
        AlbumArtists.AddRange(collection.Select(s => new PersonInfo() { Name = s.ToString(), Occupation = PersonAspect.OCCUPATION_ARTIST }));

      Composers.Clear();
      if (MediaItemAspect.TryGetAttribute(aspectData, AudioAspect.ATTR_COMPOSERS, out collection))
        Composers.AddRange(collection.Select(s => new PersonInfo() { Name = s.ToString(), Occupation = PersonAspect.OCCUPATION_COMPOSER }));

      Genres.Clear();
      if (MediaItemAspect.TryGetAttribute(aspectData, AudioAspect.ATTR_GENRES, out collection))
        Genres.AddRange(collection.Select(s => s.ToString()));

      byte[] data;
      if (MediaItemAspect.TryGetAttribute(aspectData, ThumbnailLargeAspect.ATTR_THUMBNAIL, out data))
        Thumbnail = data;

      return true;
    }

    public string ToShortString()
    {
      return string.Format(SHORT_FORMAT_STR, 0, TrackNum, TrackName);
    }

    public bool FromString(string name)
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

    public bool CopyIdsFrom(TrackInfo otherTrack)
    {
      AlbumAudioDbId = otherTrack.AlbumAudioDbId;
      AlbumCdDdId = otherTrack.AlbumCdDdId;
      AlbumMusicBrainzDiscId = otherTrack.AlbumMusicBrainzDiscId;
      AlbumMusicBrainzGroupId = otherTrack.AlbumMusicBrainzGroupId;
      AlbumMusicBrainzId = otherTrack.AlbumMusicBrainzId;

      AudioDbId = otherTrack.AudioDbId;
      MusicBrainzId = otherTrack.MusicBrainzId;
      return true;
    }

    public AlbumInfo CloneBasicAlbum()
    {
      AlbumInfo info = new AlbumInfo();
      info.AudioDbId = AlbumAudioDbId;
      info.CdDdId = AlbumCdDdId;
      info.MusicBrainzDiscId = AlbumMusicBrainzDiscId;
      info.MusicBrainzGroupId = AlbumMusicBrainzGroupId;
      info.MusicBrainzId = AlbumMusicBrainzId;

      info.Album = Album;
      info.DiscNum = DiscNum;
      info.TotalDiscs = TotalDiscs;
      info.TotalTracks = TotalTracks;
      info.Compilation = Compilation;
      return info;
    }

    #endregion

    #region Overrides

    public override string ToString()
    {
      //if (string.IsNullOrEmpty(Album))
        return TrackName;

      //Match albumMatch = _fromAlbumName.Match(Album);
      //return string.Format(TRACK_FORMAT_STR,
      //  ReleaseDate.HasValue && !albumMatch.Success ? string.Format(ALBUM_FORMAT_STR, Album, ReleaseDate.Value.Year) : Album,
      //  TrackNum,
      //  TrackName);
    }

    #endregion
  }
}
