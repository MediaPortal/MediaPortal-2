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
  public class TrackSearchResult
  {
    [DataMember(Name = "id")]
    public string Id { get; set; }

    [DataMember(Name = "title")]
    public string Title { get; set; }

    [DataMember(Name = "artist-credit")]
    public IList<TrackArtistCredit> Artists { get; set; }

    [DataMember(Name = "releases")]
    public IList<TrackRelease> Releases { get; set; }

    [DataMember(Name = "tags")]
    public IList<TrackTag> Tags { get; set; }

    public override string ToString()
    {
      return string.Format("Id: {0}, Title: {1}", Id, Title);
    }

    public List<TrackResult> GetTracks()
    {
      List<TrackResult> tracks = new List<TrackResult>();
      foreach(TrackRelease release in Releases)
      {
        if (!release.Status.Equals("Official", StringComparison.InvariantCultureIgnoreCase)) //Only official releases
          continue;

        if (release.ReleaseGroup != null && !release.ReleaseGroup.PrimaryType.Equals("Album", StringComparison.InvariantCultureIgnoreCase)) //Only album releases
          continue;

        if (Artists == null)
          continue;

        foreach (TrackMedia media in release.Media)
        {
          if (media.Track == null || media.Track.Count <= 0)
            continue;

          TrackResult track = new TrackResult();
          track.AlbumId = release.Id;
          track.Album = release.Title;
          track.AlbumBarcode = release.Barcode;
          track.DiscCount = media.Discs != null ? media.Discs.Count : 0;
          track.Artists = new List<string>();
          foreach (TrackArtistCredit artistCredit in Artists)
          {
            track.Artists.Add(artistCredit.Artist.Name);
          }
          track.Country = release.Country;
          //track.Id = media.Tracks[0].Id;
          track.Id = Id;
          DateTime releaseDate;
          if (DateTime.TryParse(release.Date, out releaseDate))
            track.ReleaseDate = releaseDate;
          else if (DateTime.TryParse(release.Date + "-01", out releaseDate))
            track.ReleaseDate = releaseDate;
          else if (DateTime.TryParse(release.Date + "-01-01", out releaseDate))
            track.ReleaseDate = releaseDate;

          track.Title = media.Track[0].Title;
          int trackNum;
          if (int.TryParse(media.Track[0].Number, out trackNum))
            track.TrackNum = trackNum;
          if (release.ReleaseGroup != null && release.ReleaseGroup.SecondaryTypes != null)
            track.FromCompilation = release.ReleaseGroup.SecondaryTypes.Contains("Compilation");

          tracks.Add(track);
        }
      }

      return tracks;
    }
  }

  [DataContract]
  public class RecordingResult
  {
    [DataMember(Name = "recordings")]
    public IList<TrackSearchResult> Results { get; set; }
  }

  public class TrackResult
  {
    public string Id { get; set; }

    public string Title { get; set; }

    public List<string> Artists { get; set; }

    public string AlbumId { get; set; }

    public string Album { get; set; }

    public string AlbumBarcode { get; set; }

    public int DiscCount { get; set; }

    public DateTime? ReleaseDate { get; set; }

    public int TrackNum { get; set; }

    public bool FromCompilation { get; set; }

    public string Country { get; set; }
  }
}
