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

using System;
using MediaPortal.Common;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Common.SystemCommunication;
using MediaPortal.UI.ServerCommunication;
using System.Collections.Generic;
using System.Threading.Tasks;
using MediaPortal.Common.Async;
using MediaPortal.Common.Services.ServerCommunication;
using MediaPortal.Common.UserManagement;
using MediaPortal.UI.Services.UserManagement;
using MediaPortal.Common.UserProfileDataManagement;

namespace MediaPortal.UiComponents.Media.MediaItemActions
{
  public abstract class AbstractWatchedAction : AbstractMediaItemAction
  {
    protected abstract bool AppliesForWatchedState(bool watched);
    protected abstract bool GetNewWatchedState();

    public override Task<bool> IsAvailableAsync(MediaItem mediaItem)
    {
      bool watched = false;
      if (mediaItem.UserData.ContainsKey(UserDataKeysKnown.KEY_PLAY_PERCENTAGE))
        watched = Convert.ToInt32(mediaItem.UserData[UserDataKeysKnown.KEY_PLAY_PERCENTAGE]) >= 100;
      if (!IsManagedByMediaLibrary(mediaItem) || !AppliesForWatchedState(watched))
        return Task.FromResult(false);

      IContentDirectory cd = ServiceRegistration.Get<IServerConnectionManager>().ContentDirectory;
      var result = cd != null;
      return Task.FromResult(result);
    }

    public override async Task<AsyncResult<ContentDirectoryMessaging.MediaItemChangeType>> ProcessAsync(MediaItem mediaItem)
    {
      var falseResult = new AsyncResult<ContentDirectoryMessaging.MediaItemChangeType>(false, ContentDirectoryMessaging.MediaItemChangeType.None);
      IContentDirectory cd = ServiceRegistration.Get<IServerConnectionManager>().ContentDirectory;
      if (cd == null)
        return falseResult;

      IList<MultipleMediaItemAspect> pras;
      if (!MediaItemAspect.TryGetAspects(mediaItem.Aspects, ProviderResourceAspect.Metadata, out pras))
        return falseResult;

      Guid? userProfile = null;
      IUserManagement userProfileDataManagement = ServiceRegistration.Get<IUserManagement>();
      if (userProfileDataManagement != null && userProfileDataManagement.IsValidUser)
        userProfile = userProfileDataManagement.CurrentUser.ProfileId;

      bool watched = GetNewWatchedState();
      int playPercentage = watched ? 100 : 0;

      if (userProfile.HasValue)
      {
        await cd.NotifyUserPlaybackAsync(userProfile.Value, mediaItem.MediaItemId, playPercentage, watched);
      }
      else
      {
        await cd.NotifyPlaybackAsync(mediaItem.MediaItemId, watched);
      }

      //Also update media item locally so changes are reflected in GUI without reloading
      mediaItem.UserData[UserDataKeysKnown.KEY_PLAY_PERCENTAGE] = UserDataKeysKnown.GetSortablePlayPercentageString(playPercentage);
      return new AsyncResult<ContentDirectoryMessaging.MediaItemChangeType>(true, ContentDirectoryMessaging.MediaItemChangeType.Updated);
    }
  }
}
