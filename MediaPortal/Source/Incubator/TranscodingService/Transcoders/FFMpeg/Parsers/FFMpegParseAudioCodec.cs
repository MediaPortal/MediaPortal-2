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

namespace MediaPortal.Plugins.Transcoding.Service.Transcoders.FFMpeg.Parsers
{
  public class FFMpegParseAudioCodec
  {
    internal static AudioCodec ParseAudioCodec(string token, string detailToken)
    {
      if (token != null)
      {
        if (token.Equals("aac", StringComparison.InvariantCultureIgnoreCase) || token.Equals("mpeg4aac", StringComparison.InvariantCultureIgnoreCase) ||
          token.Equals("aac_latm", StringComparison.InvariantCultureIgnoreCase))
          return AudioCodec.Aac;
        if (token.Equals("ac3", StringComparison.InvariantCultureIgnoreCase) || token.StartsWith("ac-3", StringComparison.InvariantCultureIgnoreCase) ||
          token.Equals("liba52", StringComparison.InvariantCultureIgnoreCase) || token.Equals("eac3", StringComparison.InvariantCultureIgnoreCase))
          return AudioCodec.Ac3;
        if (token.Equals("amrnb", StringComparison.InvariantCultureIgnoreCase) || token.Equals("amr_nb", StringComparison.InvariantCultureIgnoreCase) ||
          token.Equals("amrwb", StringComparison.InvariantCultureIgnoreCase) || token.Equals("amr_wb", StringComparison.InvariantCultureIgnoreCase))
          return AudioCodec.Amr;
        if (token.StartsWith("dca", StringComparison.InvariantCultureIgnoreCase) || token.StartsWith("dts", StringComparison.InvariantCultureIgnoreCase))
        {
          if (detailToken != null && detailToken.Equals("dts-hd ma", StringComparison.InvariantCultureIgnoreCase))
          {
            return AudioCodec.DtsHd;
          }
          return AudioCodec.Dts;
        }
        if (token.Equals("dts-hd", StringComparison.InvariantCultureIgnoreCase))
          return AudioCodec.DtsHd;
        if (token.Equals("flac", StringComparison.InvariantCultureIgnoreCase))
          return AudioCodec.Flac;
        if (token.Equals("lpcm", StringComparison.InvariantCultureIgnoreCase) || token.StartsWith("pcm_", StringComparison.InvariantCultureIgnoreCase) ||
          token.StartsWith("adpcm_", StringComparison.InvariantCultureIgnoreCase))
          return AudioCodec.Lpcm;
        if (token.Equals("mp3", StringComparison.InvariantCultureIgnoreCase))
          return AudioCodec.Mp3;
        if (token.Equals("mp2", StringComparison.InvariantCultureIgnoreCase))
          return AudioCodec.Mp2;
        if (token.Equals("mp1", StringComparison.InvariantCultureIgnoreCase))
          return AudioCodec.Mp1;
        if (token.Equals("ralf", StringComparison.InvariantCultureIgnoreCase) || token.StartsWith("real", StringComparison.InvariantCultureIgnoreCase) ||
          token.Equals("sipr", StringComparison.InvariantCultureIgnoreCase) || token.Equals("cook", StringComparison.InvariantCultureIgnoreCase))
          return AudioCodec.Real;
        if (token.Equals("truehd", StringComparison.InvariantCultureIgnoreCase))
          return AudioCodec.TrueHd;
        if (token.Equals("vorbis", StringComparison.InvariantCultureIgnoreCase) || token.Equals("libvorbis", StringComparison.InvariantCultureIgnoreCase))
          return AudioCodec.Vorbis;
        if (token.Equals("wmav1", StringComparison.InvariantCultureIgnoreCase) || token.Equals("wmav2", StringComparison.InvariantCultureIgnoreCase))
          return AudioCodec.Wma;
        if (token.Equals("wmapro", StringComparison.InvariantCultureIgnoreCase) || token.Equals("0x0162", StringComparison.InvariantCultureIgnoreCase))
          return AudioCodec.WmaPro;
        if (token.Equals("wmalossless", StringComparison.InvariantCultureIgnoreCase))
          return AudioCodec.WmaLossless;
        if (token.Equals("alac", StringComparison.InvariantCultureIgnoreCase))
          return AudioCodec.Alac;
        if (token.Equals("speex", StringComparison.InvariantCultureIgnoreCase) || token.Equals("libspeex", StringComparison.InvariantCultureIgnoreCase))
          return AudioCodec.Speex;
        if (token.Equals("ape", StringComparison.InvariantCultureIgnoreCase))
          return AudioCodec.Ape;
      }
      return AudioCodec.Unknown;
    }
  }
}
