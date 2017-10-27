using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Common.MediaManagement.MLQueries;
using MediaPortal.Plugins.MP2Extended.Common;
using MediaPortal.Plugins.MP2Extended.MAS.TvShow;
using MediaPortal.Plugins.MP2Extended.ResourceAccess.MAS.TvShow.BaseClasses;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MediaPortal.Plugins.MP2Extended.ResourceAccess.MAS.TvShow
{
  internal class GetTVEpisodesDetailedForTVShow : BaseEpisodeDetailed
  {
    public IList<WebTVEpisodeDetailed> Process(Guid id, WebSortField? sort, WebSortOrder? order)
    {
      IFilter searchFilter = new RelationshipFilter(EpisodeAspect.ROLE_EPISODE, SeriesAspect.ROLE_SERIES, id);
      IList<MediaItem> episodes = GetMediaItems.Search(DetailedNecessaryMIATypeIds, DetailedOptionalMIATypeIds, searchFilter);

      var output = new List<WebTVEpisodeDetailed>();

      if (episodes.Count == 0)
        return output;

      output.AddRange(episodes.Select(episode => EpisodeDetailed(episode, id)));

      // sort
      if (sort != null && order != null)
        output = output.SortWebTVEpisodeDetailed(sort, order).ToList();

      return output;
    }

    internal static ILogger Logger
    {
      get { return ServiceRegistration.Get<ILogger>(); }
    }
  }
}
