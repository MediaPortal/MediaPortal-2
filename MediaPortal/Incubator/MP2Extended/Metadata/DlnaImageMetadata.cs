#region Copyright (C) 2007-2012 Team MediaPortal

/*
    Copyright (C) 2007-2012 Team MediaPortal
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
using MediaPortal.Common.ResourceAccess;
using MediaPortal.Common.Services.ResourceAccess.StreamedResourceToLocalFsAccessBridge;
using MediaPortal.Plugins.Transcoding.Aspects;
using MediaPortal.Plugins.Transcoding.Service;

namespace MediaPortal.Plugins.MP2Extended.Metadata
{
  public class DlnaImageMetadata
  {
    public static MetadataContainer ParseMediaItem(MediaItem item)
    {
      MetadataContainer info = new MetadataContainer();
      IResourceAccessor mediaItemAccessor = item.GetResourceLocator().CreateAccessor();
      if (mediaItemAccessor is IFileSystemResourceAccessor)
      {
        using (var fsra = (IFileSystemResourceAccessor)mediaItemAccessor.Clone())
        {
          if (!fsra.IsFile)
            return null;
          using (var lfsra = StreamedResourceToLocalFsAccessBridge.GetLocalFsResourceAccessor(fsra))
          {
            info.Metadata.Source = lfsra;
            info.Metadata.Size = lfsra.Size;
          }
        }
      }

      if (item.Aspects.ContainsKey(TranscodeItemImageAspect.ASPECT_ID) == true)
      {
        object oValue = null;
        oValue = item.Aspects[TranscodeItemImageAspect.ASPECT_ID].GetAttributeValue(TranscodeItemImageAspect.ATTR_CONTAINER);
        if (oValue != null && string.IsNullOrEmpty(oValue.ToString()) == false)
        {
          info.Metadata.ImageContainerType = (ImageContainer)Enum.Parse(typeof(ImageContainer), oValue.ToString());
        }
        oValue = item.Aspects[TranscodeItemImageAspect.ASPECT_ID].GetAttributeValue(TranscodeItemImageAspect.ATTR_PIXEL_FORMAT);
        if (oValue != null && string.IsNullOrEmpty(oValue.ToString()) == false)
        {
          info.Image.PixelFormatType = (PixelFormat)Enum.Parse(typeof(PixelFormat), oValue.ToString());
        }
        if (item.Aspects.ContainsKey(ImageAspect.ASPECT_ID) == true)
        {
          oValue = item.Aspects[ImageAspect.ASPECT_ID].GetAttributeValue(ImageAspect.ATTR_HEIGHT);
          if (oValue != null)
          {
            info.Image.Height = Convert.ToInt32(oValue);
          }
          oValue = item.Aspects[ImageAspect.ASPECT_ID].GetAttributeValue(ImageAspect.ATTR_WIDTH);
          if (oValue != null)
          {
            info.Image.Width = Convert.ToInt32(oValue);
          }
          oValue = item.Aspects[ImageAspect.ASPECT_ID].GetAttributeValue(ImageAspect.ATTR_ORIENTATION);
          if (oValue != null)
          {
            info.Image.Orientation = Convert.ToInt32(oValue);
          }
        }
        if (item.Aspects.ContainsKey(MediaAspect.ASPECT_ID) == true)
        {
          oValue = item.Aspects[MediaAspect.ASPECT_ID].GetAttributeValue(MediaAspect.ATTR_MIME_TYPE);
          if (oValue != null && string.IsNullOrEmpty(oValue.ToString()) == false)
          {
            info.Metadata.Mime = oValue.ToString();
          }
        }
      }
      return info;
    }
  }
}
