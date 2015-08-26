using MediaPortal.Extensions.UPnPRenderer.Players;
using MediaPortal.Common;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Common.Services.ResourceAccess.RawUrlResourceProvider;
using MediaPortal.Common.SystemResolver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MediaPortal.Extensions.UPnPRenderer.MediaItems
{
    class ImageItem : MediaItem
    {
        public ImageItem(string imageId, byte[] imageData)
            : base(Guid.Empty, new Dictionary<Guid, MediaItemAspect>()
			{
				{ ProviderResourceAspect.ASPECT_ID, new MediaItemAspect(ProviderResourceAspect.Metadata)},
				{ MediaAspect.ASPECT_ID, new MediaItemAspect(MediaAspect.Metadata) },
				{ ImageAspect.ASPECT_ID, new MediaItemAspect(ImageAspect.Metadata) }
			})
        {
            ImageId = imageId;
            ImageData = imageData;
            Aspects[ProviderResourceAspect.ASPECT_ID].SetAttribute(ProviderResourceAspect.ATTR_SYSTEM_ID, ServiceRegistration.Get<ISystemResolver>().LocalSystemId);
            Aspects[ProviderResourceAspect.ASPECT_ID].SetAttribute(ProviderResourceAspect.ATTR_RESOURCE_ACCESSOR_PATH, RawUrlResourceProvider.ToProviderResourcePath(UPnPRendererImagePlayer.DUMMY_FILE).Serialize());
            Aspects[MediaAspect.ASPECT_ID].SetAttribute(MediaAspect.ATTR_MIME_TYPE, UPnPRendererImagePlayer.MIMETYPE);
            Aspects[ImageAspect.ASPECT_ID].SetAttribute(ImageAspect.ATTR_ORIENTATION, 0);
        }

        public string ImageId { get; protected set; }
        public byte[] ImageData { get; protected set; }



        public void SetMetaData(DmapData metaData)
        {
          Aspects[MediaAspect.ASPECT_ID].SetAttribute(MediaAspect.ATTR_TITLE, metaData.Title);
          MediaItemAspect videoAspect = Aspects[ImageAspect.ASPECT_ID];
          //videoAspect.SetCollectionAttribute(VideoAspect.ATTR_ACTORS, metaData.Actors);
        }

        public void SetCover(byte[] imageData)
        {
          MediaItemAspect.SetAttribute(Aspects, ThumbnailLargeAspect.ATTR_THUMBNAIL, imageData);
        }
    }
}
