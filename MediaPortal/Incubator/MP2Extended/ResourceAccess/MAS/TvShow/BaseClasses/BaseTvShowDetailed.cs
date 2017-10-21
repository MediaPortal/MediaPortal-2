using System;
using System.Collections.Generic;
using System.Linq;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Common.ResourceAccess;
using MediaPortal.Plugins.MP2Extended.Common;
using MediaPortal.Plugins.MP2Extended.MAS;
using MediaPortal.Plugins.MP2Extended.MAS.TvShow;
using MediaPortal.Utilities;
using MediaPortal.Common.MediaManagement.MLQueries;
using MediaPortal.Common;
using MediaPortal.Backend.MediaLibrary;
using MediaPortal.Common.MediaManagement.Helpers;

namespace MediaPortal.Plugins.MP2Extended.ResourceAccess.MAS.TvShow.BaseClasses
{

  // TODO: Add more detailes
  class BaseTvShowDetailed : BaseTvShowBasic
  {
    internal WebTVShowDetailed TVShowDetailed(MediaItem item, MediaItem showItem = null)
    {
      var seriesAspect = item[SeriesAspect.Metadata];
      var importerAspect = item[ImporterAspect.Metadata];

      var tvShowBasic = TVShowBasic(item);

      return new WebTVShowDetailed()
      {
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
