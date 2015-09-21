using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MediaPortal.Plugins.Transcoding.Service.Transcoders.FFMpeg.Converters
{
  internal class FFMpegGetVideoCodec
  {
    public static string GetVideoCodec(VideoCodec codec, bool allowNvidiaHwAccelleration, bool allowIntelHwAccelleration, bool supportNvidiaHw, bool supportIntelHw)
    {
      switch (codec)
      {
        case VideoCodec.H265:
          if (allowNvidiaHwAccelleration && supportNvidiaHw)
            return "hevc_nvenc";
          else
            return "libx265";
        case VideoCodec.H264:
          if (allowIntelHwAccelleration && supportIntelHw)
            return "h264_qsv";
          else if (allowNvidiaHwAccelleration && supportNvidiaHw)
            return "h264_nvenc";
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
          if (allowIntelHwAccelleration && supportIntelHw)
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
        case VideoCodec.Vp6:
          return "vp6";
        case VideoCodec.Vp8:
          return "vp8";
        case VideoCodec.Theora:
          return "theora";
        case VideoCodec.DvVideo:
          return "dvvideo";
        case VideoCodec.Real:
          return "rv";
      }
      return null;
    }
  }
}
