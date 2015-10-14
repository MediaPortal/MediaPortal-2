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
  class BaseMovieDetailed
  {
    internal WebMovieDetailed MovieDetailed(MediaItem item)
    {
      MediaItemAspect movieAspects = item.Aspects[MovieAspect.ASPECT_ID];

      WebMovieDetailed webMovieDetailed = new WebMovieDetailed();
      var TMDBId = movieAspects[MovieAspect.ATTR_TMDB_ID];
      if (TMDBId != null)
      {
        webMovieDetailed.ExternalId.Add(new WebExternalId
        {
          Site = "TMDB",
          Id = ((int)TMDBId).ToString()
        });
      }
      var ImdbId = movieAspects[MovieAspect.ATTR_IMDB_ID];
      if (ImdbId != null)
      {
        webMovieDetailed.ExternalId.Add(new WebExternalId
        {
          Site = "IMDB",
          Id = (string)movieAspects[MovieAspect.ATTR_IMDB_ID]
        });
      }

      webMovieDetailed.IsProtected = false; //??
      var rating = movieAspects.GetAttributeValue(MovieAspect.ATTR_TOTAL_RATING);
      if (rating != null)
        webMovieDetailed.Rating = Convert.ToSingle(rating);
      webMovieDetailed.Type = WebMediaType.Movie;
      webMovieDetailed.Watched = ((int)(item.Aspects[MediaAspect.ASPECT_ID][MediaAspect.ATTR_PLAYCOUNT] ?? 0) > 0);
      //webMovieDetailed.Path = ;
      //webMovieDetailed.Artwork = ;
      var year = item.Aspects[MediaAspect.ASPECT_ID][MediaAspect.ATTR_RECORDINGTIME];
      if (year != null)
        webMovieDetailed.Year = ((DateTime)year).Year;
      //webMovieDetailed.Language = ;
      webMovieDetailed.Runtime = (int)(movieAspects[MovieAspect.ATTR_RUNTIME_M] ?? 0);
      webMovieDetailed.Tagline = (string)(movieAspects[MovieAspect.ATTR_TAGLINE] ?? string.Empty);
      webMovieDetailed.DateAdded = (DateTime)item.Aspects[ImporterAspect.ASPECT_ID][ImporterAspect.ATTR_DATEADDED];
      webMovieDetailed.Id = item.MediaItemId.ToString();
      webMovieDetailed.PID = 0;
      webMovieDetailed.Title = (string)item[MediaAspect.ASPECT_ID][MediaAspect.ATTR_TITLE];
      webMovieDetailed.Summary = (string)(item[VideoAspect.ASPECT_ID][VideoAspect.ATTR_STORYPLOT] ?? string.Empty);
      var videoWriters = (HashSet<object>)item[VideoAspect.ASPECT_ID][VideoAspect.ATTR_WRITERS];
      if (videoWriters != null)
        webMovieDetailed.Writers = videoWriters.Cast<string>().ToList();
      var videoDirectors = (HashSet<object>)item[VideoAspect.ASPECT_ID][VideoAspect.ATTR_DIRECTORS];
      if (videoDirectors != null)
        webMovieDetailed.Directors = videoDirectors.Cast<string>().ToList();
      var movieGenres = (HashSet<object>)item[VideoAspect.ASPECT_ID][VideoAspect.ATTR_GENRES];
      if (movieGenres != null)
        webMovieDetailed.Genres = movieGenres.Cast<string>().ToList();
      var movieActors = (HashSet<object>)item[VideoAspect.ASPECT_ID][VideoAspect.ATTR_ACTORS];
      if (movieActors != null)
      {
        webMovieDetailed.Actors = new List<WebActor>();
        foreach (var actor in movieActors)
        {
          webMovieDetailed.Actors.Add(new WebActor
          {
            Title = actor.ToString(),
            PID = 0
          });
        }
      }

      return webMovieDetailed;
    }
  }
}
