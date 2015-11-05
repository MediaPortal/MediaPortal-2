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
      string seriesId = httpParam["id"].Value;
      if (seriesId == null)
        throw new BadRequestException("GetTVEpisodeCountForSeason: id is null");

      ISet<Guid> necessaryMIATypes = new HashSet<Guid>();
      necessaryMIATypes.Add(MediaAspect.ASPECT_ID);
      necessaryMIATypes.Add(RelationshipAspect.ASPECT_ID);

      // this is the MediaItem for the show
      MediaItem item = GetMediaItems.GetMediaItemById(seriesId, necessaryMIATypes);

      if (item == null)
        throw new BadRequestException(String.Format("GetTVEpisodeCountForSeason: No MediaItem found with id: {0}", seriesId));

      int episodeCount = item[RelationshipAspect.ASPECT_ID].Count(x => x.GetAttributeValue<Guid>(RelationshipAspect.ATTR_ROLE) == SeasonAspect.ROLE_SEASON && x.GetAttributeValue<Guid>(RelationshipAspect.ATTR_LINKED_ROLE) == EpisodeAspect.ROLE_EPISODE);

      WebIntResult webIntResult = new WebIntResult { Result = episodeCount };

      return webIntResult;
    }

    internal static ILogger Logger
    {
      get { return ServiceRegistration.Get<ILogger>(); }
    }
  }
}