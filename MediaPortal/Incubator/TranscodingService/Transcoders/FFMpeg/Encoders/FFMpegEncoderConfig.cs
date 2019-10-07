﻿#region Copyright (C) 2007-2017 Team MediaPortal

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

using System.Collections.Generic;
using MediaPortal.Extensions.TranscodingService.Interfaces;

namespace MediaPortal.Extensions.TranscodingService.Service.Transcoders.FFMpeg.Encoders
{
  class FFMpegEncoderConfig
  {
    public Dictionary<VideoCodec, Dictionary<EncodingProfile, string>> Profiles { get; set; }
    public Dictionary<VideoCodec, Dictionary<EncodingPreset, string>> Presets { get; set; }
    public List<VideoCodec> SupportedCodecs { get; set; }

    public FFMpegEncoderConfig()
    {
      Profiles = new Dictionary<VideoCodec, Dictionary<EncodingProfile, string>>();
      Presets = new Dictionary<VideoCodec, Dictionary<EncodingPreset, string>>();
      SupportedCodecs = new List<VideoCodec>();
    }

    public string GetEncoderPreset(VideoCodec codec, EncodingPreset preset)
    {
      if (Presets.ContainsKey(codec))
      {
        string presetArg;
        Presets[codec].TryGetValue(preset, out presetArg);
        return presetArg;
      }
      return null;
    }

    public string GetEncoderProfile(VideoCodec codec, EncodingProfile profile)
    {
      if (Profiles.ContainsKey(codec))
      {
        string profileArg;
        Profiles[codec].TryGetValue(profile, out profileArg);
        return profileArg;
      }
      return null;
    }
  }
}
