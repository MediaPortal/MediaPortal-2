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

namespace MediaPortal.Extensions.OnlineLibraries.Libraries.Trakt.DataStructures
{
  [DataContract]
  public class TraktShowSummary : TraktShow
  {
    [DataMember(Name = "images")]
    public TraktShowImages Images { get; set; }

    [DataMember(Name = "first_aired")]
    public string FirstAired { get; set; }

    [DataMember(Name = "airs")]
    public AirDate Airs { get; set; }

    [DataMember(Name = "runtime")]
    public int? Runtime { get; set; }

    [DataMember(Name = "certification")]
    public string Certification { get; set; }

    [DataMember(Name = "network")]
    public string Network { get; set; }

    [DataMember(Name = "country")]
    public string Country { get; set; }

    [DataMember(Name = "updated_at")]
    public string UpdatedAt { get; set; }

    [DataMember(Name = "overview")]
    public string Overview { get; set; }

    [DataMember(Name = "trailer")]
    public string Trailer { get; set; }

    [DataMember(Name = "homepage")]
    public string Homepage { get; set; }

    [DataMember(Name = "status")]
    public string Status { get; set; }

    [DataMember(Name = "rating")]
    public double? Rating { get; set; }

    [DataMember(Name = "votes")]
    public int Votes { get; set; }

    [DataMember(Name = "language")]
    public string Language { get; set; }

    [DataMember(Name = "available_translations")]
    public List<string> AvailableTranslations { get; set; }

    [DataMember(Name = "genres")]
    public List<string> Genres { get; set; }

    [DataMember(Name = "aired_episodes")]
    public int AiredEpisodes { get; set; }

    [DataContract]
    public class AirDate
    {
      [DataMember(Name = "day")]
      public string Day { get; set; }

      [DataMember(Name = "time")]
      public string Time { get; set; }

      [DataMember(Name = "timezone")]
      public string Timezone { get; set; }
    }
  }
}