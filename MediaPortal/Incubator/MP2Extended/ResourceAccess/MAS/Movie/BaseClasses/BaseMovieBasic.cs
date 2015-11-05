using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Plugins.MP2Extended.Common;
using MediaPortal.Plugins.MP2Extended.MAS;
using MediaPortal.Plugins.MP2Extended.MAS.General;
using MediaPortal.Plugins.MP2Extended.MAS.Movie;

namespace MediaPortal.Plugins.MP2Extended.ResourceAccess.MAS.Movie.BaseClasses
{
  class BaseMovieBasic
  {
    internal WebMovieBasic MovieBasic(MediaItem item)
    {
      MediaItemAspect movieAspects = item.Aspects[MovieAspect.ASPECT_ID];
      
      WebMovieBasic webMovieBasic = new WebMovieBasic();
      webMovieBasic.ExternalId = new List<WebExternalId>();
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

      webMovieBasic.Runtime = (int)movieAspects[MovieAspect.ATTR_RUNTIME_M];
      webMovieBasic.IsProtected = false; //??
      var rating = movieAspects.GetAttributeValue(MovieAspect.ATTR_TOTAL_RATING);
      if (rating != null)
        webMovieBasic.Rating = Convert.ToSingle(rating);
      webMovieBasic.Type = WebMediaType.Movie;
      webMovieBasic.Watched = ((int)(item.Aspects[MediaAspect.ASPECT_ID][MediaAspect.ATTR_PLAYCOUNT] ?? 0) > 0);
      //webTvEpisodeBasic.Path = ;
      //webTvEpisodeBasic.Artwork = ;
      var year = item.Aspects[MediaAspect.ASPECT_ID][MediaAspect.ATTR_RECORDINGTIME];
      if (year != null)
        webMovieBasic.Year = ((DateTime)year).Year;
      webMovieBasic.DateAdded = (DateTime)item.Aspects[ImporterAspect.ASPECT_ID][ImporterAspect.ATTR_DATEADDED];
      webMovieBasic.Id = item.MediaItemId.ToString();
      webMovieBasic.PID = 0;
      webMovieBasic.Title = (string)movieAspects[MovieAspect.ATTR_MOVIE_NAME];
      var movieActors = (HashSet<object>)item[VideoAspect.ASPECT_ID][VideoAspect.ATTR_ACTORS];
      if (movieActors != null)
      {
        webMovieBasic.Actors = new List<WebActor>();
        foreach (var actor in movieActors)
        {
          webMovieBasic.Actors.Add(new WebActor
          {
            Title = actor.ToString(),
            PID = 0
          });
        }
      }
      var movieGenres = (HashSet<object>)item[VideoAspect.ASPECT_ID][VideoAspect.ATTR_GENRES];
      if (movieGenres != null)
        webMovieBasic.Genres = movieGenres.Cast<string>().ToList();

      return webMovieBasic;
    }
  }
}
