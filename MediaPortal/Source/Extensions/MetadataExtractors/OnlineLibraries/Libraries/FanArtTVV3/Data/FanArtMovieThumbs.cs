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
//  "name": "The Lord of the Rings: The Fellowship of the Ring",
//  "tmdb_id": "120",
//  "imdb_id": "tt0120737",
//  "hdmovielogo": [
//    {
//      "id": "50927",
//      "url": "http://assets.fanart.tv/fanart/movies/120/hdmovielogo/the-lord-of-the-rings-the-fellowship-of-the-ring-5232c108a0b11.png",
//      "lang": "en",
//      "likes": "7"
//    }
//  ],
//  "moviedisc": [
//    {
//      "id": "29003",
//      "url": "http://assets.fanart.tv/fanart/movies/120/moviedisc/the-lord-of-the-rings-the-fellowship-of-the-ring-5156389dc28f2.png",
//      "lang": "en",
//      "likes": "5",
//      "disc": "1",
//      "disc_type": "bluray"
//    }
//  ],
//  "movielogo": [
//    {
//      "id": "1613",
//      "url": "http://assets.fanart.tv/fanart/movies/120/movielogo/the-lord-of-the-rings-the-fellowship-of-the-ring-4f78564165f48.png",
//      "lang": "en",
//      "likes": "4"
//    }
//  ],
//  "movieposter": [
//    {
//      "id": "57317",
//      "url": "http://assets.fanart.tv/fanart/movies/120/movieposter/the-lord-of-the-rings-the-fellowship-of-the-ring-528aa45a8633a.jpg",
//      "lang": "en",
//      "likes": "4"
//    }
//  ],
//  "hdmovieclearart": [
//    {
//      "id": "34307",
//      "url": "http://assets.fanart.tv/fanart/movies/120/hdmovieclearart/the-lord-of-the-rings-the-fellowship-of-the-ring-518f5ccc16a40.png",
//      "lang": "en",
//      "likes": "3"
//    }
//  ],
//  "movieart": [
//    {
//      "id": "1140",
//      "url": "http://assets.fanart.tv/fanart/movies/120/movieart/the-lord-of-the-rings-the-fellowship-of-the-ring-4f6c938a134a1.png",
//      "lang": "en",
//      "likes": "2"
//    }
//  ],
//  "moviebackground": [
//    {
//      "id": "5299",
//      "url": "http://assets.fanart.tv/fanart/movies/120/moviebackground/the-lord-of-the-rings-the-fellowship-of-the-ring-4fdb8b38d6453.jpg",
//      "lang": "en",
//      "likes": "2"
//    }
//  ],
//  "moviebanner": [
//    {
//      "id": "12355",
//      "url": "http://assets.fanart.tv/fanart/movies/120/moviebanner/the-lord-of-the-rings-the-fellowship-of-the-ring-50485f0da465c.jpg",
//      "lang": "en",
//      "likes": "1"
//    }
//  ],
//  "moviethumb": [
//    {
//      "id": "40949",
//      "url": "http://assets.fanart.tv/fanart/movies/120/moviethumb/the-lord-of-the-rings-the-fellowship-of-the-ring-51d7e42a53a1d.jpg",
//      "lang": "en",
//      "likes": "1"
//    }
//  ]
//}
  [DataContract]
  public class FanArtMovieThumbs
  {
    [DataMember(Name = "name")]
    public string Name { get; set; }

    [DataMember(Name = "tmdb_id")]
    public int? TheMovieDbID { get; set; }

    [DataMember(Name = "imdb_id")]
    public string ImDbID { get; set; }

    [DataMember(Name = "hdmovielogo")]
    public List<FanArtMovieThumb> HDMovieLogos { get; set; }

    [DataMember(Name = "moviedisc")]
    public List<FanArtMovieDiscThumb> MovieCDArt { get; set; }

    [DataMember(Name = "movielogo")]
    public List<FanArtMovieThumb> MovieLogos { get; set; }

    [DataMember(Name = "movieposter")]
    public List<FanArtMovieThumb> MoviePosters { get; set; }

    [DataMember(Name = "hdmovieclearart")]
    public List<FanArtMovieThumb> HDMovieClearArt { get; set; }

    [DataMember(Name = "movieart")]
    public List<FanArtMovieThumb> MovieClearArt { get; set; }

    [DataMember(Name = "moviebackground")]
    public List<FanArtMovieThumb> MovieFanArt { get; set; }

    [DataMember(Name = "moviebanner")]
    public List<FanArtMovieThumb> MovieBanners { get; set; }

    [DataMember(Name = "moviethumb")]
    public List<FanArtMovieThumb> MovieThumbnails { get; set; }
  }
}
