#region Copyright (C) 2007-2020 Team MediaPortal

/*
    Copyright (C) 2007-2020 Team MediaPortal
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

using MediaPortal.Common;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.MLQueries;
using MediaPortal.Common.SystemCommunication;
using MediaPortal.Common.UserProfileDataManagement;
using MediaPortal.UI.ServerCommunication;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MediaPortal.Common.UserManagement;

namespace MediaPortal.UiComponents.Media.Helpers
{
  public static class UserHelper
  {
    private static (UserProfile UserProfile, bool ApplyUserRestrictions) CheckUserRestrictions()
    {
      UserProfile userProfile = null;
      bool applyUserRestrictions = false;
      IUserManagement userProfileDataManagement = ServiceRegistration.Get<IUserManagement>();
      if (userProfileDataManagement != null && userProfileDataManagement.IsValidUser)
      {
        userProfile = userProfileDataManagement.CurrentUser;
        applyUserRestrictions = userProfileDataManagement.ApplyUserRestriction;
      }
      if (userProfile == null || !applyUserRestrictions)
        return (null, false);

      return (userProfile, applyUserRestrictions);
    }

    public static IFilter GetUserRestrictionFilter(ICollection<Guid> necessaryMias, IFilter filter = null)
    {
      var check = CheckUserRestrictions();
      if (!check.ApplyUserRestrictions)
        return null;

      return GetUserRestrictionFilter(necessaryMias, check.UserProfile, filter);
    }

    public static IFilter GetUserRestrictionFilter(ICollection<Guid> necessaryMias, UserProfile userProfile, IFilter filter = null)
    {
      var check = CheckUserRestrictions();
      if (!check.ApplyUserRestrictions)
        return null;

      var userFilter = userProfile.GetUserFilter(necessaryMias);
      if (userFilter != null && filter != null)
        return BooleanCombinationFilter.CombineFilters(BooleanOperator.And, filter, userFilter);
      else if (userFilter != null)
        return userFilter;

      return filter;
    }
  }
}
