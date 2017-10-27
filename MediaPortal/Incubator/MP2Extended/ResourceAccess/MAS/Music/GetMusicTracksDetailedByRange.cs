using MediaPortal.Plugins.MP2Extended.Common;
using MediaPortal.Plugins.MP2Extended.Extensions;
using MediaPortal.Plugins.MP2Extended.MAS.Music;
using System.Collections.Generic;
using System.Linq;

namespace MediaPortal.Plugins.MP2Extended.ResourceAccess.MAS.Music
{
  internal class GetMusicTracksDetailedByRange : GetMusicTracksDetailed
  {
    public IList<WebMusicTrackDetailed> Process(int start, int end, string filter, WebSortField? sort, WebSortOrder? order)
    {
      var output = Process(filter, sort, order);

      output = output.TakeRange(start, end).ToList();

      return output;
    }
  }
}
