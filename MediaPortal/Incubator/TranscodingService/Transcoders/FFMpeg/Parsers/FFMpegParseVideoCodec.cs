using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
        if (token.StartsWith("vp8", StringComparison.InvariantCultureIgnoreCase))
          return VideoCodec.Vp8;
        if (token.Equals("wmv1", StringComparison.InvariantCultureIgnoreCase) || token.Equals("wmv2", StringComparison.InvariantCultureIgnoreCase) ||
          token.Equals("wmv3", StringComparison.InvariantCultureIgnoreCase))
          return VideoCodec.Wmv;
      }
      return VideoCodec.Unknown;
    }
  }
}
