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
using System.Data;
using MediaPortal.Common;
using MediaPortal.Common.PathManager;
using MediaPortal.Backend.Database;
using MediaPortal.Utilities;

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
    public const int EXPECTED_SCHEMA_VERSION_MINOR = 0;

    internal const string USER_MEDIA_ITEM_DATA_TABLE_NAME = "USER_MEDIA_ITEM_DATA";
    internal const string USER_PROFILE_ID_COL_NAME = "PROFILE_ID";
    internal const string USER_DATA_KEY_COL_NAME = "DATA_KEY";
    internal const string USER_DATA_VALUE_COL_NAME = "MEDIA_ITEM_DATA";

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
        out int profileIdIndex, out int nameIndex)
    {
      ISQLDatabase database = transaction.Database;
      IDbCommand result = transaction.CreateCommand();
      result.CommandText = "SELECT PROFILE_ID, NAME FROM USER_PROFILES";

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
      return result;
    }

    public static IDbCommand CreateUserProfileCommand(ITransaction transaction, Guid profileId, string name)
    {
      IDbCommand result = transaction.CreateCommand();
      result.CommandText = "INSERT INTO USER_PROFILES (PROFILE_ID, NAME) VALUES (@PROFILE_ID, @NAME)";
      ISQLDatabase database = transaction.Database;
      database.AddParameter(result, "PROFILE_ID", profileId, typeof(Guid));
      database.AddParameter(result, "NAME", name, typeof(string));
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

    public static IDbCommand SelectUserAdditionalDataCommand(ITransaction transaction, Guid profileId, string dataKey, out int additionalDataIndex)
    {
      IDbCommand result = transaction.CreateCommand();
      result.CommandText = "SELECT ADDITIONAL_DATA FROM USER_ADDITIONAL_DATA WHERE PROFILE_ID=@PROFILE_ID AND DATA_KEY=@DATA_KEY";
      ISQLDatabase database = transaction.Database;
      database.AddParameter(result, "PROFILE_ID", profileId, typeof(Guid));
      database.AddParameter(result, "DATA_KEY", dataKey, typeof(string));

      additionalDataIndex = 0;
      return result;
    }

    public static IDbCommand CreateUserAdditionalDataCommand(ITransaction transaction, Guid profileId, string dataKey, string additionalData)
    {
      IDbCommand result = transaction.CreateCommand();
      result.CommandText = "INSERT INTO USER_ADDITIONAL_DATA (PROFILE_ID, DATA_KEY, ADDITIONAL_DATA) VALUES (@PROFILE_ID, @DATA_KEY, @ADDITIONAL_DATA)";
      ISQLDatabase database = transaction.Database;
      database.AddParameter(result, "PROFILE_ID", profileId, typeof(Guid));
      database.AddParameter(result, "DATA_KEY", dataKey, typeof(string));
      database.AddParameter(result, "ADDITIONAL_DATA", additionalData, typeof(string));
      return result;
    }

    public static IDbCommand DeleteUserAdditionalDataCommand(ITransaction transaction, Guid profileId, string dataKey)
    {
      IDbCommand result = transaction.CreateCommand();
      result.CommandText = "DELETE FROM USER_ADDITIONAL_DATA WHERE PROFILE_ID=@PROFILE_ID";
      if (!string.IsNullOrEmpty(dataKey))
        result.CommandText += " AND DATA_KEY=@DATA_KEY";

      ISQLDatabase database = transaction.Database;
      database.AddParameter(result, "PROFILE_ID", profileId, typeof(Guid));

      if (!string.IsNullOrEmpty(dataKey))
        database.AddParameter(result, "DATA_KEY", dataKey, typeof(string));

      return result;
    }
  }
}
