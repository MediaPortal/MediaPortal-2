using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace MediaPortal.Plugins.Transcoding.Service.Transcoders.FFMpeg.Parsers
{
  public class FFMpegParseStreamAudioLine
  {
    internal static void ParseStreamAudioLine(string streamAudioLine, ref MetadataContainer info, Dictionary<string, CultureInfo> countryCodesMapping)
    {
      streamAudioLine = streamAudioLine.Trim();
      AudioStream audio = new AudioStream();
      string[] tokens = streamAudioLine.Split(',');
      foreach (string mediaToken in tokens)
      {
        string token = mediaToken.Trim();
        if (token.StartsWith("Stream", StringComparison.InvariantCultureIgnoreCase))
        {
          Match match = Regex.Match(token, @"#[\d][\.:](?<stream>[\d]{1,2}).*\((?<lang>(\w+))\)[\.:]", RegexOptions.IgnoreCase);
          if (match.Success)
          {
            audio.StreamIndex = Convert.ToInt32(match.Groups["stream"].Value.Trim());
            if (match.Groups.Count == 4)
            {
              string lang = match.Groups["lang"].Value.Trim().ToUpperInvariant();
              if (countryCodesMapping.ContainsKey(lang))
              {
                audio.Language = countryCodesMapping[lang].TwoLetterISOLanguageName.ToUpperInvariant();
              }
            }
          }
          else
          {
            match = Regex.Match(token, @"#[\d][\.:](?<stream>[\d]{1,2}).*[\.:]", RegexOptions.IgnoreCase);
            if (match.Success)
            {
              audio.StreamIndex = Convert.ToInt32(match.Groups["stream"].Value.Trim());
            }
          }

          string[] parts = token.Substring(token.IndexOf("Audio: ", StringComparison.InvariantCultureIgnoreCase) + 7).Split(' ');
          string codec = parts[0];
          string codecDetails = null;
          if (parts.Length > 1)
          {
            string details = string.Join(" ", parts).Trim();
            if (details.StartsWith("("))
            {
              int iIndex = details.IndexOf("(");
              codecDetails = details.Substring(iIndex + 1, details.IndexOf(")") - iIndex - 1);
            }
          }
          audio.Codec = FFMpegParseAudioCodec.ParseAudioCodec(codec, codecDetails);
        }
        else if (token.IndexOf("channels", StringComparison.InvariantCultureIgnoreCase) > -1)
        {
          audio.Channels = int.Parse(token.Substring(0, token.IndexOf("channels", StringComparison.InvariantCultureIgnoreCase)).Trim(), CultureInfo.InvariantCulture);
        }
        else if (token.IndexOf("stereo", StringComparison.InvariantCultureIgnoreCase) > -1)
        {
          audio.Channels = 2;
        }
        else if (token.Contains("5.1"))
        {
          audio.Channels = 6;
        }
        else if (token.Contains("7.1"))
        {
          audio.Channels = 8;
        }
        else if (token.IndexOf("mono", StringComparison.InvariantCultureIgnoreCase) > -1)
        {
          audio.Channels = 1;
        }
        else if (token.IndexOf("Hz", StringComparison.InvariantCultureIgnoreCase) > -1)
        {
          audio.Frequency = long.Parse(token.Substring(0, token.IndexOf("Hz", StringComparison.InvariantCultureIgnoreCase)).Trim(), CultureInfo.InvariantCulture);
        }
        else if (token.IndexOf("kb/s", StringComparison.InvariantCultureIgnoreCase) > -1)
        {
          audio.Bitrate = int.Parse(token.Substring(0, token.IndexOf("kb/s", StringComparison.InvariantCultureIgnoreCase)).Trim(), CultureInfo.InvariantCulture);
        }
        else if (token.IndexOf("mb/s", StringComparison.InvariantCultureIgnoreCase) > -1)
        {
          audio.Bitrate = long.Parse(token.Substring(0, token.IndexOf("mb/s", StringComparison.InvariantCultureIgnoreCase)).Trim(), CultureInfo.InvariantCulture) * 1024;
        }
      }
      audio.Default = streamAudioLine.IndexOf("(default)", StringComparison.InvariantCultureIgnoreCase) > -1;
      info.Audio.Add(audio);
    }
  }
}
