using System;
using System.Collections.Generic;
using HttpServer;
using HttpServer.Exceptions;
using HttpServer.Sessions;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Plugins.MP2Extended.Attributes;
using MediaPortal.Plugins.MP2Extended.MAS.TvShow;

namespace MediaPortal.Plugins.MP2Extended.ResourceAccess.MAS.TvShow
{
  // TODO: We don't have so much information about a season right now. We could get it by a hack from the episodes, but
  // we should wait for teh MIA rework.
  [ApiFunctionDescription(Type = ApiFunctionDescription.FunctionType.Json, Summary = "")]
  [ApiFunctionParam(Name = "id", Type = typeof(string), Nullable = false)]
  internal class GetTVShowDetailedById : IRequestMicroModuleHandler
  {
    public dynamic Process(IHttpRequest request, IHttpSession session)
    {
      HttpParam httpParam = request.Param;
      string id = httpParam["id"].Value;
      if (id == null)
        throw new BadRequestException("GetTVShowDetailedById: no id is null");

      ISet<Guid> necessaryMIATypes = new HashSet<Guid>();
      necessaryMIATypes.Add(MediaAspect.ASPECT_ID);

      MediaItem item = GetMediaItems.GetMediaItemById(id, necessaryMIATypes);

      if (item == null)
        throw new BadRequestException(String.Format("GetTVShowDetailedById: No MediaItem found with id: {0}", id));

      string seasonTitle = (string)item[MediaAspect.ASPECT_ID][MediaAspect.ATTR_TITLE];


      // Get all episodes for this season to count them
      ISet<Guid> necessaryMIATypesEpisodes = new HashSet<Guid>();
      necessaryMIATypes.Add(MediaAspect.ASPECT_ID);
      necessaryMIATypes.Add(SeriesAspect.ASPECT_ID);

      IList<MediaItem> episodes = GetMediaItems.GetMediaItemsByString(seasonTitle, necessaryMIATypesEpisodes, null, SeriesAspect.ATTR_SERIESNAME, null);

      WebTVShowDetailed webTVShowDetailed = new WebTVShowDetailed
      {
        Title = seasonTitle,
        EpisodeCount = episodes.Count,
      };

      return webTVShowDetailed;
    }

    internal static ILogger Logger
    {
      get { return ServiceRegistration.Get<ILogger>(); }
    }
  }
}