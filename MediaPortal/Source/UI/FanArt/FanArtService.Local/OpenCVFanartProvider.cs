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

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using MediaPortal.Backend.MediaLibrary;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Common.MediaManagement.MLQueries;
using MediaPortal.Common.ResourceAccess;
using MediaPortal.Extensions.UserServices.FanArtService.Interfaces;
using MediaPortal.Common.FanArt;
using OpenCvSharp;

namespace MediaPortal.Extensions.UserServices.FanArtService.Local
{
  class OpenCvFanartProvider : IBinaryFanArtProvider
  {
    private static readonly Guid[] NECESSARY_MIAS = { VideoStreamAspect.ASPECT_ID, ProviderResourceAspect.ASPECT_ID };
    private const double DEFAULT_OPENCV_THUMBNAIL_OFFSET = 1.0 / 3.0;

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
      string fileSystemPath = string.Empty;

      // File based access
      try
      {
        using (var accessor = resourceLocator?.CreateAccessor())
        {
          ILocalFsResourceAccessor lfsra = accessor as ILocalFsResourceAccessor;
          if (lfsra != null)
          {
            // Check for a reasonable time offset
            int defaultVideoOffset = 720;
            long videoDuration;
            double width = 0;
            double height = 0;
            double downscale = 7.5; // Reduces the HD video frame size to a quarter size to around 256
            IList<MultipleMediaItemAspect> videoAspects;
            if (MediaItemAspect.TryGetAspects(mediaItem.Aspects, VideoStreamAspect.Metadata, out videoAspects))
            {
              if ((videoDuration = videoAspects[0].GetAttributeValue<long>(VideoStreamAspect.ATTR_DURATION)) > 0)
              {
                if (defaultVideoOffset > videoDuration * DEFAULT_OPENCV_THUMBNAIL_OFFSET)
                  defaultVideoOffset = Convert.ToInt32(videoDuration * DEFAULT_OPENCV_THUMBNAIL_OFFSET);
              }

              width = videoAspects[0].GetAttributeValue<int>(VideoStreamAspect.ATTR_WIDTH);
              height = videoAspects[0].GetAttributeValue<int>(VideoStreamAspect.ATTR_HEIGHT);
              downscale = width / 256.0; //256 is max size of large thumbnail aspect
            }

            var sw = Stopwatch.StartNew();
            using (VideoCapture capture = new VideoCapture())
            {
              capture.Open(lfsra.LocalFileSystemPath);
              int capturePos = defaultVideoOffset * 1000;
              if (capture.FrameCount > 0 && capture.Fps > 0)
              {
                var duration = capture.FrameCount / capture.Fps;
                if (defaultVideoOffset > duration)
                  capturePos = Convert.ToInt32(duration * DEFAULT_OPENCV_THUMBNAIL_OFFSET * 1000);
              }

              if (capture.FrameWidth > 0)
                downscale = capture.FrameWidth / 256.0; //256 is max size of large thumbnail aspect

              capture.PosMsec = capturePos;
              using (var mat = capture.RetrieveMat())
              {
                if (mat.Height > 0 && mat.Width > 0)
                {
                  width = mat.Width;
                  height = mat.Height;
                  using (var scaledMat = mat.Resize(new OpenCvSharp.Size(width / downscale, height / downscale)))
                  {
                    var binary = scaledMat.ToBytes();
                    result = new List<FanArtImage> { new FanArtImage(name, binary) };
                    ServiceRegistration.Get<ILogger>().Debug("OpenCvFanartProvider: Successfully extracted thumbnail for resource '{0}' ({1} ms)", lfsra.LocalFileSystemPath, sw.ElapsedMilliseconds);
                    return true;
                  }
                }
                else
                {
                  ServiceRegistration.Get<ILogger>().Warn("OpenCvFanartProvider: Failed to extract thumbnail for resource '{0}'", lfsra.LocalFileSystemPath);
                }
              }
            }
          }
        }
      }
      catch (Exception ex)
      {
        ServiceRegistration.Get<ILogger>().Warn("OpenCvFanartProvider: Exception while reading thumbnail of type '{0}' from '{1}'", ex, fanArtType, fileSystemPath);
      }
      return false;
    }

    #endregion
  }
}
