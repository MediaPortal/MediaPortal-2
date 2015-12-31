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
using Newtonsoft.Json;

namespace MediaPortal.Plugins.MP2Extended.ResourceAccess.MAS.TvShow
{
  // This is a work around -> wait for MIA rework
  // TODO: Add more details
  [ApiFunctionDescription(Type = ApiFunctionDescription.FunctionType.Json, Summary = "")]
  [ApiFunctionParam(Name = "sort", Type = typeof(WebSortField), Nullable = true)]
  [ApiFunctionParam(Name = "order", Type = typeof(WebSortOrder), Nullable = true)]
  [ApiFunctionParam(Name = "filter", Type = typeof(string), Nullable = true)]
  internal class GetTVShowsBasic
  {
    public IList<WebTVShowBasic> Process(string filter, WebSortField? sort, WebSortOrder? order)
    {
      // we can't select only for shows, so we take all episodes and filter the shows.
      ISet<Guid> necessaryMIATypes = new HashSet<Guid>();
      necessaryMIATypes.Add(MediaAspect.ASPECT_ID);
      necessaryMIATypes.Add(ProviderResourceAspect.ASPECT_ID);
      necessaryMIATypes.Add(ImporterAspect.ASPECT_ID);
      necessaryMIATypes.Add(SeriesAspect.ASPECT_ID);

      IList<MediaItem> items = GetMediaItems.GetMediaItemsByAspect(necessaryMIATypes);

      if (items.Count == 0)
        throw new BadRequestException("GetTVShowsBasic: no Tv Episodes found");

      var output = new List<WebTVShowBasic>();

      foreach (var item in items)
      {
        var seriesAspect = item[SeriesAspect.Metadata];
        int index = output.FindIndex(x => x.Title == (string)seriesAspect[SeriesAspect.ATTR_SERIESNAME]);
        if (index == -1)
        {
          var episodesInThisShow = items.ToList().FindAll(x => (string)x[SeriesAspect.Metadata][SeriesAspect.ATTR_SERIESNAME] == (string)seriesAspect[SeriesAspect.ATTR_SERIESNAME]);
          var episodesInThisShowUnwatched = episodesInThisShow.FindAll(x => x[MediaAspect.Metadata][MediaAspect.ATTR_PLAYCOUNT] == null || (int)x[MediaAspect.Metadata][MediaAspect.ATTR_PLAYCOUNT] == 0);
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
      if (sort != null && order != null)
      {
        output = output.Filter(filter).SortWebTVShowBasic(sort, order).ToList();
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