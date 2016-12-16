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
using System.Runtime.Serialization;

namespace MediaPortal.Extensions.OnlineLibraries.Libraries.MusicBrainzV2.Data
{
  //{
  //  "tags": [
  //  ],
  //  "isrcs": [
  //    "JPB600760301"
  //  ],
  //  "artist-credit": [
  //    {
  //      "joinphrase": "",
  //      "artist": {
  //        "disambiguation": "",
  //        "sort-name": "Depeche Mode",
  //        "id": "8538e728-ca0b-4321-b7e5-cff6565dd4c0",
  //        "name": "Depeche Mode"
  //      },
  //      "name": "Depeche Mode"
  //    }
  //  ],
  //  "relations": [
  //  ],
  //  "video": false,
  //  "id": "282acfcf-6a05-47b6-b344-8aa05423a65d",
  //  "length": 223000,
  //  "releases": [
  //    {
  //      "media": [
  //        {
  //          "title": "",
  //          "position": 1,
  //          "discs": [
  //          ],
  //          "track-count": 2,
  //          "tracks": [
  //            {
  //              "title": "New Life",
  //              "artist-credit": [
  //                {
  //                  "artist": {
  //                    "disambiguation": "",
  //                    "name": "Depeche Mode",
  //                    "id": "8538e728-ca0b-4321-b7e5-cff6565dd4c0",
  //                    "sort-name": "Depeche Mode"
  //                  },
  //                  "name": "Depeche Mode",
  //                  "joinphrase": ""
  //                }
  //              ],
  //              "id": "40a141c2-08f2-36b9-9ccd-d75f47c847b1",
  //              "number": "A",
  //              "length": 223000
  //            }
  //          ],
  //          "format": "7\" Vinyl",
  //          "track-offset": 0
  //        }
  //      ],
  //      "status": "Official",
  //      "artist-credit": [
  //        {
  //          "name": "Depeche Mode",
  //          "artist": {
  //            "sort-name": "Depeche Mode",
  //            "id": "8538e728-ca0b-4321-b7e5-cff6565dd4c0",
  //            "name": "Depeche Mode",
  //            "disambiguation": ""
  //          },
  //          "joinphrase": ""
  //        }
  //      ],
  //      "text-representation": {
  //        "script": "Latn",
  //        "language": "eng"
  //      },
  //      "id": "76a2c55d-37a7-4258-97d1-8d3d7da094fc",
  //      "packaging": "Cardboard/Paper Sleeve",
  //      "title": "New Life",
  //      "barcode": "",
  //      "release-events": [
  //        {
  //          "date": "1981-06-13",
  //          "area": {
  //            "id": "8a754a16-0027-3a29-b6d7-2b40ea0481ed",
  //            "name": "United Kingdom",
  //            "sort-name": "United Kingdom",
  //            "iso-3166-1-codes": [
  //              "GB"
  //            ],
  //            "disambiguation": ""
  //          }
  //        }
  //      ],
  //      "date": "1981-06-13",
  //      "quality": "normal",
  //      "country": "GB",
  //      "disambiguation": ""
  //    }
  //  ],
  //  "title": "New Life",
  //  "rating": {
  //    "value": null,
  //    "votes-count": 0
  //  },
  //  "disambiguation": ""
  //}
  [DataContract]
  public class Track
  {
    [DataMember(Name = "id")]
    public string Id { get; set; }

    [DataMember(Name = "title")]
    public string Title { get; set; }

    [DataMember(Name = "length")]
    public long? Length { get; set; }

    [DataMember(Name = "artist-credit")]
    public List<TrackArtistCredit> Artists { get; set; }

    [DataMember(Name = "rating")]
    public TrackRating Rating { get; set; }

    [DataMember(Name = "releases")]
    public List<TrackRelease> Releases { get; set; }

    [DataMember(Name = "tags")]
    public List<TrackTag> Tags { get; set; }

    [DataMember(Name = "isrcs")]
    public List<string> Isrcs { get; set; }

    [DataMember(Name = "relations")]
    public List<TrackRelation> Relations { get; set; }

    public List<TrackBaseName> TrackArtists { get; set; }

    public List<TrackBaseName> Composers { get; set; }

    public string AlbumId { get; set; }

    public string AlbumAmazonId { get; set; }

    public string Album { get; set; }

    public List<string> TagValues { get; set; }

    public int TrackNum { get; set; }

    public int TotalTracks { get; set; }

    public int DiscId { get; set; }

    public List<TrackBaseName> AlbumArtists { get; set; }

    public DateTime? ReleaseDate { get; set; }

    public double RatingValue { get; set; }

    public int RatingVotes { get; set; }

    public bool InitPropertiesFromAlbum(string albumId, string albumName, string country)
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

        if (albumName != null && !release.Title.Equals(albumName, StringComparison.InvariantCultureIgnoreCase))
          continue;

        if (country != null && !release.Country.Equals(country, StringComparison.InvariantCultureIgnoreCase))
          continue;

        foreach (TrackMedia media in release.Media)
        {
          if (media.Tracks == null || media.Tracks.Count <= 0)
            continue;

          AlbumId = release.Id;
          AlbumAmazonId = release.AmazonId;
          Album = release.Title;

          TrackArtists = new List<TrackBaseName>();
          foreach (TrackArtistCredit artistCredit in Artists)
          {
            TrackArtists.Add(artistCredit.Artist);
          }

          AlbumArtists = new List<TrackBaseName>();
          foreach (TrackArtistCredit artistCredit in release.Artists)
          {
            AlbumArtists.Add(artistCredit.Artist);
          }
          if (AlbumArtists.Count == 0) AlbumArtists = TrackArtists;

          Composers = new List<TrackBaseName>();
          foreach (TrackRelation relation in Relations)
          {
            if (relation.Type.Equals("Composer", StringComparison.InvariantCultureIgnoreCase))
              Composers.Add(relation.Artist);
          }

          TagValues = new List<string>();
          foreach (TrackTag tag in Tags)
          {
            if (tag.Count > 1) TagValues.Add(tag.Name); //Only use tags with multiple taggings
          }

          RatingValue = Rating.Value.HasValue ? Rating.Value.Value : 0;
          RatingVotes = Rating.VoteCount;

          //DateTime releaseDate;
          //if (DateTime.TryParse(release.Date, out releaseDate))
          //  ReleaseDate = releaseDate;
          //else if (DateTime.TryParse(release.Date + "-01", out releaseDate))
          //  ReleaseDate = releaseDate;
          //else if (DateTime.TryParse(release.Date + "-01-01", out releaseDate))
          //  ReleaseDate = releaseDate;

          int trackNum;
          if (int.TryParse(media.Tracks[0].Number, out trackNum))
            TrackNum = trackNum;
          TotalTracks = media.TrackCount;
          DiscId = media.Position;

          return true;
        }
      }
      return false;
    }
  }
}
