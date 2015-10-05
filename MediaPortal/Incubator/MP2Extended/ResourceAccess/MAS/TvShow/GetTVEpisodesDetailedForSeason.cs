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
  class GetTVEpisodesDetailedForSeason : IRequestMicroModuleHandler
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
      necessaryMIATypesEpisodes.Add(VideoAspect.ASPECT_ID);
      necessaryMIATypesEpisodes.Add(SeriesAspect.ASPECT_ID);
      necessaryMIATypesEpisodes.Add(ImporterAspect.ASPECT_ID);

      IFilter searchFilter = BooleanCombinationFilter.CombineFilters(BooleanOperator.And,
          new RelationalFilter(SeriesAspect.ATTR_SEASON, RelationalOperator.EQ, seasonNumber),
          new RelationalFilter(SeriesAspect.ATTR_SERIESNAME, RelationalOperator.EQ, showName));
      MediaItemQuery searchQuery = new MediaItemQuery(necessaryMIATypesEpisodes, null, searchFilter);

      IList<MediaItem> episodes = ServiceRegistration.Get<IMediaLibrary>().Search(searchQuery, false);

      if (episodes.Count == 0)
        throw new BadRequestException("No Tv Episodes found");

      var output = new List<WebTVEpisodeDetailed>();

      foreach (var item in episodes)
      {
        MediaItemAspect seriesAspects = item.Aspects[SeriesAspect.ASPECT_ID];

        WebTVEpisodeDetailed webTvEpisodeDetailed = new WebTVEpisodeDetailed();
        var episodeNumber = ((HashSet<object>)item[SeriesAspect.ASPECT_ID][SeriesAspect.ATTR_EPISODE]).Cast<int>().ToList();
        webTvEpisodeDetailed.EpisodeNumber = episodeNumber[0];
        var TvDbId = seriesAspects[SeriesAspect.ATTR_TVDB_ID];
        if (TvDbId != null)
        {
          webTvEpisodeDetailed.ExternalId.Add(new WebExternalId
          {
            Site = "TVDB",
            Id = ((int)TvDbId).ToString()
          });
        }
        var ImdbId = seriesAspects[SeriesAspect.ATTR_TVDB_ID];
        if (ImdbId != null)
        {
          webTvEpisodeDetailed.ExternalId.Add(new WebExternalId
          {
            Site = "IMDB",
            Id = (string)seriesAspects[SeriesAspect.ATTR_IMDB_ID]
          });
        }

        var firstAired = seriesAspects[SeriesAspect.ATTR_FIRSTAIRED];
        if (firstAired != null)
          webTvEpisodeDetailed.FirstAired = (DateTime)seriesAspects[SeriesAspect.ATTR_FIRSTAIRED];
        webTvEpisodeDetailed.IsProtected = false; //??
        webTvEpisodeDetailed.Rating = Convert.ToSingle((double)seriesAspects[SeriesAspect.ATTR_TOTAL_RATING]);
        // TODO: Check => doesn't work
        MediaItem seasonItem = GetMediaItems.GetMediaItemByName((string)seriesAspects[SeriesAspect.ATTR_SERIES_SEASON], null);
        if (seasonItem != null)
          webTvEpisodeDetailed.SeasonId = seasonItem.MediaItemId.ToString();
        webTvEpisodeDetailed.SeasonNumber = (int)seriesAspects[SeriesAspect.ATTR_SEASON];
        webTvEpisodeDetailed.ShowId = showItem.MediaItemId.ToString();
        webTvEpisodeDetailed.Type = WebMediaType.TVEpisode;
        webTvEpisodeDetailed.Watched = ((int)(item.Aspects[MediaAspect.ASPECT_ID][MediaAspect.ATTR_PLAYCOUNT] ?? 0) > 0);
        //webTvEpisodeBasic.Path = ;
        //webTvEpisodeBasic.Artwork = ;
        webTvEpisodeDetailed.DateAdded = (DateTime)item.Aspects[ImporterAspect.ASPECT_ID][ImporterAspect.ATTR_DATEADDED];
        webTvEpisodeDetailed.Id = item.MediaItemId.ToString();
        webTvEpisodeDetailed.PID = 0;
        webTvEpisodeDetailed.Title = (string)item[MediaAspect.ASPECT_ID][MediaAspect.ATTR_TITLE];
        webTvEpisodeDetailed.Summary = (string)item[VideoAspect.ASPECT_ID][VideoAspect.ATTR_STORYPLOT];
        webTvEpisodeDetailed.Show = (string)item[SeriesAspect.ASPECT_ID][SeriesAspect.ATTR_SERIESNAME];
        var videoWriters = (HashSet<object>)item[VideoAspect.ASPECT_ID][VideoAspect.ATTR_WRITERS];
        if (videoWriters != null)
          webTvEpisodeDetailed.Writers = videoWriters.Cast<string>().ToList();
        var videoDirectors = (HashSet<object>)item[VideoAspect.ASPECT_ID][VideoAspect.ATTR_DIRECTORS];
        if (videoDirectors != null)
          webTvEpisodeDetailed.Directors = videoDirectors.Cast<string>().ToList();
        // webTVEpisodeDetailed.GuestStars not in the MP DB

        output.Add(webTvEpisodeDetailed);
      }

      // sort
      string sort = httpParam["sort"].Value;
      string order = httpParam["order"].Value;
      if (sort != null && order != null)
      {
        WebSortField webSortField = (WebSortField)JsonConvert.DeserializeObject(sort, typeof(WebSortField));
        WebSortOrder webSortOrder = (WebSortOrder)JsonConvert.DeserializeObject(order, typeof(WebSortOrder));

        output = output.SortWebTVEpisodeDetailed(webSortField, webSortOrder).ToList();
      }

      return output;
    }

    internal static ILogger Logger
    {
      get { return ServiceRegistration.Get<ILogger>(); }
    }
  }
}
