using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MediaPortal.Plugins.Transcoding.Service.Transcoders.FFMpeg.Parsers
{
  public class FFMpegParseDurationLine
  {
    internal static void ParseDurationLine(string durationLine, ref MetadataContainer info)
    {
      durationLine = durationLine.Trim();
      string[] tokens = durationLine.Split(',');
      foreach (string mediaToken in tokens)
      {
        string token = mediaToken.Trim();
        if (token.StartsWith("Duration: ", StringComparison.InvariantCultureIgnoreCase))
        {
          string duration = token.Substring(10).Trim();
          if (duration.IndexOf("N/A") == -1)
          {
            if (duration.Contains(".") == true)
            {
              string[] parts = duration.Split('.');
              duration = parts[0];
            }
            info.Metadata.Duration = TimeSpan.ParseExact(duration, @"hh\:mm\:ss", CultureInfo.InvariantCulture).TotalSeconds;
          }
        }
        else if (token.StartsWith("bitrate: ", StringComparison.InvariantCultureIgnoreCase))
        {
          string bitrateStr = token.Substring(9).Trim();
          int spacePos = bitrateStr.IndexOf(" ");
          if (spacePos > -1)
          {
            string value = bitrateStr.Substring(0, spacePos);
            string unit = bitrateStr.Substring(spacePos + 1);
            int bitrate = int.Parse(value, CultureInfo.InvariantCulture);
            if (unit.Equals("mb/s"))
            {
              bitrate = 1024 * bitrate;
            }
            info.Metadata.Bitrate = bitrate;
          }
        }
      }
    }
  }
}
