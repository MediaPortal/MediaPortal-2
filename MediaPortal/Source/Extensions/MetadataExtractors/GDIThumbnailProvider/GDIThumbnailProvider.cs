#region Copyright (C) 2007-2018 Team MediaPortal

/*
    Copyright (C) 2007-2018 Team MediaPortal
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
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Common.Services.ThumbnailGenerator;
using MediaPortal.Utilities.Graphics;

namespace MediaPortal.Extensions.MetadataExtractors.GDIThumbnailProvider
{
  /// <summary>
  /// GDIThumbnailProvider extracts thumbnails for images using GDI.
  /// </summary>
  public class GDIThumbnailProvider : IThumbnailProvider
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

      try
      {
        using (Image fullsizeImage = Image.FromStream(stream))
        using (MemoryStream resizedStream = new MemoryStream())
        {
          // Needs to be done before we process the image further
          var pixelFormat = fullsizeImage.PixelFormat;

          fullsizeImage.ExifAutoRotate();
          using (Image newImage = ImageUtilities.ResizeImage(fullsizeImage, width, height))
          {
            //Check whether the image has an alpha channel to determine whether to save it as a png or jpg
            //Must be done before resizing as resizing disposes the fullsizeImage
            bool isAlphaPixelFormat = Image.IsAlphaPixelFormat(pixelFormat);

            if (isAlphaPixelFormat)
            {
              //Image supports an alpha channel, save as a png and add the appropriate extension
              imageType = ImageType.Png;
              ImageUtilities.SavePng(resizedStream, newImage);
            }
            else
            {
              //No alpha channel, save as a jpg and add the appropriate extension
              imageType = ImageType.Jpeg;
              ImageUtilities.SaveJpeg(resizedStream, newImage, 95);
            }
            imageData = resizedStream.ToArray();
          }
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
