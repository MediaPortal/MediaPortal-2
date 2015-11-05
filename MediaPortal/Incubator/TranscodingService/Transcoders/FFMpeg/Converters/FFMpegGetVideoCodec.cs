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

using System.Collections.Generic;
using MediaPortal.Plugins.Transcoding.Service.Transcoders.Base;

namespace MediaPortal.Plugins.Transcoding.Service.Transcoders.FFMpeg.Converters
{
  internal class FFMpegGetVideoCodec
  {
    public static string GetVideoCodec(VideoCodec codec, EncoderHandler encoder)
    {
      switch (codec)
      {
        case VideoCodec.H265:
          if (encoder == EncoderHandler.HardwareIntel)
            return "hevc_qsv";
          else if (encoder == EncoderHandler.HardwareNvidia)
            return "hevc_nvenc";
          else
            return "libx265";
        case VideoCodec.H264:
          if (encoder == EncoderHandler.HardwareIntel)
            return "h264_qsv";
          else if (encoder == EncoderHandler.HardwareNvidia)
            return "nvenc_h264";
          else
            return "libx264";
        case VideoCodec.H263:
          return "h263";
        case VideoCodec.Vc1:
          return "vc1";
        case VideoCodec.Mpeg4:
          return "mpeg4";
        case VideoCodec.MsMpeg4:
          return "msmpeg4";
        case VideoCodec.Mpeg2:
          if (encoder == EncoderHandler.HardwareIntel)
            return "mpeg2_qsv";
          else
            return "mpeg2video";
        case VideoCodec.Wmv:
          return "wmv1";
        case VideoCodec.Mpeg1:
          return "mpeg1video";
        case VideoCodec.MJpeg:
          return "mjpeg";
        case VideoCodec.Flv:
          return "flv";
        case VideoCodec.Vp8:
          return "libvpx";
        case VideoCodec.Vp9:
          return "libvpx-vp9";
        case VideoCodec.Theora:
          return "libtheora";
        case VideoCodec.DvVideo:
          return "dvvideo";
        case VideoCodec.Real:
          return "rv10";
      }
      return "copy";
    }
  }
}
