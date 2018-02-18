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
using MediaPortal.Common.UPnP;
using MediaPortal.Common.UserProfileDataManagement;
using MediaPortal.Utilities.UPnP;
using UPnP.Infrastructure.CP.DeviceTree;
using System.Linq;
using System.Threading.Tasks;
using MediaPortal.Common.MediaManagement.MLQueries;

namespace MediaPortal.Common.Services.ServerCommunication
{
  /// <summary>
  /// Provides the MediaPortal 2 UPnP client's proxy for the user profile data management service.
  /// </summary>
  public class UPnPUserProfileDataManagementServiceProxy : UPnPServiceProxyBase, IUserProfileDataManagement
  {
    public UPnPUserProfileDataManagementServiceProxy(CpService serviceStub) : base(serviceStub, "UserProfileDataManagement") { }

    #region User profiles management

    public async Task<ICollection<UserProfile>> GetProfilesAsync()
    {
      CpAction action = GetAction("GetProfiles");
      IList<object> outParameters = await action.InvokeAsync(null);
      return new List<UserProfile>((IEnumerable<UserProfile>)outParameters[0]);
    }

    public async Task<AsyncResult<UserProfile>> GetProfileAsync(Guid profileId)
    {
      CpAction action = GetAction("GetProfile");
      IList<object> inParameters = new List<object> { MarshallingHelper.SerializeGuid(profileId) };
      IList<object> outParameters = await action.InvokeAsync(inParameters);
      var userProfile = (UserProfile)outParameters[0];
      return new AsyncResult<UserProfile>(userProfile != null, userProfile);
    }

    public async Task<AsyncResult<UserProfile>> GetProfileByNameAsync(string profileName)
    {
      CpAction action = GetAction("GetProfileByName");
      IList<object> inParameters = new List<object> { profileName };
      IList<object> outParameters = await action.InvokeAsync(inParameters);
      var userProfile = (UserProfile)outParameters[0];
      return new AsyncResult<UserProfile>(userProfile != null, userProfile);
    }

    public async Task<Guid> CreateProfileAsync(string profileName)
    {
      CpAction action = GetAction("CreateProfile");
      IList<object> inParameters = new List<object> { profileName };
      IList<object> outParameters = await action.InvokeAsync(inParameters);
      return MarshallingHelper.DeserializeGuid((string)outParameters[0]);
    }

    public async Task<Guid> CreateProfileAsync(string profileName, UserProfileType profileType, string profilePassword)
    {
      CpAction action = GetAction("CreateUserProfile");
      IList<object> inParameters = new List<object> { profileName, (int)profileType, profilePassword };
      IList<object> outParameters = await action.InvokeAsync(inParameters);
      return MarshallingHelper.DeserializeGuid((string)outParameters[0]);
    }

    public async Task<bool> UpdateProfileAsync(Guid profileId, string profileName, UserProfileType profileType, string profilePassword)
    {
      CpAction action = GetAction("UpdateUserProfile");
      IList<object> inParameters = new List<object> { MarshallingHelper.SerializeGuid(profileId), profileName, (int)profileType, profilePassword };
      IList<object> outParameters = await action.InvokeAsync(inParameters);
      return (bool)outParameters[0];
    }

    public async Task<bool> SetProfileImageAsync(Guid profileId, byte[] profileImage)
    {
      CpAction action = GetAction("SetProfileImage");
      IList<object> inParameters = new List<object> { MarshallingHelper.SerializeGuid(profileId), profileImage != null && profileImage.Length > 0 ? Convert.ToBase64String(profileImage) : "" };
      IList<object> outParameters = await action.InvokeAsync(inParameters);
      return (bool)outParameters[0];
    }

    public async Task<bool> RenameProfileAsync(Guid profileId, string newName)
    {
      CpAction action = GetAction("RenameProfile");
      IList<object> inParameters = new List<object> { MarshallingHelper.SerializeGuid(profileId), newName };
      IList<object> outParameters = await action.InvokeAsync(inParameters);
      return (bool)outParameters[0];
    }

    public async Task<bool> DeleteProfileAsync(Guid profileId)
    {
      CpAction action = GetAction("DeleteProfile");
      IList<object> inParameters = new List<object> { MarshallingHelper.SerializeGuid(profileId) };
      IList<object> outParameters = await action.InvokeAsync(inParameters);
      return (bool)outParameters[0];
    }

    public async Task<bool> LoginProfileAsync(Guid profileId)
    {
      CpAction action = GetAction("LoginProfile");
      IList<object> inParameters = new List<object> { MarshallingHelper.SerializeGuid(profileId) };
      IList<object> outParameters = await action.InvokeAsync(inParameters);
      return (bool)outParameters[0];
    }

    #endregion

    #region User playlist data

    public async Task<AsyncResult<string>> GetUserPlaylistDataAsync(Guid profileId, Guid playlistId, string key)
    {
      CpAction action = GetAction("GetUserPlaylistData");
      IList<object> inParameters = new List<object>
        {
            MarshallingHelper.SerializeGuid(profileId),
            MarshallingHelper.SerializeGuid(playlistId),
            key
        };
      IList<object> outParameters = await action.InvokeAsync(inParameters);
      return new AsyncResult<string>((bool)outParameters[1], (string)outParameters[0]);
    }

    public async Task<bool> SetUserPlaylistDataAsync(Guid profileId, Guid playlistId, string key, string data)
    {
      CpAction action = GetAction("SetUserPlaylistData");
      IList<object> inParameters = new List<object>
        {
            MarshallingHelper.SerializeGuid(profileId),
            MarshallingHelper.SerializeGuid(playlistId),
            key,
            data
        };
      IList<object> outParameters = await action.InvokeAsync(inParameters);
      return (bool)outParameters[0];
    }

    #endregion

    #region User media item data

    public async Task<AsyncResult<string>> GetUserMediaItemDataAsync(Guid profileId, Guid mediaItemId, string key)
    {
      CpAction action = GetAction("GetUserMediaItemData");
      IList<object> inParameters = new List<object>
        {
            MarshallingHelper.SerializeGuid(profileId),
            MarshallingHelper.SerializeGuid(mediaItemId),
            key
        };
      IList<object> outParameters = await action.InvokeAsync(inParameters);
      return new AsyncResult<string>((bool)outParameters[1], (string)outParameters[0]);
    }

    public async Task<bool> SetUserMediaItemDataAsync(Guid profileId, Guid mediaItemId, string key, string data)
    {
      CpAction action = GetAction("SetUserMediaItemData");
      IList<object> inParameters = new List<object>
        {
            MarshallingHelper.SerializeGuid(profileId),
            MarshallingHelper.SerializeGuid(mediaItemId),
            key,
            data
        };
      IList<object> outParameters = await action.InvokeAsync(inParameters);
      return (bool)outParameters[0];
    }

    #endregion

    #region User additional data

    public async Task<AsyncResult<string>> GetUserAdditionalDataAsync(Guid profileId, string key, int dataNo = 0)
    {
      CpAction action = GetAction("GetUserAdditionalData");
      IList<object> inParameters = new List<object>
        {
            MarshallingHelper.SerializeGuid(profileId),
            key,
            dataNo
        };
      IList<object> outParameters = await action.InvokeAsync(inParameters);
      return new AsyncResult<string>((bool)outParameters[1], (string)outParameters[0]);
    }

    public async Task<bool> SetUserAdditionalDataAsync(Guid profileId, string key, string data, int dataNo = 0)
    {
      CpAction action = GetAction("SetUserAdditionalData");
      IList<object> inParameters = new List<object>
        {
            MarshallingHelper.SerializeGuid(profileId),
            key,
            dataNo,
            data
        };
      IList<object> outParameters = await action.InvokeAsync(inParameters);
      return (bool)outParameters[0];
    }

    public async Task<AsyncResult<IEnumerable<Tuple<int, string>>>> GetUserAdditionalDataListAsync(Guid profileId, string key, bool sortByKey = false, SortDirection sortDirection = SortDirection.Ascending, uint? offset = null, uint? limit = null)
    {
      CpAction action = GetAction("GetUserAdditionalDataList");
      IList<object> inParameters = new List<object>
        {
            MarshallingHelper.SerializeGuid(profileId),
            key,
            sortByKey,
            (int)sortDirection,
            offset,
            limit
        };
      IList<object> outParameters = await action.InvokeAsync(inParameters);
      IEnumerable<Tuple<int, string>> data = null;
      if (outParameters[0] != null)
        data = MarshallingHelper.ParseCsvTuple2Collection((string)outParameters[0]).Select(t => new Tuple<int, string>(Convert.ToInt32(t.Item1), t.Item2));
      return new AsyncResult<IEnumerable<Tuple<int, string>>>((bool)outParameters[1], data);
    }

    public async Task<AsyncResult<IEnumerable<Tuple<string, int, string>>>> GetUserSelectedAdditionalDataListAsync(Guid profileId, string[] keys, bool sortByKey = false, SortDirection sortDirection = SortDirection.Ascending, uint? offset = null, uint? limit = null)
    {
      CpAction action = GetAction("GetUserSelectedAdditionalDataList");
      IList<object> inParameters = new List<object>
        {
            MarshallingHelper.SerializeGuid(profileId),
            MarshallingHelper.SerializeStringEnumerationToCsv(keys),
            sortByKey,
            (int)sortDirection,
            offset,
            limit
        };
      IList<object> outParameters = await action.InvokeAsync(inParameters);
      IEnumerable<Tuple<string, int, string>> data = null;
      if (outParameters[0] != null)
        data = MarshallingHelper.ParseCsvTuple3Collection((string)outParameters[0]).Select(t => new Tuple<string, int, string>(t.Item1, Convert.ToInt32(t.Item2), t.Item3));
      return new AsyncResult<IEnumerable<Tuple<string, int, string>>>((bool)outParameters[1], data);
    }

    #endregion

    #region Cleanup user data

    public async Task<bool> ClearAllUserDataAsync(Guid profileId)
    {
      CpAction action = GetAction("ClearAllUserData");
      IList<object> inParameters = new List<object> { MarshallingHelper.SerializeGuid(profileId) };
      IList<object> outParameters = await action.InvokeAsync(inParameters);
      return (bool)outParameters[0];
    }

    public async Task<bool> ClearUserMediaItemDataKeyAsync(Guid profileId, string key)
    {
      CpAction action = GetAction("ClearUserMediaItemDataKey");
      IList<object> inParameters = new List<object> { MarshallingHelper.SerializeGuid(profileId), key };
      IList<object> outParameters = await action.InvokeAsync(inParameters);
      return (bool)outParameters[0];
    }

    public async Task<bool> ClearUserAdditionalDataKeyAsync(Guid profileId, string key)
    {
      CpAction action = GetAction("ClearUserAdditionalDataKey");
      IList<object> inParameters = new List<object> { MarshallingHelper.SerializeGuid(profileId), key };
      IList<object> outParameters = await action.InvokeAsync(inParameters);
      return (bool)outParameters[0];
    }

    #endregion
  }
}
