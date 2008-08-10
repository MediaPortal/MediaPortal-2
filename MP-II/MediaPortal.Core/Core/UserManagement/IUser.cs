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
using System.Collections.Generic;
using MediaPortal.Services.UserManagement;

namespace MediaPortal.Core.UserManagement
{
  public interface IUser
  {
    /// <summary>
    /// gets or sets the name for this user
    /// </summary>
    string UserName { get; set; }

    /// <summary>
    /// gets or sets the path to the userimage
    /// </summary>
    string UserImage { get; set; }

    /// <summary>
    /// gets or sets the path to the userimage
    /// </summary>
    string Password { get; set; }

    /// <summary>
    /// returns true if a password is needed to login, false otherwise
    /// </summary>
    bool NeedsPassword { get; set; }

    /// <summary>
    /// gets or sets the last time of login
    /// </summary>
    DateTime LastLogin { get; set; }

    /// <summary>
    /// adds a role to this user
    /// </summary>
    /// <param name="role">the role to add</param>
    /// <returns>true if added, false otherwise</returns>
    bool AddRole(IRole role);

    /// <summary>
    /// removes a role from this user
    /// </summary>
    /// <param name="role">the role to remove</param>
    /// <returns>true if removed, false otherwise</returns>
    bool RemoveRole(IRole role);

    /// <summary>
    /// gets the roles assigned to this user
    /// </summary>
    /// <returns>list of roles for this user</returns>
    List<IRole> GetRoles();

    /// <summary>
    /// checks if this user has permission on a IPermissionObject
    /// </summary>
    /// <param name="obj">the oject to check permission for</param>
    /// <returns>true if the user has permission to access the object, false otherwise</returns>
    bool HasPermissionOn(IPermissionObject obj);
  }
}
