///
/// Added by ric@rts.co.at
/// 

using System.Collections.Generic;
using System.Runtime.Serialization;

namespace TvMosaic.API
{
  [DataContract(Name = "playlist_request", Namespace = "")]
  public class PlayListRequest
  {
  }

  public class PlayListMember
  {
    public string Name { get; set; }
    public int Number { get; set; }
    public string Url { get; set; }
  }

  public class PlayList : List<PlayListMember>
  {
  }
}
