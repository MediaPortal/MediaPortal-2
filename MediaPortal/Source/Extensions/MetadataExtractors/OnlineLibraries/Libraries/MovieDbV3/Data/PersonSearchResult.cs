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
  //      "adult": false,
  //      "id": 287,
  //      "known_for": [
  //        {
  //          "adult": false,
  //          "backdrop_path": "/8uO0gUM8aNqYLs1OsTBQiXu0fEv.jpg",
  //          "genre_ids": [
  //            18
  //          ],
  //          "id": 550,
  //          "original_language": "en",
  //          "original_title": "Fight Club",
  //          "overview": "A ticking-time-bomb insomniac and a slippery soap salesman channel primal male aggression into a shocking new form of therapy. Their concept catches on, with underground \"fight clubs\" forming in every town, until an eccentric gets in the way and ignites an out-of-control spiral toward oblivion.",
  //          "release_date": "1999-10-14",
  //          "poster_path": "/811DjJTon9gD6hZ8nCjSitaIXFQ.jpg",
  //          "popularity": 3.146334,
  //          "title": "Fight Club",
  //          "video": false,
  //          "vote_average": 7.8,
  //          "vote_count": 3526,
  //          "media_type": "movie"
  //        }
  //      ],
  //      "name": "Brad Pitt",
  //      "popularity": 8.357117,
  //      "profile_path": "/kc3M04QQAuZ9woUvH3Ju5T7ZqG5.jpg"
  //    }
  [DataContract]
  public class PersonSearchResult
  {
    [DataMember(Name = "id")]
    public int Id { get; set; }

    [DataMember(Name = "name")]
    public string Name { get; set; }

    [DataMember(Name = "popularity")]
    public float? Popularity { get; set; }

    [DataMember(Name = "profile_path")]
    public string ProfilePath { get; set; }

    public override string ToString()
    {
      return Name;
    }
  }
}
