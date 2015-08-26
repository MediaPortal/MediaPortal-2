using System;
using System.Collections.Generic;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Common;
using MediaPortal.Common.SystemResolver;
using MediaPortal.Common.Services.ResourceAccess.RawUrlResourceProvider;
using MediaPortal.Extensions.UPnPRenderer.Players;

namespace MediaPortal.Extensions.UPnPRenderer.MediaItems
{
  class VideoItem : MediaItem
	{
    public VideoItem(string resolvedPlaybackUrl)
        : base(Guid.Empty, new Dictionary<Guid, MediaItemAspect>()
    {
	    { ProviderResourceAspect.ASPECT_ID, new MediaItemAspect(ProviderResourceAspect.Metadata)},
	    { MediaAspect.ASPECT_ID, new MediaItemAspect(MediaAspect.Metadata) },
	    { VideoAspect.ASPECT_ID, new MediaItemAspect(VideoAspect.Metadata) }
    })
    {
        Aspects[ProviderResourceAspect.ASPECT_ID].SetAttribute(ProviderResourceAspect.ATTR_SYSTEM_ID, ServiceRegistration.Get<ISystemResolver>().LocalSystemId);

        Aspects[ProviderResourceAspect.ASPECT_ID].SetAttribute(ProviderResourceAspect.ATTR_RESOURCE_ACCESSOR_PATH, RawUrlResourceProvider.ToProviderResourcePath(resolvedPlaybackUrl).Serialize());
        Aspects[MediaAspect.ASPECT_ID].SetAttribute(MediaAspect.ATTR_MIME_TYPE, UPnPRendererVideoPlayer.MIMETYPE);
    }

    public void SetMetaData(DmapData metaData)
    {
      Aspects[MediaAspect.ASPECT_ID].SetAttribute(MediaAspect.ATTR_TITLE, metaData.Title);
      MediaItemAspect videoAspect = Aspects[VideoAspect.ASPECT_ID];
      videoAspect.SetCollectionAttribute(VideoAspect.ATTR_ACTORS, metaData.Actors);
      videoAspect.SetCollectionAttribute(VideoAspect.ATTR_GENRES, metaData.Genres);
      videoAspect.SetCollectionAttribute(VideoAspect.ATTR_DIRECTORS, metaData.Directors);
    }

    public void SetCover(byte[] imageData)
    {
      MediaItemAspect.SetAttribute(Aspects, ThumbnailLargeAspect.ATTR_THUMBNAIL, imageData);
    }
	}
}
