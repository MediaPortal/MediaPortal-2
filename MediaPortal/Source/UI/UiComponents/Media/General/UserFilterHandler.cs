#region Copyright (C) 2007-2015 Team MediaPortal

/*
    Copyright (C) 2007-2015 Team MediaPortal
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
using MediaPortal.Common;
using MediaPortal.UI.Services.UserManagement;
using MediaPortal.UiComponents.Media.FilterCriteria;
using Newtonsoft.Json;

namespace MediaPortal.UiComponents.Media.General
{
  /// <summary>
  /// <see cref="UserFilterHandler"/> manages the loading and saving of user defined MediaLibrary filters.
  /// The filters will be stored per user (<see cref="IUserManagement"/>).
  /// </summary>
  public class UserFilterHandler
  {
    /// <summary>
    /// Tries to load saved filters for current user.
    /// </summary>
    /// <param name="filters">Returns filters.</param>
    /// <returns><c>true</c> if at least one filter could be found.</returns>
    public bool GetSavedUserFilters(out List<FilterValue> filters)
    {
      filters = null;
      IUserManagement userProfileDataManagement = ServiceRegistration.Get<IUserManagement>();
      if (!userProfileDataManagement.IsValidUser)
        return false;

      string serializeObject;
      if (userProfileDataManagement.UserProfileDataManagement.GetUserAdditionalData(userProfileDataManagement.CurrentUser.ProfileId, Consts.KEY_USER_QUERIES, out serializeObject))
      {
        filters = JsonConvert.DeserializeObject<List<FilterValue>>(serializeObject, SerializerConfig.Default);
        return filters != null && filters.Count > 0;
      }
      return false;
    }

    /// <summary>
    /// Saves a list of filters for the current user.
    /// </summary>
    /// <param name="filters">List of filters.</param>
    /// <returns><c>true</c> if successful.</returns>
    public bool SaveUserFilters(List<FilterValue> filters)
    {
      IUserManagement userProfileDataManagement = ServiceRegistration.Get<IUserManagement>();
      if (!userProfileDataManagement.IsValidUser)
        return false;

      var serializeObject = JsonConvert.SerializeObject(filters, SerializerConfig.Default);
      userProfileDataManagement.UserProfileDataManagement.SetUserAdditionalData(userProfileDataManagement.CurrentUser.ProfileId, Consts.KEY_USER_QUERIES, serializeObject);
      return true;
    }
  }
}
