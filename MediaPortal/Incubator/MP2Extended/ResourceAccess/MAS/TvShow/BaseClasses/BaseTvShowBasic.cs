using MediaPortal.Backend.MediaLibrary;
using MediaPortal.Common;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Common.MediaManagement.MLQueries;
using MediaPortal.Plugins.MP2Extended.Common;
using MediaPortal.Plugins.MP2Extended.MAS;
using MediaPortal.Plugins.MP2Extended.MAS.General;
using MediaPortal.Plugins.MP2Extended.MAS.TvShow;
using MP2Extended.Extensions;
using MP2Extended.ResourceAccess;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MediaPortal.Plugins.MP2Extended.ResourceAccess.MAS.TvShow.BaseClasses
{
  // TODO: Add more detailes
  class BaseTvShowBasic
  {
    internal ISet<Guid> BasicNecessaryMIATypeIds = new HashSet<Guid>
    {
      MediaAspect.ASPECT_ID,
      ImporterAspect.ASPECT_ID,
      SeriesAspect.ASPECT_ID
    };

    internal ISet<Guid> BasicOptionalMIATypeIds = new HashSet<Guid>
    {
      GenreAspect.ASPECT_ID,
      ExternalIdentifierAspect.ASPECT_ID
    };

    internal WebTVShowBasic TVShowBasic(MediaItem item)
    {
      ISet<Guid> necessaryMIATypespisodes = new HashSet<Guid>();
      necessaryMIATypespisodes.Add(MediaAspect.ASPECT_ID);
      necessaryMIATypespisodes.Add(EpisodeAspect.ASPECT_ID);

      IFilter unwatchedEpisodeFilter = BooleanCombinationFilter.CombineFilters(BooleanOperator.And,
        new RelationshipFilter(EpisodeAspect.ROLE_EPISODE, SeriesAspect.ROLE_SERIES, item.MediaItemId),
        BooleanCombinationFilter.CombineFilters(BooleanOperator.Or,
          new EmptyFilter(MediaAspect.ATTR_PLAYCOUNT), new RelationalFilter(MediaAspect.ATTR_PLAYCOUNT, RelationalOperator.EQ, 0)));

      int unwatchedCount = ServiceRegistration.Get<IMediaLibrary>().CountMediaItems(necessaryMIATypespisodes, unwatchedEpisodeFilter, false, false);

      var mediaAspect = item.GetAspect(MediaAspect.Metadata);
      var seriesAspect = item.GetAspect(SeriesAspect.Metadata);
      var importerAspect = item.GetAspect(ImporterAspect.Metadata);
      DateTime? firstAired = mediaAspect.GetAttributeValue<DateTime?>(MediaAspect.ATTR_RECORDINGTIME);
      IList<WebActor> actors = seriesAspect.GetCollectionAttribute<string>(SeriesAspect.ATTR_ACTORS)?.Distinct().Select(a => new WebActor(a)).ToList() ?? new List<WebActor>();
      WebArtwork aw = new WebArtwork();

      var show = new WebTVShowBasic()
      {
        Id = item.MediaItemId.ToString(),
        Title = seriesAspect.GetAttributeValue<string>(SeriesAspect.ATTR_SERIES_NAME),
        DateAdded = importerAspect.GetAttributeValue<DateTime>(ImporterAspect.ATTR_DATEADDED),
        EpisodeCount = seriesAspect.GetAttributeValue<int>(SeriesAspect.ATTR_AVAILABLE_EPISODES),
        SeasonCount = seriesAspect.GetAttributeValue<int>(SeriesAspect.ATTR_AVAILABLE_SEASONS),
        Rating = Convert.ToSingle(seriesAspect.GetAttributeValue<double>(SeriesAspect.ATTR_TOTAL_RATING)),
        ContentRating = seriesAspect.GetAttributeValue<string>(SeriesAspect.ATTR_CERTIFICATION),
        Actors = actors,
        UnwatchedEpisodeCount = unwatchedCount,
        Year = firstAired.HasValue ? firstAired.Value.Year : 0,
        Artwork = GetFanart.GetArtwork(item.MediaItemId, WebMediaType.TVShow)
      };

      IList<MediaItemAspect> genres;
      if (item.Aspects.TryGetValue(GenreAspect.ASPECT_ID, out genres))
        show.Genres = genres.Select(g => g.GetAttributeValue<string>(GenreAspect.ATTR_GENRE)).ToList();

      string tvDbId;
      MediaItemAspect.TryGetExternalAttribute(item.Aspects, ExternalIdentifierAspect.SOURCE_TVDB, ExternalIdentifierAspect.TYPE_SERIES, out tvDbId);
      if (tvDbId != null)
        show.ExternalId.Add(new WebExternalId { Site = "TVDB", Id = tvDbId });
      string imdbId;
      MediaItemAspect.TryGetExternalAttribute(item.Aspects, ExternalIdentifierAspect.SOURCE_IMDB, ExternalIdentifierAspect.TYPE_SERIES, out imdbId);
      if (imdbId != null)
        show.ExternalId.Add(new WebExternalId { Site = "IMDB", Id = imdbId });

      return show;
    }
  }
}
