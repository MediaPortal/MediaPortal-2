using System;
using System.Collections.Generic;
using HttpServer;
using HttpServer.Exceptions;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Plugins.MP2Extended.Common;
using MediaPortal.Plugins.MP2Extended.MAS.TvShow;

namespace MediaPortal.Plugins.MP2Extended.ResourceAccess.MAS.TvShow
{
  // TODO: Add filter
  internal class GetTVShowCount : IRequestMicroModuleHandler
  {
    public dynamic Process(IHttpRequest request)
    {
      // we can't select only for shows, so we take all episodes and filter the shows.
      ISet<Guid> necessaryMIATypes = new HashSet<Guid>();
      necessaryMIATypes.Add(MediaAspect.ASPECT_ID);
      necessaryMIATypes.Add(ProviderResourceAspect.ASPECT_ID);
      necessaryMIATypes.Add(ImporterAspect.ASPECT_ID);
      necessaryMIATypes.Add(SeriesAspect.ASPECT_ID);

      IList<MediaItem> items = GetMediaItems.GetMediaItemsByAspect(necessaryMIATypes);

      if (items.Count == 0)
        throw new BadRequestException("No Tv Episodes found");

      var output = new List<WebTVShowBasic>();

      foreach (var item in items)
      {
        var seriesAspect = item.Aspects[SeriesAspect.ASPECT_ID];
        int index = output.FindIndex(x => x.Title == (string)seriesAspect[SeriesAspect.ATTR_SERIESNAME]);
        if (index == -1)
        {
          WebTVShowBasic webTVShowBasic = new WebTVShowBasic();
          webTVShowBasic.Title = (string)seriesAspect[SeriesAspect.ATTR_SERIESNAME];

          output.Add(webTVShowBasic);
        }
      }


      return new WebIntResult { Result = output.Count };
    }

    internal static ILogger Logger
    {
      get { return ServiceRegistration.Get<ILogger>(); }
    }
  }
}