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