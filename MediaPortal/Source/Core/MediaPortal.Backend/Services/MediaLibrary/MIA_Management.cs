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
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading;
using MediaPortal.Backend.Services.Database;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Backend.Database;
using MediaPortal.Backend.Services.MediaLibrary.QueryEngine;
using MediaPortal.Utilities;
using MediaPortal.Utilities.DB;
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
  /// All ID columns (and foreign key columns referencing to those ID columns) are of type <see cref="Guid"/>.
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
    /// ID column name which is foreign key referencing the ID in the main media item's table.
    /// This column name is used both for each MIA main table and for MIA attribute collection tables.
    /// </summary>
    protected internal const string MIA_MEDIA_ITEM_ID_COL_NAME = "MEDIA_ITEM_ID";

    /// <summary>
    /// ID column name for MIA collection attribute tables
    /// </summary>
    protected internal const string FOREIGN_COLL_ATTR_ID_COL_NAME = "VALUE_ID";

    /// <summary>
    /// Value column name for MIA collection attribute tables.
    /// </summary>
    protected internal const string COLL_ATTR_VALUE_COL_NAME = "VALUE";

    /// <summary>
    /// Value order column name for MIA collection attribute tables.
    /// </summary>
    protected internal const string COLL_ATTR_VALUE_ORDER_COL_NAME = "VALUE_ORDER";

    protected readonly IDictionary<string, string> _nameAliases = new Dictionary<string, string>();

    /// <summary>
    /// Caches all media item aspect types the database is able to store. A MIA type which is currently being
    /// added or removed will have a <c>null</c> value assigned to its ID.
    /// </summary>
    protected readonly IDictionary<Guid, MediaItemAspectMetadata> _managedMIATypes = new Dictionary<Guid, MediaItemAspectMetadata>();

    /// <summary>
    /// Caches the creation dates of all managed MIAs.
    /// </summary>
    protected IDictionary<Guid, DateTime> _MIACreationDates = new ConcurrentDictionary<Guid, DateTime>();

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
      LoadMIACreationDateCache();
    }

    #region Table name generation and alias management

    protected void ReloadAliasCache()
    {
      lock (_syncObj)
      {
        ISQLDatabase database = ServiceRegistration.Get<ISQLDatabase>();
        ITransaction transaction = database.BeginTransaction();
        try
        {
          int miamIdIndex;
          int identifierIndex;
          int dbObjectNameIndex;
          using (IDbCommand command = MediaLibrary_SubSchema.SelectMIANameAliasesCommand(transaction, out miamIdIndex,
              out identifierIndex, out dbObjectNameIndex))
          {
            _nameAliases.Clear();
            using (IDataReader reader = command.ExecuteReader())
              while (reader.Read())
              {
                string identifier = database.ReadDBValue<string>(reader, identifierIndex);
                string dbObjectName = database.ReadDBValue<string>(reader, dbObjectNameIndex);
                _nameAliases.Add(identifier, dbObjectName);
              }
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
    /// <param name="allowExisting">Allow same name to exist in cache. This usually only true for having the same column
    /// name in multiple tables.
    /// <returns>Table name corresponding to the specified table identifier.</returns>
    private string GenerateDBObjectName(ITransaction transaction, Guid aspectId, string objectIdentifier, string desiredName, bool allowExisting = false)
    {
      lock (_syncObj)
      {
        if (_nameAliases.ContainsKey(objectIdentifier))
          throw new InvalidDataException("Table identifier '{0}' is already present in alias cache", objectIdentifier);
        ISQLDatabase database = ServiceRegistration.Get<ISQLDatabase>();
        uint maxLen = database.MaxObjectNameLength;
        uint ct = 0;
        string result;
        desiredName = SqlUtils.ToSQLIdentifier(desiredName);
        if (!NamePresent(desiredName) || allowExisting)
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
      return _nameAliases.Values.Any(objectName => dbObjectName == objectName);
    }

    protected void AddNameAlias(ITransaction transaction, Guid aspectId, string objectIdentifier, string dbObjectName)
    {
      using (IDbCommand command = MediaLibrary_SubSchema.CreateMIANameAliasCommand(transaction, aspectId, objectIdentifier, dbObjectName))
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
      return GenerateDBObjectName(transaction, spec.ParentMIAM.AspectId, identifier, spec.AttributeName, true);
    }

    internal string GenerateMIACollectionAttributeTableName(ITransaction transaction, MediaItemAspectMetadata.AttributeSpecification spec)
    {
      string identifier = GetMIACollectionAttributeTableIdentifier(spec);
      return GenerateDBObjectName(transaction, spec.ParentMIAM.AspectId, identifier, "V_" + spec.ParentMIAM.Name + "_" + spec.AttributeName);
    }

    internal string GenerateMIACollectionAttributeNMTableName(ITransaction transaction, MediaItemAspectMetadata.AttributeSpecification spec)
    {
      string identifier = GetMIACollectionAttributeNMTableIdentifier(spec);
      return GenerateDBObjectName(transaction, spec.ParentMIAM.AspectId, identifier, "NM_" + spec.ParentMIAM.Name + "_" + spec.AttributeName);
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
      ISQLDatabase database = ServiceRegistration.Get<ISQLDatabase>();
      ITransaction transaction = database.BeginTransaction();
      try
      {
        int miamIdIndex;
        int serializationIndex;
        using (IDbCommand command = MediaLibrary_SubSchema.SelectAllMediaItemAspectMetadataCommand(
            transaction, out miamIdIndex, out serializationIndex))
        using (IDataReader reader = command.ExecuteReader())
        {
          IDictionary<Guid, string> result = new Dictionary<Guid, string>();
          while (reader.Read())
            result.Add(database.ReadDBValue<Guid>(reader, miamIdIndex),
                database.ReadDBValue<string>(reader, serializationIndex));
          return result;
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
      {
        try
        {
          result.Add(MediaItemAspectMetadata.Deserialize(serialization));
        }
        catch (Exception ex)
        {
          ServiceRegistration.Get<ILogger>().Warn("MIA Management: Skipping incompatible MediaItemAspectMetadata: {0} ({1})", ex.Message, serialization.Substring(0, 50));
        }
      }
      return result;
    }

    protected void LoadMIACreationDateCache()
    {
      Interlocked.Exchange(ref _MIACreationDates, SelectAllMediaItemAspectMetadataCreationDates());
    }

    protected static IDictionary<Guid, DateTime> SelectAllMediaItemAspectMetadataCreationDates()
    {
      var database = ServiceRegistration.Get<ISQLDatabase>();
      var transaction = database.BeginTransaction();
      try
      {
        int miamIdIndex;
        int creationDateIndex;
        using (var command = MediaLibrary_SubSchema.SelectAllMediaItemAspectMetadataCreationDatesCommand(transaction, out miamIdIndex, out creationDateIndex))
        using (var reader = command.ExecuteReader())
        {
          IDictionary<Guid, DateTime> result = new ConcurrentDictionary<Guid, DateTime>();
          while (reader.Read())
            result.Add(database.ReadDBValue<Guid>(reader, miamIdIndex),
                database.ReadDBValue<DateTime>(reader, creationDateIndex));
          return result;
        }
      }
      finally
      {
        transaction.Dispose();
      }      
    }

    protected static object TruncateBigValue(object value, MediaItemAspectMetadata.AttributeSpecification attributeSpecification)
    {
      string str = value as string;
      uint maxNumChars = attributeSpecification.MaxNumChars;
      if (!string.IsNullOrEmpty(str) && maxNumChars > 0 && str.Length > maxNumChars)
        return str.Substring(0, (int) maxNumChars);
      return value;
    }

    protected IList GetOneToManyMIAAttributeValues(ITransaction transaction, Guid mediaItemId,
        MediaItemAspectMetadata.AttributeSpecification spec)
    {
      string collectionAttributeTableName = GetMIACollectionAttributeTableName(spec);
      ISQLDatabase database = transaction.Database;
      using (IDbCommand command = transaction.CreateCommand())
      {
        command.CommandText = "SELECT " + COLL_ATTR_VALUE_COL_NAME + " FROM " + collectionAttributeTableName + " WHERE " +
            MIA_MEDIA_ITEM_ID_COL_NAME + " = @MEDIA_ITEM_ID" +
            " ORDER BY " + COLL_ATTR_VALUE_ORDER_COL_NAME;
        database.AddParameter(command, "MEDIA_ITEM_ID", mediaItemId, typeof(Guid));

        Type valueType = spec.AttributeType;
        using (IDataReader reader = command.ExecuteReader())
        {
          IList result = new ArrayList();
          while (reader.Read())
            result.Add(database.ReadDBValue(valueType, reader, 0));
          return result;
        }
      }
    }

    protected object GetManyToOneMIAAttributeValue(ITransaction transaction, Guid mediaItemId,
        MediaItemAspectMetadata.AttributeSpecification spec, string miaTableName)
    {
      string collectionAttributeTableName = GetMIACollectionAttributeTableName(spec);
      string mainTableAttrName = GetMIAAttributeColumnName(spec);
      ISQLDatabase database = transaction.Database;
      using (IDbCommand command = transaction.CreateCommand())
      {
        command.CommandText = "SELECT " + COLL_ATTR_VALUE_COL_NAME + " FROM " + collectionAttributeTableName + " V" +
            " INNER JOIN " + miaTableName + " MAIN ON V." + FOREIGN_COLL_ATTR_ID_COL_NAME + " = MAIN." + mainTableAttrName +
            " WHERE MAIN." + MIA_MEDIA_ITEM_ID_COL_NAME + " = @MEDIA_ITEM_ID" +
            " ORDER BY " + "V." + COLL_ATTR_VALUE_ORDER_COL_NAME;

        database.AddParameter(command, "MEDIA_ITEM_ID", mediaItemId, typeof(Guid));

        Type valueType = spec.AttributeType;
        using (IDataReader reader = command.ExecuteReader(CommandBehavior.SingleRow))
        {
          if (reader.Read())
            return database.ReadDBValue(valueType, reader, 0);
          return null;
        }
      }
    }

    protected IList GetManyToManyMIAAttributeValues(ITransaction transaction, Guid mediaItemId,
        MediaItemAspectMetadata.AttributeSpecification spec)
    {
      string collectionAttributeTableName = GetMIACollectionAttributeTableName(spec);
      string nmTableName = GenerateMIACollectionAttributeNMTableName(transaction, spec);
      ISQLDatabase database = transaction.Database;
      using (IDbCommand command = transaction.CreateCommand())
      {
        command.CommandText = "SELECT " + COLL_ATTR_VALUE_COL_NAME + " FROM " + collectionAttributeTableName + " V" +
            " INNER JOIN " + nmTableName + " NM ON V." + FOREIGN_COLL_ATTR_ID_COL_NAME + " = NM." + FOREIGN_COLL_ATTR_ID_COL_NAME +
            " WHERE NM." + MIA_MEDIA_ITEM_ID_COL_NAME + " = @MEDIA_ITEM_ID" +
            " ORDER BY " + "V." + COLL_ATTR_VALUE_ORDER_COL_NAME;

        database.AddParameter(command, "MEDIA_ITEM_ID", mediaItemId, typeof(Guid));

        Type valueType = spec.AttributeType;
        using (IDataReader reader = command.ExecuteReader())
        {
          IList result = new ArrayList();
          while (reader.Read())
            result.Add(database.ReadDBValue(valueType, reader, 0));
          return result;
        }
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

    // If called with values == null, all values will be deleted for the given spec/mediaItemId
    protected void DeleteOneToManyAttributeValuesNotInEnumeration(ITransaction transaction,
        MediaItemAspectMetadata.AttributeSpecification spec, Guid mediaItemId, IEnumerable values)
    {
      string collectionAttributeTableName = GetMIACollectionAttributeTableName(spec);

      ISQLDatabase database = transaction.Database;
      using (IDbCommand command = transaction.CreateCommand())
      {
        string commandText = "DELETE FROM " + collectionAttributeTableName + " WHERE " + MIA_MEDIA_ITEM_ID_COL_NAME + " = @MEDIA_ITEM_ID";
        database.AddParameter(command, "MEDIA_ITEM_ID", mediaItemId, typeof(Guid));

        if (values != null)
        {
          IList<string> bindVars = new List<string>();
          int ct = 0;
          foreach (object value in values)
          {
            string bindVar = "V" + ct++;
            database.AddParameter(command, bindVar, value, spec.AttributeType);
            bindVars.Add("@" + bindVar);
          }
          commandText += " AND " + COLL_ATTR_VALUE_COL_NAME + " NOT IN(" +
              StringUtils.Join(", ", bindVars) + ")";
        }
        command.CommandText = commandText;
        command.ExecuteNonQuery();
      }
    }

    protected void InsertOrUpdateOneToManyMIAAttributeValues(ITransaction transaction,
        MediaItemAspectMetadata.AttributeSpecification spec, Guid mediaItemId, IEnumerable values, bool insert)
    {
      string collectionAttributeTableName = GetMIACollectionAttributeTableName(spec);
      if (!insert)
        // Delete old entries
        DeleteOneToManyAttributeValuesNotInEnumeration(transaction, spec, mediaItemId, values);

      ISQLDatabase database = transaction.Database;
      IDatabaseManager databaseManager = ServiceRegistration.Get<IDatabaseManager>();
      // Add new entries - commands for insert and update are the same here
      int order = 0;
      foreach (object value in values)
      {
        using (IDbCommand command = transaction.CreateCommand())
        {
          command.CommandText = "INSERT INTO " + collectionAttributeTableName + "(" +
              MIA_MEDIA_ITEM_ID_COL_NAME + ", " + COLL_ATTR_VALUE_COL_NAME + ", " + COLL_ATTR_VALUE_ORDER_COL_NAME + 
              ") SELECT @MEDIA_ITEM_ID, @COLL_ATTR_VALUE, " + order++ + " FROM " + databaseManager.DummyTableName +
              " WHERE NOT EXISTS(SELECT " + MIA_MEDIA_ITEM_ID_COL_NAME + " FROM " + collectionAttributeTableName + " WHERE " +
              MIA_MEDIA_ITEM_ID_COL_NAME + " = @MEDIA_ITEM_ID AND " + COLL_ATTR_VALUE_COL_NAME + " = @COLL_ATTR_VALUE)";

          database.AddParameter(command, "MEDIA_ITEM_ID", mediaItemId, typeof(Guid)); // Used twice in query
          object writeValue = TruncateBigValue(value, spec);
          database.AddParameter(command, "COLL_ATTR_VALUE", writeValue, spec.AttributeType); // Used twice in query

          command.ExecuteNonQuery();
        }
      }
    }

    public void CleanupAllOrphanedAttributeValues(ITransaction transaction)
    {
      foreach (MediaItemAspectMetadata miaType in ManagedMediaItemAspectTypes.Values)
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
              CleanupManyToManyOrphanedAttributeValues(transaction, spec);
              break;
            default:
              throw new NotImplementedException(string.Format("Cardinality '{0}' for attribute '{1}.{2}' is not implemented",
                  spec.Cardinality, spec.ParentMIAM.AspectId, spec.AttributeName));
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

      using (IDbCommand command = transaction.CreateCommand())
      {
        command.CommandText = "DELETE FROM " + collectionAttributeTableName + " WHERE NOT EXISTS (" +
            "SELECT " + MIA_MEDIA_ITEM_ID_COL_NAME + " FROM " + miaTableName + " MIA WHERE MIA." +
            attrColName + " = " + collectionAttributeTableName + "." + FOREIGN_COLL_ATTR_ID_COL_NAME + ")";

        command.ExecuteNonQuery();
      }
    }

    protected void GetOrCreateManyToOneMIAAttributeValue(ITransaction transaction,
        MediaItemAspectMetadata.AttributeSpecification spec, object value, bool insert, out Guid valuePk)
    {
      string collectionAttributeTableName = GetMIACollectionAttributeTableName(spec);
      ISQLDatabase database = transaction.Database;

      LockAttribute(spec);
      try
      {
        using (IDbCommand command = transaction.CreateCommand())
        {
          // First check if value already exists...
          command.CommandText = "SELECT " + FOREIGN_COLL_ATTR_ID_COL_NAME + " FROM " + collectionAttributeTableName +
              " WHERE " + COLL_ATTR_VALUE_COL_NAME + " = @COLL_ATTR_VALUE";

          database.AddParameter(command, "COLL_ATTR_VALUE", value, spec.AttributeType);

          using (IDataReader reader = command.ExecuteReader())
          {
            if (reader.Read())
            {
              valuePk = database.ReadDBValue<Guid>(reader, 0);
              return;
            }
          }

          // ... if not, insert it
          valuePk = Guid.NewGuid();
          command.CommandText = "INSERT INTO " + collectionAttributeTableName + " (" +
              FOREIGN_COLL_ATTR_ID_COL_NAME + ", " + COLL_ATTR_VALUE_COL_NAME + ", " + COLL_ATTR_VALUE_ORDER_COL_NAME +
              ") VALUES (@FOREIGN_COLL_ATTR_ID, @COLL_ATTR_VALUE, 0)";

          database.AddParameter(command, "FOREIGN_COLL_ATTR_ID", valuePk, typeof(Guid));

          command.ExecuteNonQuery();
        }
      }
      finally
      {
        UnlockAttribute(spec);
      }
    }

    protected void DeleteManyToManyAttributeAssociationsNotInEnumeration(ITransaction transaction,
        MediaItemAspectMetadata.AttributeSpecification spec, Guid mediaItemId, IEnumerable values)
    {
      string collectionAttributeTableName = GetMIACollectionAttributeTableName(spec);
      string nmTableName = GetMIACollectionAttributeNMTableName(spec);

      ISQLDatabase database = transaction.Database;
      using (IDbCommand command = transaction.CreateCommand())
      {
        database.AddParameter(command, "MEDIA_ITEM_ID", mediaItemId, typeof(Guid));

        IList<string> bindVars = new List<string>();
        int ct = 0;
        if (values != null)
          foreach (object value in values)
          {
            string bindVar = "V" + ct++;
            bindVars.Add("@" + bindVar);
            database.AddParameter(command, bindVar, value, spec.AttributeType);
          }
        string commandText = "DELETE FROM " + nmTableName + " WHERE " + MIA_MEDIA_ITEM_ID_COL_NAME + " = @MEDIA_ITEM_ID";

        if (bindVars.Count > 0)
          commandText += " AND NOT EXISTS(" +
              "SELECT " + FOREIGN_COLL_ATTR_ID_COL_NAME + " FROM " + collectionAttributeTableName + " V WHERE V." +
              FOREIGN_COLL_ATTR_ID_COL_NAME + " = " + nmTableName + "." + FOREIGN_COLL_ATTR_ID_COL_NAME +
              " AND " + COLL_ATTR_VALUE_COL_NAME + " IN (" + StringUtils.Join(", ", bindVars) + "))";
        command.CommandText = commandText;
        command.ExecuteNonQuery();
      }
    }

    protected void CleanupManyToManyOrphanedAttributeValues(ITransaction transaction,
        MediaItemAspectMetadata.AttributeSpecification spec)
    {
      string collectionAttributeTableName = GetMIACollectionAttributeTableName(spec);
      string nmTableName = GetMIACollectionAttributeNMTableName(spec);

      using (IDbCommand command = transaction.CreateCommand())
      {
        command.CommandText = "DELETE FROM " + collectionAttributeTableName + " WHERE NOT EXISTS (" +
            "SELECT " + FOREIGN_COLL_ATTR_ID_COL_NAME + " FROM " + nmTableName + " NM WHERE " +
            FOREIGN_COLL_ATTR_ID_COL_NAME + " = " + collectionAttributeTableName + "." + FOREIGN_COLL_ATTR_ID_COL_NAME + ")";

        command.ExecuteNonQuery();
      }
    }

    protected void InsertOrUpdateManyToManyMIAAttributeValue(ITransaction transaction,
        MediaItemAspectMetadata.AttributeSpecification spec, Guid mediaItemId, object value, int order)
    {
      string collectionAttributeTableName = GetMIACollectionAttributeTableName(spec);
      IDatabaseManager databaseManager = ServiceRegistration.Get<IDatabaseManager>();
      ISQLDatabase database = transaction.Database;
      // Insert value into collection attribute table if not exists: We do it in a single statement to avoid rountrips to the DB
      using (IDbCommand command = transaction.CreateCommand())
      {
        command.CommandText = "INSERT INTO " + collectionAttributeTableName + " (" +
            FOREIGN_COLL_ATTR_ID_COL_NAME + ", " + COLL_ATTR_VALUE_COL_NAME + ", " + COLL_ATTR_VALUE_ORDER_COL_NAME +
            ") SELECT @FOREIGN_COLL_ATTR, @COLL_ATTR_VALUE, " + order + " FROM " +
            databaseManager.DummyTableName + " WHERE NOT EXISTS(SELECT " + FOREIGN_COLL_ATTR_ID_COL_NAME +
            " FROM " + collectionAttributeTableName + " WHERE " + COLL_ATTR_VALUE_COL_NAME + " = @COLL_ATTR_VALUE)";

        database.AddParameter(command, "FOREIGN_COLL_ATTR", Guid.NewGuid(), typeof(Guid));
        value = TruncateBigValue(value, spec);
        database.AddParameter(command, "COLL_ATTR_VALUE", value, spec.AttributeType); // Used twice in query

        command.ExecuteNonQuery();
      }

      // Check association: We do it here with a single statement to avoid roundtrips to the DB
      string nmTableName = GetMIACollectionAttributeNMTableName(spec);
      using (IDbCommand command = transaction.CreateCommand())
      {
        command.CommandText = "INSERT INTO " + nmTableName + " (" + MIA_MEDIA_ITEM_ID_COL_NAME + ", " + FOREIGN_COLL_ATTR_ID_COL_NAME +
            ") SELECT @MEDIA_ITEM_ID, " + FOREIGN_COLL_ATTR_ID_COL_NAME + " FROM " + collectionAttributeTableName +
            " WHERE " + COLL_ATTR_VALUE_COL_NAME + " = @COLL_ATTR_VALUE AND NOT EXISTS(" +
              "SELECT V." + FOREIGN_COLL_ATTR_ID_COL_NAME + " FROM " + collectionAttributeTableName + " V " +
              " INNER JOIN " + nmTableName + " NM ON V." + FOREIGN_COLL_ATTR_ID_COL_NAME + " = NM." + FOREIGN_COLL_ATTR_ID_COL_NAME +
              " WHERE V." + COLL_ATTR_VALUE_COL_NAME + " = @COLL_ATTR_VALUE AND NM." + MIA_MEDIA_ITEM_ID_COL_NAME + " = @MEDIA_ITEM_ID" +
            ")";

        database.AddParameter(command, "MEDIA_ITEM_ID", mediaItemId, typeof(Guid)); // Used twice in query
        database.AddParameter(command, "COLL_ATTR_VALUE", value, spec.AttributeType); // Used twice in query

        command.ExecuteNonQuery();
      }
    }

    protected void InsertOrUpdateManyToManyMIAAttributeValues(ITransaction transaction,
        MediaItemAspectMetadata.AttributeSpecification spec, Guid mediaItemId, IEnumerable values, bool insert)
    {
      LockAttribute(spec);
      try
      {
        if (!insert)
          DeleteManyToManyAttributeAssociationsNotInEnumeration(transaction, spec, mediaItemId, values);
        if (values != null)
        {
          int order = 0;
          foreach (object value in values)
            InsertOrUpdateManyToManyMIAAttributeValue(transaction, spec, mediaItemId, value, order++);
        }
        if (!insert)
          CleanupManyToManyOrphanedAttributeValues(transaction, spec);
      }
      finally
      {
        UnlockAttribute(spec);
      }
    }

    protected object ReadObject(ISQLDatabase database, IDataReader reader, int colIndex, MediaItemAspectMetadata.AttributeSpecification spec)
    {
      // Because the IDataReader interface doesn't provide a getter method which takes the desired return type,
      // we have to write this method
      Type type = spec.AttributeType;
      try
      {
        return database.ReadDBValue(type, reader, colIndex);
      }
      catch (ArgumentException)
      {
        throw new NotSupportedException(string.Format(
            "The datatype '{0}' of attribute '{1}' in media item aspect type '{2}' (id '{3}') is not supported", type, spec.AttributeName, spec.ParentMIAM.Name, spec.ParentMIAM.AspectId));
      }
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

    public IDictionary<Guid, DateTime> ManagedMediaItemAspectCreationDates
    {
      get
      {
        return new Dictionary<Guid, DateTime>(_MIACreationDates);
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

    public bool AddMediaItemAspectStorage(MediaItemAspectMetadata miam)
    {
      if (miam.IsTransientAspect)
        return true;

      lock (_syncObj)
      {
        if (_managedMIATypes.ContainsKey(miam.AspectId))
          return false;
        _managedMIATypes.Add(miam.AspectId, null);
      }
      ISQLDatabase database = ServiceRegistration.Get<ISQLDatabase>();
      ITransaction transaction = database.BeginTransaction();
      ServiceRegistration.Get<ILogger>().Info("MIA_Management: Adding media library storage for media item aspect '{0}' (id '{1}')",
          miam.Name, miam.AspectId);
      try
      {
        // Register metadata first - generated aliases will reference to the new MIA type row
        using (IDbCommand command = MediaLibrary_SubSchema.CreateMediaItemAspectMetadataCommand(transaction, miam.AspectId, miam.Name, miam.Serialize()))
          command.ExecuteNonQuery();

        // Create main table for new MIA type
        string miaTableName = GenerateMIATableName(transaction, miam);
        StringBuilder mainStatementBuilder = new StringBuilder("CREATE TABLE " + miaTableName + " (" +
            MIA_MEDIA_ITEM_ID_COL_NAME + " " + database.GetSQLType(typeof(Guid)) + ", ");
        IList<string> terms = new List<string>();
        IList<string> keyColumns = new List<string>();
        IList<string> additionalAttributesConstraints = new List<string>();
        string collectionAttributeTableName;
        string pkConstraintName;

        keyColumns.Add(MIA_MEDIA_ITEM_ID_COL_NAME);

        // Attributes: First run
        foreach (MediaItemAspectMetadata.AttributeSpecification spec in miam.AttributeSpecifications.Values)
        {
          string sqlType = spec.AttributeType == typeof(string) ? database.GetSQLVarLengthStringType(spec.MaxNumChars) :
              database.GetSQLType(spec.AttributeType);
          string attributeColumnName = GenerateMIAAttributeColumnName(transaction, spec);
          string attributeColumnIdentifier = GetMIAAttributeColumnIdentifier(spec);
          switch (spec.Cardinality)
          {
            case Cardinality.Inline:
              terms.Add(attributeColumnName + " " + sqlType);
              if (miam is MultipleMediaItemAspectMetadata && ((MultipleMediaItemAspectMetadata)miam).UniqueAttributeSpecifications.Values.Contains(spec))
                keyColumns.Add(attributeColumnName);
              break;
            case Cardinality.OneToMany:
              GenerateMIACollectionAttributeTableName(transaction, spec);
              break;
            case Cardinality.ManyToOne:
              // Create foreign table - the join attribute will be located in the main MIA table
              // We need to create the "One" table first because the main table references on it
              collectionAttributeTableName = GenerateMIACollectionAttributeTableName(transaction, spec);
              pkConstraintName = GenerateDBObjectName(transaction, miam.AspectId, attributeColumnIdentifier + "_PK", "PK");

              using (IDbCommand command = transaction.CreateCommand())
              {
                command.CommandText = "CREATE TABLE " + collectionAttributeTableName + " (" +
                    FOREIGN_COLL_ATTR_ID_COL_NAME + " " + database.GetSQLType(typeof(Guid)) + ", " +
                    COLL_ATTR_VALUE_COL_NAME + " " + sqlType + ", " +
                    COLL_ATTR_VALUE_ORDER_COL_NAME + " " + database.GetSQLType(typeof(int)) + ", " +
                    "CONSTRAINT " + pkConstraintName + " PRIMARY KEY (" + FOREIGN_COLL_ATTR_ID_COL_NAME + ")" +
                    ")";
                ServiceRegistration.Get<ILogger>().Debug("MIA_Management: Creating MTO table '{0}' for attribute '{1}' in media item aspect '{2}'",
                    collectionAttributeTableName, spec.AttributeName, miam.AspectId);
                command.ExecuteNonQuery();
              }

              // Create foreign table - the join attribute will be located in the main MIA table
              string fkMediaItemConstraintName = GenerateDBObjectName(transaction, miam.AspectId, attributeColumnIdentifier + "_FK", "FK");

              terms.Add(attributeColumnName + " " + database.GetSQLType(typeof(Guid)));
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

        //Add dependency if any
        bool foreignKeyAdded = false;
        SingleMediaItemAspectMetadata smiam = miam as SingleMediaItemAspectMetadata;
        MultipleMediaItemAspectMetadata mmiam = miam as MultipleMediaItemAspectMetadata;
        if ((smiam != null && smiam.ReferencingAspectId.HasValue) || (mmiam != null && mmiam.ReferencingAspectId.HasValue))
        {
          MediaItemAspectMetadata.AttributeSpecification[] fkSpecifications = null;
          MediaItemAspectMetadata refMiam = null;
          MediaItemAspectMetadata.AttributeSpecification[] refSpecifications = null;
          if (smiam != null && smiam.ReferencingAspectId.HasValue && _managedMIATypes.ContainsKey(smiam.ReferencingAspectId.Value))
          {
            fkSpecifications = smiam.ReferencedAttributeSpecifications.Values.Count > 0 ? smiam.ReferencedAttributeSpecifications.Values.ToArray() : null;
            refMiam = _managedMIATypes[smiam.ReferencingAspectId.Value];
            if (refMiam is MultipleMediaItemAspectMetadata && ((MultipleMediaItemAspectMetadata)refMiam).UniqueAttributeSpecifications.Values.Count > 0)
              refSpecifications = ((MultipleMediaItemAspectMetadata)refMiam).UniqueAttributeSpecifications.Values.ToArray();
          }
          else if (mmiam != null && mmiam.ReferencingAspectId.HasValue && _managedMIATypes.ContainsKey(mmiam.ReferencingAspectId.Value))
          {
            fkSpecifications = mmiam.ReferencedAttributeSpecifications.Values.Count > 0 ? mmiam.ReferencedAttributeSpecifications.Values.ToArray() : null;
            refMiam = _managedMIATypes[mmiam.ReferencingAspectId.Value];
            if (refMiam is MultipleMediaItemAspectMetadata && ((MultipleMediaItemAspectMetadata)refMiam).UniqueAttributeSpecifications.Values.Count > 0)
              refSpecifications = ((MultipleMediaItemAspectMetadata)refMiam).UniqueAttributeSpecifications.Values.ToArray();
          }
          if (refMiam != null)
          {
            string refMiaTableName = GetMIATableName(refMiam);
            string fkDependencyMediaItemConstraintName = GenerateDBObjectName(transaction, miam.AspectId, miaTableName + "_" + refMiaTableName + "_FK", "FK");
            List<string> fkColumns = new List<string>(new string[] { MediaLibrary_SubSchema.MEDIA_ITEMS_ITEM_ID_COL_NAME });
            if(fkSpecifications != null)
              fkColumns.AddRange(fkSpecifications.Select(s => GetMIAAttributeColumnName(s)));
            List<string> refColumns = new List<string>(new string[] { MediaLibrary_SubSchema.MEDIA_ITEMS_ITEM_ID_COL_NAME });
            if (refSpecifications != null)
              refColumns.AddRange(refSpecifications.Select(s => GetMIAAttributeColumnName(s)));

            additionalAttributesConstraints.Add("CONSTRAINT " + fkDependencyMediaItemConstraintName +
                    " FOREIGN KEY (" + string.Join(", ", fkColumns) + ")" +
                    " REFERENCES " + refMiaTableName + " (" + string.Join(", ", refColumns) + ") ON DELETE CASCADE");
            foreignKeyAdded = true;
          }
        }
        
        // Main table
        foreach (string term in terms)
        {
          mainStatementBuilder.Append(term);
          mainStatementBuilder.Append(", ");
        }
        string pkConstraintName1 = GenerateDBObjectName(transaction, miam.AspectId, miaTableName + "_PK", miaTableName + "_PK");
        string fkMediaItemConstraintName1 = GenerateDBObjectName(transaction, miam.AspectId, miaTableName + "_MEDIA_ITEMS_FK", "FK");
        mainStatementBuilder.Append(
            "CONSTRAINT " + pkConstraintName1 + " PRIMARY KEY (" + string.Join(",", keyColumns) + ")");
        if(!foreignKeyAdded)
        {
          //Avoid circular foreign keys
          additionalAttributesConstraints.Add("CONSTRAINT " + fkMediaItemConstraintName1 +
                  " FOREIGN KEY (" + MIA_MEDIA_ITEM_ID_COL_NAME + ")" +
                  " REFERENCES " + MediaLibrary_SubSchema.MEDIA_ITEMS_TABLE_NAME + " (" + MediaLibrary_SubSchema.MEDIA_ITEMS_ITEM_ID_COL_NAME + ") ON DELETE CASCADE");
        }
        if (additionalAttributesConstraints.Count > 0)
        {
          mainStatementBuilder.Append(", ");
          mainStatementBuilder.Append(StringUtils.Join(", ", additionalAttributesConstraints));
        }
        mainStatementBuilder.Append(")");
        using (IDbCommand command = transaction.CreateCommand())
        {
          command.CommandText = mainStatementBuilder.ToString();
          ServiceRegistration.Get<ILogger>().Debug(
              "MIA_Management: Creating main table '{0}' for media item aspect '{1}'", miaTableName, miam.AspectId);
          command.ExecuteNonQuery();
        }

        string indexName = GenerateDBObjectName(transaction, miam.AspectId, miaTableName + "_PK_IDX", "IDX");
        ServiceRegistration.Get<ILogger>().Debug("MIA_Management: Creating primary key index '{0}' for media item aspect '{1}'",
            indexName, miam.AspectId);
        using (IDbCommand command = transaction.CreateCommand())
        {
          command.CommandText = "CREATE UNIQUE INDEX " + indexName + " ON " + miaTableName + "(" + string.Join(",", keyColumns) + ")";
          command.ExecuteNonQuery();
        }

        // Attributes: Second run
        foreach (MediaItemAspectMetadata.AttributeSpecification spec in miam.AttributeSpecifications.Values)
        {
          string sqlType = spec.AttributeType == typeof(string) ? database.GetSQLVarLengthStringType(spec.MaxNumChars) :
              database.GetSQLType(spec.AttributeType);
          string attributeColumnName = GetMIAAttributeColumnName(spec); // Name was already generated in previous loop
          string attributeColumnIdentifier = GetMIAAttributeColumnIdentifier(spec);
          switch (spec.Cardinality)
          {
            case Cardinality.Inline:
              if (spec.IsIndexed)
              {
                // Value index
                indexName = GenerateDBObjectName(transaction, miam.AspectId, attributeColumnIdentifier + "_IDX", "IDX");
                using (IDbCommand command = transaction.CreateCommand())
                {
                  command.CommandText = "CREATE INDEX " + indexName + " ON " + miaTableName + "(" + attributeColumnName + ")";
                  ServiceRegistration.Get<ILogger>().Debug(
                      "MIA_Management: Creating index '{0}' for inline attribute '{1}' in media item aspect '{2}'",
                      indexName, spec.AttributeName, miam.AspectId);
                  command.ExecuteNonQuery();
                }
              }
              break;
            case Cardinality.OneToMany:
              // Create foreign table with the join attribute inside
              collectionAttributeTableName = GetMIACollectionAttributeTableName(spec); // Name was already generated in previous loop
              pkConstraintName = GenerateDBObjectName(transaction, miam.AspectId, attributeColumnIdentifier + "_PK", "PK");
              string fkMediaItemConstraintName = GenerateDBObjectName(transaction, miam.AspectId, attributeColumnIdentifier + "_MEDIA_ITEM_FK", "FK");

              using (IDbCommand command = transaction.CreateCommand())
              {
                command.CommandText = "CREATE TABLE " + collectionAttributeTableName + " (" +
                    MIA_MEDIA_ITEM_ID_COL_NAME + " " + database.GetSQLType(typeof(Guid)) + ", " +
                    COLL_ATTR_VALUE_COL_NAME + " " + sqlType + ", " +
                    COLL_ATTR_VALUE_ORDER_COL_NAME + " " + database.GetSQLType(typeof(int)) + ", " +
                    "CONSTRAINT " + pkConstraintName + " PRIMARY KEY (" + MIA_MEDIA_ITEM_ID_COL_NAME + "), " +
                    "CONSTRAINT " + fkMediaItemConstraintName +
                    " FOREIGN KEY (" + MIA_MEDIA_ITEM_ID_COL_NAME + ")" +
                    " REFERENCES " + MediaLibrary_SubSchema.MEDIA_ITEMS_TABLE_NAME + " (" + MediaLibrary_SubSchema.MEDIA_ITEMS_ITEM_ID_COL_NAME + ") ON DELETE CASCADE" +
                    ")";
                ServiceRegistration.Get<ILogger>().Debug(
                    "MIA_Management: Creating OTM table '{0}' for attribute '{1}' in media item aspect '{2}'",
                    collectionAttributeTableName, spec.AttributeName, miam.AspectId);
                command.ExecuteNonQuery();
              }

              // Foreign key index
              indexName = GenerateDBObjectName(transaction, miam.AspectId, attributeColumnIdentifier + "_FK_IDX", "IDX");
              using (IDbCommand command = transaction.CreateCommand())
              {
                command.CommandText = "CREATE INDEX " + indexName + " ON " + collectionAttributeTableName + "(" +
                    MIA_MEDIA_ITEM_ID_COL_NAME + ")";
                ServiceRegistration.Get<ILogger>().Debug("MIA_Management: Creating foreign key index '{0}' for OTM attribute '{1}' in media item aspect '{2}'",
                    indexName, spec.AttributeName, miam.AspectId);
                command.ExecuteNonQuery();
              }

              if (spec.IsIndexed)
              {
                // Value index
                indexName = GenerateDBObjectName(transaction, miam.AspectId, attributeColumnIdentifier + "_VAL_IDX", "IDX");
                using (IDbCommand command = transaction.CreateCommand())
                {
                  command.CommandText = "CREATE INDEX " + indexName + " ON " + collectionAttributeTableName + "(" +
                      COLL_ATTR_VALUE_COL_NAME + ")";
                  ServiceRegistration.Get<ILogger>().Debug(
                      "MIA_Management: Creating value index '{0}' for OTM attribute '{1}' in media item aspect '{2}'",
                      indexName, spec.AttributeName, miam.AspectId);
                  command.ExecuteNonQuery();
                }
              }
              break;
            case Cardinality.ManyToOne:
              collectionAttributeTableName = GetMIACollectionAttributeTableName(spec); // Name was already generated in previous loop

              if (spec.IsIndexed)
              {
                // Foreign key index
                indexName = GenerateDBObjectName(transaction, miam.AspectId, attributeColumnIdentifier + "_FK_IDX", "IDX");
                using (IDbCommand command = transaction.CreateCommand())
                {
                  command.CommandText = "CREATE INDEX " + indexName + " ON " + miaTableName + "(" +
                      attributeColumnName + ")";
                  ServiceRegistration.Get<ILogger>().Debug(
                      "MIA_Management: Creating foreign key index '{0}' for MTO attribute '{1}' in media item aspect '{2}'",
                      indexName, spec.AttributeName, miam.AspectId);
                  command.ExecuteNonQuery();
                }
              }

              // Value index
              indexName = GenerateDBObjectName(transaction, miam.AspectId, attributeColumnIdentifier + "_VAL_IDX", "IDX");
              using (IDbCommand command = transaction.CreateCommand())
              {
                command.CommandText = "CREATE UNIQUE INDEX " + indexName + " ON " + collectionAttributeTableName + "(" +
                    COLL_ATTR_VALUE_COL_NAME + ")";
                ServiceRegistration.Get<ILogger>().Debug(
                    "MIA_Management: Creating value index '{0}' for MTO attribute '{1}' in media item aspect '{2}'",
                    indexName, spec.AttributeName, miam.AspectId);
                command.ExecuteNonQuery();
              }
              break;
            case Cardinality.ManyToMany:
              // Create foreign table and additional table for the N:M join attributes
              collectionAttributeTableName = GetMIACollectionAttributeTableName(spec); // Name was already generated in previous loop
              pkConstraintName = GenerateDBObjectName(transaction, miam.AspectId, attributeColumnIdentifier + "_PK", "PK");
              string nmTableName = GenerateMIACollectionAttributeNMTableName(transaction, spec);
              string pkNMConstraintName = GenerateDBObjectName(transaction, miam.AspectId, nmTableName + "_PK", "PK");
              string fkMainTableConstraintName = GenerateDBObjectName(transaction, miam.AspectId, nmTableName + "_MAIN_FK", "FK");
              string fkForeignTableConstraintName = GenerateDBObjectName(transaction, miam.AspectId, nmTableName + "_FOREIGN_FK", "PK");

              using (IDbCommand command = transaction.CreateCommand())
              {
                command.CommandText = "CREATE TABLE " + collectionAttributeTableName + " (" +
                    FOREIGN_COLL_ATTR_ID_COL_NAME + " " + database.GetSQLType(typeof(Guid)) + ", " +
                    COLL_ATTR_VALUE_COL_NAME + " " + sqlType + ", " +
                    COLL_ATTR_VALUE_ORDER_COL_NAME + " " + database.GetSQLType(typeof(int)) + ", " +
                    "CONSTRAINT " + pkConstraintName + " PRIMARY KEY (" + FOREIGN_COLL_ATTR_ID_COL_NAME + ")" + ")";
                ServiceRegistration.Get<ILogger>().Debug(
                    "MIA_Management: Creating MTM value table '{0}' for attribute '{1}' in media item aspect '{2}'",
                    collectionAttributeTableName, spec.AttributeName, miam.AspectId);
                command.ExecuteNonQuery();
              }

              using (IDbCommand command = transaction.CreateCommand())
              {
                command.CommandText = "CREATE TABLE " + nmTableName + " (" +
                    MIA_MEDIA_ITEM_ID_COL_NAME + " " + database.GetSQLType(typeof(Guid)) + ", " +
                    FOREIGN_COLL_ATTR_ID_COL_NAME + " " + database.GetSQLType(typeof(Guid)) + ", " +
                    "CONSTRAINT " + pkNMConstraintName + " PRIMARY KEY (" + MIA_MEDIA_ITEM_ID_COL_NAME + "," + FOREIGN_COLL_ATTR_ID_COL_NAME + "), " +
                    "CONSTRAINT " + fkMainTableConstraintName + " FOREIGN KEY (" + MIA_MEDIA_ITEM_ID_COL_NAME + ")" +
                    " REFERENCES " + miaTableName + " (" + MediaLibrary_SubSchema.MEDIA_ITEMS_ITEM_ID_COL_NAME + ") ON DELETE CASCADE, " +
                    "CONSTRAINT " + fkForeignTableConstraintName + " FOREIGN KEY (" + FOREIGN_COLL_ATTR_ID_COL_NAME + ")" +
                    " REFERENCES " + collectionAttributeTableName + " (" + FOREIGN_COLL_ATTR_ID_COL_NAME + ") ON DELETE CASCADE" +
                    ")";
                ServiceRegistration.Get<ILogger>().Debug(
                    "MIA_Management: Creating N:M table '{0}' for MTM attribute '{1}' in media item aspect '{2}'",
                    nmTableName, spec.AttributeName, miam.AspectId);
                command.ExecuteNonQuery();
              }

              // Foreign key index to MIA table
              indexName = GenerateDBObjectName(transaction, miam.AspectId, nmTableName + "_MIA_FK_IDX", "IDX");
              using (IDbCommand command = transaction.CreateCommand())
              {
                command.CommandText = "CREATE INDEX " + indexName + " ON " + nmTableName + "(" +
                    MIA_MEDIA_ITEM_ID_COL_NAME + ")";
                ServiceRegistration.Get<ILogger>().Debug(
                    "MIA_Management: Creating foreign index '{0}' to main MIA table for MTM attribute '{1}' in media item aspect '{2}'",
                    indexName, spec.AttributeName, miam.AspectId);
                command.ExecuteNonQuery();
              }

              // Foreign key index to value table
              indexName = GenerateDBObjectName(transaction, miam.AspectId, nmTableName + "_VAL_FK_IDX", "IDX");
              using (IDbCommand command = transaction.CreateCommand())
              {
                command.CommandText = "CREATE INDEX " + indexName + " ON " + nmTableName + "(" +
                    FOREIGN_COLL_ATTR_ID_COL_NAME + ")";
                ServiceRegistration.Get<ILogger>().Debug(
                    "MIA_Management: Creating foreign index '{0}' to value table for MTM attribute '{1}' in media item aspect '{2}'",
                    indexName, spec.AttributeName, miam.AspectId);
                command.ExecuteNonQuery();
              }

              if (spec.IsIndexed)
              {
                // Value index
                indexName = GenerateDBObjectName(transaction, miam.AspectId, attributeColumnIdentifier + "_VAL_IDX", "IDX");
                using (IDbCommand command = transaction.CreateCommand())
                {
                  command.CommandText = "CREATE UNIQUE INDEX " + indexName + " ON " + collectionAttributeTableName + "(" +
                      COLL_ATTR_VALUE_COL_NAME + ")";
                  ServiceRegistration.Get<ILogger>().Debug(
                      "MIA_Management: Creating value index '{0}' for MTM attribute '{1}' in media item aspect '{2}'",
                      indexName, spec.AttributeName, miam.AspectId);
                  command.ExecuteNonQuery();
                }
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
        ServiceRegistration.Get<ILogger>().Error("MIA_Management: Error adding media item aspect storage '{0}'", e, miam.AspectId);
        transaction.Rollback();
        throw;
      }
      lock (_syncObj)
        _managedMIATypes[miam.AspectId] = miam;
      _MIACreationDates[miam.AspectId] = DateTime.Now;
      return true;
    }

    public bool RemoveMediaItemAspectStorage(Guid aspectId)
    {
      lock (_syncObj)
      {
        if (!_managedMIATypes.ContainsKey(aspectId))
          return false;
        _managedMIATypes[aspectId] = null;
      }
      MediaItemAspectMetadata miam = GetMediaItemAspectMetadata(aspectId);
      ISQLDatabase database = ServiceRegistration.Get<ISQLDatabase>();
      ITransaction transaction = database.BeginTransaction();
      ServiceRegistration.Get<ILogger>().Info("MIA_Management: Removing media library storage for media item aspect '{0}' (id '{1}')",
          miam.Name, miam.AspectId);
      try
      {
        // We don't remove the name alias mappings from the alias cache because we simply reload the alias cache at the end.
        // We don't remove the name alias mappings from the name aliases table because they are deleted by the DB system
        // (ON DELETE CASCADE).
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
              using (IDbCommand command = transaction.CreateCommand())
              {
                command.CommandText = "DROP TABLE " + tableName;
                ServiceRegistration.Get<ILogger>().Debug(
                    "MIA_Management: Dropping OTM table '{0}' for MTM attribute '{1}' in media item aspect '{2}'",
                    tableName, spec.AttributeName, miam.AspectId);
                command.ExecuteNonQuery();
              }
              break;
            case Cardinality.ManyToOne:
              // After the main table was dropped
              break;
            case Cardinality.ManyToMany:
              tableName = GetMIACollectionAttributeNMTableName(spec);

              // N:M table
              using (IDbCommand command = transaction.CreateCommand())
              {
                command.CommandText = "DROP TABLE " + tableName;
                ServiceRegistration.Get<ILogger>().Debug(
                    "MIA_Management: Dropping MTM value table '{0}' for attribute '{1}' in media item aspect '{2}'",
                    tableName, spec.AttributeName, miam.AspectId);
                command.ExecuteNonQuery();
              }

              tableName = GetMIACollectionAttributeTableName(spec);

              // Foreign attribute value table
              using (IDbCommand command = transaction.CreateCommand())
              {
                command.CommandText = "DROP TABLE " + tableName;
                ServiceRegistration.Get<ILogger>().Debug(
                    "MIA_Management: Dropping N:M table '{0}' for MTM attribute '{1}' in media item aspect '{2}'",
                    tableName, spec.AttributeName, miam.AspectId);
                command.ExecuteNonQuery();
              }
              break;
            default:
              throw new NotImplementedException(string.Format("Attribute '{0}.{1}': Cardinality '{2}' is not implemented",
                  aspectId, spec.AttributeName, spec.Cardinality));
          }
        }

        // Main table
        using (IDbCommand command = transaction.CreateCommand())
        {
          command.CommandText = "DROP TABLE " + miaTableName;
          ServiceRegistration.Get<ILogger>().Debug(
              "MIA_Management: Dropping main table '{0}' for media item aspect '{1}')", miaTableName, miam.AspectId);
          command.ExecuteNonQuery();
        }

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
              using (IDbCommand command = transaction.CreateCommand())
              {
                command.CommandText = "DROP TABLE " + tableName;
                ServiceRegistration.Get<ILogger>().Debug(
                    "MIA_Management: Dropping MTO value table '{0}' for attribute '{1}' in media item aspect '{2}'",
                    tableName, spec.AttributeName, miam.AspectId);
                command.ExecuteNonQuery();
              }
              break;
            case Cardinality.ManyToMany:
              break;
            default:
              throw new NotImplementedException(string.Format("Attribute '{0}.{1}': Cardinality '{2}' is not implemented",
                  aspectId, spec.AttributeName, spec.Cardinality));
          }
        }
        // Unregister metadata
        using (IDbCommand command = MediaLibrary_SubSchema.DeleteMediaItemAspectMetadataCommand(transaction, aspectId))
          command.ExecuteNonQuery();
        transaction.Commit();
        ReloadAliasCache();
      }
      catch (Exception e)
      {
        ServiceRegistration.Get<ILogger>().Error("MIA_Management: Error removing media item aspect storage '{0}'", e, aspectId);
        transaction.Rollback();
        throw;
      }
      lock (_syncObj)
        _managedMIATypes.Remove(aspectId);
      _MIACreationDates.Remove(aspectId);
      return true;
    }

    public bool IsCLOBAttribute(MediaItemAspectMetadata.AttributeSpecification specification)
    {
      ISQLDatabase database = ServiceRegistration.Get<ISQLDatabase>();
      return specification.AttributeType == typeof(string) && database.IsCLOB(specification.MaxNumChars);
    }

    #endregion

    #region MIA management

    public bool MIAExists(ITransaction transaction, Guid mediaItemId, MediaItemAspect mia)
    {
      MediaItemAspectMetadata miam;
      if (!_managedMIATypes.TryGetValue(mia.Metadata.AspectId, out miam) || miam == null)
        throw new ArgumentException(string.Format("MIA_Management: Requested media item aspect type with id '{0}' doesn't exist", mia.Metadata.AspectId));
      string miaTableName = GetMIATableName(miam);

      ISQLDatabase database = transaction.Database;
      using (IDbCommand command = transaction.CreateCommand())
      {
        command.CommandText = "SELECT " + MIA_MEDIA_ITEM_ID_COL_NAME + " FROM " + miaTableName +
            " WHERE " + MIA_MEDIA_ITEM_ID_COL_NAME + " = @MEDIA_ITEM_ID";

        database.AddParameter(command, "MEDIA_ITEM_ID", mediaItemId, typeof(Guid));

        MultipleMediaItemAspectMetadata mmiam = miam as MultipleMediaItemAspectMetadata;
        if (mmiam != null)
        {
          foreach (MediaItemAspectMetadata.AttributeSpecification spec in mmiam.UniqueAttributeSpecifications.Values.Where(x => !x.IsCollectionAttribute))
          {
            string name = GetMIAAttributeColumnName(spec);
            command.CommandText += " AND " + name + " = @" + name;
            database.AddParameter(command, name, mia[spec], spec.AttributeType);
          }
        }

        return command.ExecuteScalar() != null;
      }
    }

    public MediaItemAspect GetMediaItemAspect(ITransaction transaction, Guid mediaItemId, Guid aspectId)
    {
      MediaItemAspectMetadata miaType;
      if (!_managedMIATypes.TryGetValue(aspectId, out miaType) || miaType == null)
        throw new ArgumentException(string.Format("MIA_Management: Requested media item aspect type with id '{0}' doesn't exist", aspectId));

      MediaItemAspect result;
      if(miaType is MultipleMediaItemAspectMetadata)
        result = new MultipleMediaItemAspect((MultipleMediaItemAspectMetadata)miaType);
      else
        result = new SingleMediaItemAspect((SingleMediaItemAspectMetadata)miaType);
    
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
      // TODO: More where clause for multiple MIA
    
      StringBuilder mainQueryBuilder = new StringBuilder("SELECT ");
      mainQueryBuilder.Append(StringUtils.Join(", ", terms));
      mainQueryBuilder.Append(" FROM ");
      mainQueryBuilder.Append(miaTableName);
      mainQueryBuilder.Append(" WHERE ");
      mainQueryBuilder.Append(MIA_MEDIA_ITEM_ID_COL_NAME);
      mainQueryBuilder.Append(" = @MEDIA_ITEM_ID");
      // TODO: More where clause for multiple MIA

      ISQLDatabase database = transaction.Database;
      using (IDbCommand command = transaction.CreateCommand())
      {
        command.CommandText = mainQueryBuilder.ToString();

        database.AddParameter(command, "MEDIA_ITEM_ID", mediaItemId, typeof(Guid));

        // TODO: More where clause for multiple MIA

        using (IDataReader reader = command.ExecuteReader(CommandBehavior.SingleRow))
        {
          int i = 0;
          if (reader.Read())
            foreach (MediaItemAspectMetadata.AttributeSpecification spec in miaType.AttributeSpecifications.Values)
            {
              if (spec.Cardinality == Cardinality.Inline)
              {
                object value = ReadObject(database, reader, i, spec);
                if (!AttributeIsEmpty(value))
                  result.SetAttribute(spec, value);
              }
              i++;
            }
        }
      }
      return result;
    }

    public void AddOrUpdateMIA(ITransaction transaction, Guid mediaItemId, MediaItemAspect mia, bool add)
    {
      MediaItemAspectMetadata miaType;
      if (!_managedMIATypes.TryGetValue(mia.Metadata.AspectId, out miaType) || miaType == null)
        throw new ArgumentException(string.Format("MIA_Management: Requested media item aspect type with id '{0}' doesn't exist",
          mia.Metadata.AspectId));

      IList<string> terms1 = new List<string>();
      IList<string> terms2 = new List<string>();
      IList<string> terms3 = new List<string>();
      IList<BindVar> bindVars = new List<BindVar>();
      int ct = 0;
      string miaTableName = GetMIATableName(miaType);

      // Attributes: First run
      foreach (MediaItemAspectMetadata.AttributeSpecification spec in miaType.AttributeSpecifications.Values)
      {
        if (mia.IsIgnore(spec))
          continue;
        MultipleMediaItemAspectMetadata mmiam = mia.Metadata as MultipleMediaItemAspectMetadata;
        string attrColName;
        object attributeValue;
        string bindVarName = "V" + ct++;
        switch (spec.Cardinality)
        {
          case Cardinality.Inline:
            attrColName = GetMIAAttributeColumnName(spec);
            if (add)
            {
              terms1.Add(attrColName);
              terms2.Add("@" + bindVarName);
            }
            else
            {
              // Unique attributes cannot be set in an update (they are part of the where clause)
              if (mmiam != null && mmiam.UniqueAttributeSpecifications.Values.Contains(spec))
                terms3.Add("AND " + attrColName + " = @" + bindVarName);
              else
                terms1.Add(attrColName + " = @" + bindVarName);
            }
            attributeValue = mia.GetAttributeValue(spec);
            attributeValue = TruncateBigValue(attributeValue, spec);
            bindVars.Add(new BindVar(bindVarName, AttributeIsEmpty(attributeValue) ? null : attributeValue, spec.AttributeType));
            break;
          case Cardinality.OneToMany:
            // After main query
            break;
          case Cardinality.ManyToOne:
            attrColName = GetMIAAttributeColumnName(spec);
            attributeValue = mia.GetAttributeValue(spec);
            Guid? insertValue;
            if (AttributeIsEmpty(attributeValue))
              insertValue = null;
            else
            {
              Guid valuePk;
              GetOrCreateManyToOneMIAAttributeValue(transaction, spec, mia.GetAttributeValue(spec), add, out valuePk);
              insertValue = valuePk;
            }
            if (add)
            {
              terms1.Add(attrColName);
              terms2.Add("@" + bindVarName);
            }
            else
              terms1.Add(attrColName + " = @" + bindVarName);
            bindVars.Add(new BindVar(bindVarName, insertValue.HasValue ? (Guid?) insertValue.Value : null, typeof(Guid)));
            break;
          case Cardinality.ManyToMany:
            // After main query
            break;
          default:
            throw new NotImplementedException(string.Format("Cardinality '{0}' for attribute '{1}.{2}' is not implemented",
                spec.Cardinality, miaType.AspectId, spec.AttributeName));
        }
      }
      // terms = all inline attributes
      // sqlValues = all inline attribute values
      if (add || terms1.Count > 0 || terms3.Count > 0)
      {
        // Main query
        StringBuilder mainQueryBuilder = new StringBuilder();
        if (add)
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

        mainQueryBuilder.Append(StringUtils.Join(", ", terms1));
        bindVars.Add(new BindVar("MEDIA_ITEM_ID", mediaItemId, typeof(Guid)));
        // values = all inline attribute values plus media item ID
        if (add)
        {
          if (terms1.Count > 0)
            mainQueryBuilder.Append(", ");
          mainQueryBuilder.Append(MIA_MEDIA_ITEM_ID_COL_NAME); // Append the ID column as a normal attribute
          mainQueryBuilder.Append(") VALUES (");
          terms2.Add("@MEDIA_ITEM_ID");
          mainQueryBuilder.Append(StringUtils.Join(", ", terms2));
          mainQueryBuilder.Append(")");
        }
        else
        {
          mainQueryBuilder.Append(" WHERE ");
          mainQueryBuilder.Append(MIA_MEDIA_ITEM_ID_COL_NAME); // Use the ID column in WHERE condition
          mainQueryBuilder.Append(" = @MEDIA_ITEM_ID");
          if (terms3.Count > 0)
            mainQueryBuilder.Append(" " + StringUtils.Join(" ", terms3));
        }

        ISQLDatabase database = transaction.Database;
        using (IDbCommand command = transaction.CreateCommand())
        {
          command.CommandText = mainQueryBuilder.ToString();
          foreach (BindVar bindVar in bindVars)
            database.AddParameter(command, bindVar.Name, bindVar.Value, bindVar.VariableType);
          command.ExecuteNonQuery();
        }
      }

      // Attributes: Second run
      foreach (MediaItemAspectMetadata.AttributeSpecification spec in miaType.AttributeSpecifications.Values)
      {
        if (mia.IsIgnore(spec))
          continue;
        switch (spec.Cardinality)
        {
          case Cardinality.Inline:
            break;
          case Cardinality.OneToMany:
            InsertOrUpdateOneToManyMIAAttributeValues(transaction, spec, mediaItemId, mia.GetCollectionAttribute(spec), add);
            break;
          case Cardinality.ManyToOne:
            break;
          case Cardinality.ManyToMany:
            InsertOrUpdateManyToManyMIAAttributeValues(transaction, spec, mediaItemId, mia.GetCollectionAttribute(spec), add);
            break;
          default:
            throw new NotImplementedException(string.Format("Cardinality '{0}' for attribute '{1}.{2}' is not implemented",
                spec.Cardinality, miaType.AspectId, spec.AttributeName));
        }
      }

      CleanupAllManyToOneOrphanedAttributeValues(transaction, miaType);
    }

    /// <summary>
    /// Adds or updates the given media item aspect on the media item with the given id.
    /// </summary>
    /// <param name="transaction">Database transaction to use.</param>
    /// <param name="mediaItemId">Id of the media item to be added or updated.</param>
    /// <param name="mia">Media item aspect to write to DB.</param>
    public void AddOrUpdateMIA(ITransaction transaction, Guid mediaItemId, MediaItemAspect mia)
    {
      AddOrUpdateMIA(transaction, mediaItemId, mia, !MIAExists(transaction, mediaItemId, mia));
    }

    public bool RemoveMIA(ITransaction transaction, Guid mediaItemId, Guid aspectId)
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
      bool result;
      ISQLDatabase database = transaction.Database;
      using (IDbCommand command = transaction.CreateCommand())
      {
        command.CommandText = "DELETE FROM " + miaTableName + " WHERE " + MIA_MEDIA_ITEM_ID_COL_NAME + " = @MEDIA_ITEM_ID";
        database.AddParameter(command, "MEDIA_ITEM_ID", mediaItemId, typeof(Guid));
        result = command.ExecuteNonQuery() > 0;
      }
      CleanupAllManyToOneOrphanedAttributeValues(transaction, miaType);
      return result;
    }

    #endregion
  }
}
