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
  public class TraktLastSyncActivities
  {
    [DataMember(Name = "all")]
    public string All { get; set; }

    [DataMember(Name = "movies")]
    public MovieActivities Movies { get; set; }

    [DataContract]
    public class MovieActivities
    {
      [DataMember(Name = "watched_at")]
      public string Watched { get; set; }

      [DataMember(Name = "collected_at")]
      public string Collection { get; set; }

      [DataMember(Name = "rated_at")]
      public string Rating { get; set; }

      [DataMember(Name = "watchlisted_at")]
      public string Watchlist { get; set; }

      [DataMember(Name = "commented_at")]
      public string Comment { get; set; }

      [DataMember(Name = "paused_at")]
      public string PausedAt { get; set; }
    }

    [DataMember(Name = "episodes")]
    public EpisodeActivities Episodes { get; set; }

    [DataContract]
    public class EpisodeActivities
    {
      [DataMember(Name = "watched_at")]
      public string Watched { get; set; }

      [DataMember(Name = "collected_at")]
      public string Collection { get; set; }

      [DataMember(Name = "rated_at")]
      public string Rating { get; set; }

      [DataMember(Name = "watchlisted_at")]
      public string Watchlist { get; set; }

      [DataMember(Name = "commented_at")]
      public string Comment { get; set; }

      [DataMember(Name = "paused_at")]
      public string PausedAt { get; set; }
    }

    [DataMember(Name = "shows")]
    public ShowActivities Shows { get; set; }

    [DataContract]
    public class ShowActivities
    {
      [DataMember(Name = "rated_at")]
      public string Rating { get; set; }

      [DataMember(Name = "watchlisted_at")]
      public string Watchlist { get; set; }

      [DataMember(Name = "commented_at")]
      public string Comment { get; set; }
    }

    [DataMember(Name = "seasons")]
    public SeasonActivities Seasons { get; set; }

    [DataContract]
    public class SeasonActivities
    {
      [DataMember(Name = "rated_at")]
      public string Rating { get; set; }

      [DataMember(Name = "watchlisted_at")]
      public string Watchlist { get; set; }

      [DataMember(Name = "commented_at")]
      public string Comment { get; set; }
    }

    [DataMember(Name = "comments")]
    public CommentActivities Comments { get; set; }

    [DataContract]
    public class CommentActivities
    {
      [DataMember(Name = "liked_at")]
      public string LikedAt { get; set; }
    }

    [DataMember(Name = "lists")]
    public ListActivities Lists { get; set; }

    [DataContract]
    public class ListActivities
    {
      [DataMember(Name = "liked_at")]
      public string LikedAt { get; set; }

      [DataMember(Name = "updated_at")]
      public string UpdatedAt { get; set; }

      [DataMember(Name = "commented_at")]
      public string Comment { get; set; }
    }
  }
}