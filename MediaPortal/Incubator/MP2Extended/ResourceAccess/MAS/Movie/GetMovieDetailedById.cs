using System;
using System.Collections.Generic;
using System.Linq;
using HttpServer;
using HttpServer.Exceptions;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Plugins.MP2Extended.Common;
using MediaPortal.Plugins.MP2Extended.MAS;
using MediaPortal.Plugins.MP2Extended.MAS.General;
using MediaPortal.Plugins.MP2Extended.MAS.Movie;
using MediaPortal.Plugins.MP2Extended.MAS.TvShow;

namespace MediaPortal.Plugins.MP2Extended.ResourceAccess.MAS.Movie
{
  class GetMovieDetailedById : IRequestMicroModuleHandler
  {
    public dynamic Process(IHttpRequest request)
    {
      HttpParam httpParam = request.Param;
      if (httpParam["id"].Value == null)
        throw new BadRequestException("GetMovieDetailedById: no id is null");

      ISet<Guid> necessaryMIATypes = new HashSet<Guid>();
      necessaryMIATypes.Add(MediaAspect.ASPECT_ID);
      necessaryMIATypes.Add(ProviderResourceAspect.ASPECT_ID);
      necessaryMIATypes.Add(ImporterAspect.ASPECT_ID);
      necessaryMIATypes.Add(VideoAspect.ASPECT_ID);
      necessaryMIATypes.Add(MovieAspect.ASPECT_ID);

      MediaItem item = GetMediaItems.GetMediaItemById(httpParam["id"].Value, necessaryMIATypes);

      if (item == null)
        throw new BadRequestException(String.Format("GetMovieDetailedById: No MediaItem found with id: {0}", httpParam["id"].Value));

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
      webMovieDetailed.Rating = Convert.ToSingle((double)movieAspects[MovieAspect.ATTR_TOTAL_RATING]);
      webMovieDetailed.Type = WebMediaType.Movie;
      webMovieDetailed.Watched = ((int)(item.Aspects[MediaAspect.ASPECT_ID][MediaAspect.ATTR_PLAYCOUNT] ?? 0) > 0);
      //webMovieDetailed.Path = ;
      //webMovieDetailed.Artwork = ;
      //webMovieDetailed.Year = ;
      //webMovieDetailed.Language = ;
      webMovieDetailed.Runtime = (int)movieAspects[MovieAspect.ATTR_RUNTIME_M];
      webMovieDetailed.Tagline = (string)movieAspects[MovieAspect.ATTR_TAGLINE];
      webMovieDetailed.DateAdded = (DateTime)item.Aspects[ImporterAspect.ASPECT_ID][ImporterAspect.ATTR_DATEADDED];
      webMovieDetailed.Id = item.MediaItemId.ToString();
      webMovieDetailed.PID = 0;
      webMovieDetailed.Title = (string)item[MediaAspect.ASPECT_ID][MediaAspect.ATTR_TITLE];
      webMovieDetailed.Summary = (string)item[VideoAspect.ASPECT_ID][VideoAspect.ATTR_STORYPLOT];
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

    internal static ILogger Logger
    {
      get { return ServiceRegistration.Get<ILogger>(); }
    }
  }
}
