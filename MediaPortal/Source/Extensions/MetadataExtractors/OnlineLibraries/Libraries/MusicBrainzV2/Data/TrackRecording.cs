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
//          {
//            "artist-credit": [
//              {
//                "name": "Autechre",
//                "joinphrase": " & ",
//                "artist": {
//                  "disambiguation": "",
//                  "sort-name": "Autechre",
//                  "id": "410c9baf-5469-44f6-9852-826524b80c61",
//                  "name": "Autechre"
//                }
//              },
//              {
//                "artist": {
//                  "name": "The Hafler Trio",
//                  "disambiguation": "",
//                  "sort-name": "Hafler Trio, The",
//                  "id": "146c01d0-d3a2-44c3-acb5-9208bce75e14"
//                },
//                "joinphrase": "",
//                "name": "The Hafler Trio"
//              }
//            ],
//            "length": 974546,
//            "title": "�o",
//            "video": false,
//            "id": "af87f070-238b-46c1-aa3e-f831ab91fa20",
//            "disambiguation": ""
//          }
  [DataContract]
  public class TrackRecording
  {
    [DataMember(Name = "id")]
    public string Id { get; set; }

    [DataMember(Name = "title")]
    public string Title { get; set; }

    [DataMember(Name = "artist-credit")]
    public List<TrackArtistCredit> Artists { get; set; }

    [DataMember(Name = "length")]
    public long? Length { get; set; }

    public override string ToString()
    {
      return string.Format("Id: {0}, Title: {1}", Id, Title);
    }
  }
}
