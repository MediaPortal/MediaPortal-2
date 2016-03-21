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
  public class FFMpegParseAudioContainer
  {
    internal static AudioContainer ParseAudioContainer(string token)
    {
      if (token != null)
      {
        if (token.Equals("ac3", StringComparison.InvariantCultureIgnoreCase))
          return AudioContainer.Ac3;
        if (token.Equals("adts", StringComparison.InvariantCultureIgnoreCase))
          return AudioContainer.Adts;
        if (token.Equals("ape", StringComparison.InvariantCultureIgnoreCase))
          return AudioContainer.Ape;
        if (token.Equals("asf", StringComparison.InvariantCultureIgnoreCase))
          return AudioContainer.Asf;
        if (token.Equals("flac", StringComparison.InvariantCultureIgnoreCase))
          return AudioContainer.Flac;
        if (token.Equals("flv", StringComparison.InvariantCultureIgnoreCase))
          return AudioContainer.Flv;
        if (token.Equals("lpcm", StringComparison.InvariantCultureIgnoreCase))
          return AudioContainer.Lpcm;
        if (token.Equals("mov", StringComparison.InvariantCultureIgnoreCase) || token.Equals("mp4", StringComparison.InvariantCultureIgnoreCase) ||
          token.Equals("aac", StringComparison.InvariantCultureIgnoreCase))
          return AudioContainer.Mp4;
        if (token.Equals("mp3", StringComparison.InvariantCultureIgnoreCase))
          return AudioContainer.Mp3;
        if (token.Equals("mp2", StringComparison.InvariantCultureIgnoreCase))
          return AudioContainer.Mp2;
        if (token.Equals("musepack", StringComparison.InvariantCultureIgnoreCase) || token.Equals("mpc", StringComparison.InvariantCultureIgnoreCase))
          return AudioContainer.MusePack;
        if (token.Equals("ogg", StringComparison.InvariantCultureIgnoreCase))
          return AudioContainer.Ogg;
        if (token.Equals("rtp", StringComparison.InvariantCultureIgnoreCase))
          return AudioContainer.Rtp;
        if (token.Equals("rtsp", StringComparison.InvariantCultureIgnoreCase))
          return AudioContainer.Rtsp;
        if (token.Equals("wavpack", StringComparison.InvariantCultureIgnoreCase))
          return AudioContainer.WavPack;
      }
      return AudioContainer.Unknown;
    }
  }
}
