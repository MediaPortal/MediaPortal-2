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

using MediaPortal.Common.General;
using MediaPortal.UiComponents.Configuration.ConfigurationControllers;
using MediaPortal.UiComponents.Nereus.Settings.Configuration;

namespace MediaPortal.UiComponents.Nereus.Settings.ConfigurationControllers
{
  public class FanartVisibilityController : NumberSelectController
  {
    protected AbstractProperty _enableFanartProperty;

    public FanartVisibilityController()
    {
      _enableFanartProperty = new WProperty(typeof(bool), false);
    }

    public AbstractProperty EnableFanartProperty
    {
      get { return _enableFanartProperty; }
    }

    // For binding to the checkbox in the screen
    public bool EnableFanart
    {
      get { return (bool)_enableFanartProperty.GetValue(); }
      set { _enableFanartProperty.SetValue(value); }
    }

    // Called when the setting is going to be displayed, update properties
    // from the underlying setting so they are available to the screen
    protected override void SettingChanged()
    {
      // This handles all the number related settings in the base class
      base.SettingChanged();
      // Cast the setting stored in the base class to our setting type
      FanartOverlayOpacityConfiguration fanartSetting = (FanartOverlayOpacityConfiguration)_setting;
      // Update the enableFanart setting
      EnableFanart = fanartSetting.EnableFanart;
    }

    // Called when saving the setting
    protected override void UpdateSetting()
    {
      // Get the underlying setting
      FanartOverlayOpacityConfiguration fanartSetting = (FanartOverlayOpacityConfiguration)_setting;
      // and save any changes back to it
      fanartSetting.EnableFanart = EnableFanart;
      // This saves the number related setting in the base class
      base.UpdateSetting();
    }

    // Use custom dialog
    protected override string DialogScreen
    {
      get { return "dialogFanartSettings"; }
    }
  }
}

