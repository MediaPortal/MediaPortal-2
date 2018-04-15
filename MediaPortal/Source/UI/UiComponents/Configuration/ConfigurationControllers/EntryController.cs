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
using MediaPortal.Common.Configuration.ConfigurationClasses;

namespace MediaPortal.UiComponents.Configuration.ConfigurationControllers
{
  /// <summary>
  /// Configuration controller for the <see cref="Entry"/> configuration setting.
  /// </summary>
  public class EntryController : AbstractEntryController
  {
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
  }
}
