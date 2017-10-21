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

using MediaPortal.Plugins.Transcoding.Interfaces;

namespace MediaPortal.Plugins.Transcoding.Service.Transcoders.FFMpeg.Converters
{
  internal class FFMpegGetSubtitleContainer
  {
    public static string GetSubtitleContainer(SubtitleCodec codec)
    {
      switch (codec)
      {
        case SubtitleCodec.Srt:
          return "srt";
        case SubtitleCodec.MicroDvd:
          return "microdvd";
        case SubtitleCodec.SubView:
          return "subviewer";
        case SubtitleCodec.Ass:
          return "ass";
        case SubtitleCodec.Ssa:
          return "ssa";
        case SubtitleCodec.Smi:
          return "sami";
        case SubtitleCodec.MovTxt:
          return "mov_text";
        case SubtitleCodec.DvbSub:
          return "dvbsub";
        case SubtitleCodec.WebVtt:
          return "webvtt";
      }
      return "copy";
    }
  }
}
