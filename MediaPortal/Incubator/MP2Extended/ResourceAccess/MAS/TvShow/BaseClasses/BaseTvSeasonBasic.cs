using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Common.MediaManagement.MLQueries;
using MediaPortal.Plugins.MP2Extended.Common;
using MediaPortal.Plugins.MP2Extended.MAS.TvShow;
using MP2Extended.Extensions;
using MP2Extended.ResourceAccess;
using System;
using System.Collections.Generic;

namespace MediaPortal.Plugins.MP2Extended.ResourceAccess.MAS.TvShow.BaseClasses
{
  // TODO: Add more detailes
  class BaseTvSeasonBasic
  {
    internal ISet<Guid> BasicNecessaryMIATypeIds = new HashSet<Guid>
    {
      MediaAspect.ASPECT_ID,
      SeasonAspect.ASPECT_ID,
      ImporterAspect.ASPECT_ID,
      RelationshipAspect.ASPECT_ID
    };

    internal ISet<Guid> BasicOptionalMIATypeIds = new HashSet<Guid>
    {
    };

    internal WebTVSeasonBasic TVSeasonBasic(MediaItem item, Guid? showId = null)
    {
      ISet<Guid> necessaryMIATypespisodes = new HashSet<Guid>();
      necessaryMIATypespisodes.Add(MediaAspect.ASPECT_ID);
      necessaryMIATypespisodes.Add(EpisodeAspect.ASPECT_ID);

      IFilter unwatchedEpisodeFilter = BooleanCombinationFilter.CombineFilters(BooleanOperator.And,
        new RelationshipFilter(EpisodeAspect.ROLE_EPISODE, SeasonAspect.ROLE_SEASON, item.MediaItemId),
        BooleanCombinationFilter.CombineFilters(BooleanOperator.Or,
          new EmptyFilter(MediaAspect.ATTR_PLAYCOUNT), new RelationalFilter(MediaAspect.ATTR_PLAYCOUNT, RelationalOperator.EQ, 0)));

      int unwatchedCount = GetMediaItems.CountMediaItems(necessaryMIATypespisodes, unwatchedEpisodeFilter);

      if (!showId.HasValue)
        showId = item.GetLinkedIdOrDefault(SeasonAspect.ROLE_SEASON, SeriesAspect.ROLE_SERIES);

      var mediaAspect = item.GetAspect(MediaAspect.Metadata);
      var seasonAspect = item.GetAspect(SeasonAspect.Metadata);
      var importerAspect = item.GetAspect(ImporterAspect.Metadata);

      DateTime? firstAired = mediaAspect.GetAttributeValue<DateTime?>(MediaAspect.ATTR_RECORDINGTIME);

      return new WebTVSeasonBasic
      {
        Title = mediaAspect.GetAttributeValue<string>(MediaAspect.ATTR_TITLE),
        Id = item.MediaItemId.ToString(),
        ShowId = showId.HasValue ? showId.Value.ToString() : null,
        SeasonNumber = seasonAspect.GetAttributeValue<int>(SeasonAspect.ATTR_SEASON),
        EpisodeCount = seasonAspect.GetAttributeValue<int>(SeasonAspect.ATTR_AVAILABLE_EPISODES),
        UnwatchedEpisodeCount = unwatchedCount,
        DateAdded = importerAspect.GetAttributeValue<DateTime>(ImporterAspect.ATTR_DATEADDED),
        Year = firstAired.HasValue ? firstAired.Value.Year : 0,
        Artwork = GetFanart.GetArtwork(item.MediaItemId, WebMediaType.TVSeason),
        IsProtected = false,
        PID = 0
      };
    }
  }
}
