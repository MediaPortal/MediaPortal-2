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
using System.Reflection;
using System.Runtime.Serialization;

namespace MediaPortal.Extensions.OnlineLibraries.Libraries.OmDbV1.Data
{
//   {
//      "Title": "Batman Begins",
//      "Year": "2005",
//      "imdbID": "tt0372784",
//      "Type": "movie",
//      "Poster": "http://ia.media-imdb.com/images/M/MV5BNTM3OTc0MzM2OV5BMl5BanBnXkFtZTYwNzUwMTI3._V1_SX300.jpg"
//    }
  [DataContract]
  public class OmDbSearchItem : OmDbBaseResponse
  {
    [DataMember(Name = "Title")]
    public string Title { get; set; }

    [DataMember(Name = "Year")]
    public string StrYear { get; set; }

    [DataMember(Name = "imdbID")]
    public string ImdbID { get; set; }

    [DataMember(Name = "Type")]
    public string Type { get; set; }

    [DataMember(Name = "Poster")]
    public string PosterUrl { get; set; }

    public int? Year { get; private set; }
    public int? EndYear { get; private set; }

    public void AssignProperties()
    {
      InitProperties();

      int i;
      if (!string.IsNullOrEmpty(StrYear) && !StrYear.Contains("-") && int.TryParse(StrYear, out i)) Year = i;
      if (!string.IsNullOrEmpty(StrYear) && StrYear.Contains("-") && int.TryParse(StrYear.Split('-')[0], out i)) Year = i;
      if (!string.IsNullOrEmpty(StrYear) && StrYear.Contains("-") && int.TryParse(StrYear.Split('-')[1], out i)) EndYear = i;
      if (!string.IsNullOrEmpty(StrYear) && StrYear.Contains("�") && int.TryParse(StrYear.Split('�')[0], out i)) Year = i;
      if (!string.IsNullOrEmpty(StrYear) && StrYear.Contains("�") && int.TryParse(StrYear.Split('�')[1], out i)) EndYear = i;
    }
  }
}
