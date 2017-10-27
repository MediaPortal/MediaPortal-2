using HttpServer.Exceptions;
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
  internal class GetMusicAlbumsBasicForArtist : BaseMusicAlbumBasic
  {
    public IList<WebMusicAlbumBasic> Process(Guid id, string filter, WebSortField? sort, WebSortOrder? order)
    {
      IFilter searchFilter = new RelationshipFilter(AudioAlbumAspect.ROLE_ALBUM, PersonAspect.ROLE_ALBUMARTIST, id);
      IList<MediaItem> items = GetMediaItems.Search(BasicNecessaryMIATypeIds, BasicOptionalMIATypeIds, searchFilter);

      if (items.Count == 0)
        throw new BadRequestException("No albums found");

      //Get all artists for the found albums
      IFilter artistSearchFilter = new FilteredRelationshipFilter(PersonAspect.ROLE_ALBUMARTIST, AudioAlbumAspect.ROLE_ALBUM,
        new MediaItemIdFilter(items.Select(mi => mi.MediaItemId)));
      IList<MediaItem> artists = GetMediaItems.Search(BaseMusicArtistBasic.BasicNecessaryMIATypeIds, BaseMusicArtistBasic.BasicOptionalMIATypeIds, artistSearchFilter);

      //Map each album artist to their respective albums
      IDictionary<Guid, IList<MediaItem>> albumToArtistMap = ArtistHelper.MapAlbumsToArtists(artists);

      var output = items.Select(item => MusicAlbumBasic(item, albumToArtistMap))
        .Filter(filter);

      //sort
      if (sort != null)
        output = output.AsQueryable().SortMediaItemList(sort, order);

      return output.ToList();
    }
  }
}
