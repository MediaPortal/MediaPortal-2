using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Plugins.MP2Extended.MAS.TvShow;
using MP2Extended.Extensions;

namespace MediaPortal.Plugins.MP2Extended.ResourceAccess.MAS.TvShow.BaseClasses
{

  // TODO: Add more detailes
  class BaseTvShowDetailed : BaseTvShowBasic
  {
    internal WebTVShowDetailed TVShowDetailed(MediaItem item, MediaItem showItem = null)
    {
      var seriesAspect = item.GetAspect(SeriesAspect.Metadata);
      var tvShowBasic = TVShowBasic(item);

      return new WebTVShowDetailed()
      {
        Summary = seriesAspect.GetAttributeValue<string>(SeriesAspect.ATTR_DESCRIPTION),
        // From TvShowBasic
        Id = tvShowBasic.Id,
        Title = tvShowBasic.Title,
        DateAdded = tvShowBasic.DateAdded,
        EpisodeCount = tvShowBasic.EpisodeCount,
        UnwatchedEpisodeCount = tvShowBasic.UnwatchedEpisodeCount,
        PID = tvShowBasic.PID,
        Genres = tvShowBasic.Genres,
        Actors = tvShowBasic.Actors,
        Artwork = tvShowBasic.Artwork,
        ContentRating = tvShowBasic.ContentRating,
        ExternalId = tvShowBasic.ExternalId,
        IsProtected = tvShowBasic.IsProtected,
        Rating = tvShowBasic.Rating,
        SeasonCount = tvShowBasic.SeasonCount,
        Year = tvShowBasic.Year
      };
    }
  }
}
