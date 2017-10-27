using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Common.MediaManagement.MLQueries;
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
  internal class GetMusicTracksDetailed : BaseMusicTrackDetailed
  {
    public IList<WebMusicTrackDetailed> Process(string filter, WebSortField? sort, WebSortOrder? order)
    {
      IList<MediaItem> items = GetMediaItems.GetMediaItemsByAspect(BasicNecessaryMIATypeIds, BasicOptionalMIATypeIds, null);

      //For bulk track requests request all artists up front to avoid having to do an individual request for every track

      IFilter searchFilter = BooleanCombinationFilter.CombineFilters(BooleanOperator.Or,
        new RelationshipFilter(PersonAspect.ROLE_ARTIST, AudioAspect.ROLE_TRACK, Guid.Empty),
        new RelationshipFilter(PersonAspect.ROLE_ALBUMARTIST, AudioAspect.ROLE_TRACK, Guid.Empty));
      IList<MediaItem> artists = GetMediaItems.Search(BaseMusicArtistBasic.BasicNecessaryMIATypeIds, BaseMusicArtistBasic.BasicOptionalMIATypeIds, searchFilter);

      //Map each artist to their respective tracks
      IDictionary<Guid, IList<MediaItem>> trackToArtistMap = ArtistHelper.MapTracksToArtists(artists);
      IDictionary<Guid, IList<MediaItem>> trackToAlbumArtistMap = ArtistHelper.MapTracksToAlbumArtists(artists);

      var output = items.Select(item => MusicTrackDetailed(item, trackToArtistMap, trackToAlbumArtistMap))
        .Filter(filter);

      //sort
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
