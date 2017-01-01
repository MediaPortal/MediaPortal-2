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
  //"title": "",
  //"tracks": [
  //  {
  //    "id": "a1c3df99-8a10-3dd3-9f31-2e838747a0d9",
  //    "number": "1",
  //    "title": "Hit the Lights",
  //    "length": 257440,
  //    "artist-credit": [
  //      {
  //        "joinphrase": "",
  //        "name": "Metallica",
  //        "artist": {
  //          "name": "Metallica",
  //          "disambiguation": "",
  //          "id": "65f4f0c5-ef9e-490c-aee3-909e7ae6b2ab",
  //          "sort-name": "Metallica"
  //        }
  //      }
  //    ]
  //  }
  //],
  //"track-count": 10,
  //"track-offset": 0,
  //"discs": [
  //  {
  //    "sectors": 230747,
  //    "offset-count": 10,
  //    "offsets": [
  //      197,
  //      19430,
  //      51860,
  //      65960,
  //      87067,
  //      106445,
  //      124757,
  //      147382,
  //      176332,
  //      207470
  //    ],
  //    "id": "1H8ozo72BOtL._vrJwfqzeU6zqk-"
  //  }
  //],
  //"format": "CD",
  //"position": 1
  //}
  [DataContract]
  public class TrackMedia
  {
    [DataMember(Name = "position")]
    public int Position { get; set; }

    [DataMember(Name = "title")]
    public string Title { get; set; }

    [DataMember(Name = "format")]
    public string Format { get; set; }

    [DataMember(Name = "track-count")]
    public int TrackCount { get; set; }

    [DataMember(Name = "track")]
    public List<TrackData> Track { get; set; }

    [DataMember(Name = "tracks")]
    public List<TrackData> Tracks { get; set; }

    [DataMember(Name = "discs")]
    public List<TrackDisc> Discs { get; set; }

    public override string ToString()
    {
      return string.Format("Position: {0}, Format: {1}, TrackCount: {2}, Tracks: [{3}]", Position, Format, TrackCount, string.Join(",", Track));
    }
  }
}
