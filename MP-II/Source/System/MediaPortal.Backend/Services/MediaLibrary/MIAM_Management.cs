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
using System.Text;
using MediaPortal.Core;
using MediaPortal.Core.MediaManagement;
using MediaPortal.Database;
using MediaPortal.Services.Database;
using MediaPortal.Utilities;
using MediaPortal.Utilities.Exceptions;

namespace MediaPortal.Services.MediaLibrary
{
  // TODO: Preparation of some SQL statements? We could use a lazy initialized DBCommand cache which prepares DBCommands
  // on the fly and holds up to N prepared commands.
  public class MIAM_Management
  {
    internal const string MIAM_MEDIA_ITEM_ID_COL_NAME = "MEDIA_ITEM_ID";
    internal const string COLL_MIAM_VALUE_COL_NAME = "VALUE";

    internal static string GetMIAMTableName(MediaItemAspectMetadata miam)
    {
      return "MIAM_" + SqlUtils.ToSQLIdentifier(miam.AspectId.ToString());
    }

    internal static string GetMIAMAttributeColumnName(string attributeName)
    {
      return SqlUtils.ToSQLIdentifier(attributeName);
    }

    internal static string GetMIAMCollectionAttributeTableName(
        MediaItemAspectMetadata.AttributeSpecification spec)
    {
      return GetMIAMTableName(spec.ParentMIAM) + "_" + SqlUtils.ToSQLIdentifier(spec.AttributeName);
    }

    public static bool GetMediaItemAspectMetadata(Guid aspectId, out string name, out string serialization)
    {
      ISQLDatabase database = ServiceScope.Get<ISQLDatabase>();
      ITransaction transaction = database.BeginTransaction();
      try
      {
        int nameIndex;
        int serializationIndex;
        IDbCommand command = MediaLibrary_SubSchema.SelectMediaItemAspectMetadataByIdCommand(transaction, aspectId,
            out nameIndex, out serializationIndex);
        IDataReader reader = command.ExecuteReader();
        try
        {
          if (reader.Read())
          {
            name = reader.GetString(nameIndex);
            serialization = reader.GetString(serializationIndex);
            return true;
          }
          name = null;
          serialization = null;
          return false;
        }
        finally
        {
          reader.Close();
        }
      }
      finally
      {
        transaction.Dispose();
      }
    }

    public static IDictionary<Guid, string> GetAllMediaItemAspectMetadata()
    {
      ISQLDatabase database = ServiceScope.Get<ISQLDatabase>();
      ITransaction transaction = database.BeginTransaction();
      try
      {
        int miamIdIndex;
        int serializationsIndex;
        IDbCommand command = MediaLibrary_SubSchema.SelectAllMediaItemAspectMetadataCommand(transaction, out miamIdIndex, out serializationsIndex);
        IDataReader reader = command.ExecuteReader();
        try
        {
          IDictionary<Guid, string> result = new Dictionary<Guid, string>();
          while (reader.Read())
            result.Add(new Guid(reader.GetString(miamIdIndex)), reader.GetString(serializationsIndex));
          return result;
        }
        finally
        {
          reader.Close();
        }
      }
      finally
      {
        transaction.Dispose();
      }
    }

    public static bool MediaItemAspectStorageExists(Guid aspectId)
    {
      string name;
      string serialization;
      return GetMediaItemAspectMetadata(aspectId, out name, out serialization);
    }

    public static MediaItemAspectMetadata GetMediaItemAspectMetadata(Guid aspectId)
    {
      string name;
      string serialization;
      if (!GetMediaItemAspectMetadata(aspectId, out name, out serialization))
        throw new InvalidDataException("The requested MediaItemAspectMetadata of id '{0}' is unknown", aspectId);
      return MediaItemAspectMetadata.Deserialize(serialization);
    }

    public static void AddMediaItemAspectStorage(MediaItemAspectMetadata miam)
    {
      ISQLDatabase database = ServiceScope.Get<ISQLDatabase>();
      ITransaction transaction = database.BeginTransaction();
      try
      {
        IDbCommand command = transaction.CreateCommand();
        string miamTableName = GetMIAMTableName(miam);
        StringBuilder sb = new StringBuilder("CREATE TABLE " + miamTableName + " (" +
            MIAM_MEDIA_ITEM_ID_COL_NAME + " " + database.GetSQLType(typeof(Int64)) + ",");
        IList<string> terms = new List<string>();
        foreach (MediaItemAspectMetadata.AttributeSpecification spec in miam.AttributeSpecifications)
        {
          string attrName = GetMIAMAttributeColumnName(spec.AttributeName);
          string sqlType = spec.AttributeType == typeof(string) ? database.GetSQLUnicodeStringType(spec.MaxNumChars) :
              database.GetSQLType(spec.AttributeType);
          switch (spec.Cardinality)
          {
            case Cardinality.Inline:
              terms.Add(attrName + " " + sqlType);
              break;
            case Cardinality.OneToMany:
            case Cardinality.ManyToOne:
            case Cardinality.ManyToMany:
              command.CommandText = "CREATE TABLE " + GetMIAMCollectionAttributeTableName(spec) + " (" +
                  MIAM_MEDIA_ITEM_ID_COL_NAME + " " + database.GetSQLType(typeof(Int64)) +
                  COLL_MIAM_VALUE_COL_NAME + " " + sqlType +
                  "CONSTRAINT " + GetMIAMCollectionAttributeTableName(spec) + "_PK PRIMARY KEY (" + MIAM_MEDIA_ITEM_ID_COL_NAME + ")," +
                  "CONSTRAINT " + GetMIAMCollectionAttributeTableName(spec) + "_MEDIA_ITEM_FK" +
                  " FOREIGN KEY (" + MIAM_MEDIA_ITEM_ID_COL_NAME + ")" +
                  " REFERENCES " + MediaLibrary_SubSchema.MEDIA_ITEMS_TABLE_NAME + " (" + MediaLibrary_SubSchema.MEDIA_ITEMS_ITEM_ID_COL_NAME + ") ON DELETE CASCADE" +
                  ")";
              command.ExecuteNonQuery();
              break;
            default:
              throw new NotImplementedException(string.Format("Cardinality '{0}' for attribute '{1}.{2}' is not implemented",
                  spec.Cardinality, miam.AspectId, spec.AttributeName));
          }
        }
        sb.Append(
            "CONSTRAINT " + miamTableName + "_PK PRIMARY KEY (" + MIAM_MEDIA_ITEM_ID_COL_NAME + ")," +
            "CONSTRAINT " + miamTableName + "_MEDIA_ITEMS_FK" +
            " FOREIGN KEY (" + MIAM_MEDIA_ITEM_ID_COL_NAME + ") REFERENCES " +
                MediaLibrary_SubSchema.MEDIA_ITEMS_TABLE_NAME + " (" + MediaLibrary_SubSchema.MEDIA_ITEMS_ITEM_ID_COL_NAME + ") ON DELETE CASCADE");
        sb.Append(StringUtils.Join(", ", terms));
        sb.Append(")");
        command.CommandText = sb.ToString();
        command.ExecuteNonQuery();

        // Register metadata
        command = MediaLibrary_SubSchema.CreateMediaItemAspectMetadataCommand(transaction, miam.AspectId, miam.Name, miam.Serialize());
        command.ExecuteNonQuery();
        transaction.Commit();
      }
      catch (Exception)
      {
        transaction.Rollback();
        throw;
      }
    }

    public static void RemoveMediaItemAspectStorage(Guid aspectId)
    {
      MediaItemAspectMetadata miam = GetMediaItemAspectMetadata(aspectId);
      ISQLDatabase database = ServiceScope.Get<ISQLDatabase>();
      ITransaction transaction = database.BeginTransaction();
      try
      {
        IDbCommand command = transaction.CreateCommand();
        command.CommandText = "DROP TABLE " + GetMIAMTableName(miam);
        command.ExecuteNonQuery();
        foreach (MediaItemAspectMetadata.AttributeSpecification spec in miam.AttributeSpecifications)
        {
          switch (spec.Cardinality)
          {
            case Cardinality.Inline:
              break;
            case Cardinality.OneToMany:
            case Cardinality.ManyToOne:
            case Cardinality.ManyToMany:
              command.CommandText = "DROP TABLE " + GetMIAMCollectionAttributeTableName(spec);
              command.ExecuteNonQuery();
              break;
            default:
              throw new NotImplementedException(string.Format("Attribute '{0}.{1}': Cardinality '{2}' is not implemented",
                  aspectId, spec.AttributeName, spec.Cardinality));
          }
        }
        // Unregister metadata
        command = MediaLibrary_SubSchema.DeleteMediaItemAspectMetadataCommand(transaction, aspectId);
        command.ExecuteNonQuery();
        transaction.Commit();
      }
      catch (Exception)
      {
        transaction.Rollback();
        throw;
      }
    }

    public static ICollection<MediaItemAspectMetadata> GetManagedMediaItemAspectMetadata()
    {
      ICollection<string> miamSerializations = GetAllMediaItemAspectMetadata().Values;
      IList<MediaItemAspectMetadata> result = new List<MediaItemAspectMetadata>(miamSerializations.Count);
      foreach (string serialization in miamSerializations)
        result.Add(MediaItemAspectMetadata.Deserialize(serialization));
      return result;
    }

    public static MediaItemAspectMetadata GetManagedMediaItemAspectMetadata(Guid aspectId)
    {
      string name;
      string serialization;
      if (GetMediaItemAspectMetadata(aspectId, out name, out serialization))
        return MediaItemAspectMetadata.Deserialize(serialization);
      else
        return null;
    }
  }
}
