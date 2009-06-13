#region Copyright (C) 2007-2008 Team MediaPortal

/*
    Copyright (C) 2007-2008 Team MediaPortal
    http://www.team-mediaportal.com
 
    This file is part of MediaPortal II

    MediaPortal II is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    MediaPortal II is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with MediaPortal II.  If not, see <http://www.gnu.org/licenses/>.
*/

#endregion

using System;
using MediaPortal.Core.Configuration;
using MediaPortal.Core.General;
using MediaPortal.Core.Localization;

namespace UiComponents.Configuration
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
    protected Property _helpProperty;
    protected Property _textProperty;
    protected Property _visibleProperty;
    protected Property _enabledProperty;

    protected ConfigurationController()
    {
      _helpProperty = new Property(typeof(IResourceString), null);
      _textProperty = new Property(typeof(IResourceString), null);
      _visibleProperty = new Property(typeof(bool), true);
      _enabledProperty = new Property(typeof(bool), true);
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

    public abstract bool IsSettingSupported(ConfigSetting setting);

    public abstract Type ConfigSettingType { get; }

    public ConfigSetting Setting
    {
      get { return _setting; }
    }

    public Property HelpProperty
    {
      get { return _helpProperty; }
    }

    public IResourceString Help
    {
      get { return (IResourceString) _helpProperty.GetValue(); }
      set { _helpProperty.SetValue(value); }
    }

    public Property TextProperty
    {
      get { return _textProperty; }
    }

    public IResourceString Text
    {
      get { return (IResourceString) _textProperty.GetValue(); }
      set { _textProperty.SetValue(value); }
    }

    public Property VisibleProperty
    {
      get { return _visibleProperty; }
    }

    public bool Visible
    {
      get { return (bool) _visibleProperty.GetValue(); }
      set { _visibleProperty.SetValue(value); }
    }

    public Property EnabledProperty
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
