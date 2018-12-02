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

using System;
using System.Runtime.Serialization;

namespace MediaPortal.Extensions.OnlineLibraries.Libraries.SimApiV1.Data
{
  //    {
  //        "id": "0468569",
  //        "title": "The Dark Knight",
  //        "year": "2008",
  //        "poster": "N/A",
  //        "type": "movie"
  //    }
  [DataContract]
  public class SimApiMovieSearchItem
  {
    [DataMember(Name = "title")]
    public string Title { get; set; }

    [DataMember(Name = "year")]
    public string StrYear { get; set; }

    [DataMember(Name = "id")]
    public string ID { get; set; }

    [DataMember(Name = "type")]
    public string Type { get; set; }

    [DataMember(Name = "poster")]
    public string PosterUrl { get; set; }

    public int? Year
    {
      get
      {
        int year;
        if (int.TryParse(StrYear, out year) && year > 1000)
          return year;
        return null;
      }
    }

    public string ImdbID
    {
      get
      {
        if (ID != null && !ID.StartsWith("tt", StringComparison.InvariantCultureIgnoreCase))
          return "tt" + ID;
        return ID;
      }
    }

    public int? EndYear { get; private set; }
  }
}
