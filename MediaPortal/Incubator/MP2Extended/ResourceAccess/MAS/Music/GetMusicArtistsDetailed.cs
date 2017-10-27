using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Common.MediaManagement.MLQueries;
using MediaPortal.Plugins.MP2Extended.Attributes;
using MediaPortal.Plugins.MP2Extended.Common;
using MediaPortal.Plugins.MP2Extended.Extensions;
using MediaPortal.Plugins.MP2Extended.MAS.Music;
using MP2Extended.ResourceAccess.MAS.Music.BaseClasses;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MediaPortal.Plugins.MP2Extended.ResourceAccess.MAS.Music
{
  [ApiFunctionDescription(Type = ApiFunctionDescription.FunctionType.Json, Summary = "")]
  [ApiFunctionParam(Name = "filter", Type = typeof(string), Nullable = true)]
  [ApiFunctionParam(Name = "sort", Type = typeof(WebSortField), Nullable = true)]
  [ApiFunctionParam(Name = "order", Type = typeof(WebSortOrder), Nullable = true)]
  internal class GetMusicArtistsDetailed : BaseMusicArtistDetailed
  {
    public IList<WebMusicArtistDetailed> Process(string filter, WebSortField? sort, WebSortOrder? order)
    {
      IFilter searchFilter = BooleanCombinationFilter.CombineFilters(BooleanOperator.Or,
        new RelationshipFilter(PersonAspect.ROLE_ARTIST, AudioAspect.ROLE_TRACK, Guid.Empty),
        new RelationshipFilter(PersonAspect.ROLE_ALBUMARTIST, AudioAlbumAspect.ROLE_ALBUM, Guid.Empty));

      IList<MediaItem> items = GetMediaItems.Search(BasicNecessaryMIATypeIds, BasicOptionalMIATypeIds, searchFilter);

      if (items.Count == 0)
        return new List<WebMusicArtistDetailed>();

      var output = items.Select(item => MusicArtistDetailed(item))
        .Filter(filter);

      // sort
      if (sort != null && order != null)
        output = output.AsQueryable().SortMediaItemList(sort, order);

      return output.ToList();
    }

    internal static ILogger Logger
    {
      get { return ServiceRegistration.Get<ILogger>(); }
    }
  }
}
