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
using MediaPortal.Common;
using MediaPortal.Common.UPnP;
using MediaPortal.Common.UserProfileDataManagement;
using MediaPortal.Utilities.UPnP;
using UPnP.Infrastructure.Common;
using UPnP.Infrastructure.Dv;
using UPnP.Infrastructure.Dv.DeviceTree;
using MediaPortal.Backend.MediaLibrary;

namespace MediaPortal.Backend.Services.UserProfileDataManagement
{
  /// <summary>
  /// Provides the UPnP service for the MediaPortal 2 user profile data management.
  /// </summary>
  public class UPnPUserProfileDataManagementServiceImpl : DvService
  {
    public UPnPUserProfileDataManagementServiceImpl() : base(
        UPnPTypesAndIds.USER_PROFILE_DATA_MANAGEMENT_SERVICE_TYPE, UPnPTypesAndIds.USER_PROFILE_DATA_MANAGEMENT_SERVICE_TYPE_VERSION,
        UPnPTypesAndIds.USER_PROFILE_DATA_MANAGEMENT_SERVICE_ID)
    {
      // Used to transport an enumeration of user profiles
      DvStateVariable A_ARG_TYPE_UserProfileEnumeration = new DvStateVariable("A_ARG_TYPE_UserProfileEnumeration", new DvExtendedDataType(UPnPExtendedDataTypes.DtUserProfileEnumeration))
          {
            SendEvents = false,
          };
      AddStateVariable(A_ARG_TYPE_UserProfileEnumeration);

      //Used for transporting a user profile
      DvStateVariable A_ARG_TYPE_UserProfile = new DvStateVariable("A_ARG_TYPE_UserProfile", new DvExtendedDataType(UPnPExtendedDataTypes.DtUserProfile))
          {
            SendEvents = false,
          };
      AddStateVariable(A_ARG_TYPE_UserProfile);

      // Used for boolean values
      DvStateVariable A_ARG_TYPE_Bool = new DvStateVariable("A_ARG_TYPE_Bool", new DvStandardDataType(UPnPStandardDataType.Boolean))
          {
            SendEvents = false
          };
      AddStateVariable(A_ARG_TYPE_Bool);

      // Used for any single GUID value
      DvStateVariable A_ARG_TYPE_Uuid = new DvStateVariable("A_ARG_TYPE_Id", new DvStandardDataType(UPnPStandardDataType.Uuid))
          {
            SendEvents = false
          };
      AddStateVariable(A_ARG_TYPE_Uuid);

      // Used for string values
      DvStateVariable A_ARG_TYPE_String = new DvStateVariable("A_ARG_TYPE_String", new DvStandardDataType(UPnPStandardDataType.String))
          {
            SendEvents = false
          };
      AddStateVariable(A_ARG_TYPE_String);

      // More state variables go here


      // User profiles management
      DvAction getProfilesAction = new DvAction("GetProfiles", OnGetProfiles,
          new DvArgument[] {
          },
          new DvArgument[] {
            new DvArgument("Profiles", A_ARG_TYPE_UserProfileEnumeration, ArgumentDirection.Out, true)
          });
      AddAction(getProfilesAction);

      DvAction getProfileAction = new DvAction("GetProfile", OnGetProfile,
          new DvArgument[] {
            new DvArgument("ProfileId", A_ARG_TYPE_Uuid, ArgumentDirection.In)
          },
          new DvArgument[] {
            new DvArgument("Profile", A_ARG_TYPE_UserProfile, ArgumentDirection.Out, true)
          });
      AddAction(getProfileAction);

      DvAction getProfileByNameAction = new DvAction("GetProfileByName", OnGetProfileByName,
          new DvArgument[] {
            new DvArgument("ProfileName", A_ARG_TYPE_String, ArgumentDirection.In)
          },
          new DvArgument[] {
            new DvArgument("Profile", A_ARG_TYPE_UserProfile, ArgumentDirection.Out, true)
          });
      AddAction(getProfileByNameAction);

      DvAction createProfileAction = new DvAction("CreateProfile", OnCreateProfile,
          new DvArgument[] {
            new DvArgument("ProfileName", A_ARG_TYPE_String, ArgumentDirection.In)
          },
          new DvArgument[] {
            new DvArgument("ProfileId", A_ARG_TYPE_Uuid, ArgumentDirection.Out, true)
          });
      AddAction(createProfileAction);

      DvAction renameProfileAction = new DvAction("RenameProfile", OnRenameProfile,
          new DvArgument[] {
            new DvArgument("ProfileId", A_ARG_TYPE_Uuid, ArgumentDirection.In),
            new DvArgument("NewName", A_ARG_TYPE_String, ArgumentDirection.In)
          },
          new DvArgument[] {
            new DvArgument("Success", A_ARG_TYPE_Bool, ArgumentDirection.Out, true)
          });
      AddAction(renameProfileAction);

      DvAction deleteProfileAction = new DvAction("DeleteProfile", OnDeleteProfile,
          new DvArgument[] {
            new DvArgument("ProfileId", A_ARG_TYPE_Uuid, ArgumentDirection.In)
          },
          new DvArgument[] {
            new DvArgument("Success", A_ARG_TYPE_Bool, ArgumentDirection.Out, true)
          });
      AddAction(deleteProfileAction);

      // User playlist data
      DvAction getUserPlaylistDataAction = new DvAction("GetUserPlaylistData", OnGetUserPlaylistData,
          new DvArgument[] {
            new DvArgument("ProfileId", A_ARG_TYPE_Uuid, ArgumentDirection.In),
            new DvArgument("PlaylistId", A_ARG_TYPE_Uuid, ArgumentDirection.In),
            new DvArgument("Key", A_ARG_TYPE_String, ArgumentDirection.In)
          },
          new DvArgument[] {
            new DvArgument("Data", A_ARG_TYPE_String, ArgumentDirection.Out),
            new DvArgument("Success", A_ARG_TYPE_Bool, ArgumentDirection.Out, true)
          });
      AddAction(getUserPlaylistDataAction);

      DvAction setUserPlaylistDataAction = new DvAction("SetUserPlaylistData", OnSetUserPlaylistData,
          new DvArgument[] {
            new DvArgument("ProfileId", A_ARG_TYPE_Uuid, ArgumentDirection.In),
            new DvArgument("PlaylistId", A_ARG_TYPE_Uuid, ArgumentDirection.In),
            new DvArgument("Key", A_ARG_TYPE_String, ArgumentDirection.In),
            new DvArgument("Data", A_ARG_TYPE_String, ArgumentDirection.In)
          },
          new DvArgument[] {
            new DvArgument("Success", A_ARG_TYPE_Bool, ArgumentDirection.Out, true)
          });
      AddAction(setUserPlaylistDataAction);

      // User media item data
      DvAction getUserMediaItemDataAction = new DvAction("GetUserMediaItemData", OnGetUserMediaItemData,
          new DvArgument[] {
            new DvArgument("ProfileId", A_ARG_TYPE_Uuid, ArgumentDirection.In),
            new DvArgument("MediaItemId", A_ARG_TYPE_Uuid, ArgumentDirection.In),
            new DvArgument("Key", A_ARG_TYPE_String, ArgumentDirection.In)
          },
          new DvArgument[] {
            new DvArgument("Data", A_ARG_TYPE_String, ArgumentDirection.Out),
            new DvArgument("Success", A_ARG_TYPE_Bool, ArgumentDirection.Out, true)
          });
      AddAction(getUserMediaItemDataAction);

      DvAction setUserMediaItemDataAction = new DvAction("SetUserMediaItemData", OnSetUserMediaItemData,
          new DvArgument[] {
            new DvArgument("ProfileId", A_ARG_TYPE_Uuid, ArgumentDirection.In),
            new DvArgument("MediaItemId", A_ARG_TYPE_Uuid, ArgumentDirection.In),
            new DvArgument("Key", A_ARG_TYPE_String, ArgumentDirection.In),
            new DvArgument("Data", A_ARG_TYPE_String, ArgumentDirection.In)
          },
          new DvArgument[] {
            new DvArgument("Success", A_ARG_TYPE_Bool, ArgumentDirection.Out, true)
          });
      AddAction(setUserMediaItemDataAction);

      // User additional data
      DvAction getUserAdditionalDataAction = new DvAction("GetUserAdditionalData", OnGetUserAdditionalData,
          new DvArgument[] {
            new DvArgument("ProfileId", A_ARG_TYPE_Uuid, ArgumentDirection.In),
            new DvArgument("Key", A_ARG_TYPE_String, ArgumentDirection.In)
          },
          new DvArgument[] {
            new DvArgument("Data", A_ARG_TYPE_String, ArgumentDirection.Out),
            new DvArgument("Success", A_ARG_TYPE_Bool, ArgumentDirection.Out, true)
          });
      AddAction(getUserAdditionalDataAction);

      DvAction setUserAdditionalDataAction = new DvAction("SetUserAdditionalData", OnSetUserAdditionalData,
          new DvArgument[] {
            new DvArgument("ProfileId", A_ARG_TYPE_Uuid, ArgumentDirection.In),
            new DvArgument("Key", A_ARG_TYPE_String, ArgumentDirection.In),
            new DvArgument("Data", A_ARG_TYPE_String, ArgumentDirection.In)
          },
          new DvArgument[] {
            new DvArgument("Success", A_ARG_TYPE_Bool, ArgumentDirection.Out, true)
          });
      AddAction(setUserAdditionalDataAction);

      // Cleanup user data
      DvAction clearAllUserDataAction = new DvAction("ClearAllUserData", OnClearAllUserData,
          new DvArgument[] {
            new DvArgument("ProfileId", A_ARG_TYPE_Uuid, ArgumentDirection.In)
          },
          new DvArgument[] {
            new DvArgument("Success", A_ARG_TYPE_Bool, ArgumentDirection.Out, true)
          });
      AddAction(clearAllUserDataAction);

      // More actions go here
    }

    // User profiles management

    static UPnPError OnGetProfiles(DvAction action, IList<object> inParams, out IList<object> outParams,
        CallContext context)
    {
      ICollection<UserProfile> profiles = ServiceRegistration.Get<IUserProfileDataManagement>().GetProfiles();
      outParams = new List<object> {profiles};
      return null;
    }

    static UPnPError OnGetProfile(DvAction action, IList<object> inParams, out IList<object> outParams,
        CallContext context)
    {
      Guid profileId = MarshallingHelper.DeserializeGuid((string) inParams[0]);
      UserProfile profile;
      if (!ServiceRegistration.Get<IUserProfileDataManagement>().GetProfile(profileId, out profile))
        profile = null;
      outParams = new List<object> {profile};
      return null;
    }

    static UPnPError OnGetProfileByName(DvAction action, IList<object> inParams, out IList<object> outParams,
        CallContext context)
    {
      string profileName = (string) inParams[0];
      UserProfile profile;
      if (!ServiceRegistration.Get<IUserProfileDataManagement>().GetProfileByName(profileName, out profile))
        profile = null;
      outParams = new List<object> {profile};
      return null;
    }

    static UPnPError OnCreateProfile(DvAction action, IList<object> inParams, out IList<object> outParams,
        CallContext context)
    {
      string profileName = (string) inParams[0];
      Guid profileId = ServiceRegistration.Get<IUserProfileDataManagement>().CreateProfile(profileName);
      outParams = new List<object> {profileId};
      return null;
    }

    static UPnPError OnRenameProfile(DvAction action, IList<object> inParams, out IList<object> outParams,
        CallContext context)
    {
      Guid profileId = MarshallingHelper.DeserializeGuid((string) inParams[0]);
      string newName = (string) inParams[1];
      bool success = ServiceRegistration.Get<IUserProfileDataManagement>().RenameProfile(profileId, newName);
      outParams = new List<object> {success};
      return null;
    }

    static UPnPError OnDeleteProfile(DvAction action, IList<object> inParams, out IList<object> outParams,
        CallContext context)
    {
      Guid profileId = MarshallingHelper.DeserializeGuid((string) inParams[0]);
      bool success = ServiceRegistration.Get<IUserProfileDataManagement>().DeleteProfile(profileId);
      outParams = new List<object> {success};
      return null;
    }

    // User playlist data

    static UPnPError OnGetUserPlaylistData(DvAction action, IList<object> inParams, out IList<object> outParams,
        CallContext context)
    {
      Guid profileId = MarshallingHelper.DeserializeGuid((string) inParams[0]);
      Guid playlistId = MarshallingHelper.DeserializeGuid((string) inParams[1]);
      string key = (string) inParams[2];
      string data;
      bool success;
      if (!(success = ServiceRegistration.Get<IUserProfileDataManagement>().GetUserPlaylistData(profileId, playlistId, key, out data)))
        data = null;
      outParams = new List<object> {data, success};
      return null;
    }

    static UPnPError OnSetUserPlaylistData(DvAction action, IList<object> inParams, out IList<object> outParams,
        CallContext context)
    {
      Guid profileId = MarshallingHelper.DeserializeGuid((string) inParams[0]);
      Guid playlistId = MarshallingHelper.DeserializeGuid((string) inParams[1]);
      string key = (string) inParams[2];
      string data = (string) inParams[3];
      bool success = ServiceRegistration.Get<IUserProfileDataManagement>().SetUserPlaylistData(profileId, playlistId, key, data);
      outParams = new List<object> {success};
      return null;
    }

    // User media item data

    static UPnPError OnGetUserMediaItemData(DvAction action, IList<object> inParams, out IList<object> outParams,
        CallContext context)
    {
      Guid profileId = MarshallingHelper.DeserializeGuid((string) inParams[0]);
      Guid mediaItemId = MarshallingHelper.DeserializeGuid((string) inParams[1]);
      string key = (string) inParams[2];
      string data;
      bool success;
      if (!(success = ServiceRegistration.Get<IUserProfileDataManagement>().GetUserMediaItemData(profileId, mediaItemId, key, out data)))
        data = null;
      outParams = new List<object> {data, success};
      return null;
    }

    static UPnPError OnSetUserMediaItemData(DvAction action, IList<object> inParams, out IList<object> outParams,
        CallContext context)
    {
      Guid profileId = MarshallingHelper.DeserializeGuid((string) inParams[0]);
      Guid mediaItemId = MarshallingHelper.DeserializeGuid((string) inParams[1]);
      string key = (string) inParams[2];
      string data = (string) inParams[3];
      bool success = ServiceRegistration.Get<IUserProfileDataManagement>().SetUserMediaItemData(profileId, mediaItemId, key, data);
      if(success)
        ServiceRegistration.Get<IMediaLibrary>().UserDataUpdated(profileId, mediaItemId, key, data);
      outParams = new List<object> {success};
      return null;
    }

    // User additional data

    static UPnPError OnGetUserAdditionalData(DvAction action, IList<object> inParams, out IList<object> outParams,
        CallContext context)
    {
      Guid profileId = MarshallingHelper.DeserializeGuid((string) inParams[0]);
      string key = (string) inParams[1];
      string data;
      bool success;
      if (!(success = ServiceRegistration.Get<IUserProfileDataManagement>().GetUserAdditionalData(profileId, key, out data)))
        data = null;
      outParams = new List<object> {data, success};
      return null;
    }

    static UPnPError OnSetUserAdditionalData(DvAction action, IList<object> inParams, out IList<object> outParams,
        CallContext context)
    {
      Guid profileId = MarshallingHelper.DeserializeGuid((string) inParams[0]);
      string key = (string) inParams[1];
      string data = (string) inParams[2];
      bool success = ServiceRegistration.Get<IUserProfileDataManagement>().SetUserAdditionalData(profileId, key, data);
      outParams = new List<object> {success};
      return null;
    }

    // Cleanup user data

    static UPnPError OnClearAllUserData(DvAction action, IList<object> inParams, out IList<object> outParams,
        CallContext context)
    {
      Guid profileId = MarshallingHelper.DeserializeGuid((string) inParams[0]);
      bool success = ServiceRegistration.Get<IUserProfileDataManagement>().ClearAllUserData(profileId);
      outParams = new List<object> {success};
      return null;
    }
  }
}
