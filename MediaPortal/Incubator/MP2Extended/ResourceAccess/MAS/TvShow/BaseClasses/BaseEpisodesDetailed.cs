using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Plugins.MP2Extended.Common;
using MediaPortal.Plugins.MP2Extended.MAS;
using MediaPortal.Plugins.MP2Extended.MAS.TvShow;

namespace MediaPortal.Plugins.MP2Extended.ResourceAccess.MAS.TvShow.BaseClasses
{
  class BaseEpisodesDetailed
  {
    internal WebTVEpisodeDetailed EpisodeDetailed(MediaItem item)
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
      webTvEpisodeDetailed.Title = (string)seriesAspects[SeriesAspect.ATTR_EPISODENAME];
      webTvEpisodeDetailed.Summary = (string)item[VideoAspect.ASPECT_ID][VideoAspect.ATTR_STORYPLOT];
      webTvEpisodeDetailed.Show = (string)item[SeriesAspect.ASPECT_ID][SeriesAspect.ATTR_SERIESNAME];
      var videoWriters = (HashSet<object>)item[VideoAspect.ASPECT_ID][VideoAspect.ATTR_WRITERS];
      if (videoWriters != null)
        webTvEpisodeDetailed.Writers = videoWriters.Cast<string>().ToList();
      var videoDirectors = (HashSet<object>)item[VideoAspect.ASPECT_ID][VideoAspect.ATTR_DIRECTORS];
      if (videoDirectors != null)
        webTvEpisodeDetailed.Directors = videoDirectors.Cast<string>().ToList();
      // webTVEpisodeDetailed.GuestStars not in the MP DB

      return webTvEpisodeDetailed;
    }
  }
}
