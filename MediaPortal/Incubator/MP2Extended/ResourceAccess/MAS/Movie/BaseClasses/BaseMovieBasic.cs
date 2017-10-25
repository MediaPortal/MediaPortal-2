using System;
using System.Collections.Generic;
using System.Linq;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Common.ResourceAccess;
using MediaPortal.Plugins.MP2Extended.Common;
using MediaPortal.Plugins.MP2Extended.MAS;
using MediaPortal.Plugins.MP2Extended.MAS.Movie;
using MediaPortal.Utilities;
using MP2Extended.Extensions;
using MediaPortal.Plugins.MP2Extended.MAS.General;

namespace MediaPortal.Plugins.MP2Extended.ResourceAccess.MAS.Movie.BaseClasses
{
  class BaseMovieBasic
  {
    internal ISet<Guid> BasicNecessaryMIATypeIds = new HashSet<Guid>
    {
      MediaAspect.ASPECT_ID,
      ImporterAspect.ASPECT_ID,
      ProviderResourceAspect.ASPECT_ID,
      VideoAspect.ASPECT_ID,
      MovieAspect.ASPECT_ID
    };

    internal ISet<Guid> BasicOptionalMIATypeIds = new HashSet<Guid>
    {
      ExternalIdentifierAspect.ASPECT_ID,
      GenreAspect.ASPECT_ID
    };

    internal WebMovieBasic MovieBasic(MediaItem item)
    {
      MediaItemAspect movieAspect = item.GetAspect(MovieAspect.Metadata);
      MediaItemAspect videoAspect = item.GetAspect(VideoAspect.Metadata);
      MediaItemAspect mediaAspect = item.GetAspect(MediaAspect.Metadata);
      MediaItemAspect importerAspect = item.GetAspect(ImporterAspect.Metadata);

      ResourcePath resourcePath = ResourcePath.Deserialize(item.PrimaryProviderResourcePath());
      string path = resourcePath.PathSegments.Count > 0 ? StringUtils.RemovePrefixIfPresent(resourcePath.LastPathSegment.Path, "/") : string.Empty;
      
      WebMovieBasic webMovieBasic = new WebMovieBasic
      {
        Title = movieAspect.GetAttributeValue<string>(MovieAspect.ATTR_MOVIE_NAME),
        Id = item.MediaItemId.ToString(),
        Type = WebMediaType.Movie,
        Path = new List<string> { path },
        Year = mediaAspect.GetAttributeValue<DateTime>(MediaAspect.ATTR_RECORDINGTIME).Year,
        Runtime = movieAspect.GetAttributeValue<int>(MovieAspect.ATTR_RUNTIME_M),
        Watched = mediaAspect.GetAttributeValue<int>(MediaAspect.ATTR_PLAYCOUNT) > 0,
        DateAdded = importerAspect.GetAttributeValue<DateTime>(ImporterAspect.ATTR_DATEADDED),
        Rating = Convert.ToSingle(movieAspect.GetAttributeValue<double>(MovieAspect.ATTR_TOTAL_RATING)),
      };

      IEnumerable<string> aspectActors = videoAspect.GetCollectionAttribute<string>(VideoAspect.ATTR_ACTORS);
      if (aspectActors != null)
        webMovieBasic.Actors = aspectActors.Distinct().Select(a => new WebActor(a)).ToList();

      IList<MediaItemAspect> genres;
      if (item.Aspects.TryGetValue(GenreAspect.ASPECT_ID, out genres))
        webMovieBasic.Genres = genres.Select(g => g.GetAttributeValue<string>(GenreAspect.ATTR_GENRE)).ToList();

      string tmdbId;
      if (MediaItemAspect.TryGetExternalAttribute(item.Aspects, ExternalIdentifierAspect.SOURCE_TMDB, ExternalIdentifierAspect.TYPE_MOVIE, out tmdbId) && tmdbId != null)
        webMovieBasic.ExternalId.Add(new WebExternalId { Site = "TMDB", Id = tmdbId });
      string imdbId;
      if (MediaItemAspect.TryGetExternalAttribute(item.Aspects, ExternalIdentifierAspect.SOURCE_IMDB, ExternalIdentifierAspect.TYPE_MOVIE, out imdbId) && imdbId != null)
        webMovieBasic.ExternalId.Add(new WebExternalId { Site = "IMDB", Id = imdbId });

      return webMovieBasic;
    }
  }
}
