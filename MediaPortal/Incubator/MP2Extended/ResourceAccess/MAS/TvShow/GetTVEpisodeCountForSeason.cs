using System;
using System.Collections.Generic;
using System.Linq;
using HttpServer;
using HttpServer.Exceptions;
using MediaPortal.Backend.MediaLibrary;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Common.MediaManagement.MLQueries;
using MediaPortal.Plugins.MP2Extended.Common;

namespace MediaPortal.Plugins.MP2Extended.ResourceAccess.MAS.TvShow
{
  internal class GetTVEpisodeCountForSeason : IRequestMicroModuleHandler
  {
    public dynamic Process(IHttpRequest request)
    {
      HttpParam httpParam = request.Param;
      string id = httpParam["id"].Value;
      // The ID looks like: {GUID-Episode}
      if (id == null)
        throw new BadRequestException("GetTVEpisodeCountForSeason: no id is null");

      ISet<Guid> necessaryMIATypes = new HashSet<Guid>();
      necessaryMIATypes.Add(MediaAspect.ASPECT_ID);

      // this is the MediaItem for the show
      MediaItem showItem = GetMediaItems.GetMediaItemById(id, necessaryMIATypes);

      if (showItem == null)
        throw new BadRequestException(String.Format("GetTVEpisodeCountForSeason: No MediaItem found with id: {0}", id));

      // Get all episodes for this
      ISet<Guid> necessaryMIATypesEpisodes = new HashSet<Guid>();
      necessaryMIATypesEpisodes.Add(MediaAspect.ASPECT_ID);
      necessaryMIATypesEpisodes.Add(SeasonAspect.ASPECT_ID);
      necessaryMIATypesEpisodes.Add(RelationshipAspect.ASPECT_ID);

      IFilter searchFilter = new RelationshipFilter(showItem.MediaItemId, EpisodeAspect.ROLE_EPISODE, SeasonAspect.ROLE_SEASON);
      MediaItemQuery searchQuery = new MediaItemQuery(necessaryMIATypesEpisodes, null, searchFilter);

      MediaItem season = ServiceRegistration.Get<IMediaLibrary>().Search(searchQuery, false).FirstOrDefault();
      if(season == null)
        throw new BadRequestException(String.Format("GetTVEpisodeCountForSeason: No season for episode id: {0}", id));

      int count = season[RelationshipAspect.ASPECT_ID].Count(x =>
        x.GetAttributeValue<Guid>(RelationshipAspect.ATTR_ROLE) == SeasonAspect.ROLE_SEASON
        && x.GetAttributeValue<Guid>(RelationshipAspect.ATTR_LINKED_ROLE) == EpisodeAspect.ROLE_EPISODE);

      WebIntResult webIntResult = new WebIntResult { Result = count };

      return webIntResult;
    }

    internal static ILogger Logger
    {
      get { return ServiceRegistration.Get<ILogger>(); }
    }
  }
}