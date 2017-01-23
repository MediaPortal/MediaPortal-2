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
//  {
//  "name": "Evanescence",
//  "mbid_id": "f4a31f0a-51dd-4fa7-986d-3095c40c5ed9",
//  "artistbackground": [
//    {
//      "id": "6",
//      "url": "http://assets.fanart.tv/fanart/music/f4a31f0a-51dd-4fa7-986d-3095c40c5ed9/artistbackground/evanescence-4dc7198199ccd.jpg",
//      "likes": "4"
//    }
//  ],
//  "artistthumb": [
//    {
//      "id": "60344",
//      "url": "http://assets.fanart.tv/fanart/music/f4a31f0a-51dd-4fa7-986d-3095c40c5ed9/artistthumb/evanescence-5097c77793b6f.jpg",
//      "likes": "3"
//    }
//  ],
//  "musiclogo": [
//    {
//      "id": "5474",
//      "url": "http://assets.fanart.tv/fanart/music/f4a31f0a-51dd-4fa7-986d-3095c40c5ed9/musiclogo/evanescence-4df95bceb4b1c.png",
//      "likes": "2"
//    }
//  ],
//  "hdmusiclogo": [
//    {
//      "id": "50850",
//      "url": "http://assets.fanart.tv/fanart/music/f4a31f0a-51dd-4fa7-986d-3095c40c5ed9/hdmusiclogo/evanescence-5049ce8bbe373.png",
//      "likes": "2"
//    }
//  ],
//  "albums": {
//    "2187d248-1a3b-35d0-a4ec-bead586ff547": {
//      "albumcover": [
//        {
//          "id": "43",
//          "url": "http://assets.fanart.tv/fanart/music/f4a31f0a-51dd-4fa7-986d-3095c40c5ed9/albumcover/fallen-4dc8683fa58fe.jpg",
//          "likes": "1"
//        }
//      ],
//      "cdart": [
//        {
//          "id": "17739",
//          "url": "http://assets.fanart.tv/fanart/music/f4a31f0a-51dd-4fa7-986d-3095c40c5ed9/cdart/fallen-4f133f8a16d25.png",
//          "likes": "0",
//          "disc": "1",
//          "size": "1000"
//        }
//      ]
//    }
//  },
//  "musicbanner": [
//    {
//      "id": "56733",
//      "url": "http://assets.fanart.tv/fanart/music/f4a31f0a-51dd-4fa7-986d-3095c40c5ed9/musicbanner/evanescence-507beae754bf6.jpg",
//      "likes": "1"
//    }
//  ]
//}
  [DataContract]
  public class FanArtArtistThumbs : FanArtAlbumDetails
  {
    [DataMember(Name = "artistthumb")]
    public List<FanArtThumb> ArtistThumbnails { get; set; }

    [DataMember(Name = "musiclogo")]
    public List<FanArtThumb> ArtistLogos { get; set; }

    [DataMember(Name = "hdmusiclogo")]
    public List<FanArtThumb> HDArtistLogos { get; set; }

    [DataMember(Name = "artistbackground")]
    public List<FanArtThumb> ArtistFanart { get; set; }

    [DataMember(Name = "musicbanner")]
    public List<FanArtThumb> ArtistBanners { get; set; }
  }
}
