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

namespace MediaPortal.Plugins.Transcoding.Service.Transcoders.FFMpeg.Parsers
{
  public class FFMpegParseVideoCodec
  {
    internal static VideoCodec ParseVideoCodec(string token)
    {
      if (token != null)
      {
        if (token.Equals("dvvideo", StringComparison.InvariantCultureIgnoreCase))
          return VideoCodec.DvVideo;
        if (token.StartsWith("flv", StringComparison.InvariantCultureIgnoreCase))
          return VideoCodec.Flv;
        if (token.Equals("hevc", StringComparison.InvariantCultureIgnoreCase) || token.Equals("h265", StringComparison.InvariantCultureIgnoreCase) ||
          token.Equals("libx265", StringComparison.InvariantCultureIgnoreCase))
          return VideoCodec.H265;
        if (token.Equals("avc", StringComparison.InvariantCultureIgnoreCase) || token.Equals("h264", StringComparison.InvariantCultureIgnoreCase) ||
          token.Equals("libx264", StringComparison.InvariantCultureIgnoreCase))
          return VideoCodec.H264;
        if (token.StartsWith("h263", StringComparison.InvariantCultureIgnoreCase))
          return VideoCodec.H263;
        if (token.Equals("mpeg4", StringComparison.InvariantCultureIgnoreCase))
          return VideoCodec.Mpeg4;
        if (token.Equals("msmpeg4", StringComparison.InvariantCultureIgnoreCase) || token.Equals("msmpeg4v1", StringComparison.InvariantCultureIgnoreCase) ||
          token.Equals("msmpeg4v2", StringComparison.InvariantCultureIgnoreCase))
          return VideoCodec.MsMpeg4;
        if (token.Equals("mpeg2video", StringComparison.InvariantCultureIgnoreCase))
          return VideoCodec.Mpeg2;
        if (token.Equals("mpeg1video", StringComparison.InvariantCultureIgnoreCase) || token.Equals("mpegvideo", StringComparison.InvariantCultureIgnoreCase))
          return VideoCodec.Mpeg1;
        if (token.Equals("mjpeg", StringComparison.InvariantCultureIgnoreCase) || token.Equals("mjpegb", StringComparison.InvariantCultureIgnoreCase))
          return VideoCodec.MJpeg;
        if (token.StartsWith("rv", StringComparison.InvariantCultureIgnoreCase))
          return VideoCodec.Real;
        if (token.Equals("theora", StringComparison.InvariantCultureIgnoreCase))
          return VideoCodec.Theora;
        if (token.Equals("vc1", StringComparison.InvariantCultureIgnoreCase))
          return VideoCodec.Vc1;
        if (token.StartsWith("vp6", StringComparison.InvariantCultureIgnoreCase))
          return VideoCodec.Vp6;
        if (token.StartsWith("vp7", StringComparison.InvariantCultureIgnoreCase))
          return VideoCodec.Vp7;
        if (token.StartsWith("vp8", StringComparison.InvariantCultureIgnoreCase) || token.StartsWith("libvpx", StringComparison.InvariantCultureIgnoreCase))
          return VideoCodec.Vp8;
        if (token.StartsWith("vp9", StringComparison.InvariantCultureIgnoreCase) || token.StartsWith("libvpx-vp9", StringComparison.InvariantCultureIgnoreCase))
          return VideoCodec.Vp9;
        if (token.Equals("wmv1", StringComparison.InvariantCultureIgnoreCase) || token.Equals("wmv2", StringComparison.InvariantCultureIgnoreCase) ||
          token.Equals("wmv3", StringComparison.InvariantCultureIgnoreCase))
          return VideoCodec.Wmv;
      }
      return VideoCodec.Unknown;
    }
  }
}
