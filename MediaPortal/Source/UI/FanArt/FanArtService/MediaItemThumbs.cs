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
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using MediaPortal.Backend.MediaLibrary;
using MediaPortal.Common;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Common.MediaManagement.MLQueries;
using MediaPortal.Common.ResourceAccess;
using MediaPortal.Extensions.UserServices.FanArtService.Interfaces;
using MediaPortal.Common.FanArt;

namespace MediaPortal.Extensions.UserServices.FanArtService
{
  public class MediaItemThumbs : IBinaryFanArtProvider
  {
    public FanArtProviderSource Source { get { return FanArtProviderSource.Database; } }

    public bool TryGetFanArt(string mediaType, string fanArtType, string name, int maxWidth, int maxHeight, bool singleRandom, out IList<FanArtImage> result)
    {
      result = null;
      Guid mediaItemId;

      var isImage = mediaType == FanArtMediaTypes.Image;
      if (!Guid.TryParse(name, out mediaItemId) ||
        mediaType != FanArtMediaTypes.Undefined && !isImage ||
        fanArtType != FanArtTypes.Thumbnail)
        return false;

      IMediaLibrary mediaLibrary = ServiceRegistration.Get<IMediaLibrary>(false);
      if (mediaLibrary == null)
        return false;

      // Try to load thumbnail from ML
      List<Guid> thumbGuids = new List<Guid> { ThumbnailLargeAspect.ASPECT_ID };
      // Check for Image's rotation info
      if (isImage)
        thumbGuids.Add(ImageAspect.ASPECT_ID);

      IFilter filter = new MediaItemIdFilter(mediaItemId);
      IList<MediaItem> items = mediaLibrary.Search(new MediaItemQuery(thumbGuids, filter), false, null, true);
      if (items == null || items.Count == 0)
        return false;

      MediaItem mediaItem = items.First();
      byte[] textureData;
      if (!MediaItemAspect.TryGetAttribute(mediaItem.Aspects, ThumbnailLargeAspect.ATTR_THUMBNAIL, out textureData))
        return false;

      if (isImage)
        textureData = AutoRotateThumb(mediaItem, textureData);

      // Only one record required
      result = new List<FanArtImage> { new FanArtImage(name, textureData) };
      return true;
    }

    private static byte[] AutoRotateThumb(MediaItem mediaItem, byte[] textureData)
    {
      ImageRotation miRotation;
      bool flipX;
      bool flipY;
      if (ImageAspect.GetOrientationMetadata(mediaItem, out miRotation, out flipX, out flipY) && (miRotation != ImageRotation.Rot_0))
      {
        try
        {
          using (MemoryStream rotatedStream = new MemoryStream())
          using (MemoryStream inputStream = new MemoryStream(textureData))
          using (Image bitmap = Image.FromStream(inputStream))
          {
            if (miRotation == ImageRotation.Rot_180)
              bitmap.RotateFlip(RotateFlipType.Rotate180FlipNone);
            if (miRotation == ImageRotation.Rot_90)
              bitmap.RotateFlip(RotateFlipType.Rotate90FlipNone);
            if (miRotation == ImageRotation.Rot_270)
              bitmap.RotateFlip(RotateFlipType.Rotate270FlipNone);
            bitmap.Save(rotatedStream, ImageFormat.Jpeg);
            textureData = rotatedStream.ToArray();
          }
        }
        catch (Exception)
        {
        }
      }
      return textureData;
    }

    public bool TryGetFanArt(string mediaType, string fanArtType, string name, int maxWidth, int maxHeight, bool singleRandom, out IList<IResourceLocator> result)
    {
      result = null;
      return false;
    }
  }
}
