using System.Collections.Generic;

namespace MediaPortal.Extensions.OnlineLibraries.Libraries.Trakt.DataStructures
{
  public class TraktLikes : TraktPagination
  {
    public IEnumerable<TraktLike> Likes { get; set; }
  }
}