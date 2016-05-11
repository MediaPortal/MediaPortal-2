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
  /// <see cref="AlbumInfo"/> contains information about an album. It's used as an interface structure for external 
  /// online data scrapers to fill in metadata.
  /// </summary>
  public class AlbumInfo : BaseInfo
  {
    public string MusicBrainzId = null;
    public string MusicBrainzGroupId = null;
    public long AudioDbId = 0;
    public string CdDdId = null;

    public string Album = null;
    public string Description = null;
    public DateTime? ReleaseDate = null;
    public int TotalTracks = 0;
    public int DiscNum = 0;
    public int TotalDiscs = 0;
    public double TotalRating = 0;
    public int RatingCount = 0;
    public long Sales = 0;

    public List<PersonInfo> Artists = new List<PersonInfo>();
    public List<CompanyInfo> MusicLabels = new List<CompanyInfo>();
    public List<string> Genres = new List<string>();
    public List<string> Awards = new List<string>();

    #region Members

    /// <summary>
    /// Copies the contained track information into MediaItemAspect.
    /// </summary>
    /// <param name="aspectData">Dictionary with extracted aspects.</param>
    public bool SetMetadata(IDictionary<Guid, IList<MediaItemAspect>> aspectData)
    {
      if (string.IsNullOrEmpty(Album)) return false;

      MediaItemAspect.SetAttribute(aspectData, MediaAspect.ATTR_TITLE, Album);
      MediaItemAspect.SetAttribute(aspectData, AudioAlbumAspect.ATTR_ALBUM, Album);
      if (DiscNum > 0) MediaItemAspect.SetAttribute(aspectData, AudioAlbumAspect.ATTR_DISCID, DiscNum);
      if (TotalDiscs > 0) MediaItemAspect.SetAttribute(aspectData, AudioAlbumAspect.ATTR_NUMDISCS, TotalDiscs);
      if (ReleaseDate.HasValue) MediaItemAspect.SetAttribute(aspectData, MediaAspect.ATTR_RECORDINGTIME, ReleaseDate.Value);
      if (TotalTracks > 0) MediaItemAspect.SetAttribute(aspectData, AudioAlbumAspect.ATTR_NUMTRACKS, TotalTracks);

      if (!string.IsNullOrEmpty(MusicBrainzId)) MediaItemAspect.AddOrUpdateExternalIdentifier(aspectData, ExternalIdentifierAspect.SOURCE_MUSICBRAINZ, ExternalIdentifierAspect.TYPE_ALBUM, MusicBrainzId);
      if (AudioDbId > 0) MediaItemAspect.AddOrUpdateExternalIdentifier(aspectData, ExternalIdentifierAspect.SOURCE_AUDIODB, ExternalIdentifierAspect.TYPE_ALBUM, AudioDbId.ToString());
      if (!string.IsNullOrEmpty(CdDdId)) MediaItemAspect.AddOrUpdateExternalIdentifier(aspectData, ExternalIdentifierAspect.SOURCE_CDDB, ExternalIdentifierAspect.TYPE_ALBUM, CdDdId);

      if (TotalRating > 0d) MediaItemAspect.SetAttribute(aspectData, AudioAlbumAspect.ATTR_TOTAL_RATING, TotalRating);
      if (RatingCount > 0) MediaItemAspect.SetAttribute(aspectData, AudioAlbumAspect.ATTR_RATING_COUNT, RatingCount);

      if (Artists.Count > 0) MediaItemAspect.SetCollectionAttribute(aspectData, AudioAlbumAspect.ATTR_ARTISTS, Artists.Select(p => p.Name).ToList());

      if (Genres.Count > 0) MediaItemAspect.SetCollectionAttribute(aspectData, AudioAlbumAspect.ATTR_GENRES, Genres);
      if (Awards.Count > 0) MediaItemAspect.SetCollectionAttribute(aspectData, AudioAlbumAspect.ATTR_AWARDS, Awards);

      if (MusicLabels.Count > 0) MediaItemAspect.SetCollectionAttribute(aspectData, AudioAlbumAspect.ATTR_LABELS, MusicLabels.Select(l => l.Name).ToList());

      SetThumbnailMetadata(aspectData);

      return true;
    }

    public bool FromMetadata(IDictionary<Guid, IList<MediaItemAspect>> aspectData)
    {
      if (aspectData.ContainsKey(AudioAlbumAspect.ASPECT_ID))
      {
        MediaItemAspect.TryGetAttribute(aspectData, AudioAlbumAspect.ATTR_ALBUM, out Album);
        MediaItemAspect.TryGetAttribute(aspectData, AudioAlbumAspect.ATTR_DISCID, out DiscNum);
        MediaItemAspect.TryGetAttribute(aspectData, AudioAlbumAspect.ATTR_NUMDISCS, out TotalDiscs);
        MediaItemAspect.TryGetAttribute(aspectData, AudioAlbumAspect.ATTR_NUMTRACKS, out TotalTracks);
        MediaItemAspect.TryGetAttribute(aspectData, MediaAspect.ATTR_RECORDINGTIME, out ReleaseDate);

        string id;
        if (MediaItemAspect.TryGetExternalAttribute(aspectData, ExternalIdentifierAspect.SOURCE_AUDIODB, ExternalIdentifierAspect.TYPE_ALBUM, out id))
          AudioDbId = Convert.ToInt32(id);
        MediaItemAspect.TryGetExternalAttribute(aspectData, ExternalIdentifierAspect.SOURCE_MUSICBRAINZ, ExternalIdentifierAspect.TYPE_ALBUM, out MusicBrainzId);
        MediaItemAspect.TryGetExternalAttribute(aspectData, ExternalIdentifierAspect.SOURCE_CDDB, ExternalIdentifierAspect.TYPE_ALBUM, out CdDdId);

        MediaItemAspect.TryGetAttribute(aspectData, AudioAlbumAspect.ATTR_TOTAL_RATING, out TotalRating);
        MediaItemAspect.TryGetAttribute(aspectData, AudioAlbumAspect.ATTR_RATING_COUNT, out RatingCount);

        ICollection<object> collection;
        Artists.Clear();
        if (MediaItemAspect.TryGetAttribute(aspectData, AudioAlbumAspect.ATTR_ARTISTS, out collection))
          Artists.AddRange(collection.Select(s => new PersonInfo() { Name = s.ToString(), Occupation = PersonAspect.OCCUPATION_ARTIST }));

        Genres.Clear();
        if (MediaItemAspect.TryGetAttribute(aspectData, AudioAlbumAspect.ATTR_GENRES, out collection))
          Genres.AddRange(collection.Select(s => s.ToString()));

        Awards.Clear();
        if (MediaItemAspect.TryGetAttribute(aspectData, AudioAlbumAspect.ATTR_AWARDS, out collection))
          Awards.AddRange(collection.Select(s => s.ToString()));

        MusicLabels.Clear();
        if (MediaItemAspect.TryGetAttribute(aspectData, AudioAlbumAspect.ATTR_LABELS, out collection))
          MusicLabels.AddRange(collection.Select(s => new CompanyInfo() { Name = s.ToString(), Type = CompanyAspect.COMPANY_MUSIC_LABEL }));

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

        string id;
        if (MediaItemAspect.TryGetExternalAttribute(aspectData, ExternalIdentifierAspect.SOURCE_AUDIODB, ExternalIdentifierAspect.TYPE_ALBUM, out id))
          AudioDbId = Convert.ToInt32(id);
        MediaItemAspect.TryGetExternalAttribute(aspectData, ExternalIdentifierAspect.SOURCE_MUSICBRAINZ, ExternalIdentifierAspect.TYPE_ALBUM, out MusicBrainzId);
        MediaItemAspect.TryGetExternalAttribute(aspectData, ExternalIdentifierAspect.SOURCE_CDDB, ExternalIdentifierAspect.TYPE_ALBUM, out CdDdId);

        ICollection<object> collection;
        Artists.Clear();
        if (MediaItemAspect.TryGetAttribute(aspectData, AudioAspect.ATTR_ALBUMARTISTS, out collection))
          Artists.AddRange(collection.Select(s => new PersonInfo() { Name = s.ToString(), Occupation = PersonAspect.OCCUPATION_ARTIST }));

        return true;
      }
      else if (aspectData.ContainsKey(MediaAspect.ASPECT_ID))
      {
        MediaItemAspect.TryGetAttribute(aspectData, MediaAspect.ATTR_TITLE, out Album);

        string id;
        if (MediaItemAspect.TryGetExternalAttribute(aspectData, ExternalIdentifierAspect.SOURCE_AUDIODB, ExternalIdentifierAspect.TYPE_ALBUM, out id))
          AudioDbId = Convert.ToInt32(id);
        MediaItemAspect.TryGetExternalAttribute(aspectData, ExternalIdentifierAspect.SOURCE_MUSICBRAINZ, ExternalIdentifierAspect.TYPE_ALBUM, out MusicBrainzId);
        MediaItemAspect.TryGetExternalAttribute(aspectData, ExternalIdentifierAspect.SOURCE_CDDB, ExternalIdentifierAspect.TYPE_ALBUM, out CdDdId);

        byte[] data;
        if (MediaItemAspect.TryGetAttribute(aspectData, ThumbnailLargeAspect.ATTR_THUMBNAIL, out data))
          Thumbnail = data;
      }
      return false;
    }

    #endregion

    #region Overrides

    public override string ToString()
    {
      return Album;
    }

    #endregion
  }
}
