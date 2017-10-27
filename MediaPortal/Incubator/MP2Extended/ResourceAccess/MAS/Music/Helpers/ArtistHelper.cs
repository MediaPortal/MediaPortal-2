using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MP2Extended.Extensions;
using System;
using System.Collections.Generic;

namespace MP2Extended.ResourceAccess.MAS.Music.Helpers
{
  internal static class ArtistHelper
  {
    /// <summary>
    /// Takes a collection of artist media items and creates a map of albums to artists.
    /// </summary>
    /// <param name="artists"></param>
    /// <returns></returns>
    internal static IDictionary<Guid, IList<MediaItem>> MapAlbumsToArtists(IList<MediaItem> artists)
    {
      //Map each artist to their respective albums
      return MapRelationsToItems(artists, PersonAspect.ROLE_ALBUMARTIST, AudioAlbumAspect.ROLE_ALBUM);
    }

    /// <summary>
    /// Takes a collection of album artist media items and creates a map of tracks to album artists.
    /// </summary>
    /// <param name="albumArtists"></param>
    /// <returns></returns>
    internal static IDictionary<Guid, IList<MediaItem>> MapTracksToAlbumArtists(IList<MediaItem> albumArtists)
    {
      //Map each artist to their respective albums
      return MapRelationsToItems(albumArtists, PersonAspect.ROLE_ALBUMARTIST, AudioAspect.ROLE_TRACK);
    }

    /// <summary>
    /// Takes a collection of artist media items and creates a map of tracks to artists.
    /// </summary>
    /// <param name="artists"></param>
    /// <returns></returns>
    internal static IDictionary<Guid, IList<MediaItem>> MapTracksToArtists(IList<MediaItem> artists)
    {
      //Map each artist to their respective albums
      return MapRelationsToItems(artists, PersonAspect.ROLE_ARTIST, AudioAspect.ROLE_TRACK);
    }

    /// <summary>
    /// Takes a collection of artist media items and creates a map of artist name to item.
    /// </summary>
    /// <param name="artists"></param>
    /// <returns></returns>
    internal static IDictionary<string, MediaItem> MapNameToArtist(IList<MediaItem> artists)
    {
      return MapAttributeToItem<string>(artists, PersonAspect.ATTR_PERSON_NAME);
    }

    private static IDictionary<Guid, IList<MediaItem>> MapRelationsToItems(IList<MediaItem> items, Guid role, Guid linkedRole)
    {
      IDictionary<Guid, IList<MediaItem>> relationToItemMap = new Dictionary<Guid, IList<MediaItem>>();
      foreach (MediaItem item in items)
      {
        foreach (Guid linkedId in item.GetLinkedIds(role, linkedRole))
        {
          IList<MediaItem> mappedList;
          if (!relationToItemMap.TryGetValue(linkedId, out mappedList))
            relationToItemMap[linkedId] = mappedList = new List<MediaItem>();
          mappedList.Add(item);
        }
      }
      return relationToItemMap;
    }

    private static IDictionary<T, MediaItem> MapAttributeToItem<T>(IList<MediaItem> items, MediaItemAspectMetadata.AttributeSpecification attribute)
    {
      IDictionary<T, MediaItem> attributeToItemMap = new Dictionary<T, MediaItem>();
      foreach (MediaItem item in items)
      {
        T attributeValue = item.GetAspect(attribute.ParentMIAM).GetAttributeValue<T>(attribute);
        if (!attributeToItemMap.ContainsKey(attributeValue))
          attributeToItemMap.Add(attributeValue, item);
      }
      return attributeToItemMap;
    }
  }
}
