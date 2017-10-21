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

namespace MediaPortal.Plugins.MP2Extended.ResourceAccess.MAS.Movie.BaseClasses
{
  class BaseMovieBasic
  {
    internal WebMovieBasic MovieBasic(MediaItem item)
    {
      MediaItemAspect movieAspects = MediaItemAspect.GetAspect(item.Aspects, MovieAspect.Metadata);
      ResourcePath path = ResourcePath.Deserialize(item.PrimaryProviderResourcePath());

      WebMovieBasic webMovieBasic = new WebMovieBasic
      {
        ExternalId = new List<WebExternalId>(),
        Runtime = (int)movieAspects[MovieAspect.ATTR_RUNTIME_M],
        IsProtected = false, //??
        Type = WebMediaType.Movie,
        Watched = ((int)(item.GetAspect(MediaAspect.Metadata)[MediaAspect.ATTR_PLAYCOUNT] ?? 0) > 0),
        DateAdded = item.GetAspect(ImporterAspect.Metadata).GetAttributeValue<DateTime>(ImporterAspect.ATTR_DATEADDED),
        Id = item.MediaItemId.ToString(),
        PID = 0,
        Title = (string)movieAspects[MovieAspect.ATTR_MOVIE_NAME],
        Year = (item.GetAspect(MediaAspect.Metadata).GetAttributeValue<DateTime>(MediaAspect.ATTR_RECORDINGTIME)).Year,
        Path = new List<string> { (path != null && path.PathSegments.Count > 0) ? StringUtils.RemovePrefixIfPresent(path.LastPathSegment.Path, "/") : string.Empty },
        Actors = new BaseMovieActors().MovieActors(item)
        //Artwork = 
      };
      string TMDBId;
      if (MediaItemAspect.TryGetExternalAttribute(item.Aspects, ExternalIdentifierAspect.SOURCE_TMDB, ExternalIdentifierAspect.TYPE_MOVIE, out TMDBId) && TMDBId != null)
      {
        webMovieBasic.ExternalId.Add(new WebExternalId
        {
          Site = "TMDB",
          Id = TMDBId
        });
      }
      string ImdbId;
      if (MediaItemAspect.TryGetExternalAttribute(item.Aspects, ExternalIdentifierAspect.SOURCE_IMDB, ExternalIdentifierAspect.TYPE_MOVIE, out ImdbId) && ImdbId != null)
      {
        webMovieBasic.ExternalId.Add(new WebExternalId
        {
          Site = "IMDB",
          Id = ImdbId
        });
      }

      
      var rating = movieAspects.GetAttributeValue(MovieAspect.ATTR_TOTAL_RATING);
      if (rating != null)
        webMovieBasic.Rating = Convert.ToSingle(rating);
      
      //var movieGenres = (HashSet<object>)item[VideoAspect.Metadata][VideoAspect.ATTR_GENRES];
      //if (movieGenres != null)
      //  webMovieBasic.Genres = movieGenres.Cast<string>().ToList();

      return webMovieBasic;
    }
  }
}
