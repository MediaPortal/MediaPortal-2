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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MediaPortal.Common;
using MediaPortal.Common.Exceptions;
using MediaPortal.Common.MediaManagement.MLQueries;
using MediaPortal.Common.SystemCommunication;
using MediaPortal.Common.UserManagement;
using MediaPortal.UI.ServerCommunication;
using MediaPortal.UiComponents.Media.General;
using MediaPortal.UI.Services.UserManagement;
using MediaPortal.Common.UserProfileDataManagement;
using MediaPortal.UiComponents.Media.Helpers;

namespace MediaPortal.UiComponents.Media.FilterCriteria
{
  public class FilterByPlayCountCriterion : MLFilterCriterion
  {
    #region Base overrides

    public override async Task<ICollection<FilterValue>> GetAvailableValuesAsync(IEnumerable<Guid> necessaryMIATypeIds, IFilter selectAttributeFilter, IFilter filter)
    {
      IContentDirectory cd = ServiceRegistration.Get<IServerConnectionManager>().ContentDirectory;
      if (cd == null)
        throw new NotConnectedException("The MediaLibrary is not connected");

      IUserManagement userProfileDataManagement = ServiceRegistration.Get<IUserManagement>();
      if (!userProfileDataManagement.IsValidUser)
        return new List<FilterValue>();

      IFilter unwatchedFilter = new RelationalUserDataFilter(userProfileDataManagement.CurrentUser.ProfileId, UserDataKeysKnown.KEY_PLAY_PERCENTAGE, RelationalOperator.EQ, UserDataKeysKnown.GetSortablePlayPercentageString(0), true);
      IFilter watchedFilter = new RelationalUserDataFilter(userProfileDataManagement.CurrentUser.ProfileId, UserDataKeysKnown.KEY_PLAY_PERCENTAGE, RelationalOperator.GT, UserDataKeysKnown.GetSortablePlayPercentageString(0));
      bool showVirtual = VirtualMediaHelper.ShowVirtualMedia(necessaryMIATypeIds);
      var taskUnwatched = cd.CountMediaItemsAsync(necessaryMIATypeIds, BooleanCombinationFilter.CombineFilters(BooleanOperator.And, filter, unwatchedFilter), true, showVirtual);
      var taskWatched = cd.CountMediaItemsAsync(necessaryMIATypeIds, BooleanCombinationFilter.CombineFilters(BooleanOperator.And, filter, watchedFilter), true, showVirtual);
      var counts = await Task.WhenAll(taskUnwatched, taskWatched);

      return new List<FilterValue>(new FilterValue[]
        {
            new FilterValue(Consts.RES_VALUE_UNWATCHED, unwatchedFilter, null, counts[0], this),
            new FilterValue(Consts.RES_VALUE_WATCHED, watchedFilter, null, counts[1], this),
        }.Where(fv => !fv.NumItems.HasValue || fv.NumItems.Value > 0));
    }

    public override Task<ICollection<FilterValue>> GroupValuesAsync(ICollection<Guid> necessaryMIATypeIds, IFilter selectAttributeFilter, IFilter filter)
    {
      return Task.FromResult((ICollection<FilterValue>)null);
    }

    #endregion
  }
}
