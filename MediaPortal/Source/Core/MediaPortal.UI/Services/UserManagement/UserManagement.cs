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
using System.Windows.Forms;
using MediaPortal.Common;
using MediaPortal.Common.Services.ServerCommunication;
using MediaPortal.Common.UserProfileDataManagement;
using MediaPortal.UI.ServerCommunication;

namespace MediaPortal.UI.Services.UserManagement
{
  public class UserManagement : IUserManagement
  {
    public static UserProfile UNKNWON_USER = new UserProfile(Guid.Empty, "Unkwown");

    private UserProfile _currentUser = null;

    public bool IsValidUser
    {
      get { return CurrentUser != UNKNWON_USER; }
    }

    public UserProfile CurrentUser
    {
      get { return _currentUser ?? (_currentUser = GetOrCreateDefaultUser() ?? UNKNWON_USER); }
      set { _currentUser = value; }
    }

    public IUserProfileDataManagement UserProfileDataManagement
    {
      get
      {
        UPnPClientControlPoint controlPoint = ServiceRegistration.Get<IServerConnectionManager>().ControlPoint;
        return controlPoint != null ? controlPoint.UserProfileDataManagementService : null;
      }
    }

    public UserProfile GetOrCreateDefaultUser()
    {
      UserProfile user = null;
      string profileName = SystemInformation.ComputerName;
      IUserProfileDataManagement updm = UserProfileDataManagement;
      if (updm != null && !updm.GetProfileByName(profileName, out user))
      {
        Guid profileId = updm.CreateProfile(profileName);
        if (!updm.GetProfile(profileId, out user))
          return null;
      }
      return user;
    }
  }
}
