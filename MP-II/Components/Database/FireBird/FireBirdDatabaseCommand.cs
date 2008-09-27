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
using System.Data;
using System.Collections.Generic;
using FirebirdSql.Data.FirebirdClient;

using MediaPortal.Core;
using MediaPortal.Core.Logging;

using MediaPortal.Database;
using MediaPortal.Database.Provider;

using MediaPortal.Media.MediaManager.Views;

namespace Components.Database.FireBird
{
  class FireBirdDatabaseCommand : IDatabaseCommand, IDisposable
  {
    #region variables

    private IDatabaseConnection _connection;
    private string _commandText;

    #endregion

    #region Basic IDatabaseCommand Members
    /// <summary>
    /// Gets or sets the connection.
    /// </summary>
    /// <value>The connection.</value>
    public IDatabaseConnection Connection
    {
      get { return _connection; }
      set { _connection = value; }
    }

    /// <summary>
    /// Gets or sets the command text.
    /// </summary>
    /// <value>The command text.</value>
    public string CommandText
    {
      get { return _commandText; }
      set { _commandText = value; }
    }

    /// <summary>
    /// Executes the query without returning results
    /// </summary>
    public void ExecuteNonQuery()
    {
      using (FbCommand cmd = new FbCommand())
      {
        cmd.CommandType = CommandType.Text;
        cmd.CommandText = CommandText;
        cmd.Connection = (FbConnection)_connection.UnderlyingConnection;
        cmd.ExecuteNonQuery();
      }
    }
    #endregion

    #region methods for managing tables/columns and databases

    #region Tables
    /// <summary>
    /// returns whether the table exists or not
    /// </summary>
    /// <param name="tableName">Name of the table.</param>
    /// <returns></returns>
    public bool DoesTableExists(string tableName)
    {
      using (FbCommand cmd = new FbCommand())
      {
        // Firebird returns NULL on an not found condition
        bool found = false;
        cmd.CommandType = CommandType.Text;
        cmd.CommandText = String.Format("select distinct RDB$RELATION_NAME from RDB$RELATION_FIELDS where RDB$SYSTEM_FLAG = 0 and RDB$RELATION_NAME = upper('{0}')", tableName);
        cmd.Connection = (FbConnection)_connection.UnderlyingConnection;
        using (FbDataReader reader = cmd.ExecuteReader())
        {
          while (reader.Read())
          {
            found = true;
          }
          return found;
        }
      }
    }


    /// <summary>
    /// Creates a table.
    /// </summary>
    /// <param name="tableName">Name of the table.</param>
    public void CreateTable(string tableName)
    {
      using (FbCommand cmd = new FbCommand())
      {
        cmd.CommandType = CommandType.Text;
        cmd.CommandText = String.Format("create table {0} (id integer primary key)", tableName);
        cmd.Connection = (FbConnection)_connection.UnderlyingConnection;
        cmd.ExecuteNonQuery();

        // Firebird doesn't have AutoIncrement fields, so we need to create a Generator and a Trigger
        string generatorName = String.Format("GEN_{0}_ID", tableName);
        cmd.CommandText = String.Format("create generator {0}", generatorName);
        cmd.ExecuteNonQuery();

        // Insert Trigger
        cmd.CommandText = String.Format("CREATE TRIGGER {0}_BI FOR {0} " +
                                       "ACTIVE BEFORE INSERT POSITION 0 " +
                                       "AS " +
                                       "BEGIN " +
                                       "IF (NEW.ID IS NULL) THEN " +
                                       "NEW.ID = GEN_ID(GEN_{0}_ID,1); " +
                                       "END", tableName);
        cmd.ExecuteNonQuery();
      }
    }

    /// <summary>
    /// Drops a table.
    /// </summary>
    /// <param name="tableName">Name of the table.</param>
    public void DropTable(string tableName)
    {
      using (FbCommand cmd = new FbCommand())
      {
        cmd.CommandType = CommandType.Text;
        cmd.CommandText = String.Format("drop table {0}", tableName);
        cmd.Connection = (FbConnection)_connection.UnderlyingConnection;
        cmd.ExecuteNonQuery();
      }
    }

    /// <summary>
    /// Gets all tables in the database.
    /// </summary>
    /// <returns></returns>
    public IList<string> GetTables()
    {
      List<string> list = new List<string>();
      using (FbCommand cmd = new FbCommand())
      {
        cmd.Connection = (FbConnection)_connection.UnderlyingConnection;
        cmd.CommandType = CommandType.Text;
        cmd.CommandText = String.Format("select distinct RDB$RELATION_NAME from RDB$RELATION_FIELDS where RDB$SYSTEM_FLAG = 0");
        using (FbDataReader reader = cmd.ExecuteReader())
        {
          while (reader.Read())
          {
            list.Add((string)reader["RDB$RELATION_NAME"]);
          }
        }
      }
      return list;
    }
    #endregion

    #region Columns
    /// <summary>
    /// Returns all column names with their type.
    /// </summary>
    /// <param name="tableName">Name of the table.</param>
    /// <returns></returns>
    public IDictionary<string, Type> GetColumns(string tableName)
    {
      Dictionary<string, Type> columns = new Dictionary<string, Type>();
      using (FbCommand cmd = new FbCommand())
      {
        cmd.CommandType = CommandType.Text;
        cmd.CommandText = String.Format("select first 1 * from {0}", tableName);
        cmd.Connection = (FbConnection)_connection.UnderlyingConnection;
        using (FbDataReader reader = cmd.ExecuteReader())
        {
          for (int i = 0; i < reader.FieldCount; ++i)
          {
            string name = reader.GetName(i);
            Type type = reader.GetFieldType(i);
            columns[name] = type;
          }
        }
      }
      return columns;
    }

    /// <summary>
    /// Adds a new column to a table.
    /// </summary>
    /// <param name="_tableName">Name of the _table.</param>
    /// <param name="attribute">The attribute.</param>
    public void AddColumn(string _tableName, IDbAttribute attribute)
    {
      string strType = "";
      if (attribute.Type == typeof(bool))
      {
        strType = "smallint";
      }
      if (attribute.Type == typeof(int))
      {
        strType = "int";
      }
      if (attribute.Type == typeof(long))
      {
        strType = "int";
      }
      if (attribute.Type == typeof(short))
      {
        strType = "int";
      }
      if (attribute.Type == typeof(float))
      {
        strType = "float";
      }
      if (attribute.Type == typeof(double))
      {
        strType = "double precision";
      }
      if (attribute.Type == typeof(DateTime))
      {
        strType = "TIMESTAMP";
      }
      if (attribute.Type == typeof(string))
      {
        strType = String.Format("varchar({0})", attribute.Size);
      }
      using (FbCommand cmd = new FbCommand())
      {
        cmd.CommandType = CommandType.Text;
        cmd.CommandText = String.Format("ALTER TABLE {0} ADD \"{1}\" {2} ", _tableName, attribute.Name, strType);
        cmd.Connection = (FbConnection)_connection.UnderlyingConnection;
        cmd.ExecuteNonQuery();
      }
    }
    #endregion

    #region Databases
    /// <summary>
    /// Returns if the database exists or not
    /// </summary>
    /// <param name="database">The database.</param>
    /// <returns></returns>
    public bool DoesDatabaseExists(string database)
    {
      // The Database is created as part of the connection.
      // If we would implement Firebird Server, we need to check here.
      return true;
    }

    /// <summary>
    /// Creates the database.
    /// </summary>
    /// <param name="database">The database name.</param>
    public void CreateDatabase(string database)
    {
      // The Database is created as part of the connection.
      // If we would implement Firebird Server, we need to create it here.
    }
    #endregion

    #region Indices
    /// <summary>
    /// Creates an Index on the given Table and Column
    /// </summary>
    /// <param name="_tableName"></param>
    /// <param name="_columnName"></param>
    public void AddIndex(string _tableName, string _columnName, string _order)
    {
      // Does the Table exist
      if (!DoesTableExists(_tableName))
        return;

      // Check if this is a valid column
      IDictionary<string, Type> columns = new Dictionary<string, Type>();
      columns = GetColumns(_tableName);

      if (columns == null)
        return;

      if (!columns.ContainsKey(_columnName))
        return;

      // Now build the name of the index and check, if it already exists
      string indexName = String.Format("IX_{0}_{1}", _tableName, _columnName);
      // Firebird allows maximum of 31 chars for index / table names
      if (indexName.Length > 31)
        indexName = indexName.Substring(0, 30);

      using (FbCommand cmd = new FbCommand())
      {
        cmd.CommandType = CommandType.Text;
        cmd.CommandText = String.Format("select distinct RDB$INDEX_NAME from RDB$INDICES where RDB$INDEX_NAME = '{0}'", indexName);
        cmd.Connection = (FbConnection)_connection.UnderlyingConnection;
        using (FbDataReader reader = cmd.ExecuteReader())
        {
          bool found = false;
          while (reader.Read())
          {
            found = true;
          }
          if (found)
            return;
        }
      }

      // Index does not exist, create it
      using (FbCommand cmd = new FbCommand())
      {
        cmd.CommandType = CommandType.Text;
        cmd.CommandText = String.Format("CREATE {3} INDEX \"{0}\" ON {1} (\"{2}\")", indexName, _tableName, _columnName, _order);
        cmd.Connection = (FbConnection)_connection.UnderlyingConnection;
        cmd.ExecuteNonQuery();
      }
    }
    #endregion

    #endregion

    #region methods for loading/saving dbitems
    /// <summary>
    /// Saves the a list of items to the database
    /// </summary>
    /// <param name="items">The items.</param>
    public void Save(IList<IDbItem> items)
    {
      FbConnection connect = (FbConnection)_connection.UnderlyingConnection;
      List<IDbItem> itemsAdded = new List<IDbItem>();
      List<IDbItem> itemsModified = new List<IDbItem>();

      using (FbTransaction transaction = connect.BeginTransaction())
      {
        foreach (IDbItem item in items)
        {
          // In rare cases quotation escape might fail. Make sure it goes on with the next item.
          try
          {
            if (AddUpdateItem(item, connect, transaction))
              itemsAdded.Add(item);
            else
              itemsModified.Add(item);
          }
          catch (Exception ex)
          {
            ServiceScope.Get<ILogger>().Error("FireBird failure to save DbItem: {0}", ex.Message);
          }
        }
        transaction.Commit();
      }

      IDatabaseNotifier notifier = ServiceScope.Get<IDatabaseNotifier>();
      foreach (IDbItem newItem in itemsAdded)
      {
        notifier.Notify(newItem.Database, DatabaseNotificationType.ItemAdded, newItem);
      }

      if (itemsModified.Count > 0)
        DeleteMultiFields(itemsModified[0]);

      foreach (IDbItem newItem in itemsModified)
      {
        notifier.Notify(newItem.Database, DatabaseNotificationType.ItemModified, newItem);
      }
    }

    /// <summary>
    /// Saves the specified item.
    /// </summary>
    /// <param name="item">The item.</param>
    public void Save(IDbItem item)
    {
      FbConnection connect = (FbConnection)_connection.UnderlyingConnection;
      bool added = false;

      using (FbTransaction transaction = connect.BeginTransaction())
      {
        added = AddUpdateItem(item, connect, transaction);
        transaction.Commit();
      }

      IDatabaseNotifier notifier = ServiceScope.Get<IDatabaseNotifier>();
      if (added)
      {
        notifier.Notify(item.Database, DatabaseNotificationType.ItemAdded, item);
      }
      else
      {
        DeleteMultiFields(item);
        notifier.Notify(item.Database, DatabaseNotificationType.ItemModified, item);
      }
    }

    private bool AddUpdateItem(IDbItem item, FbConnection connect, FbTransaction transaction)
    {
      SqlCache cache = new SqlCache();
      IEnumerator<KeyValuePair<string, IDbAttribute>> enumer;
      bool added = false;

      using (FbCommand cmd = new FbCommand())
      {
        cmd.Connection = connect;
        cmd.CommandType = CommandType.Text;
        cmd.Transaction = transaction;
        string tags = "";
        string values = "";
        string sql = "";

        // Do we add or Update
        bool add = (item["ID"] == null);

        enumer = item.Attributes.GetEnumerator();
        while (enumer.MoveNext())
        {
          IDbAttribute att = enumer.Current.Value;
          if (att.Name == "ID")
          {
            continue;
          }
          if (att.Value != null)
          {
            string attrValue = att.Value.ToString();
            if (att.Type == typeof(DateTime))
            {
              attrValue = ((DateTime)att.Value).ToString("yyyy-MM-dd HH:mm:ss");
            }
            // For List fields we need to insert the ?|" in order to be able to find multiple entries on search
            if (att.IsList)
            {
              attrValue = String.Format("|{0}|", attrValue);
            }

            // We might end up in invalid SQL Statements, if the string contains apostrophes
            attrValue = attrValue.Replace("'", "''");
            attrValue = attrValue.Replace("’", "’’");

            if (add)
            {
              tags += String.Format("\"{0}\",", att.Name);
              values += String.Format("'{0}',", attrValue);
            }
            else
            {
              sql += String.Format("\"{0}\"='{1}',", att.Name, attrValue);
            }
          }
        }

        if (add)
        {
          if (tags.EndsWith(","))
          {
            tags = tags.Substring(0, tags.Length - 1);
          }
          if (values.EndsWith(","))
          {
            values = values.Substring(0, values.Length - 1);
          }
          cmd.CommandText =
            String.Format("insert into {0} ({1}) values ({2})",
                          item.Database.Name, tags, values);

          cmd.ExecuteNonQuery();
          //Int64 obj = (Int64)cmd.ExecuteScalar();
          //item["id"] = (int)obj;
          added = true;
        }
        else
        {
          if (sql.EndsWith(","))
          {
            sql = sql.Substring(0, sql.Length - 1);
          }
          cmd.CommandText = String.Format("update {0} set {1} where id={2}", item.Database.Name, sql, item["ID"]);
          cmd.ExecuteNonQuery();
        }
      }

      UpdateMultiFields(item, cache, transaction);

      //reset...
      enumer = item.Attributes.GetEnumerator();
      while (enumer.MoveNext())
      {
        enumer.Current.Value.IsChanged = false;
      }
      return added;
    }

    /// <summary>
    /// Deletes the specified item.
    /// </summary>
    /// <param name="item">The item.</param>
    public void Delete(IDbItem item)
    {
      using (FbCommand cmd = new FbCommand())
      {
        cmd.Connection = (FbConnection)_connection.UnderlyingConnection;
        cmd.CommandType = CommandType.Text;
        if (item["ID"] != null)
        {
          cmd.CommandText = String.Format("delete from {0} where id={1}", item.Database.Name, item["ID"]);
          cmd.ExecuteNonQuery();
          DeleteMultiFields(item);
        }
        item["ID"] = null;
      }
      IDatabaseNotifier notifier = ServiceScope.Get<IDatabaseNotifier>();
      notifier.Notify(item.Database, DatabaseNotificationType.ItemDeleted, item);
    }

    /// <summary>
    /// Exectues the query and returns all items matching the query.
    /// </summary>
    /// <param name="query">The query.</param>
    /// <returns></returns>
    public IList<IDbItem> Query(IDatabase db, IQuery query)
    {
      List<IDbItem> results = new List<IDbItem>();

      //if we use a distinct query on a multi-attribute field
      //then we should run the query on the seperate table
      if (query.Operator == Operator.Distinct && query.FieldNames.Count == 1)
      {
        string tableName = db.Name + query.FieldNames[0];
        if (DoesTableExists(tableName))
        {
          using (FbCommand cmd = new FbCommand())
          {
            cmd.Connection = (FbConnection)_connection.UnderlyingConnection;
            cmd.CommandType = CommandType.Text;
            string from = String.Format("distinct \"{0}\"", query.FieldNames[0]);
            cmd.CommandText = String.Format("select {0} from {1}", from, tableName);

            using (FbDataReader reader = cmd.ExecuteReader())
            {
              while (reader.Read())
              {
                DbItem item = new DbItem(db, reader);
                item.Attributes.Add("multifield", new DbAttribute("multifield", typeof(Boolean), true));
                results.Add(item);
              }
            }
          }
          return results;
        }
      }

      using (FbCommand cmd = new FbCommand())
      {
        cmd.Connection = (FbConnection)_connection.UnderlyingConnection;
        cmd.CommandType = CommandType.Text;
        List<string> fromKeys = query.FromStatement;
        string from = "distinct ";
        if (fromKeys.Count == 0)
        {
          from = "*";
        }
        else
        {
          foreach (string key in fromKeys)
          {
            from += String.Format("\"{0}\",", key);
          }
          if (from.EndsWith(","))
          {
            from = from.Substring(0, from.Length - 1);
          }
        }
        string where = query.WhereStatement;

        cmd.CommandText = "select ";
        if (query.Limit > 0)
        {
          cmd.CommandText += String.Format("first {0}", query.Limit);
        }
        if (where.Length > 0)
        {
          cmd.CommandText += String.Format(" {0} from {1} where {2}", from, db.Name, where);
        }
        else
        {
          cmd.CommandText += String.Format(" {0} from {1}", from, db.Name);
        }

        if (query.Sort != SortOrder.None && query.SortFields.Count > 0)
        {
          cmd.CommandText += " order by ";
          string fields = "";
          foreach (string sortfield in query.SortFields)
          {
            fields += sortfield;
            fields += ",";
          }
          if (fields.EndsWith(","))
            fields = fields.Substring(0, fields.Length - 1);
          cmd.CommandText += fields;
          if (query.Sort == SortOrder.Descending)
          {
            cmd.CommandText += " desc";
          }
          else if (query.Sort == SortOrder.Ascending)
          {
            cmd.CommandText += " asc";
          }
        }
        using (FbDataReader reader = cmd.ExecuteReader())
        {
          while (reader.Read())
          {
            results.Add(new DbItem(db, reader));
          }
        }
      }

      return results;
    }

    /// <summary>
    /// Deletes Multifields
    /// </summary>
    /// <param name="newItem"></param>
    private void DeleteMultiFields(IDbItem newItem)
    {
      IEnumerator<KeyValuePair<string, IDbAttribute>> enumer = newItem.Attributes.GetEnumerator();
      while (enumer.MoveNext())
      {
        //does the attribute have a value
        IDbAttribute att = enumer.Current.Value;
        if (att.IsList)
        {
          DeleteMultiField(newItem, att.Name);
        }
      }
    }

    /// <summary>
    /// Delete a Multi Field
    /// </summary>
    /// <param name="item"></param>
    /// <param name="attName"></param>
    private void DeleteMultiField(IDbItem item, string attName)
    {
      using (FbCommand cmd = new FbCommand())
      {
        cmd.Connection = (FbConnection)_connection.UnderlyingConnection;
        string tableName = String.Format("{0}{1}", item.Database.Name, attName);
        cmd.CommandText = String.Format("delete from {0} where \"{1}\" not in ( select distinct replace(replace(\"{1}\", '''', ''''''), '|', '') from {2})", tableName, attName, item.Database.Name);
        cmd.ExecuteNonQuery();
      }

    }

    /// <summary>
    /// A database might contain multi-field attributes
    /// Example 
    ///  - a movie contains 1 or more actors
    ///  - a song contains 1 or more genres
    /// 
    /// All multi-field attributes ares stored in a seperate table to be able
    /// to find all unique instances very fast using a simple select distinct... query
    /// This table is named [DatabaseName][MultiFieldAttributeName]
    /// So.. if the movie database is called Movie
    /// and the multi-field attribute is called actors
    /// then the table is called Movieactors and this table will contain all (unique) actors
    /// 
    /// So... in the movie table we see:
    ///   - title="..." duration=123  actors="actor1|actor2|actor3|actor4|actor5|"
    /// 
    /// Then in the Movieactors table we see:
    ///   - actor1
    ///   - actor2
    ///   - actor3
    ///   - actor4
    ///   - actor5
    /// </summary>
    /// 
    /// optimalisations possible:
    ///   - cache the seperate tables, although caching is also performed by the database so we should
    ///      check if this brings us a real performance benefit. 
    ///      For a SqlServer we should minimize network roundtrips anyway
    ///   - for sqlite we should use transactions (speeds things up a lot)...
    ///   - for a database we should keep a cache of multi-field attributes instead of
    ///     finding them every time.
    ///   - multi-field values should be stored in a List<string> and only converted to the
    ///     actor1|actor2|actor3 format during database loading/saving
    /// 
    /// todo:
    ///   - instead of trying to dynamicly find out if a field is a multi-field attribute this should
    ///     be stored somewhere
    /// 
    ///   - when values are removed from a multi-field attribute, they must also be removed from the
    ///     seperate table IF no other movie-records use it
    /// 
    /// <param name="item">The item.</param>
    private void UpdateMultiFields(IDbItem item, SqlCache cache, FbTransaction transaction)
    {
      IEnumerator<KeyValuePair<string, IDbAttribute>> enumer;
      using (FbCommand cmd = new FbCommand())
      {
        cmd.Connection = (FbConnection)_connection.UnderlyingConnection;
        cmd.CommandType = CommandType.Text;
        cmd.Transaction = transaction;

        //if this item has a multi-attribute field
        //for each attribute
        enumer = item.Attributes.GetEnumerator();
        while (enumer.MoveNext())
        {
          //does the attribute has a value
          IDbAttribute att = enumer.Current.Value;
          if (att.IsList)
          {
            //yes, is it a multi-field attribute
            IList<string> attrValues = att.Values;
            string tableName = String.Format("{0}{1}", item.Database.Name, att.Name);
            string columnName = att.Name;

            // get all values from the multi-field attibute
            //for each value
            foreach (string part in attrValues)
            {
              if (part.Length == 0)
              {
                continue;
              }
              if (cache.Contains(tableName) == false)
              {
                SqlTableCache tableCache = new SqlTableCache(tableName);
                cmd.CommandText = String.Format("select * from {0}", tableName);
                using (FbDataReader reader = cmd.ExecuteReader())
                {
                  while (reader.Read())
                  {
                    tableCache.Add(reader);
                  }
                }
                cache[tableName] = tableCache;
              }

              if (cache[tableName].Contains(part))
              {
                continue;
              }
              // no? then insert the value in the seperate table
              string attrValue = part.Replace("'", "''");
              cmd.CommandText = String.Format("insert into {0} (\"{1}\") values( '{2}')", tableName, columnName, attrValue);
              cmd.ExecuteNonQuery();
              cache[tableName][part] = 1;
            }
          }
        }
      }
    }
    #endregion

    #region methods for managing the attribute-type table

    /// <summary>
    /// Inserts a new record with attribute information in the attribute-type table
    /// </summary>
    /// <param name="tableName">Name of the attribute-type table.</param>
    /// <param name="attribute">The new attribute.</param>
    public void InsertAttribute(string tableName, IDbAttribute attribute)
    {
      using (FbCommand cmd = new FbCommand())
      {
        string type = attribute.Type.ToString();
        if (attribute.IsList)
        {
          type = typeof(List<string>).ToString();
        }

        cmd.Connection = (FbConnection)_connection.UnderlyingConnection;
        cmd.CommandType = CommandType.Text;
        cmd.CommandText = String.Format("insert into {0} (\"name\",\"type\",\"size\") values( '{1}','{2}','{3}')",
                                        tableName,
                                        attribute.Name, type, attribute.Size);
        cmd.ExecuteNonQuery();
      }
    }

    /// <summary>
    /// Gets all attribute definitions from the attribute-type table
    /// </summary>
    /// <param name="tableName">Name of the attribute-type.</param>
    /// <returns></returns>
    public IList<IDbAttribute> GetAttributes(string tableName)
    {
      IList<IDbAttribute> list = new List<IDbAttribute>();
      // Attenntion: "ID" needs to be uppercase, since firebird creates the field in this way
      list.Add(new DbAttribute("ID", typeof(int)));
      using (FbCommand cmd = new FbCommand())
      {
        cmd.Connection = (FbConnection)_connection.UnderlyingConnection;
        cmd.CommandType = CommandType.Text;
        cmd.CommandText = String.Format("select * from {0}", tableName);
        using (FbDataReader reader = cmd.ExecuteReader())
        {
          while (reader.Read())
          {
            string name = (string)reader["name"];
            string type = (string)reader["type"];
            int size = (int)reader["size"];
            if (type == typeof(string).ToString())
            {
              list.Add(new DbAttribute(name, typeof(string), size));
            }
            if (type == typeof(float).ToString())
            {
              list.Add(new DbAttribute(name, typeof(float)));
            }
            if (type == typeof(double).ToString())
            {
              list.Add(new DbAttribute(name, typeof(double)));
            }
            if (type == typeof(int).ToString())
            {
              list.Add(new DbAttribute(name, typeof(int)));
            }
            if (type == typeof(DateTime).ToString())
            {
              list.Add(new DbAttribute(name, typeof(DateTime)));
            }
            if (type == typeof(List<string>).ToString())
            {
              list.Add(new DbAttribute(name, typeof(List<string>)));
            }
          }
        }
      }
      return list;
    }
    #endregion

    #region IDisposable Members
    /// <summary>
    /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
    /// </summary>
    public void Dispose()
    {
      // Nothing to dispose
    }

    #endregion
  }
}
