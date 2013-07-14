using System;
using System.Collections.Generic;
using MediaPortal.Backend.MediaLibrary;
using MediaPortal.Common;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Common.MediaManagement.MLQueries;
using MediaPortal.Extensions.UserServices.FanArtService.Interfaces;

namespace MediaPortal.Extensions.UserServices.FanArtService
{
  class AlbumThumbs : IBinaryFanArtProvider
  {
    private static readonly Guid[] NECESSARY_MIAS = new Guid[]
      {
        AudioAspect.ASPECT_ID,
        ThumbnailLargeAspect.ASPECT_ID,
      };

    #region Implementation of IFanArtProvider

    public bool TryGetFanArt(FanArtConstants.FanArtMediaType mediaType, FanArtConstants.FanArtType fanArtType, string name, int maxWidth, int maxHeight, bool singleRandom, out IList<string> result)
    {
      throw new NotImplementedException("Use IBinaryFanArtProvider's method.");
    }

    public bool TryGetFanArt(FanArtConstants.FanArtMediaType mediaType, FanArtConstants.FanArtType fanArtType, string name, int maxWidth, int maxHeight, bool singleRandom, out IList<FanArtImage> result)
    {
      result = null;
      if (mediaType != FanArtConstants.FanArtMediaType.Album || fanArtType != FanArtConstants.FanArtType.Poster || string.IsNullOrEmpty(name))
        return false;

      IMediaLibrary mediaLibrary = ServiceRegistration.Get<IMediaLibrary>(false);
      if (mediaLibrary == null)
        return false;

      IFilter filter = new RelationalFilter(AudioAspect.ATTR_ALBUM, RelationalOperator.EQ, name);
      MediaItemQuery query = new MediaItemQuery(NECESSARY_MIAS, filter);
      var items = mediaLibrary.Search(query, false);
      result = new List<FanArtImage>();
      foreach (var mediaItem in items)
      {
        byte[] textureData;
        if (!MediaItemAspect.TryGetAttribute(mediaItem.Aspects, ThumbnailLargeAspect.ATTR_THUMBNAIL, out textureData))
          continue;

        // Only one record required
        result.Add(new FanArtImage(name, textureData));
        return true;
      }
      return true;
    }

    #endregion
  }
}
