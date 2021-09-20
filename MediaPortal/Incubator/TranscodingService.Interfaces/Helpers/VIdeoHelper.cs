#region Copyright (C) 2007-2020 Team MediaPortal

/*
    Copyright (C) 2007-2020 Team MediaPortal
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

namespace MediaPortal.Extensions.TranscodingService.Interfaces.Helpers
{
  public class VideoHelper
  {
    public static string GetVideoExtension(VideoContainer container)
    {
      switch (container)
      {
        case VideoContainer.Asf:
          return "wmv";
        case VideoContainer.Avi:
          return "avi";
        case VideoContainer.Flv:
          return "flv";
        case VideoContainer.Gp3:
          return "3gp";
        case VideoContainer.Matroska:
          return "mkv";
        case VideoContainer.Mp4:
          return "mp4v";
        case VideoContainer.M2Ts:
          return "m2ts";
        case VideoContainer.Mpeg2Ps:
          return "mpg";
        case VideoContainer.Mpeg2Ts:
          return "ts";
        case VideoContainer.Mpeg1:
          return "mpeg";
        case VideoContainer.MJpeg:
          return "mjpeg";
        case VideoContainer.Ogg:
          return "ogg";
        case VideoContainer.RealMedia:
          return "rm";
        case VideoContainer.Wtv:
          return "wtv";
      }
      return "mptv";
    }
  }
}
