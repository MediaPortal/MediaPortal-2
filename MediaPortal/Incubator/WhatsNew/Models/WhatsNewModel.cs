#region Copyright (C) 2007-2021 Team MediaPortal

/*
    Copyright (C) 2007-2021 Team MediaPortal
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

using MediaPortal.Common;
using MediaPortal.Common.Settings;
using MediaPortal.UI.Presentation.Screens;
using MediaPortal.UiComponents.Media.Settings;
using WhatsNew.Settings;

namespace WhatsNew.Models
{
  public class WhatsNewModel
  {
    public const string CURRENT_VERSION = "2.4";

    public void ChangeSkin()
    {
      IScreenManager screenManager = ServiceRegistration.Get<IScreenManager>();
      if (screenManager.CurrentSkinResourceBundle.SkinName != "Nereus")
        screenManager.SwitchSkinAndTheme("Nereus", "default");
    }

    public void ChangeSkinDefaults()
    {
      var settingsManager = ServiceRegistration.Get<ISettingsManager>();
      var viewSettings = settingsManager.Load<ViewSettings>();
      foreach (var screenConfig in viewSettings.ScreenConfigs)
      {
        screenConfig.Value.LayoutSize = ViewSettings.DEFAULT_LAYOUT_SIZE;
        screenConfig.Value.LayoutType = ViewSettings.DEFAULT_LAYOUT_TYPE;
        screenConfig.Value.AdditionalProperties["extEnableGridDetails"] = "True";
        screenConfig.Value.AdditionalProperties["extEnableListDetails"] = "True";
        screenConfig.Value.AdditionalProperties["extEnableCoverDetails"] = "True";
        screenConfig.Value.AdditionalProperties["extEnableGridWatchFlags"] = "True";
        screenConfig.Value.AdditionalProperties["extEnableListWatchFlags"] = "True";
        screenConfig.Value.AdditionalProperties["extEnableCoverWatchFlags"] = "True";
      }
      settingsManager.Save(viewSettings);
      IScreenManager screenManager = ServiceRegistration.Get<IScreenManager>();
      screenManager.CloseTopmostDialog();
    }

    public void Dismiss()
    {
      var settingsManager = ServiceRegistration.Get<ISettingsManager>();
      var settings = settingsManager.Load<WhatsNewSettings>();
      settings.NewsConfirmedVersion = CURRENT_VERSION;
      settingsManager.Save(settings);

      IScreenManager screenManager = ServiceRegistration.Get<IScreenManager>();
      screenManager.CloseTopmostDialog();
    }
  }
}
