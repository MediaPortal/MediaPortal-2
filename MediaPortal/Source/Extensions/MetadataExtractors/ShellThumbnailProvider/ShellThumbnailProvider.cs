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
using System.Drawing.Imaging;
using System.IO;
using MediaPortal.Common.Services.ThumbnailGenerator;
using MediaPortal.Utilities.Graphics;
using Microsoft.WindowsAPICodePack.Shell;

namespace MediaPortal.Extensions.MetadataExtractors.ShellThumbnailProvider
{
  /// <summary>
  /// ShellThumbnailProvider extracts thumbnails for image and video files using the Windows provided thumbnail creation and
  /// caching feature.
  /// </summary>
  public class ShellThumbnailProvider : IThumbnailProvider
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
        using (MemoryStream memoryStream = new MemoryStream())
        using (ShellObject item = ShellObject.FromParsingName(fileName))
        {
          item.Thumbnail.RetrievalOption = cachedOnly ? ShellThumbnailRetrievalOption.CacheOnly : ShellThumbnailRetrievalOption.Default;
          // If no thumbnail is available, we don't want to use an icon (will be black). 
          // Shell library throws an error then, we catch this below.
          item.Thumbnail.FormatOption = ShellThumbnailFormatOption.ThumbnailOnly;
          Bitmap bestMatchingBmp;
          // Try to use the best matching resolution, 2 different are enough.
          if (width > 96 || height > 96)
            bestMatchingBmp = item.Thumbnail.LargeBitmap;
          else
            bestMatchingBmp = item.Thumbnail.MediumBitmap;

          if (bestMatchingBmp != null)
            using (bestMatchingBmp)
            {
              ImageFormat imageFormat;
              // If the image has an Alpha channel, prefer .png as type to keep transparency.
              if (Image.IsAlphaPixelFormat(bestMatchingBmp.PixelFormat))
              {
                imageType = ImageType.Png;
                imageFormat = ImageFormat.Png;
              }
              else
              {
                imageType = ImageType.Jpeg;
                imageFormat = ImageFormat.Jpeg;
              }

              bestMatchingBmp.Save(memoryStream, imageFormat);
              thumbnailBinary = memoryStream.ToArray();
              return true;
            }
        }
      }
      catch { } // Ignore all internal exception that can occure inside shell library.
      thumbnailBinary = null;
      imageType = ImageType.Unknown;
      return false;
    }

    public bool GetThumbnail(Stream stream, int width, int height, bool cachedOnly, out byte[] imageData, out ImageType imageType)
    {
      try
      {
        using (MemoryStream resized = (MemoryStream)ImageUtilities.ResizeImage(stream, ImageFormat.Jpeg, width, height))
        {
          imageData = resized.ToArray();
          imageType = ImageType.Jpeg;
          return true;
        }
      }
      catch (Exception)
      {
        imageData = null;
        imageType = ImageType.Unknown;
        return false;
      }
    }
  }
}
