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

using MediaPortal.Common;
using MediaPortal.Common.Settings;
using System;
using System.Collections.Generic;
using System.Linq;
using MediaPortal.Common.UserManagement;
using MediaPortal.Common.UserProfileDataManagement;
using MediaPortal.UI.Services.UserManagement;

namespace MediaPortal.UiComponents.Login.Settings
{
  public static class UserSettingStorage
  {
    private static bool _userLoginEnabled = false;

    public static void Refresh()
    {
      ISettingsManager localSettings = ServiceRegistration.Get<ISettingsManager>();
      UserSettings settings = localSettings.Load<UserSettings>();
      UserLoginEnabled = settings.EnableUserLogin;
      UserLoginScreenEnabled = settings.EnableUserLoginScreen;
      AutoLoginUser = settings.AutoLoginUser;
      AutoLogoutEnabled = settings.AutoLogoutEnabled;
      AutoLogoutIdleTimeoutInMin = settings.AutoLogoutIdleTimeoutInMin;

      var templates = localSettings.Load<UserTemplateSettings>();
      UserProfileTemplates = templates.UserProfileTemplates?.Count > 0 ? 
        templates.UserProfileTemplates :
        UserTemplateSettings.DEFAULT_USER_PROFILE_TEMPLATES;

      IUserManagement userManagement = ServiceRegistration.Get<IUserManagement>();
      if (userManagement != null)
        userManagement.ApplyUserRestriction = UserLoginEnabled;
    }

    public static bool UserLoginEnabled
    {
      get
      {
        return _userLoginEnabled;
      }
      set
      {
        if (_userLoginEnabled != value)
        {
          IUserManagement userManagement = ServiceRegistration.Get<IUserManagement>();
          if (userManagement != null)
            userManagement.ApplyUserRestriction = value;
        }
        _userLoginEnabled = value;
      }
    }
    public static Guid AutoLoginUser { get; set; }
    public static bool AutoLogoutEnabled { get; set; }
    public static int AutoLogoutIdleTimeoutInMin { get; set; }
    public static bool UserLoginScreenEnabled { get; set; }
    public static ICollection<UserProfileTemplate> UserProfileTemplates { get; set; }
  }
}
