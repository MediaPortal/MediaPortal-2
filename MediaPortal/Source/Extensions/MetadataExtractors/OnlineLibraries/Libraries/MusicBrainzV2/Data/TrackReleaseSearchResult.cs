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
  //    {
  //        "created": "2016-05-19T20:50:37.104Z",
  //        "count": 21,
  //        "offset": 0,
  //        "releases": [
  //        {
  //          "id": "21ea7b2c-8633-4aa7-9aa1-be3bb7972a98",
  //          "score": "100",
  //          "count": 1,
  //          "title": "Fred Schneider",
  //          "status": "Official",
  //          "packaging": "Jewel Case",
  //          "text-representation": {
  //            "language": "eng",
  //            "script": "Latn"
  //          },
  //          "artist-credit": [
  //            {
  //              "artist": {
  //                "id": "43bcca8b-9edc-4997-8343-122350e790bf",
  //                "name": "Fred Schneider",
  //                "sort-name": "Schneider, Fred",
  //                "aliases": [
  //                  {
  //                    "sort-name": "Schneider, Frederick William",
  //                    "name": "Frederick William Schneider",
  //                    "locale": null,
  //                    "type": "Legal name",
  //                    "primary": null,
  //                    "begin-date": null,
  //                    "end-date": null
  //                  }
  //                ]
  //              }
  //            }
  //          ],
  //          "release-group": {
  //            "id": "0ef97d52-3f00-31bf-8413-f83ccb362675",
  //            "primary-type": "Album"
  //          },
  //          "date": "1991",
  //          "country": "DE",
  //          "release-events": [
  //            {
  //              "date": "1991",
  //              "area": {
  //                "id": "85752fda-13c4-31a3-bee5-0e5cb1f51dad",
  //                "name": "Germany",
  //                "sort-name": "Germany",
  //                "iso-3166-1-codes": [
  //                  "DE"
  //                ]
  //              }
  //            }
  //          ],
  //          "barcode": "075992659222",
  //          "label-info": [
  //            {
  //              "catalog-number": "7599-26592-2",
  //              "label": {
  //                "id": "af6d6f49-2b4d-40fe-86d4-241906772b59",
  //                "name": "Reprise Records"
  //              }
  //            }
  //          ],
  //          "track-count": 9,
  //          "media": [
  //            {
  //              "format": "CD",
  //              "disc-count": 0,
  //              "track-count": 9
  //            }
  //          ]
  //        }
  //      ]
  //    }
  [DataContract]
  public class TrackReleaseSearchResult
  {
    [DataMember(Name = "releases")]
    public List<TrackRelease> Releases { get; set; }
  }
}
