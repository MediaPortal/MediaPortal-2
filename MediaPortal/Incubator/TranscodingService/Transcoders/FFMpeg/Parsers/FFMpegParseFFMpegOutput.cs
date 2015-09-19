using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MediaPortal.Plugins.Transcoding.Service.Transcoders.FFMpeg.Parsers
{
  public class FFMpegParseFFMpegOutput
  {
    internal static void ParseFFMpegOutput(string output, ref MetadataContainer info, Dictionary<string, CultureInfo> countryCodesMapping)
    {
      var input = output.Split('\n');
      if (!input[0].StartsWith("ffmpeg version") && !input[0].StartsWith("ffprobe version"))
        return;
      FFMpegParseFFMpegOutputLines.ParseFFMpegOutputLines(input, ref info, countryCodesMapping);
    }
  }
}
