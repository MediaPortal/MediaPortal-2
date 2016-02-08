#region Copyright (C) 2007-2015 Team MediaPortal

/*
    Copyright (C) 2007-2015 Team MediaPortal
    http://www.team-mediaportal.com

    This file is part of MediaPortal 2

    MediaPortal 2 is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    MediaPortal 2 is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with MediaPortal 2. If not, see <http://www.gnu.org/licenses/>.
*/

#endregion

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using MediaPortal.Plugins.Transcoding.Service.Metadata;
using MediaPortal.Plugins.Transcoding.Service.Metadata.Streams;

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
