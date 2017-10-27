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
  internal class GetTVEpisodesBasicForTVShow : BaseEpisodeBasic
  {
    public IList<WebTVEpisodeBasic> Process(Guid id, WebSortField? sort, WebSortOrder? order)
    {
      IFilter searchFilter = new RelationshipFilter(EpisodeAspect.ROLE_EPISODE, SeriesAspect.ROLE_SERIES, id);
      IList<MediaItem> episodes = GetMediaItems.Search(BasicNecessaryMIATypeIds, BasicOptionalMIATypeIds, searchFilter);

      var output = new List<WebTVEpisodeBasic>();

      if (episodes.Count == 0)
        return output;

      output.AddRange(episodes.Select(episode => EpisodeBasic(episode, id)));

      // sort
      if (sort != null && order != null)
        output = output.SortWebTVEpisodeBasic(sort, order).ToList();

      return output;
    }

    internal static ILogger Logger
    {
      get { return ServiceRegistration.Get<ILogger>(); }
    }
  }
}
