using System.Collections.Generic;
using System.Linq;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Plugins.MP2Extended.MAS.Movie;
using MP2Extended.Extensions;

namespace MediaPortal.Plugins.MP2Extended.ResourceAccess.MAS.Movie.BaseClasses
{
  class BaseMovieDetailed
  {
    internal WebMovieDetailed MovieDetailed(MediaItem item)
    {
      WebMovieBasic webMovieBasic = new BaseMovieBasic().MovieBasic(item);

      MediaItemAspect movieAspects = item.GetAspect(MovieAspect.Metadata);
      MediaItemAspect videoAspects = item.GetAspect(VideoAspect.Metadata);

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
        Tagline = (string)(movieAspects[MovieAspect.ATTR_TAGLINE] ?? string.Empty),
        Summary = (string)(videoAspects[VideoAspect.ATTR_STORYPLOT] ?? string.Empty),
      };

      //webMovieDetailed.Language = ;
      var videoWriters = (HashSet<object>)videoAspects[VideoAspect.ATTR_WRITERS];
      if (videoWriters != null)
        webMovieDetailed.Writers = videoWriters.Cast<string>().ToList();
      var videoDirectors = (HashSet<object>)videoAspects[VideoAspect.ATTR_DIRECTORS];
      if (videoDirectors != null)
        webMovieDetailed.Directors = videoDirectors.Cast<string>().ToList();

      return webMovieDetailed;
    }
  }
}
