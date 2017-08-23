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

namespace MediaPortal.Extensions.OnlineLibraries.Libraries.TvMazeV1.Data
{
  //  {
  //    "score": 2.072441,
  //    "show": {
  //      "id": 139,
  //      "url": "http://www.tvmaze.com/shows/139/girls",
  //      "name": "Girls",
  //      "type": "Scripted",
  //      "language": "English",
  //      "genres": [
  //        "Drama",
  //        "Romance"
  //      ],
  //      "status": "Running",
  //      "runtime": 30,
  //      "premiered": "2012-04-15",
  //      "schedule": {
  //        "time": "22:00",
  //        "days": [
  //          "Sunday"
  //        ]
  //},
  //      "rating": {
  //        "average": 5.5
  //      },
  //      "weight": 5,
  //      "network": {
  //        "id": 8,
  //        "name": "HBO",
  //        "country": {
  //          "name": "United States",
  //          "code": "US",
  //          "timezone": "America/New_York"
  //        }
  //      },
  //      "webChannel": null,
  //      "externals": {
  //        "tvrage": 30124,
  //        "thetvdb": 220411,
  //        "imdb": "tt1723816"
  //      },
  //      "image": {
  //        "medium": "http://tvmazecdn.com/uploads/images/medium_portrait/31/78286.jpg",
  //        "original": "http://tvmazecdn.com/uploads/images/original_untouched/31/78286.jpg"
  //      },
  //      "summary": "<p>Created by and starring Lena Dunham, the Emmy(R)-winning series is a comic look at the assorted humiliations and rare triumphs of a group of girls in their 20s.</p>",
  //      "updated": 1459882572,
  //      "_links": {
  //        "self": {
  //          "href": "http://api.tvmaze.com/shows/139"
  //        },
  //        "previousepisode": {
  //          "href": "http://api.tvmaze.com/episodes/616043"
  //        },
  //        "nextepisode": {
  //          "href": "http://api.tvmaze.com/episodes/621945"
  //        }
  //      }
  //    }
  //  }
  [DataContract]
  public class TvMazeSeriesSearchResult
  {
    [DataMember(Name = "score")]
    public double Score { get; set; }

    [DataMember(Name = "show")]
    public TvMazeSeries Series { get; set; }
  }
}
