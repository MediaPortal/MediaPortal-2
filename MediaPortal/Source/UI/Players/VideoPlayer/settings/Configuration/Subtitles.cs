#region Copyright (C) 2007-2018 Team MediaPortal

/*
    Copyright (C) 2007-2018 Team MediaPortal
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

using MediaPortal.Common.Configuration.ConfigurationClasses;
using MediaPortal.Common.Localization;

namespace MediaPortal.UI.Players.Video.Settings.Configuration
{
  public class Subtitles : MultipleSelectionList
  {
    public Subtitles()
    {
      _items.Add(LocalizationHelper.CreateResourceString("[Settings.Players.EnableAtscClosedCaptions]"));
      _items.Add(LocalizationHelper.CreateResourceString("[Settings.Players.EnableDvbSubtitles]"));
      _items.Add(LocalizationHelper.CreateResourceString("[Settings.Players.EnableTeletextSubtitles]"));
      _items.Add(LocalizationHelper.CreateResourceString("[Settings.Players.EnableMpcHcEngineSubtitles]"));
      _items.Add(LocalizationHelper.CreateResourceString("[Settings.Players.EnableDvdSubtitles]"));
      _items.Add(LocalizationHelper.CreateResourceString("[Settings.Players.EnableDvdClosedCaptions]"));
    }

    public override void Load()
    {
      base.Load();
      var settings = SettingsManager.Load<VideoSettings>();
      if (settings.EnableAtscClosedCaptions)
        _selected.Add(0);
      if (settings.EnableDvbSubtitles)
        _selected.Add(1);
      if (settings.EnableTeletextSubtitles)
        _selected.Add(2);
      if (settings.EnableMpcSubtitlesEngine)
        _selected.Add(3);
      if (settings.EnableDvdSubtitles)
        _selected.Add(4);
      if (settings.EnableDvdClosedCaptions)
        _selected.Add(5);
    }

    public override void Save()
    {
      base.Save();
      var settings = SettingsManager.Load<VideoSettings>();
      settings.EnableAtscClosedCaptions = _selected.Contains(0);
      settings.EnableDvbSubtitles = _selected.Contains(1);
      settings.EnableTeletextSubtitles = _selected.Contains(2);
      settings.EnableMpcSubtitlesEngine = _selected.Contains(3);
      settings.EnableDvdSubtitles = _selected.Contains(4);
      settings.EnableDvdClosedCaptions = _selected.Contains(5);
      SettingsManager.Save(settings);
    }
  }
}
