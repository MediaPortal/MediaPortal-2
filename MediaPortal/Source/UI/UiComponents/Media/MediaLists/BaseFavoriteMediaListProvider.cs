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
using System;
using System.Collections.Generic;

namespace MediaPortal.UiComponents.Media.MediaLists
{
  public abstract class BaseFavoriteMediaListProvider : BaseMediaListProvider
  {
    public override bool UpdateItems(int maxItems, UpdateReason updateReason)
    {
      if ((updateReason & UpdateReason.Forced) == UpdateReason.Forced ||
          (updateReason & UpdateReason.PlaybackComplete) == UpdateReason.PlaybackComplete ||
          (updateReason & UpdateReason.ImportComplete) == UpdateReason.ImportComplete)
      {
        Guid? userProfile = CurrentUserProfile?.ProfileId;
        _mediaQuery = new MediaItemQuery(_necessaryMias, null)
        {
          Filter = userProfile.HasValue ? new NotFilter(new EmptyUserDataFilter(userProfile.Value, UserDataKeysKnown.KEY_PLAY_COUNT)) : null,
          Limit = (uint)maxItems, // Last 5 imported items
          SortInformation = new List<ISortInformation> { new DataSortInformation(UserDataKeysKnown.KEY_PLAY_COUNT, SortDirection.Descending) }
        };
        base.UpdateItems(maxItems, UpdateReason.Forced);
      }
      return true;
    }
  }
}
