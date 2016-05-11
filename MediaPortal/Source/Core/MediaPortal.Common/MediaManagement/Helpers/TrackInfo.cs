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

namespace MediaPortal.Common.MediaManagement.Helpers
{
  /// <summary>
  /// <see cref="TrackInfo"/> contains information about a track. It's used as an interface structure for external 
  /// online data scrapers to fill in metadata.
  /// </summary>
  public class TrackInfo : BaseInfo
  {
    public string MusicBrainzId = null;
    public long AudioDbId = 0;

    public string Album = null;
    public string AlbumMusicBrainzId = null;
    public string AlbumMusicBrainzGroupId = null;
    public string AlbumMusicBrainzDiscId = null;
    public string AlbumCdDdId = null;
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

    public List<PersonInfo> Artists = new List<PersonInfo>();
    public List<PersonInfo> AlbumArtists = new List<PersonInfo>();
    public List<PersonInfo> Composers = new List<PersonInfo>();
    public List<CompanyInfo> MusicLabels = new List<CompanyInfo>();
    public List<string> Genres = new List<string>();

    #region Members

    /// <summary>
    /// Copies the contained track information into MediaItemAspect.
    /// </summary>
    /// <param name="aspectData">Dictionary with extracted aspects.</param>
    public bool SetMetadata(IDictionary<Guid, IList<MediaItemAspect>> aspectData)
    {
      if (string.IsNullOrEmpty(TrackName)) return false;

      MediaItemAspect.SetAttribute(aspectData, MediaAspect.ATTR_TITLE, TrackName);
      if (!string.IsNullOrEmpty(TrackLyrics)) MediaItemAspect.SetAttribute(aspectData, AudioAspect.ATTR_LYRICS, TrackLyrics);
      if (ReleaseDate.HasValue) MediaItemAspect.SetAttribute(aspectData, MediaAspect.ATTR_RECORDINGTIME, ReleaseDate.Value);
      if (TrackNum > 0) MediaItemAspect.SetAttribute(aspectData, AudioAspect.ATTR_TRACK, TrackNum);
      if (TotalTracks > 0) MediaItemAspect.SetAttribute(aspectData, AudioAspect.ATTR_NUMTRACKS, TotalTracks);

      if (!string.IsNullOrEmpty(MusicBrainzId)) MediaItemAspect.AddOrUpdateExternalIdentifier(aspectData, ExternalIdentifierAspect.SOURCE_MUSICBRAINZ, ExternalIdentifierAspect.TYPE_TRACK, MusicBrainzId);
      if (AudioDbId > 0) MediaItemAspect.AddOrUpdateExternalIdentifier(aspectData, ExternalIdentifierAspect.SOURCE_AUDIODB, ExternalIdentifierAspect.TYPE_TRACK, AudioDbId.ToString());

      if (!string.IsNullOrEmpty(Album)) MediaItemAspect.SetAttribute(aspectData, AudioAspect.ATTR_ALBUM, Album);
      if (DiscNum > 0) MediaItemAspect.SetAttribute(aspectData, AudioAspect.ATTR_DISCID, DiscNum);
      if (TotalDiscs > 0) MediaItemAspect.SetAttribute(aspectData, AudioAspect.ATTR_NUMDISCS, TotalDiscs);

      if (TotalRating > 0d) MediaItemAspect.SetAttribute(aspectData, AudioAspect.ATTR_TOTAL_RATING, TotalRating);
      if (RatingCount > 0) MediaItemAspect.SetAttribute(aspectData, AudioAspect.ATTR_RATING_COUNT, RatingCount);

      if (!string.IsNullOrEmpty(AlbumCdDdId)) MediaItemAspect.AddOrUpdateExternalIdentifier(aspectData, ExternalIdentifierAspect.SOURCE_CDDB, ExternalIdentifierAspect.TYPE_ALBUM, AlbumCdDdId);
      if (!string.IsNullOrEmpty(AlbumMusicBrainzId)) MediaItemAspect.AddOrUpdateExternalIdentifier(aspectData, ExternalIdentifierAspect.SOURCE_MUSICBRAINZ, ExternalIdentifierAspect.TYPE_ALBUM, AlbumMusicBrainzId);
      if (AlbumAudioDbId > 0) MediaItemAspect.AddOrUpdateExternalIdentifier(aspectData, ExternalIdentifierAspect.SOURCE_AUDIODB, ExternalIdentifierAspect.TYPE_ALBUM, AlbumAudioDbId.ToString());

      if (Artists.Count > 0) MediaItemAspect.SetCollectionAttribute(aspectData, AudioAspect.ATTR_ARTISTS, Artists.Select(p => p.Name).ToList());
      if (AlbumArtists.Count > 0) MediaItemAspect.SetCollectionAttribute(aspectData, AudioAspect.ATTR_ALBUMARTISTS, AlbumArtists.Select(p => p.Name).ToList());
      if (Composers.Count > 0) MediaItemAspect.SetCollectionAttribute(aspectData, AudioAspect.ATTR_COMPOSERS, Composers.Select(p => p.Name).ToList());

      if (Genres.Count > 0) MediaItemAspect.SetCollectionAttribute(aspectData, AudioAspect.ATTR_GENRES, Genres);

      SetThumbnailMetadata(aspectData);

      return true;
    }

    public bool FromMetadata(IDictionary<Guid, IList<MediaItemAspect>> aspectData)
    {
      if (!aspectData.ContainsKey(AudioAspect.ASPECT_ID))
        return false;

      MediaItemAspect.TryGetAttribute(aspectData, MediaAspect.ATTR_TITLE, out TrackName);
      MediaItemAspect.TryGetAttribute(aspectData, AudioAspect.ATTR_LYRICS, out TrackLyrics);
      MediaItemAspect.TryGetAttribute(aspectData, MediaAspect.ATTR_RECORDINGTIME, out ReleaseDate);
      MediaItemAspect.TryGetAttribute(aspectData, AudioAspect.ATTR_TRACK, out TrackNum);
      MediaItemAspect.TryGetAttribute(aspectData, AudioAspect.ATTR_NUMTRACKS, out TotalTracks);

      MediaItemAspect.TryGetAttribute(aspectData, AudioAspect.ATTR_ALBUM, out Album);
      MediaItemAspect.TryGetAttribute(aspectData, AudioAspect.ATTR_DISCID, out DiscNum);
      MediaItemAspect.TryGetAttribute(aspectData, AudioAspect.ATTR_NUMDISCS, out TotalDiscs);

      MediaItemAspect.TryGetAttribute(aspectData, AudioAspect.ATTR_TOTAL_RATING, out TotalRating);
      MediaItemAspect.TryGetAttribute(aspectData, AudioAspect.ATTR_RATING_COUNT, out RatingCount);

      string id;
      if (MediaItemAspect.TryGetExternalAttribute(aspectData, ExternalIdentifierAspect.SOURCE_AUDIODB, ExternalIdentifierAspect.TYPE_TRACK, out id))
        AudioDbId = Convert.ToInt32(id);
      MediaItemAspect.TryGetExternalAttribute(aspectData, ExternalIdentifierAspect.SOURCE_MUSICBRAINZ, ExternalIdentifierAspect.TYPE_TRACK, out MusicBrainzId);

      if (MediaItemAspect.TryGetExternalAttribute(aspectData, ExternalIdentifierAspect.SOURCE_AUDIODB, ExternalIdentifierAspect.TYPE_ALBUM, out id))
        AlbumAudioDbId = Convert.ToInt32(id);
      MediaItemAspect.TryGetExternalAttribute(aspectData, ExternalIdentifierAspect.SOURCE_MUSICBRAINZ, ExternalIdentifierAspect.TYPE_ALBUM, out AlbumMusicBrainzId);
      MediaItemAspect.TryGetExternalAttribute(aspectData, ExternalIdentifierAspect.SOURCE_CDDB, ExternalIdentifierAspect.TYPE_ALBUM, out AlbumCdDdId);

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

    #endregion

    #region Overrides

    public override string ToString()
    {
      return TrackName;
    }

    #endregion
  }
}
