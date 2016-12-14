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

namespace MediaPortal.Extensions.OnlineLibraries.Libraries.Trakt.DataStructures
{
  [DataContract]
  public class TraktRating
  {
    [DataMember(Name = "rating")]
    public double? Rating { get; set; }

    [DataMember(Name = "votes")]
    public int? Votes { get; set; }

    [DataMember(Name = "distribution")]
    public RatingsDistribution Distribution { get; set; }

    [DataContract]
    public class RatingsDistribution
    {
      [DataMember(Name = "1")]
      public int One { get; set; }

      [DataMember(Name = "2")]
      public int Two { get; set; }

      [DataMember(Name = "3")]
      public int Three { get; set; }

      [DataMember(Name = "4")]
      public int Four { get; set; }

      [DataMember(Name = "5")]
      public int Five { get; set; }

      [DataMember(Name = "6")]
      public int Six { get; set; }

      [DataMember(Name = "7")]
      public int Seven { get; set; }

      [DataMember(Name = "8")]
      public int Eight { get; set; }

      [DataMember(Name = "9")]
      public int Nine { get; set; }

      [DataMember(Name = "10")]
      public int Ten { get; set; }
    }
  }
}