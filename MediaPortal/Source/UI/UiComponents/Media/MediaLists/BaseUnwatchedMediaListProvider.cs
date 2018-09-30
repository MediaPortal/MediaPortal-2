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

using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Common.MediaManagement.MLQueries;
using MediaPortal.Common.UserProfileDataManagement;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MediaPortal.UiComponents.Media.MediaLists
{
  public abstract class BaseUnwatchedMediaListProvider : BaseMediaListProvider
  {
    protected override async Task<MediaItemQuery> CreateQueryAsync()
    {
      Guid? userProfile = CurrentUserProfile?.ProfileId;
      IFilter filter;
      if (userProfile.HasValue)
        filter = await AppendUserFilterAsync(
          new RelationalUserDataFilter(userProfile.Value, UserDataKeysKnown.KEY_PLAY_PERCENTAGE, RelationalOperator.EQ, UserDataKeysKnown.GetSortablePlayPercentageString(0), true), _necessaryMias);
      else
        filter = new RelationalFilter(MediaAspect.ATTR_PLAYCOUNT, RelationalOperator.EQ, 0);

      IFilter navigationFilter = GetNavigationFilter(_navigationInitializerType);
      if (navigationFilter != null)
        filter = BooleanCombinationFilter.CombineFilters(BooleanOperator.And, filter, navigationFilter);

      return new MediaItemQuery(_necessaryMias, _optionalMias, filter)
      {
        SortInformation = new List<ISortInformation> { new AttributeSortInformation(ImporterAspect.ATTR_DATEADDED, SortDirection.Ascending) }
      };
    }

    protected override bool ShouldUpdate(UpdateReason updateReason)
    {
      return updateReason.HasFlag(UpdateReason.PlaybackComplete) || updateReason.HasFlag(UpdateReason.ImportComplete) || base.ShouldUpdate(updateReason);
    }
  }
}
