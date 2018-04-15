#region Copyright (C) 2007-2017 Team MediaPortal

/*
    Copyright (C) 2007-2017 Team MediaPortal
    http://www.team-mediaportal.com

    This file is part of MediaPortal 2

    MediaPortal 2 is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    MediaPortal 2 is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with MediaPortal 2. If not, see <http://www.gnu.org/licenses/>.
*/

#endregion

using System;
using System.Collections.Generic;
using MediaPortal.Common;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Common.Services.ResourceAccess.RawUrlResourceProvider;
using MediaPortal.Common.SystemResolver;
using MediaPortal.UPnPRenderer.Players;
using MediaPortal.UPnPRenderer.UPnP;

namespace MediaPortal.UPnPRenderer.MediaItems
{
  static class UPnPMediaItemFactory
  {
    public static MediaItem CreateAudioItem(string resolvedPlaybackUrl)
    {
      var item = new MediaItem(Guid.Empty, new Dictionary<Guid, IList<MediaItemAspect>>
      {
        { ProviderResourceAspect.ASPECT_ID, new MediaItemAspect[] { new MultipleMediaItemAspect(ProviderResourceAspect.Metadata) }},
        { MediaAspect.ASPECT_ID, new MediaItemAspect[] { new SingleMediaItemAspect(MediaAspect.Metadata) }},
        { AudioAspect.ASPECT_ID, new MediaItemAspect[] { new SingleMediaItemAspect(AudioAspect.Metadata) }}
      });

      SetProviderResourceAspect(resolvedPlaybackUrl, item, UPnPRendererAudioPlayer.MIMETYPE);
      return item;
    }

    public static MediaItem CreateVideoItem(string resolvedPlaybackUrl)
    {
      var item = new MediaItem(Guid.Empty, new Dictionary<Guid, IList<MediaItemAspect>>
      {
        { ProviderResourceAspect.ASPECT_ID, new MediaItemAspect[] { new MultipleMediaItemAspect(ProviderResourceAspect.Metadata) }},
        { MediaAspect.ASPECT_ID, new MediaItemAspect[] { new SingleMediaItemAspect(MediaAspect.Metadata) }},
        { VideoAspect.ASPECT_ID, new MediaItemAspect[] { new SingleMediaItemAspect(VideoAspect.Metadata) }}
      });

      SetProviderResourceAspect(resolvedPlaybackUrl, item, UPnPRendererVideoPlayer.MIMETYPE);
      return item;
    }

    public static MediaItem CreateImageItem(string resolvedPlaybackUrl)
    {
      var item = new MediaItem(Guid.Empty, new Dictionary<Guid, IList<MediaItemAspect>>
      {
        { ProviderResourceAspect.ASPECT_ID, new MediaItemAspect[] { new MultipleMediaItemAspect(ProviderResourceAspect.Metadata) }},
        { MediaAspect.ASPECT_ID, new MediaItemAspect[] { new SingleMediaItemAspect(MediaAspect.Metadata) }},
        { ImageAspect.ASPECT_ID, new MediaItemAspect[] { new SingleMediaItemAspect(ImageAspect.Metadata) }}
      });

      SetProviderResourceAspect(resolvedPlaybackUrl, item, UPnPRendererImagePlayer.MIMETYPE);
      MediaItemAspect.SetAttribute(item.Aspects, ImageAspect.ATTR_ORIENTATION, 0);
      MediaItemAspect.SetAttribute(item.Aspects, MediaAspect.ATTR_TITLE, resolvedPlaybackUrl);
      return item;
    }

    private static void SetProviderResourceAspect(string resolvedPlaybackUrl, MediaItem item, string mimeType)
    {
      MultipleMediaItemAspect providerResourceAspect = MediaItemAspect.CreateAspect(item.Aspects, ProviderResourceAspect.Metadata);
      providerResourceAspect.SetAttribute(ProviderResourceAspect.ATTR_SYSTEM_ID, ServiceRegistration.Get<ISystemResolver>().LocalSystemId);
      providerResourceAspect.SetAttribute(ProviderResourceAspect.ATTR_RESOURCE_ACCESSOR_PATH, RawUrlResourceProvider.ToProviderResourcePath(resolvedPlaybackUrl).Serialize());
      providerResourceAspect.SetAttribute(ProviderResourceAspect.ATTR_MIME_TYPE, mimeType);
    }

    public static void SetAudioMetaData(this MediaItem item, DmapData metaData)
    {
      MediaItemAspect.SetAttribute(item.Aspects, MediaAspect.ATTR_TITLE, metaData.Title);
      MediaItemAspect.SetAttribute(item.Aspects, AudioAspect.ATTR_ALBUM, metaData.Album);
      MediaItemAspect.SetCollectionAttribute(item.Aspects, AudioAspect.ATTR_ARTISTS, metaData.Artists);
      foreach (string genre in metaData.Genres)
      {
        MultipleMediaItemAspect genreAspect = MediaItemAspect.CreateAspect(item.Aspects, GenreAspect.Metadata);
        genreAspect.SetAttribute(GenreAspect.ATTR_GENRE, genre);
      }
      MediaItemAspect.SetAttribute(item.Aspects, AudioAspect.ATTR_TRACK, metaData.OriginalTrackNumber);
      MediaItemAspect.SetAttribute(item.Aspects, AudioAspect.ATTR_NUMDISCS, metaData.OriginalDiscCount);
    }

    public static void SetVideoMetaData(this MediaItem item, DmapData metaData)
    {
      MediaItemAspect.SetAttribute(item.Aspects, MediaAspect.ATTR_TITLE, metaData.Title);
      MediaItemAspect.SetCollectionAttribute(item.Aspects, VideoAspect.ATTR_ACTORS, metaData.Actors);
      foreach (string genre in metaData.Genres)
      {
        MultipleMediaItemAspect genreAspect = MediaItemAspect.CreateAspect(item.Aspects, GenreAspect.Metadata);
        genreAspect.SetAttribute(GenreAspect.ATTR_GENRE, genre);
      }
      MediaItemAspect.SetCollectionAttribute(item.Aspects, VideoAspect.ATTR_DIRECTORS, metaData.Directors);
    }

    public static void SetImageMetaData(this MediaItem item, DmapData metaData)
    {
      MediaItemAspect.SetAttribute(item.Aspects, MediaAspect.ATTR_TITLE, metaData.Title);
    }

    public static void SetCover(this MediaItem item, byte[] imageData)
    {
      MediaItemAspect.SetAttribute(item.Aspects, ThumbnailLargeAspect.ATTR_THUMBNAIL, imageData);
    }

    public static void AddMetaDataToMediaItem(this MediaItem item, string metaData)
    {
      if (metaData == null)
        return;

      string coverUrl;
      DmapData dmapData = Utils.ExtractMetaDataFromDidlLite(metaData, out coverUrl);

      if (item.Aspects.ContainsKey(AudioAspect.ASPECT_ID))
        item.SetAudioMetaData(dmapData);
      if (item.Aspects.ContainsKey(VideoAspect.ASPECT_ID))
        item.SetVideoMetaData(dmapData);
      if (item.Aspects.ContainsKey(ImageAspect.ASPECT_ID))
        item.SetImageMetaData(dmapData);

      if (!String.IsNullOrEmpty(coverUrl))
        item.SetCover(Utils.DownloadImage(coverUrl));
    }
  }
}
