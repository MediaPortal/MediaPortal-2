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
using System.Collections;

namespace MediaPortal.Common.MediaManagement.Helpers
{
  /// <summary>
  /// <see cref="AlbumInfo"/> contains information about an album. It's used as an interface structure for external 
  /// online data scrapers to fill in metadata.
  /// </summary>
  public class AlbumInfo : BaseInfo
  {
    /// <summary>
    /// Returns the index for "Album" used in <see cref="FormatString"/>.
    /// </summary>
    public static int ALBUM_INDEX = 0;
    /// <summary>
    /// Returns the index for "Year" used in <see cref="FormatString"/>.
    /// </summary>
    public static int ALBUM_YEAR_INDEX = 1;
    /// <summary>
    /// Format string that holds album name including release year.
    /// </summary>
    public static string ALBUM_FORMAT_STR = "{0} ({1})";
    /// <summary>
    /// Short format string that holds album name.
    /// </summary>
    public static string SHORT_FORMAT_STR = "{0}";

    protected static Regex _fromName = new Regex(@"(?<album>.*) \((?<year>\d+)\)", RegexOptions.IgnoreCase);

    public string MusicBrainzId = null;
    public string MusicBrainzGroupId = null;
    public string MusicBrainzDiscId = null;
    public long AudioDbId = 0;
    public string CdDdId = null;
    public string UpcEanId = null;
    public string AmazonId = null;
    public string ItunesId = null;

    public string Album = null;
    public SimpleTitle Description = null;
    public DateTime? ReleaseDate = null;
    public int TotalTracks = 0;
    public int DiscNum = 0;
    public int TotalDiscs = 0;
    public SimpleRating Rating = new SimpleRating();
    public long Sales = 0;
    public bool Compilation = false;

    public List<PersonInfo> Artists = new List<PersonInfo>();
    public List<CompanyInfo> MusicLabels = new List<CompanyInfo>();
    public List<string> Genres = new List<string>();
    public List<string> Awards = new List<string>();
    public List<string> Languages = new List<string>();
    public List<TrackInfo> Tracks = new List<TrackInfo>();

    public override bool IsBaseInfoPresent
    {
      get
      {
        if (string.IsNullOrEmpty(Album))
          return false;
        if (!ReleaseDate.HasValue)
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
        if (!string.IsNullOrEmpty(MusicBrainzId))
          return true;
        if (!string.IsNullOrEmpty(MusicBrainzGroupId))
          return true;
        if (!string.IsNullOrEmpty(CdDdId))
          return true;
        if (!string.IsNullOrEmpty(UpcEanId))
          return true;
        if (!string.IsNullOrEmpty(AmazonId))
          return true;

        return false;
      }
    }

    #region Members

    /// <summary>
    /// Copies the contained track information into MediaItemAspect.
    /// </summary>
    /// <param name="aspectData">Dictionary with extracted aspects.</param>
    public override bool SetMetadata(IDictionary<Guid, IList<MediaItemAspect>> aspectData)
    {
      if (string.IsNullOrEmpty(Album)) return false;

      MediaItemAspect.SetAttribute(aspectData, MediaAspect.ATTR_TITLE, ToString());
      MediaItemAspect.SetAttribute(aspectData, MediaAspect.ATTR_SORT_TITLE, GetSortTitle(Album));
      //MediaItemAspect.SetAttribute(aspectData, MediaAspect.ATTR_ISVIRTUAL, true); //Is maintained by medialibrary and metadataextractors
      MediaItemAspect.SetAttribute(aspectData, AudioAlbumAspect.ATTR_ALBUM, Album);
      MediaItemAspect.SetAttribute(aspectData, AudioAlbumAspect.ATTR_COMPILATION, Compilation);
      if (!Description.IsEmpty) MediaItemAspect.SetAttribute(aspectData, AudioAlbumAspect.ATTR_DESCRIPTION, Description.Text);
      if (DiscNum > 0) MediaItemAspect.SetAttribute(aspectData, AudioAlbumAspect.ATTR_DISCID, DiscNum);
      if (TotalDiscs > 0) MediaItemAspect.SetAttribute(aspectData, AudioAlbumAspect.ATTR_NUMDISCS, TotalDiscs);
      if (ReleaseDate.HasValue) MediaItemAspect.SetAttribute(aspectData, MediaAspect.ATTR_RECORDINGTIME, ReleaseDate.Value);
      if (TotalTracks > 0) MediaItemAspect.SetAttribute(aspectData, AudioAlbumAspect.ATTR_NUMTRACKS, TotalTracks);

      if (!string.IsNullOrEmpty(MusicBrainzId)) MediaItemAspect.AddOrUpdateExternalIdentifier(aspectData, ExternalIdentifierAspect.SOURCE_MUSICBRAINZ, ExternalIdentifierAspect.TYPE_ALBUM, MusicBrainzId);
      if (!string.IsNullOrEmpty(MusicBrainzGroupId)) MediaItemAspect.AddOrUpdateExternalIdentifier(aspectData, ExternalIdentifierAspect.SOURCE_MUSICBRAINZ_GROUP, ExternalIdentifierAspect.TYPE_ALBUM, MusicBrainzGroupId);
      if (AudioDbId > 0) MediaItemAspect.AddOrUpdateExternalIdentifier(aspectData, ExternalIdentifierAspect.SOURCE_AUDIODB, ExternalIdentifierAspect.TYPE_ALBUM, AudioDbId.ToString());
      if (!string.IsNullOrEmpty(CdDdId)) MediaItemAspect.AddOrUpdateExternalIdentifier(aspectData, ExternalIdentifierAspect.SOURCE_CDDB, ExternalIdentifierAspect.TYPE_ALBUM, CdDdId);
      if (!string.IsNullOrEmpty(UpcEanId)) MediaItemAspect.AddOrUpdateExternalIdentifier(aspectData, ExternalIdentifierAspect.SOURCE_UPCEAN, ExternalIdentifierAspect.TYPE_ALBUM, UpcEanId);
      if (!string.IsNullOrEmpty(AmazonId)) MediaItemAspect.AddOrUpdateExternalIdentifier(aspectData, ExternalIdentifierAspect.SOURCE_AMAZON, ExternalIdentifierAspect.TYPE_ALBUM, AmazonId);

      if (!Rating.IsEmpty)
      {
         MediaItemAspect.SetAttribute(aspectData, AudioAlbumAspect.ATTR_TOTAL_RATING, Rating.RatingValue.Value);
        if (Rating.VoteCount.HasValue) MediaItemAspect.SetAttribute(aspectData, AudioAlbumAspect.ATTR_RATING_COUNT, Rating.VoteCount.Value);
      }

      if (Artists.Count > 0) MediaItemAspect.SetCollectionAttribute(aspectData, AudioAlbumAspect.ATTR_ARTISTS, Artists.Where(p => !string.IsNullOrEmpty(p.Name)).Select(p => p.Name).ToList<object>());

      if (Genres.Count > 0) MediaItemAspect.SetCollectionAttribute(aspectData, AudioAlbumAspect.ATTR_GENRES, Genres.Where(g => !string.IsNullOrEmpty(g)).ToList<object>());
      if (Awards.Count > 0) MediaItemAspect.SetCollectionAttribute(aspectData, AudioAlbumAspect.ATTR_AWARDS, Awards.Where(a => !string.IsNullOrEmpty(a)).ToList<object>());

      if (MusicLabels.Count > 0) MediaItemAspect.SetCollectionAttribute(aspectData, AudioAlbumAspect.ATTR_LABELS, MusicLabels.Where(l => !string.IsNullOrEmpty(l.Name)).Select(l => l.Name).ToList<object>());

      SetThumbnailMetadata(aspectData);

      return true;
    }

    public override bool FromMetadata(IDictionary<Guid, IList<MediaItemAspect>> aspectData)
    {
      if (aspectData.ContainsKey(AudioAlbumAspect.ASPECT_ID))
      {
        MediaItemAspect.TryGetAttribute(aspectData, AudioAlbumAspect.ATTR_ALBUM, out Album);
        MediaItemAspect.TryGetAttribute(aspectData, AudioAlbumAspect.ATTR_DISCID, out DiscNum);
        MediaItemAspect.TryGetAttribute(aspectData, AudioAlbumAspect.ATTR_NUMDISCS, out TotalDiscs);
        MediaItemAspect.TryGetAttribute(aspectData, AudioAlbumAspect.ATTR_NUMTRACKS, out TotalTracks);
        MediaItemAspect.TryGetAttribute(aspectData, AudioAlbumAspect.ATTR_COMPILATION, out Compilation);
        MediaItemAspect.TryGetAttribute(aspectData, MediaAspect.ATTR_RECORDINGTIME, out ReleaseDate);

        string tempString;
        MediaItemAspect.TryGetAttribute(aspectData, AudioAlbumAspect.ATTR_DESCRIPTION, out tempString);
        Description = new SimpleTitle(tempString, false);

        string id;
        if (MediaItemAspect.TryGetExternalAttribute(aspectData, ExternalIdentifierAspect.SOURCE_AUDIODB, ExternalIdentifierAspect.TYPE_ALBUM, out id))
          AudioDbId = Convert.ToInt32(id);
        MediaItemAspect.TryGetExternalAttribute(aspectData, ExternalIdentifierAspect.SOURCE_MUSICBRAINZ, ExternalIdentifierAspect.TYPE_ALBUM, out MusicBrainzId);
        MediaItemAspect.TryGetExternalAttribute(aspectData, ExternalIdentifierAspect.SOURCE_MUSICBRAINZ_GROUP, ExternalIdentifierAspect.TYPE_ALBUM, out MusicBrainzGroupId);
        MediaItemAspect.TryGetExternalAttribute(aspectData, ExternalIdentifierAspect.SOURCE_CDDB, ExternalIdentifierAspect.TYPE_ALBUM, out CdDdId);
        MediaItemAspect.TryGetExternalAttribute(aspectData, ExternalIdentifierAspect.SOURCE_UPCEAN, ExternalIdentifierAspect.TYPE_ALBUM, out UpcEanId);
        MediaItemAspect.TryGetExternalAttribute(aspectData, ExternalIdentifierAspect.SOURCE_AMAZON, ExternalIdentifierAspect.TYPE_ALBUM, out AmazonId);

        double? rating;
        MediaItemAspect.TryGetAttribute(aspectData, AudioAlbumAspect.ATTR_TOTAL_RATING, out rating);
        int? voteCount;
        MediaItemAspect.TryGetAttribute(aspectData, AudioAlbumAspect.ATTR_RATING_COUNT, out voteCount);
        Rating = new SimpleRating(rating, voteCount);

        //Brownard 17.06.2016
        //The returned type of the collection differs on the server and client.
        //On the server it's an object collection but on the client it's a string collection due to [de]serialization.
        //Use the non generic Ienumerable to allow for both types.
        IEnumerable collection;
        Artists.Clear();
        if (MediaItemAspect.TryGetAttribute(aspectData, AudioAlbumAspect.ATTR_ARTISTS, out collection))
          Artists.AddRange(collection.Cast<object>().Select(s => new PersonInfo() { Name = s.ToString(), Occupation = PersonAspect.OCCUPATION_ARTIST }));

        Genres.Clear();
        if (MediaItemAspect.TryGetAttribute(aspectData, AudioAlbumAspect.ATTR_GENRES, out collection))
          Genres.AddRange(collection.Cast<object>().Select(s => s.ToString()));

        Awards.Clear();
        if (MediaItemAspect.TryGetAttribute(aspectData, AudioAlbumAspect.ATTR_AWARDS, out collection))
          Awards.AddRange(collection.Cast<object>().Select(s => s.ToString()));

        MusicLabels.Clear();
        if (MediaItemAspect.TryGetAttribute(aspectData, AudioAlbumAspect.ATTR_LABELS, out collection))
          MusicLabels.AddRange(collection.Cast<object>().Select(s => new CompanyInfo() { Name = s.ToString(), Type = CompanyAspect.COMPANY_MUSIC_LABEL }));

        byte[] data;
        if (MediaItemAspect.TryGetAttribute(aspectData, ThumbnailLargeAspect.ATTR_THUMBNAIL, out data))
          Thumbnail = data;

        return true;
      }
      else if (aspectData.ContainsKey(AudioAspect.ASPECT_ID))
      {
        MediaItemAspect.TryGetAttribute(aspectData, AudioAspect.ATTR_ALBUM, out Album);
        MediaItemAspect.TryGetAttribute(aspectData, AudioAspect.ATTR_DISCID, out DiscNum);
        MediaItemAspect.TryGetAttribute(aspectData, AudioAspect.ATTR_NUMDISCS, out TotalDiscs);
        MediaItemAspect.TryGetAttribute(aspectData, AudioAspect.ATTR_NUMTRACKS, out TotalTracks);
        MediaItemAspect.TryGetAttribute(aspectData, AudioAspect.ATTR_COMPILATION, out Compilation);

        string id;
        if (MediaItemAspect.TryGetExternalAttribute(aspectData, ExternalIdentifierAspect.SOURCE_AUDIODB, ExternalIdentifierAspect.TYPE_ALBUM, out id))
          AudioDbId = Convert.ToInt32(id);
        MediaItemAspect.TryGetExternalAttribute(aspectData, ExternalIdentifierAspect.SOURCE_MUSICBRAINZ, ExternalIdentifierAspect.TYPE_ALBUM, out MusicBrainzId);
        MediaItemAspect.TryGetExternalAttribute(aspectData, ExternalIdentifierAspect.SOURCE_MUSICBRAINZ_GROUP, ExternalIdentifierAspect.TYPE_ALBUM, out MusicBrainzGroupId);
        MediaItemAspect.TryGetExternalAttribute(aspectData, ExternalIdentifierAspect.SOURCE_CDDB, ExternalIdentifierAspect.TYPE_ALBUM, out CdDdId);
        MediaItemAspect.TryGetExternalAttribute(aspectData, ExternalIdentifierAspect.SOURCE_UPCEAN, ExternalIdentifierAspect.TYPE_ALBUM, out UpcEanId);
        MediaItemAspect.TryGetExternalAttribute(aspectData, ExternalIdentifierAspect.SOURCE_AMAZON, ExternalIdentifierAspect.TYPE_ALBUM, out AmazonId);

        IEnumerable collection;
        Artists.Clear();
        if (MediaItemAspect.TryGetAttribute(aspectData, AudioAspect.ATTR_ALBUMARTISTS, out collection))
          Artists.AddRange(collection.Cast<object>().Select(s => new PersonInfo() { Name = s.ToString(), Occupation = PersonAspect.OCCUPATION_ARTIST }));

        return true;
      }
      else if (aspectData.ContainsKey(MediaAspect.ASPECT_ID))
      {
        MediaItemAspect.TryGetAttribute(aspectData, MediaAspect.ATTR_TITLE, out Album);

        string id;
        if (MediaItemAspect.TryGetExternalAttribute(aspectData, ExternalIdentifierAspect.SOURCE_AUDIODB, ExternalIdentifierAspect.TYPE_ALBUM, out id))
          AudioDbId = Convert.ToInt32(id);
        MediaItemAspect.TryGetExternalAttribute(aspectData, ExternalIdentifierAspect.SOURCE_MUSICBRAINZ, ExternalIdentifierAspect.TYPE_ALBUM, out MusicBrainzId);
        MediaItemAspect.TryGetExternalAttribute(aspectData, ExternalIdentifierAspect.SOURCE_MUSICBRAINZ_GROUP, ExternalIdentifierAspect.TYPE_ALBUM, out MusicBrainzGroupId);
        MediaItemAspect.TryGetExternalAttribute(aspectData, ExternalIdentifierAspect.SOURCE_CDDB, ExternalIdentifierAspect.TYPE_ALBUM, out CdDdId);
        MediaItemAspect.TryGetExternalAttribute(aspectData, ExternalIdentifierAspect.SOURCE_UPCEAN, ExternalIdentifierAspect.TYPE_ALBUM, out UpcEanId);
        MediaItemAspect.TryGetExternalAttribute(aspectData, ExternalIdentifierAspect.SOURCE_AMAZON, ExternalIdentifierAspect.TYPE_ALBUM, out AmazonId);

        byte[] data;
        if (MediaItemAspect.TryGetAttribute(aspectData, ThumbnailLargeAspect.ATTR_THUMBNAIL, out data))
          Thumbnail = data;
      }
      return false;
    }

    public string ToShortString()
    {
      return string.Format(SHORT_FORMAT_STR, Album);
    }

    public override bool FromString(string name)
    {
      if (name.Contains("("))
      {
        Match match = _fromName.Match(name);
        if (match.Success)
        {
          Album = match.Groups["album"].Value;
          int year = Convert.ToInt32(match.Groups["year"].Value);
          if (year > 0)
            ReleaseDate = new DateTime(year, 1, 1);
          return true;
        }
        return false;
      }
      Album = name;
      return true;
    }

    public override bool CopyIdsFrom<T>(T otherInstance)
    {
      if (otherInstance == null)
        return false;

      if (otherInstance is AlbumInfo)
      {
        AlbumInfo otherAlbum = otherInstance as AlbumInfo;
        AudioDbId = otherAlbum.AudioDbId;
        CdDdId = otherAlbum.CdDdId;
        MusicBrainzDiscId = otherAlbum.MusicBrainzDiscId;
        MusicBrainzGroupId = otherAlbum.MusicBrainzGroupId;
        MusicBrainzId = otherAlbum.MusicBrainzId;
        AmazonId = otherAlbum.AmazonId;
        return true;
      }
      else if (otherInstance is TrackInfo)
      {
        TrackInfo albumTrack = otherInstance as TrackInfo;
        AudioDbId = albumTrack.AlbumAudioDbId;
        CdDdId = albumTrack.AlbumCdDdId;
        MusicBrainzDiscId = albumTrack.AlbumMusicBrainzDiscId;
        MusicBrainzGroupId = albumTrack.AlbumMusicBrainzGroupId;
        MusicBrainzId = albumTrack.AlbumMusicBrainzId;
        AmazonId = albumTrack.AlbumAmazonId;
        return true;
      }
      return false;
    }

    #endregion

    #region Overrides

    public override string ToString()
    {
      return Album ?? "?";
    }

    public override bool Equals(object obj)
    {
      AlbumInfo other = obj as AlbumInfo;
      if (other == null) return false;

      if (AudioDbId > 0 && other.AudioDbId > 0)
        return AudioDbId == other.AudioDbId;
      if (!string.IsNullOrEmpty(MusicBrainzId) && !string.IsNullOrEmpty(other.MusicBrainzId))
        return string.Equals(MusicBrainzId, other.MusicBrainzId, StringComparison.InvariantCultureIgnoreCase);
      if (!string.IsNullOrEmpty(MusicBrainzGroupId) && !string.IsNullOrEmpty(other.MusicBrainzGroupId))
        return string.Equals(MusicBrainzGroupId, other.MusicBrainzGroupId, StringComparison.InvariantCultureIgnoreCase);
      if (!string.IsNullOrEmpty(CdDdId) && !string.IsNullOrEmpty(other.CdDdId))
        return string.Equals(CdDdId, other.CdDdId, StringComparison.InvariantCultureIgnoreCase);
      if (!string.IsNullOrEmpty(UpcEanId) && !string.IsNullOrEmpty(other.UpcEanId))
        return string.Equals(UpcEanId, other.UpcEanId, StringComparison.InvariantCultureIgnoreCase);
      if (!string.IsNullOrEmpty(AmazonId) && !string.IsNullOrEmpty(other.AmazonId))
        return string.Equals(AmazonId, other.AmazonId, StringComparison.InvariantCultureIgnoreCase);
      if (!string.IsNullOrEmpty(Album) && !string.IsNullOrEmpty(other.Album) && MatchNames(Album, other.Album) && 
        ReleaseDate.HasValue && other.ReleaseDate.HasValue && ReleaseDate.Value == other.ReleaseDate.Value)
        return true;
      if (!string.IsNullOrEmpty(Album) && !string.IsNullOrEmpty(other.Album))
        return MatchNames(Album, other.Album);

      return false;
    }

    public override int GetHashCode()
    {
      //TODO: Check if this is functional
      return (string.IsNullOrEmpty(Album) ? "Unnamed Album" : Album).GetHashCode();
    }

    #endregion
  }
}
