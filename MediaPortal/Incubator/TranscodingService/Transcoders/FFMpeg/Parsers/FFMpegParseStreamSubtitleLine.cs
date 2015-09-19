using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace MediaPortal.Plugins.Transcoding.Service.Transcoders.FFMpeg.Parsers
{
  public class FFMpegParseStreamSubtitleLine
  {
    internal static void ParseStreamSubtitleLine(string streamSubtitleLine, ref MetadataContainer info, Dictionary<string, CultureInfo>  countryCodesMapping)
    {
      streamSubtitleLine = streamSubtitleLine.Trim();

      SubtitleStream sub = new SubtitleStream();
      Match match = Regex.Match(streamSubtitleLine, @"#[\d][\.:](?<stream>[\d]{1,2}).*\((?<lang>(\w+))\)[\.:]", RegexOptions.IgnoreCase);
      if (match.Success)
      {
        sub.StreamIndex = Convert.ToInt32(match.Groups["stream"].Value.Trim());
        if (match.Groups.Count == 4)
        {
          string lang = match.Groups["lang"].Value.Trim().ToUpperInvariant();
          if (countryCodesMapping.ContainsKey(lang))
          {
            sub.Language = countryCodesMapping[lang].TwoLetterISOLanguageName.ToUpperInvariant();
          }
        }
      }
      else
      {
        match = Regex.Match(streamSubtitleLine, @"#[\d][\.:](?<stream>[\d]{1,2}).*[\.:]", RegexOptions.IgnoreCase);
        if (match.Success)
        {
          sub.StreamIndex = Convert.ToInt32(match.Groups["stream"].Value.Trim());
        }
      }

      string codecValue = streamSubtitleLine.Substring(streamSubtitleLine.IndexOf("Subtitle: ", StringComparison.InvariantCultureIgnoreCase) + 10).Split(' ')[0];
      sub.Codec = FFMpegParseSubtitleCodec.ParseSubtitleCodec(codecValue);
      sub.Default = streamSubtitleLine.IndexOf("(default)", StringComparison.InvariantCultureIgnoreCase) > -1;
      info.Subtitles.Add(sub);
    }
  }
}
