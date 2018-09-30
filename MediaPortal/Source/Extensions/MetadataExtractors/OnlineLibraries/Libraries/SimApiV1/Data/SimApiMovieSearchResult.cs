#region Copyright (C) 2007-2018 Team MediaPortal

/*
    Copyright (C) 2007-2018 Team MediaPortal
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

namespace MediaPortal.Extensions.OnlineLibraries.Libraries.SimApiV1.Data
{
  //[
  //    {
  //        "id": "0096895",
  //        "title": "Batman",
  //        "year": "1989",
  //        "poster": "N/A",
  //        "type": "movie"
  //    },
  //    {
  //        "id": "0059968",
  //        "title": "Batman",
  //        "year": "1966",
  //        "poster": "N/A",
  //        "type": "movie"
  //    },
  //    {
  //        "id": "0103359",
  //        "title": "Batman: The Animated Series",
  //        "year": "1992",
  //        "poster": "N/A",
  //        "type": "movie"
  //    },
  //    {
  //        "id": "4116284",
  //        "title": "The LEGO Batman Movie",
  //        "year": "2017",
  //        "poster": "N/A",
  //        "type": "movie"
  //    },
  //    {
  //        "id": "0468569",
  //        "title": "The Dark Knight",
  //        "year": "2008",
  //        "poster": "N/A",
  //        "type": "movie"
  //    }
  //]
  [DataContract]
  public class SimApiMovieSearchResult
  {
    public List<SimApiMovieSearchItem> SearchResults { get; set; }
  }
}
