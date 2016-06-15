using System.Collections.Generic;
using System.Runtime.Serialization;

namespace MediaPortal.Extensions.OnlineLibraries.Libraries.Trakt.DataStructures
{
  [DataContract]
  public class TraktSyncEpisodes
  {
    [DataMember(Name = "episodes")]
    public List<TraktEpisode> Episodes { get; set; }
  }
}