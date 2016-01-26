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