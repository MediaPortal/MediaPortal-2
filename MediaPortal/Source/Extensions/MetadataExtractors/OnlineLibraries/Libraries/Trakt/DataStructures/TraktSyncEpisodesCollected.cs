using System.Collections.Generic;
using System.Runtime.Serialization;

namespace MediaPortal.Extensions.OnlineLibraries.Libraries.Trakt.DataStructures
{
  [DataContract]
  public class TraktSyncEpisodesCollected
  {
    [DataMember(Name = "episodes")]
    public List<TraktSyncEpisodeCollected> Episodes { get; set; }
  }
}