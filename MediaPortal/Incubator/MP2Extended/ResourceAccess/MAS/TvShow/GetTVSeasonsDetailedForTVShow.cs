using System;
using System.Collections.Generic;
using System.Linq;
using HttpServer;
using HttpServer.Sessions;
using MediaPortal.Backend.MediaLibrary;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Common.MediaManagement.MLQueries;
using MediaPortal.Plugins.MP2Extended.Attributes;
using MediaPortal.Plugins.MP2Extended.Common;
using MediaPortal.Plugins.MP2Extended.Exceptions;
using MediaPortal.Plugins.MP2Extended.MAS.TvShow;
using Newtonsoft.Json;

namespace MediaPortal.Plugins.MP2Extended.ResourceAccess.MAS.TvShow
{
  // TODO: Add more details
  [ApiFunctionDescription(Type = ApiFunctionDescription.FunctionType.Json, Summary = "")]
  [ApiFunctionParam(Name = "id", Type = typeof(Guid), Nullable = false)]
  [ApiFunctionParam(Name = "sort", Type = typeof(WebSortField), Nullable = true)]
  [ApiFunctionParam(Name = "order", Type = typeof(WebSortOrder), Nullable = true)]
  internal class GetTVSeasonsDetailedForTVShow
  {
    public IList<WebTVSeasonDetailed> Process(Guid id, WebSortField? sort, WebSortOrder? order)
    {
      // Get all seasons for this series
      ISet<Guid> necessaryMIATypesSeason = new HashSet<Guid>();
      necessaryMIATypesSeason.Add(MediaAspect.ASPECT_ID);
      necessaryMIATypesSeason.Add(SeasonAspect.ASPECT_ID);
      necessaryMIATypesSeason.Add(RelationshipAspect.ASPECT_ID);

      IFilter searchFilter = new RelationshipFilter(id, SeriesAspect.ROLE_SERIES, SeasonAspect.ROLE_SEASON);
      MediaItemQuery searchQuery = new MediaItemQuery(necessaryMIATypesSeason, null, searchFilter);

      IList<MediaItem> seasons = ServiceRegistration.Get<IMediaLibrary>().Search(searchQuery, false);

      if (seasons.Count == 0)
        throw new BadRequestException("No seasons found");

      var output = new List<WebTVSeasonDetailed>();

      foreach (var season in seasons)
      {
        // Get all episodes for this season
        ISet<Guid> necessaryMIATypesEpisode = new HashSet<Guid>();
        necessaryMIATypesEpisode.Add(MediaAspect.ASPECT_ID);
        necessaryMIATypesEpisode.Add(EpisodeAspect.ASPECT_ID);
        necessaryMIATypesEpisode.Add(RelationshipAspect.ASPECT_ID);

        IFilter episodeFilter = new RelationshipFilter(season.MediaItemId, SeasonAspect.ROLE_SEASON, EpisodeAspect.ROLE_EPISODE);
        MediaItemQuery episodeQuery = new MediaItemQuery(necessaryMIATypesEpisode, null, episodeFilter);

        IList<MediaItem> episodesInThisSeason = ServiceRegistration.Get<IMediaLibrary>().Search(episodeQuery, false);

        var seasonAspect = season[SeasonAspect.Metadata];

        var episodesInThisSeasonUnwatched = episodesInThisSeason.ToList().FindAll(x => x[MediaAspect.Metadata][MediaAspect.ATTR_PLAYCOUNT] == null || (int)x[MediaAspect.Metadata][MediaAspect.ATTR_PLAYCOUNT] == 0);

        WebTVSeasonDetailed webTVSeasonDetailed = new WebTVSeasonDetailed
        {
          Title = (string)seasonAspect[SeasonAspect.ATTR_SERIES_SEASON],
          Id = season.MediaItemId.ToString(),
          ShowId = id.ToString(),
          SeasonNumber = (int)seasonAspect[SeasonAspect.ATTR_SEASON],
          EpisodeCount = episodesInThisSeason.Count,
          UnwatchedEpisodeCount = episodesInThisSeasonUnwatched.Count,
          IsProtected = false,
          PID = 0
        };
        //webTVSeasonBasic.DateAdded;
        //webTVSeasonBasic.Year;
        // Artwork

        output.Add(webTVSeasonDetailed);
      }

      // sort
      if (sort != null && order != null)
      {
        output = output.SortWebTVSeasonDetailed(sort, order).ToList();
      }

      return output;
    }

    internal static ILogger Logger
    {
      get { return ServiceRegistration.Get<ILogger>(); }
    }
  }
}