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
using System.IO;
using MediaPortal.Common.Services.ThumbnailGenerator;
using SharpDX.WIC;

namespace MediaPortal.Extensions.MetadataExtractors.WICThumbnailProvider
{
  /// <summary>
  /// WICThumbnailProvider extracts thumbnails for image and video files using the Windows provided thumbnail creation and
  /// caching feature.
  /// </summary>
  public class WICThumbnailProvider : IThumbnailProvider
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
        if (stream.CanSeek)
          stream.Seek(0, SeekOrigin.Begin);

        // open the image file for reading
        using (var factory = new ImagingFactory2())
        using (var inputStream = new WICStream(factory, stream))
        using (var decoder = new BitmapDecoder(factory, inputStream, DecodeOptions.CacheOnLoad))
        using (var scaler = new BitmapScaler(factory))
        using (var output = new MemoryStream())
        using (var encoder = new BitmapEncoder(factory, ContainerFormatGuids.Jpeg))
        {
          // decode the loaded image to a format that can be consumed by D2D
          BitmapSource source = decoder.GetFrame(0);

          // Scale down larger images
          int sourceWidth = source.Size.Width;
          int sourceHeight = source.Size.Height;
          if (width > 0 && height > 0 && (sourceWidth > width || sourceHeight > height))
          {
            if (sourceWidth <= height)
              width = sourceWidth;

            int newHeight = sourceHeight * height / sourceWidth;
            if (newHeight > height)
            {
              // Resize with height instead
              width = sourceWidth * height / sourceHeight;
              newHeight = height;
            }

            scaler.Initialize(source, width, newHeight, BitmapInterpolationMode.Fant);
            source = scaler;
          }
          encoder.Initialize(output);

          using (var bitmapFrameEncode = new BitmapFrameEncode(encoder))
          {
            // Create image encoder
            var wicPixelFormat = PixelFormat.FormatDontCare;
            bitmapFrameEncode.Initialize();
            bitmapFrameEncode.SetSize(source.Size.Width, source.Size.Height);
            bitmapFrameEncode.SetPixelFormat(ref wicPixelFormat);
            bitmapFrameEncode.WriteSource(source);
            bitmapFrameEncode.Commit();
            encoder.Commit();
          }
          imageData = output.ToArray();
          imageType = ImageType.Jpeg;
          return true;
        }
      }
      catch (Exception e)
      {
        // ServiceRegistration.Get<ILogger>().Warn("WICThumbnailProvider: Error loading bitmapSource from file data stream", e);
        return false;
      }
    }
  }
}
