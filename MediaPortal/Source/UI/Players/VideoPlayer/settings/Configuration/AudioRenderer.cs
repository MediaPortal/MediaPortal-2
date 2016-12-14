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

using MediaPortal.UI.Players.Video.Tools;

namespace MediaPortal.UI.Players.Video.Settings.Configuration
{
  public class AudioRenderer : GenericCodecSelection
  {
    public AudioRenderer()
      : base(null,null)
    {
      _selectByName = true; // ClsIds of audio renderer devices appear multiple times, as one device can offer multiple renderers. So we compare here by device names.
    }

    protected override void GetAvailableFilters()
    {
      _codecList = CodecHandler.GetAudioRenderers();
    }

    public override void Load()
    {
      // Load settings
      VideoSettings settings = SettingsManager.Load<VideoSettings>();
      if (settings != null && settings.AudioRenderer != null)
        _currentSelectionName = settings.AudioRenderer.Name;

      base.Load();
    }

    public override void Save()
    {
      // Load settings
      VideoSettings settings = SettingsManager.Load<VideoSettings>();
      settings.AudioRenderer = _codecList[Selected];
      SettingsManager.Save(settings);
    }
  }
}
