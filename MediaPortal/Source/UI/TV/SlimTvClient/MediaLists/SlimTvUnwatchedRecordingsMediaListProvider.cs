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

using MediaPortal.Common.MediaManagement.MLQueries;
using MediaPortal.Common.UserProfileDataManagement;
using MediaPortal.Plugins.SlimTv.Client.TvHandler;
using MediaPortal.UiComponents.Media.MediaLists;
using System;
using System.Collections.Generic;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;

namespace MediaPortal.Plugins.SlimTv.Client.MediaLists
{
  public class SlimTvUnwatchedRecordingsMediaListProvider : BaseRecordingMediaListProvider
  {
    public override bool UpdateItems(int maxItems, UpdateReason updateReason)
    {
      if ((updateReason & UpdateReason.Forced) == UpdateReason.Forced ||
          (updateReason & UpdateReason.PlaybackComplete) == UpdateReason.PlaybackComplete ||
          (updateReason & UpdateReason.ImportComplete) == UpdateReason.ImportComplete)
      {
        Guid? userProfile = CurrentUserProfile?.ProfileId;
        _query = new MediaItemQuery(SlimTvConsts.NECESSARY_RECORDING_MIAS, null)
        {
          Filter = userProfile.HasValue ? AppendUserFilter(BooleanCombinationFilter.CombineFilters(BooleanOperator.Or,
            new EmptyUserDataFilter(userProfile.Value, UserDataKeysKnown.KEY_PLAY_COUNT),
            new RelationalUserDataFilter(userProfile.Value, UserDataKeysKnown.KEY_PLAY_COUNT, RelationalOperator.EQ, UserDataKeysKnown.GetSortablePlayCountString(0)))) : null,
          Limit = (uint)maxItems, // Last 5 imported items
          SortInformation = new List<ISortInformation> { new AttributeSortInformation(ImporterAspect.ATTR_DATEADDED, SortDirection.Descending) }
        };
        return base.UpdateItems(maxItems, UpdateReason.Forced);
      }
      return true;
    }
  }
}
