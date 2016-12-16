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
  //    {
  //      "air_date": "2008-01-19",
  //      "crew": [
  //        {
  //          "id": 2483,
  //          "credit_id": "52b7029219c29533d00dd2c1",
  //          "name": "John Toll",
  //          "department": "Camera",
  //          "job": "Director of Photography",
  //          "profile_path": null
  //        }
  //      ],
  //      "episode_number": 1,
  //      "guest_stars": [
  //        {
  //          "id": 92495,
  //          "name": "John Koyama",
  //          "credit_id": "52542273760ee3132800068e",
  //          "character": "Emilio Koyama",
  //          "order": 1,
  //          "profile_path": "/uh4g85qbQGZZ0HH6IQI9fM9VUGS.jpg"
  //        }
  //      ],
  //      "name": "Pilot",
  //      "overview": "When an unassuming high school chemistry teacher discovers he has a rare form of lung cancer...",
  //      "id": 62085,
  //      "production_code": null,
  //      "season_number": 1,
  //      "still_path": "/88Z0fMP8a88EpQWMCs1593G0ngu.jpg",
  //      "vote_average": 8.5,
  //      "vote_count": 2
  //    }
  [DataContract]
  public class SeasonEpisode
  {
    [DataMember(Name = "id")]
    public int SeasonId { get; set; }

    [DataMember(Name = "name")]
    public string Name { get; set; }

    [DataMember(Name = "air_date")]
    public DateTime? AirDate { get; set; }

    [DataMember(Name = "overview")]
    public string Overview { get; set; }

    [DataMember(Name = "production_code")]
    public string ProductionCode { get; set; }

    [DataMember(Name = "episode_number")]
    public int EpisodeNumber { get; set; }

    [DataMember(Name = "season_number")]
    public int SeasonNumber { get; set; }

    [DataMember(Name = "guest_stars")]
    public List<CastItem> GuestStars { get; set; }

    [DataMember(Name = "crew")]
    public List<CrewItem> Crew { get; set; }

    [DataMember(Name = "still_path")]
    public string StillPath { get; set; }

    [DataMember(Name = "vote_average")]
    public float? Rating { get; set; }

    [DataMember(Name = "vote_count")]
    public int? RatingCount { get; set; }

    public override string ToString()
    {
      return string.Format("S{0:00}E{1:00} {2}", SeasonNumber, EpisodeNumber, Name);
    }
  }
}
