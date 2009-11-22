#region Copyright (C) 2007-2009 Team MediaPortal

/*
    Copyright (C) 2007-2009 Team MediaPortal
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
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Text;
using MediaPortal.Core;
using MediaPortal.Core.Logging;
using MediaPortal.Core.MediaManagement;
using MediaPortal.Backend.Database;
using MediaPortal.Backend.Services.Database;
using MediaPortal.Backend.Services.MediaLibrary.QueryEngine;
using MediaPortal.Utilities;
using MediaPortal.Utilities.Exceptions;

namespace MediaPortal.Backend.Services.MediaLibrary
{
  /// <summary>
  /// Management class for media item aspect types and media item aspects.
  /// </summary>
  public class MIA_Management
  {
    /// <summary>
    /// ID column name which is both primary key and foreign key referencing the ID in the main media item's table.
    /// This column name is used both for each MIA main table and for MIA attribute collection tables.
    /// </summary>
    protected internal const string MIA_MEDIA_ITEM_ID_COL_NAME = "MEDIA_ITEM_ID";

    /// <summary>
    /// Value column name for MIA attribute collection tables.
    /// </summary>
    protected internal const string COLL_MIA_VALUE_COL_NAME = "ATTRIBUTE_VALUE";

    protected readonly IDictionary<string, string> _nameAliases = new Dictionary<string, string>();

    /// <summary>
    /// Caches all media item aspect types the database is able to store. A MIA type which is currently being
    /// added or removed will have a <c>null</c> value assigned to its ID.
    /// </summary>
    protected readonly IDictionary<Guid, MediaItemAspectMetadata> _managedMIATypes =
        new Dictionary<Guid, MediaItemAspectMetadata>();

    protected readonly object _syncObj = new object();

    public MIA_Management()
    {
      ReloadAliasCache();
      LoadMIATypeCache();
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
    /// <param name="miam">MIA type to return the table identifier for.</param>
    /// <returns>Table identifier for the given MIA type. The returned identifier must be mapped to a shortened
    /// table name to be used in the DB.</returns>
    internal string GetMIATableIdentifier(MediaItemAspectMetadata miam)
    {
      return "MIA_" + SqlUtils.ToSQLIdentifier(miam.AspectId.ToString()).ToUpperInvariant();
    }

    /// <summary>
    /// Gets a technical column identifier for the given inline attribute specification.
    /// </summary>
    /// <param name="spec">Attribute specification to return the column identifier for.</param>
    /// <returns>Column identifier for the given attribute specification. The returned identifier must be
    /// shortened to match the maximum column name length.</returns>
    internal string GetMIAAttributeColumnIdentifier(MediaItemAspectMetadata.AttributeSpecification spec)
    {
      return SqlUtils.ToSQLIdentifier(spec.AttributeName);
    }

    /// <summary>
    /// Gets a technical table identifier for the given MIAM collection attribute.
    /// </summary>
    /// <returns>Table identifier for the given collection attribute. The returned identifier must be mapped to a
    /// shortened table name to be used in the DB.</returns>
    internal string GetMIACollectionAttributeTableIdentifier(MediaItemAspectMetadata.AttributeSpecification spec)
    {
      return GetMIATableName(spec.ParentMIAM) + "_" + SqlUtils.ToSQLIdentifier(spec.AttributeName);
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
    internal string GetMIATableName(MediaItemAspectMetadata miam)
    {
      string identifier = GetMIATableIdentifier(miam);
      return GetAliasMapping(identifier, string.Format("MIAM '{0}' (id: '{1}') doesn't have a corresponding table name yet", miam.Name, miam.AspectId));
    }

    /// <summary>
    /// Gets the actual column name for a MIAM attribute specification.
    /// </summary>
    /// <returns>Column name for the column containing the inline attribute data of the specified attribute
    /// <paramref name="spec"/>.</returns>
    internal string GetMIAAttributeColumnName(MediaItemAspectMetadata.AttributeSpecification spec)
    {
      string columnName = GetMIAAttributeColumnIdentifier(spec);
      return GetClippedColumnName(columnName);
    }

    /// <summary>
    /// Gets the actual table name for a MIAM collection attribute table.
    /// </summary>
    /// <returns>Table name for the table containing the specified collection attribute.</returns>
    internal string GetMIACollectionAttributeTableName(MediaItemAspectMetadata.AttributeSpecification spec)
    {
      string identifier = GetMIACollectionAttributeTableIdentifier(spec);
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
    /// Given a generated, technical, long <paramref name="objectIdentifier"/>, this method calculates an
    /// object name which is unique among the generated database objects. The returned object name will automatically be stored
    /// in the internal cache of identifiers to object names mappings.
    /// </summary>
    /// <param name="transaction">Transaction to be used to add the specified name mapping to the DB.</param>
    /// <param name="aspectId">ID of the media item aspect type the given mapping belongs to.</param>
    /// <param name="objectIdentifier">Technical indentifier to be mapped to a table name for our DB.</param>
    /// <param name="desiredName">Root name to start the name generation.</param>
    /// <returns>Table name corresponding to the specified table identifier.</returns>
    private string GenerateDBObjectName(ITransaction transaction, Guid aspectId, string objectIdentifier, string desiredName)
    {
      lock (_syncObj)
      {
        if (_nameAliases.ContainsKey(objectIdentifier))
          throw new InvalidDataException("Table identifier '{0}' is already present in alias cache", objectIdentifier);
        ISQLDatabase database = ServiceScope.Get<ISQLDatabase>();
        uint maxLen = database.MaxTableNameLength;
        uint ct = 0;
        string result;
        desiredName = SqlUtils.ToSQLIdentifier(desiredName);
        if (!NamePresent(desiredName))
          result = desiredName;
        else
          while (NamePresent(result = ConcatNameParts(desiredName, ct, maxLen)))
            ct++;
        AddNameAlias(transaction, aspectId, objectIdentifier, result);
        return result;
      }
    }

    protected bool NamePresent(string dbObjectName)
    {
      foreach (string objectName in _nameAliases.Values)
        if (dbObjectName == objectName)
          return true;
      return false;
    }

    protected void AddNameAlias(ITransaction transaction, Guid aspectId, string objectIdentifier, string dbObjectName)
    {
      IDbCommand command = MediaLibrary_SubSchema.CreateMIANameAliasCommand(transaction, aspectId, objectIdentifier, dbObjectName);
      command.ExecuteNonQuery();
      lock (_syncObj)
        _nameAliases[objectIdentifier] = dbObjectName;
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

    internal string GenerateMIATableName(ITransaction transaction, MediaItemAspectMetadata miam)
    {
      string identifier = GetMIATableIdentifier(miam);
      return GenerateDBObjectName(transaction, miam.AspectId, identifier, miam.Name);
    }

    internal string GenerateMIAAttributeColumnName(MediaItemAspectMetadata.AttributeSpecification spec)
    {
      string columnName = GetMIAAttributeColumnIdentifier(spec);
      return GetClippedColumnName(columnName);
    }

    internal string GenerateMIACollectionAttributeTableName(ITransaction transaction, MediaItemAspectMetadata.AttributeSpecification spec)
    {
      string identifier = GetMIACollectionAttributeTableIdentifier(spec);
      return GenerateDBObjectName(transaction, spec.ParentMIAM.AspectId, identifier, spec.AttributeName);
    }

    #endregion

    #region Other protected & internal methods

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

    protected void LoadMIATypeCache()
    {
      lock (_syncObj)
      {
        foreach (MediaItemAspectMetadata miam in SelectAllManagedMediaItemAspectMetadata())
          _managedMIATypes[miam.AspectId] = miam;
      }
    }

    #endregion

    #region MIA storage management

    public IDictionary<Guid, MediaItemAspectMetadata> ManagedMediaItemAspectTypes
    {
      get
      {
        lock (_syncObj)
          return new Dictionary<Guid, MediaItemAspectMetadata>(_managedMIATypes);
      }
    }

    public bool MediaItemAspectStorageExists(Guid aspectId)
    {
      lock (_syncObj)
      {
        MediaItemAspectMetadata miam;
        return _managedMIATypes.TryGetValue(aspectId, out miam) && miam != null;
      }
    }

    public MediaItemAspectMetadata GetMediaItemAspectMetadata(Guid aspectId)
    {
      MediaItemAspectMetadata result;
      lock (_syncObj)
        if (_managedMIATypes.TryGetValue(aspectId, out result))
          return result;
      return null;
    }

    public void AddMediaItemAspectStorage(MediaItemAspectMetadata miam)
    {
      lock (_syncObj)
      {
        if (_managedMIATypes.ContainsKey(miam.AspectId))
          return;
        _managedMIATypes.Add(miam.AspectId, null);
      }
      ISQLDatabase database = ServiceScope.Get<ISQLDatabase>();
      ITransaction transaction = database.BeginTransaction();
      try
      {
        // Register metadata first - generated aliases will reference to the new MIA type row
        IDbCommand command = MediaLibrary_SubSchema.CreateMediaItemAspectMetadataCommand(transaction, miam.AspectId, miam.Name, miam.Serialize());
        command.ExecuteNonQuery();

        // Generate tables for new MIA type
        command = transaction.CreateCommand();
        string miaTableName = GenerateMIATableName(transaction, miam);
        StringBuilder mainStatementBuilder = new StringBuilder("CREATE TABLE " + miaTableName + " (" +
            MIA_MEDIA_ITEM_ID_COL_NAME + " " + database.GetSQLType(typeof(Int64)) + ",");
        IList<string> terms = new List<string>();
        foreach (MediaItemAspectMetadata.AttributeSpecification spec in miam.AttributeSpecifications.Values)
        {
          string sqlType = spec.AttributeType == typeof(string) ? database.GetSQLVarLengthStringType(spec.MaxNumChars) :
              database.GetSQLType(spec.AttributeType);
          switch (spec.Cardinality)
          {
            case Cardinality.Inline:
              string attrName = GenerateMIAAttributeColumnName(spec);
              terms.Add(attrName + " " + sqlType);
              break;
            case Cardinality.OneToMany:
            case Cardinality.ManyToOne:
            case Cardinality.ManyToMany:
              string collectionAttributeTableName = GenerateMIACollectionAttributeTableName(transaction, spec);
              string pkConstraintName = GenerateDBObjectName(transaction, miam.AspectId, collectionAttributeTableName + "_PK", "PK");
              string fkMediaItemConstraintName = GenerateDBObjectName(transaction, miam.AspectId, collectionAttributeTableName + "_MEDIA_ITEM_FK", "FK");
              command.CommandText = "CREATE TABLE " + collectionAttributeTableName + " (" +
                  MIA_MEDIA_ITEM_ID_COL_NAME + " " + database.GetSQLType(typeof(Int64)) + ", " +
                  COLL_MIA_VALUE_COL_NAME + " " + sqlType + ", " +
                  "CONSTRAINT " + pkConstraintName + " PRIMARY KEY (" + MIA_MEDIA_ITEM_ID_COL_NAME + "), " +
                  "CONSTRAINT " + fkMediaItemConstraintName +
                  " FOREIGN KEY (" + MIA_MEDIA_ITEM_ID_COL_NAME + ")" +
                  " REFERENCES " + MediaLibrary_SubSchema.MEDIA_ITEMS_TABLE_NAME + " (" + MediaLibrary_SubSchema.MEDIA_ITEMS_ITEM_ID_COL_NAME + ") ON DELETE CASCADE" +
                  ")";
              command.ExecuteNonQuery();
              break;
            default:
              throw new NotImplementedException(string.Format("Cardinality '{0}' for attribute '{1}.{2}' is not implemented",
                  spec.Cardinality, miam.AspectId, spec.AttributeName));
          }
        }
        mainStatementBuilder.Append(StringUtils.Join(", ", terms));
        mainStatementBuilder.Append(", ");
        string pkConstraintName1 = GenerateDBObjectName(transaction, miam.AspectId, miaTableName + "_PK", "PK");
        string fkMediaItemConstraintName1 = GenerateDBObjectName(transaction, miam.AspectId, miaTableName + "_MEDIA_ITEMS_FK", "FK");
        mainStatementBuilder.Append(
            "CONSTRAINT " + pkConstraintName1 + " PRIMARY KEY (" + MIA_MEDIA_ITEM_ID_COL_NAME + "), " +
            "CONSTRAINT " + fkMediaItemConstraintName1 +
            " FOREIGN KEY (" + MIA_MEDIA_ITEM_ID_COL_NAME + ") REFERENCES " +
                MediaLibrary_SubSchema.MEDIA_ITEMS_TABLE_NAME + " (" + MediaLibrary_SubSchema.MEDIA_ITEMS_ITEM_ID_COL_NAME + ") ON DELETE CASCADE");
        mainStatementBuilder.Append(")");
        command.CommandText = mainStatementBuilder.ToString();
        command.ExecuteNonQuery();

        transaction.Commit();
      }
      catch (Exception e)
      {
        ServiceScope.Get<ILogger>().Error("MIA_Management: Error adding media item aspect storage '{0}'", e, miam.AspectId);
        transaction.Rollback();
        throw;
      }
      lock (_syncObj)
        _managedMIATypes[miam.AspectId] = miam;
    }

    public void RemoveMediaItemAspectStorage(Guid aspectId)
    {
      lock (_syncObj)
      {
        if (!_managedMIATypes.ContainsKey(aspectId))
          return;
        _managedMIATypes[aspectId] = null;
      }
      MediaItemAspectMetadata miam = GetMediaItemAspectMetadata(aspectId);
      ISQLDatabase database = ServiceScope.Get<ISQLDatabase>();
      ITransaction transaction = database.BeginTransaction();
      try
      {
        // We don't remove the name alias mappings from the alias cache because we simply reload the alias cache at the end.
        // We don't remove the name alias mappings from the name aliases table because they are deleted by the DB system
        // (ON DELETE CASCADE).
        IDbCommand command = transaction.CreateCommand();
        command.CommandText = "DROP TABLE " + GetMIATableName(miam);
        command.ExecuteNonQuery();
        foreach (MediaItemAspectMetadata.AttributeSpecification spec in miam.AttributeSpecifications.Values)
        {
          switch (spec.Cardinality)
          {
            case Cardinality.Inline:
              break;
            case Cardinality.OneToMany:
            case Cardinality.ManyToOne:
            case Cardinality.ManyToMany:
              command.CommandText = "DROP TABLE " + GetMIACollectionAttributeTableName(spec);
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
        ServiceScope.Get<ILogger>().Error("MIA_Management: Error removing media item aspect storage '{0}'", e, aspectId);
        transaction.Rollback();
        throw;
      }
      lock (_syncObj)
        _managedMIATypes.Remove(aspectId);
    }

    #endregion

    #region MIA management

    public bool MIAExists(ITransaction transaction, Int64 mediaItemId, Guid aspectId)
    {
      MediaItemAspectMetadata miam;
      if (!_managedMIATypes.TryGetValue(aspectId, out miam) || miam == null)
        throw new ArgumentException(string.Format("MIA_Management: Requested media item aspect type with id '{0}' doesn't exist", aspectId));
      string miaTableName = GetMIATableName(miam);

      IDbCommand command = transaction.CreateCommand();
      command.CommandText = "SELECT " + MIA_MEDIA_ITEM_ID_COL_NAME + " FROM " + miaTableName +
          " WHERE " + MIA_MEDIA_ITEM_ID_COL_NAME + " = ?";

      IDbDataParameter param = command.CreateParameter();
      param.Value = mediaItemId;
      command.Parameters.Add(param);

      return command.ExecuteScalar() != null;
    }

    public MediaItemAspect GetMediaItemAspect(ITransaction transaction, Int64 mediaItemId, Guid aspectId)
    {
      MediaItemAspectMetadata miam;
      if (!_managedMIATypes.TryGetValue(aspectId, out miam) || miam == null)
        throw new ArgumentException(string.Format("MIA_Management: Requested media item aspect type with id '{0}' doesn't exist", aspectId));

      MediaItemAspect result = new MediaItemAspect(miam);

      IDbCommand command = transaction.CreateCommand();
      Namespace ns = new Namespace();
      IList<string> terms = new List<string>();
      IDbDataParameter param;
      IDataReader reader;
      foreach (MediaItemAspectMetadata.AttributeSpecification spec in miam.AttributeSpecifications.Values)
      {
        switch (spec.Cardinality)
        {
          case Cardinality.Inline:
            string attrName = GetMIAAttributeColumnName(spec);
            terms.Add(attrName + " " + ns.GetOrCreate(spec, "A"));
            break;
          case Cardinality.OneToMany:
          case Cardinality.ManyToOne:
          case Cardinality.ManyToMany:
            string collectionAttributeTableName = GetMIACollectionAttributeTableName(spec);
            command.CommandText = "SELECT " + COLL_MIA_VALUE_COL_NAME + " FROM " + collectionAttributeTableName + " WHERE " +
                MIA_MEDIA_ITEM_ID_COL_NAME + " = ?";

            param = command.CreateParameter();
            param.Value = mediaItemId;
            command.Parameters.Add(param);

            reader = command.ExecuteReader();
            try
            {
              IList values = new ArrayList();
              while (reader.Read())
                values.Add(reader.GetValue(0));
              result.SetCollectionAttribute(spec, values);
            }
            finally
            {
              reader.Close();
            }
            break;
          default:
            throw new NotImplementedException(string.Format("Cardinality '{0}' for attribute '{1}.{2}' is not implemented",
                spec.Cardinality, miam.AspectId, spec.AttributeName));
        }
      }
      string miaTableName = GetMIATableName(miam);
      StringBuilder mainQueryBuilder = new StringBuilder("SELECT ");
      mainQueryBuilder.Append(StringUtils.Join(", ", terms));
      mainQueryBuilder.Append(" FROM ");
      mainQueryBuilder.Append(miaTableName);
      mainQueryBuilder.Append(" WHERE ");
      mainQueryBuilder.Append(MIA_MEDIA_ITEM_ID_COL_NAME);
      mainQueryBuilder.Append(" = ?");
      command.CommandText = mainQueryBuilder.ToString();

      param = command.CreateParameter();
      param.Value = mediaItemId;
      command.Parameters.Add(param);

      reader = command.ExecuteReader();
      try
      {
        int i = 0;
        if (reader.Read())
          foreach (MediaItemAspectMetadata.AttributeSpecification spec in miam.AttributeSpecifications.Values)
          {
            if (spec.Cardinality == Cardinality.Inline)
              result.SetAttribute(spec, reader.GetValue(i));
            i++;
          }
      }
      finally
      {
        reader.Close();
      }
      return result;
    }

    public void InsertOrUpdateMIA(ITransaction transaction, Int64 mediaItemId, MediaItemAspect mia, bool insert)
    {
      MediaItemAspectMetadata miam;
      if (!_managedMIATypes.TryGetValue(mia.Metadata.AspectId, out miam) || miam == null)
        throw new ArgumentException(string.Format("MIA_Management: Requested media item aspect type with id '{0}' doesn't exist",
          mia.Metadata.AspectId));

      IDbCommand command = transaction.CreateCommand();
      IList<string> terms = new List<string>();
      IList<object> sqlValues = new List<object>();
      IDbDataParameter param;
      string miaTableName = GetMIATableName(miam);
      StringBuilder mainQueryBuilder = new StringBuilder();
      if (insert)
      {
        mainQueryBuilder.Append("INSERT INTO ");
        mainQueryBuilder.Append(miaTableName);
        mainQueryBuilder.Append(" (");
      }
      else
      {
        mainQueryBuilder.Append("UPDATE ");
        mainQueryBuilder.Append(miaTableName);
        mainQueryBuilder.Append(" SET ");
      }
      foreach (MediaItemAspectMetadata.AttributeSpecification spec in miam.AttributeSpecifications.Values)
      {
        if (mia.IsIgnore(spec))
          break;
        switch (spec.Cardinality)
        {
          case Cardinality.Inline:
            string attrName = GetMIAAttributeColumnName(spec);
            if (insert)
              terms.Add(attrName);
            else
              terms.Add(attrName + " = ?");
            sqlValues.Add(mia.GetAttribute(spec));
            break;
          case Cardinality.OneToMany:
          case Cardinality.ManyToOne:
          case Cardinality.ManyToMany:
            string collectionAttributeTableName = GetMIACollectionAttributeTableName(spec);
            if (!insert)
            {
              // Delete old entries
              command.CommandText = "DELETE FROM " + collectionAttributeTableName + " WHERE " + MIA_MEDIA_ITEM_ID_COL_NAME + " = ?";

              param = command.CreateParameter();
              param.Value = mediaItemId;
              command.Parameters.Add(param);

              command.ExecuteNonQuery();
            }

            // Add new entries - commands for insert and update are the same here
            IEnumerable values = mia.GetCollectionAttribute(spec);
            foreach (object value in values)
            {
              command.CommandText = "INSERT INTO " + collectionAttributeTableName + "(" +
                  COLL_MIA_VALUE_COL_NAME + ", " + MIA_MEDIA_ITEM_ID_COL_NAME + ") VALUES (?, ?)";

              param = command.CreateParameter();
              param.Value = value;
              command.Parameters.Add(param);

              param = command.CreateParameter();
              param.Value = mediaItemId;
              command.Parameters.Add(param);

              command.ExecuteNonQuery();
            }
            break;
          default:
            throw new NotImplementedException(string.Format("Cardinality '{0}' for attribute '{1}.{2}' is not implemented",
                spec.Cardinality, miam.AspectId, spec.AttributeName));
        }
      }
      // terms = all inline attributes
      // values = all inline attribute values
      if (terms.Count == 0)
        return;
      mainQueryBuilder.Append(StringUtils.Join(", ", terms));
      sqlValues.Add(mediaItemId);
      // values = all inline attribute values plus media item ID
      if (insert)
      {
        mainQueryBuilder.Append(", ");
        mainQueryBuilder.Append(MIA_MEDIA_ITEM_ID_COL_NAME); // Append the ID column as a normal attribute
        mainQueryBuilder.Append(") VALUES (");
        terms.Clear();
        for (int i = 0; i < sqlValues.Count; i++)
          terms.Add("?");
        mainQueryBuilder.Append(StringUtils.Join(", ", terms));
        mainQueryBuilder.Append(")");
      }
      else
      {
        mainQueryBuilder.Append(" WHERE ");
        mainQueryBuilder.Append(MIA_MEDIA_ITEM_ID_COL_NAME); // Use the ID column in WHERE condition
        mainQueryBuilder.Append(" = ?");
      }
      command.CommandText = mainQueryBuilder.ToString();

      foreach (object value in sqlValues)
      {
        param = command.CreateParameter();
        param.Value = value;
        command.Parameters.Add(param);
      }

      command.ExecuteNonQuery();
    }

    public void AddOrUpdateMIA(ITransaction transaction, Int64 mediaItemId, MediaItemAspect mia)
    {
      InsertOrUpdateMIA(transaction, mediaItemId, mia, !MIAExists(transaction, mediaItemId, mia.Metadata.AspectId));
    }

    public bool RemoveMIA(ITransaction transaction, Int64 mediaItemId, Guid aspectId)
    {
      MediaItemAspectMetadata miam;
      if (!_managedMIATypes.TryGetValue(aspectId, out miam) || miam == null)
        throw new ArgumentException(string.Format("MIA_Management: Requested media item aspect type with id '{0}' doesn't exist", aspectId));
      string miaTableName = GetMIATableName(miam);

      IDbCommand command = transaction.CreateCommand();
      command.CommandText = "DELETE FROM " + miaTableName + " WHERE " + MIA_MEDIA_ITEM_ID_COL_NAME + " = ?";

      IDbDataParameter param = command.CreateParameter();
      param.Value = mediaItemId;
      command.Parameters.Add(param);

      return command.ExecuteNonQuery() > 0;
    }

    #endregion
  }
}
