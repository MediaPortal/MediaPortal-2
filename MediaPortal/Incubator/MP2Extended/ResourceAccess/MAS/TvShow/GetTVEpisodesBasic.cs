using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HttpServer;
using HttpServer.Exceptions;
using MediaPortal.Backend.MediaLibrary;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Common.MediaManagement.MLQueries;
using MediaPortal.Plugins.MP2Extended.Common;
using MediaPortal.Plugins.MP2Extended.Extensions;
using MediaPortal.Plugins.MP2Extended.MAS;
using MediaPortal.Plugins.MP2Extended.MAS.TvShow;
using Newtonsoft.Json;

namespace MediaPortal.Plugins.MP2Extended.ResourceAccess.MAS.TvShow
{
  class GetTVEpisodesBasic : IRequestMicroModuleHandler
  {
    public dynamic Process(IHttpRequest request)
    {
      ISet<Guid> necessaryMIATypes = new HashSet<Guid>();
      necessaryMIATypes.Add(MediaAspect.ASPECT_ID);
      necessaryMIATypes.Add(ProviderResourceAspect.ASPECT_ID);
      necessaryMIATypes.Add(ImporterAspect.ASPECT_ID);
      necessaryMIATypes.Add(SeriesAspect.ASPECT_ID);

      IList<MediaItem> items = GetMediaItems.GetMediaItemsByAspect(necessaryMIATypes);

      if (items.Count == 0)
        throw new BadRequestException("No Tv Episodes found");

      var output = new List<WebTVEpisodeBasic>();

      foreach (var item in items)
      {

        MediaItemAspect seriesAspects = item.Aspects[SeriesAspect.ASPECT_ID];

        WebTVEpisodeBasic webTvEpisodeBasic = new WebTVEpisodeBasic();
        var episodeNumber = ((HashSet<object>)item[SeriesAspect.ASPECT_ID][SeriesAspect.ATTR_EPISODE]).Cast<int>().ToList();
        webTvEpisodeBasic.EpisodeNumber = episodeNumber[0];
        webTvEpisodeBasic.ExternalId = new List<WebExternalId>();
        var TvDbId = seriesAspects[SeriesAspect.ATTR_TVDB_ID];
        if (TvDbId != null)
        {
          webTvEpisodeBasic.ExternalId.Add(new WebExternalId
          {
            Site = "TVDB",
            Id = ((int)TvDbId).ToString()
          });
        }
        var ImdbId = seriesAspects[SeriesAspect.ATTR_TVDB_ID];
        if (ImdbId != null)
        {
          webTvEpisodeBasic.ExternalId.Add(new WebExternalId
          {
            Site = "IMDB",
            Id = (string)seriesAspects[SeriesAspect.ATTR_IMDB_ID]
          });
        }


        var firstAired = seriesAspects[SeriesAspect.ATTR_FIRSTAIRED];
        if (firstAired != null)
          webTvEpisodeBasic.FirstAired = (DateTime)seriesAspects[SeriesAspect.ATTR_FIRSTAIRED];
        webTvEpisodeBasic.IsProtected = false; //??
        webTvEpisodeBasic.Rating = Convert.ToSingle((double)seriesAspects[SeriesAspect.ATTR_TOTAL_RATING]);
        // TODO: Check => doesn't work
        MediaItem seasonItem = GetMediaItems.GetMediaItemByName((string)seriesAspects[SeriesAspect.ATTR_SERIES_SEASON], null);
        if (seasonItem != null)
          webTvEpisodeBasic.SeasonId = seasonItem.MediaItemId.ToString();
        webTvEpisodeBasic.SeasonNumber = (int)seriesAspects[SeriesAspect.ATTR_SEASON];
        MediaItem showItem = GetMediaItems.GetMediaItemByName((string)seriesAspects[SeriesAspect.ATTR_SERIESNAME], null);
        if (showItem != null)
          webTvEpisodeBasic.ShowId = showItem.MediaItemId.ToString();
        webTvEpisodeBasic.Type = WebMediaType.TVEpisode;
        webTvEpisodeBasic.Watched = ((int)(item.Aspects[MediaAspect.ASPECT_ID][MediaAspect.ATTR_PLAYCOUNT] ?? 0) > 0);
        //webTvEpisodeBasic.Path = ;
        //webTvEpisodeBasic.Artwork = ;
        webTvEpisodeBasic.DateAdded = (DateTime)item.Aspects[ImporterAspect.ASPECT_ID][ImporterAspect.ATTR_DATEADDED];
        webTvEpisodeBasic.Id = item.MediaItemId.ToString();
        webTvEpisodeBasic.PID = 0;
        webTvEpisodeBasic.Title = (string)item[MediaAspect.ASPECT_ID][MediaAspect.ATTR_TITLE];

        output.Add(webTvEpisodeBasic);
      }

      // sort
      HttpParam httpParam = request.Param;
      string sort = httpParam["sort"].Value;
      string order = httpParam["order"].Value;
      if (sort != null && order != null)
      {
        WebSortField webSortField = (WebSortField)JsonConvert.DeserializeObject(sort, typeof(WebSortField));
        WebSortOrder webSortOrder = (WebSortOrder)JsonConvert.DeserializeObject(order, typeof(WebSortOrder));

        output = output.SortWebTVEpisodeBasic(webSortField, webSortOrder).ToList();
      }

      return output;
    }

    internal static ILogger Logger
    {
      get { return ServiceRegistration.Get<ILogger>(); }
    }
  }
}
