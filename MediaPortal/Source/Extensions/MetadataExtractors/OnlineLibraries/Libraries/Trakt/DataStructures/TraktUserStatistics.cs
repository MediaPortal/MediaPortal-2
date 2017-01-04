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
  public class TraktUserStatistics
  {
    [DataMember(Name = "movies")]
    public MovieStats Movies { get; set; }

    [DataMember(Name = "shows")]
    public ShowStats Shows { get; set; }

    [DataMember(Name = "seasons")]
    public SeasonStats Seasons { get; set; }

    [DataMember(Name = "episodes")]
    public EpisodeStats Episodes { get; set; }

    [DataMember(Name = "network")]
    public NetworkStats Network { get; set; }

    [DataMember(Name = "ratings")]
    public RatingStats Ratings { get; set; }


    [DataContract]
    public class MovieStats
    {
      [DataMember(Name = "plays")]
      public int Plays { get; set; }

      [DataMember(Name = "collected")]
      public int Collected { get; set; }

      [DataMember(Name = "watched")]
      public int Watched { get; set; }

      [DataMember(Name = "ratings")]
      public int Ratings { get; set; }

      [DataMember(Name = "comments")]
      public int Comments { get; set; }
    }

    [DataContract]
    public class ShowStats
    {
      [DataMember(Name = "collected")]
      public int Collected { get; set; }

      [DataMember(Name = "watched")]
      public int Watched { get; set; }

      [DataMember(Name = "ratings")]
      public int Ratings { get; set; }

      [DataMember(Name = "comments")]
      public int Comments { get; set; }
    }

    [DataContract]
    public class SeasonStats
    {
      [DataMember(Name = "ratings")]
      public int Ratings { get; set; }

      [DataMember(Name = "comments")]
      public int Comments { get; set; }
    }

    [DataContract]
    public class EpisodeStats
    {
      [DataMember(Name = "plays")]
      public int Plays { get; set; }

      [DataMember(Name = "collected")]
      public int Collected { get; set; }

      [DataMember(Name = "watched")]
      public int Watched { get; set; }

      [DataMember(Name = "ratings")]
      public int Ratings { get; set; }

      [DataMember(Name = "comments")]
      public int Comments { get; set; }
    }

    [DataContract]
    public class NetworkStats
    {
      [DataMember(Name = "friends")]
      public int Friends { get; set; }

      [DataMember(Name = "followers")]
      public int Followers { get; set; }

      [DataMember(Name = "following")]
      public int Following { get; set; }
    }

    [DataContract]
    public class RatingStats
    {
      [DataMember(Name = "total")]
      public int Total { get; set; }

      [DataMember(Name = "distribution")]
      public TraktRatingDistribution Distribution { get; set; }
    }
  }
}