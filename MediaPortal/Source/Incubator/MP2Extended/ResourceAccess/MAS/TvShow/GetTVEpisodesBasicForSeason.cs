using System;
using System.Collections.Generic;
using System.Diagnostics;
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
  [ApiFunctionParam(Name = "id", Type = typeof(Guid), Nullable = false)]
  [ApiFunctionParam(Name = "sort", Type = typeof(WebSortField), Nullable = true)]
  [ApiFunctionParam(Name = "order", Type = typeof(WebSortOrder), Nullable = true)]
  internal class GetTVEpisodesBasicForSeason : BaseEpisodeBasic
  {
    public IList<WebTVEpisodeBasic> Process(Guid id, WebSortField? sort, WebSortOrder? order)
    {
      Stopwatch watch = new Stopwatch();

      watch.Start();
      ISet<Guid> necessaryMIATypes = new HashSet<Guid>();
      necessaryMIATypes.Add(MediaAspect.ASPECT_ID);
      necessaryMIATypes.Add(SeasonAspect.ASPECT_ID);
      necessaryMIATypes.Add(RelationshipAspect.ASPECT_ID);

      // this is the MediaItem for the season
      MediaItem item = GetMediaItems.GetMediaItemById(id, necessaryMIATypes);
      watch.Stop();
      Logger.Info("ShowItem: {0}", watch.Elapsed);
      watch.Reset();

      if (item == null)
        throw new BadRequestException(String.Format("GetTVEpisodeCountForSeason: No MediaItem found with id: {0}", id));

      // Get all episodes for this season
      ISet<Guid> necessaryMIATypesEpisodes = new HashSet<Guid>();
      necessaryMIATypesEpisodes.Add(MediaAspect.ASPECT_ID);
      necessaryMIATypesEpisodes.Add(EpisodeAspect.ASPECT_ID);
      necessaryMIATypesEpisodes.Add(ImporterAspect.ASPECT_ID);
      necessaryMIATypesEpisodes.Add(ProviderResourceAspect.ASPECT_ID);

      IFilter searchFilter = new RelationshipFilter(item.MediaItemId, SeasonAspect.ROLE_SEASON, EpisodeAspect.ROLE_EPISODE);
      MediaItemQuery searchQuery = new MediaItemQuery(necessaryMIATypesEpisodes, null, searchFilter);

      IList<MediaItem> episodes = ServiceRegistration.Get<IMediaLibrary>().Search(searchQuery, false);

      if (episodes.Count == 0)
        throw new BadRequestException("No Tv Episodes found");

      watch.Stop();
      Logger.Info("Episodes: {0}", watch.Elapsed);
      watch.Reset();
      watch.Start();

      var output = episodes.Select(episode => EpisodeBasic(episode)).ToList();

      watch.Stop();
      Logger.Info("Create output: {0}", watch.Elapsed);

      watch.Reset();
      watch.Start();

      // sort
      if (sort != null && order != null)
      {
        output = output.SortWebTVEpisodeBasic(sort, order).ToList();
      }
      watch.Stop();
      Logger.Info("Sort: {0}", watch.Elapsed);

      return output;
    }

    internal static ILogger Logger
    {
      get { return ServiceRegistration.Get<ILogger>(); }
    }
  }
}