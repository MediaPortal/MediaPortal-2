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
  public class DialogSettingController : NumberSelectController
  {
    protected AbstractProperty _dialogBackgroundOpacityProperty;
    protected AbstractProperty _useRoundedDialogCornersProperty;
    protected AbstractProperty _useNoColorProperty;
    protected AbstractProperty _useWhiteColorProperty;
    protected AbstractProperty _useFocusColorProperty;
    protected AbstractProperty _useTransparencyProperty;

    public DialogSettingController()
    {
      _useRoundedDialogCornersProperty = new WProperty(typeof(bool), true);
      _useNoColorProperty = new WProperty(typeof(bool), true);
      _useWhiteColorProperty = new WProperty(typeof(bool), true);
      _useFocusColorProperty = new WProperty(typeof(bool), true);
      _useTransparencyProperty = new WProperty(typeof(bool), true);
    }

    // For binding to the checkbox in the screen
    public AbstractProperty UseRoundedDialogCornersProperty
    {
      get { return _useRoundedDialogCornersProperty; }
    }
    public bool UseRoundedDialogCorners
    {
      get { return (bool)_useRoundedDialogCornersProperty.GetValue(); }
      set { _useRoundedDialogCornersProperty.SetValue(value); }
    }

    public AbstractProperty UseTransparencyProperty
    {
      get { return _useTransparencyProperty; }
    }
    public bool UseTransparency
    {
      get { return (bool)_useTransparencyProperty.GetValue(); }
      set { _useTransparencyProperty.SetValue(value); }
    }

    public AbstractProperty UseNoColorProperty
    {
      get { return _useNoColorProperty; }
    }
    public bool UseNoColor
    {
      get { return (bool)_useNoColorProperty.GetValue(); }
      set { _useNoColorProperty.SetValue(value); }
    }

    public AbstractProperty UseWhiteColorProperty
    {
      get { return _useWhiteColorProperty; }
    }
    public bool UseWhiteColor
    {
      get { return (bool)_useWhiteColorProperty.GetValue(); }
      set { _useWhiteColorProperty.SetValue(value); }
    }

    public AbstractProperty UseFocusColorProperty
    {
      get { return _useFocusColorProperty; }
    }
    public bool UseFocusColor
    {
      get { return (bool)_useFocusColorProperty.GetValue(); }
      set { _useFocusColorProperty.SetValue(value); }
    }

    // Called when the setting is going to be displayed, update properties
    // from the underlying setting so they are available to the screen
    protected override void SettingChanged()
    {
      // This handles all the number related settings in the base class
      base.SettingChanged();
      // Cast the setting stored in the base class to our setting type
      DialogOpacityConfiguration dialogSetting = (DialogOpacityConfiguration)_setting;

      // Update the enableFanart setting
      UseRoundedDialogCorners = dialogSetting.UseRoundedDialogCorners;
      UseNoColor = dialogSetting.UseNoColor;
      UseWhiteColor = dialogSetting.UseWhiteColor;
      UseFocusColor = dialogSetting.UseFocusColor;
      UseTransparency = dialogSetting.UseTransparency;
  }

    // Called when saving the setting
    protected override void UpdateSetting()
    {
      // Get the underlying setting
      DialogOpacityConfiguration dialogSetting = (DialogOpacityConfiguration)_setting;
      // and save any changes back to it
      dialogSetting.UseRoundedDialogCorners = UseRoundedDialogCorners;
      dialogSetting.UseNoColor = UseNoColor;
      dialogSetting.UseWhiteColor = UseWhiteColor;
      dialogSetting.UseFocusColor = UseFocusColor;
      dialogSetting.UseTransparency = UseTransparency;
      // This saves the number related setting in the base class
      base.UpdateSetting();
    }

    // Use custom dialog
    protected override string DialogScreen
    {
      get { return "dialogDialogSettings"; }
    }
  }
}

