using System;
using System.Runtime.Serialization;

namespace MediaPortal.Extensions.OnlineLibraries.Libraries.Trakt.DataStructures
{
  [DataContract]
  public class TraktCommentItem : IEquatable<TraktCommentItem>
  {
    [DataMember(Name = "type")]
    public string Type { get; set; }

    [DataMember(Name = "movie")]
    public TraktMovieSummary Movie { get; set; }

    [DataMember(Name = "show")]
    public TraktShowSummary Show { get; set; }

    [DataMember(Name = "season")]
    public TraktSeasonSummary Season { get; set; }

    [DataMember(Name = "episode")]
    public TraktEpisodeSummary Episode { get; set; }

    [DataMember(Name = "list")]
    public TraktListDetail List { get; set; }

    [DataMember(Name = "comment")]
    public TraktComment Comment { get; set; }

    #region IEquatable

    public bool Equals(TraktCommentItem other)
    {
      if (other == null || other.Comment == null)
        return false;

      return (this.Comment.Id == other.Comment.Id && this.Type == other.Type);
    }

    public override int GetHashCode()
    {
      return (this.Comment.Id.ToString() + "_" + this.Type).GetHashCode();
    }

    #endregion
  }
}