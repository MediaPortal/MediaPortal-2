using System.Runtime.Serialization;

namespace MediaPortal.Extensions.OnlineLibraries.Libraries.Trakt.DataStructures
{
  [DataContract]
  public class TraktMovie
  {
    [DataMember(Name = "title", EmitDefaultValue = false)]
    public string Title { get; set; }

    [DataMember(Name = "year", EmitDefaultValue = false)]
    public int? Year { get; set; }

    [DataMember(Name = "ids")]
    public TraktMovieId Ids { get; set; }
  }
}