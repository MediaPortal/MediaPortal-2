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
using MediaPortal.Plugins.Transcoding.Interfaces;
using MediaPortal.Common.ResourceAccess;

namespace MediaPortal.Plugins.Transcoding.Service.Transcoders.FFMpeg.Parsers
{
  public class FFMpegParseImageContainer
  {
    internal static ImageContainer ParseImageContainer(string token, ILocalFsResourceAccessor lfsra)
    {
      if (token != null)
      {
        if (token.Equals("bmp", StringComparison.InvariantCultureIgnoreCase) || token.StartsWith("bmp", StringComparison.InvariantCultureIgnoreCase))
          return ImageContainer.Bmp;
        if (token.Equals("gif", StringComparison.InvariantCultureIgnoreCase) || token.StartsWith("gif", StringComparison.InvariantCultureIgnoreCase))
          return ImageContainer.Gif;
        if (token.Equals("mjpeg", StringComparison.InvariantCultureIgnoreCase) || token.StartsWith("image2", StringComparison.InvariantCultureIgnoreCase))
          return ImageContainer.Jpeg;
        if (token.Equals("png", StringComparison.InvariantCultureIgnoreCase) || token.StartsWith("png", StringComparison.InvariantCultureIgnoreCase))
          return ImageContainer.Png;
        if (token.Equals("raw", StringComparison.InvariantCultureIgnoreCase))
          return ImageContainer.Raw;
      }

      //Try file extensions
      if (lfsra?.LocalFileSystemPath != null)
      {
        if (lfsra.LocalFileSystemPath.EndsWith(".bmp", StringComparison.InvariantCultureIgnoreCase))
          return ImageContainer.Bmp;
        if (lfsra.LocalFileSystemPath.EndsWith(".gif", StringComparison.InvariantCultureIgnoreCase))
          return ImageContainer.Gif;
        if (lfsra.LocalFileSystemPath.EndsWith(".jpg", StringComparison.InvariantCultureIgnoreCase) || lfsra.LocalFileSystemPath.EndsWith(".jpeg", StringComparison.InvariantCultureIgnoreCase))
          return ImageContainer.Jpeg;
        if (lfsra.LocalFileSystemPath.EndsWith(".png", StringComparison.InvariantCultureIgnoreCase))
          return ImageContainer.Png;
        if (lfsra.LocalFileSystemPath.EndsWith(".raw", StringComparison.InvariantCultureIgnoreCase))
          return ImageContainer.Raw;
      }

      return ImageContainer.Unknown;
    }
  }
}
