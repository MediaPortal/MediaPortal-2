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
using MediaPortal.Core.Logging;
using MediaPortal.Core.MediaManagement;
using MediaPortal.Database;
using MediaPortal.Services.Database;
using MediaPortal.Utilities;
using MediaPortal.Utilities.Exceptions;

namespace MediaPortal.Services.MediaLibrary
{
  /// <summary>
  /// Management class for media item aspect types.
  /// </summary>
  public class MIAM_Management
  {
    protected internal const string MIAM_MEDIA_ITEM_ID_COL_NAME = "MEDIA_ITEM_ID";
    protected internal const string COLL_MIAM_VALUE_COL_NAME = "VALUE";

    protected IDictionary<string, string> _nameAliases = new Dictionary<string, string>();
    protected IDictionary<Guid, MediaItemAspectMetadata> _managedMIAMs =
        new Dictionary<Guid, MediaItemAspectMetadata>();
    protected object _syncObj = new object();

    public MIAM_Management()
    {
      ReloadAliasCache();
      ReloadMIATypeCache();
    }

    #region Table name generation and alias management

    protected void ReloadAliasCache()
    {
      lock (_syncObj)
      {
        ISQLDatabase database = ServiceScope.Get<ISQLDatabase>();
        ITransaction transaction = database.BeginTransaction();
        try
        {
          int miamIdIndex;
          int identifierIndex;
          int dbObjectNameIndex;
          IDbCommand command = MediaLibrary_SubSchema.SelectMIANameAliasesCommand(transaction, out miamIdIndex,
              out identifierIndex, out dbObjectNameIndex);
          IDataReader reader = command.ExecuteReader();
          _nameAliases.Clear();
          try
          {
            while (reader.Read())
            {
              string identifier = reader.GetString(identifierIndex);
              string dbObjectName = reader.GetString(dbObjectNameIndex);
              _nameAliases.Add(identifier, dbObjectName);
            }
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
    }

    /// <summary>
    /// Gets a technical table identifier for the given MIAM.
    /// </summary>
    /// <param name="miam">MIAM to return the table identifier for.</param>
    /// <returns>Table identifier for the given MIAM. The returned identifier must be mapped to a shortened
    /// table name to be used in the DB.</returns>
    internal string GetMIAMTableIdentifier(MediaItemAspectMetadata miam)
    {
      return "MIAM_" + SqlUtils.ToSQLIdentifier(miam.AspectId.ToString());
    }

    /// <summary>
    /// Gets a technical column identifier for the given inline attribute specification.
    /// </summary>
    /// <param name="spec">Attribute specification to return the column identifier for.</param>
    /// <returns>Column identifier for the given attribute specification. The returned identifier must be
    /// shortened to match the maximum column name length.</returns>
    internal string GetMIAMAttributeColumnIdentifier(MediaItemAspectMetadata.AttributeSpecification spec)
    {
      return SqlUtils.ToSQLIdentifier(spec.AttributeName);
    }

    /// <summary>
    /// Gets a technical table identifier for the given MIAM collection attribute.
    /// </summary>
    /// <returns>Table identifier for the given collection attribute. The returned identifier must be mapped to a
    /// shortened table name to be used in the DB.</returns>
    internal string GetMIAMCollectionAttributeTableIdentifier(MediaItemAspectMetadata.AttributeSpecification spec)
    {
      return GetMIAMTableName(spec.ParentMIAM) + "_" + SqlUtils.ToSQLIdentifier(spec.AttributeName);
    }

    private string GetAliasMapping(string generatedName, string errorOnNotFound)
    {
      string result;
      lock (_syncObj)
        if (!_nameAliases.TryGetValue(generatedName, out result))
          throw new InvalidDataException(errorOnNotFound);
      return result;
    }

    /// <summary>
    /// Gets the actual table name for a MIAM table.
    /// </summary>
    /// <returns>Table name for the table containing the inline attributes of the specified <paramref name="miam"/>.</returns>
    internal string GetMIAMTableName(MediaItemAspectMetadata miam)
    {
      string identifier = GetMIAMTableIdentifier(miam);
      return GetAliasMapping(identifier, string.Format("MIAM '{0}' (id: '{1}') doesn't have a corresponding table name yet", miam.Name, miam.AspectId));
    }

    /// <summary>
    /// Gets the actual column name for a MIAM attribute specification.
    /// </summary>
    /// <returns>Column name for the column containing the inline attribute data of the specified attribute
    /// <paramref name="spec"/>.</returns>
    internal string GetMIAMAttributeColumnName(MediaItemAspectMetadata.AttributeSpecification spec)
    {
      string columnName = GetMIAMAttributeColumnIdentifier(spec);
      return GetClippedColumnName(columnName);
    }

    /// <summary>
    /// Gets the actual table name for a MIAM collection attribute table.
    /// </summary>
    /// <returns>Table name for the table containing the specified collection attribute.</returns>
    internal string GetMIAMCollectionAttributeTableName(MediaItemAspectMetadata.AttributeSpecification spec)
    {
      string identifier = GetMIAMCollectionAttributeTableIdentifier(spec);
      return GetAliasMapping(identifier, string.Format("Attribute '{0}' of MIAM '{1}' (id: '{2}') doesn't have a corresponding table name yet",
          spec, spec.ParentMIAM.Name, spec.ParentMIAM.AspectId));
    }

    private static string ConcatNameParts(string prefix, uint suffix, uint maxLen)
    {
      string suf = suffix.ToString();
      if (prefix.Length + suf.Length > maxLen)
        return (prefix + suf).Substring(0, (int) maxLen);
      else
        return prefix + suf;
    }

    /// <summary>
    /// Given a generated, technical, long table <paramref name="tableIdentifier"/>, this method calculates a
    /// table name which is unique among the generated tables. The returned table name will automatically be stored
    /// in the internal cache of table identifiers to table names mappings.
    /// </summary>
    /// <param name="transaction">Transaction to be used to add the specified name mapping to the DB.</param>
    /// <param name="aspectId">ID of the media item aspect type the given mapping belongs to.</param>
    /// <param name="tableIdentifier">Technical indentifier to be mapped to a table name for our DB.</param>
    /// <param name="desiredName">Root name to start the name generation.</param>
    /// <returns>Table name corresponding to the specified table identifier.</returns>
    private string GenerateDBTableName(ITransaction transaction, Guid aspectId, string tableIdentifier, string desiredName)
    {
      lock (_syncObj)
      {
        if (_nameAliases.ContainsKey(tableIdentifier))
          throw new InvalidDataException("Table identifier '{0}' is already present in alias cache", tableIdentifier);
        ISQLDatabase database = ServiceScope.Get<ISQLDatabase>();
        uint maxLen = database.MaxTableNameLength;
        uint ct = 0;
        string result;
        while (_nameAliases.ContainsKey(result = ConcatNameParts(desiredName, ct, maxLen)))
          ct++;
        AddNameAlias(transaction, aspectId, tableIdentifier, result);
        return result;
      }
    }

    protected void AddNameAlias(ITransaction transaction, Guid aspectId, string tableIdentifier, string dbObjectName)
    {
      IDbCommand command = MediaLibrary_SubSchema.CreateMIANameAliasCommand(transaction, aspectId, tableIdentifier, dbObjectName);
      command.ExecuteNonQuery();
      lock (_syncObj)
        _nameAliases[tableIdentifier] = dbObjectName;
    }

    private static string GetClippedColumnName(string columnName)
    {
      ISQLDatabase database = ServiceScope.Get<ISQLDatabase>();
      uint clipSize = database.MaxTableNameLength;
      if (columnName.Length > clipSize)
        return columnName.Substring(0, (int) clipSize);
      else
        return columnName;
    }

    internal string GenerateMIAMTableName(ITransaction transaction, MediaItemAspectMetadata miam)
    {
      string identifier = GetMIAMTableIdentifier(miam);
      return GenerateDBTableName(transaction, miam.AspectId, identifier, miam.Name);
    }

    internal string GenerateMIAMAttributeColumnName(MediaItemAspectMetadata.AttributeSpecification spec)
    {
      string columnName = GetMIAMAttributeColumnIdentifier(spec);
      return GetClippedColumnName(columnName);
    }

    internal string GenerateMIAMCollectionAttributeTableName(ITransaction transaction, MediaItemAspectMetadata.AttributeSpecification spec)
    {
      string identifier = GetMIAMCollectionAttributeTableIdentifier(spec);
      return GenerateDBTableName(transaction, spec.ParentMIAM.AspectId, identifier, spec.AttributeName);
    }

    #endregion

    protected static IDictionary<Guid, string> SelectAllMediaItemAspectMetadataSerializations()
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

    protected static ICollection<MediaItemAspectMetadata> SelectAllManagedMediaItemAspectMetadata()
    {
      ICollection<string> miamSerializations = SelectAllMediaItemAspectMetadataSerializations().Values;
      IList<MediaItemAspectMetadata> result = new List<MediaItemAspectMetadata>(miamSerializations.Count);
      foreach (string serialization in miamSerializations)
        result.Add(MediaItemAspectMetadata.Deserialize(serialization));
      return result;
    }

    protected void ReloadMIATypeCache()
    {
      lock (_syncObj)
      {
        foreach (MediaItemAspectMetadata miam in SelectAllManagedMediaItemAspectMetadata())
          _managedMIAMs[miam.AspectId] = miam;
      }
    }

    public IDictionary<Guid, MediaItemAspectMetadata> ManagedMediaItemAspectTypes
    {
      get
      {
        lock (_syncObj)
          return new Dictionary<Guid, MediaItemAspectMetadata>(_managedMIAMs);
      }
    }

    public bool MediaItemAspectStorageExists(Guid aspectId)
    {
      lock (_syncObj)
        return _managedMIAMs.ContainsKey(aspectId);
    }

    public MediaItemAspectMetadata GetMediaItemAspectMetadata(Guid aspectId)
    {
      MediaItemAspectMetadata result;
      lock (_syncObj)
        if (_managedMIAMs.TryGetValue(aspectId, out result))
          return result;
      return null;
    }

    public void AddMediaItemAspectStorage(MediaItemAspectMetadata miam)
    {
      ISQLDatabase database = ServiceScope.Get<ISQLDatabase>();
      ITransaction transaction = database.BeginTransaction();
      try
      {
        IDbCommand command = transaction.CreateCommand();
        string miamTableName = GenerateMIAMTableName(transaction, miam);
        StringBuilder sb = new StringBuilder("CREATE TABLE " + miamTableName + " (" +
            MIAM_MEDIA_ITEM_ID_COL_NAME + " " + database.GetSQLType(typeof(Int64)) + ",");
        IList<string> terms = new List<string>();
        foreach (MediaItemAspectMetadata.AttributeSpecification spec in miam.AttributeSpecifications)
        {
          string sqlType = spec.AttributeType == typeof(string) ? database.GetSQLVarLengthStringType(spec.MaxNumChars) :
              database.GetSQLType(spec.AttributeType);
          switch (spec.Cardinality)
          {
            case Cardinality.Inline:
              string attrName = GenerateMIAMAttributeColumnName(spec);
              terms.Add(attrName + " " + sqlType);
              break;
            case Cardinality.OneToMany:
            case Cardinality.ManyToOne:
            case Cardinality.ManyToMany:
              string collectionAttributeTableName = GenerateMIAMCollectionAttributeTableName(transaction, spec);
              command.CommandText = "CREATE TABLE " + collectionAttributeTableName + " (" +
                  MIAM_MEDIA_ITEM_ID_COL_NAME + " " + database.GetSQLType(typeof(Int64)) +
                  COLL_MIAM_VALUE_COL_NAME + " " + sqlType +
                  "CONSTRAINT " + collectionAttributeTableName + "_PK PRIMARY KEY (" + MIAM_MEDIA_ITEM_ID_COL_NAME + ")," +
                  "CONSTRAINT " + collectionAttributeTableName + "_MEDIA_ITEM_FK" +
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
        lock (_syncObj)
          _managedMIAMs.Add(miam.AspectId, miam);
      }
      catch (Exception e)
      {
        ServiceScope.Get<ILogger>().Error("MIAM_Management: Error adding media item aspect storage '{0}'", e, miam.AspectId);
        transaction.Rollback();
        throw;
      }
    }

    public void RemoveMediaItemAspectStorage(Guid aspectId)
    {
      MediaItemAspectMetadata miam = GetMediaItemAspectMetadata(aspectId);
      ISQLDatabase database = ServiceScope.Get<ISQLDatabase>();
      ITransaction transaction = database.BeginTransaction();
      try
      {
        lock (_syncObj)
          _managedMIAMs.Remove(aspectId);
        // We don't remove the name alias mappings from the alias cache because we simply reload the alias cache at the end.
        // We don't remove the name alias mappings from the name aliases table because they are deleted by the DB system
        // (ON DELETE CASCADE).
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
        ReloadAliasCache();
      }
      catch (Exception e)
      {
        ServiceScope.Get<ILogger>().Error("MIAM_Management: Error removing media item aspect storage '{0}'", e, aspectId);
        transaction.Rollback();
        throw;
      }
    }
  }
}
