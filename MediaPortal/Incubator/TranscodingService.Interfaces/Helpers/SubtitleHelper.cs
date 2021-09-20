#region Copyright (C) 2007-2020 Team MediaPortal

/*
    Copyright (C) 2007-2020 Team MediaPortal
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

using MediaPortal.Common.MediaManagement.DefaultItemAspects;

namespace MediaPortal.Extensions.TranscodingService.Interfaces.Helpers
{
  public class SubtitleHelper
  {
    public static bool SubtitleIsUnicode(string encoding)
    {
      if (string.IsNullOrEmpty(encoding))
      {
        return false;
      }
      if (encoding.ToUpperInvariant().StartsWith("UTF-") || encoding.ToUpperInvariant().StartsWith("UNICODE"))
      {
        return true;
      }
      return false;
    }

    public static bool SubtitleIsImage(string encoding)
    {
      if (string.IsNullOrEmpty(encoding))
      {
        return false;
      }
      if (encoding.ToUpperInvariant().StartsWith("BIN"))
      {
        return true;
      }
      return false;
    }

    public static SubtitleCodec GetSubtitleCodec(string subtitleFormat)
    {
        if (subtitleFormat == SubtitleAspect.FORMAT_SRT)
          return  SubtitleCodec.Srt;
        if (subtitleFormat == SubtitleAspect.FORMAT_MICRODVD)
          return SubtitleCodec.MicroDvd;
        if (subtitleFormat == SubtitleAspect.FORMAT_SUBVIEW)
          return SubtitleCodec.SubView;
        if (subtitleFormat == SubtitleAspect.FORMAT_ASS)
          return SubtitleCodec.Ass;
        if (subtitleFormat == SubtitleAspect.FORMAT_SSA)
          return SubtitleCodec.Ssa;
        if (subtitleFormat == SubtitleAspect.FORMAT_SMI)
          return SubtitleCodec.Smi;
        if (subtitleFormat == SubtitleAspect.FORMAT_WEBVTT)
          return SubtitleCodec.WebVtt;
        if (subtitleFormat == SubtitleAspect.FORMAT_PGS)
          return SubtitleCodec.HdmvPgs;
        if (subtitleFormat == SubtitleAspect.FORMAT_VOBSUB)
          return SubtitleCodec.VobSub;
        if (subtitleFormat == SubtitleAspect.FORMAT_DVBTEXT)
          return SubtitleCodec.DvbTxt;

        return SubtitleCodec.Unknown;
    }

    public static string GetSubtitleMime(SubtitleCodec codec)
    {
      switch (codec)
      {
        case SubtitleCodec.Srt:
          return "text/srt";
        case SubtitleCodec.MicroDvd:
          return "text/microdvd";
        case SubtitleCodec.SubView:
          return "text/plain";
        case SubtitleCodec.Ass:
          return "text/x-ass";
        case SubtitleCodec.Ssa:
          return "text/x-ssa";
        case SubtitleCodec.Smi:
          return "smi/caption";
        case SubtitleCodec.WebVtt:
          return "text/vtt";
      }
      return "text/plain";
    }

    public static string GetSubtitleExtension(SubtitleCodec codec)
    {
      switch (codec)
      {
        case SubtitleCodec.Srt:
          return "srt";
        case SubtitleCodec.MicroDvd:
          return "sub";
        case SubtitleCodec.SubView:
          return "sub";
        case SubtitleCodec.Ass:
          return "ass";
        case SubtitleCodec.Ssa:
          return "ssa";
        case SubtitleCodec.Smi:
          return "smi";
        case SubtitleCodec.WebVtt:
          return "vtt";
      }
      return "mpts";
    }
  }
}
