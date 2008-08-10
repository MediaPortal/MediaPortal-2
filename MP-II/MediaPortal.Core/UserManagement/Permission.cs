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
  /// implements a permission
  /// </summary>
  public class Permission : IPermission
  {
    protected List<IPermissionObject> _objects;

    /// <summary>
    /// ctor
    /// </summary>
    public Permission()
    {
      _objects = new List<IPermissionObject>();
    }

    /// <summary>
    /// adds a permisson object to this permission (f.e. a MediaItem)
    /// </summary>
    /// <param name="item">the item to add</param>
    /// <returns>true if added, false otherwise</returns>
    public bool AddPermissionObject(IPermissionObject item)
    {
      _objects.Add(item);
      return true;
    }

    /// <summary>
    /// removes a permisson object from this permission
    /// </summary>
    /// <param name="item">the item to remove</param>
    /// <returns>true if removed, false otherwise</returns>
    public bool RemovePermissionObject(IPermissionObject item)
    {
      return _objects.Remove(item);
    }

    /// <summary>
    /// gets all permission objects that exist on this permission
    /// </summary>
    /// <returns></returns>
    public List<IPermissionObject> GetPermissionObjects()
    {
      return _objects;
    }

    /// <summary>
    /// checks if there is permission for this item
    /// </summary>
    /// <param name="item">the item to check permission for</param>
    /// <returns>true if permission exists, false otherwise</returns>
    public bool HasPermissionOn(IPermissionObject item)
    {
      foreach (IPermissionObject obj in _objects)
      {
        if (obj.IsSameAs(item))
        {
          return true;
        }
      }
      return false;
    }
  }
}
