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
  //  "name": "Bones",
  //  "thetvdb_id": "75682",
  //  "clearlogo": [
  //    {
  //      "id": "2112",
  //      "url": "http://assets.fanart.tv/fanart/tv/75682/clearlogo/Bones-75682.png",
  //      "lang": "en",
  //      "likes": "7"
  //    }
  //  ],
  //  "hdtvlogo": [
  //    {
  //      "id": "20329",
  //      "url": "http://assets.fanart.tv/fanart/tv/75682/hdtvlogo/bones-503e7abe533b1.png",
  //      "lang": "en",
  //      "likes": "3"
  //    }
  //  ],
  //  "clearart": [
  //    {
  //      "id": "4301",
  //      "url": "http://assets.fanart.tv/fanart/tv/75682/clearart/B_75682 (3).png",
  //      "lang": "en",
  //      "likes": "2"
  //    }
  //  ],
  //  "showbackground": [
  //    {
  //      "id": "19374",
  //      "url": "http://assets.fanart.tv/fanart/tv/75682/showbackground/bones-500994f33356b.jpg",
  //      "lang": "en",
  //      "likes": "2",
  //      "season": "7"
  //    }
  //  ],
  //  "tvthumb": [
  //    {
  //      "id": "21765",
  //      "url": "http://assets.fanart.tv/fanart/tv/75682/tvthumb/bones-5070c96416c4e.jpg",
  //      "lang": "en",
  //      "likes": "2"
  //    }
  //  ],
  //  "seasonposter": [
  //    {
  //      "id": "38103",
  //      "url": "http://assets.fanart.tv/fanart/tv/75682/seasonposter/bones-533e41121d4c8.jpg",
  //      "lang": "en",
  //      "likes": "2"
  //    }
  //  ],
  //  "seasonthumb": [
  //    {
  //      "id": "4311",
  //      "url": "http://assets.fanart.tv/fanart/tv/75682/seasonthumb/Bones (5).jpg",
  //      "lang": "en",
  //      "likes": "1",
  //      "season": "5"
  //    }
  //  ],
  //  "hdclearart": [
  //    {
  //      "id": "22106",
  //      "url": "http://assets.fanart.tv/fanart/tv/75682/hdclearart/bones-5076b476a0cee.png",
  //      "lang": "en",
  //      "likes": "1"
  //    }
  //  ],
  //  "tvbanner": [
  //    {
  //      "id": "28254",
  //      "url": "http://assets.fanart.tv/fanart/tv/75682/tvbanner/bones-517afc7dc43ed.jpg",
  //      "lang": "en",
  //      "likes": "1"
  //    }
  //  ],
  //  "characterart": [
  //    {
  //      "id": "18513",
  //      "url": "http://assets.fanart.tv/fanart/tv/75682/characterart/bones-4fc8e8b0d3490.png",
  //      "lang": "en",
  //      "likes": "0"
  //    }
  //  ],
  //  "tvposter": [
  //    {
  //      "id": "36565",
  //      "url": "http://assets.fanart.tv/fanart/tv/75682/tvposter/bones-52deb10f82d0f.jpg",
  //      "lang": "en",
  //      "likes": "0"
  //    }
  //  ],
  //  "seasonbanner": [
  //    {
  //      "id": "37718",
  //      "url": "http://assets.fanart.tv/fanart/tv/75682/seasonbanner/bones-532e0883bb9d7.jpg",
  //      "lang": "en",
  //      "likes": "0",
  //      "season": "1"
  //    }
  //  ]
  //}

  [DataContract]
  public class FanArtTVThumbs
  {
    [DataMember(Name = "name")]
    public string Name { get; set; }

    [DataMember(Name = "thetvdb_id")]
    public int? TheTVDbID { get; set; }

    [DataMember(Name = "hdtvlogo")]
    public List<FanArtMovieThumb> HDSeriesLogos { get; set; }

    [DataMember(Name = "characterart")]
    public List<FanArtMovieDiscThumb> SeriesCharacterArt { get; set; }

    [DataMember(Name = "clearlogo")]
    public List<FanArtMovieThumb> SeriesLogos { get; set; }

    [DataMember(Name = "seasonposter")]
    public List<FanArtSeasonThumb> SeasonPosters { get; set; }

    [DataMember(Name = "tvposter")]
    public List<FanArtMovieThumb> SeriesPosters { get; set; }

    [DataMember(Name = "hdclearart")]
    public List<FanArtMovieThumb> HDSeriesClearArt { get; set; }

    [DataMember(Name = "clearart")]
    public List<FanArtMovieThumb> SeriesClearArt { get; set; }

    [DataMember(Name = "showbackground")]
    public List<FanArtMovieThumb> SeriesFanArt { get; set; }

    [DataMember(Name = "tvbanner")]
    public List<FanArtMovieThumb> SeriesBanners { get; set; }

    [DataMember(Name = "seasonbanner")]
    public List<FanArtSeasonThumb> SeasonBanners { get; set; }

    [DataMember(Name = "tvthumb")]
    public List<FanArtMovieThumb> SeriesThumbnails { get; set; }

    [DataMember(Name = "seasonthumb")]
    public List<FanArtSeasonThumb> SeasonThumbnails { get; set; }
  }
}
