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

using System.Collections.Generic;
using System.Runtime.Serialization;

namespace MediaPortal.Extensions.OnlineLibraries.Libraries.MusicBrainzV2.Data
{
  //{
  //  "created": "2016-04-27T11:11:27.118Z",
  //  "count": 1,
  //  "offset": 0,
  //  "artists": [
  //    {
  //      "id": "8538e728-ca0b-4321-b7e5-cff6565dd4c0",
  //      "type": "Group",
  //      "score": "100",
  //      "name": "Depeche Mode",
  //      "sort-name": "Depeche Mode",
  //      "country": "GB",
  //      "area": {
  //        "id": "8a754a16-0027-3a29-b6d7-2b40ea0481ed",
  //        "name": "United Kingdom",
  //        "sort-name": "United Kingdom"
  //      },
  //      "begin-area": {
  //        "id": "9b4cb463-9777-46c3-8190-e1cb3da2749f",
  //        "name": "Basildon",
  //        "sort-name": "Basildon"
  //      },
  //      "life-span": {
  //        "begin": "1980",
  //        "ended": null
  //      },
  //      "aliases": [
  //        {
  //          "sort-name": "Depech Mode",
  //          "name": "Depech Mode",
  //          "locale": null,
  //          "type": null,
  //          "primary": null,
  //          "begin-date": null,
  //          "end-date": null
  //        },
  //        {
  //          "sort-name": "DM",
  //          "name": "DM",
  //          "locale": null,
  //          "type": "Search hint",
  //          "primary": null,
  //          "begin-date": null,
  //          "end-date": null
  //        }
  //      ],
  //      "tags": [
  //        {
  //          "count": 1,
  //          "name": "electronica"
  //        },
  //        {
  //          "count": 1,
  //          "name": "post punk"
  //        },
  //        {
  //          "count": 1,
  //          "name": "alternative dance"
  //        },
  //        {
  //          "count": 6,
  //          "name": "electronic"
  //        },
  //        {
  //          "count": 1,
  //          "name": "dark wave"
  //        },
  //        {
  //          "count": 0,
  //          "name": "britannique"
  //        },
  //        {
  //          "count": 4,
  //          "name": "british"
  //        },
  //        {
  //          "count": 1,
  //          "name": "english"
  //        },
  //        {
  //          "count": 2,
  //          "name": "uk"
  //        },
  //        {
  //          "count": 0,
  //          "name": "rock and indie"
  //        },
  //        {
  //          "count": 1,
  //          "name": "electronic rock"
  //        },
  //        {
  //          "count": 1,
  //          "name": "remix"
  //        },
  //        {
  //          "count": 0,
  //          "name": "synth pop"
  //        },
  //        {
  //          "count": 2,
  //          "name": "alternative rock"
  //        },
  //        {
  //          "count": 0,
  //          "name": "barrel"
  //        },
  //        {
  //          "count": 6,
  //          "name": "synthpop"
  //        },
  //        {
  //          "count": 4,
  //          "name": "new wave"
  //        },
  //        {
  //          "count": 1,
  //          "name": "new romantic"
  //        },
  //        {
  //          "count": 1,
  //          "name": "downtempo"
  //        },
  //        {
  //          "count": 0,
  //          "name": "producteur"
  //        },
  //        {
  //          "count": 0,
  //          "name": "producer"
  //        },
  //        {
  //          "count": 1,
  //          "name": "synth-pop"
  //        }
  //      ]
  //    }
  //  ]
  //}
  [DataContract]
  public class TrackArtistResult
  {
    [DataMember(Name = "artists")]
    public List<TrackArtist> Results { get; set; }
  }
}
