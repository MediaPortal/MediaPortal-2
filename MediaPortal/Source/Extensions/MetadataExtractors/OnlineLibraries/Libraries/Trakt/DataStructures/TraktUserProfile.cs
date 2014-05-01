using System.Collections.Generic;
using System.Runtime.Serialization;

namespace MediaPortal.Extensions.OnlineLibraries.Libraries.Trakt.DataStructures
{
  [DataContract]
  public class TraktUserProfile : TraktUser
  {
    #region Statistics

    [DataMember(Name = "stats")]
    public Statistics Stats { get; set; }

    [DataContract]
    public class Statistics
    {
      [DataMember(Name = "friends")]
      public string Friends { get; set; }

      [DataMember(Name = "shows")]
      public ShowStats Shows { get; set; }

      [DataMember(Name = "episodes")]
      public EpisodeStats Episodes { get; set; }

      [DataMember(Name = "movies")]
      public MovieStats Movies { get; set; }

      [DataContract]
      public class ShowStats
      {
        [DataMember(Name = "library")]
        public string Library { get; set; }

        [DataMember(Name = "watched")]
        public string Watched { get; set; }

        [DataMember(Name = "collection")]
        public string Collection { get; set; }

        [DataMember(Name = "shouts")]
        public string Shouts { get; set; }

        [DataMember(Name = "loved")]
        public string Loved { get; set; }

        [DataMember(Name = "hated")]
        public string Hated { get; set; }
      }

      [DataContract]
      public class EpisodeStats
      {
        [DataMember(Name = "watched")]
        public string Watched { get; set; }

        [DataMember(Name = "watched_elsewhere")]
        public string WatchedElseWhere { get; set; }

        [DataMember(Name = "watched_trakt")]
        public string WatchedTrakt { get; set; }

        [DataMember(Name = "watched_trakt_unique")]
        public string WatchedTraktUnique { get; set; }

        [DataMember(Name = "watched_unique")]
        public string WatchedUnique { get; set; }

        [DataMember(Name = "scrobbles")]
        public string Scrobbles { get; set; }

        [DataMember(Name = "scrobbles_unique")]
        public string ScrobblesUnique { get; set; }

        [DataMember(Name = "checkins")]
        public string Checkins { get; set; }

        [DataMember(Name = "checkins_unique")]
        public string CheckinsUnique { get; set; }

        [DataMember(Name = "seen")]
        public string Seen { get; set; }

        [DataMember(Name = "unwatched")]
        public string UnWatched { get; set; }

        [DataMember(Name = "collection")]
        public string Collection { get; set; }

        [DataMember(Name = "shouts")]
        public string Shouts { get; set; }

        [DataMember(Name = "loved")]
        public string Loved { get; set; }

        [DataMember(Name = "hated")]
        public string Hated { get; set; }
      }

      [DataContract]
      public class MovieStats
      {
        [DataMember(Name = "watched")]
        public string Watched { get; set; }

        [DataMember(Name = "watched_elsewhere")]
        public string WatchedElseWhere { get; set; }

        [DataMember(Name = "watched_trakt")]
        public string WatchedTrakt { get; set; }

        [DataMember(Name = "watched_trakt_unique")]
        public string WatchedTraktUnique { get; set; }

        [DataMember(Name = "watched_unique")]
        public string WatchedUnique { get; set; }

        [DataMember(Name = "scrobbles")]
        public string Scrobbles { get; set; }

        [DataMember(Name = "scrobbles_unique")]
        public string ScrobblesUnique { get; set; }

        [DataMember(Name = "checkins")]
        public string Checkins { get; set; }

        [DataMember(Name = "checkins_unique")]
        public string CheckinsUnique { get; set; }

        [DataMember(Name = "seen")]
        public string Seen { get; set; }

        [DataMember(Name = "library")]
        public string Library { get; set; }

        [DataMember(Name = "unwatched")]
        public string UnWatched { get; set; }

        [DataMember(Name = "collection")]
        public string Collection { get; set; }

        [DataMember(Name = "shouts")]
        public string Shouts { get; set; }

        [DataMember(Name = "loved")]
        public string Loved { get; set; }

        [DataMember(Name = "hated")]
        public string Hated { get; set; }
      }
    }

    #endregion

    #region Watch Item

    [DataMember(Name = "watching")]
    public WatchItem Watching { get; set; }

    [DataMember(Name = "watched")]
    public List<WatchItem> WatchedHistory { get; set; }

    [DataMember(Name = "watched_episodes")]
    public List<WatchItem> WatchedEpisodes { get; set; }

    [DataMember(Name = "watched_movies")]
    public List<WatchItem> WatchedMovies { get; set; }

    [DataContract]
    public class WatchItem
    {
      [DataMember(Name = "episode")]
      public TraktEpisode Episode { get; set; }

      [DataMember(Name = "show")]
      public TraktShow Show { get; set; }

      [DataMember(Name = "movie")]
      public TraktMovie Movie { get; set; }

      [DataMember(Name = "type")]
      public string Type { get; set; }

      [DataMember(Name = "watched")]
      public long WatchedDate { get; set; }

      public override string ToString()
      {
        return Type == "episode" ? string.Format("{0} - {1}x{2}{3}", Show.Title, Episode.Season, Episode.Number, string.IsNullOrEmpty(Episode.Title) ? string.Empty : " - " + Episode.Title) : Movie.Title;
      }
    }

    #endregion
  }
}