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
  /// Interface for the User management service.
  /// It adds functionality to add / remove users and to get a list of available users.
  /// </summary>
  /// <remarks>
  /// If the user management service is present in the application, the modules doing the
  /// work for the GUI are responsible for checking permissions for their jobs for the
  /// current user. If the user management service is not present in the application,
  /// code in those modules should always continue as if the permission checks would have succeeded.
  /// </remarks>
  public interface IUserService
  {
    /// <summary>
    /// Gets or sets the current user.
    /// </summary>
    IUser CurrentUser { get; set; }

    /// <summary>
    /// Adds a user to the pool of available users.
    /// </summary>
    /// <remarks>
    /// Additional properties of the new user won't be initialized by this method, they have to
    /// be initialized on the returned user instance.
    /// </remarks>
    /// <param name="name">The name of the new user.</param>
    /// <returns>New user instance.</returns>
    IUser AddUser(string name);

    /// <summary>
    /// Removes a user from the pool of available users.
    /// </summary>
    /// <param name="user">The user to remove.</param>
    /// <returns><c>true</c>, if the user could successfully be removed, else <c>false</c>.</returns>
    bool RemoveUser(IUser user);

    /// <summary>
    /// Gets a list of available users.
    /// </summary>
    /// <returns>List of available users.</returns>
    IList<IUser> GetUsers();
  }
}
