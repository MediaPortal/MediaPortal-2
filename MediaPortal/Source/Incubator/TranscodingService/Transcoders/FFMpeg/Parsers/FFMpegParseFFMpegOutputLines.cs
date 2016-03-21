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

using System.Collections.Generic;
using System.Globalization;
using MediaPortal.Plugins.Transcoding.Interfaces.Metadata;

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
