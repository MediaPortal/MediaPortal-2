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

using System;
using MediaPortal.Extensions.TranscodingService.Interfaces;
using MediaPortal.Common.ResourceAccess;

namespace MediaPortal.Extensions.TranscodingService.Service.Transcoders.FFMpeg.Parsers
{
  public class FFMpegParseAudioContainer
  {
    internal static AudioContainer ParseAudioContainer(string token, IResourceAccessor ra)
    {
      if (token != null)
      {
        if (token.StartsWith("image", StringComparison.InvariantCultureIgnoreCase))
          return AudioContainer.Unknown;

        if (token.Equals("ac3", StringComparison.InvariantCultureIgnoreCase))
          return AudioContainer.Ac3;
        if (token.Equals("adts", StringComparison.InvariantCultureIgnoreCase))
          return AudioContainer.Adts;
        if (token.Equals("ape", StringComparison.InvariantCultureIgnoreCase))
          return AudioContainer.Ape;
        if (token.Equals("asf", StringComparison.InvariantCultureIgnoreCase) || token.Equals("wmav1", StringComparison.InvariantCultureIgnoreCase) || 
          token.Equals("wmav2", StringComparison.InvariantCultureIgnoreCase))
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
        if (token.Equals("wavpack", StringComparison.InvariantCultureIgnoreCase) || token.Equals("wv", StringComparison.InvariantCultureIgnoreCase))
          return AudioContainer.WavPack;
        if (token.Equals("mpegts", StringComparison.InvariantCultureIgnoreCase))
          return AudioContainer.Mpeg2Ts;
        if (token.Equals("dsf", StringComparison.InvariantCultureIgnoreCase))
          return AudioContainer.Dsf;
        if (token.Equals("wav", StringComparison.InvariantCultureIgnoreCase))
          return AudioContainer.Wav;
      }

      //Try file extensions
      var ext = ra?.ResourceName;
      if (ext != null)
      {
        if (ext.EndsWith(".ac3", StringComparison.InvariantCultureIgnoreCase))
          return AudioContainer.Ac3;
        if (ext.EndsWith(".mp3", StringComparison.InvariantCultureIgnoreCase))
          return AudioContainer.Mp3;
        if (ext.EndsWith(".mp2", StringComparison.InvariantCultureIgnoreCase))
          return AudioContainer.Mp2;
        if (ext.EndsWith(".wma", StringComparison.InvariantCultureIgnoreCase))
          return AudioContainer.Asf;
        if (ext.EndsWith(".mp4", StringComparison.InvariantCultureIgnoreCase) || ext.EndsWith(".mp4a", StringComparison.InvariantCultureIgnoreCase))
          return AudioContainer.Mp4;
        if (ext.EndsWith(".flac", StringComparison.InvariantCultureIgnoreCase))
          return AudioContainer.Flac;
        if (ext.EndsWith(".ogg", StringComparison.InvariantCultureIgnoreCase))
          return AudioContainer.Ogg;
      }

      return AudioContainer.Unknown;
    }
  }
}
