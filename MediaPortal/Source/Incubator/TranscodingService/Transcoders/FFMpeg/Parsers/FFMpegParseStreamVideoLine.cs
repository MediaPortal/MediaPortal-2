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
using System.Linq;
using MediaPortal.Plugins.Transcoding.Interfaces.Metadata;
using MediaPortal.Plugins.Transcoding.Interfaces;

namespace MediaPortal.Plugins.Transcoding.Service.Transcoders.FFMpeg.Parsers
{
  public class FFMpegParseStreamVideoLine
  {
    internal static void ParseStreamVideoLine(string streamVideoLine, ref MetadataContainer info, Dictionary<string, CultureInfo> countryCodesMapping)
    {
      if (info.Video.Codec != VideoCodec.Unknown) return;

      streamVideoLine = streamVideoLine.Trim();
      string beforeVideo = streamVideoLine.Substring(0, streamVideoLine.IndexOf("Video:", StringComparison.InvariantCultureIgnoreCase));
      string afterVideo = streamVideoLine.Substring(beforeVideo.Length);
      string[] beforeVideoTokens = beforeVideo.Split(',');
      string[] afterVideoTokens = afterVideo.Split(',');
      foreach (string mediaToken in beforeVideoTokens)
      {
        string token = mediaToken.Trim();
        if (token.StartsWith("Stream", StringComparison.InvariantCultureIgnoreCase))
        {
          Match match = Regex.Match(token, @"#[\d][\.:](?<stream>[\d]{1,2}).*\((?<lang>(\w+))\)[\.:]", RegexOptions.IgnoreCase);
          if (match.Success)
          {
            info.Video.StreamIndex = Convert.ToInt32(match.Groups["stream"].Value.Trim());
            if (match.Groups.Count == 4)
            {
              string lang = match.Groups["lang"].Value.Trim().ToUpperInvariant();
              if (countryCodesMapping.ContainsKey(lang))
              {
                info.Video.Language = countryCodesMapping[lang].TwoLetterISOLanguageName.ToUpperInvariant();
              }
            }
          }
          else
          {
            match = Regex.Match(token, @"#[\d][\.:](?<stream>[\d]{1,2}).*[\.:]", RegexOptions.IgnoreCase);
            if (match.Success)
            {
              info.Video.StreamIndex = Convert.ToInt32(match.Groups["stream"].Value.Trim());
            }
          }
        }
      }
      bool nextTokenIsPixelFormat = false;
      foreach (string mediaToken in afterVideoTokens)
      {
        string token = mediaToken.Trim();
        if (token.StartsWith("Video:", StringComparison.InvariantCultureIgnoreCase))
        {
          string codecValue = token.Substring(token.Trim().IndexOf("Video: ", StringComparison.InvariantCultureIgnoreCase) + 7).Split(' ')[0];
          if ((codecValue != null) && (codecValue.StartsWith("drm", StringComparison.InvariantCultureIgnoreCase)))
          {
            throw new Exception(info.Metadata.Source + " is DRM protected");
          }

          string[] parts = token.Substring(token.IndexOf("Video: ", StringComparison.InvariantCultureIgnoreCase) + 7).Split(' ');
          string codec = parts[0];
          if ((codec != null) && (codec.StartsWith("drm", StringComparison.InvariantCultureIgnoreCase)))
          {
            throw new Exception(info.Metadata.Source + " is DRM protected");
          }
          string codecDetails = null;
          if (parts.Length > 1)
          {
            string details = string.Join(" ", parts).Trim();
            if (details.Contains("("))
            {
              int iIndex = details.IndexOf("(");
              codecDetails = details.Substring(iIndex + 1, details.IndexOf(")") - iIndex - 1);
            }
          }
          info.Video.Codec = FFMpegParseVideoCodec.ParseVideoCodec(codecValue);
          if (info.IsImage)
          {
            info.Metadata.ImageContainerType = FFMpegParseImageContainer.ParseImageContainer(codecValue);
          }
          if (info.Video.Codec == VideoCodec.H264 || info.Video.Codec == VideoCodec.H265)
          {
            info.Video.ProfileType = FFMpegParseProfile.ParseProfile(codecDetails);
          }

          string fourCC = token.Trim();
          if (token.Contains("("))
          {
            string fourCCBlock = fourCC.Substring(fourCC.LastIndexOf("(") + 1, fourCC.LastIndexOf(")") - fourCC.LastIndexOf("(") - 1);
            if (fourCCBlock.IndexOf("/") > -1)
            {
              fourCC = (fourCCBlock.Split('/')[0].Trim()).ToLowerInvariant();
              if (fourCC.IndexOf("[") == -1)
              {
                info.Video.FourCC = fourCC;
              }
            }
          }
          nextTokenIsPixelFormat = true;
        }
        else if (nextTokenIsPixelFormat)
        {
          nextTokenIsPixelFormat = false;
          if (info.IsImage)
            info.Image.PixelFormatType = FFMpegParsePixelFormat.ParsePixelFormat(token.Trim());
          else
            info.Video.PixelFormatType = FFMpegParsePixelFormat.ParsePixelFormat(token.Trim());
        }
        else if (token.IndexOf("x", StringComparison.InvariantCultureIgnoreCase) > -1 && token.Contains("max") == false)
        {
          string resolution = token.Trim();
          int aspectStart = resolution.IndexOf(" [");
          if (aspectStart > -1)
          {
            info.Video.PixelAspectRatio = 1.0F;
            string aspectDef = resolution.Substring(aspectStart + 2, resolution.IndexOf("]") - aspectStart - 2);
            int sarIndex = aspectDef.IndexOf("SAR"); //Sample AR
            if (sarIndex < 0)
            {
              sarIndex = aspectDef.IndexOf("PAR"); //Pixel AR
            }
            if (sarIndex > -1)
            {
              aspectDef = aspectDef.Substring(sarIndex + 4);
              string sar = aspectDef.Substring(0, aspectDef.IndexOf(" ")).Trim();
              string[] sarRatio = sar.Split(':');
              if (sarRatio.Length == 2)
              {
                try
                {
                  info.Video.PixelAspectRatio = Convert.ToSingle(sarRatio[0], CultureInfo.InvariantCulture) / Convert.ToSingle(sarRatio[1], CultureInfo.InvariantCulture);
                }
                catch
                { }
              }
            }

            resolution = resolution.Substring(0, aspectStart);
          }
          string[] res = resolution.Split('x');
          if (res.Length == 2 && res[0].All(Char.IsDigit) && res[1].All(Char.IsDigit))
          {
            try
            {
              if (info.IsImage)
              {
                info.Image.Width = Convert.ToInt32(res[0], CultureInfo.InvariantCulture);
                info.Image.Height = Convert.ToInt32(res[1], CultureInfo.InvariantCulture);
              }
              else
              {
                info.Video.Width = Convert.ToInt32(res[0], CultureInfo.InvariantCulture);
                info.Video.Height = Convert.ToInt32(res[1], CultureInfo.InvariantCulture);
              }
            }
            catch
            { }

            if (info.Video.Height > 0)
            {
              info.Video.AspectRatio = (float)info.Video.Width / (float)info.Video.Height;
            }
          }
        }
        else if (token.IndexOf("SAR", StringComparison.InvariantCultureIgnoreCase) > -1 || token.IndexOf("PAR", StringComparison.InvariantCultureIgnoreCase) > -1)
        {
          info.Video.PixelAspectRatio = 1.0F;
          string aspectDef = token.Trim();
          int sarIndex = aspectDef.IndexOf("SAR", StringComparison.InvariantCultureIgnoreCase); //Sample AR
          if (sarIndex < 0)
          {
            sarIndex = aspectDef.IndexOf("PAR", StringComparison.InvariantCultureIgnoreCase); //Pixel AR
          }
          if (sarIndex > -1)
          {
            aspectDef = aspectDef.Substring(sarIndex + 4);
            string sar = aspectDef.Substring(0, aspectDef.IndexOf(" ")).Trim();
            string[] sarRatio = sar.Split(':');
            if (sarRatio.Length == 2)
            {
              try
              {
                info.Video.PixelAspectRatio = Convert.ToSingle(sarRatio[0], CultureInfo.InvariantCulture) / Convert.ToSingle(sarRatio[1], CultureInfo.InvariantCulture);
              }
              catch
              { }
            }
          }
        }
        else if (token.IndexOf("kb/s", StringComparison.InvariantCultureIgnoreCase) > -1)
        {
          string[] parts = token.Split(' ');
          //if (parts.Length == 3 && parts[1].All(Char.IsDigit))
          //  info.Video.Bitrate = long.Parse(parts[1].Trim(), CultureInfo.InvariantCulture);
          if (parts.Length == 2 && parts[0].All(Char.IsDigit))
            info.Video.Bitrate = long.Parse(parts[0].Trim(), CultureInfo.InvariantCulture);
        }
        else if (token.IndexOf("mb/s", StringComparison.InvariantCultureIgnoreCase) > -1)
        {
          string[] parts = token.Split(' ');
          //if (parts.Length == 3 && parts[1].All(Char.IsDigit))
          //  info.Video.Bitrate = long.Parse(parts[1].Trim(), CultureInfo.InvariantCulture) * 1024;
          if (parts.Length == 2 && parts[0].All(Char.IsDigit))
            info.Video.Bitrate = long.Parse(parts[0].Trim(), CultureInfo.InvariantCulture) * 1024;
        }
        else if (token.IndexOf("tbr", StringComparison.InvariantCultureIgnoreCase) > -1 || token.IndexOf("fps", StringComparison.InvariantCultureIgnoreCase) > -1)
        {
          if (info.Video.Framerate == 0)
          {
            string fpsValue = "23.976";
            if (token.IndexOf("tbr", StringComparison.InvariantCultureIgnoreCase) > -1)
            {
              fpsValue = token.Substring(0, token.IndexOf("tbr", StringComparison.InvariantCultureIgnoreCase)).Trim();
            }
            else if (token.IndexOf("fps", StringComparison.InvariantCultureIgnoreCase) > -1)
            {
              fpsValue = token.Substring(0, token.IndexOf("fps", StringComparison.InvariantCultureIgnoreCase)).Trim();
            }
            if (fpsValue.Contains("k"))
            {
              fpsValue = fpsValue.Replace("k", "000");
            }
            double fr = 0;
            float validFrameRate = 23.976F;
            if (double.TryParse(fpsValue, out fr) == true)
            {
              if (fr > 23.899999999999999D && fr < 23.989999999999998D)
                validFrameRate = 23.976F;
              else if (fr > 23.989999999999998D && fr < 24.100000000000001D)
                validFrameRate = 24;
              else if (fr >= 24.989999999999998D && fr < 25.100000000000001D)
                validFrameRate = 25;
              else if (fr > 29.899999999999999D && fr < 29.989999999999998D)
                validFrameRate = 29.97F;
              else if (fr >= 29.989999999999998D && fr < 30.100000000000001D)
                validFrameRate = 30;
              else if (fr > 49.899999999999999D && fr < 50.100000000000001D)
                validFrameRate = 50;
              else if (fr > 59.899999999999999D && fr < 59.990000000000002D)
                validFrameRate = 59.94F;
              else if (fr >= 59.990000000000002D && fr < 60.100000000000001D)
                validFrameRate = 60;
            }
            info.Video.Framerate = validFrameRate;
          }
        }
      }
    }
  }
}
