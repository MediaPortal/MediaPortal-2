#region Copyright (C) 2007-2018 Team MediaPortal

/*
    Copyright (C) 2007-2018 Team MediaPortal
    http://www.team-mediaportal.com

    This file is part of MediaPortal 2

    MediaPortal 2 is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    MediaPortal 2 is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with MediaPortal 2. If not, see <http://www.gnu.org/licenses/>.
*/

#endregion

using System.Collections.Generic;
using MediaPortal.Common.UserProfileDataManagement;

namespace MediaPortal.Common.UserManagement
{
  /// <summary>
  /// <see cref="IUserManagement"/> provides properties and methods for managing user profiles. It is able to provide the <see cref="CurrentUser"/> to be used in all
  /// parts that require user specific logic. Before accessing this property, the caller should check the <see cref="IsValidUser"/> property.
  /// <para>This service depends on server side management, accessed via UPnP infrastructure. In detached mode the <see cref="CurrentUser"/> will not be available.</para>
  /// </summary>
  public interface IUserManagement
  {
    /// <summary>
    /// Indicates if the <see cref="CurrentUser"/> is valid.
    /// </summary>
    bool IsValidUser { get; }

    /// <summary>
    /// Gets or sets the current <see cref="UserProfile"/>. 
    /// </summary>
    UserProfile CurrentUser { get; set; }

    /// <summary>
    /// Indicates if any restrictions for the current user should be applied to Media Library searches.
    /// </summary>
    bool ApplyUserRestriction { get; set; }

    /// <summary>
    /// Exposes access to the <see cref="IUserProfileDataManagement"/> service. This property will be <c>null</c>, if no server connection is available.
    /// </summary>
    IUserProfileDataManagement UserProfileDataManagement { get; }

    /// <summary>
    /// Allows components and plugins to register known restriction groups, which can be used inside user management to restrict user profiles.
    /// This method can be called multiple times with same values, only distinct case-insensitive values are stored.
    /// </summary>
    /// <param name="restrictionGroup"></param>
    void RegisterRestrictionGroup(string restrictionGroup);

    /// <summary>
    /// Gets a unique collection of known restriction group names.
    /// </summary>
    ICollection<string> RestrictionGroups { get; }

    /// <summary>
    /// Checks if the <see cref="CurrentUser"/> is allowed to access the given <see cref="IUserRestriction"/>.
    /// </summary>
    /// <param name="restrictedElement">Element to check</param>
    /// <returns><c>true</c> if user has access or the element does not require a specific access level.</returns>
    bool CheckUserAccess(IUserRestriction restrictedElement);
  }
}
