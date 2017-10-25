using System;
using System.Collections.Generic;
using System.Linq;
using HttpServer;
using HttpServer.Sessions;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Plugins.MP2Extended.Attributes;
using MediaPortal.Plugins.MP2Extended.Common;
using MediaPortal.Plugins.MP2Extended.Exceptions;
using MediaPortal.Plugins.MP2Extended.Extensions;
using MediaPortal.Plugins.MP2Extended.MAS.TvShow;
using MediaPortal.Plugins.MP2Extended.ResourceAccess.MAS.TvShow.BaseClasses;
using Newtonsoft.Json;

namespace MediaPortal.Plugins.MP2Extended.ResourceAccess.MAS.TvShow
{
  internal class GetTVShowsDetailed : BaseTvShowDetailed
  {
    public IList<WebTVShowDetailed> Process(string filter, WebSortField? sort, WebSortOrder? order)
    {
      IList<MediaItem> items = GetMediaItems.GetMediaItemsByAspect(BasicNecessaryMIATypeIds, BasicOptionalMIATypeIds, null);

      if (items.Count == 0)
        throw new BadRequestException("GetTVShowsBasic: no Tv Episodes found");

      var output = items.Select(item => TVShowDetailed(item))
        .Filter(filter);
      
      // sort and filter
      if (sort != null && order != null)
        output = output.SortWebTVShowBasic(sort, order);

      return output.ToList();
    }

    internal static ILogger Logger
    {
      get { return ServiceRegistration.Get<ILogger>(); }
    }
  }
}
