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

using System;
using MediaPortal.Common;
using MediaPortal.Common.Configuration;
using MediaPortal.Common.General;
using MediaPortal.Common.Localization;
using MediaPortal.Common.UserManagement;
using MediaPortal.UI.Services.UserManagement;

namespace MediaPortal.UiComponents.Configuration
{
  /// <summary>
  /// Base class for all configuration controllers.
  /// </summary>
  /// <remarks>
  /// Configuration controllers control the interaction between the MP configuration GUI and
  /// the <see cref="ConfigSetting"/> subclasses. The controller prepares the setting
  /// data for the GUI dialogs and provides the function to transfer the edited data
  /// back to the setting.
  /// </remarks>
  public abstract class ConfigurationController
  {
    public const string CONFIGURATION_MODEL_ID_STR = "545674F1-D92A-4383-B6C1-D758CECDBDF5";

    protected ConfigSetting _setting;
    protected AbstractProperty _helpProperty;
    protected AbstractProperty _textProperty;
    protected AbstractProperty _visibleProperty;
    protected AbstractProperty _enabledProperty;

    protected ConfigurationController()
    {
      _helpProperty = new WProperty(typeof(IResourceString), null);
      _textProperty = new WProperty(typeof(IResourceString), null);
      _visibleProperty = new WProperty(typeof(bool), true);
      _enabledProperty = new WProperty(typeof(bool), true);
    }

    protected void OnSettingChanged(ConfigSetting sender)
    {
      SettingChanged();
    }

    public void Initialize(ConfigSetting setting)
    {
      if (_setting != null)
        _setting.Changed -= OnSettingChanged;
      _setting = setting;
      if (_setting != null)
      {
        _setting.Changed += OnSettingChanged;
        _setting.Load();
      }
      SettingChanged();
    }

    /// <summary>
    /// Saves the associated config setting.
    /// </summary>
    public void Save()
    {
      UpdateSetting();
      _setting.Save();
    }

    protected virtual void SettingChanged()
    {
      Help = _setting.Help;
      Text = _setting.Text;
      Visible = _setting.Visible;
      Enabled = _setting.Enabled;
    }

    protected virtual void UpdateSetting()
    {
    }

    public abstract void ExecuteConfiguration();

    public virtual bool IsSettingSupported(ConfigSetting setting)
    {
      if (setting == null)
        return false;
      IUserManagement userManagement = ServiceRegistration.Get<IUserManagement>();
      return userManagement.CheckUserAccess(setting);
    }

    public abstract Type ConfigSettingType { get; }

    public ConfigSetting Setting
    {
      get { return _setting; }
    }

    public AbstractProperty HelpProperty
    {
      get { return _helpProperty; }
    }

    public IResourceString Help
    {
      get { return (IResourceString) _helpProperty.GetValue(); }
      set { _helpProperty.SetValue(value); }
    }

    public AbstractProperty TextProperty
    {
      get { return _textProperty; }
    }

    public IResourceString Text
    {
      get { return (IResourceString) _textProperty.GetValue(); }
      set { _textProperty.SetValue(value); }
    }

    public AbstractProperty VisibleProperty
    {
      get { return _visibleProperty; }
    }

    public bool Visible
    {
      get { return (bool) _visibleProperty.GetValue(); }
      set { _visibleProperty.SetValue(value); }
    }

    public AbstractProperty EnabledProperty
    {
      get { return _enabledProperty; }
    }

    public bool Enabled
    {
      get { return (bool) _enabledProperty.GetValue(); }
      set { _enabledProperty.SetValue(value); }
    }
  }
}
