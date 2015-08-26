using MediaPortal.Extensions.UPnPRenderer.Players;
using MediaPortal.Common;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Common.Services.ResourceAccess.RawUrlResourceProvider;
using MediaPortal.Common.SystemResolver;
using System;
using System.Collections.Generic;

namespace MediaPortal.Extensions.UPnPRenderer.MediaItems
{
    class AudioItem : MediaItem
    {
        //PlayerSettings playerSettings;

      public AudioItem(string resolvedPlaybackUrl)
            : base(Guid.Empty, new Dictionary<Guid, MediaItemAspect>()
			{
				{ ProviderResourceAspect.ASPECT_ID, new MediaItemAspect(ProviderResourceAspect.Metadata)},
				{ MediaAspect.ASPECT_ID, new MediaItemAspect(MediaAspect.Metadata) },
				{ AudioAspect.ASPECT_ID, new MediaItemAspect(AudioAspect.Metadata) }
			})
        {
            //this.playerSettings = playerSettings;
            Aspects[ProviderResourceAspect.ASPECT_ID].SetAttribute(ProviderResourceAspect.ATTR_SYSTEM_ID, ServiceRegistration.Get<ISystemResolver>().LocalSystemId);
            Aspects[ProviderResourceAspect.ASPECT_ID].SetAttribute(ProviderResourceAspect.ATTR_RESOURCE_ACCESSOR_PATH, RawUrlResourceProvider.ToProviderResourcePath(resolvedPlaybackUrl).Serialize());
            Aspects[MediaAspect.ASPECT_ID].SetAttribute(MediaAspect.ATTR_MIME_TYPE, UPnPRendererAudioPlayer.MIMETYPE);
        }

        //public PlayerSettings PlayerSettings { get { return playerSettings; } }

        public void SetMetaData(DmapData metaData)
        {
            Aspects[MediaAspect.ASPECT_ID].SetAttribute(MediaAspect.ATTR_TITLE, metaData.Title);
            MediaItemAspect audioAspect = Aspects[AudioAspect.ASPECT_ID];
            audioAspect.SetAttribute(AudioAspect.ATTR_ALBUM, metaData.Album);
            audioAspect.SetCollectionAttribute(AudioAspect.ATTR_ARTISTS, metaData.Artists);
            audioAspect.SetCollectionAttribute(AudioAspect.ATTR_GENRES, metaData.Genres);
            audioAspect.SetAttribute(AudioAspect.ATTR_TRACK, metaData.OriginalTrackNumber);
            audioAspect.SetAttribute(AudioAspect.ATTR_NUMDISCS, metaData.OriginalDiscCount);
        }

        public void SetCover(byte[] imageData)
        {
            MediaItemAspect.SetAttribute(Aspects, ThumbnailLargeAspect.ATTR_THUMBNAIL, imageData);
        }
    }
}