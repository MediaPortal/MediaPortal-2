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

namespace MediaPortal.Extensions.TranscodingService.Interfaces.Helpers
{
  public class AudioHelper
  {
    public static string GetAudioExtension(AudioContainer container)
    {
      switch (container)
      {
        case AudioContainer.Ac3:
          return "ac3";
        case AudioContainer.Ape:
          return "ape";
        case AudioContainer.Asf:
          return "wma";
        case AudioContainer.Flac:
          return "flac";
        case AudioContainer.Flv:
          return "flv";
        case AudioContainer.Lpcm:
          return "lpcm";
        case AudioContainer.Mp4:
          return "mp4a";
        case AudioContainer.Mp3:
          return "mp3";
        case AudioContainer.Mp2:
          return "mp2";
        case AudioContainer.Ogg:
          return "ogg";
        case AudioContainer.WavPack:
          return "wav";
        case AudioContainer.Mpeg2Ts:
          return "mpg";
        case AudioContainer.Dsf:
          return "dsf";
        case AudioContainer.Wav:
          return "wav";
      }
      return "mpta";
    }
  }
}
