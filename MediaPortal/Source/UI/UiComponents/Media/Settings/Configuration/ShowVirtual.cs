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

using MediaPortal.Common.Configuration.ConfigurationClasses;
using MediaPortal.Common.Localization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MediaPortal.UiComponents.Media.Settings.Configuration
{
  public class ShowVirtual : MultipleSelectionList
  {
    public ShowVirtual()
    {
      _items.Add(LocalizationHelper.CreateResourceString("[Settings.Media.ShowVirtual.Audio]"));
      _items.Add(LocalizationHelper.CreateResourceString("[Settings.Media.ShowVirtual.Movie]"));
      _items.Add(LocalizationHelper.CreateResourceString("[Settings.Media.ShowVirtual.Series]"));
    }

    public override void Load()
    {
      base.Load();
      ViewSettings settings = SettingsManager.Load<ViewSettings>();
      if (settings.ShowVirtualAudioMedia)
        _selected.Add(0);
      if (settings.ShowVirtualMovieMedia)
        _selected.Add(1);
      if (settings.ShowVirtualSeriesMedia)
        _selected.Add(2);
    }

    public override void Save()
    {
      base.Save();
      ViewSettings settings = SettingsManager.Load<ViewSettings>();
      settings.ShowVirtualAudioMedia = _selected.Contains(0);
      settings.ShowVirtualMovieMedia = _selected.Contains(1);
      settings.ShowVirtualSeriesMedia = _selected.Contains(2);
      SettingsManager.Save(settings);
    }
  }
}
