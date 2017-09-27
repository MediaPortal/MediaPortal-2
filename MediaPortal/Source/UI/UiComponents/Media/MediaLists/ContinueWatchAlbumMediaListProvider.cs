#region Copyright (C) 2007-2017 Team MediaPortal

/*
    Copyright (C) 2007-2017 Team MediaPortal
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
using MediaPortal.UiComponents.Media.General;
using MediaPortal.UiComponents.Media.Models.Navigation;
using System;
using System.Collections.Generic;

namespace MediaPortal.UiComponents.Media.MediaLists
{
  public class ContinueWatchAlbumMediaListProvider : BaseMediaListProvider
  {
    public ContinueWatchAlbumMediaListProvider()
    {
      _necessaryMias = Consts.NECESSARY_ALBUM_MIAS;
      _playableContainerConverterAction = item => new AlbumFilterItem(item);
    }

    public override bool UpdateItems(int maxItems, UpdateReason updateReason)
    {
      if ((updateReason & UpdateReason.Forced) == UpdateReason.Forced ||
        (updateReason & UpdateReason.PlaybackComplete) == UpdateReason.PlaybackComplete)
      {
        Guid? userProfile = CurrentUserProfile?.ProfileId;
        _mediaQuery = new MediaItemQuery(_necessaryMias, null)
        {
          Filter = userProfile.HasValue ? new FilteredRelationshipFilter(AudioAspect.ROLE_TRACK, AudioAlbumAspect.ROLE_ALBUM, BooleanCombinationFilter.CombineFilters(BooleanOperator.And,
            new NotFilter(new EmptyUserDataFilter(userProfile.Value, UserDataKeysKnown.KEY_PLAY_DATE)),
            new NotFilter(new RelationalUserDataFilter(userProfile.Value, UserDataKeysKnown.KEY_PLAY_PERCENTAGE, RelationalOperator.NEQ, "100")))) : null,
          Limit = (uint)maxItems, // Last 5 imported items
          SortInformation = new List<ISortInformation> { new DataSortInformation(UserDataKeysKnown.KEY_PLAY_DATE, SortDirection.Descending) }
        };
        base.UpdateItems(maxItems, UpdateReason.Forced);
      }
      return true;
    }
  }
}
