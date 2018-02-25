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
using System.Drawing;
using System.IO;
using ImageProcessor;
using ImageProcessor.Imaging;
using ImageProcessor.Imaging.Formats;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Common.Services.ThumbnailGenerator;

namespace MediaPortal.Extensions.MetadataExtractors.ImageProcessorThumbnailProvider
{
  /// <summary>
  /// ShellThumbnailProvider extracts thumbnails for image and video files using the Windows provided thumbnail creation and
  /// caching feature.
  /// </summary>
  public class ImageProcessorThumbnailProvider : IThumbnailProvider
  {
    /// <summary>
    /// Extracts a thumbnail for a given <paramref name="fileName"/> and returns the best matching resolution from the windows thumbs cache.
    /// This method can be used to return only already cached thumbnails by setting <paramref name="cachedOnly"/> to true.
    /// </summary>
    /// <param name="fileName">File name to extract thumb for.</param>
    /// <param name="width">Thumbnail width.</param>
    /// <param name="height">Thumbnail height.</param>
    /// <param name="cachedOnly">True to return only cached thumbs.</param>
    /// <param name="imageType">ImageFormat of thumb.</param>
    /// <param name="thumbnailBinary">Returns the thumb binary data.</param>
    /// <returns>True if thumbnail could be extracted.</returns>
    public bool GetThumbnail(string fileName, int width, int height, bool cachedOnly, out byte[] thumbnailBinary, out ImageType imageType)
    {
      try
      {
        using (var fileStream = new FileStream(fileName, FileMode.Open, FileAccess.Read))
          return GetThumbnail(fileStream, width, height, cachedOnly, out thumbnailBinary, out imageType);
      }
      catch
      {
        thumbnailBinary = null;
        imageType = ImageType.Unknown;
        return false;
      }
    }

    public bool GetThumbnail(Stream stream, int width, int height, bool cachedOnly, out byte[] imageData, out ImageType imageType)
    {
      imageData = null;
      imageType = ImageType.Unknown;
      // No support for cache
      if (cachedOnly)
        return false;

      // Note: Current version 2.6 has concurrency issues. So we need to lock here. This needs to be checked later again as it is a major performance drawback.
      lock (this)
      try
      {
        // Format is automatically detected though can be changed.
        using (MemoryStream outStream = new MemoryStream())
        using (ImageFactory imageFactory = new ImageFactory())
        {
          // Load, resize, set the format and quality and save an image.
          imageFactory.Load(stream);

          ISupportedImageFormat format = imageFactory.CurrentImageFormat;
          // We want to preserve png's alpha channel, otherwise use always jpg
          if (imageFactory.CurrentBitDepth == 32)
          {
            format = new PngFormat();
            imageType = ImageType.Png;
          }
          else
          {
            format = new JpegFormat { Quality = 90 };
            imageType = ImageType.Jpeg;
          }

          //imageFactory.AutoRotate();

          // Use the dimension that has the largest size for limiting. Has to be done after auto-rotate!
          Size targetSize = imageFactory.Image.Width > imageFactory.Image.Height ?
            new Size(width, 0) :
            new Size(0, height);

          ResizeLayer size = new ResizeLayer(targetSize, upscale: false);

          imageFactory.Resize(size)
            .Format(format)
            .Save(outStream);

          imageData = outStream.ToArray();
          return true;
        }
      }
      catch (Exception ex)
      {
        ServiceRegistration.Get<ILogger>().Warn("ImageProcessorThumbnailProvider: Error loading bitmapSource from file data stream", ex);
        return false;
      }
    }
  }
}
