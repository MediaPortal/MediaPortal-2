using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MediaPortal.Plugins.Transcoding.Service.Transcoders.FFMpeg.Converters
{
  internal class FFMpegGetImageCodec
  {
    public static string GetImageCodec(ImageContainer container)
    {
      switch (container)
      {
        case ImageContainer.Jpeg:
          return "mjpeg";
        case ImageContainer.Png:
          return "png";
        case ImageContainer.Gif:
          return "gif";
        case ImageContainer.Bmp:
          return "bmp";
        case ImageContainer.Raw:
          return "raw";
      }
      return null;
    }
  }
}
