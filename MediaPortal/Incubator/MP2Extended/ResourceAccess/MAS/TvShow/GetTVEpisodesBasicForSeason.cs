using System;
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
using MediaPortal.Plugins.MP2Extended.MAS;
using MediaPortal.Plugins.MP2Extended.MAS.TvShow;
using Newtonsoft.Json;

namespace MediaPortal.Plugins.MP2Extended.ResourceAccess.MAS.TvShow
{
  class GetTVEpisodesBasicForSeason : IRequestMicroModuleHandler
  {
    public dynamic Process(IHttpRequest request)
    {
      HttpParam httpParam = request.Param;
      string id = httpParam["id"].Value;
      if (id == null)
        throw new BadRequestException("GetTVEpisodeCountForSeason: no id is null");

      // The ID looks like: {GUID-TvSHow:Season}
      string[] ids = id.Split(':');
      if (ids.Length < 2)
        throw new BadRequestException(String.Format("GetTVEpisodeCountForSeason: not enough ids: {0}", ids.Length));

      string showId = ids[0];
      string seasonId = ids[1];

      ISet<Guid> necessaryMIATypes = new HashSet<Guid>();
      necessaryMIATypes.Add(MediaAspect.ASPECT_ID);

      // this is the MediaItem for the show
      MediaItem showItem = GetMediaItems.GetMediaItemById(showId, necessaryMIATypes);

      if (showItem == null)
        throw new BadRequestException(String.Format("GetTVEpisodeCountForSeason: No MediaItem found with id: {0}", showId));

      string showName;
      try
      {
        showName = (string)showItem[MediaAspect.ASPECT_ID][MediaAspect.ATTR_TITLE];
      }
      catch (Exception ex)
      {
        throw new BadRequestException(String.Format("GetTVEpisodeCountForSeason: Couldn't convert Title: {0}", ex.Message));
      }

      int seasonNumber;
      if (!Int32.TryParse(seasonId, out seasonNumber))
      {
        throw new BadRequestException(String.Format("GetTVEpisodeCountForSeason: Couldn't convert SeasonId to int: {0}", seasonId));
      }

      // Get all episodes for this
      ISet<Guid> necessaryMIATypesEpisodes = new HashSet<Guid>();
      necessaryMIATypesEpisodes.Add(MediaAspect.ASPECT_ID);
      necessaryMIATypesEpisodes.Add(SeriesAspect.ASPECT_ID);
      necessaryMIATypesEpisodes.Add(ImporterAspect.ASPECT_ID);

      IFilter searchFilter = BooleanCombinationFilter.CombineFilters(BooleanOperator.And,
          new RelationalFilter(SeriesAspect.ATTR_SEASON, RelationalOperator.EQ, seasonNumber),
          new RelationalFilter(SeriesAspect.ATTR_SERIESNAME, RelationalOperator.EQ, showName));
      MediaItemQuery searchQuery = new MediaItemQuery(necessaryMIATypesEpisodes, null, searchFilter);

      IList<MediaItem> episodes = ServiceRegistration.Get<IMediaLibrary>().Search(searchQuery, false);

      if (episodes.Count == 0)
        throw new BadRequestException("No Tv Episodes found");

      var output = new List<WebTVEpisodeBasic>();

      foreach (var item in episodes)
      {
        var seriesAspects = item.Aspects[SeriesAspect.ASPECT_ID];

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
