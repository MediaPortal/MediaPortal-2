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

namespace MediaPortal.Extensions.OnlineLibraries.Libraries.TvMazeV1.Data
{
  //{
  //  "id": 169,
  //  "url": "http://www.tvmaze.com/shows/169/breaking-bad",
  //  "name": "Breaking Bad",
  //  "type": "Scripted",
  //  "language": "English",
  //  "genres": [
  //    "Drama",
  //    "Crime",
  //    "Thriller"
  //  ],
  //  "status": "Ended",
  //  "runtime": 60,
  //  "premiered": "2008-01-20",
  //  "schedule": {
  //    "time": "22:00",
  //    "days": [
  //      "Sunday"
  //    ]
  //},
  //  "rating": {
  //    "average": 9.4
  //  },
  //  "weight": 4,
  //  "network": {
  //    "id": 20,
  //    "name": "AMC",
  //    "country": {
  //      "name": "United States",
  //      "code": "US",
  //      "timezone": "America/New_York"
  //    }
  //  },
  //  "webChannel": null,
  //  "externals": {
  //    "tvrage": 18164,
  //    "thetvdb": 81189,
  //    "imdb": "tt0903747"
  //  },
  //  "image": {
  //    "medium": "http://tvmazecdn.com/uploads/images/medium_portrait/0/2400.jpg",
  //    "original": "http://tvmazecdn.com/uploads/images/original_untouched/0/2400.jpg"
  //  },
  //  "summary": "<p><em><strong>\"Breaking Bad\"</strong></em> follows protagonist Walter White, a chemistry teacher who lives in New Mexico with his wife and teenage son who has cerebral palsy. White is diagnosed with Stage III cancer and given a prognosis of two years left to live. With a new sense of fearlessness based on his medical prognosis, and a desire to secure his family's financial security, White chooses to enter a dangerous world of drugs and crime and ascends to power in this world. The series explores how a fatal diagnosis such as White's releases a typical man from the daily concerns and constraints of normal society and follows his transformation from mild family man to a kingpin of the drug trade.</p>",
  //  "updated": 1458598416,
  //  "_links": {
  //    "self": {
  //      "href": "http://api.tvmaze.com/shows/169"
  //    },
  //    "previousepisode": {
  //      "href": "http://api.tvmaze.com/episodes/12253"
  //    }
  //  },
  //  "_embedded": {
  //    "episodes": [
  //      {
  //        "id": 12192,
  //        "url": "http://www.tvmaze.com/episodes/12192/breaking-bad-1x01-pilot",
  //        "name": "Pilot",
  //        "season": 1,
  //        "number": 1,
  //        "airdate": "2008-01-20",
  //        "airtime": "22:00",
  //        "airstamp": "2008-01-20T22:00:00-05:00",
  //        "runtime": 60,
  //        "image": {
  //          "medium": "http://tvmazecdn.com/uploads/images/medium_landscape/23/59145.jpg",
  //          "original": "http://tvmazecdn.com/uploads/images/original_untouched/23/59145.jpg"
  //        },
  //        "summary": "<p>A high-school chemistry teacher (Bryan Cranston) is diagnosed with a deadly cancer, so he puts his expertise to use and teams with an ex-student (Aaron Paul) to manufacture top-grade crystal meth in hopes of providing for his family after he's gone.</p>",
  //        "_links": {
  //          "self": {
  //            "href": "http://api.tvmaze.com/episodes/12192"
  //          }
  //        }
  //      }
  //    ]
  //  }
  //}
  [DataContract]
  public class TvMazeSeries
  {
    [DataMember(Name = "id")]
    public int Id { get; set; }

    [DataMember(Name = "url")]
    public string URL { get; set; }

    [DataMember(Name = "name")]
    public string Name { get; set; }

    [DataMember(Name = "type")]
    public string Type { get; set; }

    [DataMember(Name = "language")]
    public string Language { get; set; }

    [DataMember(Name = "genres")]
    public List<string> Genres { get; set; }

    [DataMember(Name = "status")]
    public string Status { get; set; }

    [DataMember(Name = "runtime")]
    public int? Runtime { get; set; }

    [DataMember(Name = "premiered")]
    public DateTime? Premiered { get; set; }

    [DataMember(Name = "schedule")]
    public TvMazeSchedule Schedule { get; set; }

    [DataMember(Name = "rating")]
    public TvMazeRating Rating { get; set; }

    [DataMember(Name = "weight")]
    public int Weight { get; set; }

    [DataMember(Name = "webChannel")]
    public TvMazeNetwork WebNetwork { get; set; }

    [DataMember(Name = "network")]
    public TvMazeNetwork Network { get; set; }

    [DataMember(Name = "externals")]
    public TvMazeExternals Externals { get; set; }

    [DataMember(Name = "image")]
    public TvMazeImageCollection Images { get; set; }

    [DataMember(Name = "summary")]
    public string Summary { get; set; }

    [DataMember(Name = "updated")]
    public long Updated { get; set; }

    [DataMember(Name = "_links")]
    public TvMazeLinkCollection Links { get; set; }

    [DataMember(Name = "_embedded")]
    public TvMazeEmbedded Embedded { get; set; }

    public override string ToString()
    {
      return Name;
    }
  }
}
