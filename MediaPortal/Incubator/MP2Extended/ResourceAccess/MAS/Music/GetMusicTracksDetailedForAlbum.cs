using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Common.MediaManagement.MLQueries;
using MediaPortal.Plugins.MP2Extended.Attributes;
using MediaPortal.Plugins.MP2Extended.Common;
using MediaPortal.Plugins.MP2Extended.Extensions;
using MediaPortal.Plugins.MP2Extended.MAS.Music;
using MediaPortal.Plugins.MP2Extended.ResourceAccess.MAS.Music.BaseClasses;
using MP2Extended.ResourceAccess.MAS.Music.BaseClasses;
using MP2Extended.ResourceAccess.MAS.Music.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MediaPortal.Plugins.MP2Extended.ResourceAccess.MAS.Music
{
  [ApiFunctionDescription(Type = ApiFunctionDescription.FunctionType.Json, Summary = "")]
  [ApiFunctionParam(Name = "id", Type = typeof(Guid), Nullable = false)]
  [ApiFunctionParam(Name = "filter", Type = typeof(string), Nullable = true)]
  [ApiFunctionParam(Name = "sort", Type = typeof(WebSortField), Nullable = true)]
  [ApiFunctionParam(Name = "order", Type = typeof(WebSortOrder), Nullable = true)]
  internal class GetMusicTracksDetailedForAlbum : BaseMusicTrackDetailed
  {
    public IList<WebMusicTrackDetailed> Process(Guid id, string filter, WebSortField? sort, WebSortOrder? order)
    {
      IFilter searchFilter = new RelationshipFilter(AudioAspect.ROLE_TRACK, AudioAlbumAspect.ROLE_ALBUM, id);
      IList<MediaItem> items = GetMediaItems.Search(BasicNecessaryMIATypeIds, BasicOptionalMIATypeIds, searchFilter);

      IFilter trackIdFilter = new MediaItemIdFilter(items.Select(i => i.MediaItemId));
      IFilter artistSearchFilter = BooleanCombinationFilter.CombineFilters(BooleanOperator.Or,
        new FilteredRelationshipFilter(PersonAspect.ROLE_ARTIST, AudioAspect.ROLE_TRACK, trackIdFilter),
        new FilteredRelationshipFilter(PersonAspect.ROLE_ALBUMARTIST, AudioAspect.ROLE_TRACK, trackIdFilter));
      IList<MediaItem> artists = GetMediaItems.Search(BaseMusicArtistBasic.BasicNecessaryMIATypeIds, BaseMusicArtistBasic.BasicOptionalMIATypeIds, artistSearchFilter);

      IDictionary<Guid, IList<MediaItem>> trackArtistMap = ArtistHelper.MapTracksToArtists(artists);
      IDictionary<Guid, IList<MediaItem>> trackAlbumArtistMap = ArtistHelper.MapTracksToAlbumArtists(artists);

      var output = items.Select(i => MusicTrackDetailed(i, trackArtistMap, trackAlbumArtistMap))
        .Filter(filter);

      if (sort != null)
        output = output.AsQueryable().SortMediaItemList(sort, order);

      return output.ToList();
    }

    internal static ILogger Logger
    {
      get { return ServiceRegistration.Get<ILogger>(); }
    }
  }
}
