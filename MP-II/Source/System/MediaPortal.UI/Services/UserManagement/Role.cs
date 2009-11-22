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
using MediaPortal.UI.UserManagement;

namespace MediaPortal.UI.Services.UserManagement
{
  /// <summary>
  /// Implements a role.
  /// </summary>
  public class Role : Permission, IRole
  {
    protected IList<IPermission> _permissions;

    public Role(string name): base(name)
    {
      _permissions = new List<IPermission>();
    }

    public bool AddPermission(IPermission permission)
    {
      _permissions.Add(permission);
      return true;
    }

    public bool RemovePermission(IPermission permission)
    {
      return _permissions.Remove(permission);
    }

    public IList<IPermission> GetPermissions()
    {
      return _permissions;
    }

    #region Base overrides

    public override IList<IPermissionObject> GetPermissionObjects()
    {
      List<IPermissionObject> result = new List<IPermissionObject>(base.GetPermissionObjects());
      foreach (IPermission permission in _permissions)
        foreach (IPermissionObject obj in permission.GetPermissionObjects())
          result.Add(obj);
      return result;
    }

    public override bool IncludesPermissionOn(IPermissionObject item)
    {
      if (base.IncludesPermissionOn(item))
        return true;
      foreach (IPermission permission in _permissions)
        if (permission.IncludesPermissionOn(item))
          return true;
      return false;
    }

    #endregion
  }
}
