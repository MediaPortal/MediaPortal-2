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
using MediaPortal.Common.ResourceAccess;
using MediaPortal.Common.Services.ResourceAccess;

namespace MediaPortal.UiComponents.BackgroundManager.Helper
{
  static class MediaItemHelper
  {
    public static readonly Guid[] NECESSARY_VIDEO_MIAS = new Guid[] { VideoAspect.ASPECT_ID };
    public static MediaItem CreateMediaItem(string filename)
    {
      IMediaAccessor mediaAccessor = ServiceRegistration.Get<IMediaAccessor>();
      IEnumerable<Guid> meIds = mediaAccessor.GetMetadataExtractorsForMIATypes(NECESSARY_VIDEO_MIAS);
      ResourceLocator resourceLocator = new ResourceLocator(LocalFsResourceProviderBase.ToResourcePath(filename));
      IResourceAccessor ra = resourceLocator.CreateAccessor();
      if (ra == null)
        return null;
      using (ra)
        return mediaAccessor.CreateLocalMediaItem(ra, meIds);
    }
    public static bool IsValidVideo(MediaItem mediaItem)
    {
      if (mediaItem == null)
        return false;

      IList<MultipleMediaItemAspect> pras;
      if (!MediaItemAspect.TryGetAspects(mediaItem.Aspects, ProviderResourceAspect.Metadata, out pras))
        return false;

      string mimeType = pras[0].GetAttributeValue<string>(ProviderResourceAspect.ATTR_MIME_TYPE);
      return mimeType.StartsWith("video/");
    }
  }
}
