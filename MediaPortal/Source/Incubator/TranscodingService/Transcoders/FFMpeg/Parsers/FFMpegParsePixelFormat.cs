#region Copyright (C) 2007-2015 Team MediaPortal

/*
    Copyright (C) 2007-2015 Team MediaPortal
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

namespace MediaPortal.Plugins.Transcoding.Service.Transcoders.FFMpeg.Parsers
{
  public class FFMpegParsePixelFormat
  {
    internal static PixelFormat ParsePixelFormat(string token)
    {
      if (token != null)
      {
        if (token.StartsWith("yuvj411p", StringComparison.InvariantCultureIgnoreCase) || token.StartsWith("yuv411p", StringComparison.InvariantCultureIgnoreCase))
          return PixelFormat.Yuv411;
        if (token.StartsWith("yuvj420p", StringComparison.InvariantCultureIgnoreCase) || token.StartsWith("yuv420p", StringComparison.InvariantCultureIgnoreCase))
          return PixelFormat.Yuv420;
        if (token.StartsWith("yuvj422p", StringComparison.InvariantCultureIgnoreCase) || token.StartsWith("yuv422p", StringComparison.InvariantCultureIgnoreCase))
          return PixelFormat.Yuv422;
        if (token.StartsWith("yuvj440p", StringComparison.InvariantCultureIgnoreCase) || token.StartsWith("yuv440p", StringComparison.InvariantCultureIgnoreCase))
          return PixelFormat.Yuv440;
        if (token.StartsWith("yuvj444p", StringComparison.InvariantCultureIgnoreCase) || token.StartsWith("yuv444p", StringComparison.InvariantCultureIgnoreCase))
          return PixelFormat.Yuv444;
      }
      return PixelFormat.Unknown;
    }
  }
}
