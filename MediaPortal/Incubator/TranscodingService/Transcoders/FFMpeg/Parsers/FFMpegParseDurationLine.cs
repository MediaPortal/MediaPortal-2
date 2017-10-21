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
using System.Globalization;
using MediaPortal.Plugins.Transcoding.Interfaces.Metadata;

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
