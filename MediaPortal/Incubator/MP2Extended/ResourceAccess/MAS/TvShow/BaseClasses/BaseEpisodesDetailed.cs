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
    internal WebTVEpisodeDetailed EpisodeDetailed(MediaItem item, MediaItem showItem = null)
    {
      WebTVEpisodeBasic webMovieBasic = new BaseEpisodeBasic().EpisodeBasic(item);
      MediaItemAspect seriesAspects = item.Aspects[SeriesAspect.ASPECT_ID];

      if (showItem == null)
        showItem = GetMediaItems.GetMediaItemByName((string)seriesAspects[SeriesAspect.ATTR_SERIESNAME], null);

      WebTVEpisodeDetailed webTvEpisodeDetailed = new WebTVEpisodeDetailed
      {
        EpisodeNumber = webMovieBasic.EpisodeNumber,
        ExternalId = webMovieBasic.ExternalId,
        FirstAired = webMovieBasic.FirstAired,
        IsProtected = webMovieBasic.IsProtected,
        Rating = webMovieBasic.Rating,
        SeasonNumber = webMovieBasic.SeasonNumber,
        ShowId = webMovieBasic.ShowId,
        SeasonId = webMovieBasic.SeasonId,
        Type = webMovieBasic.Type,
        Watched = webMovieBasic.Watched,
        Path = webMovieBasic.Path,
        DateAdded = webMovieBasic.DateAdded,
        Id = webMovieBasic.Id,
        PID = webMovieBasic.PID,
        Title = webMovieBasic.Title,
        Artwork = webMovieBasic.Artwork
      };
      
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
