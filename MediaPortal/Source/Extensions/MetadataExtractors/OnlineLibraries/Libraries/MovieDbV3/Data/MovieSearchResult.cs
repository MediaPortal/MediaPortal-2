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
  //  "backdrop_path": "/AkE7LQs2hPMG5tpWYcum847Knre.jpg",
  //  "id": 1891,
  //  "original_title": "Star Wars: Episode V - The Empire Strikes Back",
  //  "popularity": 8412.049,
  //  "poster_path": "/6u1fYtxG5eqjhtCPDx04pJphQRW.jpg",
  //  "release_date": "1980-05-21",
  //  "title": "Star Wars: Episode V - The Empire Strikes Back"
  [DataContract]
  public class MovieSearchResult
  {
    [DataMember(Name = "id")]
    public int Id { get; set; }

    [DataMember(Name = "title")]
    public string Title { get; set; }

    [DataMember(Name = "original_title")]
    public string OriginalTitle { get; set; }

    [DataMember(Name = "poster_path")]
    public string PosterPath { get; set; }

    [DataMember(Name = "backdrop_path")]
    public string BackdropPath { get; set; }

    [DataMember(Name = "release_date")]
    public DateTime? ReleaseDate { get; set; }
  }
}