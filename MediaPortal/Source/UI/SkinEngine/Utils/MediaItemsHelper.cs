#region Copyright (C) 2007-2013 Team MediaPortal

/*
    Copyright (C) 2007-2013 Team MediaPortal
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
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.UI.SkinEngine.Controls.ImageSources;
using MediaPortal.UI.SkinEngine.Rendering;

namespace MediaPortal.UI.SkinEngine.Utils
{
  public static class MediaItemsHelper
  {
    /// <summary>
    /// Constructs a <see cref="BinaryTextureImageSource"/> for thumbnails of the given size from MediaItems.
    /// </summary>
    /// <param name="mediaItem">MediaItem to create thumbnail for.</param>
    /// <param name="thumbnailSize">Requested thumbnail size.</param>
    public static BinaryTextureImageSource CreateThumbnailImageSource(MediaItem mediaItem, int thumbnailSize)
    {
      Guid id = mediaItem.MediaItemId;
      // Local media items don't have an item id
      string key = (id == Guid.Empty ? Guid.NewGuid() : id).ToString();
      byte[] textureData = null;

      // Each resolution is cached separately. If we read cache only and our favourite resolution is not yet in cache,
      // we try to find any other existing.
      if (thumbnailSize <= 96)
      {
        if (mediaItem.Aspects.ContainsKey(ThumbnailSmallAspect.ASPECT_ID))
          textureData = (byte[]) mediaItem.Aspects[ThumbnailSmallAspect.ASPECT_ID].GetAttributeValue(ThumbnailSmallAspect.ATTR_THUMBNAIL);

        if (textureData == null && mediaItem.Aspects.ContainsKey(ThumbnailLargeAspect.ASPECT_ID))
          textureData = (byte[]) mediaItem.Aspects[ThumbnailLargeAspect.ASPECT_ID].GetAttributeValue(ThumbnailLargeAspect.ATTR_THUMBNAIL);
      }
      else
      {
        if (mediaItem.Aspects.ContainsKey(ThumbnailLargeAspect.ASPECT_ID))
          textureData = (byte[]) mediaItem.Aspects[ThumbnailLargeAspect.ASPECT_ID].GetAttributeValue(ThumbnailLargeAspect.ATTR_THUMBNAIL);
        
        if (textureData == null && mediaItem.Aspects.ContainsKey(ThumbnailSmallAspect.ASPECT_ID))
          textureData = (byte[]) mediaItem.Aspects[ThumbnailSmallAspect.ASPECT_ID].GetAttributeValue(ThumbnailSmallAspect.ATTR_THUMBNAIL);
      }
      ImageRotation miRotation;
      bool flipX;
      bool flipY;
      ImageAspect.GetOrientationMetadata(mediaItem, out miRotation, out flipX, out flipY);
      RightAngledRotation rotation = RotationTranslator.TranslateToRightAngledRotation(miRotation);
      return new BinaryTextureImageSource(textureData, rotation, key);
    }
  }
}