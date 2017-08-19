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

using MediaPortal.Common;
using MediaPortal.Common.Services.Settings;
using MediaPortal.UI.Services.UserManagement;
using System;

namespace MediaPortal.UiComponents.Login.Settings
{
  public static class UserSettingWatcher
  {
    private static SettingsChangeWatcher<UserSettings> _settingsWatcher;
    private static bool _userLoginEnabled = false;

    static UserSettingWatcher()
    {
      _settingsWatcher = new SettingsChangeWatcher<UserSettings>();
      _settingsWatcher.SettingsChanged += SettingsChanged;

      UserLoginEnabled = _settingsWatcher.Settings.EnableUserLogin;
    }

    private static void SettingsChanged(object sender, EventArgs e)
    {
      UserLoginEnabled = _settingsWatcher.Settings.EnableUserLogin;
    }

    public static bool UserLoginEnabled
    {
      get
      {
        return _userLoginEnabled;
      }
      private set
      {
        _userLoginEnabled = value;

        IUserManagement userManagement = ServiceRegistration.Get<IUserManagement>();
        if (userManagement != null)
          userManagement.ApplyUserRestriction = value;
      }
    }
  }
}
