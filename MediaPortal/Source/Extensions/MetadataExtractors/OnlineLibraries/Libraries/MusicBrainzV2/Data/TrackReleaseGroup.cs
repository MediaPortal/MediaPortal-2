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
  //  {
  //    "id": "c9fdb94c-4975-4ed6-a96f-ef6d80bb7738",
  //    "first-release-date": "2012-05-22",
  //    "title": "The Lost Tape",
  //    "artist-credit": [
  //        "name": "50 Cent",
  //        "joinphrase": "",
  //        "artist": {
  //            "id": "8e68819d-71be-4e7d-b41d-f1df81b01d3f",
  //            "name": "50 Cent",
  //            "sort-name": "50 Cent",
  //            "disambiguation": null
  //        }
  //    ],
  //    "primary-type": "Album",
  //    "disambiguation": null,
  //    "secondary-types": [ "Mixtape/Street" ],
  //    "releases": [
  //        {
  //            "id": "2ec84eb6-ab92-4ac3-9720-32ad84c34f11",
  //            "title": "The Lost Tape",
  //            /* some properties omitted to keep this example shorter. */
  //        }
  //    ]
  //}
  [DataContract]
  public class TrackReleaseGroup
  {
    [DataMember(Name = "id")]
    public string Id { get; set; }

    [DataMember(Name = "title")]
    public string Title { get; set; }

    [DataMember(Name = "first-release-date")]
    public string _firstRelease
    {
      set
      {
        if (value != null)
        {
          DateTime date;
          if (DateTime.TryParse(value, out date))
            FirstRelease = date;
          if (DateTime.TryParse(value + "-01", out date))
            FirstRelease = date;
          if (DateTime.TryParse(value + "-01-01", out date))
            FirstRelease = date;
        }
      }
    }

    public DateTime? FirstRelease { get; set; }

    [DataMember(Name = "artist-credit")]
    public List<TrackArtistCredit> ArtistCredit { get; set; }

    [DataMember(Name = "primary-type")]
    public string PrimaryType { get; set; }

    [DataMember(Name = "secondary-types")]
    public List<string> SecondaryTypes { get; set; }

    [DataMember(Name = "releases")]
    public List<TrackRelease> Releases { get; set; }

    [DataMember(Name = "tags")]
    public List<TrackTag> Tags { get; set; }

    [DataMember(Name = "relations")]
    public List<TrackRelation> Relations { get; set; }

    [DataMember(Name = "rating")]
    public TrackRating Rating { get; set; }

    public string AlbumId { get; set; }

    public string Album { get; set; }

    public bool InitPropertiesFromAlbum(string albumId, string albumName, string country)
    {
      foreach (TrackRelease release in Releases)
      {
        if (!release.Status.Equals("Official", StringComparison.InvariantCultureIgnoreCase)) //Only official releases
          continue;

        if (release.ReleaseGroup != null && !release.ReleaseGroup.PrimaryType.Equals("Album", StringComparison.InvariantCultureIgnoreCase)) //Only album releases
          continue;

        if (ArtistCredit == null)
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
          Album = release.Title;

          return true;
        }
      }
      return false;
    }

    public override string ToString()
    {
      return string.Format("Id: {0}, PrimaryType: {1}", Id, PrimaryType);
    }
  }
}
