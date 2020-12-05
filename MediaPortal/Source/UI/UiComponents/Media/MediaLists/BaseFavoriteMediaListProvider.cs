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

using MediaPortal.Common.MediaManagement.MLQueries;
using MediaPortal.Common.UserProfileDataManagement;
using MediaPortal.UI.ContentLists;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;

namespace MediaPortal.UiComponents.Media.MediaLists
{
  public abstract class BaseFavoriteMediaListProvider : BaseMediaListProvider
  {
    protected override async Task<MediaItemQuery> CreateQueryAsync()
    {
      Guid? userProfile = CurrentUserProfile?.ProfileId;
      IFilter filter = userProfile.HasValue ? await AppendUserFilterAsync(new NotFilter(new EmptyUserDataFilter(userProfile.Value, UserDataKeysKnown.KEY_PLAY_COUNT)),
            _necessaryMias) : new RelationalFilter(MediaAspect.ATTR_PLAYCOUNT, RelationalOperator.GT, 0);

      IFilter navigationFilter = GetNavigationFilter(_navigationInitializerType);
      if (navigationFilter != null)
        filter = BooleanCombinationFilter.CombineFilters(BooleanOperator.And, filter, navigationFilter);

      List<ISortInformation> sort = userProfile.HasValue ? new List<ISortInformation>
        {
          new DataSortInformation(UserDataKeysKnown.KEY_PLAY_COUNT, SortDirection.Descending),
          new DataSortInformation(UserDataKeysKnown.KEY_PLAY_DATE, SortDirection.Descending)
        } :
        new List<ISortInformation>
        {
          new AttributeSortInformation(MediaAspect.ATTR_PLAYCOUNT, SortDirection.Descending),
          new AttributeSortInformation(MediaAspect.ATTR_LASTPLAYED, SortDirection.Descending)
        };

      return new MediaItemQuery(_necessaryMias, _optionalMias, filter)
      {
        SortInformation = sort
      };
    }

    protected override bool ShouldUpdate(UpdateReason updateReason)
    {
      return updateReason.HasFlag(UpdateReason.MediaItemChanged) || updateReason.HasFlag(UpdateReason.ImportComplete) || updateReason.HasFlag(UpdateReason.UserChanged) || base.ShouldUpdate(updateReason);
    }
  }

  public abstract class BaseFavoriteRelationshipMediaListProvider : BaseFavoriteMediaListProvider
  {
    protected Guid _role;
    protected Guid _linkedRole;
    protected IEnumerable<Guid> _necessaryLinkedMias;

    protected override async Task<MediaItemQuery> CreateQueryAsync()
    {
      Guid? userProfile = CurrentUserProfile?.ProfileId;
      IFilter linkedFilter = userProfile.HasValue ? BooleanCombinationFilter.CombineFilters(BooleanOperator.And, 
        new NotFilter(new EmptyUserDataFilter(userProfile.Value, UserDataKeysKnown.KEY_PLAY_COUNT)), 
        new RelationalUserDataFilter(userProfile.Value, UserDataKeysKnown.KEY_PLAY_COUNT, RelationalOperator.GT, UserDataKeysKnown.GetSortablePlayCountString(0))) :
        null;
      IFilter filter = userProfile.HasValue ? BooleanCombinationFilter.CombineFilters(BooleanOperator.And,
        new FilteredRelationshipFilter(_role, _linkedRole, await AppendUserFilterAsync(linkedFilter, _necessaryLinkedMias)),
        new NotFilter(new EmptyUserDataFilter(userProfile.Value, UserDataKeysKnown.KEY_PLAY_MAX_CHILD_COUNT))) :
        new RelationalFilter(MediaAspect.ATTR_PLAYCOUNT, RelationalOperator.GT, 0);

      List<ISortInformation> sort = userProfile.HasValue ? new List<ISortInformation>
        {
          new DataSortInformation(UserDataKeysKnown.KEY_PLAY_MAX_CHILD_COUNT, SortDirection.Descending),
          new DataSortInformation(UserDataKeysKnown.KEY_PLAY_COUNT, SortDirection.Descending),
          new DataSortInformation(UserDataKeysKnown.KEY_PLAY_DATE, SortDirection.Descending)
        } :
        new List<ISortInformation>
        {
          new AttributeSortInformation(MediaAspect.ATTR_PLAYCOUNT, SortDirection.Descending),
          new AttributeSortInformation(MediaAspect.ATTR_LASTPLAYED, SortDirection.Descending)
        };
      return new MediaItemQuery(_necessaryMias, _optionalMias, filter)
      {
        SubqueryFilter = GetNavigationFilter(_navigationInitializerType),
        SortInformation = sort
      };
    }
  }
}
