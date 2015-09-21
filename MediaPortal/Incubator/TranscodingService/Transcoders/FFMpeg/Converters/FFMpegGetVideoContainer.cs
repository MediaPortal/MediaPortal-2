using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
