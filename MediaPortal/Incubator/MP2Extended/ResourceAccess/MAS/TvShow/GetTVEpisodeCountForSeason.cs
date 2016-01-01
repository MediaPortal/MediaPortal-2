using System;
using System.Collections.Generic;
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

namespace MediaPortal.Plugins.MP2Extended.ResourceAccess.MAS.TvShow
{
  [ApiFunctionDescription(Type = ApiFunctionDescription.FunctionType.Json, Summary = "")]
  [ApiFunctionParam(Name = "id", Type = typeof(Guid), Nullable = false)]
  internal class GetTVEpisodeCountForSeason
  {
    public WebIntResult Process(Guid id)
    {
      ISet<Guid> necessaryMIATypes = new HashSet<Guid>();
      necessaryMIATypes.Add(MediaAspect.ASPECT_ID);
      necessaryMIATypes.Add(RelationshipAspect.ASPECT_ID);

      // this is the MediaItem for the season
      MediaItem item = GetMediaItems.GetMediaItemById(id, necessaryMIATypes);

      if (item == null)
        throw new BadRequestException(String.Format("GetTVEpisodeCountForSeason: No MediaItem found with id: {0}", id));

      IFilter episodeFilter = new RelationshipFilter(item.MediaItemId, SeasonAspect.ROLE_SEASON, EpisodeAspect.ROLE_EPISODE);
      MediaItemQuery episodeQuery = new MediaItemQuery(necessaryMIATypes, null, episodeFilter);

      IList<MediaItem> episodesInThisSeason = ServiceRegistration.Get<IMediaLibrary>().Search(episodeQuery, false);

      return new WebIntResult { Result = episodesInThisSeason.Count };
    }

    internal static ILogger Logger
    {
      get { return ServiceRegistration.Get<ILogger>(); }
    }
  }
}