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