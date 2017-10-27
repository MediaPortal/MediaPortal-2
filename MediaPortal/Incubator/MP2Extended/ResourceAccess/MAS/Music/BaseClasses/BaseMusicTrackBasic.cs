using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Common.MediaManagement.MLQueries;
using MediaPortal.Common.ResourceAccess;
using MediaPortal.Plugins.MP2Extended.MAS.Music;
using MediaPortal.Utilities;
using MP2Extended.Extensions;
using MP2Extended.ResourceAccess.MAS.Music.BaseClasses;
using MP2Extended.ResourceAccess.MAS.Music.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MediaPortal.Plugins.MP2Extended.ResourceAccess.MAS.Music.BaseClasses
{
  internal class BaseMusicTrackBasic
  {
    protected static readonly IList<MediaItem> EMPTY_MEDIA_ITEMS_LIST = new List<MediaItem>();

    internal static ISet<Guid> BasicNecessaryMIATypeIds = new HashSet<Guid>
    {
      MediaAspect.ASPECT_ID,
      ProviderResourceAspect.ASPECT_ID,
      ImporterAspect.ASPECT_ID,
      AudioAspect.ASPECT_ID
    };

    internal static ISet<Guid> BasicOptionalMIATypeIds = new HashSet<Guid>
    {
      GenreAspect.ASPECT_ID,
      RelationshipAspect.ASPECT_ID
    };

    internal WebMusicTrackBasic MusicTrackBasic(MediaItem item, IDictionary<Guid, IList<MediaItem>> trackArtistMap = null,
      IDictionary<Guid, IList<MediaItem>> trackAlbumArtistMap = null)
    {
      MediaItemAspect trackAspect = item.GetAspect(AudioAspect.Metadata);
      MediaItemAspect mediaAspect = item.GetAspect(MediaAspect.Metadata);
      MediaItemAspect importerAspect = item.GetAspect(ImporterAspect.Metadata);

      IList<string> paths = item.PrimaryResources.Select(p =>
      {
        ResourcePath resourcePath = ResourcePath.Deserialize(p.GetAttributeValue<string>(ProviderResourceAspect.ATTR_RESOURCE_ACCESSOR_PATH));
        return resourcePath.PathSegments.Count > 0 ? StringUtils.RemovePrefixIfPresent(resourcePath.LastPathSegment.Path, "/") : null;
      }).Where(p => p != null).ToList();

      IList<MediaItem> trackArtistItems = GetAllArtistsForTrack(item.MediaItemId, PersonAspect.ROLE_ARTIST, trackArtistMap);
      IList<MediaItem> trackAlbumArtistItems = GetAllArtistsForTrack(item.MediaItemId, PersonAspect.ROLE_ALBUMARTIST, trackAlbumArtistMap);

      //Map artist name to item, this assumes that there won't be 2 artists with the same name on this track which shouldn't ever be the case
      IDictionary<string, MediaItem> artistNameToItem = ArtistHelper.MapNameToArtist(trackArtistItems);
      IDictionary<string, MediaItem> albumArtistNameToItem = ArtistHelper.MapNameToArtist(trackAlbumArtistItems);

      string albumArtist = trackAspect.GetCollectionAttribute<string>(AudioAspect.ATTR_ALBUMARTISTS)?.FirstOrDefault() ?? string.Empty;
      string albumArtistId = albumArtistNameToItem.TryGetValue(albumArtist, out MediaItem albumArtistItem) ? albumArtistItem.MediaItemId.ToString() : string.Empty;

      //Map artists from aspect to media items as the aspect artists should be sorted
      IList<string> artists = new List<string>();
      IList<string> artistIds = new List<string>();
      var aspectArtists = trackAspect.GetCollectionAttribute<string>(AudioAspect.ATTR_ARTISTS) ?? new List<string>();
      foreach (string aspectArtist in aspectArtists)
      {
        artists.Add(aspectArtist);
        artistIds.Add(artistNameToItem.TryGetValue(aspectArtist, out MediaItem artistItem) ? artistItem.MediaItemId.ToString() : string.Empty);
      }

      IList<MultipleMediaItemAspect> genres;
      if (!MediaItemAspect.TryGetAspects(item.Aspects, GenreAspect.Metadata, out genres))
        genres = new List<MultipleMediaItemAspect>();

      Guid? albumId = item.GetLinkedIdOrDefault(AudioAspect.ROLE_TRACK, AudioAlbumAspect.ROLE_ALBUM);
      
      return new WebMusicTrackBasic
      {
        PID = 0,
        Title = trackAspect.GetAttributeValue<string>(AudioAspect.ATTR_TRACKNAME),
        Id = item.MediaItemId.ToString(),
        Path = paths,
        Type = Common.WebMediaType.MusicTrack,
        TrackNumber = trackAspect.GetAttributeValue<int>(AudioAspect.ATTR_TRACK),
        Duration = Convert.ToInt32(trackAspect.GetAttributeValue<long>(AudioAspect.ATTR_DURATION)),
        DiscNumber = trackAspect.GetAttributeValue<int?>(AudioAspect.ATTR_DISCID) ?? 1,
        Album = trackAspect.GetAttributeValue<string>(AudioAspect.ATTR_ALBUM),
        AlbumId = albumId.HasValue ? albumId.Value.ToString() : string.Empty,
        Year = mediaAspect.GetAttributeValue<DateTime?>(MediaAspect.ATTR_RECORDINGTIME)?.Year ?? 0,
        Rating = Convert.ToSingle(trackAspect.GetAttributeValue<double>(AudioAspect.ATTR_TOTAL_RATING)),
        AlbumArtist = albumArtist,
        AlbumArtistId = albumArtistId,
        Artist = artists,
        ArtistId = artistIds,
        DateAdded = importerAspect.GetAttributeValue<DateTime>(ImporterAspect.ATTR_DATEADDED),        
        Genres = genres.Select(a => a.GetAttributeValue<string>(GenreAspect.ATTR_GENRE)).ToList(),
      };
    }

    /// <summary>
    /// Gets all artists for the specified track, or all tracks if <paramref name="trackId"/> is <c>null</c>.
    /// </summary>
    /// <param name="trackId"></param>
    /// <returns></returns>
    internal IList<MediaItem> GetAllArtistsForTrack(Guid? trackId, Guid artistRole, IDictionary<Guid, IList<MediaItem>> trackArtistMap = null)
    {
      if (trackId.HasValue && trackArtistMap != null)
        return trackArtistMap.TryGetValue(trackId.Value, out IList<MediaItem> artists) ? artists : EMPTY_MEDIA_ITEMS_LIST;
      
      Guid id = trackId ?? Guid.Empty;
      IFilter searchFilter = new RelationshipFilter(artistRole, AudioAspect.ROLE_TRACK, id);
      return GetMediaItems.Search(BaseMusicArtistBasic.BasicNecessaryMIATypeIds, BaseMusicArtistBasic.BasicOptionalMIATypeIds, searchFilter);
    }
  }
}
