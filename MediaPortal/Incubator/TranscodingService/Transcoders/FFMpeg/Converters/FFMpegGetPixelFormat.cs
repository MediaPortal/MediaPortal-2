using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MediaPortal.Plugins.Transcoding.Service.Transcoders.FFMpeg.Converters
{
  internal class FFMpegGetPixelFormat
  {
    public static string GetPixelFormat(PixelFormat pixelFormat)
    {
      switch (pixelFormat)
      {
        case PixelFormat.Unknown:
          return "yuv420p";
        case PixelFormat.Yuv444:
          return "yuv444p";
        case PixelFormat.Yuv440:
          return "yuv440p";
        case PixelFormat.Yuv422:
          return "yuv422p";
        case PixelFormat.Yuv420:
          return "yuv420p";
        case PixelFormat.Yuv411:
          return "yuv411p";
      }
      return null;
    }
  }
}
