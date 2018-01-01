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
using System.Threading.Tasks;
using DirectShow;
using MediaPortal.UI.Players.Video.Tools;

namespace MediaPortal.UI.Players.Video.Settings.Configuration
{
  public class AudioCodecLATMAAC : GenericCodecSelection
  {
    public AudioCodecLATMAAC()
      : base(
        new Guid[] { MediaType.Audio, CodecHandler.MEDIASUBTYPE_LATM_AAC_AUDIO }, // require LATM-AAC input
        new Guid[] { MediaType.Audio, Guid.Empty} // require any Audio output
      )
    { }

    public override async Task Load()
    {
      // Load settings
      VideoSettings settings = await SettingsManager.LoadAsync<VideoSettings>();
      if (settings != null && settings.AudioCodecLATMAAC != null)
        _currentSelection = settings.AudioCodecLATMAAC.GetCLSID();
      await base.Load();
    }

    public override async Task Save()
    {
      // Load settings
      VideoSettings settings = await SettingsManager.LoadAsync<VideoSettings>();
      settings.AudioCodecLATMAAC = _codecList[Selected];
      await SettingsManager.SaveAsync(settings);
    }
  }
}
