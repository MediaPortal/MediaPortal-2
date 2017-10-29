using MediaPortal.Common.MediaManagement;
using MediaPortal.Plugins.MP2Extended.MAS.TvShow;
using System;

namespace MediaPortal.Plugins.MP2Extended.ResourceAccess.MAS.TvShow.BaseClasses
{
  // TODO: Add more detailes
  class BaseTvSeasonDetailed : BaseTvSeasonBasic
  {
    internal WebTVSeasonDetailed TVSeasonDetailed(MediaItem item, Guid? showId)
    {
      WebTVSeasonBasic basic = TVSeasonBasic(item, showId);

      return new WebTVSeasonDetailed
      {
        Title = basic.Title,
        Id = basic.Id,
        ShowId = basic.ShowId,
        SeasonNumber = basic.SeasonNumber,
        EpisodeCount = basic.EpisodeCount,
        UnwatchedEpisodeCount = basic.UnwatchedEpisodeCount,
        DateAdded = basic.DateAdded,
        Year = basic.Year,
        Artwork = basic.Artwork,
        IsProtected = basic.IsProtected,
        PID = basic.PID
      };
    }
  }
}
