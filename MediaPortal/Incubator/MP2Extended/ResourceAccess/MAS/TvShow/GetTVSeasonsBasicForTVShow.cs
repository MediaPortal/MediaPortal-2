using System;
using System.Collections.Generic;
using System.Linq;
using HttpServer;
using HttpServer.Exceptions;
using HttpServer.Sessions;
using MediaPortal.Backend.MediaLibrary;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Common.MediaManagement.MLQueries;
using MediaPortal.Plugins.MP2Extended.Attributes;
using MediaPortal.Plugins.MP2Extended.Common;
using MediaPortal.Plugins.MP2Extended.MAS.TvShow;
using Newtonsoft.Json;

namespace MediaPortal.Plugins.MP2Extended.ResourceAccess.MAS.TvShow
{
  // TODO: Add more details
  [ApiFunctionDescription(Type = ApiFunctionDescription.FunctionType.Json, Summary = "")]
  [ApiFunctionParam(Name = "id", Type = typeof(string), Nullable = false)]
  [ApiFunctionParam(Name = "sort", Type = typeof(WebSortField), Nullable = true)]
  [ApiFunctionParam(Name = "order", Type = typeof(WebSortOrder), Nullable = true)]
  internal class GetTVSeasonsBasicForTVShow : IRequestMicroModuleHandler
  {
    public dynamic Process(IHttpRequest request, IHttpSession session)
    {
      HttpParam httpParam = request.Param;
      string id = httpParam["id"].Value;
      if (id == null)
        throw new BadRequestException("GetTVSeasonsBasicForTVShow: id is null");

      ISet<Guid> necessaryMIATypes = new HashSet<Guid>();
      necessaryMIATypes.Add(MediaAspect.ASPECT_ID);
      necessaryMIATypes.Add(SeriesAspect.ASPECT_ID);
      necessaryMIATypes.Add(RelationshipAspect.ASPECT_ID);

      // this is the MediaItem for the show
      MediaItem item = GetMediaItems.GetMediaItemById(id, necessaryMIATypes);

      if (item == null)
        throw new BadRequestException(String.Format("GetTVSeasonsBasicForTVShow: No MediaItem found with id: {0}", id));

      // Get all seasons for this series
      ISet<Guid> necessaryMIATypesSeason = new HashSet<Guid>();
      necessaryMIATypesSeason.Add(MediaAspect.ASPECT_ID);
      necessaryMIATypesSeason.Add(SeasonAspect.ASPECT_ID);
      necessaryMIATypesSeason.Add(RelationshipAspect.ASPECT_ID);

      IFilter searchFilter = new RelationshipFilter(item.MediaItemId, SeriesAspect.ROLE_SERIES, SeasonAspect.ROLE_SEASON);
      MediaItemQuery searchQuery = new MediaItemQuery(necessaryMIATypesSeason, null, searchFilter);

      IList<MediaItem> seasons = ServiceRegistration.Get<IMediaLibrary>().Search(searchQuery, false);

      if (seasons.Count == 0)
        throw new BadRequestException("No seasons found");

      var output = new List<WebTVSeasonBasic>();

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

        WebTVSeasonBasic webTVSeasonBasic = new WebTVSeasonBasic
        {
          Title = (string)seasonAspect[SeasonAspect.ATTR_SERIES_SEASON],
          Id = season.MediaItemId.ToString(),
          ShowId = item.MediaItemId.ToString(),
          SeasonNumber = (int)seasonAspect[SeasonAspect.ATTR_SEASON],
          EpisodeCount = episodesInThisSeason.Count,
          UnwatchedEpisodeCount = episodesInThisSeasonUnwatched.Count,
          IsProtected = false,
          PID = 0
        };
        //webTVSeasonBasic.DateAdded;
        //webTVSeasonBasic.Year;

        output.Add(webTVSeasonBasic);
      }

      // sort
      string sort = httpParam["sort"].Value;
      string order = httpParam["order"].Value;
      if (sort != null && order != null)
      {
        WebSortField webSortField = (WebSortField)JsonConvert.DeserializeObject(sort, typeof(WebSortField));
        WebSortOrder webSortOrder = (WebSortOrder)JsonConvert.DeserializeObject(order, typeof(WebSortOrder));

        output = output.SortWebTVSeasonBasic(webSortField, webSortOrder).ToList();
      }

      return output;
    }

    internal static ILogger Logger
    {
      get { return ServiceRegistration.Get<ILogger>(); }
    }
  }
}