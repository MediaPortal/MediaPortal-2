using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Plugins.MP2Extended.MAS.Music;
using MP2Extended.ResourceAccess.MAS.Music.BaseClasses;
using MP2Extended.ResourceAccess.MAS.Music.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MediaPortal.Plugins.MP2Extended.ResourceAccess.MAS.Music.BaseClasses
{
  internal class BaseMusicTrackDetailed : BaseMusicTrackBasic
  {
    internal WebMusicTrackDetailed MusicTrackDetailed(MediaItem item, IDictionary<Guid, IList<MediaItem>> trackArtistMap = null,
      IDictionary<Guid, IList<MediaItem>> trackAlbumArtistMap = null)
    {
      IList<MediaItem> trackArtistItems = GetAllArtistsForTrack(item.MediaItemId, PersonAspect.ROLE_ARTIST, trackArtistMap);
      IList<MediaItem> trackAlbumArtistItems = GetAllArtistsForTrack(item.MediaItemId, PersonAspect.ROLE_ALBUMARTIST, trackAlbumArtistMap);

      //Map artist name to item, this assumes that there won't be 2 artists with the same name on this track which shouldn't ever be the case
      IDictionary<string, MediaItem> artistNameToItem = ArtistHelper.MapNameToArtist(trackArtistItems);
      IDictionary<string, MediaItem> albumArtistNameToItem = ArtistHelper.MapNameToArtist(trackAlbumArtistItems);

      trackArtistMap = ArtistHelper.MapTracksToArtists(trackArtistItems);
      trackAlbumArtistMap = ArtistHelper.MapTracksToAlbumArtists(trackAlbumArtistItems);

      var basic = MusicTrackBasic(item, trackArtistMap, trackAlbumArtistMap);

      var detailed = new WebMusicTrackDetailed
      {
        PID = 0,
        Title = basic.Title,
        Id = basic.Id,
        Path = basic.Path,
        Type = basic.Type,
        TrackNumber = basic.TrackNumber,
        Duration = basic.Duration,
        DiscNumber = basic.DiscNumber,
        Album = basic.Album,
        AlbumId = basic.AlbumId,
        Year = basic.Year,
        Rating = basic.Rating,
        AlbumArtist = basic.AlbumArtist,
        AlbumArtistId = basic.AlbumArtistId,
        Artist = basic.Artist,
        ArtistId = basic.ArtistId,
        DateAdded = basic.DateAdded,
        Genres = basic.Genres,
        Artwork = basic.Artwork
      };

      BaseMusicArtistBasic artistBasic = new BaseMusicArtistBasic();
      detailed.AlbumArtistObject = !string.IsNullOrEmpty(detailed.AlbumArtist) && albumArtistNameToItem.TryGetValue(detailed.AlbumArtist, out MediaItem albumArtist) ?
        artistBasic.MusicArtistBasic(albumArtist) : new WebMusicArtistBasic() { Title = detailed.AlbumArtist };

      if (detailed.Artist != null)
        detailed.Artists = detailed.Artist.Select(a => !string.IsNullOrEmpty(a) && artistNameToItem.TryGetValue(a, out MediaItem artist) ?
          artistBasic.MusicArtistBasic(artist) : new WebMusicArtistBasic() { Title = a }).ToList();

      return detailed;
    }
  }
}
