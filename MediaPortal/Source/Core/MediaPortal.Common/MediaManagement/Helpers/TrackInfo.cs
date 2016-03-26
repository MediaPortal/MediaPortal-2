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
using System.Globalization;
using System.Linq;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;

namespace MediaPortal.Common.MediaManagement.Helpers
{
  /// <summary>
  /// <see cref="TrackInfo"/> contains information about a track. It's used as an interface structure for external 
  /// online data scrapers to fill in metadata.
  /// </summary>
  public class TrackInfo
  {
    public bool Matched { get; set; }

    public string MusicBrainzId { get; set; }
    public string CdDdId { get; set; }
    public string AudioDbId { get; set; }

    public string Title { get; set; }
    public string Album { get; set; }
    public int Year { get; set; }
    public int TrackNum { get; set; }
    public int TotalTracks { get; set; }
    public int DiscNum { get; set; }
    public int TotalDiscs { get; set; }
    public double TotalRating { get; set; }
    public int RatingCount { get; set; }

    public List<string> Artists { get; internal set; }
    public List<string> AlbumArtists { get; internal set; }
    public List<string> Composers { get; internal set; }
    public List<string> Genres { get; internal set; }

    public TrackInfo()
    {
      Artists = new List<string>();
      AlbumArtists = new List<string>();
      Composers = new List<string>();
      Genres = new List<string>();
    }

    /// <summary>
    /// Copies the contained track information into MediaItemAspect.
    /// </summary>
    /// <param name="aspectData">Dictionary with extracted aspects.</param>
    public bool SetMetadata(IDictionary<Guid, IList<MediaItemAspect>> aspectData)
    {
      MediaItemAspect.SetAttribute(aspectData, MediaAspect.ATTR_TITLE, Title);
      if (!string.IsNullOrEmpty(MusicBrainzId)) MediaItemAspect.AddOrUpdateExternalIdentifier(aspectData, ExternalIdentifierAspect.SOURCE_MUSICBRAINZ, ExternalIdentifierAspect.TYPE_TRACK, MusicBrainzId);
      if (!string.IsNullOrEmpty(CdDdId)) MediaItemAspect.AddOrUpdateExternalIdentifier(aspectData, ExternalIdentifierAspect.SOURCE_CDDB, ExternalIdentifierAspect.TYPE_TRACK, CdDdId);
      if (!string.IsNullOrEmpty(AudioDbId)) MediaItemAspect.AddOrUpdateExternalIdentifier(aspectData, ExternalIdentifierAspect.SOURCE_AUDIODB, ExternalIdentifierAspect.TYPE_TRACK, AudioDbId);

      if (!string.IsNullOrEmpty(Album)) MediaItemAspect.SetAttribute(aspectData, AudioAspect.ATTR_ALBUM, Album);
      if (DiscNum > 0) MediaItemAspect.SetAttribute(aspectData, AudioAspect.ATTR_DISCID, DiscNum);
      if (TotalDiscs > 0) MediaItemAspect.SetAttribute(aspectData, AudioAspect.ATTR_NUMDISCS, TotalDiscs);
      if (TrackNum > 0) MediaItemAspect.SetAttribute(aspectData, AudioAspect.ATTR_TRACK, TrackNum);
      if (TotalTracks > 0) MediaItemAspect.SetAttribute(aspectData, AudioAspect.ATTR_NUMTRACKS, TotalTracks);

      if (Artists.Count > 0) MediaItemAspect.SetCollectionAttribute(aspectData, AudioAspect.ATTR_ARTISTS, Artists);
      if (AlbumArtists.Count > 0) MediaItemAspect.SetCollectionAttribute(aspectData, AudioAspect.ATTR_ALBUMARTISTS, AlbumArtists);
      if (Composers.Count > 0) MediaItemAspect.SetCollectionAttribute(aspectData, AudioAspect.ATTR_COMPOSERS, Composers);
      if (Genres.Count > 0) MediaItemAspect.SetCollectionAttribute(aspectData, AudioAspect.ATTR_GENRES, Genres);

      if (Year > 0) MediaItemAspect.SetAttribute(aspectData, MediaAspect.ATTR_RECORDINGTIME, new DateTime(Year, 1, 1));

      return true;
    }
  }
}
