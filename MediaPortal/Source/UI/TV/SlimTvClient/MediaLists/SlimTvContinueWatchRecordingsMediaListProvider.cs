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

using MediaPortal.Common;
using MediaPortal.Common.Commands;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.MLQueries;
using MediaPortal.Common.UserProfileDataManagement;
using MediaPortal.Plugins.SlimTv.Client.Models.Navigation;
using MediaPortal.Plugins.SlimTv.Client.TvHandler;
using MediaPortal.UI.Presentation.DataObjects;
using MediaPortal.UI.ServerCommunication;
using MediaPortal.UI.Services.UserManagement;
using MediaPortal.UiComponents.Media.MediaLists;
using MediaPortal.UiComponents.Media.Models;
using MediaPortal.UiComponents.Media.Models.Navigation;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MediaPortal.Plugins.SlimTv.Client.MediaLists
{
  public class SlimTvContinueWatchRecordingsMediaListProvider : IMediaListProvider
  {
    public SlimTvContinueWatchRecordingsMediaListProvider()
    {
      AllItems = new ItemsList();
    }

    public ItemsList AllItems { get; private set; }

    public bool UpdateItems(int maxItems, UpdateReason updateReason)
    {
      var contentDirectory = ServiceRegistration.Get<IServerConnectionManager>().ContentDirectory;
      if (contentDirectory == null)
        return false;

      if ((updateReason & UpdateReason.Forced) == UpdateReason.Forced ||
          (updateReason & UpdateReason.PlaybackComplete) == UpdateReason.PlaybackComplete)
      {
        Guid? userProfile = null;
        Guid[] mias = SlimTvConsts.NECESSARY_RECORDING_MIAS;
        IUserManagement userProfileDataManagement = ServiceRegistration.Get<IUserManagement>();
        if (userProfileDataManagement != null && userProfileDataManagement.IsValidUser)
        {
          userProfile = userProfileDataManagement.CurrentUser.ProfileId;
        }

        MediaItemQuery query = new MediaItemQuery(SlimTvConsts.NECESSARY_RECORDING_MIAS, null)
        {
          Filter = userProfile.HasValue ? BooleanCombinationFilter.CombineFilters(BooleanOperator.And, 
            new NotFilter(new EmptyUserDataFilter(userProfile.Value, UserDataKeysKnown.KEY_PLAY_DATE)),
            new NotFilter(new RelationalUserDataFilter(userProfile.Value, UserDataKeysKnown.KEY_PLAY_PERCENTAGE, RelationalOperator.NEQ, "100"))) : null,
          Limit = (uint)maxItems, // Last 5 imported items
          SortInformation = new List<ISortInformation> { new DataSortInformation(UserDataKeysKnown.KEY_PLAY_DATE, SortDirection.Descending) }
        };

        var items = contentDirectory.Search(query, false, userProfile, false);
        if (!AllItems.Select(pmi => ((PlayableMediaItem)pmi).MediaItem.MediaItemId).SequenceEqual(items.Select(mi => mi.MediaItemId)))
        {
          AllItems.Clear();
          foreach (MediaItem mediaItem in items)
          {
            PlayableMediaItem listItem = new RecordingItem(mediaItem);
            listItem.Command = new MethodDelegateCommand(() => PlayItemsModel.CheckQueryPlayAction(listItem.MediaItem));
            AllItems.Add(listItem);
          }
          AllItems.FireChange();
        }
      }
      return true;
    }
  }
}
