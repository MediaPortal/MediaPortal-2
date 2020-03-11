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

using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Plugins.MP2Extended.MAS.Picture;
using MP2Extended.Extensions;

namespace MediaPortal.Plugins.MP2Extended.ResourceAccess.MAS.Picture.BaseClasses
{
  class BasePictureDetailed : BasePictureBasic
  {
    internal static WebPictureDetailed PictureDetailed(MediaItem item)
    {
      MediaItemAspect imageAspects = item.GetAspect(ImageAspect.Metadata);

      WebPictureBasic webPictureBasic = PictureBasic(item);
      WebPictureDetailed webPictureDetailed = new WebPictureDetailed
      {
        Type = webPictureBasic.Type,
        DateAdded = webPictureBasic.DateAdded,
        Id = item.MediaItemId.ToString(),
        Title = webPictureBasic.Title,
        DateTaken = webPictureBasic.DateTaken,
        Path = webPictureBasic.Path,
        Artwork = webPictureBasic.Artwork,
        Categories = webPictureBasic.Categories,
        Width = imageAspects.GetAttributeValue<int?>(ImageAspect.ATTR_WIDTH)?.ToString(),
        Height = imageAspects.GetAttributeValue<int?>(ImageAspect.ATTR_HEIGHT)?.ToString(),
        CameraModel = imageAspects.GetAttributeValue<string>(ImageAspect.ATTR_MODEL),
        CameraManufacturer = imageAspects.GetAttributeValue<string>(ImageAspect.ATTR_MAKE),
        PID = webPictureBasic.PID
      };
      if (double.TryParse(webPictureDetailed.Width, out double width) && double.TryParse(webPictureDetailed.Height, out double height))
        webPictureDetailed.Mpixel = (width * height) / 1000000.0;

      return webPictureDetailed;
    }
  }
}
