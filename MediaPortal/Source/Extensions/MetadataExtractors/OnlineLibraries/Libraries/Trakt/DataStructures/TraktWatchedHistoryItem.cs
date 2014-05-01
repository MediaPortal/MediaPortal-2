using System.Runtime.Serialization;

namespace MediaPortal.Extensions.OnlineLibraries.Libraries.Trakt.DataStructures
{
  [DataContract]
  public class TraktWatchedEpisode
  {
    [DataMember(Name = "episode")]
    public TraktEpisode Episode { get; set; }

    [DataMember(Name = "show")]
    public TraktShow Show { get; set; }

    [DataMember(Name = "watched")]
    public long WatchedDate { get; set; }

    public override string ToString()
    {
      return string.Format("{0} - {1}x{2}{3}", Show.Title, Episode.Season, Episode.Number, string.IsNullOrEmpty(Episode.Title) ? string.Empty : " - " + Episode.Title);
    }
  }

  [DataContract]
  public class TraktWatchedMovie
  {
    [DataMember(Name = "movie")]
    public TraktMovie Movie { get; set; }

    [DataMember(Name = "watched")]
    public long WatchedDate { get; set; }

    public override string ToString()
    {
      return Movie.Title;
    }
  }
}
