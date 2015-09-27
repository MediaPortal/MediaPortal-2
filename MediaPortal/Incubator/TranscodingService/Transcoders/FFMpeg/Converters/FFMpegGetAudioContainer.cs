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



namespace MediaPortal.Plugins.Transcoding.Service.Transcoders.FFMpeg.Converters
{
  internal class FFMpegGetAudioContainer
  {
    public static string GetAudioContainer(AudioContainer container)
    {
      switch (container)
      {
        case AudioContainer.Ac3:
          return "ac3";
        case AudioContainer.Mp3:
          return "mp3";
        case AudioContainer.Mp2:
          return "mp2";
        case AudioContainer.Asf:
          return "asf";
        case AudioContainer.Lpcm:
          return "s16le";
        case AudioContainer.Mp4:
          return "mp4";
        case AudioContainer.Flac:
          return "flac";
        case AudioContainer.Ogg:
          return "ogg";
        case AudioContainer.Flv:
          return "flv";
        case AudioContainer.Rtp:
          return "rtp";
        case AudioContainer.Rtsp:
          return "rtsp";
        case AudioContainer.Adts:
          return "adts";
      }
      return null;
    }
  }
}
