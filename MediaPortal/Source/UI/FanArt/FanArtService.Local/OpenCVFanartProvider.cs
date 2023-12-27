#region Copyright (C) 2007-2020 Team MediaPortal

/*
    Copyright (C) 2007-2020 Team MediaPortal
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

using MediaPortal.Backend.MediaLibrary;
using MediaPortal.Common;
using MediaPortal.Common.FanArt;
using MediaPortal.Common.Logging;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Common.MediaManagement.MLQueries;
using MediaPortal.Common.ResourceAccess;
using MediaPortal.Extensions.UserServices.FanArtService.Interfaces;
using OpenCvLib;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MediaPortal.Extensions.UserServices.FanArtService.Local
{
  class OpenCvFanartProvider : IBinaryFanArtProvider
  {
    private static readonly Guid[] NECESSARY_MIAS = { VideoStreamAspect.ASPECT_ID, ProviderResourceAspect.ASPECT_ID };

    #region Implementation of IFanArtProvider

    public FanArtProviderSource Source { get { return FanArtProviderSource.FallBack; } }

    public bool TryGetFanArt(string mediaType, string fanArtType, string name, int maxWidth, int maxHeight, bool singleRandom, out IList<IResourceLocator> result)
    {
      result = null;
      return false;
    }

    public bool TryGetFanArt(string mediaType, string fanArtType, string name, int maxWidth, int maxHeight, bool singleRandom, out IList<FanArtImage> result)
    {
      result = null;

      if ((mediaType != FanArtMediaTypes.Episode && mediaType != FanArtMediaTypes.Movie) || (fanArtType != FanArtTypes.Thumbnail && fanArtType != FanArtTypes.Undefined))
        return false;

      Guid mediaItemId;
      if (!Guid.TryParse(name, out mediaItemId))
        return false;

      IMediaLibrary mediaLibrary = ServiceRegistration.Get<IMediaLibrary>(false);
      if (mediaLibrary == null)
        return false;

      IFilter filter = new MediaItemIdFilter(mediaItemId);
      IList<MediaItem> items = mediaLibrary.Search(new MediaItemQuery(NECESSARY_MIAS, filter), false, null, true);
      if (items == null || items.Count == 0)
        return false;

      MediaItem mediaItem = items.First();
      // Virtual resources won't have any local fanart
      if (mediaItem.IsVirtual)
        return false;
      var resourceLocator = mediaItem.GetResourceLocator();
      using(var accessor = resourceLocator?.CreateAccessor())
      {
        ILocalFsResourceAccessor lfsra = accessor as ILocalFsResourceAccessor;
        if (lfsra == null)
          return false;

        // Try and extract a thumbnail 10% into the video with a max width of 256, the default size for a ThumbnailLargeAspect
        if (!OpenCvWrapper.TryExtractThumbnail(lfsra.LocalFileSystemPath, 0.1, 256, out byte[] thumbnail))
        {
          ServiceRegistration.Get<ILogger>().Warn("OpenCvFanartProvider: Failed to extract thumbnail for resource '{0}')", lfsra.LocalFileSystemPath);
          return false;
        }

        ServiceRegistration.Get<ILogger>().Debug("OpenCvFanartProvider: Successfully extracted thumbnail for resource '{0}')", lfsra.LocalFileSystemPath);
        result = new List<FanArtImage> { new FanArtImage(name, thumbnail) };
      }

      return true;
    }

#endregion
  }
}
