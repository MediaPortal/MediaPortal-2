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
using System.Runtime.Serialization;

namespace MediaPortal.Extensions.OnlineLibraries.Libraries.MusicBrainzV2.Data
{
  [DataContract]
  public class Track
  {
    [DataMember(Name = "id")]
    public string Id { get; set; }

    [DataMember(Name = "title")]
    public string Title { get; set; }

    [DataMember(Name = "length")]
    public long Length { get; set; }

    [DataMember(Name = "artist-credit")]
    public IList<TrackArtistCredit> Artists { get; set; }

    [DataMember(Name = "rating")]
    public TrackRating Rating { get; set; }

    [DataMember(Name = "releases")]
    public IList<TrackRelease> Releases { get; set; }

    [DataMember(Name = "tags")]
    public IList<TrackTag> Tags { get; set; }

    [DataMember(Name = "relations")]
    public IList<TrackRelation> Relations { get; set; }

    public List<string> TrackArtists { get; set; }

    public List<string> Composers { get; set; }

    public string AlbumId { get; set; }

    public string Album { get; set; }

    public List<string> Genre { get; set; }

    public int TrackNum { get; set; }

    public int TotalTracks { get; set; }

    public int DiscId { get; set; }

    public List<string> AlbumArtists { get; set; }

    public DateTime? ReleaseDate { get; set; }

    public double RatingValue { get; set; }

    public int RatingVotes { get; set; }

    public void InitProperties(string albumId)
    {
      foreach (TrackRelease release in Releases)
      {
        if (!release.Status.Equals("Official", StringComparison.InvariantCultureIgnoreCase)) //Only official releases
          continue;

        if (release.ReleaseGroup != null && !release.ReleaseGroup.PrimaryType.Equals("Album", StringComparison.InvariantCultureIgnoreCase)) //Only album releases
          continue;

        if (Artists == null)
          continue;

        if (albumId != null && !release.Id.Equals(albumId, StringComparison.InvariantCultureIgnoreCase))
          continue;

        foreach (TrackMedia media in release.Media)
        {
          if (media.Tracks == null || media.Tracks.Count <= 0)
            continue;

          AlbumId = release.Id;
          Album = release.Title;

          TrackArtists = new List<string>();
          foreach (TrackArtistCredit artistCredit in Artists)
          {
            TrackArtists.Add(artistCredit.Artist.Name);
          }

          AlbumArtists = new List<string>();
          foreach (TrackArtistCredit artistCredit in release.Artists)
          {
            AlbumArtists.Add(artistCredit.Artist.Name);
          }
          if (AlbumArtists.Count == 0) AlbumArtists = TrackArtists;

          Composers = new List<string>();
          foreach (TrackRelation relation in Relations)
          {
            if(relation.Type.Equals("Composer", StringComparison.InvariantCultureIgnoreCase))
              Composers.Add(relation.Artist.Name);
          }

          Genre = new List<string>();
          foreach (TrackTag tag in Tags)
          {
            if(tag.Count > 1) Genre.Add(tag.Name); //Only use tags with multiple taggings
          }

          RatingValue = Rating.Value.HasValue ? Rating.Value.Value : 0;
          RatingVotes = Rating.VoteCount;

          DateTime releaseDate;
          if (DateTime.TryParse(release.Date, out releaseDate))
            ReleaseDate = releaseDate;
          else if (DateTime.TryParse(release.Date + "-01", out releaseDate))
            ReleaseDate = releaseDate;
          else if (DateTime.TryParse(release.Date + "-01-01", out releaseDate))
            ReleaseDate = releaseDate;

          int trackNum;
          if (int.TryParse(media.Tracks[0].Number, out trackNum))
            TrackNum = trackNum;
          TotalTracks = media.TrackCount;
          DiscId = media.Position;

          return;
        }
      }
    }
  }
}
