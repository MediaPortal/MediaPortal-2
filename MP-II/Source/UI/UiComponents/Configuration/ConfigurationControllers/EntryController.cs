#region Copyright (C) 2007-2009 Team MediaPortal

/*
    Copyright (C) 2007-2009 Team MediaPortal
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
using MediaPortal.Core.Configuration.ConfigurationClasses;
using MediaPortal.Core.General;

namespace UiComponents.Configuration.ConfigurationControllers
{
  /// <summary>
  /// Configuration controller for the <see cref="Entry"/> configuration setting.
  /// </summary>
  public class EntryController : DialogConfigurationController
  {
    #region Protected fields

    protected AbstractProperty _valueProperty;
    protected AbstractProperty _displayLengthProperty;

    #endregion

    public EntryController()
    {
      _valueProperty = new WProperty(typeof(string), string.Empty);
      _displayLengthProperty = new WProperty(typeof(int), 0);
    }

    public override Type ConfigSettingType
    {
      get { return typeof(Entry); }
    }

    protected override string DialogScreen
    {
      get { return "dialog_configuration_entry"; }
    }

    protected override void SettingChanged()
    {
      base.SettingChanged();
      if (_setting == null)
        return;
      Entry entry = (Entry) _setting;
      Value = entry.Value;
      DisplayLength = entry.DisplayLength;
    }

    protected override void UpdateSetting()
    {
      Entry entry = (Entry) _setting;
      entry.Value = Value;
      base.UpdateSetting();
    }

    public AbstractProperty ValueProperty
    {
      get { return _valueProperty; }
    }

    public string Value
    {
      get { return (string) _valueProperty.GetValue(); }
      set { _valueProperty.SetValue(value); }
    }

    public AbstractProperty DisplayLengthProperty
    {
      get { return _displayLengthProperty; }
      internal set { _displayLengthProperty.SetValue(value); }
    }

    public int DisplayLength
    {
      get { return (int) _displayLengthProperty.GetValue(); }
      set { _displayLengthProperty.SetValue(value); }
    }
  }
}
