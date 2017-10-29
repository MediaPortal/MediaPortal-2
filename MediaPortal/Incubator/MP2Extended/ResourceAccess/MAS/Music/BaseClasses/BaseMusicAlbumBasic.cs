using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Common.MediaManagement.MLQueries;
using MediaPortal.Plugins.MP2Extended.Common;
using MediaPortal.Plugins.MP2Extended.MAS.Music;
using MP2Extended.Extensions;
using MP2Extended.ResourceAccess;
using MP2Extended.ResourceAccess.MAS.Music.BaseClasses;
using MP2Extended.ResourceAccess.MAS.Music.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MediaPortal.Plugins.MP2Extended.ResourceAccess.MAS.Music.BaseClasses
{
  internal class BaseMusicAlbumBasic
  {
    protected static readonly IList<MediaItem> EMPTY_MEDIA_ITEMS_LIST = new List<MediaItem>();

    internal static ISet<Guid> BasicNecessaryMIATypeIds = new HashSet<Guid>
    {
      MediaAspect.ASPECT_ID,
      ImporterAspect.ASPECT_ID,
      AudioAlbumAspect.ASPECT_ID
    };

    internal static ISet<Guid> BasicOptionalMIATypeIds = new HashSet<Guid>
    {
      GenreAspect.ASPECT_ID,
    };

    internal WebMusicAlbumBasic MusicAlbumBasic(MediaItem item, IDictionary<Guid, IList<MediaItem>> albumArtistMap = null)
    {
      MediaItemAspect albumAspect = item.GetAspect(AudioAlbumAspect.Metadata);
      MediaItemAspect mediaAspect = item.GetAspect(MediaAspect.Metadata);
      MediaItemAspect importerAspect = item.GetAspect(ImporterAspect.Metadata);

      IList<MediaItem> albumArtistItems = GetAllArtistsForAlbum(item.MediaItemId, albumArtistMap);

      //Map artist name to item, this assumes that there won't be 2 artists with the same name on this album which shouldn't ever be the case
      IDictionary<string, MediaItem> artistNameToItem = ArtistHelper.MapNameToArtist(albumArtistItems);

      IList<string> artists = new List<string>();
      IList<string> artistIds = new List<string>();

      //Map artists from aspect to media items as the aspect artists should be sorted
      var aspectArtists = albumAspect.GetCollectionAttribute<string>(AudioAlbumAspect.ATTR_ARTISTS) ?? new List<string>();
      foreach (string aspectArtist in aspectArtists)
      {
        artists.Add(aspectArtist);
        artistIds.Add(artistNameToItem.TryGetValue(aspectArtist, out MediaItem artistItem) ? artistItem.MediaItemId.ToString() : string.Empty);
      }

      IList<MultipleMediaItemAspect> genres;
      if (!MediaItemAspect.TryGetAspects(item.Aspects, GenreAspect.Metadata, out genres))
        genres = new List<MultipleMediaItemAspect>();
      
      return new WebMusicAlbumBasic
      {
        PID = 0,
        Title = albumAspect.GetAttributeValue<string>(AudioAlbumAspect.ATTR_ALBUM),
        Id = item.MediaItemId.ToString(),
        Year = mediaAspect.GetAttributeValue<DateTime?>(MediaAspect.ATTR_RECORDINGTIME)?.Year ?? 0,
        Rating = Convert.ToSingle(albumAspect.GetAttributeValue<double>(AudioAlbumAspect.ATTR_TOTAL_RATING)),
        AlbumArtist = artists.FirstOrDefault() ?? string.Empty,
        AlbumArtistId = artistIds.FirstOrDefault() ?? string.Empty,
        Artists = artists,
        ArtistsId = artistIds,
        DateAdded = importerAspect.GetAttributeValue<DateTime>(ImporterAspect.ATTR_DATEADDED),        
        Genres = genres.Select(a => a.GetAttributeValue<string>(GenreAspect.ATTR_GENRE)).ToList(),
        Artwork = GetFanart.GetArtwork(item.MediaItemId, WebMediaType.MusicAlbum)
      };
    }

    /// <summary>
    /// Gets all artist for the specified album, or all albums if <paramref name="albumId"/> is <c>null</c>.
    /// </summary>
    /// <param name="albumId"></param>
    /// <returns></returns>
    internal IList<MediaItem> GetAllArtistsForAlbum(Guid? albumId, IDictionary<Guid, IList<MediaItem>> albumArtistMap = null)
    {
      if (albumId.HasValue && albumArtistMap != null)
        return albumArtistMap.TryGetValue(albumId.Value, out IList<MediaItem> artists) ? artists : EMPTY_MEDIA_ITEMS_LIST;

      Guid id = albumId ?? Guid.Empty;
      IFilter searchFilter = new RelationshipFilter(PersonAspect.ROLE_ALBUMARTIST, AudioAlbumAspect.ROLE_ALBUM, id);
      return GetMediaItems.Search(BaseMusicArtistBasic.BasicNecessaryMIATypeIds, BaseMusicArtistBasic.BasicOptionalMIATypeIds, searchFilter);
    }
  }
}
