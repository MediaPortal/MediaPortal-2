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

using MediaPortal.Common.MediaManagement.MLQueries;
using MediaPortal.Common.UserProfileDataManagement;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MediaPortal.UiComponents.Media.MediaLists
{
  public abstract class BaseContinueWatchMediaListProvider : BaseMediaListProvider
  {
    protected override async Task<MediaItemQuery> CreateQueryAsync()
    {
      Guid? userProfile = CurrentUserProfile?.ProfileId;
      IFilter filter = userProfile.HasValue ? await AppendUserFilterAsync(BooleanCombinationFilter.CombineFilters(BooleanOperator.And,
            new NotFilter(new EmptyUserDataFilter(userProfile.Value, UserDataKeysKnown.KEY_PLAY_DATE)),
            new RelationalUserDataFilter(userProfile.Value, UserDataKeysKnown.KEY_PLAY_PERCENTAGE, RelationalOperator.LT, UserDataKeysKnown.GetSortablePlayPercentageString(100)),
            new RelationalUserDataFilter(userProfile.Value, UserDataKeysKnown.KEY_PLAY_PERCENTAGE, RelationalOperator.GT, UserDataKeysKnown.GetSortablePlayPercentageString(0))),
            _necessaryMias) : null;

      IFilter navigationFilter = GetNavigationFilter(_navigationInitializerType);
      if (navigationFilter != null)
        filter = BooleanCombinationFilter.CombineFilters(BooleanOperator.And, filter, navigationFilter);

      return new MediaItemQuery(_necessaryMias, _optionalMias, filter)
      {
        SortInformation = new List<ISortInformation> { new DataSortInformation(UserDataKeysKnown.KEY_PLAY_DATE, SortDirection.Descending) }
      };
    }

    protected override bool ShouldUpdate(UpdateReason updateReason)
    {
      return updateReason.HasFlag(UpdateReason.PlaybackComplete) || base.ShouldUpdate(updateReason);
    }
  }

  public abstract class BaseContinueWatchRelationshipMediaListProvider : BaseContinueWatchMediaListProvider
  {
    protected Guid _role;
    protected Guid _linkedRole;
    protected IEnumerable<Guid> _necessaryLinkedMias;

    protected override async Task<MediaItemQuery> CreateQueryAsync()
    {
      Guid? userProfile = CurrentUserProfile?.ProfileId;
      return new MediaItemQuery(_necessaryMias, _optionalMias, null)
      {
        Filter = userProfile.HasValue ? new FilteredRelationshipFilter(_role, _linkedRole, await AppendUserFilterAsync(
          BooleanCombinationFilter.CombineFilters(BooleanOperator.And,
          new NotFilter(new EmptyUserDataFilter(userProfile.Value, UserDataKeysKnown.KEY_PLAY_DATE)),
          new RelationalUserDataFilter(userProfile.Value, UserDataKeysKnown.KEY_PLAY_PERCENTAGE, RelationalOperator.LT, UserDataKeysKnown.GetSortablePlayPercentageString(100)),
          new RelationalUserDataFilter(userProfile.Value, UserDataKeysKnown.KEY_PLAY_PERCENTAGE, RelationalOperator.GT, UserDataKeysKnown.GetSortablePlayPercentageString(0))),
          _necessaryLinkedMias)) : null,
        SubqueryFilter = GetNavigationFilter(_navigationInitializerType),
        SortInformation = new List<ISortInformation> { new DataSortInformation(UserDataKeysKnown.KEY_PLAY_DATE, SortDirection.Descending) }
      };
    }
  }
}
