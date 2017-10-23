using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Plugins.MP2Extended.Common;
using MediaPortal.Plugins.MP2Extended.Extensions;
using MediaPortal.Plugins.MP2Extended.MAS.TvShow;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MediaPortal.Plugins.MP2Extended.ResourceAccess.MAS.TvShow
{
  internal class GetTVEpisodesDetailedForTVShowByRange : GetTVEpisodesDetailedForTVShow
  {
    public IList<WebTVEpisodeDetailed> Process(Guid id, int start, int end, string filter, WebSortField? sort, WebSortOrder? order)
    {
      var output = Process(id, sort, order)
        .Filter(filter);

      // get range
      output = output.TakeRange(start, end);

      return output.ToList();
    }
  }
}
