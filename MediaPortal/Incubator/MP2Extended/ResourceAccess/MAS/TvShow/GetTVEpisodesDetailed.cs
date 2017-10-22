using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Plugins.MP2Extended.Attributes;
using MediaPortal.Plugins.MP2Extended.Common;
using MediaPortal.Plugins.MP2Extended.Exceptions;
using MediaPortal.Plugins.MP2Extended.Extensions;
using MediaPortal.Plugins.MP2Extended.MAS.TvShow;
using MediaPortal.Plugins.MP2Extended.ResourceAccess.MAS.TvShow.BaseClasses;
using System.Collections.Generic;
using System.Linq;

namespace MediaPortal.Plugins.MP2Extended.ResourceAccess.MAS.TvShow
{
  [ApiFunctionDescription(Type = ApiFunctionDescription.FunctionType.Json, Summary = "")]
  [ApiFunctionParam(Name = "sort", Type = typeof(WebSortField), Nullable = true)]
  [ApiFunctionParam(Name = "order", Type = typeof(WebSortOrder), Nullable = true)]
  internal class GetTVEpisodesDetailed : BaseEpisodeDetailed
  {
    public IList<WebTVEpisodeDetailed> Process(string filter, WebSortField? sort, WebSortOrder? order)
    {
      IList<MediaItem> items = GetMediaItems.GetMediaItemsByAspect(DetailedNecessaryMIATypeIds, DetailedOptionalMIATypeIds, null);

      if (items.Count == 0)
        throw new BadRequestException("No Tv Episodes found");

      var output = items.Select(item => EpisodeDetailed(item))
        .Filter(filter);

      // sort
      if (sort != null && order != null)
        output = output.SortWebTVEpisodeBasic(sort, order);

      return output.ToList();
    }

    internal static ILogger Logger
    {
      get { return ServiceRegistration.Get<ILogger>(); }
    }
  }
}
