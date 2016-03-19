using System.Collections.Generic;

namespace MediaPortal.Extensions.OnlineLibraries.Libraries.Trakt.DataStructures
{
  public class TraktComments : TraktPagination
  {
    public IEnumerable<TraktCommentItem> Comments { get; set; }
  }
}