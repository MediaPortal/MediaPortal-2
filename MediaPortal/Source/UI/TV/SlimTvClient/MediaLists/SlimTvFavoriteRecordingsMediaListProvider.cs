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

namespace MediaPortal.Plugins.SlimTv.Client.MediaLists
{
  public class SlimTvFavoriteRecordingsMediaListProvider : BaseRecordingMediaListProvider
  {
    public override bool UpdateItems(int maxItems, UpdateReason updateReason)
    {
      if ((updateReason & UpdateReason.Forced) == UpdateReason.Forced ||
          (updateReason & UpdateReason.PlaybackComplete) == UpdateReason.PlaybackComplete)
      {
        Guid? userProfile = CurrentUserProfile?.ProfileId;
        _query = new MediaItemQuery(SlimTvConsts.NECESSARY_RECORDING_MIAS, null)
        {
          Filter = userProfile.HasValue ? AppendUserFilter(new NotFilter(new EmptyUserDataFilter(userProfile.Value, UserDataKeysKnown.KEY_PLAY_COUNT))) : null,
          Limit = (uint)maxItems, // Last 5 imported items
          SortInformation = new List<ISortInformation> { new DataSortInformation(UserDataKeysKnown.KEY_PLAY_COUNT, SortDirection.Descending) }
        };
        return base.UpdateItems(maxItems, UpdateReason.Forced);
      }
      return true;
    }
  }
}
