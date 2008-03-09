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

using System.Collections.Generic;
using MediaPortal.Core.UserManagement;

namespace MediaPortal.Services.UserManagement
{
  /// <summary>
  /// implements a role
  /// </summary>
  public class Role : IRole
  {
    private List<IPermission> _permissions;
    private string _name;

    /// <summary>
    /// ctor
    /// </summary>
    public Role()
    {
      _permissions = new List<IPermission>();
    }

    /// <summary>
    /// gets or sets the name of this role
    /// </summary>
    public string Name
    {
      get { return _name; }
      set { _name = value; }
    }

    /// <summary>
    /// Adds a permission to this role
    /// </summary>
    /// <param name="permission">the permission to add</param>
    /// <returns>true if the permission has been added, false otherwise</returns>
    public bool AddPermission(IPermission permission)
    {
      _permissions.Add(permission);
      return true;
    }

    /// <summary>
    /// Removes a permission from this role
    /// </summary>
    /// <param name="permission">the permission to remove</param>
    /// <returns>true if the permission has been removed, false otherwise</returns>
    public bool RemovePermission(IPermission permission)
    {
      return _permissions.Remove(permission);
    }

    /// <summary>
    /// Gets the list of Permissions assigned to this role
    /// </summary>
    /// <returns>list of permissions for this role</returns>
    public List<IPermission> GetPermissions()
    {
      return _permissions;
    }
  }
}
