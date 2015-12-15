using System;
using System.Collections.Generic;
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

namespace MediaPortal.Plugins.MP2Extended.ResourceAccess.MAS.TvShow
{
  [ApiFunctionDescription(Type = ApiFunctionDescription.FunctionType.Json, Summary = "")]
  [ApiFunctionParam(Name = "id", Type = typeof(string), Nullable = false)]
  internal class GetTVEpisodeCountForSeason : IRequestMicroModuleHandler
  {
    public dynamic Process(IHttpRequest request, IHttpSession session)
    {
      HttpParam httpParam = request.Param;
      string id = httpParam["id"].Value;
      if (id == null)
        throw new BadRequestException("GetTVEpisodeCountForSeason: no id is null");

      // The ID looks like: {GUID-TvSHow:Season}
      string[] ids = id.Split(':');
      if (ids.Length < 2)
        throw new BadRequestException(String.Format("GetTVEpisodeCountForSeason: not enough ids: {0}", ids.Length));

      string showId = ids[0];
      string seasonId = ids[1];

      ISet<Guid> necessaryMIATypes = new HashSet<Guid>();
      necessaryMIATypes.Add(MediaAspect.ASPECT_ID);

      // this is the MediaItem for the show
      MediaItem showItem = GetMediaItems.GetMediaItemById(showId, necessaryMIATypes);

      if (showItem == null)
        throw new BadRequestException(String.Format("GetTVEpisodeCountForSeason: No MediaItem found with id: {0}", showId));

      string showName;
      try
      {
        showName = (string)showItem[MediaAspect.ASPECT_ID][MediaAspect.ATTR_TITLE];
      }
      catch (Exception ex)
      {
        throw new BadRequestException(String.Format("GetTVEpisodeCountForSeason: Couldn't convert Title: {0}", ex.Message));
      }

      int seasonNumber;
      if (!Int32.TryParse(seasonId, out seasonNumber))
      {
        throw new BadRequestException(String.Format("GetTVEpisodeCountForSeason: Couldn't convert SeasonId to int: {0}", seasonId));
      }

      // Get all episodes for this
      ISet<Guid> necessaryMIATypesEpisodes = new HashSet<Guid>();
      necessaryMIATypesEpisodes.Add(MediaAspect.ASPECT_ID);
      necessaryMIATypesEpisodes.Add(SeriesAspect.ASPECT_ID);

      IFilter searchFilter = BooleanCombinationFilter.CombineFilters(BooleanOperator.And,
        new RelationalFilter(SeriesAspect.ATTR_SEASON, RelationalOperator.EQ, seasonNumber),
        new RelationalFilter(SeriesAspect.ATTR_SERIESNAME, RelationalOperator.EQ, showName));
      MediaItemQuery searchQuery = new MediaItemQuery(necessaryMIATypesEpisodes, null, searchFilter);

      IList<MediaItem> episodes = ServiceRegistration.Get<IMediaLibrary>().Search(searchQuery, false);

      WebIntResult webIntResult = new WebIntResult { Result = episodes.Count };

      return webIntResult;
    }

    internal static ILogger Logger
    {
      get { return ServiceRegistration.Get<ILogger>(); }
    }
  }
}