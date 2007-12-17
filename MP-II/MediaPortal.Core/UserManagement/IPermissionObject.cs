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

namespace MediaPortal.Services.UserManagement
{
  /// <summary>
  /// interface for the Permisson object
  /// </summary>
  public interface IPermissionObject
  {
    /// <summary>
    /// checks if the obj given is equal to this object
    /// after various critera (f.e. a name of a share)
    /// </summary>
    /// <param name="obj">obj to compare with</param>
    /// <returns>true if object is the same, false otherwise</returns>
    bool IsSameAs(IPermissionObject obj);
  }
}