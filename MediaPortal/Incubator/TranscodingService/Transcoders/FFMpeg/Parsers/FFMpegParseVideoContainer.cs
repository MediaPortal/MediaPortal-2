using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MediaPortal.Common.ResourceAccess;

namespace MediaPortal.Plugins.Transcoding.Service.Transcoders.FFMpeg.Parsers
{
  public class FFMpegParseVideoContainer
  {
    internal static VideoContainer ParseVideoContainer(string token, ILocalFsResourceAccessor lfsra)
    {
      if (token != null)
      {
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
          if (lfsra.LocalFileSystemPath != null && lfsra.LocalFileSystemPath.EndsWith(".3g", StringComparison.InvariantCultureIgnoreCase))
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
      return VideoContainer.Unknown;
    }
  }
}
