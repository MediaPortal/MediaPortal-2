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
  [DataContract]
  public class TrackRelease
  {
    [DataMember(Name = "id")]
    public string Id { get; set; }

    [DataMember(Name = "status")]
    public string Status { get; set; }

    [DataMember(Name = "title")]
    public string Title { get; set; }

    [DataMember(Name = "release-group")]
    public TrackReleaseGroup ReleaseGroup { get; set; }
    
    [DataMember(Name = "date")]
    public DateTime? Date { get; set; }

    [DataMember(Name = "country")]
    public string Country { get; set; }

    [DataMember(Name = "barcode")]
    public string Barcode { get; set; }

    [DataMember(Name = "track-count")]
    public int TrackCount { get; set; }

    [DataMember(Name = "media")]
    public IList<TrackMedia> Media { get; set; }

    [DataMember(Name = "artist-credit")]
    public IList<TrackArtistCredit> Artists { get; set; }

    public override string ToString()
    {
      return string.Format("Id: {0}, Title: {1}, Status: {2}, Date: {3}, Country: {4}, TrackCount: {5}, Media: [{6}]", Id, Title, Status, Date, Country, TrackCount, string.Join(",", Media));
    }
  }
}
