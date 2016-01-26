using System.Collections.Generic;
using System.Runtime.Serialization;

namespace MediaPortal.Extensions.OnlineLibraries.Libraries.Trakt.DataStructures
{
  [DataContract]
  public class TraktSyncEpisodesWatched
  {
    [DataMember(Name = "episodes")]
    public List<TraktSyncEpisodeWatched> Episodes { get; set; }
  }
}