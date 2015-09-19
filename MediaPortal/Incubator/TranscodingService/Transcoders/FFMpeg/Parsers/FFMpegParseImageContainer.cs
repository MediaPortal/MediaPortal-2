using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MediaPortal.Plugins.Transcoding.Service.Transcoders.FFMpeg.Parsers
{
  public class FFMpegParseImageContainer
  {
    internal static ImageContainer ParseImageContainer(string token)
    {
      if (token != null)
      {
        if (token.Equals("bmp", StringComparison.InvariantCultureIgnoreCase))
          return ImageContainer.Bmp;
        if (token.Equals("gif", StringComparison.InvariantCultureIgnoreCase))
          return ImageContainer.Gif;
        if (token.Equals("mjpeg", StringComparison.InvariantCultureIgnoreCase))
          return ImageContainer.Jpeg;
        if (token.Equals("png", StringComparison.InvariantCultureIgnoreCase))
          return ImageContainer.Png;
        if (token.Equals("raw", StringComparison.InvariantCultureIgnoreCase))
          return ImageContainer.Raw;
      }
      return ImageContainer.Unknown;
    }
  }
}
