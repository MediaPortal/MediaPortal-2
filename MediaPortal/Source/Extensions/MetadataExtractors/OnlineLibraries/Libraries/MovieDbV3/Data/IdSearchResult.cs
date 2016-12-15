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

namespace MediaPortal.Extensions.OnlineLibraries.Libraries.MovieDbV3.Data
{
  //{
  //  "movie_results": [
  //    {
  //      "adult": false,
  //      "backdrop_path": "/n2vIGWw4ezslXjlP0VNxkp9wqwU.jpg",
  //      "genre_ids": [
  //        12,
  //        16,
  //        35,
  //        10751
  //      ],
  //      "id": 12,
  //      "original_language": "en",
  //      "original_title": "Finding Nemo",
  //      "overview": "A tale which follows the comedic and eventful journeys of two fish, the fretful Marlin and his young son Nemo, who are separated from each other in the Great Barrier Reef when Nemo is unexpectedly taken from his home and thrust into a fish tank in a dentist's office overlooking Sydney Harbor. Buoyed by the companionship of a friendly but forgetful fish named Dory, the overly cautious Marlin embarks on a dangerous trek and finds himself the unlikely hero of an epic journey to rescue his son.",
  //      "release_date": "2003-05-30",
  //      "poster_path": "/zjqInUwldOBa0q07fOyohYCWxWX.jpg",
  //      "popularity": 4.255547,
  //      "title": "Finding Nemo",
  //      "video": false,
  //      "vote_average": 7.2,
  //      "vote_count": 2417
  //    }
  //  ],
  //  "person_results": [],
  //  "tv_results": [],
  //  "tv_episode_results": [],
  //  "tv_season_results": []
  //}
  [DataContract]
  public class IdSearchResult
  {
    [DataMember(Name = "movie_results")]
    public List<IdResult> MovieResults { get; set; }

    [DataMember(Name = "person_results")]
    public List<IdResult> PersonResults { get; set; }

    [DataMember(Name = "tv_results")]
    public List<IdResult> SeriesResults { get; set; }

    [DataMember(Name = "tv_episode_results")]
    public List<IdResult> SeriesEpisodeResults { get; set; }

    [DataMember(Name = "tv_season_results")]
    public List<IdResult> SeriesSeasonResults { get; set; }
  }
}
