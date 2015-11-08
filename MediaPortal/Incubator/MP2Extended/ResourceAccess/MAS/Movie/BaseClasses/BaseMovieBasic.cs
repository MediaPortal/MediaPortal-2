using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Common.ResourceAccess;
using MediaPortal.Plugins.MP2Extended.Common;
using MediaPortal.Plugins.MP2Extended.MAS;
using MediaPortal.Plugins.MP2Extended.MAS.General;
using MediaPortal.Plugins.MP2Extended.MAS.Movie;
using MediaPortal.Utilities;

namespace MediaPortal.Plugins.MP2Extended.ResourceAccess.MAS.Movie.BaseClasses
{
  class BaseMovieBasic
  {
    internal WebMovieBasic MovieBasic(MediaItem item)
    {
      MediaItemAspect movieAspects = item.Aspects[MovieAspect.ASPECT_ID];
      ResourcePath path = ResourcePath.Deserialize((string)item.Aspects[ProviderResourceAspect.ASPECT_ID][ProviderResourceAspect.ATTR_RESOURCE_ACCESSOR_PATH]);

      WebMovieBasic webMovieBasic = new WebMovieBasic
      {
        ExternalId = new List<WebExternalId>(),
        Runtime = (int)movieAspects[MovieAspect.ATTR_RUNTIME_M],
        IsProtected = false, //??
        Type = WebMediaType.Movie,
        Watched = ((int)(item.Aspects[MediaAspect.ASPECT_ID][MediaAspect.ATTR_PLAYCOUNT] ?? 0) > 0),
        DateAdded = (DateTime)item.Aspects[ImporterAspect.ASPECT_ID][ImporterAspect.ATTR_DATEADDED],
        Id = item.MediaItemId.ToString(),
        PID = 0,
        Title = (string)movieAspects[MovieAspect.ATTR_MOVIE_NAME],
        Year = ((DateTime)item.Aspects[MediaAspect.ASPECT_ID].GetAttributeValue(MediaAspect.ATTR_RECORDINGTIME)).Year,
        Path = new List<string> { (path != null && path.PathSegments.Count > 0) ? StringUtils.RemovePrefixIfPresent(path.LastPathSegment.Path, "/") : string.Empty },
        Actors = new BaseMovieActors().MovieActors(item)
        //Artwork = 
      };
      var TMDBId = movieAspects[MovieAspect.ATTR_TMDB_ID];
      if (TMDBId != null)
      {
        webMovieBasic.ExternalId.Add(new WebExternalId
        {
          Site = "TMDB",
          Id = ((int)TMDBId).ToString()
        });
      }
      var ImdbId = movieAspects[MovieAspect.ATTR_IMDB_ID];
      if (ImdbId != null)
      {
        webMovieBasic.ExternalId.Add(new WebExternalId
        {
          Site = "IMDB",
          Id = (string)movieAspects[MovieAspect.ATTR_IMDB_ID]
        });
      }

      
      var rating = movieAspects.GetAttributeValue(MovieAspect.ATTR_TOTAL_RATING);
      if (rating != null)
        webMovieBasic.Rating = Convert.ToSingle(rating);
      
      var movieGenres = (HashSet<object>)item[VideoAspect.ASPECT_ID][VideoAspect.ATTR_GENRES];
      if (movieGenres != null)
        webMovieBasic.Genres = movieGenres.Cast<string>().ToList();

      return webMovieBasic;
    }
  }
}
