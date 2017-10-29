using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Plugins.MP2Extended.Common;
using MediaPortal.Plugins.MP2Extended.MAS.Music;
using MP2Extended.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MP2Extended.ResourceAccess.MAS.Music.BaseClasses
{
  internal class BaseMusicArtistBasic
  {
    internal static ISet<Guid> BasicNecessaryMIATypeIds = new HashSet<Guid>
    {
      MediaAspect.ASPECT_ID,
      PersonAspect.ASPECT_ID
    };

    internal static ISet<Guid> BasicOptionalMIATypeIds = new HashSet<Guid>
    {
      RelationshipAspect.ASPECT_ID,
    };

    internal WebMusicArtistBasic MusicArtistBasic(MediaItem item)
    {
      return new WebMusicArtistBasic()
      {
        Title = item.GetAspect(PersonAspect.Metadata).GetAttributeValue<string>(PersonAspect.ATTR_PERSON_NAME),
        Id = item.MediaItemId.ToString(),
        HasAlbums = item.GetLinkedIds(PersonAspect.ROLE_ALBUMARTIST, AudioAlbumAspect.ROLE_ALBUM).Any(),
        Artwork = GetFanart.GetArtwork(item.MediaItemId, WebMediaType.MusicArtist)
      };
    }
  }
}
