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

using MediaPortal.Common.Configuration.ConfigurationClasses;
using SkinSettings;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MediaPortal.UI.Presentation.DataObjects;
using MediaPortal.UI.Presentation.Models;
using MediaPortal.UI.Presentation.Screens;
using MediaPortal.UI.Presentation.Workflow;
using MediaPortal.Common;
using MediaPortal.Common.Settings;
using System.Threading.Tasks;

namespace MediaPortal.UiComponents.Nereus.Settings.Configuration
{
  public class DialogOpacityConfiguration : LimitedNumberSelect, IDisposable
  {
    public DialogOpacityConfiguration()
    {
      SkinChangeMonitor.Instance.RegisterConfiguration(NereusSkinSettings.SKIN_NAME, this);
    }

    // Accessed and set from FanartVisibilityController
    public bool UseRoundedDialogCorners { get; set; }
    public bool UseNoColor { get; set; }
    public bool UseWhiteColor { get; set; }
    public bool UseFocusColor { get; set; }
    public bool UseTransparency { get; set; }


    public override void Load()
    {
      base.Load();
      _lowerLimit = 0.7;
      _upperLimit = 1.0;
      _step = 0.05;
      _type = NumberType.FloatingPoint;
      var settings = SettingsManager.Load<NereusSkinSettings>();
      _value = settings.DialogBackgroundOpacity;

      UseRoundedDialogCorners = settings.UseRoundedDialogCorners;
      UseNoColor = settings.UseNoColor;
      UseWhiteColor = settings.UseWhiteColor;
      UseFocusColor = settings.UseFocusColor;
      UseTransparency = settings.UseTransparency;
    }

    public override void Save()
    {
      base.Save();
      var settings = SettingsManager.Load<NereusSkinSettings>();
      settings.DialogBackgroundOpacity = _value;
      settings.EnableFanart = UseRoundedDialogCorners;
      settings.EnableFanart = UseNoColor;
      settings.EnableFanart = UseWhiteColor;
      settings.EnableFanart = UseFocusColor;
      settings.EnableFanart = UseTransparency;
      SettingsManager.Save(settings);
    }

    public void Refresh()
    {
      IScreenManager screenManager = ServiceRegistration.Get<IScreenManager>();
      screenManager.Reload();
    }

    public void Dispose()
    {
      SkinChangeMonitor.Instance.UnregisterConfiguration(NereusSkinSettings.SKIN_NAME, this);
    }
  }
}
