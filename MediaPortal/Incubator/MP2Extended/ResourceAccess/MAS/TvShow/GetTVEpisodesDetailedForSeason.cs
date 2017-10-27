using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Common.MediaManagement.MLQueries;
using MediaPortal.Plugins.MP2Extended.Attributes;
using MediaPortal.Plugins.MP2Extended.Common;
using MediaPortal.Plugins.MP2Extended.Exceptions;
using MediaPortal.Plugins.MP2Extended.MAS.TvShow;
using MediaPortal.Plugins.MP2Extended.ResourceAccess.MAS.TvShow.BaseClasses;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MediaPortal.Plugins.MP2Extended.ResourceAccess.MAS.TvShow
{
  [ApiFunctionDescription(Type = ApiFunctionDescription.FunctionType.Json, Summary = "")]
  [ApiFunctionParam(Name = "id", Type = typeof(string), Nullable = false)]
  [ApiFunctionParam(Name = "sort", Type = typeof(WebSortField), Nullable = true)]
  [ApiFunctionParam(Name = "order", Type = typeof(WebSortOrder), Nullable = true)]
  internal class GetTVEpisodesDetailedForSeason : BaseEpisodeDetailed
  {
    public IList<WebTVEpisodeDetailed> Process(Guid id, WebSortField? sort, WebSortOrder? order)
    {
      // Get all seasons for this series
      IFilter searchFilter = new RelationshipFilter(EpisodeAspect.ROLE_EPISODE, SeasonAspect.ROLE_SEASON, id);
      IList<MediaItem> episodes = GetMediaItems.Search(DetailedNecessaryMIATypeIds, DetailedOptionalMIATypeIds, searchFilter);

      if (episodes.Count == 0)
        throw new BadRequestException("No Tv Episodes found");

      var output = episodes.Select(episode => EpisodeDetailed(episode, null, id));

      // sort
      if (sort != null && order != null)
        output = output.SortWebTVEpisodeDetailed(sort, order);

      return output.ToList();
    }

    internal static ILogger Logger
    {
      get { return ServiceRegistration.Get<ILogger>(); }
    }
  }
}
