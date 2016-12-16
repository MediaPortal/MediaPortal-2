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

namespace MediaPortal.Extensions.OnlineLibraries.Libraries.OmDbV1.Data
{
  // 10 results per page
  //  {
  //  "Search": [
  //    {
  //      "Title": "Batman Begins",
  //      "Year": "2005",
  //      "imdbID": "tt0372784",
  //      "Type": "movie",
  //      "Poster": "http://ia.media-imdb.com/images/M/MV5BNTM3OTc0MzM2OV5BMl5BanBnXkFtZTYwNzUwMTI3._V1_SX300.jpg"
  //    }
  //  ],
  //  "totalResults": "1",
  //  "Response": "True"
  //}
  [DataContract]
  public class OmDbSearchResult
  {
    [DataMember(Name = "Search")]
    public List<OmDbSearchItem> SearchResults { get; set; }

    [DataMember(Name = "totalResults")]
    public int TotalResults { get; set; }

    [DataMember(Name = "Response")]
    public bool ResponseValid { get; set; }
  }
}
