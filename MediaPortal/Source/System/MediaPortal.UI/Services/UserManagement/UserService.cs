#region Copyright (C) 2007-2010 Team MediaPortal

/*
    Copyright (C) 2007-2010 Team MediaPortal
    http://www.team-mediaportal.com
 
    This file is part of MediaPortal 2

    MediaPortal 2 is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    MediaPortal 2 is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with MediaPortal 2.  If not, see <http://www.gnu.org/licenses/>.
*/

#endregion

using System.Collections.Generic;
using MediaPortal.Core;
using MediaPortal.Core.Logging;
using MediaPortal.UI.UserManagement;

namespace MediaPortal.UI.Services.UserManagement
{
  /// <summary>
  /// Service that provides user management.
  /// </summary>
  public class UserService : IUserService
  {
    protected IUser _currentUser = null;
    protected IList<IUser> _users;

    public UserService()
    {
      _users = new List<IUser>();
    }

    public IUser CurrentUser
    {
      get { return _currentUser; }
      set { _currentUser = value; }
    }

    public IUser AddUser(string name)
    {
      User result = new User(name);
      _users.Add(result);
      return result;
    }

    public bool RemoveUser(IUser user)
    {
      return _users.Remove(user);
    }

    public IList<IUser> GetUsers()
    {
      return _users;
    }
  }
}
