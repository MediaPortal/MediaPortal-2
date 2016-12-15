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
  //      "backdrop_path": "/eSzpy96DwBujGFj0xMbXBcGcfxX.jpg",
  //      "first_air_date": "2008-01-19",
  //      "genre_ids": [
  //        18
  //      ],
  //      "id": 1396,
  //      "original_language": "en",
  //      "original_name": "Breaking Bad",
  //      "overview": "Breaking Bad is an American crime drama television series created and produced by Vince Gilligan. Set and produced in Albuquerque, New Mexico, Breaking Bad is the story of Walter White, a struggling high school chemistry teacher who is diagnosed with inoperable lung cancer at the beginning of the series. He turns to a life of crime, producing and selling methamphetamine, in order to secure his family's financial future before he dies, teaming with his former student, Jesse Pinkman. Heavily serialized, the series is known for positioning its characters in seemingly inextricable corners and has been labeled a contemporary western by its creator.",
  //      "origin_country": [
  //        "US"
  //      ],
  //      "poster_path": "/4yMXf3DW6oCL0lVPZaZM2GypgwE.jpg",
  //      "popularity": 18.095686,
  //      "name": "Breaking Bad",
  //      "vote_average": 8.9,
  //      "vote_count": 245
  //    }
  [DataContract]
  public class SeriesSearchResult
  {
    [DataMember(Name = "id")]
    public int Id { get; set; }

    [DataMember(Name = "name")]
    public string Name { get; set; }

    [DataMember(Name = "first_air_date")]
    public DateTime? FirstAirDate { get; set; }

    [DataMember(Name = "genre_ids")]
    public List<int> GenreIds { get; set; }

    [DataMember(Name = "last_air_date")]
    public DateTime? LastAirDate { get; set; }

    [DataMember(Name = "origin_country")]
    public List<string> OriginCountry { get; set; }

    [DataMember(Name = "original_language")]
    public string OriginalLanguage { get; set; }

    [DataMember(Name = "original_name")]
    public string OriginalName { get; set; }

    [DataMember(Name = "overview")]
    public string Overview { get; set; }

    [DataMember(Name = "popularity")]
    public float? Popularity { get; set; }

    [DataMember(Name = "vote_average")]
    public float? Rating { get; set; }

    [DataMember(Name = "vote_count")]
    public int? RatingCount { get; set; }

    [DataMember(Name = "poster_path")]
    public string PosterPath { get; set; }

    [DataMember(Name = "backdrop_path")]
    public string BackdropPath { get; set; }

    public override string ToString()
    {
      return Name;
    }
  }
}
