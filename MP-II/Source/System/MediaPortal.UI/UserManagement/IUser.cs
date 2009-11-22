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
using System.Collections.Generic;
using MediaPortal.UI.Services.UserManagement;

namespace MediaPortal.UI.UserManagement
{
  /// <summary>
  /// Interface that provides access to the public data for a registered user object.
  /// </summary>
  public interface IUser
  {
    /// <summary>
    /// Gets or sets the name for this user.
    /// </summary>
    string UserName { get; set; }

    /// <summary>
    /// Gets or sets the user's password.
    /// </summary>
    string Password { get; set; }

    /// <summary>
    /// Gets or sets the path or name of the userimage.
    /// </summary>
    string UserImage { get; set; }

    /// <summary>
    /// Returns the information if a password is needed to login for this user.
    /// </summary>
    bool NeedsPassword { get; set; }

    /// <summary>
    /// Gets or sets the last time of login.
    /// </summary>
    DateTime LastLogin { get; set; }

    /// <summary>
    /// Adds a role to this user.
    /// </summary>
    /// <param name="role">The role to be added.</param>
    /// <returns><c>true</c>, if the role could successfully be added, else <c>false</c>.</returns>
    bool AddRole(IRole role);

    /// <summary>
    /// Removes a role from this user.
    /// </summary>
    /// <param name="role">The role to be removed.</param>
    /// <returns><c>true</c>, if the role could successfully be removed, else <c>false</c>.</returns>
    bool RemoveRole(IRole role);

    /// <summary>
    /// Gets the roles assigned to this user.
    /// </summary>
    /// <returns>List of roles for this user.</returns>
    IList<IRole> GetRoles();

    /// <summary>
    /// Checks if this user has permission on an <see cref="IPermissionObject"/>.
    /// </summary>
    /// <param name="obj">The oject to check permission for.</param>
    /// <returns><c>true</c>, if the user has permission to access the object, else <c>false</c>.</returns>
    bool HasPermissionOn(IPermissionObject obj);
  }
}
