#region Copyright (C) 2007 Team MediaPortal

/*
    Copyright (C) 2007 Team MediaPortal
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

using System.Collections.Generic;
using MediaPortal.Core;
using MediaPortal.Core.Logging;
using MediaPortal.Core.UserManagement;

namespace MediaPortal.Services.UserManagement
{
  /// <summary>
  /// Service that provides Usermanagement
  /// </summary>
  public class UserService : IUserService
  {
    protected List<IUser> _users;

    /// <summary>
    /// constructor
    /// </summary>
    public UserService()
    {
      _users = new List<IUser>();
    }

    /// <summary>
    /// Adds a new User to the Service
    /// </summary>
    /// <param name="user">the user to add</param>
    /// <returns>true if user has been added successfully, false otherwise</returns>
    public bool AddUser(IUser user)
    {
      foreach (IUser listuser in _users)
      {
        if (user.UserName.Equals(listuser.UserName))
        {
          ServiceScope.Get<ILogger>().Warn(
            "UserService: Attempted to add a user with a name that is already in the list. Adding failed");
          return false;
        }
      }
      _users.Add(user);
      return true;
    }

    /// <summary>
    /// Removes a User from the service
    /// </summary>
    /// <param name="user">the user to remove</param>
    /// <returns>true if the user has been removed successfully, false otherwise</returns>
    public bool RemoveUser(IUser user)
    {
      return _users.Remove(user);
    }

    /// <summary>
    /// retrieves a list of all registered Users
    /// </summary>
    /// <returns>List of IUser</returns>
    public List<IUser> GetUsers()
    {
      return _users;
    }
  }
}