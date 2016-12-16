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
using MediaPortal.UI.Players.Video.Tools;

namespace MediaPortal.UI.Players.Video.Settings.Configuration
{
  public class VideoVC1Codec : GenericCodecSelection
  {
    public VideoVC1Codec()
      : base(
        new Guid[] { MediaType.Video, CodecHandler.MEDIASUBTYPE_VC1 }, // require VC1 video input
        new Guid[] { MediaType.Video, Guid.Empty } // requires any video output, but exclude streams (as produced my Muxers)
      ) { }

    public override void Load()
    {
      // Load settings
      BluRayPlayerSettings settings = SettingsManager.Load<BluRayPlayerSettings>();
      if (settings != null && settings.VC1Codec != null)
        _currentSelection = settings.VC1Codec.GetCLSID();
      base.Load();
    }

    public override void Save()
    {
      // Load settings
      BluRayPlayerSettings settings = SettingsManager.Load<BluRayPlayerSettings>();
      settings.VC1Codec = _codecList[Selected];
      SettingsManager.Save(settings);
    }
  }
}
