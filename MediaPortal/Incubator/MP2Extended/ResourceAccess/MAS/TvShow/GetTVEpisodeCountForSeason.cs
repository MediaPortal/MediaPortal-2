using System;
using System.Collections.Generic;
using HttpServer;
using HttpServer.Sessions;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Plugins.MP2Extended.Attributes;
using MediaPortal.Plugins.MP2Extended.Common;
using MediaPortal.Plugins.MP2Extended.Exceptions;

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
        throw new BadRequestException("GetTVEpisodeCountForSeason: id is null");

      ISet<Guid> necessaryMIATypes = new HashSet<Guid>();
      necessaryMIATypes.Add(MediaAspect.ASPECT_ID);
      necessaryMIATypes.Add(RelationshipAspect.ASPECT_ID);

      // this is the MediaItem for the season
      MediaItem item = GetMediaItems.GetMediaItemById(id, necessaryMIATypes);

      if (item == null)
        throw new BadRequestException(String.Format("GetTVEpisodeCountForSeason: No MediaItem found with id: {0}", id));

      return new WebIntResult { Result = MediaItemAspect.GetRelationships(item.Aspects, SeriesAspect.ROLE_SERIES, SeasonAspect.ROLE_SEASON).Count };
    }

    internal static ILogger Logger
    {
      get { return ServiceRegistration.Get<ILogger>(); }
    }
  }
}