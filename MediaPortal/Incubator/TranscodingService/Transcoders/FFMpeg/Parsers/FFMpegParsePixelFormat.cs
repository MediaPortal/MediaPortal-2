using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MediaPortal.Plugins.Transcoding.Service.Transcoders.FFMpeg.Parsers
{
  public class FFMpegParsePixelFormat
  {
    internal static PixelFormat ParsePixelFormat(string token)
    {
      if (token != null)
      {
        if (token.StartsWith("yuvj411p", StringComparison.InvariantCultureIgnoreCase) || token.StartsWith("yuv411p", StringComparison.InvariantCultureIgnoreCase))
          return PixelFormat.Yuv411;
        if (token.StartsWith("yuvj420p", StringComparison.InvariantCultureIgnoreCase) || token.StartsWith("yuv420p", StringComparison.InvariantCultureIgnoreCase))
          return PixelFormat.Yuv420;
        if (token.StartsWith("yuvj422p", StringComparison.InvariantCultureIgnoreCase) || token.StartsWith("yuv422p", StringComparison.InvariantCultureIgnoreCase))
          return PixelFormat.Yuv422;
        if (token.StartsWith("yuvj440p", StringComparison.InvariantCultureIgnoreCase) || token.StartsWith("yuv440p", StringComparison.InvariantCultureIgnoreCase))
          return PixelFormat.Yuv440;
        if (token.StartsWith("yuvj444p", StringComparison.InvariantCultureIgnoreCase) || token.StartsWith("yuv444p", StringComparison.InvariantCultureIgnoreCase))
          return PixelFormat.Yuv444;
      }
      return PixelFormat.Unknown;
    }
  }
}
