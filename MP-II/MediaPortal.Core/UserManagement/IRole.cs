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

namespace MediaPortal.Core.UserManagement
{
  /// <summary>
  /// Interface that defines a role
  /// </summary>
  public interface IRole
  {
    /// <summary>
    /// gets or sets the name of this role
    /// </summary>
    string Name { get; set; }

    /// <summary>
    /// Adds a permission to this role
    /// </summary>
    /// <param name="permission">the permission to add</param>
    /// <returns>true if the permission has been added, false otherwise</returns>
    bool AddPermission(IPermission permission);

    /// <summary>
    /// Removes a permission from this role
    /// </summary>
    /// <param name="permission">the permission to remove</param>
    /// <returns>true if the permission has been removed, false otherwise</returns>
    bool RemovePermission(IPermission permission);

    /// <summary>
    /// Gets the list of Permissions assigned to this role
    /// </summary>
    /// <returns>list of permissions for this role</returns>
    List<IPermission> GetPermissions();
  }
}