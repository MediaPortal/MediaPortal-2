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



namespace MediaPortal.Plugins.Transcoding.Service.Transcoders.FFMpeg.Converters
{
  internal class FFMpegGetPixelFormat
  {
    public static string GetPixelFormat(PixelFormat pixelFormat)
    {
      switch (pixelFormat)
      {
        case PixelFormat.Unknown:
          return "yuv420p";
        case PixelFormat.Yuv444:
          return "yuv444p";
        case PixelFormat.Yuv440:
          return "yuv440p";
        case PixelFormat.Yuv422:
          return "yuv422p";
        case PixelFormat.Yuv420:
          return "yuv420p";
        case PixelFormat.Yuv411:
          return "yuv411p";
      }
      return null;
    }
  }
}
