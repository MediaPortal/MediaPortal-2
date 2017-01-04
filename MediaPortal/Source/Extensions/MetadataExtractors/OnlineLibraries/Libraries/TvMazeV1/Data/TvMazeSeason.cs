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

namespace MediaPortal.Extensions.OnlineLibraries.Libraries.TvMazeV1.Data
{
  //{
  //"id": 1,
  //"url": "http://www.tvmaze.com/seasons/1/under-the-dome-season-1",
  //"number": 1,
  //"name": "",
  //"episodeOrder": 13,
  //"premiereDate": "2013-06-24",
  //"endDate": "2013-09-16",
  //"network": {
  //  "id": 2,
  //  "name": "CBS",
  //  "country": {
  //    "name": "United States",
  //    "code": "US",
  //    "timezone": "America/New_York"
  //  }
  //}
  [DataContract]
  public class TvMazeSeason
  {
    [DataMember(Name = "id")]
    public int Id { get; set; }

    [DataMember(Name = "url")]
    public string URL { get; set; }

    [DataMember(Name = "name")]
    public string Name { get; set; }

    [DataMember(Name = "number")]
    public int SeasonNumber { get; set; }

    [DataMember(Name = "episodeOrder")]
    public int? EpisodeCount { get; set; }

    [DataMember(Name = "premiereDate")]
    public DateTime? PremiereDate { get; set; }

    [DataMember(Name = "endDate")]
    public DateTime? EndDate { get; set; }

    [DataMember(Name = "network")]
    public TvMazeNetwork Network { get; set; }
  }
}
