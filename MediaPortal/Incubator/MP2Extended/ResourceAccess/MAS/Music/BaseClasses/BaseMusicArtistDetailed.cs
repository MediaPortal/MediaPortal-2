using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Plugins.MP2Extended.MAS.Music;
using MP2Extended.Extensions;

namespace MP2Extended.ResourceAccess.MAS.Music.BaseClasses
{
  internal class BaseMusicArtistDetailed : BaseMusicArtistBasic
  {
    internal WebMusicArtistDetailed MusicArtistDetailed(MediaItem item)
    {
      var basic = MusicArtistBasic(item);

      return new WebMusicArtistDetailed()
      {
        Title = basic.Title,
        Id = basic.Id,
        HasAlbums = basic.HasAlbums,
        Biography = item.GetAspect(PersonAspect.Metadata).GetAttributeValue<string>(PersonAspect.ATTR_BIOGRAPHY)
      };
    }
  }
}
