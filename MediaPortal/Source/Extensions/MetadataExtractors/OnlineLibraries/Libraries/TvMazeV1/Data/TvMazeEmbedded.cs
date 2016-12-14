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

namespace MediaPortal.Extensions.OnlineLibraries.Libraries.TvMazeV1.Data
{
  //{
  //  "episodes": [
  //    {
  //      "id": 12192,
  //      "url": "http://www.tvmaze.com/episodes/12192/breaking-bad-1x01-pilot",
  //      "name": "Pilot",
  //      "season": 1,
  //      "number": 1,
  //      "airdate": "2008-01-20",
  //      "airtime": "22:00",
  //      "airstamp": "2008-01-20T22:00:00-05:00",
  //      "runtime": 60,
  //      "image": {
  //        "medium": "http://tvmazecdn.com/uploads/images/medium_landscape/23/59145.jpg",
  //        "original": "http://tvmazecdn.com/uploads/images/original_untouched/23/59145.jpg"
  //      }
  //    ],
  //  "cast": [
  //   {
  //    "person": {
  //      "id": 12328,
  //      "url": "http://www.tvmaze.com/people/12328/aaron-paul",
  //      "name": "Aaron Paul",
  //      "image": {
  //        "medium": "http://tvmazecdn.com/uploads/images/medium_portrait/3/8206.jpg",
  //        "original": "http://tvmazecdn.com/uploads/images/original_untouched/3/8206.jpg"
  //      },
  //      "_links": {
  //        "self": {
  //          "href": "http://api.tvmaze.com/people/12328"
  //        }
  //      }
  //    }
  //  ]
  //}
  [DataContract]
  public class TvMazeEmbedded
  {
    [DataMember(Name = "episodes")]
    public List<TvMazeEpisode> Episodes { get; set; }

    [DataMember(Name = "cast")]
    public List<TvMazeCast> Cast { get; set; }
  }
}
