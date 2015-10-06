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
using MediaPortal.Plugins.MP2Extended.Extensions;
using MediaPortal.Plugins.MP2Extended.MAS;
using MediaPortal.Plugins.MP2Extended.MAS.General;
using MediaPortal.Plugins.MP2Extended.MAS.Movie;
using Newtonsoft.Json;

namespace MediaPortal.Plugins.MP2Extended.ResourceAccess.MAS.Movie
{
  class GetMoviesBasic : IRequestMicroModuleHandler
  {
    public dynamic Process(IHttpRequest request)
    {
      ISet<Guid> necessaryMIATypes = new HashSet<Guid>();
      necessaryMIATypes.Add(MediaAspect.ASPECT_ID);
      necessaryMIATypes.Add(ProviderResourceAspect.ASPECT_ID);
      necessaryMIATypes.Add(ImporterAspect.ASPECT_ID);
      necessaryMIATypes.Add(VideoAspect.ASPECT_ID);
      necessaryMIATypes.Add(MovieAspect.ASPECT_ID);

      IList<MediaItem> items = GetMediaItems.GetMediaItemsByAspect(necessaryMIATypes);

      if (items.Count == 0)
        throw new BadRequestException("No Movies found");

      var output = new List<WebMovieBasic>();

      foreach (var item in items)
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
        webMovieBasic.Rating = Convert.ToSingle((double)movieAspects[MovieAspect.ATTR_TOTAL_RATING]);
        webMovieBasic.Type = WebMediaType.Movie;
        webMovieBasic.Watched = ((int)(item.Aspects[MediaAspect.ASPECT_ID][MediaAspect.ATTR_PLAYCOUNT] ?? 0) > 0);
        webMovieBasic.Rating = Convert.ToSingle((double)movieAspects[MovieAspect.ATTR_TOTAL_RATING]);
        //webTvEpisodeBasic.Path = ;
        //webTvEpisodeBasic.Artwork = ;
        //webMovieBasic.Year =;
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

        output.Add(webMovieBasic);
      }

      // sort and filter
      HttpParam httpParam = request.Param;
      string sort = httpParam["sort"].Value;
      string order = httpParam["order"].Value;
      string filter = httpParam["filter"].Value;
      if (sort != null && order != null)
      {
        WebSortField webSortField = (WebSortField)JsonConvert.DeserializeObject(sort, typeof(WebSortField));
        WebSortOrder webSortOrder = (WebSortOrder)JsonConvert.DeserializeObject(order, typeof(WebSortOrder));

        output = output.Filter(filter).SortWebMovieBasic(webSortField, webSortOrder).ToList();
      }
      else
        output = output.Filter(filter).ToList();

      return output;
    }

    internal static ILogger Logger
    {
      get { return ServiceRegistration.Get<ILogger>(); }
    }
  }
}
