#region Copyright (C) 2007-2011 Team MediaPortal

/*
    Copyright (C) 2007-2011 Team MediaPortal
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
using MediaPortal.Core.Logging;
using Microsoft.WindowsAPICodePack.Shell;

namespace MediaPortal.Core.Services.ThumbnailGenerator
{
  /// <summary>
  /// ShellThumnbnailBuilder extracts thumbnails for picture and video files using the Windows provided thumbnail creation and
  /// caching feature.
  /// </summary>
  public class ShellThumbnailBuilder
  {
    /// <summary>
    /// Extracts a thumbnail for a given <paramref name="fileName"/> and returns the best matching resolution from the windows thumbs cache.
    /// This method can be used to return only already cached thumbnails by setting <paramref name="cachedOnly"/> to true.
    /// </summary>
    /// <param name="fileName">File name to extract thumb for.</param>
    /// <param name="width">Thumbnail width.</param>
    /// <param name="height">Thumbnail height.</param>
    /// <param name="cachedOnly">True to return only cached thumbs.</param>
    /// <param name="imageFormat">ImageFormat of thumb.</param>
    /// <param name="thumbnailBinary">Returns the thumb binary data.</param>
    /// <returns>True if thumbnail could be extracted.</returns>
    public bool GetThumbnail(string fileName, int width, int height, bool cachedOnly, ImageFormat imageFormat, out byte[] thumbnailBinary)
    {
      try
      {
        using (MemoryStream memoryStream = new MemoryStream())
        using (ShellObject item = ShellObject.FromParsingName(fileName))
        {
          item.Thumbnail.RetrievalOption = cachedOnly
                                             ? ShellThumbnailRetrievalOption.CacheOnly
                                             : ShellThumbnailRetrievalOption.Default;
          Bitmap bestMatchingBmp;
          // Try to use the best matching resolution, 2 different are enough.
          if (width > 96 || height > 96)
            bestMatchingBmp = item.Thumbnail.LargeBitmap;
          else 
            bestMatchingBmp = item.Thumbnail.MediumBitmap;

          if (bestMatchingBmp != null)
            using (bestMatchingBmp)
            {
              bestMatchingBmp.Save(memoryStream, imageFormat);
              thumbnailBinary = memoryStream.ToArray();
              return true;
            }

          thumbnailBinary = null;
          return false;
        }
      }
      catch (Exception e)
      {
        ServiceRegistration.Get<ILogger>().Warn("ShellThumbnailBuilder: Could not create thumbnail for file '{0}'", e, fileName);
        thumbnailBinary = null;
        return false;
      }
    }
  }
}
