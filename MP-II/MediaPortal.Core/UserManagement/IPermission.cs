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
using MediaPortal.Services.UserManagement;

namespace MediaPortal.Core.UserManagement
{
  /// <summary>
  /// Interface for a permission
  /// </summary>
  public interface IPermission
  {
    /// <summary>
    /// adds a permisson object to this permission (f.e. a MediaItem)
    /// </summary>
    /// <param name="item">the item to add</param>
    /// <returns>true if added, false otherwise</returns>
    bool AddPermissionObject(IPermissionObject item);

    /// <summary>
    /// removes a permisson object from this permission
    /// </summary>
    /// <param name="item">the item to remove</param>
    /// <returns>true if removed, false otherwise</returns>
    bool RemovePermissionObject(IPermissionObject item);

    /// <summary>
    /// gets all permission objects that exist on this permission
    /// </summary>
    /// <returns></returns>
    List<IPermissionObject> GetPermissionObjects();

    /// <summary>
    /// checks if there is permission for this item
    /// </summary>
    /// <param name="item">the item to check permission for</param>
    /// <returns>true if permission exists, false otherwise</returns>
    bool HasPermissionOn(IPermissionObject item);
  }
}