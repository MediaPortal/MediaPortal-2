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

using MediaPortal.Common.MediaManagement.MLQueries;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MediaPortal.Common.Async;
using MediaPortal.Common.Services.ServerCommunication;

namespace MediaPortal.Common.UserProfileDataManagement
{
  /// <summary>
  /// Service for managing user profiles and personalized user data.
  /// </summary>
  public interface IUserProfileDataManagement
  {
    #region User profiles management

    Task<ICollection<UserProfile>> GetProfilesAsync();
    Task<AsyncResult<UserProfile>> GetProfileAsync(Guid profileId);
    Task<AsyncResult<UserProfile>> GetProfileByNameAsync(string profileName);
    Task<Guid> CreateProfileAsync(string profileName);
    Task<Guid> CreateClientProfileAsync(Guid profileId, string profileName);
    Task<Guid> CreateProfileAsync(string profileName, UserProfileType profileType, string profilePassword);
    Task<bool> UpdateProfileAsync(Guid profileId, string profileName, UserProfileType profileType, string profilePassword);
    Task<bool> SetProfileImageAsync(Guid profileId, byte[] profileImage);
    Task<bool> RenameProfileAsync(Guid profileId, string newName);
    Task<bool> ChangeProfileIdAsync(Guid profileId, Guid newProfileId);
    Task<bool> DeleteProfileAsync(Guid profileId);
    Task<bool> LoginProfileAsync(Guid profileId);

    #endregion

    #region User playlist data

    // For example: Last played item position, playlist configuration etc. per user
    Task<AsyncResult<string>> GetUserPlaylistDataAsync(Guid profileId, Guid playlistId, string key);
    Task<bool> SetUserPlaylistDataAsync(Guid profileId, Guid playlistId, string key, string data);

    #endregion

    #region User media item data

    // For example: Last video play position
    Task<AsyncResult<string>> GetUserMediaItemDataAsync(Guid profileId, Guid mediaItemId, string key);
    Task<bool> SetUserMediaItemDataAsync(Guid profileId, Guid mediaItemId, string key, string data);

    #endregion

    #region User additional data

    // Other global user data 
    Task<bool> SetUserAdditionalDataAsync(Guid profileId, string key, string data, int dataNo = 0);
    Task<AsyncResult<string>> GetUserAdditionalDataAsync(Guid profileId, string key, int dataNo = 0);
    Task<AsyncResult<IEnumerable<Tuple<int, string>>>> GetUserAdditionalDataListAsync(Guid profileId, string key, bool sortByKey = false, SortDirection sortDirection = SortDirection.Ascending, uint? offset = null, uint ? limit = null);
    Task<AsyncResult<IEnumerable<Tuple<string, int, string>>>> GetUserSelectedAdditionalDataListAsync(Guid profileId, string[] keys, bool sortByKey = false, SortDirection sortDirection = SortDirection.Ascending, uint? offset = null, uint? limit = null);

    #endregion

    #region Cleanup user data

    Task<bool> ClearAllUserDataAsync(Guid profileId);
    Task<bool> ClearUserMediaItemDataKeyAsync(Guid profileId, string key);
    Task<bool> ClearUserAdditionalDataKeyAsync(Guid profileId, string key);

    #endregion
  }
}
