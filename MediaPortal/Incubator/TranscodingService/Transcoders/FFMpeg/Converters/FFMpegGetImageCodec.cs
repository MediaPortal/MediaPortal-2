﻿#region Copyright (C) 2007-2017 Team MediaPortal

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

using MediaPortal.Extensions.TranscodingService.Interfaces;

namespace MediaPortal.Extensions.TranscodingService.Service.Transcoders.FFMpeg.Converters
{
  internal class FFMpegGetImageCodec
  {
    public static string GetImageCodec(ImageContainer container)
    {
      switch (container)
      {
        case ImageContainer.Jpeg:
          return "mjpeg";
        case ImageContainer.Png:
          return "png";
        case ImageContainer.Gif:
          return "gif";
        case ImageContainer.Bmp:
          return "bmp";
      }
      return "copy";
    }
  }
}
