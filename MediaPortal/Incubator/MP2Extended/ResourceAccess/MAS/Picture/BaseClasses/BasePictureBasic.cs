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
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Common.ResourceAccess;
using MediaPortal.Plugins.MP2Extended.Common;
using MediaPortal.Plugins.MP2Extended.MAS;
using MediaPortal.Plugins.MP2Extended.MAS.Picture;
using MediaPortal.Utilities;
using MP2Extended.Extensions;

namespace MediaPortal.Plugins.MP2Extended.ResourceAccess.MAS.Picture.BaseClasses
{
  class BasePictureBasic
  {
    internal static ISet<Guid> BasicNecessaryMIATypeIds = new HashSet<Guid>
    {
      MediaAspect.ASPECT_ID,
      ImporterAspect.ASPECT_ID,
      ProviderResourceAspect.ASPECT_ID,
      ImageAspect.ASPECT_ID
    };

    internal static ISet<Guid> BasicOptionalMIATypeIds = new HashSet<Guid>
    {
    };

    internal static WebPictureBasic PictureBasic(MediaItem item)
    {
      MediaItemAspect imageAspects = item.GetAspect(ImageAspect.Metadata);

      WebPictureBasic webPictureBasic = new WebPictureBasic
      {
        Type = WebMediaType.Picture,
        DateAdded = (DateTime)item.GetAspect(ImporterAspect.Metadata).GetAttributeValue(ImporterAspect.ATTR_DATEADDED),
        Id = item.MediaItemId.ToString(),
        Title = (string)item.GetAspect(MediaAspect.Metadata).GetAttributeValue(MediaAspect.ATTR_TITLE),
        DateTaken = (DateTime)item.GetAspect(MediaAspect.Metadata)[MediaAspect.ATTR_RECORDINGTIME],
        Path = ResourceAccessUtils.GetPaths(item),
        Artwork = ResourceAccessUtils.GetWebArtwork(item),
      };

      webPictureBasic.Categories = new[] 
      {
        new WebCategory { Title = (webPictureBasic.DateTaken).ToString("yyyy"), Id = (webPictureBasic.DateTaken).ToString("yyyy") }
      };

      return webPictureBasic;
    }
  }
}
