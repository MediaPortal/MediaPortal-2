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
using MediaPortal.Common.ResourceAccess;
using MediaPortal.Extensions.TranscodingService.Interfaces;

namespace MediaPortal.Extensions.TranscodingService.Service.Transcoders.FFMpeg.Parsers
{
  public class FFMpegParseVideoContainer
  {
    internal static VideoContainer ParseVideoContainer(string token, IResourceAccessor ra)
    {
      if (token != null)
      {
        if (token.StartsWith("image", StringComparison.InvariantCultureIgnoreCase))
          return VideoContainer.Unknown;

        if (token.Equals("asf", StringComparison.InvariantCultureIgnoreCase))
          return VideoContainer.Asf;
        if (token.Equals("avi", StringComparison.InvariantCultureIgnoreCase))
          return VideoContainer.Avi;
        if (token.Equals("flv", StringComparison.InvariantCultureIgnoreCase))
          return VideoContainer.Flv;
        if (token.Equals("3gp", StringComparison.InvariantCultureIgnoreCase))
          return VideoContainer.Gp3;
        if (token.Equals("applehttp", StringComparison.InvariantCultureIgnoreCase) || token.Equals("hls", StringComparison.InvariantCultureIgnoreCase))
          return VideoContainer.Hls;
        if (token.Equals("matroska", StringComparison.InvariantCultureIgnoreCase) || token.Equals("webm", StringComparison.InvariantCultureIgnoreCase))
          return VideoContainer.Matroska;
        if (token.Equals("mov", StringComparison.InvariantCultureIgnoreCase) || token.Equals("mp4", StringComparison.InvariantCultureIgnoreCase))
        {
          if (ra?.ResourceName != null && ra.ResourceName.EndsWith(".3g", StringComparison.InvariantCultureIgnoreCase))
          {
            return VideoContainer.Gp3;
          }
          return VideoContainer.Mp4;
        }
        if (token.Equals("m2ts", StringComparison.InvariantCultureIgnoreCase))
          return VideoContainer.M2Ts;
        if (token.Equals("mpeg", StringComparison.InvariantCultureIgnoreCase) || token.Equals("vob", StringComparison.InvariantCultureIgnoreCase))
          return VideoContainer.Mpeg2Ps;
        if (token.Equals("mpegts", StringComparison.InvariantCultureIgnoreCase))
          return VideoContainer.Mpeg2Ts;
        if (token.Equals("mpegvideo", StringComparison.InvariantCultureIgnoreCase))
          return VideoContainer.Mpeg1;
        if (token.Equals("mjpeg", StringComparison.InvariantCultureIgnoreCase))
          return VideoContainer.MJpeg;
        if (token.Equals("ogg", StringComparison.InvariantCultureIgnoreCase))
          return VideoContainer.Ogg;
        if (token.Equals("rm", StringComparison.InvariantCultureIgnoreCase))
          return VideoContainer.RealMedia;
        if (token.Equals("rtp", StringComparison.InvariantCultureIgnoreCase))
          return VideoContainer.Rtp;
        if (token.Equals("rtsp", StringComparison.InvariantCultureIgnoreCase))
          return VideoContainer.Rtsp;
        if (token.Equals("wtv", StringComparison.InvariantCultureIgnoreCase))
          return VideoContainer.Wtv;
      }

      //Try file extensions
      var ext = ra?.ResourceName;
      if (ext != null)
      {
        if (ext.EndsWith(".wmv", StringComparison.InvariantCultureIgnoreCase))
          return VideoContainer.Asf;
        if (ext.EndsWith(".avi", StringComparison.InvariantCultureIgnoreCase))
          return VideoContainer.Avi;
        if (ext.EndsWith(".flv", StringComparison.InvariantCultureIgnoreCase))
          return VideoContainer.Flv;
        if (ext.EndsWith(".3gp", StringComparison.InvariantCultureIgnoreCase) || ext.EndsWith(".3g", StringComparison.InvariantCultureIgnoreCase))
          return VideoContainer.Gp3;
        if (ext.EndsWith(".mov", StringComparison.InvariantCultureIgnoreCase) || ext.EndsWith(".mp4", StringComparison.InvariantCultureIgnoreCase) ||
            ext.EndsWith(".mp4v", StringComparison.InvariantCultureIgnoreCase))
          return VideoContainer.Mp4;
        if (ext.EndsWith(".mkv", StringComparison.InvariantCultureIgnoreCase))
          return VideoContainer.Matroska;
        if (ext.EndsWith(".m2ts", StringComparison.InvariantCultureIgnoreCase))
          return VideoContainer.M2Ts;
        if (ext.EndsWith(".mpeg", StringComparison.InvariantCultureIgnoreCase) || ext.EndsWith(".mpg", StringComparison.InvariantCultureIgnoreCase))
          return VideoContainer.Mpeg2Ps;
        if (ext.EndsWith(".ts", StringComparison.InvariantCultureIgnoreCase))
          return VideoContainer.Mpeg2Ts;
        if (ext.EndsWith(".ogg", StringComparison.InvariantCultureIgnoreCase))
          return VideoContainer.Ogg;
      }

      return VideoContainer.Unknown;
    }
  }
}
