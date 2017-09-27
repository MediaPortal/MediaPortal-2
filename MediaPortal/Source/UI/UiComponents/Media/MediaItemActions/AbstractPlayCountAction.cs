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

using System;
using MediaPortal.Common;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Common.SystemCommunication;
using MediaPortal.UI.ServerCommunication;
using System.Collections.Generic;
using MediaPortal.UI.Services.UserManagement;
using MediaPortal.Common.UserProfileDataManagement;

namespace MediaPortal.UiComponents.Media.MediaItemActions
{
  public abstract class AbstractPlayCountAction : AbstractMediaItemAction
  {
    protected abstract bool AppliesForPlayCount(int playCount);
    protected abstract int GetNewPlayCount();

    public override bool IsAvailable(MediaItem mediaItem)
    {
      int playCount = 0;
      if (mediaItem.UserData.ContainsKey(UserDataKeysKnown.KEY_PLAY_COUNT))
        playCount = Convert.ToInt32(mediaItem.UserData[UserDataKeysKnown.KEY_PLAY_COUNT]);
      if (!IsManagedByMediaLibrary(mediaItem) || !AppliesForPlayCount(playCount))
        return false;
      IContentDirectory cd = ServiceRegistration.Get<IServerConnectionManager>().ContentDirectory;
      return cd != null;
    }

    public override bool Process(MediaItem mediaItem, out ContentDirectoryMessaging.MediaItemChangeType changeType)
    {
      changeType = ContentDirectoryMessaging.MediaItemChangeType.None;
      IContentDirectory cd = ServiceRegistration.Get<IServerConnectionManager>().ContentDirectory;
      if (cd == null)
        return false;
      
      IList<MultipleMediaItemAspect> pras;
      if (!MediaItemAspect.TryGetAspects(mediaItem.Aspects, ProviderResourceAspect.Metadata, out pras))
        return false;

      Guid? userProfile = null;
      IUserManagement userProfileDataManagement = ServiceRegistration.Get<IUserManagement>();
      if (userProfileDataManagement != null && userProfileDataManagement.IsValidUser)
        userProfile = userProfileDataManagement.CurrentUser.ProfileId;

      int playCount = GetNewPlayCount();
      int playPercentage = playCount > 0 ? 100 : 0;

      if (userProfile.HasValue)
      {
        if (cd != null)
          cd.NotifyUserPlayback(userProfile.Value, mediaItem.MediaItemId, playCount > 0 ? playPercentage : -1, playCount > 0);
      }
      else if (cd != null)
      {
        cd.NotifyPlayback(mediaItem.MediaItemId, playCount > 0);
      }

      //Also update media item locally so changes are reflected in GUI without reloading
      MediaItemAspect.SetAttribute(mediaItem.Aspects, MediaAspect.ATTR_PLAYCOUNT, playCount);
      mediaItem.UserData[UserDataKeysKnown.KEY_PLAY_COUNT] = playCount.ToString();
      if(mediaItem.UserData.ContainsKey(UserDataKeysKnown.KEY_PLAY_PERCENTAGE))
        mediaItem.UserData[UserDataKeysKnown.KEY_PLAY_PERCENTAGE] = playPercentage.ToString();
      if (playCount <= 0)
        mediaItem.UserData.Remove(UserDataKeysKnown.KEY_PLAY_DATE);
      changeType = ContentDirectoryMessaging.MediaItemChangeType.Updated;
      return true;
    }
  }
}
