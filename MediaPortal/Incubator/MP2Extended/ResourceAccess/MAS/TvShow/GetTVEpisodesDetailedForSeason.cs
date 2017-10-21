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
using MediaPortal.Plugins.MP2Extended.ResourceAccess.MAS.TvShow.BaseClasses;
using Newtonsoft.Json;

namespace MediaPortal.Plugins.MP2Extended.ResourceAccess.MAS.TvShow
{
  [ApiFunctionDescription(Type = ApiFunctionDescription.FunctionType.Json, Summary = "")]
  [ApiFunctionParam(Name = "id", Type = typeof(string), Nullable = false)]
  [ApiFunctionParam(Name = "sort", Type = typeof(WebSortField), Nullable = true)]
  [ApiFunctionParam(Name = "order", Type = typeof(WebSortOrder), Nullable = true)]
  internal class GetTVEpisodesDetailedForSeason : BaseEpisodesDetailed
  {
    public IList<WebTVEpisodeDetailed> Process(Guid id, WebSortField? sort, WebSortOrder? order)
    {
      ISet<Guid> necessaryMIATypes = new HashSet<Guid>();
      necessaryMIATypes.Add(MediaAspect.ASPECT_ID);
      necessaryMIATypes.Add(RelationshipAspect.ASPECT_ID);

      // this is the MediaItem for the season
      MediaItem item = GetMediaItems.GetMediaItemById(id, necessaryMIATypes);

      if (item == null)
        throw new BadRequestException(String.Format("GetTVEpisodeCountForSeason: No MediaItem found with id: {0}", id));

      ISet<Guid> necessaryEpisodesMiaTypes = new HashSet<Guid>();
      necessaryEpisodesMiaTypes.Add(MediaAspect.ASPECT_ID);
      necessaryEpisodesMiaTypes.Add(ProviderResourceAspect.ASPECT_ID);
      necessaryEpisodesMiaTypes.Add(ImporterAspect.ASPECT_ID);
      necessaryEpisodesMiaTypes.Add(VideoAspect.ASPECT_ID);
      necessaryEpisodesMiaTypes.Add(EpisodeAspect.ASPECT_ID);

      IFilter episodeFilter = new RelationshipFilter(item.MediaItemId, SeasonAspect.ROLE_SEASON, EpisodeAspect.ROLE_EPISODE);
      MediaItemQuery episodeQuery = new MediaItemQuery(necessaryEpisodesMiaTypes, null, episodeFilter);

      IList<MediaItem> episodes = ServiceRegistration.Get<IMediaLibrary>().Search(episodeQuery, false);

      if (episodes.Count == 0)
        throw new BadRequestException("No Tv Episodes found");

      var output = episodes.Select(episode => EpisodeDetailed(episode)).ToList();

      // sort
      if (sort != null && order != null)
      {
        output = output.SortWebTVEpisodeDetailed(sort, order).ToList();
      }

      return output;
    }

    internal static ILogger Logger
    {
      get { return ServiceRegistration.Get<ILogger>(); }
    }
  }
}