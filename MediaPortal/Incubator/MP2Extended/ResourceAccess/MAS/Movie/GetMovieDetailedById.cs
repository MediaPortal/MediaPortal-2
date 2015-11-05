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

namespace MediaPortal.Plugins.MP2Extended.ResourceAccess.MAS.Movie
{
  internal class GetMovieDetailedById : IRequestMicroModuleHandler
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

      SingleMediaItemAspect mediaAspect = MediaItemAspect.GetAspect(item.Aspects, MediaAspect.Metadata);
      SingleMediaItemAspect movieAspect = MediaItemAspect.GetAspect(item.Aspects, MovieAspect.Metadata);
      SingleMediaItemAspect videoAspect = MediaItemAspect.GetAspect(item.Aspects, VideoAspect.Metadata);
      SingleMediaItemAspect importAspect = MediaItemAspect.GetAspect(item.Aspects, ImporterAspect.Metadata);

      WebMovieDetailed webMovieDetailed = new WebMovieDetailed();
      string TMDBId;
      MediaItemAspect.TryGetExternalAttribute(item.Aspects, ExternalIdentifierAspect.SOURCE_TMDB, ExternalIdentifierAspect.TYPE_MOVIE, out TMDBId);
      if (TMDBId != null)
      {
        webMovieDetailed.ExternalId.Add(new WebExternalId
        {
          Site = "TMDB",
          Id = TMDBId
        });
      }
      string ImdbId;
      MediaItemAspect.TryGetExternalAttribute(item.Aspects, ExternalIdentifierAspect.SOURCE_IMDB, ExternalIdentifierAspect.TYPE_MOVIE, out ImdbId);
      if (ImdbId != null)
      {
        webMovieDetailed.ExternalId.Add(new WebExternalId
        {
          Site = "IMDB",
          Id = ImdbId
        });
      }

      webMovieDetailed.IsProtected = false; //??
      var rating = movieAspect.GetAttributeValue(MovieAspect.ATTR_TOTAL_RATING);
      if (rating != null)
        webMovieDetailed.Rating = Convert.ToSingle(rating);
      webMovieDetailed.Type = WebMediaType.Movie;
      webMovieDetailed.Watched = ((int)(mediaAspect[MediaAspect.ATTR_PLAYCOUNT] ?? 0) > 0);
      //webMovieDetailed.Path = ;
      //webMovieDetailed.Artwork = ;
      //webMovieDetailed.Year = ;
      //webMovieDetailed.Language = ;
      webMovieDetailed.Runtime = (int)(movieAspect[MovieAspect.ATTR_RUNTIME_M] ?? 0);
      webMovieDetailed.Tagline = (string)(movieAspect[MovieAspect.ATTR_TAGLINE] ?? string.Empty);
      webMovieDetailed.DateAdded = (DateTime)importAspect[ImporterAspect.ATTR_DATEADDED];
      webMovieDetailed.Id = item.MediaItemId.ToString();
      webMovieDetailed.PID = 0;
      webMovieDetailed.Title = (string)mediaAspect[MediaAspect.ATTR_TITLE];
      webMovieDetailed.Summary = (string)(videoAspect[VideoAspect.ATTR_STORYPLOT] ?? string.Empty);
      var videoWriters = (HashSet<object>)videoAspect[VideoAspect.ATTR_WRITERS];
      if (videoWriters != null)
        webMovieDetailed.Writers = videoWriters.Cast<string>().ToList();
      var videoDirectors = (HashSet<object>)videoAspect[VideoAspect.ATTR_DIRECTORS];
      if (videoDirectors != null)
        webMovieDetailed.Directors = videoDirectors.Cast<string>().ToList();
      var movieGenres = (HashSet<object>)videoAspect[VideoAspect.ATTR_GENRES];
      if (movieGenres != null)
        webMovieDetailed.Genres = movieGenres.Cast<string>().ToList();
      var movieActors = (HashSet<object>)videoAspect[VideoAspect.ATTR_ACTORS];
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