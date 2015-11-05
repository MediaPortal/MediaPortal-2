using System;
using System.Collections.Generic;
using System.Linq;
using HttpServer;
using HttpServer.Exceptions;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Plugins.MP2Extended.MAS.TvShow;

namespace MediaPortal.Plugins.MP2Extended.ResourceAccess.MAS.TvShow
{
  internal class GetTVShowDetailedById : IRequestMicroModuleHandler
  {
    public dynamic Process(IHttpRequest request)
    {
      HttpParam httpParam = request.Param;
      string id = httpParam["id"].Value;
      if (id == null)
        throw new BadRequestException("GetTVShowDetailedById: no id is null");

      ISet<Guid> necessaryMIATypes = new HashSet<Guid>();
      necessaryMIATypes.Add(MediaAspect.ASPECT_ID);
      necessaryMIATypes.Add(RelationshipAspect.ASPECT_ID);

      MediaItem item = GetMediaItems.GetMediaItemById(id, necessaryMIATypes);

      if (item == null)
        throw new BadRequestException(String.Format("GetTVShowDetailedById: No MediaItem found with id: {0}", id));

      SingleMediaItemAspect mediaAspect = MediaItemAspect.GetAspect(item.Aspects, MediaAspect.Metadata);
      string seasonTitle = (string)mediaAspect[MediaAspect.ATTR_TITLE];


      // Get all episodes for this season to count them
      int episodeCount = item[RelationshipAspect.ASPECT_ID].Count(x => x.GetAttributeValue<Guid>(RelationshipAspect.ATTR_ROLE) == SeriesAspect.ROLE_SERIES && x.GetAttributeValue<Guid>(RelationshipAspect.ATTR_LINKED_ROLE) == EpisodeAspect.ROLE_EPISODE);

      WebTVShowDetailed webTVShowDetailed = new WebTVShowDetailed
      {
        Title = seasonTitle,
        EpisodeCount = episodeCount
      };

      return webTVShowDetailed;
    }

    internal static ILogger Logger
    {
      get { return ServiceRegistration.Get<ILogger>(); }
    }
  }
}