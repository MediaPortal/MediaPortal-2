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

using MediaPortal.Plugins.Transcoding.Service.Metadata;

namespace MediaPortal.Plugins.Transcoding.Service.Profiles.MediaInfo
{
  public class ImageInfo
  {
    public ImageContainer ImageContainerType = ImageContainer.Unknown;
    public PixelFormat PixelFormatType = PixelFormat.Unknown;
    public QualityMode QualityType = QualityMode.Default;
    public bool ForceInheritance = false;

    public bool Matches(MetadataContainer info)
    {
      bool bPass = true;
      bPass &= (ImageContainerType == ImageContainer.Unknown || ImageContainerType == info.Metadata.ImageContainerType);
      bPass &= (PixelFormatType == PixelFormat.Unknown || PixelFormatType == info.Image.PixelFormatType);

      return bPass;
    }

    public bool Matches(ImageInfo imageItem)
    {
      return ImageContainerType == imageItem.ImageContainerType &&
        PixelFormatType == imageItem.PixelFormatType &&
        QualityType == imageItem.QualityType;
    }
  }
}
