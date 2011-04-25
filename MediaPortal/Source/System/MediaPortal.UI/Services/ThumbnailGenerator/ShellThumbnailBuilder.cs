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
using MediaPortal.Core;
using MediaPortal.Core.Logging;
using Microsoft.WindowsAPICodePack.Shell;

namespace MediaPortal.UI.Services.ThumbnailGenerator
{
  public class ShellThumbnailBuilder
  {
    public bool GetThumbnail(string fileName, int width, int height, out byte[] thumbnailBinary)
    {
      try
      {
        MemoryStream memoryStream = new MemoryStream();

        using (memoryStream)
        using (ShellObject item = ShellObject.FromParsingName(fileName))
        {
          Bitmap bestMatchingBmp;
          if (width > 256 || height > 256)
            bestMatchingBmp = item.Thumbnail.ExtraLargeBitmap;
          else if (width > 96 || height > 96)
            bestMatchingBmp = item.Thumbnail.LargeBitmap;
          else if (width > 32 || height > 32)
            bestMatchingBmp = item.Thumbnail.MediumBitmap;
          else
            bestMatchingBmp = item.Thumbnail.SmallBitmap;

          using (bestMatchingBmp)
            bestMatchingBmp.Save(memoryStream, ImageFormat.Jpeg);

          thumbnailBinary = memoryStream.ToArray();
        }
        return true;
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
