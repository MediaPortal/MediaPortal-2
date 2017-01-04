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

namespace MediaPortal.Plugins.RefreshRateChanger.Settings.Configuration
{
  public class RateMappings : Entry
  {
    public override void Load()
    {
      base.Load();
      var dict = SettingsManager.Load<RefreshRateChangerSettings>().RateMappings;
      List<string> rates = dict.Keys.Select(key => string.Format("{0}:{1}", key, dict[key])).ToList();
      _value = string.Join("; ", rates.ToArray());
    }

    public override void Save()
    {
      base.Save();

      RefreshRateChangerSettings settings = SettingsManager.Load<RefreshRateChangerSettings>();
      settings.RateMappings.Clear();

      var parts = _value.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
      foreach (var part in parts)
      {
        var map = part.Split(new[] { ':' }, StringSplitOptions.RemoveEmptyEntries);
        if (map.Length == 2)
        {
          int fps;
          int hz;
          if (int.TryParse(map[0], out fps) && int.TryParse(map[1], out hz))
            settings.RateMappings[fps] = hz;
        }
      }

      SettingsManager.Save(settings);
    }

    public override int DisplayLength
    {
      get { return 50; }
    }
  }
}
