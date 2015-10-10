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
  internal class FFMpegGetVideoContainer
  {
    public static string GetVideoContainer(VideoContainer container)
    {
      switch (container)
      {
        case VideoContainer.Unknown:
          return null;
        case VideoContainer.Avi:
          return "avi";
        case VideoContainer.Matroska:
          return "matroska";
        case VideoContainer.Asf:
          return "asf";
        case VideoContainer.Mp4:
          return "mp4";
        case VideoContainer.Mpeg2Ps:
          return "mpeg";
        case VideoContainer.Mpeg2Ts:
          return "mpegts";
        case VideoContainer.Mpeg1:
          return "mpegvideo";
        case VideoContainer.Flv:
          return "flv";
        case VideoContainer.Wtv:
          return "wtv";
        case VideoContainer.Ogg:
          return "ogg";
        case VideoContainer.Gp3:
          return "3gp";
        case VideoContainer.M2Ts:
          return "mpegts";
        case VideoContainer.Hls:
          return "segment";
        case VideoContainer.Rtp:
          return "rtp";
        case VideoContainer.Rtsp:
          return "rtsp";
        case VideoContainer.RealMedia:
          return "rm";
      }

      return null;
    }
  }
}
