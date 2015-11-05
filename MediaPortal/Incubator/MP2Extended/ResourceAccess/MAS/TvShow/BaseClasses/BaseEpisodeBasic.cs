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
  class BaseEpisodeBasic
  {
    internal WebTVEpisodeBasic EpisodeBasic(MediaItem item)
    {
      SingleMediaItemAspect mediaAspect = MediaItemAspect.GetAspect(item.Aspects, MediaAspect.Metadata);
      SingleMediaItemAspect episodeAspect = MediaItemAspect.GetAspect(item.Aspects, EpisodeAspect.Metadata);
      SingleMediaItemAspect importerAspect = MediaItemAspect.GetAspect(item.Aspects, ImporterAspect.Metadata);

      WebTVEpisodeBasic webTvEpisodeBasic = new WebTVEpisodeBasic();
      var episodeNumber = ((HashSet<object>)episodeAspect[EpisodeAspect.ATTR_EPISODE]).Cast<int>().ToList();
      webTvEpisodeBasic.EpisodeNumber = episodeNumber[0];
      string TvDbId;
      MediaItemAspect.TryGetExternalAttribute(item.Aspects, ExternalIdentifierAspect.SOURCE_TVDB, ExternalIdentifierAspect.TYPE_EPISODE, out TvDbId);
      if (TvDbId != null)
      {
        webTvEpisodeBasic.ExternalId.Add(new WebExternalId
        {
          Site = "TVDB",
          Id = TvDbId
        });
      }
      string ImdbId;
      MediaItemAspect.TryGetExternalAttribute(item.Aspects, ExternalIdentifierAspect.SOURCE_IMDB, ExternalIdentifierAspect.TYPE_SERIES, out ImdbId);
      if (ImdbId != null)
      {
        webTvEpisodeBasic.ExternalId.Add(new WebExternalId
        {
          Site = "IMDB",
          Id = ImdbId
        });
      }

      var firstAired = episodeAspect[EpisodeAspect.ATTR_FIRSTAIRED];
      if (firstAired != null)
        webTvEpisodeBasic.FirstAired = (DateTime)episodeAspect[EpisodeAspect.ATTR_FIRSTAIRED];
      webTvEpisodeBasic.IsProtected = false; //??
      webTvEpisodeBasic.Rating = Convert.ToSingle((double)episodeAspect[EpisodeAspect.ATTR_TOTAL_RATING]);
      // TODO: Check => doesn't work
      MediaItem seasonItem = GetMediaItems.GetMediaItemByName((string)episodeAspect[EpisodeAspect.ATTR_SERIES_SEASON], null);
      if (seasonItem != null)
        webTvEpisodeBasic.SeasonId = seasonItem.MediaItemId.ToString();
      webTvEpisodeBasic.SeasonNumber = (int)episodeAspect[EpisodeAspect.ATTR_SEASON];
      MediaItem showItem = GetMediaItems.GetMediaItemByName((string)episodeAspect[EpisodeAspect.ATTR_SERIESNAME], null);
      if (showItem != null)
        webTvEpisodeBasic.ShowId = showItem.MediaItemId.ToString();
      webTvEpisodeBasic.Type = WebMediaType.TVEpisode;
      webTvEpisodeBasic.Watched = ((int)(mediaAspect[MediaAspect.ATTR_PLAYCOUNT] ?? 0) > 0);
      webTvEpisodeBasic.Path = new List<string> { item.MediaItemId.ToString() };
      //webTvEpisodeBasic.Artwork = ;
      webTvEpisodeBasic.DateAdded = (DateTime)importerAspect[ImporterAspect.ATTR_DATEADDED];
      webTvEpisodeBasic.Id = item.MediaItemId.ToString();
      webTvEpisodeBasic.PID = 0;
      webTvEpisodeBasic.Title = (string)episodeAspect[EpisodeAspect.ATTR_EPISODENAME];

      return webTvEpisodeBasic;
    }
  }
}
