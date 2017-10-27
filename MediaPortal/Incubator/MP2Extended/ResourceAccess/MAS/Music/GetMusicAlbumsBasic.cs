using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Common.MediaManagement.MLQueries;
using MediaPortal.Plugins.MP2Extended.Common;
using MediaPortal.Plugins.MP2Extended.Extensions;
using MediaPortal.Plugins.MP2Extended.MAS.Music;
using MediaPortal.Plugins.MP2Extended.ResourceAccess.MAS.Music.BaseClasses;
using MP2Extended.ResourceAccess.MAS.Music.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MediaPortal.Plugins.MP2Extended.ResourceAccess.MAS.Music
{
  internal class GetMusicAlbumsBasic : BaseMusicAlbumBasic
  {
    public IList<WebMusicAlbumBasic> Process(string filter, WebSortField? sort, WebSortOrder? order)
    {
      IList<MediaItem> items = GetMediaItems.GetMediaItemsByAspect(BasicNecessaryMIATypeIds, BasicOptionalMIATypeIds, null);

      //For bulk album requests request all album artists up front to avoid having to do an individual request for every album
      IList<MediaItem> artists = GetAllArtistsForAlbum(null);

      //Map each album artist to their respective albums
      IDictionary<Guid, IList<MediaItem>> albumToArtistMap = ArtistHelper.MapAlbumsToArtists(artists);

      var output = items.Select(item =>MusicAlbumBasic(item, albumToArtistMap))
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
