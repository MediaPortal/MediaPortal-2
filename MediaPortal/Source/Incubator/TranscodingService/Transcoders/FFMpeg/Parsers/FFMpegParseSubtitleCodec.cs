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
using MediaPortal.Plugins.Transcoding.Interfaces;

namespace MediaPortal.Plugins.Transcoding.Service.Transcoders.FFMpeg.Parsers
{
  public class FFMpegParseSubtitleCodec
  {
    internal static SubtitleCodec ParseSubtitleCodec(string token)
    {
      if (token != null)
      {
        if (token.Equals("ass", StringComparison.InvariantCultureIgnoreCase))
          return SubtitleCodec.Ass;
        if (token.Equals("ssa", StringComparison.InvariantCultureIgnoreCase))
          return SubtitleCodec.Ssa;
        if (token.Equals("mov_text", StringComparison.InvariantCultureIgnoreCase))
          return SubtitleCodec.MovTxt;
        if (token.Equals("sami", StringComparison.InvariantCultureIgnoreCase))
          return SubtitleCodec.Smi;
        if (token.Equals("srt", StringComparison.InvariantCultureIgnoreCase) || token.Equals("subrip", StringComparison.InvariantCultureIgnoreCase))
          return SubtitleCodec.Srt;
        if (token.Equals("microdvd", StringComparison.InvariantCultureIgnoreCase))
          return SubtitleCodec.MicroDvd;
        if (token.Equals("subviewer", StringComparison.InvariantCultureIgnoreCase))
          return SubtitleCodec.SubView;
        if (token.Equals("webvtt", StringComparison.InvariantCultureIgnoreCase))
          return SubtitleCodec.WebVtt;
        if (token.Equals("dvb_subtitle", StringComparison.InvariantCultureIgnoreCase) || token.Equals("dvbsub", StringComparison.InvariantCultureIgnoreCase))
          return SubtitleCodec.DvbSub;
        if (token.Equals("dvb_teletext", StringComparison.InvariantCultureIgnoreCase))
          return SubtitleCodec.DvbTxt;
        if (token.Equals("dvd_subtitle", StringComparison.InvariantCultureIgnoreCase) || token.Equals("dvdsub", StringComparison.InvariantCultureIgnoreCase))
          return SubtitleCodec.VobSub;
      }
      return SubtitleCodec.Unknown;
    }
  }
}
