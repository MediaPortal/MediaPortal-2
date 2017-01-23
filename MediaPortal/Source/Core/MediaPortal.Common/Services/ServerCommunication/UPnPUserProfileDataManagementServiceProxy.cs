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

namespace MediaPortal.Common.Services.ServerCommunication
{
  /// <summary>
  /// Provides the MediaPortal 2 UPnP client's proxy for the user profile data management service.
  /// </summary>
  public class UPnPUserProfileDataManagementServiceProxy : UPnPServiceProxyBase, IUserProfileDataManagement
  {
    public UPnPUserProfileDataManagementServiceProxy(CpService serviceStub) : base(serviceStub, "UserProfileDataManagement") { }

    #region User profiles management

    public ICollection<UserProfile> GetProfiles()
    {
      CpAction action = GetAction("GetProfiles");
      IList<object> outParameters = action.InvokeAction(null);
      return new List<UserProfile>((IEnumerable<UserProfile>) outParameters[0]);
    }

    public bool GetProfile(Guid profileId, out UserProfile userProfile)
    {
      CpAction action = GetAction("GetProfile");
      IList<object> inParameters = new List<object> {MarshallingHelper.SerializeGuid(profileId)};
      IList<object> outParameters = action.InvokeAction(inParameters);
      userProfile = (UserProfile) outParameters[0];
      return userProfile != null;
    }

    public bool GetProfileByName(string profileName, out UserProfile userProfile)
    {
      CpAction action = GetAction("GetProfileByName");
      IList<object> inParameters = new List<object> {profileName};
      IList<object> outParameters = action.InvokeAction(inParameters);
      userProfile = (UserProfile) outParameters[0];
      return userProfile != null;
    }

    public Guid CreateProfile(string profileName)
    {
      CpAction action = GetAction("CreateProfile");
      IList<object> inParameters = new List<object> {profileName};
      IList<object> outParameters = action.InvokeAction(inParameters);
      return MarshallingHelper.DeserializeGuid((string) outParameters[0]);
    }

    public bool RenameProfile(Guid profileId, string newName)
    {
      CpAction action = GetAction("RenameProfile");
      IList<object> inParameters = new List<object> {MarshallingHelper.SerializeGuid(profileId), newName};
      IList<object> outParameters = action.InvokeAction(inParameters);
      return (bool) outParameters[0];
    }

    public bool DeleteProfile(Guid profileId)
    {
      CpAction action = GetAction("DeleteProfile");
      IList<object> inParameters = new List<object> {MarshallingHelper.SerializeGuid(profileId)};
      IList<object> outParameters = action.InvokeAction(inParameters);
      return (bool) outParameters[0];
    }

    #endregion

    #region User playlist data

    public bool GetUserPlaylistData(Guid profileId, Guid playlistId, string key, out string data)
    {
      CpAction action = GetAction("GetUserPlaylistData");
      IList<object> inParameters = new List<object>
        {
            MarshallingHelper.SerializeGuid(profileId),
            MarshallingHelper.SerializeGuid(playlistId),
            key
        };
      IList<object> outParameters = action.InvokeAction(inParameters);
      data = (string) outParameters[0];
      return (bool) outParameters[1];
    }

    public bool SetUserPlaylistData(Guid profileId, Guid playlistId, string key, string data)
    {
      CpAction action = GetAction("SetUserPlaylistData");
      IList<object> inParameters = new List<object>
        {
            MarshallingHelper.SerializeGuid(profileId),
            MarshallingHelper.SerializeGuid(playlistId),
            key,
            data
        };
      IList<object> outParameters = action.InvokeAction(inParameters);
      return (bool) outParameters[0];
    }

    #endregion

    #region User media item data

    public bool GetUserMediaItemData(Guid profileId, Guid mediaItemId, string key, out string data)
    {
      CpAction action = GetAction("GetUserMediaItemData");
      IList<object> inParameters = new List<object>
        {
            MarshallingHelper.SerializeGuid(profileId),
            MarshallingHelper.SerializeGuid(mediaItemId),
            key
        };
      IList<object> outParameters = action.InvokeAction(inParameters);
      data = (string) outParameters[0];
      return (bool) outParameters[1];
    }

    public bool SetUserMediaItemData(Guid profileId, Guid mediaItemId, string key, string data)
    {
      CpAction action = GetAction("SetUserMediaItemData");
      IList<object> inParameters = new List<object>
        {
            MarshallingHelper.SerializeGuid(profileId),
            MarshallingHelper.SerializeGuid(mediaItemId),
            key,
            data
        };
      IList<object> outParameters = action.InvokeAction(inParameters);
      return (bool) outParameters[0];
    }

    #endregion

    #region User additional data

    public bool GetUserAdditionalData(Guid profileId, string key, out string data)
    {
      CpAction action = GetAction("GetUserAdditionalData");
      IList<object> inParameters = new List<object>
        {
            MarshallingHelper.SerializeGuid(profileId),
            key
        };
      IList<object> outParameters = action.InvokeAction(inParameters);
      data = (string) outParameters[0];
      return (bool) outParameters[1];
    }

    public bool SetUserAdditionalData(Guid profileId, string key, string data)
    {
      CpAction action = GetAction("SetUserAdditionalData");
      IList<object> inParameters = new List<object>
        {
            MarshallingHelper.SerializeGuid(profileId),
            key,
            data
        };
      IList<object> outParameters = action.InvokeAction(inParameters);
      return (bool) outParameters[0];
    }

    #endregion

    #region Cleanup user data

    public bool ClearAllUserData(Guid profileId)
    {
      CpAction action = GetAction("ClearAllUserData");
      IList<object> inParameters = new List<object> {MarshallingHelper.SerializeGuid(profileId)};
      IList<object> outParameters = action.InvokeAction(inParameters);
      return (bool) outParameters[0];
    }

    #endregion
  }
}
