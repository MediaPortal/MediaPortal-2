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
using System.Collections.Generic;
using System.Data;
using MediaPortal.Common;
using MediaPortal.Common.PathManager;
using MediaPortal.Backend.Database;
using MediaPortal.Utilities;
using MediaPortal.Common.UserProfileDataManagement;
using MediaPortal.Common.MediaManagement.MLQueries;

namespace MediaPortal.Backend.Services.UserProfileDataManagement
{
  /// <summary>
  /// Creates SQL commands for the communication with the UserProfileDataManagement subschema.
  /// </summary>
  public class UserProfileDataManagement_SubSchema
  {
    #region Consts

    public const string SUBSCHEMA_NAME = "UserProfileDataManagement";

    public const int EXPECTED_SCHEMA_VERSION_MAJOR = 1;
    public const int EXPECTED_SCHEMA_VERSION_MINOR = 1;

    internal const string USER_TABLE_NAME = "USER_PROFILES";
    internal const string USER_DATA_TABLE_NAME = "USER_ADDITIONAL_DATA";
    internal const string USER_MEDIA_ITEM_DATA_TABLE_NAME = "USER_MEDIA_ITEM_DATA";
    internal const string USER_PROFILE_ID_COL_NAME = "PROFILE_ID";
    internal const string USER_DATA_KEY_COL_NAME = "DATA_KEY";
    internal const string USER_DATA_VALUE_COL_NAME = "MEDIA_ITEM_DATA";
    internal const string USER_DATA_VALUE_NO_COL_NAME = "DATA_NO";
    internal const string USER_ADDITIONAL_DATA_VALUE_COL_NAME = "ADDITIONAL_DATA";

    #endregion

    public static string SubSchemaScriptDirectory
    {
      get
      {
        IPathManager pathManager = ServiceRegistration.Get<IPathManager>();
        return pathManager.GetPath(@"<APPLICATION_ROOT>\Scripts\");
      }
    }

    // User profiles

    public static IDbCommand SelectUserProfilesCommand(ITransaction transaction, Guid? profileId, string name,
        out int profileIdIndex, out int nameIndex, out int profileTypeIndex, out int passwordIndex, out int lastLoginIndex, out int imageIndex)
    {
      ISQLDatabase database = transaction.Database;
      IDbCommand result = transaction.CreateCommand();
      result.CommandText = "SELECT PROFILE_ID, NAME, PROFILE_TYPE, PASSWORD, LAST_LOGIN, IMAGE FROM USER_PROFILES";

      IList<string> filters = new List<string>(2);
      if (profileId.HasValue)
        filters.Add("PROFILE_ID=@PROFILE_ID");
      if (!string.IsNullOrEmpty(name))
        filters.Add("NAME=@NAME");

      if (filters.Count > 0)
        result.CommandText += " WHERE " + StringUtils.Join(" AND ", filters);

      if (profileId.HasValue)
        database.AddParameter(result, "PROFILE_ID", profileId, typeof(Guid));
      if (!string.IsNullOrEmpty(name))
        database.AddParameter(result, "NAME", name, typeof(string));

      profileIdIndex = 0;
      nameIndex = 1;
      profileTypeIndex = 2;
      passwordIndex = 3;
      lastLoginIndex = 4;
      imageIndex = 5;
      return result;
    }

    public static IDbCommand CreateUserProfileCommand(ITransaction transaction, Guid profileId, string name, UserProfileType profileType = UserProfileType.ClientProfile, string password = null, byte[] image = null)
    {
      IDbCommand result = transaction.CreateCommand();
      result.CommandText = "INSERT INTO USER_PROFILES (PROFILE_ID, NAME, PROFILE_TYPE, PASSWORD, LAST_LOGIN, IMAGE) VALUES (@PROFILE_ID, @NAME, @PROFILE_TYPE, @PASSWORD, @LAST_LOGIN, @IMAGE)";
      ISQLDatabase database = transaction.Database;
      database.AddParameter(result, "PROFILE_ID", profileId, typeof(Guid));
      database.AddParameter(result, "NAME", name, typeof(string));
      database.AddParameter(result, "PROFILE_TYPE", (int)profileType, typeof(int));
      database.AddParameter(result, "PASSWORD", password, typeof(string));
      database.AddParameter(result, "LAST_LOGIN", DateTime.Now, typeof(DateTime));
      database.AddParameter(result, "IMAGE", image, typeof(byte[]));
      return result;
    }

    public static IDbCommand UpdateUserProfileCommand(ITransaction transaction, Guid profileId, string name, UserProfileType profileType = UserProfileType.ClientProfile, string password = null)
    {
      IDbCommand result = transaction.CreateCommand();
      result.CommandText = "UPDATE USER_PROFILES SET NAME=@NAME, PROFILE_TYPE=@PROFILE_TYPE, PASSWORD=@PASSWORD WHERE PROFILE_ID=@PROFILE_ID";
      ISQLDatabase database = transaction.Database;
      database.AddParameter(result, "PROFILE_ID", profileId, typeof(Guid));
      database.AddParameter(result, "NAME", name, typeof(string));
      database.AddParameter(result, "PROFILE_TYPE", (int)profileType, typeof(int));
      database.AddParameter(result, "PASSWORD", password, typeof(string));
      return result;
    }

    public static IDbCommand UpdateUserProfileNameCommand(ITransaction transaction, Guid profileId, string newName)
    {
      IDbCommand result = transaction.CreateCommand();
      result.CommandText = "UPDATE USER_PROFILES SET NAME=@NAME WHERE PROFILE_ID=@PROFILE_ID";
      ISQLDatabase database = transaction.Database;
      database.AddParameter(result, "NAME", newName, typeof(string));
      database.AddParameter(result, "PROFILE_ID", profileId, typeof(Guid));
      return result;
    }

    public static IDbCommand SelectUserProfileNameCommand(ITransaction transaction, Guid profileId, out int nameIndex)
    {
      IDbCommand result = transaction.CreateCommand();
      result.CommandText = "SELECT NAME FROM USER_PROFILES WHERE PROFILE_ID=@PROFILE_ID";
      ISQLDatabase database = transaction.Database;
      database.AddParameter(result, "PROFILE_ID", profileId, typeof(Guid));

      nameIndex = 0;
      return result;
    }

    public static IDbCommand CopyUserProfileCommand(ITransaction transaction, Guid profileId, Guid newProfileId, string newName)
    {
      IDbCommand result = transaction.CreateCommand();
      result.CommandText = "INSERT INTO USER_PROFILES (PROFILE_ID, NAME, PROFILE_TYPE, PASSWORD, IMAGE, LAST_LOGIN) " +
        "SELECT @NEW_PROFILE_ID, @NAME, PROFILE_TYPE, PASSWORD, IMAGE, LAST_LOGIN FROM USER_PROFILES WHERE PROFILE_ID=@PROFILE_ID";
      ISQLDatabase database = transaction.Database;
      database.AddParameter(result, "PROFILE_ID", profileId, typeof(Guid));
      database.AddParameter(result, "NEW_PROFILE_ID", newProfileId, typeof(Guid));
      database.AddParameter(result, "NAME", newName, typeof(string));
      return result;
    }

    public static IDbCommand UpdateUserProfileCommand(ITransaction transaction, Guid profileId, string newName, string newPassword)
    {
      IDbCommand result = transaction.CreateCommand();
      result.CommandText = "UPDATE USER_PROFILES SET NAME=@NAME, PASSWORD=@PASSWORD WHERE PROFILE_ID=@PROFILE_ID";
      ISQLDatabase database = transaction.Database;
      database.AddParameter(result, "NAME", newName, typeof(string));
      database.AddParameter(result, "PROFILE_ID", profileId, typeof(Guid));
      database.AddParameter(result, "PASSWORD", newPassword, typeof(string));
      return result;
    }

    public static IDbCommand SetUserProfileImageCommand(ITransaction transaction, Guid profileId, byte[] newImage)
    {
      IDbCommand result = transaction.CreateCommand();
      result.CommandText = "UPDATE USER_PROFILES SET IMAGE=@IMAGE WHERE PROFILE_ID=@PROFILE_ID";
      ISQLDatabase database = transaction.Database;
      database.AddParameter(result, "PROFILE_ID", profileId, typeof(Guid));
      database.AddParameter(result, "IMAGE", newImage, typeof(byte[]));
      return result;
    }

    public static IDbCommand LoginUserProfileCommand(ITransaction transaction, Guid profileId)
    {
      IDbCommand result = transaction.CreateCommand();
      result.CommandText = "UPDATE USER_PROFILES SET LAST_LOGIN=@LAST_LOGIN WHERE PROFILE_ID=@PROFILE_ID";
      ISQLDatabase database = transaction.Database;
      database.AddParameter(result, "LAST_LOGIN", DateTime.Now, typeof(DateTime));
      database.AddParameter(result, "PROFILE_ID", profileId, typeof(Guid));
      return result;
    }

    public static IDbCommand DeleteUserProfileCommand(ITransaction transaction, Guid profileId)
    {
      IDbCommand result = transaction.CreateCommand();
      result.CommandText = "DELETE FROM USER_PROFILES WHERE PROFILE_ID=@PROFILE_ID";
      ISQLDatabase database = transaction.Database;
      database.AddParameter(result, "PROFILE_ID", profileId, typeof(Guid));
      return result;
    }

    // User playlist data

    public static IDbCommand SelectUserPlaylistDataCommand(ITransaction transaction, Guid profileId, Guid playlistId, string dataKey, out int playlistDataIndex)
    {
      IDbCommand result = transaction.CreateCommand();
      result.CommandText = "SELECT PLAYLIST_DATA FROM USER_PLAYLIST_DATA WHERE PROFILE_ID=@PROFILE_ID AND PLAYLIST_ID=@PLAYLIST_ID";
      if (!string.IsNullOrEmpty(dataKey))
        result.CommandText += " AND DATA_KEY=@DATA_KEY";
      ISQLDatabase database = transaction.Database;
      database.AddParameter(result, "PROFILE_ID", profileId, typeof(Guid));
      database.AddParameter(result, "PLAYLIST_ID", playlistId, typeof(Guid));

      if (!string.IsNullOrEmpty(dataKey))
        database.AddParameter(result, "DATA_KEY", dataKey, typeof(string));

      playlistDataIndex = 0;
      return result;
    }

    public static IDbCommand CreateUserPlaylistDataCommand(ITransaction transaction, Guid profileId, Guid playlistId, string dataKey, string playlistData)
    {
      IDbCommand result = transaction.CreateCommand();
      result.CommandText = "INSERT INTO USER_PLAYLIST_DATA (PROFILE_ID, PLAYLIST_ID, DATA_KEY, PLAYLIST_DATA) VALUES (@PROFILE_ID, @PLAYLIST_ID, @DATA_KEY, @PLAYLIST_DATA)";
      ISQLDatabase database = transaction.Database;
      database.AddParameter(result, "PROFILE_ID", profileId, typeof(Guid));
      database.AddParameter(result, "PLAYLIST_ID", playlistId, typeof(Guid));
      database.AddParameter(result, "DATA_KEY", dataKey, typeof(string));
      database.AddParameter(result, "PLAYLIST_DATA", playlistData, typeof(string));
      return result;
    }

    public static IDbCommand CopyUserPlaylistDataCommand(ITransaction transaction, Guid profileId, Guid newProfileId)
    {
      IDbCommand result = transaction.CreateCommand();
      result.CommandText = "INSERT INTO USER_PLAYLIST_DATA (PROFILE_ID, PLAYLIST_ID, DATA_KEY, PLAYLIST_DATA) " +
        "SELECT @NEW_PROFILE_ID, PLAYLIST_ID, DATA_KEY, PLAYLIST_DATA FROM USER_PLAYLIST_DATA WHERE PROFILE_ID=@PROFILE_ID";
      ISQLDatabase database = transaction.Database;
      database.AddParameter(result, "PROFILE_ID", profileId, typeof(Guid));
      database.AddParameter(result, "NEW_PROFILE_ID", newProfileId, typeof(Guid));
      return result;
    }

    public static IDbCommand DeleteUserPlaylistDataCommand(ITransaction transaction, Guid profileId, Guid? playlistId, string dataKey)
    {
      IDbCommand result = transaction.CreateCommand();
      result.CommandText = "DELETE FROM USER_PLAYLIST_DATA WHERE PROFILE_ID=@PROFILE_ID";
      if (playlistId.HasValue)
        result.CommandText += " AND PLAYLIST_ID=@PLAYLIST_ID";
      if (!string.IsNullOrEmpty(dataKey))
        result.CommandText += " AND DATA_KEY=@DATA_KEY";

      ISQLDatabase database = transaction.Database;
      database.AddParameter(result, "PROFILE_ID", profileId, typeof(Guid));

      if (playlistId.HasValue)
        database.AddParameter(result, "PLAYLIST_ID", playlistId.Value, typeof(Guid));

      if (!string.IsNullOrEmpty(dataKey))
        database.AddParameter(result, "DATA_KEY", dataKey, typeof(string));

      return result;
    }

    // User media item data

    public static IDbCommand SelectUserMediaItemDataCommand(ITransaction transaction, Guid profileId, Guid mediaItemId, string dataKey, out int mediaItemDataIndex)
    {
      IDbCommand result = transaction.CreateCommand();
      result.CommandText = "SELECT MEDIA_ITEM_DATA FROM USER_MEDIA_ITEM_DATA WHERE PROFILE_ID=@PROFILE_ID AND MEDIA_ITEM_ID=@MEDIA_ITEM_ID";
      if (!string.IsNullOrEmpty(dataKey))
        result.CommandText += " AND DATA_KEY=@DATA_KEY";

      ISQLDatabase database = transaction.Database;
      database.AddParameter(result, "PROFILE_ID", profileId, typeof(Guid));
      database.AddParameter(result, "MEDIA_ITEM_ID", mediaItemId, typeof(Guid));

      if (!string.IsNullOrEmpty(dataKey))
        database.AddParameter(result, "DATA_KEY", dataKey, typeof(string));

      mediaItemDataIndex = 0;
      return result;
    }

    public static IDbCommand SelectAllUserMediaItemDataCommand(ITransaction transaction, Guid profileId, Guid mediaItemId, out int mediaItemDataKeyIndex, out int mediaItemDataIndex)
    {
      IDbCommand result = transaction.CreateCommand();
      result.CommandText = "SELECT DATA_KEY, MEDIA_ITEM_DATA FROM USER_MEDIA_ITEM_DATA WHERE PROFILE_ID=@PROFILE_ID AND MEDIA_ITEM_ID=@MEDIA_ITEM_ID";

      ISQLDatabase database = transaction.Database;
      database.AddParameter(result, "PROFILE_ID", profileId, typeof(Guid));
      database.AddParameter(result, "MEDIA_ITEM_ID", mediaItemId, typeof(Guid));

      mediaItemDataKeyIndex = 0;
      mediaItemDataIndex = 1;
      return result;
    }

    public static IDbCommand CreateUserMediaItemDataCommand(ITransaction transaction, Guid profileId, Guid mediaItemId, string dataKey, string mediaItemData)
    {
      IDbCommand result = transaction.CreateCommand();
      result.CommandText = "INSERT INTO USER_MEDIA_ITEM_DATA (PROFILE_ID, MEDIA_ITEM_ID, DATA_KEY, MEDIA_ITEM_DATA) VALUES (@PROFILE_ID, @MEDIA_ITEM_ID, @DATA_KEY, @MEDIA_ITEM_DATA)";
      ISQLDatabase database = transaction.Database;
      database.AddParameter(result, "PROFILE_ID", profileId, typeof(Guid));
      database.AddParameter(result, "MEDIA_ITEM_ID", mediaItemId, typeof(Guid));
      database.AddParameter(result, "DATA_KEY", dataKey, typeof(string));
      database.AddParameter(result, "MEDIA_ITEM_DATA", mediaItemData, typeof(string));
      return result;
    }

    public static IDbCommand CopyUserMediaItemDataCommand(ITransaction transaction, Guid profileId, Guid newProfileId)
    {
      IDbCommand result = transaction.CreateCommand();
      result.CommandText = "INSERT INTO USER_MEDIA_ITEM_DATA (PROFILE_ID, MEDIA_ITEM_ID, DATA_KEY, MEDIA_ITEM_DATA) " +
        "SELECT @NEW_PROFILE_ID, MEDIA_ITEM_ID, DATA_KEY, MEDIA_ITEM_DATA FROM USER_MEDIA_ITEM_DATA WHERE PROFILE_ID=@PROFILE_ID";
      ISQLDatabase database = transaction.Database;
      database.AddParameter(result, "PROFILE_ID", profileId, typeof(Guid));
      database.AddParameter(result, "NEW_PROFILE_ID", newProfileId, typeof(Guid));
      return result;
    }

    public static IDbCommand DeleteUserMediaItemDataCommand(ITransaction transaction, Guid profileId, Guid? mediaItemId, string dataKey)
    {
      IDbCommand result = transaction.CreateCommand();
      result.CommandText = "DELETE FROM USER_MEDIA_ITEM_DATA WHERE PROFILE_ID=@PROFILE_ID";
      if (mediaItemId.HasValue)
        result.CommandText += " AND MEDIA_ITEM_ID=@MEDIA_ITEM_ID";
      if (!string.IsNullOrEmpty(dataKey))
        result.CommandText += " AND DATA_KEY=@DATA_KEY";

      ISQLDatabase database = transaction.Database;
      database.AddParameter(result, "PROFILE_ID", profileId, typeof(Guid));

      if (mediaItemId.HasValue)
        database.AddParameter(result, "MEDIA_ITEM_ID", mediaItemId.Value, typeof(Guid));

      if (!string.IsNullOrEmpty(dataKey))
        database.AddParameter(result, "DATA_KEY", dataKey, typeof(string));

      return result;
    }

    // User additional data
    public static IDbCommand SelectUserAdditionalDataCommand(ITransaction transaction, Guid profileId, string dataKey, int dataNo, out int additionalDataIndex)
    {
      IDbCommand result = transaction.CreateCommand();
      result.CommandText = "SELECT ADDITIONAL_DATA FROM USER_ADDITIONAL_DATA WHERE PROFILE_ID=@PROFILE_ID AND DATA_KEY=@DATA_KEY AND DATA_NO=@DATA_NO";
      ISQLDatabase database = transaction.Database;
      database.AddParameter(result, "PROFILE_ID", profileId, typeof(Guid));
      database.AddParameter(result, "DATA_KEY", dataKey, typeof(string));
      database.AddParameter(result, "DATA_NO", dataNo, typeof(int));

      additionalDataIndex = 0;
      return result;
    }

    public static IDbCommand SelectUserAdditionalDataListCommand(ITransaction transaction, Guid profileId, string dataKey, bool sortByKey, SortDirection sortDirection,
      out int dataNoIndex, out int additionalDataIndex)
    {
      IDbCommand result = transaction.CreateCommand();
      ISQLDatabase database = transaction.Database;
      result.CommandText = "SELECT DATA_NO, ADDITIONAL_DATA FROM USER_ADDITIONAL_DATA WHERE PROFILE_ID=@PROFILE_ID AND DATA_KEY=@DATA_KEY";
      result.CommandText += sortByKey ? " ORDER BY ADDITIONAL_DATA" : " ORDER BY DATA_NO";
      result.CommandText += sortDirection == SortDirection.Descending ? " DESC" : " ASC";

      database.AddParameter(result, "PROFILE_ID", profileId, typeof(Guid));
      database.AddParameter(result, "DATA_KEY", dataKey, typeof(string));

      dataNoIndex = 0;
      additionalDataIndex = 1;
      return result;
    }

    public static IDbCommand SelectUserAdditionalDataListCommand(ITransaction transaction, Guid profileId, string[] dataKeys, bool sortByKey, SortDirection sortDirection,
      out int additionalDataKeyIndex, out int dataNoIndex, out int additionalDataIndex)
    {
      IDbCommand result = transaction.CreateCommand();
      ISQLDatabase database = transaction.Database;
      result.CommandText = @"SELECT DATA_KEY, DATA_NO, ADDITIONAL_DATA FROM USER_ADDITIONAL_DATA WHERE PROFILE_ID=@PROFILE_ID";
      if(dataKeys != null && dataKeys.Length > 0)
        result.CommandText += " AND DATA_KEY IN ('" + string.Join("','", dataKeys) + "')";
      result.CommandText += sortByKey ? " ORDER BY DATA_KEY, ADDITIONAL_DATA" : " ORDER BY DATA_KEY, DATA_NO";
      result.CommandText += sortDirection == SortDirection.Descending ? " DESC" : " ASC";

      database.AddParameter(result, "PROFILE_ID", profileId, typeof(Guid));

      additionalDataKeyIndex = 0;
      dataNoIndex = 1;
      additionalDataIndex = 2;
      return result;
    }

    public static IDbCommand CreateUserAdditionalDataCommand(ITransaction transaction, Guid profileId, string dataKey, int dataNo, string additionalData)
    {
      IDbCommand result = transaction.CreateCommand();
      result.CommandText = "INSERT INTO USER_ADDITIONAL_DATA (PROFILE_ID, DATA_KEY, DATA_NO, ADDITIONAL_DATA) VALUES (@PROFILE_ID, @DATA_KEY, @DATA_NO, @ADDITIONAL_DATA)";
      ISQLDatabase database = transaction.Database;
      database.AddParameter(result, "PROFILE_ID", profileId, typeof(Guid));
      database.AddParameter(result, "DATA_KEY", dataKey, typeof(string));
      database.AddParameter(result, "DATA_NO", dataNo, typeof(int));
      database.AddParameter(result, "ADDITIONAL_DATA", additionalData, typeof(string));
      return result;
    }

    public static IDbCommand CopyUserAdditionalDataCommand(ITransaction transaction, Guid profileId, Guid newProfileId)
    {
      IDbCommand result = transaction.CreateCommand();
      result.CommandText = "INSERT INTO USER_ADDITIONAL_DATA (PROFILE_ID, DATA_KEY, DATA_NO, ADDITIONAL_DATA) " +
        "SELECT @NEW_PROFILE_ID, DATA_KEY, DATA_NO, ADDITIONAL_DATA FROM USER_ADDITIONAL_DATA WHERE PROFILE_ID=@PROFILE_ID";
      ISQLDatabase database = transaction.Database;
      database.AddParameter(result, "PROFILE_ID", profileId, typeof(Guid));
      database.AddParameter(result, "NEW_PROFILE_ID", newProfileId, typeof(Guid));
      return result;
    }

    public static IDbCommand DeleteUserAdditionalDataCommand(ITransaction transaction, Guid profileId, int? dataNo, string dataKey)
    {
      IDbCommand result = transaction.CreateCommand();
      result.CommandText = "DELETE FROM USER_ADDITIONAL_DATA WHERE PROFILE_ID=@PROFILE_ID";
      if (dataNo.HasValue)
        result.CommandText += " AND DATA_NO=@DATA_NO";
      if (!string.IsNullOrEmpty(dataKey))
        result.CommandText += " AND DATA_KEY=@DATA_KEY";

      ISQLDatabase database = transaction.Database;
      database.AddParameter(result, "PROFILE_ID", profileId, typeof(Guid));

      if (dataNo.HasValue)
        database.AddParameter(result, "DATA_NO", dataNo.Value, typeof(int));

      if (!string.IsNullOrEmpty(dataKey))
        database.AddParameter(result, "DATA_KEY", dataKey, typeof(string));

      return result;
    }
  }
}
