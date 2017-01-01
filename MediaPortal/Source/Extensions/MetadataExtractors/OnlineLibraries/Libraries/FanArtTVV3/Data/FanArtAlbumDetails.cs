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

namespace MediaPortal.Extensions.OnlineLibraries.Libraries.FanArtTVV3.Data
{
  //{
  //  "name": "Evanescence",
  //  "mbid_id": "f4a31f0a-51dd-4fa7-986d-3095c40c5ed9",
  //  "albums": {
  //    "9ba659df-5814-32f6-b95f-02b738698e7c": {
  //      "cdart": [
  //        {
  //          "id": "12420",
  //          "url": "http://assets.fanart.tv/fanart/music/f4a31f0a-51dd-4fa7-986d-3095c40c5ed9/cdart/anywhere-but-home-4e9a1074d0999.png",
  //          "likes": "0",
  //          "disc": "1",
  //          "size": "1000"
  //        }
  //      ],
  //      "albumcover": [
  //        {
  //          "id": "116236",
  //          "url": "http://assets.fanart.tv/fanart/music/f4a31f0a-51dd-4fa7-986d-3095c40c5ed9/albumcover/anywhere-but-home-532dbf4618e4b.jpg",
  //          "likes": "0"
  //        }
  //      ]
  //    }
  //  }
  //}
  [DataContract]
  public class FanArtAlbumDetails
  {
    [DataMember(Name = "mbid_id")]
    public string MusicBrainzID { get; set; }

    [DataMember(Name = "name")]
    public string Name { get; set; }

    [DataMember(Name = "albums")]
    public Dictionary<string, FanArtAlbumThumbs> Albums { get; set; }
  }
}


