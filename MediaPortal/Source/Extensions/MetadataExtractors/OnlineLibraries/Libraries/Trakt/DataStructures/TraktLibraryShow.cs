using System.Collections.Generic;
using System.Runtime.Serialization;

namespace MediaPortal.Extensions.OnlineLibraries.Libraries.Trakt.DataStructures
{
  [DataContract]
  public class TraktLibraryShow : TraktResponse
  {
    [DataMember(Name = "title")]
    public string Title { get; set; }

    [DataMember(Name = "tvdb_id")]
    public string SeriesId { get; set; }

    [DataMember(Name = "seasons")]
    public List<Seasons> Seasons { get; set; }

    public override string ToString()
    {
      return this.Title;
    }
  }

  [DataContract]
  public class Seasons
  {
    [DataMember(Name = "season")]
    public int Season { get; set; }

    [DataMember(Name = "episodes")]
    public List<int> Episodes { get; set; }
  }
}
