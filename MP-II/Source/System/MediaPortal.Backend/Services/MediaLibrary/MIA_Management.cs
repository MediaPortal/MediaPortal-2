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
using System.Threading;
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
  /// Management class for media item aspect types and media item aspects. Contains methods to create/drop MIA table structures
  /// as well as to add/update/delete MIA values.
  /// </summary>
  /// <remarks>
  /// <para>
  /// This class for dynamic MIA management is quite complex. We have to cope with several aspects:
  /// <list type="bullet">
  /// <item>Automatic generation of IDs</item>
  /// <item>Automatic generation of database object names for attributes and tables</item>
  /// <item>Handling of all cardinalities described in class <see cref="Cardinality"/></item>
  /// <item>Locking of concurrent access to MIA attribute value tables whose values are re-used in N:M or N:1 attribute value associations</item>
  /// </list>
  /// This class can be used as reference for the structure of the dynamic media library part.
  /// </para>
  /// <para>
  /// MIA tables are dynamic in that way that the MediaPortal core implementation doesn't specify the concrete structure of
  /// concrete MIA types but it specifies a generic structure for a main MIA table, which holds all inline attribute values
  /// and all references to N:1 attribute values, and adjacent tables for holding 1:N, N:1 and N:M attribute values.
  /// This class implements all algorithms to create/drop tables for media item aspect types and to
  /// select and insert/update media item aspect instances.
  /// All ID columns (and foreign key columns referencing to those ID columns) are of type <see cref="Int64"/>.
  /// All columns holding attribute values are of a type which is infered by a call to the database's methods
  /// <see cref="ISQLDatabase.GetSQLType"/> or <see cref="ISQLDatabase.GetSQLVarLengthStringType"/>, depending on the
  /// .net type of the media item aspect attribute.
  /// </para>
  /// <para>
  /// Each main media item aspect table contains a column for the ID of the entries
  /// (column name is <see cref="MIA_MEDIA_ITEM_ID_COL_NAME"/>) and columns for each attribute type of
  /// the cardinalities <see cref="Cardinality.Inline"/> and <see cref="Cardinality.ManyToOne"/>.
  /// For each media item aspect associated to a concreate media item, one entry in the main media item aspect table
  /// exists.<br/>
  /// Extra tables exist for all attributes of the cardinalities <see cref="Cardinality.OneToMany"/>,
  /// <see cref="Cardinality.ManyToOne"/> and <see cref="Cardinality.ManyToMany"/>.
  /// </para>
  /// <para>
  /// External tables for attributes of cardinality <see cref="Cardinality.OneToMany"/> are the easiest external tables.
  /// They contain two columns, <see cref="MIA_MEDIA_ITEM_ID_COL_NAME"/> contains the foreign key to the ID of the
  /// main media item aspect type table and <see cref="COLL_ATTR_VALUE_COL_NAME"/> contains the actual attribute value.<br/>
  /// For each value of the given attribute type, which is associated to a media item, there exists one entry. If the same
  /// value is associated to more than one media item, there are multiple entries in this table.
  /// </para>
  /// <para>
  /// External tables for attributes of cardinality <see cref="Cardinality.ManyToOne"/> contain an own ID of name
  /// <see cref="FOREIGN_COLL_ATTR_ID_COL_NAME"/>, which is referenced from the foreign key attribute in the main
  /// media item aspect table for that attribute, and a column for the actual attribute value of name
  /// <see cref="COLL_ATTR_VALUE_COL_NAME"/>.<br/>
  /// Each value, which is associated to at least one media item, is contained at most once in this kind of table.
  /// </para>
  /// <para>
  /// External tables for attributes of cardinality <see cref="Cardinality.ManyToMany"/> contain an own ID of name
  /// <see cref="FOREIGN_COLL_ATTR_ID_COL_NAME"/> and a column for the actual attribute value of name
  /// <see cref="COLL_ATTR_VALUE_COL_NAME"/>.<br/>
  /// In contrast to the structure of <see cref="Cardinality.ManyToOne"/>, in the structure for cardinality
  /// <see cref="Cardinality.ManyToMany"/> there exists another foreign N:M table containing the associations between
  /// the main media item aspect table and the values table. That N:M table contains two columns, one is a foreign key
  /// to the main media item aspect table and has the name <see cref="MIA_MEDIA_ITEM_ID_COL_NAME"/>, the second is a
  /// foreign key to the values table and has the name <see cref="FOREIGN_COLL_ATTR_ID_COL_NAME"/>.
  /// Each value, which is associated to at least one media item, is contained at most once in the values table. For each
  /// association between a media item and a value in the values table, there exists an entry in the association table.
  /// </para>
  /// </remarks>
  public class MIA_Management
  {
    protected class ThreadOwnership
    {
      protected readonly Thread _currentThread;
      protected int _lockCount;

      public ThreadOwnership(Thread currentThread)
      {
        _currentThread = currentThread;
      }

      public Thread CurrentThread
      {
        get { return _currentThread; }
      }

      public int LockCount
      {
        get { return _lockCount; }
        set { _lockCount = value; }
      }
    }

    /// <summary>
    /// ID column name which is both primary key and foreign key referencing the ID in the main media item's table.
    /// This column name is used both for each MIA main table and for MIA attribute collection tables.
    /// </summary>
    protected internal const string MIA_MEDIA_ITEM_ID_COL_NAME = "MEDIA_ITEM_ID";

    /// <summary>
    /// ID column name for MIA collection attribute tables
    /// </summary>
    protected internal const string FOREIGN_COLL_ATTR_ID_COL_NAME = "ID";

    /// <summary>
    /// Value column name for MIA collection attribute tables.
    /// </summary>
    protected internal const string COLL_ATTR_VALUE_COL_NAME = "ATTRIBUTE_VALUE";

    protected readonly IDictionary<string, string> _nameAliases = new Dictionary<string, string>();

    /// <summary>
    /// Caches all media item aspect types the database is able to store. A MIA type which is currently being
    /// added or removed will have a <c>null</c> value assigned to its ID.
    /// </summary>
    protected readonly IDictionary<Guid, MediaItemAspectMetadata> _managedMIATypes =
        new Dictionary<Guid, MediaItemAspectMetadata>();

    protected readonly object _syncObj = new object();

    /// <summary>
    /// For all contained attribute types, the value tables are locked for concurrent access.
    /// </summary>
    protected readonly IDictionary<MediaItemAspectMetadata.AttributeSpecification, ThreadOwnership> _lockedAttrs =
        new Dictionary<MediaItemAspectMetadata.AttributeSpecification, ThreadOwnership>();

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
      return "T_" + SqlUtils.ToSQLIdentifier(miam.AspectId.ToString()).ToUpperInvariant();
    }

    /// <summary>
    /// Gets a technical column identifier for the given inline attribute specification.
    /// </summary>
    /// <param name="spec">Attribute specification to return the column identifier for.</param>
    /// <returns>Column identifier for the given attribute specification. The returned identifier must be
    /// shortened to match the maximum column name length.</returns>
    internal string GetMIAAttributeColumnIdentifier(MediaItemAspectMetadata.AttributeSpecification spec)
    {
      return "A_" + SqlUtils.ToSQLIdentifier(spec.ParentMIAM.AspectId + "_" + spec.AttributeName).ToUpperInvariant();
    }

    /// <summary>
    /// Gets a technical table identifier for the given MIAM collection attribute.
    /// </summary>
    /// <returns>Table identifier for the given collection attribute. The returned identifier must be mapped to a
    /// shortened table name to be used in the DB.</returns>
    internal string GetMIACollectionAttributeTableIdentifier(MediaItemAspectMetadata.AttributeSpecification spec)
    {
      return "V_" + GetMIATableName(spec.ParentMIAM) + "_" + SqlUtils.ToSQLIdentifier(spec.AttributeName);
    }

    /// <summary>
    /// Gets a technical table identifier for the N:M table for the given MIAM collection attribute.
    /// </summary>
    /// <returns>Table identifier for the N:M table for the given collection attribute. The returned identifier must be
    /// mapped to a shortened table name to be used in the DB.</returns>
    internal string GetMIACollectionAttributeNMTableIdentifier(MediaItemAspectMetadata.AttributeSpecification spec)
    {
      return "NM_" + GetMIATableName(spec.ParentMIAM) + "_" + SqlUtils.ToSQLIdentifier(spec.AttributeName);
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
      return GetAliasMapping(identifier, string.Format("MIAM '{0}' (id: '{1}') doesn't have a corresponding table name yet",
          miam.Name, miam.AspectId));
    }

    /// <summary>
    /// Gets the actual column name for a MIAM attribute specification.
    /// </summary>
    /// <returns>Column name for the column containing the inline attribute data of the specified attribute
    /// <paramref name="spec"/>.</returns>
    internal string GetMIAAttributeColumnName(MediaItemAspectMetadata.AttributeSpecification spec)
    {
      string identifier = GetMIAAttributeColumnIdentifier(spec);
      return GetAliasMapping(identifier, string.Format("Attribute '{0}' of MIAM '{1}' (id: '{2}') doesn't have a corresponding column name yet",
          spec.AttributeName, spec.ParentMIAM.Name, spec.ParentMIAM.AspectId));
    }

    /// <summary>
    /// Gets the actual table name for a MIAM collection attribute table.
    /// </summary>
    /// <returns>Table name for the table containing the specified collection attribute.</returns>
    internal string GetMIACollectionAttributeTableName(MediaItemAspectMetadata.AttributeSpecification spec)
    {
      string identifier = GetMIACollectionAttributeTableIdentifier(spec);
      return GetAliasMapping(identifier, string.Format("Attribute '{0}' of MIAM '{1}' (id: '{2}') doesn't have a corresponding table name yet",
          spec.AttributeName, spec.ParentMIAM.Name, spec.ParentMIAM.AspectId));
    }

    /// <summary>
    /// Gets the actual table name for a MIAM collection attribute table.
    /// </summary>
    /// <returns>Table name for the table containing the specified collection attribute.</returns>
    internal string GetMIACollectionAttributeNMTableName(MediaItemAspectMetadata.AttributeSpecification spec)
    {
      string identifier = GetMIACollectionAttributeNMTableIdentifier(spec);
      return GetAliasMapping(identifier, string.Format("Attribute '{0}' of MIAM '{1}' (id: '{2}') doesn't have a corresponding N:M table name yet",
          spec.AttributeName, spec.ParentMIAM.Name, spec.ParentMIAM.AspectId));
    }

    private static string ConcatNameParts(string prefix, uint suffix, uint maxLen)
    {
      string suf = "_" + suffix;
      if (prefix.Length + suf.Length > maxLen)
        return prefix.Substring(0, (int) maxLen - suf.Length) + suf;
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
    /// <param name="desiredName">Root name to start the name generation. Will be converted to a valid SQL identifier
    /// and clipped, if necessary.</param>
    /// <returns>Table name corresponding to the specified table identifier.</returns>
    private string GenerateDBObjectName(ITransaction transaction, Guid aspectId, string objectIdentifier, string desiredName)
    {
      lock (_syncObj)
      {
        if (_nameAliases.ContainsKey(objectIdentifier))
          throw new InvalidDataException("Table identifier '{0}' is already present in alias cache", objectIdentifier);
        ISQLDatabase database = ServiceScope.Get<ISQLDatabase>();
        uint maxLen = database.MaxObjectNameLength;
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

    internal string GenerateMIATableName(ITransaction transaction, MediaItemAspectMetadata miam)
    {
      string identifier = GetMIATableIdentifier(miam);
      return GenerateDBObjectName(transaction, miam.AspectId, identifier, "M_" + miam.Name);
    }

    internal string GenerateMIAAttributeColumnName(ITransaction transaction, MediaItemAspectMetadata.AttributeSpecification spec)
    {
      string identifier = GetMIAAttributeColumnIdentifier(spec);
      return GenerateDBObjectName(transaction, spec.ParentMIAM.AspectId, identifier, spec.AttributeName);
    }

    internal string GenerateMIACollectionAttributeTableName(ITransaction transaction, MediaItemAspectMetadata.AttributeSpecification spec)
    {
      string identifier = GetMIACollectionAttributeTableIdentifier(spec);
      return GenerateDBObjectName(transaction, spec.ParentMIAM.AspectId, identifier, "V_" + spec.AttributeName);
    }

    internal string GenerateMIACollectionAttributeNMTableName(ITransaction transaction, MediaItemAspectMetadata.AttributeSpecification spec)
    {
      string identifier = GetMIACollectionAttributeNMTableIdentifier(spec);
      return GenerateDBObjectName(transaction, spec.ParentMIAM.AspectId, identifier, "NM_" + spec.AttributeName);
    }

    #endregion

    #region Other protected & internal methods

    protected void LoadMIATypeCache()
    {
      lock (_syncObj)
      {
        foreach (MediaItemAspectMetadata miam in SelectAllManagedMediaItemAspectMetadata())
          _managedMIATypes[miam.AspectId] = miam;
      }
    }

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

    protected IList GetOneToManyMIAAttributeValues(ITransaction transaction, Int64 mediaItemId,
        MediaItemAspectMetadata.AttributeSpecification spec)
    {
      string collectionAttributeTableName = GetMIACollectionAttributeTableName(spec);
      IDbCommand command = transaction.CreateCommand();
      command.CommandText = "SELECT " + COLL_ATTR_VALUE_COL_NAME + " FROM " + collectionAttributeTableName + " WHERE " +
          MIA_MEDIA_ITEM_ID_COL_NAME + " = ?";

      IDbDataParameter param = command.CreateParameter();
      param.Value = mediaItemId;
      command.Parameters.Add(param);

      IDataReader reader = command.ExecuteReader();
      try
      {
        IList result = new ArrayList();
        while (reader.Read())
          result.Add(reader.GetValue(0));
        return result;
      }
      finally
      {
        reader.Close();
      }
    }

    protected object GetManyToOneMIAAttributeValue(ITransaction transaction, Int64 mediaItemId,
        MediaItemAspectMetadata.AttributeSpecification spec, string miaTableName)
    {
      string collectionAttributeTableName = GetMIACollectionAttributeTableName(spec);
      string mainTableAttrName = GetMIAAttributeColumnName(spec);
      IDbCommand command = transaction.CreateCommand();
      command.CommandText = "SELECT " + COLL_ATTR_VALUE_COL_NAME + " FROM " + collectionAttributeTableName + " AS VAL" +
          " INNER JOIN " + miaTableName + " AS MAIN ON VAL." + FOREIGN_COLL_ATTR_ID_COL_NAME + " = MAIN." + mainTableAttrName +
          " WHERE MAIN." + MIA_MEDIA_ITEM_ID_COL_NAME + " = ?";

      IDbDataParameter param = command.CreateParameter();
      param.Value = mediaItemId;
      command.Parameters.Add(param);

      IDataReader reader = command.ExecuteReader();
      try
      {
        if (reader.Read())
          return reader.GetValue(0);
        return null;
      }
      finally
      {
        reader.Close();
      }
    }

    protected IList GetManyToManyMIAAttributeValues(ITransaction transaction, Int64 mediaItemId,
        MediaItemAspectMetadata.AttributeSpecification spec)
    {
      string collectionAttributeTableName = GetMIACollectionAttributeTableName(spec);
      string nmTableName = GenerateMIACollectionAttributeNMTableName(transaction, spec);
      IDbCommand command = transaction.CreateCommand();
      command.CommandText = "SELECT " + COLL_ATTR_VALUE_COL_NAME + " FROM " + collectionAttributeTableName + " AS VAL" +
          " INNER JOIN " + nmTableName + " AS NM ON VAL." + FOREIGN_COLL_ATTR_ID_COL_NAME + " = NM." + FOREIGN_COLL_ATTR_ID_COL_NAME +
          " WHERE NM." + MIA_MEDIA_ITEM_ID_COL_NAME + " = ?";

      IDbDataParameter param = command.CreateParameter();
      param.Value = mediaItemId;
      command.Parameters.Add(param);

      IDataReader reader = command.ExecuteReader();
      try
      {
        IList result = new ArrayList();
        while (reader.Read())
          result.Add(reader.GetValue(0));
        return result;
      }
      finally
      {
        reader.Close();
      }
    }

    protected void LockAttribute(MediaItemAspectMetadata.AttributeSpecification spec)
    {
      lock (_syncObj)
      {
        Thread currentThread = Thread.CurrentThread;
        ThreadOwnership to;
        while (_lockedAttrs.TryGetValue(spec, out to) && to.CurrentThread != currentThread)
          Monitor.Wait(_syncObj);
        if (!_lockedAttrs.TryGetValue(spec, out to))
          _lockedAttrs[spec] = to = new ThreadOwnership(currentThread);
        to.LockCount++;
      }
    }

    protected void UnlockAttribute(MediaItemAspectMetadata.AttributeSpecification spec)
    {
      lock (_syncObj)
      {
        Thread currentThread = Thread.CurrentThread;
        ThreadOwnership to;
        if (!_lockedAttrs.TryGetValue(spec, out to) || to.CurrentThread != currentThread)
          throw new IllegalCallException("Media item aspect attribute '{0}' of media item aspect '{1}' (id '{2}') is not locked by the current thread",
              spec.AttributeName, spec.ParentMIAM.Name, spec.ParentMIAM.AspectId);
        to.LockCount--;
        if (to.LockCount == 0)
        {
          _lockedAttrs.Remove(spec);
          Monitor.PulseAll(_syncObj);
        }
      }
    }

    protected void DeleteOneToManyAttributeValuesNotInEnumeration(ITransaction transaction,
        MediaItemAspectMetadata.AttributeSpecification spec, Int64 mediaItemId, IEnumerable values)
    {
      string collectionAttributeTableName = GetMIACollectionAttributeTableName(spec);

      IDbCommand command = transaction.CreateCommand();

      IDbDataParameter param = command.CreateParameter();
      param.Value = mediaItemId;
      command.Parameters.Add(param);

      int numValues = 0;
      foreach (object value in values)
      {
        numValues++;
        param = command.CreateParameter();
        param.Value = value;
        command.Parameters.Add(param);
      }
      command.CommandText = "DELETE FROM " + collectionAttributeTableName + " WHERE " + MIA_MEDIA_ITEM_ID_COL_NAME + " = ? AND " +
          COLL_ATTR_VALUE_COL_NAME + " NOT IN(" + StringUtils.Join(", ", CollectionUtils.Fill("?", numValues)) + ")";
      command.ExecuteNonQuery();
    }

    protected void InsertOrUpdateOneToManyMIAAttributeValues(ITransaction transaction,
        MediaItemAspectMetadata.AttributeSpecification spec, Int64 mediaItemId, IEnumerable values, bool insert)
    {
      string collectionAttributeTableName = GetMIACollectionAttributeTableName(spec);
      if (!insert)
        // Delete old entries
        DeleteOneToManyAttributeValuesNotInEnumeration(transaction, spec, mediaItemId, values);

      IDatabaseManager databaseManager = ServiceScope.Get<IDatabaseManager>();
      // Add new entries - commands for insert and update are the same here
      foreach (object value in values)
      {
        IDbCommand command = transaction.CreateCommand();
        command.CommandText = "INSERT INTO " + collectionAttributeTableName + "(" +
            MIA_MEDIA_ITEM_ID_COL_NAME + ", " + COLL_ATTR_VALUE_COL_NAME + ") SELECT ?, ? FROM " + databaseManager.DummyTableName +
            " WHERE NOT EXISTS(SELECT " + MIA_MEDIA_ITEM_ID_COL_NAME + " FROM " + collectionAttributeTableName + " WHERE " +
            MIA_MEDIA_ITEM_ID_COL_NAME + " = ? AND " + COLL_ATTR_VALUE_COL_NAME + " = ?)";

        IDbDataParameter param = command.CreateParameter();
        param.Value = mediaItemId;
        command.Parameters.Add(param);

        param = command.CreateParameter();
        param.Value = value;
        command.Parameters.Add(param);

        param = command.CreateParameter();
        param.Value = mediaItemId;
        command.Parameters.Add(param);

        param = command.CreateParameter();
        param.Value = value;
        command.Parameters.Add(param);

        command.ExecuteNonQuery();
      }
    }

    protected void CleanupAllManyToOneOrphanedAttributeValues(ITransaction transaction, MediaItemAspectMetadata miaType)
    {
      foreach (MediaItemAspectMetadata.AttributeSpecification spec in miaType.AttributeSpecifications.Values)
        switch (spec.Cardinality)
        {
          case Cardinality.Inline:
          case Cardinality.OneToMany:
            break;
          case Cardinality.ManyToOne:
            CleanupManyToOneOrphanedAttributeValues(transaction, spec);
            break;
          case Cardinality.ManyToMany:
            break;
          default:
            throw new NotImplementedException(string.Format("Cardinality '{0}' for attribute '{1}.{2}' is not implemented",
                spec.Cardinality, spec.ParentMIAM.AspectId, spec.AttributeName));
        }
    }

    protected void CleanupManyToOneOrphanedAttributeValues(ITransaction transaction,
        MediaItemAspectMetadata.AttributeSpecification spec)
    {
      string collectionAttributeTableName = GetMIACollectionAttributeTableName(spec);
      string miaTableName = GetMIATableName(spec.ParentMIAM);
      string attrColName = GetMIAAttributeColumnName(spec);

      IDbCommand command = transaction.CreateCommand();
      command.CommandText = "DELETE FROM " + collectionAttributeTableName + " AS VAL WHERE NOT EXISTS (" +
          "SELECT " + MIA_MEDIA_ITEM_ID_COL_NAME + " FROM " + miaTableName + " AS MIA WHERE MIA." +
          attrColName + " = VAL." + FOREIGN_COLL_ATTR_ID_COL_NAME + ")";

      command.ExecuteNonQuery();
    }

    protected void GetOrCreateManyToOneMIAAttributeValue(ITransaction transaction,
        MediaItemAspectMetadata.AttributeSpecification spec, Int64 mediaItemId, object value, bool insert, out Int64 valuePk)
    {
      string collectionAttributeTableName = GetMIACollectionAttributeTableName(spec);
      IDatabaseManager databaseManager = ServiceScope.Get<IDatabaseManager>();
      ISQLDatabase database = transaction.Database;

      LockAttribute(spec);
      try
      {
        // Insert value into collection attribute table if not exists
        IDbCommand command = transaction.CreateCommand();
        command.CommandText = "INSERT INTO " + collectionAttributeTableName + " (" +
            FOREIGN_COLL_ATTR_ID_COL_NAME + ", " + COLL_ATTR_VALUE_COL_NAME + ") SELECT " +
            database.GetSelectSequenceNextValStatement(MediaLibrary_SubSchema.MEDIA_LIBRARY_ID_SEQUENCE_NAME) + ", ? FROM " +
            databaseManager.DummyTableName + " WHERE NOT EXISTS(" +
            "SELECT " + FOREIGN_COLL_ATTR_ID_COL_NAME + " FROM " +
            collectionAttributeTableName + " WHERE " + COLL_ATTR_VALUE_COL_NAME + " = ?)";

        IDbDataParameter param = command.CreateParameter();
        param.Value = value;
        command.Parameters.Add(param);
            
        param = command.CreateParameter();
        param.Value = value;
        command.Parameters.Add(param);

        command.ExecuteNonQuery();

        command = transaction.CreateCommand();
        command.CommandText = "SELECT " + FOREIGN_COLL_ATTR_ID_COL_NAME + " FROM " + collectionAttributeTableName + " WHERE " +
            COLL_ATTR_VALUE_COL_NAME + " = ?";

        param = command.CreateParameter();
        param.Value = value;
        command.Parameters.Add(param);

        valuePk = (Int64) command.ExecuteScalar();
      }
      finally
      {
        UnlockAttribute(spec);
      }
    }

    protected void DeleteManyToManyAttributeAssociationsNotInEnumeration(ITransaction transaction,
        MediaItemAspectMetadata.AttributeSpecification spec, Int64 mediaItemId, IEnumerable values)
    {
      string collectionAttributeTableName = GetMIACollectionAttributeTableName(spec);
      string nmTableName = GetMIACollectionAttributeNMTableName(spec);

      IDbCommand command = transaction.CreateCommand();

      IDbDataParameter param = command.CreateParameter();
      param.Value = mediaItemId;
      command.Parameters.Add(param);

      int numValues = 0;
      foreach (object value in values)
      {
        numValues++;
        param = command.CreateParameter();
        param.Value = value;
        command.Parameters.Add(param);
      }
      command.CommandText = "DELETE FROM " + nmTableName + " AS NM WHERE " + MIA_MEDIA_ITEM_ID_COL_NAME + " = ? AND NOT EXISTS(" +
          "SELECT " + FOREIGN_COLL_ATTR_ID_COL_NAME + " FROM " + collectionAttributeTableName + " VAL WHERE VAL." +
          FOREIGN_COLL_ATTR_ID_COL_NAME + " = NM." + FOREIGN_COLL_ATTR_ID_COL_NAME + " AND " +
          COLL_ATTR_VALUE_COL_NAME + " IN (" + StringUtils.Join(", ", CollectionUtils.Fill("?", numValues)) + "))";
      command.ExecuteNonQuery();
    }

    protected void CleanupManyToManyOrphanedAttributeValues(ITransaction transaction,
        MediaItemAspectMetadata.AttributeSpecification spec)
    {
      string collectionAttributeTableName = GetMIACollectionAttributeTableName(spec);
      string nmTableName = GetMIACollectionAttributeNMTableName(spec);

      IDbCommand command = transaction.CreateCommand();
      command.CommandText = "DELETE FROM " + collectionAttributeTableName + " AS VAL WHERE NOT EXISTS (" +
          "SELECT " + FOREIGN_COLL_ATTR_ID_COL_NAME + " FROM " + nmTableName + " AS NM WHERE " +
          FOREIGN_COLL_ATTR_ID_COL_NAME + " = VAL." + FOREIGN_COLL_ATTR_ID_COL_NAME + ")";

      command.ExecuteNonQuery();
    }

    protected void InsertOrUpdateManyToManyMIAAttributeValue(ITransaction transaction,
        MediaItemAspectMetadata.AttributeSpecification spec, Int64 mediaItemId, object value)
    {
      string collectionAttributeTableName = GetMIACollectionAttributeTableName(spec);
      IDatabaseManager databaseManager = ServiceScope.Get<IDatabaseManager>();
      // Insert value into collection attribute table if not exists: We do it in a single statement to avoid rountrips to the DB
      IDbCommand command = transaction.CreateCommand();
      command.CommandText = "INSERT INTO " + collectionAttributeTableName + " (" +
          FOREIGN_COLL_ATTR_ID_COL_NAME + ", " + COLL_ATTR_VALUE_COL_NAME + ") SELECT " +
          transaction.Database.GetSelectSequenceNextValStatement(MediaLibrary_SubSchema.MEDIA_LIBRARY_ID_SEQUENCE_NAME) +
          ", ? FROM " + databaseManager.DummyTableName + " WHERE NOT EXISTS(SELECT " + FOREIGN_COLL_ATTR_ID_COL_NAME +
          " FROM " + collectionAttributeTableName + " WHERE " + COLL_ATTR_VALUE_COL_NAME + " = ?)";

      IDbDataParameter param = command.CreateParameter();
      param.Value = value;
      command.Parameters.Add(param);
          
      param = command.CreateParameter();
      param.Value = value;
      command.Parameters.Add(param);

      command.ExecuteNonQuery();

      // Check association: We do it here with a single statement to avoid roundtrips to the DB
      string nmTableName = GetMIACollectionAttributeNMTableName(spec);
      command = transaction.CreateCommand();
      command.CommandText = "INSERT INTO " + nmTableName + " (" + MIA_MEDIA_ITEM_ID_COL_NAME + ", " + FOREIGN_COLL_ATTR_ID_COL_NAME +
          ") SELECT ?, " + FOREIGN_COLL_ATTR_ID_COL_NAME + " FROM " + collectionAttributeTableName +
          " WHERE " + COLL_ATTR_VALUE_COL_NAME + " = ? AND NOT EXISTS(" +
            "SELECT VAL." + FOREIGN_COLL_ATTR_ID_COL_NAME + " FROM " + collectionAttributeTableName + " AS VAL " +
            " INNER JOIN " + nmTableName + " AS NM ON VAL." + FOREIGN_COLL_ATTR_ID_COL_NAME + " = NM." + FOREIGN_COLL_ATTR_ID_COL_NAME +
            " WHERE VAL." + COLL_ATTR_VALUE_COL_NAME + " = ? AND NM." + MIA_MEDIA_ITEM_ID_COL_NAME + " = ?" +
          ")";

      param = command.CreateParameter();
      param.Value = mediaItemId;
      command.Parameters.Add(param);
          
      param = command.CreateParameter();
      param.Value = value;
      command.Parameters.Add(param);

      param = command.CreateParameter();
      param.Value = value;
      command.Parameters.Add(param);

      param = command.CreateParameter();
      param.Value = mediaItemId;
      command.Parameters.Add(param);

      command.ExecuteNonQuery();
    }

    protected void InsertOrUpdateManyToManyMIAAttributeValues(ITransaction transaction,
        MediaItemAspectMetadata.AttributeSpecification spec, Int64 mediaItemId, IEnumerable values, bool insert)
    {
      LockAttribute(spec);
      try
      {
        if (!insert)
          DeleteManyToManyAttributeAssociationsNotInEnumeration(transaction, spec, mediaItemId, values);
        foreach (object value in values)
          InsertOrUpdateManyToManyMIAAttributeValue(transaction, spec, mediaItemId, value);
        if (!insert)
          CleanupManyToManyOrphanedAttributeValues(transaction, spec);
      }
      finally
      {
        UnlockAttribute(spec);
      }
    }

    protected object ReadObject(IDataReader reader, int colIndex, MediaItemAspectMetadata.AttributeSpecification spec)
    {
      // Because the IDataReader interface doesn't provide a getter method which takes the desired return type,
      // we have to write this method
      Type type = spec.AttributeType;
      if (type == typeof(string))
        return reader.GetString(colIndex);
      else if (type == typeof(DateTime))
        return reader.GetDateTime(colIndex);
      else if (type == typeof(Char))
        return reader.GetChar(colIndex);
      else if (type == typeof(Boolean))
        return reader.GetBoolean(colIndex);
      else if (type == typeof(Single))
        return reader.GetFloat(colIndex);
      else if (type == typeof(Double))
        return reader.GetDouble(colIndex);
      else if (type == typeof(Int32))
        return reader.GetInt32(colIndex);
      else if (type ==typeof(Int64))
        return reader.GetInt64(colIndex);
      else
        throw new NotSupportedException(string.Format(
            "The datatype '{0}' of attribute '{1}' in media item aspect type '{2}' (id '{3}') is not supported", type, spec.AttributeName, spec.ParentMIAM.Name, spec.ParentMIAM.AspectId));
    }

    protected bool AttributeIsEmpty(object value)
    {
      return value == null || value as string == string.Empty;
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
      ServiceScope.Get<ILogger>().Info("MIA_Management: Adding media library storage for media item aspect '{0}' (id '{1}')",
          miam.Name, miam.AspectId);
      try
      {
        // Register metadata first - generated aliases will reference to the new MIA type row
        IDbCommand command = MediaLibrary_SubSchema.CreateMediaItemAspectMetadataCommand(transaction, miam.AspectId, miam.Name, miam.Serialize());
        command.ExecuteNonQuery();

        // Create main table for new MIA type
        string miaTableName = GenerateMIATableName(transaction, miam);
        StringBuilder mainStatementBuilder = new StringBuilder("CREATE TABLE " + miaTableName + " (" +
            MIA_MEDIA_ITEM_ID_COL_NAME + " " + database.GetSQLType(typeof(Int64)) + ", ");
        IList<string> terms = new List<string>();
        IList<string> additionalAttributesConstraints = new List<string>();
        string collectionAttributeTableName;
        string pkConstraintName;

        // Attributes: First run
        foreach (MediaItemAspectMetadata.AttributeSpecification spec in miam.AttributeSpecifications.Values)
        {
          string sqlType = spec.AttributeType == typeof(string) ? database.GetSQLVarLengthStringType(spec.MaxNumChars) :
              database.GetSQLType(spec.AttributeType);
          string attributeColumnName = GenerateMIAAttributeColumnName(transaction, spec);
          switch (spec.Cardinality)
          {
            case Cardinality.Inline:
              terms.Add(attributeColumnName + " " + sqlType);
              break;
            case Cardinality.OneToMany:
              GenerateMIACollectionAttributeTableName(transaction, spec);
              break;
            case Cardinality.ManyToOne:
              // Create foreign table - the join attribute will be located in the main MIA table
              // We need to create the "One" table first because the main table references on it
              collectionAttributeTableName = GenerateMIACollectionAttributeTableName(transaction, spec);
              pkConstraintName = GenerateDBObjectName(transaction, miam.AspectId, collectionAttributeTableName + "_PK", "PK");

              command = transaction.CreateCommand();
              command.CommandText = "CREATE TABLE " + collectionAttributeTableName + " (" +
                  FOREIGN_COLL_ATTR_ID_COL_NAME + " " + database.GetSQLType(typeof(Int64)) + ", " +
                  COLL_ATTR_VALUE_COL_NAME + " " + sqlType + ", " +
                  "CONSTRAINT " + pkConstraintName + " PRIMARY KEY (" + FOREIGN_COLL_ATTR_ID_COL_NAME + ")" +
                  ")";
              ServiceScope.Get<ILogger>().Debug("MIA_Management: Creating MTO table '{0}' for attribute '{1}' in media item aspect '{2}'",
                  collectionAttributeTableName, spec.AttributeName, miam.AspectId);
              command.ExecuteNonQuery();

              // Create foreign table - the join attribute will be located in the main MIA table
              string fkMediaItemConstraintName = GenerateDBObjectName(transaction, miam.AspectId, "MIA_" + collectionAttributeTableName + "_FK", "FK");

              terms.Add(attributeColumnName + " " + database.GetSQLType(typeof(Int64)));
              additionalAttributesConstraints.Add("CONSTRAINT " + fkMediaItemConstraintName +
                  " FOREIGN KEY (" + attributeColumnName + ")" +
                  " REFERENCES " + collectionAttributeTableName + " (" + FOREIGN_COLL_ATTR_ID_COL_NAME + ") ON DELETE SET NULL");
              break;
            case Cardinality.ManyToMany:
              GenerateMIACollectionAttributeTableName(transaction, spec);
              break;
            default:
              throw new NotImplementedException(string.Format("Cardinality '{0}' for attribute '{1}.{2}' is not implemented",
                  spec.Cardinality, miam.AspectId, spec.AttributeName));
          }
        }

        // Main table
        mainStatementBuilder.Append(StringUtils.Join(", ", terms));
        mainStatementBuilder.Append(", ");
        string pkConstraintName1 = GenerateDBObjectName(transaction, miam.AspectId, miaTableName + "_PK", "PK");
        string fkMediaItemConstraintName1 = GenerateDBObjectName(transaction, miam.AspectId, miaTableName + "_MEDIA_ITEMS_FK", "FK");
        mainStatementBuilder.Append(
            "CONSTRAINT " + pkConstraintName1 + " PRIMARY KEY (" + MIA_MEDIA_ITEM_ID_COL_NAME + "), " +
            "CONSTRAINT " + fkMediaItemConstraintName1 +
            " FOREIGN KEY (" + MIA_MEDIA_ITEM_ID_COL_NAME + ") REFERENCES " +
                MediaLibrary_SubSchema.MEDIA_ITEMS_TABLE_NAME + " (" + MediaLibrary_SubSchema.MEDIA_ITEMS_ITEM_ID_COL_NAME + ") ON DELETE CASCADE");
        if (additionalAttributesConstraints.Count > 0)
        {
          mainStatementBuilder.Append(", ");
          mainStatementBuilder.Append(StringUtils.Join(", ", additionalAttributesConstraints));
        }
        mainStatementBuilder.Append(")");
        command = transaction.CreateCommand();
        command.CommandText = mainStatementBuilder.ToString();
        ServiceScope.Get<ILogger>().Debug("MIA_Management: Creating main table '{0}' for media item aspect '{1}'",
            miaTableName, miam.AspectId);
        command.ExecuteNonQuery();

        string indexName = GenerateDBObjectName(transaction, miam.AspectId, miaTableName + "_PK_IDX", "IDX");
        ServiceScope.Get<ILogger>().Debug("MIA_Management: Creating primary key index '{0}' for media item aspect '{1}'",
            indexName, miam.AspectId);
        command = transaction.CreateCommand();
        command.CommandText = "CREATE INDEX " + indexName + " ON " + miaTableName + "(" + MIA_MEDIA_ITEM_ID_COL_NAME + ")";
        command.ExecuteNonQuery();

        // Attributes: Second run
        foreach (MediaItemAspectMetadata.AttributeSpecification spec in miam.AttributeSpecifications.Values)
        {
          string sqlType = spec.AttributeType == typeof(string) ? database.GetSQLVarLengthStringType(spec.MaxNumChars) :
              database.GetSQLType(spec.AttributeType);
          string attributeColumnName = GetMIAAttributeColumnName(spec); // Name was already generated in previous loop
          switch (spec.Cardinality)
          {
            case Cardinality.Inline:
              if (spec.IsIndexed)
              {
                // Value index
                indexName = GenerateDBObjectName(transaction, miam.AspectId, attributeColumnName + "_IDX", "IDX");
                command = transaction.CreateCommand();
                command.CommandText = "CREATE INDEX " + indexName + " ON " + miaTableName + "(" + attributeColumnName + ")";
                ServiceScope.Get<ILogger>().Debug("MIA_Management: Creating index '{0}' for inline attribute '{1}' in media item aspect '{2}'",
                    indexName, spec.AttributeName, miam.AspectId);
                command.ExecuteNonQuery();
              }
              break;
            case Cardinality.OneToMany:
              // Create foreign table with the join attribute inside
              collectionAttributeTableName = GetMIACollectionAttributeTableName(spec); // Name was already generated in previous loop
              pkConstraintName = GenerateDBObjectName(transaction, miam.AspectId, collectionAttributeTableName + "_PK", "PK");
              string fkMediaItemConstraintName = GenerateDBObjectName(transaction, miam.AspectId, collectionAttributeTableName + "_MEDIA_ITEM_FK", "FK");

              command = transaction.CreateCommand();
              command.CommandText = "CREATE TABLE " + collectionAttributeTableName + " (" +
                  MIA_MEDIA_ITEM_ID_COL_NAME + " " + database.GetSQLType(typeof(Int64)) + ", " +
                  COLL_ATTR_VALUE_COL_NAME + " " + sqlType + ", " +
                  "CONSTRAINT " + pkConstraintName + " PRIMARY KEY (" + MIA_MEDIA_ITEM_ID_COL_NAME + "), " +
                  "CONSTRAINT " + fkMediaItemConstraintName +
                  " FOREIGN KEY (" + MIA_MEDIA_ITEM_ID_COL_NAME + ")" +
                  " REFERENCES " + MediaLibrary_SubSchema.MEDIA_ITEMS_TABLE_NAME + " (" + MediaLibrary_SubSchema.MEDIA_ITEMS_ITEM_ID_COL_NAME + ") ON DELETE CASCADE" +
                  ")";
              ServiceScope.Get<ILogger>().Debug("MIA_Management: Creating OTM table '{0}' for attribute '{1}' in media item aspect '{2}'",
                  collectionAttributeTableName, spec.AttributeName, miam.AspectId);
              command.ExecuteNonQuery();

              // Foreign key index
              indexName = GenerateDBObjectName(transaction, miam.AspectId, collectionAttributeTableName + "_FK_IDX", "IDX");
              command = transaction.CreateCommand();
              command.CommandText = "CREATE INDEX " + indexName + " ON " + collectionAttributeTableName + "(" +
                  MIA_MEDIA_ITEM_ID_COL_NAME + ")";
              ServiceScope.Get<ILogger>().Debug("MIA_Management: Creating foreign key index '{0}' for OTM attribute '{1}' in media item aspect '{2}'",
                  indexName, spec.AttributeName, miam.AspectId);
              command.ExecuteNonQuery();

              if (spec.IsIndexed)
              {
                // Value index
                indexName = GenerateDBObjectName(transaction, miam.AspectId, collectionAttributeTableName + "_VAL_IDX", "IDX");
                command = transaction.CreateCommand();
                command.CommandText = "CREATE INDEX " + indexName + " ON " + collectionAttributeTableName + "(" +
                    COLL_ATTR_VALUE_COL_NAME + ")";
                ServiceScope.Get<ILogger>().Debug("MIA_Management: Creating value index '{0}' for OTM attribute '{1}' in media item aspect '{2}'",
                    indexName, spec.AttributeName, miam.AspectId);
                command.ExecuteNonQuery();
              }
              break;
            case Cardinality.ManyToOne:
              collectionAttributeTableName = GetMIACollectionAttributeTableName(spec); // Name was already generated in previous loop

              if (spec.IsIndexed)
              {
                // Foreign key index
                indexName = GenerateDBObjectName(transaction, miam.AspectId, collectionAttributeTableName + "_FK_IDX", "IDX");
                command = transaction.CreateCommand();
                command.CommandText = "CREATE INDEX " + indexName + " ON " + miaTableName + "(" +
                    attributeColumnName + ")";
                ServiceScope.Get<ILogger>().Debug("MIA_Management: Creating foreign key index '{0}' for MTO attribute '{1}' in media item aspect '{2}'",
                    indexName, spec.AttributeName, miam.AspectId);
                command.ExecuteNonQuery();
              }

              // Value index
              indexName = GenerateDBObjectName(transaction, miam.AspectId, collectionAttributeTableName + "_VAL_IDX", "IDX");
              command = transaction.CreateCommand();
              command.CommandText = "CREATE UNIQUE INDEX " + indexName + " ON " + collectionAttributeTableName + "(" +
                  COLL_ATTR_VALUE_COL_NAME + ")";
              ServiceScope.Get<ILogger>().Debug("MIA_Management: Creating value index '{0}' for MTO attribute '{1}' in media item aspect '{2}'",
                  indexName, spec.AttributeName, miam.AspectId);
              command.ExecuteNonQuery();
              break;
            case Cardinality.ManyToMany:
              // Create foreign table and additional table for the N:M join attributes
              collectionAttributeTableName = GetMIACollectionAttributeTableName(spec); // Name was already generated in previous loop
              pkConstraintName = GenerateDBObjectName(transaction, miam.AspectId, collectionAttributeTableName + "_PK", "PK");
              string nmTableName = GenerateMIACollectionAttributeNMTableName(transaction, spec);
              string pkNMConstraintName = GenerateDBObjectName(transaction, miam.AspectId, nmTableName + "_PK", "PK");
              string fkMainTableConstraintName = GenerateDBObjectName(transaction, miam.AspectId, nmTableName + "_MAIN_FK", "FK");
              string fkForeignTableConstraintName = GenerateDBObjectName(transaction, miam.AspectId, nmTableName + "_FOREIGN_FK", "PK");

              command = transaction.CreateCommand();
              command.CommandText = "CREATE TABLE " + collectionAttributeTableName + " (" +
                  FOREIGN_COLL_ATTR_ID_COL_NAME + " " + database.GetSQLType(typeof(Int64)) + ", " +
                  COLL_ATTR_VALUE_COL_NAME + " " + sqlType + ", " +
                  "CONSTRAINT " + pkConstraintName + " PRIMARY KEY (" + FOREIGN_COLL_ATTR_ID_COL_NAME + ")" +
                  ")";
              ServiceScope.Get<ILogger>().Debug("MIA_Management: Creating MTM value table '{0}' for attribute '{1}' in media item aspect '{2}'",
                  collectionAttributeTableName, spec.AttributeName, miam.AspectId);
              command.ExecuteNonQuery();

              command = transaction.CreateCommand();
              command.CommandText = "CREATE TABLE " + nmTableName + " (" +
                  MIA_MEDIA_ITEM_ID_COL_NAME + " " + database.GetSQLType(typeof(Int64)) + ", " +
                  FOREIGN_COLL_ATTR_ID_COL_NAME + " " + database.GetSQLType(typeof(Int64)) + ", " +
                  "CONSTRAINT " + pkNMConstraintName + " PRIMARY KEY (" + MIA_MEDIA_ITEM_ID_COL_NAME + "," + FOREIGN_COLL_ATTR_ID_COL_NAME + "), " +
                  "CONSTRAINT " + fkMainTableConstraintName + " FOREIGN KEY (" + MIA_MEDIA_ITEM_ID_COL_NAME + ")" +
                  " REFERENCES " + miaTableName + " (" + MediaLibrary_SubSchema.MEDIA_ITEMS_ITEM_ID_COL_NAME + ") ON DELETE CASCADE, " +
                  "CONSTRAINT " + fkForeignTableConstraintName + " FOREIGN KEY (" + FOREIGN_COLL_ATTR_ID_COL_NAME + ")" +
                  " REFERENCES " + collectionAttributeTableName + " (" + FOREIGN_COLL_ATTR_ID_COL_NAME + ") ON DELETE CASCADE" +
                  ")";
              ServiceScope.Get<ILogger>().Debug("MIA_Management: Creating N:M table '{0}' for MTM attribute '{1}' in media item aspect '{2}'",
                  nmTableName, spec.AttributeName, miam.AspectId);
              command.ExecuteNonQuery();

              // Foreign key index to MIA table
              indexName = GenerateDBObjectName(transaction, miam.AspectId, nmTableName + "_MIA_FK_IDX", "IDX");
              command = transaction.CreateCommand();
              command.CommandText = "CREATE INDEX " + indexName + " ON " + nmTableName + "(" +
                  MIA_MEDIA_ITEM_ID_COL_NAME + ")";
              ServiceScope.Get<ILogger>().Debug("MIA_Management: Creating foreign index '{0}' to main MIA table for MTM attribute '{1}' in media item aspect '{2}'",
                  indexName, spec.AttributeName, miam.AspectId);
              command.ExecuteNonQuery();

              // Foreign key index to value table
              indexName = GenerateDBObjectName(transaction, miam.AspectId, nmTableName + "_VAL_FK_IDX", "IDX");
              command = transaction.CreateCommand();
              command.CommandText = "CREATE INDEX " + indexName + " ON " + nmTableName + "(" +
                  FOREIGN_COLL_ATTR_ID_COL_NAME + ")";
              ServiceScope.Get<ILogger>().Debug("MIA_Management: Creating foreign index '{0}' to value table for MTM attribute '{1}' in media item aspect '{2}'",
                  indexName, spec.AttributeName, miam.AspectId);
              command.ExecuteNonQuery();

              if (spec.IsIndexed)
              {
                // Value index
                indexName = GenerateDBObjectName(transaction, miam.AspectId, collectionAttributeTableName + "_VAL_IDX", "IDX");
                command = transaction.CreateCommand();
                command.CommandText = "CREATE INDEX " + indexName + " ON " + collectionAttributeTableName + "(" +
                    COLL_ATTR_VALUE_COL_NAME + ")";
                ServiceScope.Get<ILogger>().Debug("MIA_Management: Creating value index '{0}' for MTM attribute '{1}' in media item aspect '{2}'",
                    indexName, spec.AttributeName, miam.AspectId);
                command.ExecuteNonQuery();
              }
              break;
            default:
              throw new NotImplementedException(string.Format("Cardinality '{0}' for attribute '{1}.{2}' is not implemented",
                  spec.Cardinality, miam.AspectId, spec.AttributeName));
          }
        }
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
      ServiceScope.Get<ILogger>().Info("MIA_Management: Removing media library storage for media item aspect '{0}' (id '{1}')",
          miam.Name, miam.AspectId);
      try
      {
        // We don't remove the name alias mappings from the alias cache because we simply reload the alias cache at the end.
        // We don't remove the name alias mappings from the name aliases table because they are deleted by the DB system
        // (ON DELETE CASCADE).
        IDbCommand command = transaction.CreateCommand();
        string miaTableName = GetMIATableName(miam);

        // Attributes: First run
        foreach (MediaItemAspectMetadata.AttributeSpecification spec in miam.AttributeSpecifications.Values)
        {
          string tableName;
          switch (spec.Cardinality)
          {
            case Cardinality.Inline:
              // No foreign tables to delete
              break;
            case Cardinality.OneToMany:
              tableName = GetMIACollectionAttributeTableName(spec);

              // Foreign attribute value table
              command = transaction.CreateCommand();
              command.CommandText = "DROP TABLE " + tableName;
              ServiceScope.Get<ILogger>().Debug("MIA_Management: Dropping OTM table '{0}' for MTM attribute '{1}' in media item aspect '{2}'",
                  tableName, spec.AttributeName, miam.AspectId);
              command.ExecuteNonQuery();
              break;
            case Cardinality.ManyToOne:
              // After the main table was dropped
              break;
            case Cardinality.ManyToMany:
              tableName = GetMIACollectionAttributeNMTableName(spec);

              // N:M table
              command = transaction.CreateCommand();
              command.CommandText = "DROP TABLE " + tableName;
              ServiceScope.Get<ILogger>().Debug("MIA_Management: Dropping MTM value table '{0}' for attribute '{1}' in media item aspect '{2}'",
                  tableName, spec.AttributeName, miam.AspectId);
              command.ExecuteNonQuery();

              tableName = GetMIACollectionAttributeTableName(spec);

              // Foreign attribute value table
              command = transaction.CreateCommand();
              command.CommandText = "DROP TABLE " + tableName;
              ServiceScope.Get<ILogger>().Debug("MIA_Management: Dropping N:M table '{0}' for MTM attribute '{1}' in media item aspect '{2}'",
                  tableName, spec.AttributeName, miam.AspectId);
              command.ExecuteNonQuery();
              break;
            default:
              throw new NotImplementedException(string.Format("Attribute '{0}.{1}': Cardinality '{2}' is not implemented",
                  aspectId, spec.AttributeName, spec.Cardinality));
          }
        }

        // Main table
        command.CommandText = "DROP TABLE " + miaTableName;
        ServiceScope.Get<ILogger>().Debug("MIA_Management: Dropping main table '{0}' for media item aspect '{1}')",
            miaTableName, miam.AspectId);
        command.ExecuteNonQuery();

        // Attributes: Second run
        foreach (MediaItemAspectMetadata.AttributeSpecification spec in miam.AttributeSpecifications.Values)
        {
          switch (spec.Cardinality)
          {
            case Cardinality.Inline:
              // No foreign tables to delete
              break;
            case Cardinality.OneToMany:
              break;
            case Cardinality.ManyToOne:
              string tableName = GetMIACollectionAttributeTableName(spec);

              // Foreign attribute value table
              command = transaction.CreateCommand();
              command.CommandText = "DROP TABLE " + tableName;
              ServiceScope.Get<ILogger>().Debug("MIA_Management: Dropping MTO value table '{0}' for attribute '{1}' in media item aspect '{2}'",
                  tableName, spec.AttributeName, miam.AspectId);
              command.ExecuteNonQuery();
              break;
            case Cardinality.ManyToMany:
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
      MediaItemAspectMetadata miaType;
      if (!_managedMIATypes.TryGetValue(aspectId, out miaType) || miaType == null)
        throw new ArgumentException(string.Format("MIA_Management: Requested media item aspect type with id '{0}' doesn't exist", aspectId));

      MediaItemAspect result = new MediaItemAspect(miaType);

      Namespace ns = new Namespace();
      string miaTableName = GetMIATableName(miaType);
      IList<string> terms = new List<string>();
      foreach (MediaItemAspectMetadata.AttributeSpecification spec in miaType.AttributeSpecifications.Values)
      {
        switch (spec.Cardinality)
        {
          case Cardinality.Inline:
            string attrName = GetMIAAttributeColumnName(spec);
            terms.Add(attrName + " " + ns.GetOrCreate(spec, "A"));
            break;
          case Cardinality.OneToMany:
            result.SetCollectionAttribute(spec, GetOneToManyMIAAttributeValues(transaction, mediaItemId, spec));
            break;
          case Cardinality.ManyToOne:
            object value = GetManyToOneMIAAttributeValue(transaction, mediaItemId, spec, miaTableName);
            if (!AttributeIsEmpty(value))
              result.SetAttribute(spec, value);
            break;
          case Cardinality.ManyToMany:
            result.SetCollectionAttribute(spec, GetManyToManyMIAAttributeValues(transaction, mediaItemId, spec));
            break;
          default:
            throw new NotImplementedException(string.Format("Cardinality '{0}' for attribute '{1}.{2}' is not implemented",
                spec.Cardinality, miaType.AspectId, spec.AttributeName));
        }
      }
      StringBuilder mainQueryBuilder = new StringBuilder("SELECT ");
      mainQueryBuilder.Append(StringUtils.Join(", ", terms));
      mainQueryBuilder.Append(" FROM ");
      mainQueryBuilder.Append(miaTableName);
      mainQueryBuilder.Append(" WHERE ");
      mainQueryBuilder.Append(MIA_MEDIA_ITEM_ID_COL_NAME);
      mainQueryBuilder.Append(" = ?");
      IDbCommand command = transaction.CreateCommand();
      command.CommandText = mainQueryBuilder.ToString();

      IDbDataParameter param = command.CreateParameter();
      param.Value = mediaItemId;
      command.Parameters.Add(param);

      IDataReader reader = command.ExecuteReader();
      try
      {
        int i = 0;
        if (reader.Read())
          foreach (MediaItemAspectMetadata.AttributeSpecification spec in miaType.AttributeSpecifications.Values)
          {
            if (spec.Cardinality == Cardinality.Inline)
            {
              object value = ReadObject(reader, i, spec);
              if (!AttributeIsEmpty(value))
                result.SetAttribute(spec, value);
            }
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
      MediaItemAspectMetadata miaType;
      if (!_managedMIATypes.TryGetValue(mia.Metadata.AspectId, out miaType) || miaType == null)
        throw new ArgumentException(string.Format("MIA_Management: Requested media item aspect type with id '{0}' doesn't exist",
          mia.Metadata.AspectId));

      IList<string> terms = new List<string>();
      IList<object> sqlValues = new List<object>();
      string miaTableName = GetMIATableName(miaType);

      // Attributes: First run
      foreach (MediaItemAspectMetadata.AttributeSpecification spec in miaType.AttributeSpecifications.Values)
      {
        if (mia.IsIgnore(spec))
          break;
        string attrColName;
        object attributeValue;
        switch (spec.Cardinality)
        {
          case Cardinality.Inline:
            attrColName = GetMIAAttributeColumnName(spec);
            if (insert)
              terms.Add(attrColName);
            else
              terms.Add(attrColName + " = ?");
            attributeValue = mia.GetAttributeValue(spec);
            sqlValues.Add(AttributeIsEmpty(attributeValue) ? null : attributeValue);
            break;
          case Cardinality.OneToMany:
            // After main query
            break;
          case Cardinality.ManyToOne:
            attrColName = GetMIAAttributeColumnName(spec);
            attributeValue = mia.GetAttributeValue(spec);
            Int64? insertValue;
            if (AttributeIsEmpty(attributeValue))
              insertValue = null;
            else
            {
              Int64 valuePk;
              GetOrCreateManyToOneMIAAttributeValue(transaction, spec, mediaItemId, mia.GetAttributeValue(spec), insert, out valuePk);
              insertValue = valuePk;
            }
            if (insert)
              terms.Add(attrColName);
            else
              terms.Add(attrColName + " = ?");
            if (insertValue.HasValue)
              sqlValues.Add(insertValue.Value);
            else
              sqlValues.Add(null);
            break;
          case Cardinality.ManyToMany:
            // After main query
            break;
          default:
            throw new NotImplementedException(string.Format("Cardinality '{0}' for attribute '{1}.{2}' is not implemented",
                spec.Cardinality, miaType.AspectId, spec.AttributeName));
        }
      }
      // Main query
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
      IDbCommand command = transaction.CreateCommand();
      command.CommandText = mainQueryBuilder.ToString();

      foreach (object value in sqlValues)
      {
        IDbDataParameter param = command.CreateParameter();
        param.Value = value;
        command.Parameters.Add(param);
      }

      command.ExecuteNonQuery();

      // Attributes: Second run
      foreach (MediaItemAspectMetadata.AttributeSpecification spec in miaType.AttributeSpecifications.Values)
      {
        if (mia.IsIgnore(spec))
          break;
        switch (spec.Cardinality)
        {
          case Cardinality.Inline:
            break;
          case Cardinality.OneToMany:
            InsertOrUpdateOneToManyMIAAttributeValues(transaction, spec, mediaItemId, mia.GetCollectionAttribute(spec), insert);
            break;
          case Cardinality.ManyToOne:
            break;
          case Cardinality.ManyToMany:
            InsertOrUpdateManyToManyMIAAttributeValues(transaction, spec, mediaItemId, mia.GetCollectionAttribute(spec), insert);
            break;
          default:
            throw new NotImplementedException(string.Format("Cardinality '{0}' for attribute '{1}.{2}' is not implemented",
                spec.Cardinality, miaType.AspectId, spec.AttributeName));
        }
      }

      CleanupAllManyToOneOrphanedAttributeValues(transaction, miaType);
    }

    public void AddOrUpdateMIA(ITransaction transaction, Int64 mediaItemId, MediaItemAspect mia)
    {
      InsertOrUpdateMIA(transaction, mediaItemId, mia, !MIAExists(transaction, mediaItemId, mia.Metadata.AspectId));
    }

    public bool RemoveMIA(ITransaction transaction, Int64 mediaItemId, Guid aspectId)
    {
      MediaItemAspectMetadata miaType;
      if (!_managedMIATypes.TryGetValue(aspectId, out miaType) || miaType == null)
        throw new ArgumentException(string.Format("MIA_Management: Requested media item aspect type with id '{0}' doesn't exist", aspectId));
      string miaTableName = GetMIATableName(miaType);

      // Cleanup/delete attribute value entries
      foreach (MediaItemAspectMetadata.AttributeSpecification spec in miaType.AttributeSpecifications.Values)
      {
        switch (spec.Cardinality)
        {
          case Cardinality.Inline:
            break;
          case Cardinality.OneToMany:
            DeleteOneToManyAttributeValuesNotInEnumeration(transaction, spec, mediaItemId, new object[] {});
            break;
          case Cardinality.ManyToOne:
            break;
          case Cardinality.ManyToMany:
            DeleteManyToManyAttributeAssociationsNotInEnumeration(transaction, spec, mediaItemId, new object[] {});
            CleanupManyToManyOrphanedAttributeValues(transaction, spec);
            break;
          default:
            throw new NotImplementedException(string.Format("Cardinality '{0}' for attribute '{1}.{2}' is not implemented",
                spec.Cardinality, miaType.AspectId, spec.AttributeName));
        }
      }
      // Delete main MIA entry
      IDbCommand command = transaction.CreateCommand();
      command.CommandText = "DELETE FROM " + miaTableName + " WHERE " + MIA_MEDIA_ITEM_ID_COL_NAME + " = ?";

      IDbDataParameter param = command.CreateParameter();
      param.Value = mediaItemId;
      command.Parameters.Add(param);

      bool result = command.ExecuteNonQuery() > 0;
      CleanupAllManyToOneOrphanedAttributeValues(transaction, miaType);
      return result;
    }

    #endregion
  }
}
