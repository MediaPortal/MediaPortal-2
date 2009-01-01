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
using MediaPortal.Configuration.ConfigurationClasses;

namespace UiComponents.SkinBase.Settings.Configuration
{
  public class TimeFormat : Entry
  {
    #region Base overrides

    public override Type SettingsObjectType
    {
      get { return typeof(SkinBaseSettings); }
    }

    public override void Load(object settingsObject)
    {
      _value = ((SkinBaseSettings) settingsObject).TimeFormat;
    }

    public override void Save(object settingsObject)
    {
      ((SkinBaseSettings) settingsObject).TimeFormat = _value;
    }

    public override void Apply()
    {
      SkinMessaging.SendSkinMessage(SkinMessaging.NotificationType.DateTimeFormatChanged);
    }

    #endregion
  }
}