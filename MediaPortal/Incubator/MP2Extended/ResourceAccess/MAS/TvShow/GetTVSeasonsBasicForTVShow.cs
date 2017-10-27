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
  // TODO: Add more details
  [ApiFunctionDescription(Type = ApiFunctionDescription.FunctionType.Json, Summary = "")]
  [ApiFunctionParam(Name = "id", Type = typeof(Guid), Nullable = false)]
  [ApiFunctionParam(Name = "sort", Type = typeof(WebSortField), Nullable = true)]
  [ApiFunctionParam(Name = "order", Type = typeof(WebSortOrder), Nullable = true)]
  internal class GetTVSeasonsBasicForTVShow : BaseTvSeasonBasic
  {
    public IList<WebTVSeasonBasic> Process(Guid id, string filter, WebSortField? sort, WebSortOrder? order)
    {
      // Get all seasons for this series
      IFilter searchFilter = new RelationshipFilter(SeasonAspect.ROLE_SEASON, SeriesAspect.ROLE_SERIES, id);
      IList<MediaItem> seasons = GetMediaItems.Search(BasicNecessaryMIATypeIds, BasicOptionalMIATypeIds, searchFilter);

      if (seasons.Count == 0)
        throw new BadRequestException("No seasons found");

      var output = new List<WebTVSeasonBasic>();

      foreach (var season in seasons)
        output.Add(TVSeasonBasic(season, id));

      // sort
      if (sort != null && order != null)
        output = output.SortWebTVSeasonBasic(sort, order).ToList();

      return output;
    }

    internal static ILogger Logger
    {
      get { return ServiceRegistration.Get<ILogger>(); }
    }
  }
}
