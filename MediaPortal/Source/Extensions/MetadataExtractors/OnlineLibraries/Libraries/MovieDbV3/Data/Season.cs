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
  //  "air_date": "2008-01-19",
  //  "episodes": [
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
  //      "overview": "When an unassuming high school chemistry teacher discovers he has a rare form of lung cancer, he decides to team up with a former student and create a top of the line crystal meth in a used RV, to provide for his family once he is gone.",
  //      "id": 62085,
  //      "production_code": null,
  //      "season_number": 1,
  //      "still_path": "/88Z0fMP8a88EpQWMCs1593G0ngu.jpg",
  //      "vote_average": 8.5,
  //      "vote_count": 2
  //    }
  //  ],
  //  "name": "Season 1",
  //  "overview": "The first season of the American television drama series Breaking Bad premiered on January 20, 2008 and concluded on March 9, 2008. It consisted of seven episodes, each running approximately 47 minutes in length, except the pilot episode which runs approximately 57 minutes. AMC broadcast the first season on Sundays at 10:00 pm in the United States. Season one was to consist of nine episodes, which was reduced to seven by the writer's strike. The complete first season was released on Region 1 DVD on February 24, 2009 and Region A Blu-ray on March 16, 2010.",
  //  "id": 3572,
  //  "poster_path": "/dHCYpEoHEjAV6Xt3eyNthkdLRl3.jpg",
  //  "season_number": 1
  //}
  [DataContract]
  public class Season
  {
    [DataMember(Name = "id")]
    public int SeasonId { get; set; }

    [DataMember(Name = "name")]
    public string Name { get; set; }

    [DataMember(Name = "air_date")]
    public DateTime? AirDate { get; set; }

    [DataMember(Name = "overview")]
    public string Overview { get; set; }

    [DataMember(Name = "season_number")]
    public int SeasonNumber { get; set; }

    [DataMember(Name = "poster_path")]
    public string PosterPath { get; set; }

    [DataMember(Name = "episodes")]
    public List<SeasonEpisode> Episodes { get; set; }

    [DataMember(Name = "external_ids")]
    public ExternalIds ExternalId { get; set; }

    public override string ToString()
    {
      return string.Format("S{0:00}", SeasonNumber);
    }
  }
}
