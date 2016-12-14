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

using System.Runtime.Serialization;

namespace MediaPortal.Extensions.OnlineLibraries.Libraries.MusicBrainzV2.Data
{
  // {
  //  "life-span": {
  //    "end": "1994-04-05",
  //    "begin": "1988-01",
  //    "ended": true
  //  },
  //  "area": {
  //    "sort-name": "United States",
  //    "iso-3166-1-codes": [
  //      "US"
  //    ],
  //    "name": "United States",
  //    "id": "489ce91b-6658-3307-9877-795b68554c98",
  //    "disambiguation": ""
  //  },
  //  "country": "US",
  //  "begin_area": {
  //    "id": "a640b45c-c173-49b1-8030-973603e895b5",
  //    "disambiguation": "",
  //    "sort-name": "Aberdeen",
  //    "name": "Aberdeen"
  //  },
  //  "sort-name": "Nirvana",
  //  "disambiguation": "90s US grunge band",
  //  "id": "5b11f4ce-a62d-471e-81fc-a69a8278c7da",
  //  "ipis": [


  //  ],
  //  "type": "Group",
  //  "gender": null,
  //  "end_area": null,
  //  "name": "Nirvana"
  //}
  [DataContract]
  public class TrackArtist : TrackBaseName
  {
    [DataMember(Name = "life-span")]
    public TrackLifeSpan LifeSpan { get; set; }

    [DataMember(Name = "begin_area")]
    public TrackArea BeginArea { get; set; }

    [DataMember(Name = "end_area")]
    public TrackArea EndArea { get; set; }

    [DataMember(Name = "area")]
    public TrackArea Area { get; set; }

    [DataMember(Name = "country")]
    public string Country { get; set; }

    [DataMember(Name = "type")]
    public string Type { get; set; }

    [DataMember(Name = "gender")]
    public string Gender { get; set; }

    public override string ToString()
    {
      return string.Format("Id: {0}, Name: {1}, SortName: {2}", Id, Name, SortName);
    }
  }
}
