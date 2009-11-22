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

using System.Collections.Generic;

namespace MediaPortal.UI.UserManagement
{
  /// <summary>
  /// Interface that defines a role. A role itself provides all methods to check
  /// permissions on items (inherited from <see cref="IPermission"/>, and adds the
  /// functionality to contain a set of child permissions (or roles), which will be
  /// granted as a whole if the role is assigned to a user.
  /// </summary>
  public interface IRole: IPermission
  {
    /// <summary>
    /// Adds a permission to this role.
    /// </summary>
    /// <param name="permission">The permission to add.</param>
    /// <returns><c>true</c>, if the permission could be added, <c>false</c> otherwise.</returns>
    bool AddPermission(IPermission permission);

    /// <summary>
    /// Removes a permission from this role.
    /// </summary>
    /// <param name="permission">The permission to remove.</param>
    /// <returns><c>true</c>, if the permission could be removed, <c>false</c> otherwise.</returns>
    bool RemovePermission(IPermission permission);

    /// <summary>
    /// Gets the list of Permissions contained in this role.
    /// </summary>
    /// <returns>List of permissions in this role.</returns>
    IList<IPermission> GetPermissions();
  }
}
