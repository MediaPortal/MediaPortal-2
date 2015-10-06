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
  class GetTVEpisodesDetailedByRange : IRequestMicroModuleHandler
  {
    public dynamic Process(IHttpRequest request)
    {
      HttpParam httpParam = request.Param;
      string start = httpParam["start"].Value;
      string end = httpParam["end"].Value;

      Logger.Info("GetTVShowsBasicByRange: start: {0}, end: {1}", start, end);

      if (start == null || end == null)
        throw new BadRequestException("start or end parameter is missing");

      int startInt;
      if (!Int32.TryParse(start, out startInt))
      {
        throw new BadRequestException(String.Format("GetTVShowsBasicByRange: Couldn't convert start to int: {0}", start));
      }

      int endInt;
      if (!Int32.TryParse(end, out endInt))
      {
        throw new BadRequestException(String.Format("GetTVShowsBasicByRange: Couldn't convert end to int: {0}", end));
      }
      
      ISet<Guid> necessaryMIATypes = new HashSet<Guid>();
      necessaryMIATypes.Add(MediaAspect.ASPECT_ID);
      necessaryMIATypes.Add(ProviderResourceAspect.ASPECT_ID);
      necessaryMIATypes.Add(ImporterAspect.ASPECT_ID);
      necessaryMIATypes.Add(VideoAspect.ASPECT_ID);
      necessaryMIATypes.Add(SeriesAspect.ASPECT_ID);

      IList<MediaItem> items = GetMediaItems.GetMediaItemsByAspect(necessaryMIATypes);

      if (items.Count == 0)
        throw new BadRequestException("No Tv Episodes found");

      var output = new List<WebTVEpisodeDetailed>();

      foreach (var item in items)
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
        MediaItem showItem = GetMediaItems.GetMediaItemByName((string)seriesAspects[SeriesAspect.ATTR_SERIESNAME], null);
        if (showItem != null)
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

      // get range
      if (startInt > output.Count - 1)
        startInt = output.Count - 1;

      if (endInt > output.Count - 1)
        endInt = output.Count - 1;

      if ((endInt - startInt) < 0)
        throw new BadRequestException(String.Format("Invalid range: {0}", (endInt - startInt)));

      int count = (endInt - startInt) + 1;

      output = output.GetRange(startInt, count);

      return output;
    }

    internal static ILogger Logger
    {
      get { return ServiceRegistration.Get<ILogger>(); }
    }
  }
}
