using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MediaPortal.Backend.MediaLibrary;
using MediaPortal.Common;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Common.MediaManagement.MLQueries;
using MediaPortal.Plugins.MP2Extended.MAS.Music;

namespace MediaPortal.Plugins.MP2Extended.ResourceAccess.MAS.Music.BaseClasses
{
  internal class BaseMusicAlbumBasic
  {
    internal WebMusicAlbumBasic MusicAlbumBasic(MediaItem item)
    {
      MediaItemAspect albumAspect = MediaItemAspect.GetAspect(item.Aspects, AudioAlbumAspect.Metadata);
      var artists = (HashSet<object>)albumAspect[AudioAlbumAspect.ATTR_ARTISTS];

      IList<MultipleMediaItemAspect> genres;
      if (!MediaItemAspect.TryGetAspects(item.Aspects, GenreAspect.Metadata, out genres))
        genres = new List<MultipleMediaItemAspect>();

      //var composers = (HashSet<object>)albumAspect[AudioAlbumAspect.ATTR_COMPOSERS];

      return new WebMusicAlbumBasic
      {
        PID = 0,
        Id = item.MediaItemId.ToString(),
        Artists = artists.Cast<string>().ToList(),
        Genres = genres.Select(a => a.GetAttributeValue<string>(GenreAspect.ATTR_GENRE)).ToList(),
        //Composer = composers.Cast<string>().ToList()
      };



    }
  }
}
