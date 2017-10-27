using MediaPortal.Plugins.MP2Extended.Common;
using MediaPortal.Plugins.MP2Extended.Extensions;
using MediaPortal.Plugins.MP2Extended.MAS;
using System.Collections.Generic;
using System.Linq;

namespace MediaPortal.Plugins.MP2Extended.ResourceAccess.MAS.Music
{
  internal class GetMusicGenresByRange : GetMusicGenres
  {
    public IList<WebGenre> Process(int start, int end, string filter, WebSortField? sort, WebSortOrder? order)
    {
      var output = Process(sort, order)
        .Filter(filter)
        .TakeRange(start, end);

      return output.ToList();
    }
  }
}
