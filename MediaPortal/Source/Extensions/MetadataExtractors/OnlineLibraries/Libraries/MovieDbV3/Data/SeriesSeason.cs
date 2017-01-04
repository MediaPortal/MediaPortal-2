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
using System.Runtime.Serialization;

namespace MediaPortal.Extensions.OnlineLibraries.Libraries.MovieDbV3.Data
{
  //{
  //  "air_date": "2009-02-17",
  //  "episode_count": 6,
  //  "id": 3577,
  //  "poster_path": "/spPmYZAq2xLKQOEIdBPkhiRxrb9.jpg",
  //  "season_number": 0
  //}
  [DataContract]
  public class SeriesSeason
  {
    [DataMember(Name = "id")]
    public int SeasonId { get; set; }

    [DataMember(Name = "air_date")]
    public DateTime? AirDate { get; set; }

    [DataMember(Name = "episode_count")]
    public int EpisodeCount { get; set; }

    [DataMember(Name = "season_number")]
    public int SeasonNumber { get; set; }

    [DataMember(Name = "poster_path")]
    public string PosterPath { get; set; }

    public override string ToString()
    {
      return string.Format("S{0:00}", SeasonNumber);
    }
  }
}
