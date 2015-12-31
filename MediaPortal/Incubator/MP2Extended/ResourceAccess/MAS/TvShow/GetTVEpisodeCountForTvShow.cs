using System;
using System.Collections.Generic;
using System.Linq;
using HttpServer;
using MediaPortal.Backend.MediaLibrary;
using HttpServer.Sessions;
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
  internal class GetTVEpisodeCountForTVShow
  {
    public WebIntResult Process(Guid id)
    {
      ISet<Guid> necessaryMIATypes = new HashSet<Guid>();
      necessaryMIATypes.Add(MediaAspect.ASPECT_ID);
      necessaryMIATypes.Add(SeriesAspect.ASPECT_ID);
      necessaryMIATypes.Add(RelationshipAspect.ASPECT_ID);

      // this is the MediaItem from the TvShow
      MediaItem item = GetMediaItems.GetMediaItemById(id, necessaryMIATypes);
      if (item == null)
        throw new BadRequestException(String.Format("GetTVEpisodeCountForTvShow: No MediaItem found with id: {0}", id));

      // Get all seasons for this series
      ISet<Guid> necessaryMIATypesSeason = new HashSet<Guid>();
      necessaryMIATypesSeason.Add(MediaAspect.ASPECT_ID);
      necessaryMIATypesSeason.Add(SeasonAspect.ASPECT_ID);
      necessaryMIATypesSeason.Add(RelationshipAspect.ASPECT_ID);

      IFilter searchFilter = new RelationshipFilter(item.MediaItemId, SeriesAspect.ROLE_SERIES, SeasonAspect.ROLE_SEASON);
      MediaItemQuery searchQuery = new MediaItemQuery(necessaryMIATypesSeason, null, searchFilter);

      IList<MediaItem> seasons = ServiceRegistration.Get<IMediaLibrary>().Search(searchQuery, false);

      WebIntResult webIntResult = new WebIntResult { Result = seasons.Sum(season => MediaItemAspect.GetRelationships(season.Aspects, SeasonAspect.ROLE_SEASON, EpisodeAspect.ROLE_EPISODE).Count) };

      return webIntResult;
    }

    internal static ILogger Logger
    {
      get { return ServiceRegistration.Get<ILogger>(); }
    }
  }
}