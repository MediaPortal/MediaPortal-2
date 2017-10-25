using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Plugins.MP2Extended.Common;
using MediaPortal.Plugins.MP2Extended.Extensions;
using MediaPortal.Plugins.MP2Extended.MAS.TvShow;
using System.Collections.Generic;
using System.Linq;

namespace MediaPortal.Plugins.MP2Extended.ResourceAccess.MAS.TvShow
{
  internal class GetTVEpisodesBasicByRange : GetTVEpisodesBasic
  {
    public IList<WebTVEpisodeBasic> Process(int start, int end, WebSortField? sort, WebSortOrder? order)
    {
      var output = Process(sort, order);

      // get range
      output = output.TakeRange(start, end).ToList();
      return output;
    }
  }
}
