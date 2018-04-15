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

using System;
using DirectShow;

namespace MediaPortal.UI.Players.Video.Settings.Configuration
{
  public class VideoMPEG4Codec : GenericCodecSelection
  {
    public VideoMPEG4Codec()
      : base(
        new Guid[] { MediaType.Video, MediaSubType.H264 }, // require H264 video input
        new Guid[] { MediaType.Video, Guid.Empty } // requires any video output, but exclude streams (as produced my Muxers)
      ) { }

    public override void Load()
    {
      // Load settings
      VideoSettings settings = SettingsManager.Load<VideoSettings>();
      if (settings != null && settings.H264Codec != null)
        _currentSelection = settings.H264Codec.GetCLSID();
      base.Load();
    }

    public override void Save()
    {
      // Load settings
      VideoSettings settings = SettingsManager.Load<VideoSettings>();
      settings.H264Codec = _codecList[Selected];
      SettingsManager.Save(settings);
    }
  }
}
