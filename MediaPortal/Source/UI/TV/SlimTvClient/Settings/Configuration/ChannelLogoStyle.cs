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

using System.Linq;
using MediaPortal.Common;
using MediaPortal.Common.Configuration.ConfigurationClasses;
using MediaPortal.Common.Localization;
using MediaPortal.Plugins.ServerSettings;
using MediaPortal.Plugins.SlimTv.Interfaces.Settings;

namespace MediaPortal.Plugins.SlimTv.Client.Settings.Configuration
{
  public class ChannelLogoStyleSetting : SingleSelectionList
  {
    public override void Load()
    {
      _items.Clear();
      IServerSettingsClient serverSettings = ServiceRegistration.Get<IServerSettingsClient>(false);
      if (serverSettings == null)
        return;
      SlimTvLogoSettings settings = serverSettings.Load<SlimTvLogoSettings>();
      foreach (var themes in settings.LogoThemes.Distinct())
        _items.Add(LocalizationHelper.CreateStaticString(themes));
      Selected = _items.IndexOf(LocalizationHelper.CreateStaticString(settings.LogoTheme));
    }

    public override void Save()
    {
      IServerSettingsClient serverSettings = ServiceRegistration.Get<IServerSettingsClient>(false);
      if (serverSettings == null)
        return;
      SlimTvLogoSettings settings = serverSettings.Load<SlimTvLogoSettings>();
      settings.LogoTheme = _items[Selected].Evaluate();
      serverSettings.Save(settings);
    }
  }
}
