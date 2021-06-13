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
using System.Linq;
using System.Threading.Tasks;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;

namespace MediaPortal.UiComponents.Media.MediaLists
{
  public abstract class BaseLastWatchedMediaListProvider : BaseMediaListProvider
  {
    protected Guid[] _changeAspectIds;

    protected override async Task<MediaItemQuery> CreateQueryAsync()
    {
      Guid? userProfile = CurrentUserProfile?.ProfileId;
      IFilter filter = userProfile.HasValue ? await AppendUserFilterAsync(new NotFilter(new EmptyUserDataFilter(userProfile.Value, UserDataKeysKnown.KEY_PLAY_DATE)),
          _necessaryMias) : new RelationalFilter(MediaAspect.ATTR_PLAYCOUNT, RelationalOperator.GT, 0);

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
        if (updatedObjects?.Count > 0 && _changeAspectIds != null)
        {
          foreach (MediaItem item in updatedObjects)
          {
            if (_changeAspectIds.All(i => item.Aspects.ContainsKey(i)))
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

  public abstract class BaseLastWatchedRelationshipMediaListProvider : BaseLastWatchedMediaListProvider
  {
    protected Guid _role;
    protected Guid _linkedRole;
    protected IEnumerable<Guid> _necessaryLinkedMias;

    protected override async Task<MediaItemQuery> CreateQueryAsync()
    {
      Guid? userProfile = CurrentUserProfile?.ProfileId;
      IFilter linkedFilter = userProfile.HasValue ? new NotFilter(new EmptyUserDataFilter(userProfile.Value, UserDataKeysKnown.KEY_PLAY_DATE)) :
        null;
      IFilter filter = userProfile.HasValue ? BooleanCombinationFilter.CombineFilters(BooleanOperator.And,
        new FilteredRelationshipFilter(_role, _linkedRole, await AppendUserFilterAsync(linkedFilter, _necessaryLinkedMias)),
        new NotFilter(new EmptyUserDataFilter(userProfile.Value, UserDataKeysKnown.KEY_PLAY_DATE))) :
        new RelationalFilter(MediaAspect.ATTR_PLAYCOUNT, RelationalOperator.GT, 0);

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
