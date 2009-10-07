#region Copyright (C) 2007-2008 Team MediaPortal

/*
    Copyright (C) 2007-2008 Team MediaPortal
    http://www.team-mediaportal.com
 
    This file is part of MediaPortal II

    MediaPortal II is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    MediaPortal II is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with MediaPortal II.  If not, see <http://www.gnu.org/licenses/>.
*/

#endregion

using System;
using System.Collections.Generic;
using System.Data;
using MediaPortal.Core;
using MediaPortal.Core.General;
using MediaPortal.Core.PathManager;
using MediaPortal.Database;
using MediaPortal.Utilities;

namespace MediaPortal.Services.MediaLibrary
{
  /// <summary>
  /// Creates SQL commands for the communication with the MEDIALIBRARY subschema.
  /// </summary>
  public class MediaLibrary_SubSchema
  {
    #region Consts

    public const string SUBSCHEMA_NAME = "MediaLibrary";

    public const int EXPECTED_SCHEMA_VERSION_MAJOR = 1;
    public const int EXPECTED_SCHEMA_VERSION_MINOR = 0;

    internal const string MEDIA_ITEMS_TABLE_NAME = "MEDIA_ITEMS";
    internal const string MEDIA_ITEMS_ITEM_ID_COL_NAME = "MEDIA_ITEM_ID";

    #endregion

    public static string SubSchemaScriptDirectory
    {
      get
      {
        IPathManager pathManager = ServiceScope.Get<IPathManager>();
        return pathManager.GetPath(@"<APPLICATION_ROOT>\Base\Scripts\");
      }
    }

    public static IDbCommand SelectMediaItemAspectMetadataByIdCommand(ITransaction transaction, Guid miamId,
        out int nameIndex, out int serializationIndex)
    {
      IDbCommand result = transaction.CreateCommand();
      result.CommandText = "SELECT NAME, MIAM_SERIALIZATION FROM MEDIA_ITEM_ASPECT_METADATA WHERE MIAM_ID=?";

      IDbDataParameter param = result.CreateParameter();
      param.Value = miamId.ToString();
      result.Parameters.Add(param);

      nameIndex = 0;
      serializationIndex = 1;
      return result;
    }

    public static IDbCommand SelectAllMediaItemAspectMetadataCommand(ITransaction transaction,
        out int miamIdIndex, out int serializationsIndex)
    {
      IDbCommand result = transaction.CreateCommand();
      result.CommandText = "SELECT MIAM_ID, MIAM_SERIALIZATION FROM MEDIA_ITEM_ASPECT_METADATA";

      miamIdIndex = 0;
      serializationsIndex = 1;
      return result;
    }

    public static IDbCommand CreateMediaItemAspectMetadataCommand(ITransaction transaction, Guid id,
        string name, string serialization)
    {
      IDbCommand result = transaction.CreateCommand();
      result.CommandText = "INSERT INTO MEDIA_ITEM_ASPECT_METADATA (MIAM_ID, NAME, MIAM_SERIALIZATION) VALUES (?, ?, ?)";

      IDbDataParameter param = result.CreateParameter();
      param.Value = id.ToString();
      result.Parameters.Add(param);

      param = result.CreateParameter();
      param.Value = name;
      result.Parameters.Add(param);

      param = result.CreateParameter();
      param.Value = serialization;
      result.Parameters.Add(param);

      return result;
    }

    public static IDbCommand DeleteMediaItemAspectMetadataCommand(ITransaction transaction, Guid miamId)
    {
      IDbCommand result = transaction.CreateCommand();
      result.CommandText = "DELETE FROM MEDIA_ITEM_ASPECT_METADATA WHERE MIAM_ID=?";

      IDbDataParameter param = result.CreateParameter();
      param.Value = miamId.ToString();
      result.Parameters.Add(param);

      return result;
    }

    public static IDbCommand SelectShareIdCommand(ITransaction transaction,
        SystemName nativeSystem, Guid providerId, string path, out int shareIdIndex)
    {
      IDbCommand result = transaction.CreateCommand();
      result.CommandText = "SELECT SHARE_ID FROM SHARES WHERE SYSTEM_NAME=? AND MEDIA_PROVIDER_ID=? AND MEDIAPROVIDER_PATH=?";

      IDbDataParameter param = result.CreateParameter();
      param.Value = nativeSystem.HostName;
      result.Parameters.Add(param);

      param = result.CreateParameter();
      param.Value = providerId.ToString();
      result.Parameters.Add(param);

      param = result.CreateParameter();
      param.Value = path;
      result.Parameters.Add(param);

      shareIdIndex = 0;
      return result;
    }

    public static IDbCommand SelectSharesCommand(ITransaction transaction, out int shareIdIndex, out int nativeSystemIndex,
        out int providerIdIndex, out int pathIndex, out int shareNameIndex, out int isOnlineIndex)
    {
      IDbCommand result = transaction.CreateCommand();
      result.CommandText = "SELECT (SHARE_ID, SYSTEM_NAME, MEDIAPROVIDER_ID, MEDIAPROVIDER_PATH, NAME, IS_ONLINE) FROM SHARES";

      shareIdIndex = 0;
      nativeSystemIndex = 1;
      providerIdIndex = 2;
      pathIndex = 3;
      shareNameIndex = 4;
      isOnlineIndex = 5;
      return result;
    }

    public static IDbCommand SelectShareByIdCommand(ITransaction transaction, Guid shareId, out int nativeSystemIndex,
        out int providerIdIndex, out int pathIndex, out int shareNameIndex)
    {
      IDbCommand result = transaction.CreateCommand();
      result.CommandText = "SELECT (SYSTEM_NAME, MEDIAPROVIDER_ID, MEDIAPROVIDER_PATH, NAME) FROM SHARES WHERE SHARE_ID=?";

      IDbDataParameter param = result.CreateParameter();
      param.Value = shareId.ToString();
      result.Parameters.Add(param);

      nativeSystemIndex = 0;
      providerIdIndex = 1;
      pathIndex = 2;
      shareNameIndex = 3;
      return result;
    }

    public static IDbCommand SelectSharesByNativeSystemCommand(ITransaction transaction, SystemName nativeSystem,
        out int shareIdIndex, out int nativeSystemIndex, out int providerIdIndex, out int pathIndex,
        out int shareNameIndex, out int isOnlineIndex)
    {
      IDbCommand result = transaction.CreateCommand();
      result.CommandText = "SELECT (SHARE_ID, SYSTEM_NAME, MEDIAPROVIDER_ID, MEDIAPROVIDER_PATH, NAME, IS_ONLINE) FROM SHARES WHERE SYSTEM_NAME=?";

      IDbDataParameter param = result.CreateParameter();
      param.Value = nativeSystem.HostName;
      result.Parameters.Add(param);

      shareIdIndex = 0;
      nativeSystemIndex = 1;
      providerIdIndex = 2;
      pathIndex = 3;
      shareNameIndex = 4;
      isOnlineIndex = 5;
      return result;
    }

    public static IDbCommand InsertShareCommand(ITransaction transaction, Guid shareId, SystemName nativeSystem,
        Guid providerId, string path, string shareName, bool isOnline)
    {
      IDbCommand result = transaction.CreateCommand();
      result.CommandText = "INSERT INTO SHARES (SHARE_ID, NAME, SYSTEM_NAME, MEDIAPROVIDER_ID, MEDIAPROVIDER_PATH, IS_ONLINE) VALUES (?, ?, ?, ?, ?, ?)";

      IDbDataParameter param = result.CreateParameter();
      param.Value = shareId.ToString();
      result.Parameters.Add(param);

      param = result.CreateParameter();
      param.Value = shareName;
      result.Parameters.Add(param);

      param = result.CreateParameter();
      param.Value = nativeSystem.HostName;
      result.Parameters.Add(param);

      param = result.CreateParameter();
      param.Value = providerId.ToString();
      result.Parameters.Add(param);

      param = result.CreateParameter();
      param.Value = path;
      result.Parameters.Add(param);

      param = result.CreateParameter();
      param.Value = isOnline;
      result.Parameters.Add(param);

      return result;
    }

    public static IDbCommand SelectShareCategoriesCommand(ITransaction transaction, Guid shareId, out int categoryIndex)
    {
      IDbCommand result = transaction.CreateCommand();
      result.CommandText = "SELECT CATEGORYNAME FROM SHARES_CATEGORIES WHERE SHARE_ID=?";

      IDbDataParameter param = result.CreateParameter();
      param.Value = shareId.ToString();
      result.Parameters.Add(param);

      categoryIndex = 0;
      return result;
    }

    public static IDbCommand InsertShareCategoryCommand(ITransaction transaction, Guid shareId, string mediaCategory)
    {
      IDbCommand result = transaction.CreateCommand();
      result.CommandText = "INSERT INTO SHARES_CATEGORIES (SHARE_ID, CATEGORYNAME) VALUES (?, ?)";

      IDbDataParameter param = result.CreateParameter();
      param.Value = shareId.ToString();
      result.Parameters.Add(param);

      param = result.CreateParameter();
      param.Value = mediaCategory;
      result.Parameters.Add(param);

      return result;
    }

    public static IDbCommand DeleteShareCategoryCommand(ITransaction transaction, Guid shareId, string mediaCategory)
    {
      IDbCommand result = transaction.CreateCommand();
      result.CommandText = "DELETE FROM SHARES_CATEGORIES WHERE SHARE_ID=? AND CATEGORYNAME=?";

      IDbDataParameter param = result.CreateParameter();
      param.Value = shareId.ToString();
      result.Parameters.Add(param);

      param = result.CreateParameter();
      param.Value = mediaCategory;
      result.Parameters.Add(param);

      return result;
    }

    public static IDbCommand SelectShareMetadataExtractorsCommand(ITransaction transaction, Guid shareId,
        out int metadataExtractorIdIndex)
    {
      IDbCommand result = transaction.CreateCommand();
      result.CommandText = "SELECT METADATAEXTRACTOR_ID FROM SHARES_METADATAEXTRACTORS WHERE SHARE_ID=?";

      IDbDataParameter param = result.CreateParameter();
      param.Value = shareId.ToString();
      result.Parameters.Add(param);

      metadataExtractorIdIndex = 0;
      return result;
    }

    public static IDbCommand InsertShareMetadataExtractorCommand(ITransaction transaction, Guid shareId, Guid metadataExtractorId)
    {
      IDbCommand result = transaction.CreateCommand();
      result.CommandText = "INSERT INTO SHARES_METADATAEXTRACTORS (SHARE_ID, METADATAEXTRACTOR_ID) VALUES (?, ?)";

      IDbDataParameter param = result.CreateParameter();
      param.Value = shareId.ToString();
      result.Parameters.Add(param);

      param = result.CreateParameter();
      param.Value = metadataExtractorId.ToString();
      result.Parameters.Add(param);

      return result;
    }

    public static IDbCommand DeleteShareMetadataExtractorCommand(ITransaction transaction, Guid shareId, Guid metadataExtractorId)
    {
      IDbCommand result = transaction.CreateCommand();
      result.CommandText = "DELETE FROM SHARES_METADATAEXTRACTORS WHERE SHARE_ID=? AND METADATAEXTRACTOR_ID=?";

      IDbDataParameter param = result.CreateParameter();
      param.Value = shareId.ToString();
      result.Parameters.Add(param);

      param = result.CreateParameter();
      param.Value = metadataExtractorId.ToString();
      result.Parameters.Add(param);

      return result;
    }

    public static IDbCommand UpdateShareCommand(ITransaction transaction, Guid shareId, SystemName nativeSystem,
        Guid providerId, string path, string shareName)
    {
      IDbCommand result = transaction.CreateCommand();
      result.CommandText = "UPDATE SHARES set NAME=?, SYSTEM_NAME=?, MEDIAPROVIDER_ID=?, MEDIAPROVIDER_PATH=? WHERE SHARE_ID=?";

      IDbDataParameter param = result.CreateParameter();
      param.Value = shareName;
      result.Parameters.Add(param);

      param = result.CreateParameter();
      param.Value = nativeSystem.HostName;
      result.Parameters.Add(param);

      param = result.CreateParameter();
      param.Value = providerId.ToString();
      result.Parameters.Add(param);

      param = result.CreateParameter();
      param.Value = path;
      result.Parameters.Add(param);

      param = result.CreateParameter();
      param.Value = shareId.ToString();
      result.Parameters.Add(param);

      return result;
    }

    public static IDbCommand DeleteShareCommand(ITransaction transaction, Guid shareId)
    {
      IDbCommand result = transaction.CreateCommand();
      result.CommandText = "DELETE FROM SHARES WHERE SHARE_ID=?";

      IDbDataParameter param = result.CreateParameter();
      param.Value = shareId.ToString();
      result.Parameters.Add(param);

      return result;
    }

    public static IDbCommand SetSharesConnectionStateCommand(ITransaction transaction, IEnumerable<Guid> shareIds,
        bool connectionState)
    {
      IDbCommand result = transaction.CreateCommand();

      IDbDataParameter param = result.CreateParameter();
      param.Value = connectionState;
      result.Parameters.Add(param);

      ICollection<string> placeholders = new List<string>();
      foreach (Guid shareId in shareIds)
      {
        param = result.CreateParameter();
        param.Value = shareId.ToString();
        result.Parameters.Add(param);

        placeholders.Add("?");
      }
      result.CommandText = "UPDATE SHARES SET IS_ONLINE = ? WHERE SHARE_ID IN (" + StringUtils.Join(",", placeholders) + ")";

      return result;
    }
  }
}
