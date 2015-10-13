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
      SingleMediaItemAspect mediaAspect = MediaItemAspect.GetAspect(item.Aspects, MediaAspect.Metadata);
      SingleMediaItemAspect episodeAspect = MediaItemAspect.GetAspect(item.Aspects, EpisodeAspect.Metadata);
      SingleMediaItemAspect videoAspect = MediaItemAspect.GetAspect(item.Aspects, VideoAspect.Metadata);

      WebTVEpisodeDetailed webTvEpisodeDetailed = new WebTVEpisodeDetailed();
      var episodeNumber = ((HashSet<object>)episodeAspect[EpisodeAspect.ATTR_EPISODE]).Cast<int>().ToList();
      webTvEpisodeDetailed.EpisodeNumber = episodeNumber[0];
      string TvDbId;
      MediaItemAspect.TryGetExternalAttribute(item.Aspects, ExternalIdentifierAspect.SOURCE_TVDB, ExternalIdentifierAspect.TYPE_EPISODE, out TvDbId);
      if (TvDbId != null)
      {
        webTvEpisodeDetailed.ExternalId.Add(new WebExternalId
        {
          Site = "TVDB",
          Id = TvDbId
        });
      }
      string ImdbId;
      MediaItemAspect.TryGetExternalAttribute(item.Aspects, ExternalIdentifierAspect.SOURCE_IMDB, ExternalIdentifierAspect.TYPE_SERIES, out ImdbId);
      if (ImdbId != null)
      {
        webTvEpisodeDetailed.ExternalId.Add(new WebExternalId
        {
          Site = "IMDB",
          Id = ImdbId
        });
      }

      var firstAired = episodeAspect[EpisodeAspect.ATTR_FIRSTAIRED];
      if (firstAired != null)
        webTvEpisodeDetailed.FirstAired = (DateTime)episodeAspect[EpisodeAspect.ATTR_FIRSTAIRED];
      webTvEpisodeDetailed.IsProtected = false; //??
      webTvEpisodeDetailed.Rating = Convert.ToSingle((double)episodeAspect[EpisodeAspect.ATTR_TOTAL_RATING]);
      // TODO: Check => doesn't work
      MediaItem seasonItem = GetMediaItems.GetMediaItemByName((string)episodeAspect[EpisodeAspect.ATTR_SERIES_SEASON], null);
      if (seasonItem != null)
        webTvEpisodeDetailed.SeasonId = seasonItem.MediaItemId.ToString();
      webTvEpisodeDetailed.SeasonNumber = (int)episodeAspect[EpisodeAspect.ATTR_SEASON];
      MediaItem showItem = GetMediaItems.GetMediaItemByName((string)episodeAspect[EpisodeAspect.ATTR_SERIESNAME], null);
      if (showItem != null)
        webTvEpisodeDetailed.ShowId = showItem.MediaItemId.ToString();
      webTvEpisodeDetailed.Type = WebMediaType.TVEpisode;
      webTvEpisodeDetailed.Watched = ((int)(mediaAspect[MediaAspect.ATTR_PLAYCOUNT] ?? 0) > 0);
      webTvEpisodeDetailed.Path = new List<string> { item.MediaItemId.ToString() };
      //webTvEpisodeBasic.Artwork = ;
      webTvEpisodeDetailed.DateAdded = (DateTime)mediaAspect[ImporterAspect.ATTR_DATEADDED];
      webTvEpisodeDetailed.Id = item.MediaItemId.ToString();
      webTvEpisodeDetailed.PID = 0;
      webTvEpisodeDetailed.Title = (string)episodeAspect[EpisodeAspect.ATTR_EPISODENAME];
      webTvEpisodeDetailed.Summary = (string)videoAspect[VideoAspect.ATTR_STORYPLOT];
      webTvEpisodeDetailed.Show = (string)episodeAspect[EpisodeAspect.ATTR_SERIESNAME];
      var videoWriters = (HashSet<object>)videoAspect[VideoAspect.ATTR_WRITERS];
      if (videoWriters != null)
        webTvEpisodeDetailed.Writers = videoWriters.Cast<string>().ToList();
      var videoDirectors = (HashSet<object>)videoAspect[VideoAspect.ATTR_DIRECTORS];
      if (videoDirectors != null)
        webTvEpisodeDetailed.Directors = videoDirectors.Cast<string>().ToList();
      // webTVEpisodeDetailed.GuestStars not in the MP DB

      return webTvEpisodeDetailed;
    }
  }
}
