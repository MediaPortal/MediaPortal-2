using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MediaPortal.Plugins.Transcoding.Service.Transcoders.FFMpeg.Parsers
{
  public class FFMpegParseFFMpegOutputLines
  {
    internal static void ParseFFMpegOutputLines(string[] input, ref MetadataContainer info, Dictionary<string, CultureInfo> countryCodesMapping)
    {
      foreach (string inputLine in input)
      {
        string line = inputLine.Trim();
        if (line.IndexOf("Input #0") > -1)
        {
          FFMpegParseInputLine.ParseInputLine(line, ref info);
        }
        else if (line.IndexOf("major_brand") > -1)
        {
          string[] tokens = line.Split(':');
          info.Metadata.MajorBrand = tokens[1].Trim();
        }
        else if (line.IndexOf("Duration") > -1)
        {
          FFMpegParseDurationLine.ParseDurationLine(line, ref info);
        }
        else if (line.IndexOf("Stream #0") > -1 && line.IndexOf("Video:") > -1)
        {
          FFMpegParseStreamVideoLine.ParseStreamVideoLine(line, ref info, countryCodesMapping);
        }
        else if (line.IndexOf("Stream #0") > -1 && line.IndexOf("Audio:") > -1)
        {
          FFMpegParseStreamAudioLine.ParseStreamAudioLine(line, ref info, countryCodesMapping);
        }
        else if (line.IndexOf("Stream #0") > -1 && line.IndexOf("Subtitle:") > -1)
        {
          FFMpegParseStreamSubtitleLine.ParseStreamSubtitleLine(line, ref info, countryCodesMapping);
        }
      }
    }
  }
}
