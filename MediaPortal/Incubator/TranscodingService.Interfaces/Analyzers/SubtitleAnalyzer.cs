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

namespace MediaPortal.Extensions.TranscodingService.Interfaces.Analyzers
{
  public class SubtitleAnalyzer
  {
    public static bool IsImageBasedSubtitle(SubtitleCodec codec)
    {
      if (codec == SubtitleCodec.DvbSub || codec == SubtitleCodec.VobSub)
        return true;

      return false;
    }

    public static bool IsSubtitleSupportedByContainer(SubtitleCodec codec, VideoContainer sourceContainer, VideoContainer targetContainer)
    {
      if (targetContainer != VideoContainer.Unknown && sourceContainer == targetContainer) return true;
      if (targetContainer == VideoContainer.Matroska && (codec == SubtitleCodec.VobSub || codec == SubtitleCodec.Ass || 
        codec == SubtitleCodec.MicroDvd || codec == SubtitleCodec.Smi || codec == SubtitleCodec.Srt || codec == SubtitleCodec.Ssa ||
        codec == SubtitleCodec.SubView)) return true;
      if (targetContainer == VideoContainer.Mpeg2Ps && codec == SubtitleCodec.VobSub) return true;
      if (targetContainer == VideoContainer.Mpeg2Ts && (codec == SubtitleCodec.DvbSub || codec == SubtitleCodec.DvbTxt)) return true;
      if (targetContainer == VideoContainer.Avi && codec == SubtitleCodec.Srt) return true;
      if (targetContainer == VideoContainer.Asf && codec == SubtitleCodec.Srt) return true;
      if (targetContainer == VideoContainer.Hls && codec == SubtitleCodec.MovTxt) return true;
      if (targetContainer == VideoContainer.M2Ts && codec == SubtitleCodec.VobSub) return true;
      if (targetContainer == VideoContainer.Mp4 && codec == SubtitleCodec.MovTxt) return true;
      if (targetContainer == VideoContainer.Hls && codec == SubtitleCodec.WebVtt) return true;
      if (targetContainer == VideoContainer.Mpeg1 && codec == SubtitleCodec.VobSub) return true;
      return false;
    }
  }
}
