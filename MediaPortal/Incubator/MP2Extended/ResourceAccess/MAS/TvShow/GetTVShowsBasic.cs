using System;
using System.Collections.Generic;
using System.Linq;
using HttpServer;
using HttpServer.Exceptions;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Plugins.MP2Extended.Common;
using MediaPortal.Plugins.MP2Extended.Extensions;
using MediaPortal.Plugins.MP2Extended.MAS.TvShow;
using Newtonsoft.Json;

namespace MediaPortal.Plugins.MP2Extended.ResourceAccess.MAS.TvShow
{
  // Add more details
  internal class GetTVShowsBasic : IRequestMicroModuleHandler
  {
    public dynamic Process(IHttpRequest request)
    {
      ISet<Guid> necessaryMIATypes = new HashSet<Guid>();
      necessaryMIATypes.Add(MediaAspect.ASPECT_ID);
      necessaryMIATypes.Add(SeriesAspect.ASPECT_ID);

      IList<MediaItem> items = GetMediaItems.GetMediaItemsByAspect(necessaryMIATypes);

      if (items.Count == 0)
        throw new BadRequestException("GetTVShowsBasic: no Tv Episodes found");

      var output = new List<WebTVShowBasic>();

      foreach (var item in items)
      {
        SingleMediaItemAspect seriesAspect = MediaItemAspect.GetAspect(item.Aspects, SeriesAspect.Metadata);
        int index = output.FindIndex(x => x.Title == (string)seriesAspect[SeriesAspect.ATTR_SERIESNAME]);
        if (index == -1)
        {
          var episodesInThisShow = items.ToList().FindAll(x => (string)seriesAspect[SeriesAspect.ATTR_SERIESNAME] == (string)seriesAspect[SeriesAspect.ATTR_SERIESNAME]);
          var episodesInThisShowUnwatched = episodesInThisShow.FindAll(x => x.Aspects[MediaAspect.ASPECT_ID][MediaAspect.ATTR_PLAYCOUNT] == null || (int)x.Aspects[MediaAspect.ASPECT_ID][MediaAspect.ATTR_PLAYCOUNT] == 0);
          necessaryMIATypes = new HashSet<Guid>();
          necessaryMIATypes.Add(MediaAspect.ASPECT_ID);
          MediaItem show = GetMediaItems.GetMediaItemByName((string)seriesAspect[SeriesAspect.ATTR_SERIESNAME], necessaryMIATypes);

          if (show == null)
          {
            Logger.Warn("GetTVShowsBasic: Couldn't find show: {0}", (string)seriesAspect[SeriesAspect.ATTR_SERIESNAME]);
            continue;
          }

          WebTVShowBasic webTVShowBasic = new WebTVShowBasic();
          webTVShowBasic.Id = show.MediaItemId.ToString();
          webTVShowBasic.Title = (string)seriesAspect[SeriesAspect.ATTR_SERIESNAME];
          webTVShowBasic.EpisodeCount = episodesInThisShow.Count;
          webTVShowBasic.UnwatchedEpisodeCount = episodesInThisShowUnwatched.Count;

          output.Add(webTVShowBasic);
        }
      }

      // sort and filter
      HttpParam httpParam = request.Param;
      string sort = httpParam["sort"].Value;
      string order = httpParam["order"].Value;
      string filter = httpParam["filter"].Value;
      if (sort != null && order != null)
      {
        WebSortField webSortField = (WebSortField)JsonConvert.DeserializeObject(sort, typeof(WebSortField));
        WebSortOrder webSortOrder = (WebSortOrder)JsonConvert.DeserializeObject(order, typeof(WebSortOrder));

        output = output.Filter(filter).SortWebTVShowBasic(webSortField, webSortOrder).ToList();
      }
      else
        output = output.Filter(filter).ToList();

      return output;
    }

    internal static ILogger Logger
    {
      get { return ServiceRegistration.Get<ILogger>(); }
    }
  }
}