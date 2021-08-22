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

using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Common.MediaManagement.MLQueries;
using MediaPortal.Common.UserProfileDataManagement;
using MediaPortal.UI.ContentLists;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MediaPortal.Common.MediaManagement;

namespace MediaPortal.UiComponents.Media.MediaLists
{
  public abstract class BaseContinueWatchMediaListProvider : BaseMediaListProvider
  {
    protected Guid? _changeAspectId;

    protected override async Task<MediaItemQuery> CreateQueryAsync()
    {
      Guid? userProfile = CurrentUserProfile?.ProfileId;
      IFilter filter = userProfile.HasValue ? await AppendUserFilterAsync(BooleanCombinationFilter.CombineFilters(BooleanOperator.And,
            new NotFilter(new EmptyUserDataFilter(userProfile.Value, UserDataKeysKnown.KEY_PLAY_DATE)),
            new RelationalUserDataFilter(userProfile.Value, UserDataKeysKnown.KEY_PLAY_PERCENTAGE, RelationalOperator.LT, UserDataKeysKnown.GetSortablePlayPercentageString(100)),
            new RelationalUserDataFilter(userProfile.Value, UserDataKeysKnown.KEY_PLAY_PERCENTAGE, RelationalOperator.GT, UserDataKeysKnown.GetSortablePlayPercentageString(0))),
            _necessaryMias) : new RelationalFilter(MediaAspect.ATTR_PLAYCOUNT, RelationalOperator.EQ, 0);

      IFilter navigationFilter = GetNavigationFilter(_navigationInitializerType);
      if (navigationFilter != null)
        filter = BooleanCombinationFilter.CombineFilters(BooleanOperator.And, filter, navigationFilter);

      ISortInformation sort = userProfile.HasValue ? (ISortInformation)new DataSortInformation(UserDataKeysKnown.KEY_PLAY_DATE, SortDirection.Descending) :
        (ISortInformation)new AttributeSortInformation(MediaAspect.ATTR_LASTPLAYED, SortDirection.Descending);

      return new MediaItemQuery(_necessaryMias, _optionalMias, filter)
      {
        SortInformation = new List<ISortInformation> { sort }
      };
    }

    protected override bool ShouldUpdate(UpdateReason updateReason, ICollection<object> updatedObjects)
    {
      bool update = updateReason.HasFlag(UpdateReason.UserChanged) || base.ShouldUpdate(updateReason, updatedObjects);
      if (updateReason.HasFlag(UpdateReason.MediaItemChanged))
      {
        if (updatedObjects?.Count > 0 && _changeAspectId.HasValue)
        {
          foreach (MediaItem item in updatedObjects)
          {
            if (item.Aspects.ContainsKey(_changeAspectId.Value))
            {
              update = true;
              break;
            }
          }
        }
        else
        {
          update = true;
        }
      }

      return update;
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
      IFilter linkedFilter = userProfile.HasValue ? BooleanCombinationFilter.CombineFilters(BooleanOperator.Or,
        new EmptyUserDataFilter(userProfile.Value, UserDataKeysKnown.KEY_PLAY_DATE),
        new RelationalUserDataFilter(userProfile.Value, UserDataKeysKnown.KEY_PLAY_PERCENTAGE, RelationalOperator.LT, UserDataKeysKnown.GetSortablePlayPercentageString(100))) :
        null;
      IFilter filter = userProfile.HasValue ? BooleanCombinationFilter.CombineFilters(BooleanOperator.And,
        new FilteredRelationshipFilter(_role, _linkedRole, await AppendUserFilterAsync(linkedFilter, _necessaryLinkedMias)),
        new NotFilter(new EmptyUserDataFilter(userProfile.Value, UserDataKeysKnown.KEY_PLAY_DATE)),
        new RelationalUserDataFilter(userProfile.Value, UserDataKeysKnown.KEY_PLAY_PERCENTAGE, RelationalOperator.LT, UserDataKeysKnown.GetSortablePlayPercentageString(100)),
        new RelationalUserDataFilter(userProfile.Value, UserDataKeysKnown.KEY_PLAY_PERCENTAGE, RelationalOperator.GT, UserDataKeysKnown.GetSortablePlayPercentageString(0))) :
        new RelationalFilter(MediaAspect.ATTR_PLAYCOUNT, RelationalOperator.EQ, 0);

      ISortInformation sort = userProfile.HasValue ? (ISortInformation)new DataSortInformation(UserDataKeysKnown.KEY_PLAY_DATE, SortDirection.Descending) :
        (ISortInformation)new AttributeSortInformation(MediaAspect.ATTR_LASTPLAYED, SortDirection.Descending);

      return new MediaItemQuery(_necessaryMias, _optionalMias, filter)
      {
        SubqueryFilter = GetNavigationFilter(_navigationInitializerType),
        SortInformation = new List<ISortInformation> { sort }
      };
    }
  }
}
