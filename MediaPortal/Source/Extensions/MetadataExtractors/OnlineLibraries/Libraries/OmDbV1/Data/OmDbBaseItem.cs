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
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;

namespace MediaPortal.Extensions.OnlineLibraries.Libraries.OmDbV1.Data
{
  [DataContract]
  public class OmDbBaseItem : OmDbBaseResponse
  {
    [DataMember(Name = "Title")]
    public string Title { get; set; }

    [DataMember(Name = "Released")]
    public string StrReleased { get; set; }

    [DataMember(Name = "Rated")]
    public string Rated { get; set; }

    [DataMember(Name = "Year")]
    public string StrYear { get; set; }

    [DataMember(Name = "Runtime")]
    public string StrRuntime { get; set; }

    [DataMember(Name = "Genre")]
    public string StrGenre { get; set; }

    [DataMember(Name = "Director")]
    public string StrDirector { get; set; }

    [DataMember(Name = "Writer")]
    public string StrWriter { get; set; }

    [DataMember(Name = "Actors")]
    public string StrActors { get; set; }

    [DataMember(Name = "Plot")]
    public string Plot { get; set; }

    [DataMember(Name = "Language")]
    public string Language { get; set; }

    [DataMember(Name = "Country")]
    public string Country { get; set; }

    [DataMember(Name = "Awards")]
    public string Awards { get; set; }

    [DataMember(Name = "Poster")]
    public string PosterUrl { get; set; }

    [DataMember(Name = "Metascore")]
    public string StrMetascore { get; set; }

    [DataMember(Name = "imdbRating")]
    public string StrImdbRating { get; set; }

    [DataMember(Name = "imdbVotes")]
    public string StrImdbVotes { get; set; }

    [DataMember(Name = "imdbID")]
    public string ImdbID { get; set; }

    [DataMember(Name = "Type")]
    public string Type { get; set; }

    [DataMember(Name = "tomatoMeter")]
    public string StrTomatoMeter { get; set; }

    [DataMember(Name = "tomatoImage")]
    public string TomatoImage { get; set; }

    [DataMember(Name = "tomatoRating")]
    public string StrTomatoRating { get; set; }

    [DataMember(Name = "tomatoReviews")]
    public string StrTomatoReviews { get; set; }

    [DataMember(Name = "tomatoFresh")]
    public string StrTomatoFresh { get; set; }

    [DataMember(Name = "tomatoRotten")]
    public string StrTomatoRotten { get; set; }

    [DataMember(Name = "tomatoConsensus")]
    public string TomatoConsensus { get; set; }

    [DataMember(Name = "tomatoUserMeter")]
    public string StrTomatoUserMeter { get; set; }

    [DataMember(Name = "tomatoUserRating")]
    public string StrTomatoUserRating { get; set; }

    [DataMember(Name = "tomatoUserReviews")]
    public string StrTomatoUserReviews { get; set; }

    [DataMember(Name = "tomatoURL")]
    public string TomatoURL { get; set; }

    [DataMember(Name = "DVD")]
    public string StrDVD { get; set; }

    [DataMember(Name = "BoxOffice")]
    public string StrBoxOffice { get; set; }

    [DataMember(Name = "Production")]
    public string Production { get; set; }

    [DataMember(Name = "Website")]
    public string Website { get; set; }

    [DataMember(Name = "Response")]
    public bool ResponseValid { get; set; }

    public DateTime? Released { get; private set; }
    public int? Year { get; private set; }
    public int? EndYear { get; private set; }
    public int? Runtime { get; private set; }
    public List<string> Genres { get; private set; }
    public List<string> Directors { get; private set; }
    public List<string> Writers { get; private set; }
    public List<string> Actors { get; private set; }
    public int? Metascore { get; set; }
    public double? ImdbRating { get; set; }
    public int? ImdbVotes { get; set; }
    public int? TomatoMeter { get; set; }
    public double? TomatoRating { get; set; }
    public int? TomatoTotalReviews { get; set; }
    public int? TomatoFreshReviews { get; set; }
    public int? TomatoRottenReviews { get; set; }
    public int? TomatoUserMeter { get; set; }
    public int? TomatoUserRating { get; set; }
    public int? TomatoUserTotalReviews { get; set; }
    public DateTime? DVDRelease { get; set; }
    public long? Revenue { get; set; }

    public void AssignProperties()
    {
      if (!ResponseValid) return;

      InitProperties();

      DateTime dt;
      if (!string.IsNullOrEmpty(StrReleased) && DateTime.TryParse(StrReleased, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out dt)) Released = dt;
      if (!string.IsNullOrEmpty(StrDVD) && DateTime.TryParse(StrDVD, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out dt)) DVDRelease = dt;

      int i;
      if (!string.IsNullOrEmpty(StrYear) && !StrYear.Contains("-") && int.TryParse(StrYear, out i)) Year = i;
      if (!string.IsNullOrEmpty(StrYear) && StrYear.Contains("-") && int.TryParse(StrYear.Split('-')[0], out i)) Year = i;
      if (!string.IsNullOrEmpty(StrYear) && StrYear.Contains("-") && int.TryParse(StrYear.Split('-')[1], out i)) EndYear = i;
      if (!string.IsNullOrEmpty(StrYear) && StrYear.Contains("�") && int.TryParse(StrYear.Split('�')[0], out i)) Year = i;
      if (!string.IsNullOrEmpty(StrYear) && StrYear.Contains("�") && int.TryParse(StrYear.Split('�')[1], out i)) EndYear = i;
      if (!string.IsNullOrEmpty(StrRuntime) && StrRuntime.EndsWith("min", StringComparison.InvariantCultureIgnoreCase) &&
         int.TryParse(StrRuntime.Remove(StrRuntime.Length - 3).Trim(), out i)) Runtime = i;
      if (!string.IsNullOrEmpty(StrImdbVotes) && int.TryParse(StrImdbVotes, out i)) ImdbVotes = i;
      if (!string.IsNullOrEmpty(StrTomatoMeter) && int.TryParse(StrTomatoMeter, out i)) TomatoMeter = i;
      if (!string.IsNullOrEmpty(StrTomatoReviews) && int.TryParse(StrTomatoReviews, out i)) TomatoTotalReviews = i;
      if (!string.IsNullOrEmpty(StrTomatoFresh) && int.TryParse(StrTomatoFresh, out i)) TomatoFreshReviews = i;
      if (!string.IsNullOrEmpty(StrTomatoRotten) && int.TryParse(StrTomatoRotten, out i)) TomatoRottenReviews = i;
      if (!string.IsNullOrEmpty(StrTomatoUserMeter) && int.TryParse(StrTomatoUserMeter, out i)) TomatoUserMeter = i;
      if (!string.IsNullOrEmpty(StrTomatoUserReviews) && int.TryParse(StrTomatoUserReviews, out i)) TomatoUserTotalReviews = i;

      double d;
      if (!string.IsNullOrEmpty(StrImdbRating) && double.TryParse(StrImdbRating, NumberStyles.Float, CultureInfo.InvariantCulture, out d)) ImdbRating = d;
      if (!string.IsNullOrEmpty(StrTomatoRating) && double.TryParse(StrTomatoRating, NumberStyles.Float, CultureInfo.InvariantCulture, out d)) TomatoRating = d;
      if (!string.IsNullOrEmpty(StrBoxOffice) && StrBoxOffice.StartsWith("$", StringComparison.InvariantCultureIgnoreCase) &&
        StrBoxOffice.EndsWith("M", StringComparison.InvariantCultureIgnoreCase) && 
        double.TryParse(StrBoxOffice.Substring(1, StrImdbRating.Length - 2), NumberStyles.Float, CultureInfo.InvariantCulture, out d)) Revenue = Convert.ToInt64(d * 1000000);

      string[] strings = null;
      if (!string.IsNullOrEmpty(StrGenre)) strings = StrGenre.Split(',');
      if (strings != null) Genres = new List<string>(strings).Select(s => CleanString(s)).Distinct().ToList();

      strings = null;
      if (!string.IsNullOrEmpty(StrDirector)) strings = StrDirector.Split(',');
      if (strings != null) Directors = new List<string>(strings).Select(s => CleanString(s)).Distinct().ToList();

      strings = null;
      if (!string.IsNullOrEmpty(StrWriter)) strings = StrWriter.Split(',');
      if (strings != null) Writers = new List<string>(strings).Select(s => CleanString(s)).Distinct().ToList();

      strings = null;
      if (!string.IsNullOrEmpty(StrActors)) strings = StrActors.Split(',');
      if (strings != null) Actors = new List<string>(strings).Select(s => CleanString(s)).Distinct().ToList();
    }

    private string CleanString(string orignString)
    {
      if (orignString.Contains("(")) orignString = orignString.Substring(0, orignString.IndexOf("("));
      return orignString.Trim();
    }
  }
}
