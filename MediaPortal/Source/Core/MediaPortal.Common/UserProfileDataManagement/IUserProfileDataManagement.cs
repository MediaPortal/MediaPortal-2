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
using System.Collections.Generic;

namespace MediaPortal.Common.UserProfileDataManagement
{
  /// <summary>
  /// Service for managing user profiles and personalized user data.
  /// </summary>
  public interface IUserProfileDataManagement
  {
    #region User profiles management

    ICollection<UserProfile> GetProfiles();
    bool GetProfile(Guid profileId, out UserProfile userProfile);
    bool GetProfileByName(string profileName, out UserProfile userProfile);
    Guid CreateProfile(string profileName);
    bool RenameProfile(Guid profileId, string newName);
    bool DeleteProfile(Guid profileId);

    #endregion

    #region User playlist data

    // For example: Last played item position, playlist configuration etc. per user
    bool GetUserPlaylistData(Guid profileId, Guid playlistId, string key, out string data);
    bool SetUserPlaylistData(Guid profileId, Guid playlistId, string key, string data);

    #endregion

    #region User media item data

    // For example: Last video play position
    bool GetUserMediaItemData(Guid profileId, Guid mediaItemId, string key, out string data);
    bool SetUserMediaItemData(Guid profileId, Guid mediaItemId, string key, string data);

    #endregion

    #region User additional data

    // Other global user data 
    bool GetUserAdditionalData(Guid profileId, string key, out string data);
    bool SetUserAdditionalData(Guid profileId, string key, string data);

    #endregion

    #region Cleanup user data

    bool ClearAllUserData(Guid profileId);

    #endregion
  }
}