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

namespace MediaPortal.Plugins.MP2Extended.ResourceAccess.MAS.TvShow
{
  [ApiFunctionDescription(Type = ApiFunctionDescription.FunctionType.Json, Summary = "")]
  [ApiFunctionParam(Name = "filter", Type = typeof(string), Nullable = true)]
  internal class GetTVShowCount
  {
    public WebIntResult Process(string filter)
    {
      ISet<Guid> necessaryMIATypes = new HashSet<Guid>();
      necessaryMIATypes.Add(MediaAspect.ASPECT_ID);
      necessaryMIATypes.Add(SeriesAspect.ASPECT_ID);

      IList<MediaItem> items = GetMediaItems.GetMediaItemsByAspect(necessaryMIATypes);

      if (items.Count == 0)
        throw new BadRequestException("No Tv Episodes found");

      var output = new List<WebTVShowBasic>();

      foreach (var item in items)
      {
        var seriesAspect = item[SeriesAspect.Metadata];
        WebTVShowBasic webTVShowBasic = new WebTVShowBasic();
        webTVShowBasic.Title = (string)seriesAspect[SeriesAspect.ATTR_SERIESNAME];

        output.Add(webTVShowBasic);
      }

      // Filter
      output = output.Filter(filter).ToList();

      return new WebIntResult { Result = output.Count };
    }

    internal static ILogger Logger
    {
      get { return ServiceRegistration.Get<ILogger>(); }
    }
  }
}