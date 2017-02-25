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
using System.Collections.Generic;
using System.Linq;
using MediaPortal.Common.Configuration.ConfigurationClasses;
using MediaPortal.Common.Localization;

namespace MediaPortal.UI.Players.BassPlayer.Settings.Configuration
{
  public class SongTransitionMode : SingleSelectionList
  {
    #region Fields

    private IList<PlaybackMode> _playbackModes;

    #endregion

    #region Base overrides

    public override void Load()
    {
      _playbackModes = new List<PlaybackMode>(Enum.GetValues(typeof(PlaybackMode)).Cast<PlaybackMode>().ToList());
      PlaybackMode selectedPlaybackMode = SettingsManager.Load<BassPlayerSettings>().SongTransitionMode;
      Selected = _playbackModes.IndexOf(selectedPlaybackMode);

      // Fill items
      _items = _playbackModes.Select(pbm => LocalizationHelper.CreateStaticString(pbm.ToString())).ToList();
    }

    public override void Save()
    {
      BassPlayerSettings settings = SettingsManager.Load<BassPlayerSettings>();
      int selected = Selected;
      settings.SongTransitionMode = selected > -1 && selected < _playbackModes.Count ? _playbackModes[selected] : PlaybackMode.Normal;
      SettingsManager.Save(settings);
    }

    #endregion
  }
}
