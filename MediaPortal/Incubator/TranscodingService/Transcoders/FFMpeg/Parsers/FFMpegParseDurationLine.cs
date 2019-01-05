#region Copyright (C) 2007-2017 Team MediaPortal

/*
    Copyright (C) 2007-2017 Team MediaPortal
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
using MediaPortal.Extensions.TranscodingService.Interfaces.Metadata;

namespace MediaPortal.Extensions.TranscodingService.Service.Transcoders.FFMpeg.Parsers
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
            string hours = "0";
            string minutes = "0";
            string seconds = "0";
            string fractions = "0";
            string[] parts;
            if (duration.Contains(".") == true)
            {
              parts = duration.Split('.');
              duration = parts[0];
              fractions = parts[1];
            }
            parts = duration.Split(':');
            if(parts.Length == 3)
            {
              hours = parts[0];
              minutes = parts[1];
              seconds = parts[2];
            }
            else if (parts.Length == 2)
            {
              minutes = parts[0];
              seconds = parts[1];
            }
            var timeSpan = TimeSpan.FromMilliseconds(Convert.ToDouble(fractions));
            timeSpan += TimeSpan.FromSeconds(Convert.ToDouble(seconds));
            timeSpan += TimeSpan.FromMinutes(Convert.ToDouble(minutes));
            timeSpan += TimeSpan.FromHours(Convert.ToDouble(hours));
            info.Metadata.Duration = timeSpan.TotalSeconds;
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
