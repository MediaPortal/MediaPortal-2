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
using MediaPortal.Core.Configuration.ConfigurationClasses;
using MediaPortal.Core.General;

namespace UiComponents.Configuration.ConfigurationControllers
{
  /// <summary>
  /// Configuration controller for the <see cref="YesNo"/> configuration setting.
  /// </summary>
  public class YesNoController : DialogConfigurationController
  {
    #region Protected fields

    protected Property _yesProperty;

    #endregion

    public YesNoController()
    {
      _yesProperty = new Property(typeof(bool), false);
    }

    public override Type ConfigSettingType
    {
      get { return typeof(YesNo); }
    }

    protected override string DialogScreen
    {
      get { return "dialog_configuration_yesno"; }
    }

    protected override void SettingChanged()
    {
      base.SettingChanged();
      if (_setting == null)
        return;
      YesNo yesNo = (YesNo) _setting;
      Yes = yesNo.Yes;
    }

    protected override void UpdateSetting()
    {
      YesNo yesNo = (YesNo) _setting;
      yesNo.Yes = Yes;
      base.UpdateSetting();
    }

    public Property YesProperty
    {
      get { return _yesProperty; }
    }

    public bool Yes
    {
      get { return (bool) _yesProperty.GetValue(); }
      set { _yesProperty.SetValue(value); }
    }
  }
}