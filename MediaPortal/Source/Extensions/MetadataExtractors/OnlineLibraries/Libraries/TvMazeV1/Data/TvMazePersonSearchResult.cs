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
  //{
  //  "score": 3.6594987,
  //  "person": {
  //    "id": 16360,
  //    "url": "http://www.tvmaze.com/people/16360/lauren-melendez",
  //    "name": "Lauren Melendez",
  //    "image": null,
  //    "_links": {
  //      "self": {
  //        "href": "http://api.tvmaze.com/people/16360"
  //      }
  //    }
  //  }
  //}
  [DataContract]
  public class TvMazePersonSearchResult
  {
    [DataMember(Name = "score")]
    public double Score { get; set; }

    [DataMember(Name = "person")]
    public TvMazePerson Person { get; set; }
  }
}
