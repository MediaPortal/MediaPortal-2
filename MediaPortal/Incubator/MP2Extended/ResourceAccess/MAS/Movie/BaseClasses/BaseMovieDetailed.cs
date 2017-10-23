using System.Collections.Generic;
using System.Linq;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Plugins.MP2Extended.MAS.Movie;
using MP2Extended.Extensions;

namespace MediaPortal.Plugins.MP2Extended.ResourceAccess.MAS.Movie.BaseClasses
{
  class BaseMovieDetailed : BaseMovieBasic
  {
    internal WebMovieDetailed MovieDetailed(MediaItem item)
    {
      WebMovieBasic webMovieBasic = MovieBasic(item);

      MediaItemAspect movieAspect = item.GetAspect(MovieAspect.Metadata);
      MediaItemAspect videoAspect = item.GetAspect(VideoAspect.Metadata);

      WebMovieDetailed webMovieDetailed = new WebMovieDetailed
      {
        IsProtected = webMovieBasic.IsProtected,
        Type = webMovieBasic.Type,
        Watched = webMovieBasic.Watched,
        Runtime = webMovieBasic.Runtime,
        DateAdded = webMovieBasic.DateAdded,
        Id = webMovieBasic.Id,
        PID = webMovieBasic.PID,
        Title = webMovieBasic.Title,
        ExternalId = webMovieBasic.ExternalId,
        Rating = webMovieBasic.Rating,
        Year = webMovieBasic.Year,
        Actors = webMovieBasic.Actors,
        Genres = webMovieBasic.Genres,
        Path = webMovieBasic.Path,
        Artwork = webMovieBasic.Artwork,
        Tagline = movieAspect.GetAttributeValue<string>(MovieAspect.ATTR_TAGLINE) ?? string.Empty,
        Summary = videoAspect.GetAttributeValue<string>(VideoAspect.ATTR_STORYPLOT) ?? string.Empty,
      };

      IEnumerable<string> aspectWriters = videoAspect.GetCollectionAttribute<string>(VideoAspect.ATTR_WRITERS);
      if (aspectWriters != null)
        webMovieDetailed.Writers = aspectWriters.Distinct().ToList();

      IEnumerable<string> aspectDirectors = videoAspect.GetCollectionAttribute<string>(VideoAspect.ATTR_DIRECTORS);
      if (aspectDirectors != null)
        webMovieDetailed.Directors = aspectDirectors.Distinct().ToList();

      return webMovieDetailed;
    }
  }
}
