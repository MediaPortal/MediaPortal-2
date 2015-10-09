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
  internal class FFMpegGetAudioCodec
  {
    public static string GetAudioCodec(AudioCodec codec)
    {
      switch (codec)
      {
        case AudioCodec.Mp3:
          return "libmp3lame";
        case AudioCodec.Mp2:
          return "mp2";
        case AudioCodec.Aac:
          return "libvo_aacenc";
        case AudioCodec.Ac3:
          return "ac3";
        case AudioCodec.Lpcm:
          return "pcm_s16le";
        case AudioCodec.Dts:
          return "dca";
        case AudioCodec.Wma:
          return "wmav1";
        case AudioCodec.Flac:
          return "flac";
        case AudioCodec.Vorbis:
          return "libvorbis";
        case AudioCodec.Amr:
          return "libopencore_amrnb";
        case AudioCodec.Real:
          return "ralf";
        case AudioCodec.Alac:
          return "alac";
        case AudioCodec.Speex:
          return "libspeex";
      }
      return "copy";
    }
  }
}
