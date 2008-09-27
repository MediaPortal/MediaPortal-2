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
using System.Data.SQLite;

using MediaPortal.Core;
using MediaPortal.Core.Logging;

using MediaPortal.Database;
using MediaPortal.Database.Provider;

using MediaPortal.Media.MediaManager.Views;


namespace Components.Database.SqlLite
{
  public class SqlLiteDatabaseCommand : IDatabaseCommand, IDisposable
  {
    #region variables

    private IDatabaseConnection _connection;
    private string _commandText;

    #endregion

    #region basic IDatabaseCommand Members

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
      using (SQLiteCommand cmd = new SQLiteCommand())
      {
        cmd.CommandType = CommandType.Text;
        cmd.CommandText = CommandText;
        cmd.Connection = (SQLiteConnection)_connection.UnderlyingConnection;
        cmd.ExecuteNonQuery();
      }
    }

    #endregion

    #region IDisposable Members

    /// <summary>
    /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
    /// </summary>
    public void Dispose()
    {
      //nothing to dispose...
    }

    #endregion

    #region methods for managing tables/columns and databases

    #region tables

    /// <summary>
    /// returns whether the table exists or not
    /// </summary>
    /// <param name="tableName">Name of the table.</param>
    /// <returns></returns>
    public bool DoesTableExists(string tableName)
    {
      using (SQLiteCommand cmd = new SQLiteCommand())
      {
        cmd.CommandType = CommandType.Text;
        cmd.CommandText = String.Format("select * from sqlite_master where name = '{0}'", tableName);
        cmd.Connection = (SQLiteConnection)_connection.UnderlyingConnection;
        using (SQLiteDataReader reader = cmd.ExecuteReader())
        {
          return (reader.HasRows);
        }
      }
    }

    /// <summary>
    /// Creates a table.
    /// </summary>
    /// <param name="tableName">Name of the table.</param>
    public void CreateTable(string tableName)
    {
      using (SQLiteCommand cmd = new SQLiteCommand())
      {
        cmd.CommandType = CommandType.Text;
        cmd.CommandText = String.Format("create table {0} (id integer primary key)", tableName);
        cmd.Connection = (SQLiteConnection)_connection.UnderlyingConnection;
        cmd.ExecuteNonQuery();
      }
    }

    /// <summary>
    /// Drops a table.
    /// </summary>
    /// <param name="tableName">Name of the table.</param>
    public void DropTable(string tableName)
    {
      using (SQLiteCommand cmd = new SQLiteCommand())
      {
        cmd.CommandType = CommandType.Text;
        cmd.CommandText = String.Format("drop table {0}", tableName);
        cmd.Connection = (SQLiteConnection)_connection.UnderlyingConnection;
        cmd.ExecuteNonQuery();

        //@TODO : drop any related tables
      }
    }

    /// <summary>
    /// Gets all tables in the database.
    /// </summary>
    /// <returns></returns>
    public IList<string> GetTables()
    {
      List<string> list = new List<string>();
      using (SQLiteCommand cmd = new SQLiteCommand())
      {
        cmd.Connection = (SQLiteConnection)_connection.UnderlyingConnection;
        cmd.CommandType = CommandType.Text;
        cmd.CommandText = String.Format("select * from sqlite_master ");
        using (SQLiteDataReader reader = cmd.ExecuteReader())
        {
          while (reader.Read())
          {
            list.Add((string)reader["name"]);
          }
        }
      }
      return list;
    }

    #endregion

    #region columns

    /// <summary>
    /// Returns all column names with their type.
    /// </summary>
    /// <param name="tableName">Name of the table.</param>
    /// <returns></returns>
    public IDictionary<string, Type> GetColumns(string tableName)
    {
      Dictionary<string, Type> columns = new Dictionary<string, Type>();
      using (SQLiteCommand cmd = new SQLiteCommand())
      {
        cmd.CommandType = CommandType.Text;
        cmd.CommandText = String.Format("select * from {0} LIMIT 1", tableName);
        cmd.Connection = (SQLiteConnection)_connection.UnderlyingConnection;
        using (SQLiteDataReader reader = cmd.ExecuteReader())
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
        strType = "bit";
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
        strType = "double";
      }
      if (attribute.Type == typeof(DateTime))
      {
        strType = "DATE";
      }
      if (attribute.Type == typeof(string))
      {
        strType = "varchar(1024)";
      }
      using (SQLiteCommand cmd = new SQLiteCommand())
      {
        cmd.CommandType = CommandType.Text;
        cmd.CommandText = String.Format("ALTER TABLE {0} ADD {1} {2} NULL ", _tableName, attribute.Name, strType);
        cmd.Connection = (SQLiteConnection)_connection.UnderlyingConnection;
        cmd.ExecuteNonQuery();
      }
    }

    #endregion

    #region databases

    /// <summary>
    /// returns if the database exists or not
    /// </summary>
    /// <param name="database">The database.</param>
    /// <returns></returns>
    public bool DoesDatabaseExists(string database)
    {
      return true;
    }

    /// <summary>
    /// Creates the database.
    /// </summary>
    /// <param name="database">The database name.</param>
    public void CreateDatabase(string database) { }

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
      string indexName = String.Format("idx_{0}_{1}", _tableName, _columnName);
      using (SQLiteCommand cmd = new SQLiteCommand())
      {
        cmd.CommandType = CommandType.Text;
        cmd.CommandText = String.Format("select * from sqlite_master where name = '{0}' and type='index'", indexName);
        cmd.Connection = (SQLiteConnection)_connection.UnderlyingConnection;
        using (SQLiteDataReader reader = cmd.ExecuteReader())
        {
          if (reader.HasRows)
            return;  
        }
      }

      // Index does not exist, create it
      using (SQLiteCommand cmd = new SQLiteCommand())
      {
        cmd.CommandType = CommandType.Text;
        cmd.CommandText = String.Format("CREATE INDEX {0} ON {1}({2} {3})", indexName, _tableName, _columnName, _order);
        cmd.Connection = (SQLiteConnection)_connection.UnderlyingConnection;
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
      SQLiteConnection connect = (SQLiteConnection)_connection.UnderlyingConnection;
      List<IDbItem> itemsAdded = new List<IDbItem>();
      List<IDbItem> itemsModified = new List<IDbItem>();

      using (SQLiteTransaction transaction = connect.BeginTransaction())
      {
        foreach (IDbItem item in items)
        {
          // In rare cases quotation escape might fail for sqlite. Make sure it goes on with the next item.
          try
          {
            if (AddUpdateItem(item, connect))
              itemsAdded.Add(item);
            else
              itemsModified.Add(item);
          }
          catch (Exception ex)
          {
            ServiceScope.Get<ILogger>().Error("SQLite failure to save DbItem: {0}", ex.Message);
          }
        }
        transaction.Commit();
      }

      IDatabaseNotifier notifier = ServiceScope.Get<IDatabaseNotifier>();
      foreach (IDbItem newItem in itemsAdded)
      {
        notifier.Notify(newItem.Database, DatabaseNotificationType.ItemAdded, newItem);
      }
      foreach (IDbItem newItem in itemsModified)
      {
        DeleteMultiFields(newItem);
        notifier.Notify(newItem.Database, DatabaseNotificationType.ItemModified, newItem);
      }
    }

    /// <summary>
    /// Saves the specified item.
    /// </summary>
    /// <param name="item">The item.</param>
    public void Save(IDbItem item)
    {
      SQLiteConnection connect = (SQLiteConnection)_connection.UnderlyingConnection;
      bool added = false;

      using (SQLiteTransaction transaction = connect.BeginTransaction())
      {
        added = AddUpdateItem(item, connect);
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

    private bool AddUpdateItem(IDbItem item, SQLiteConnection connect)
    {
      SqlCache cache = new SqlCache();
      IEnumerator<KeyValuePair<string, IDbAttribute>> enumer;
      bool added = false;

      using (SQLiteCommand cmd = new SQLiteCommand())
      {
        cmd.Connection = connect;
        cmd.CommandType = CommandType.Text;
        if (item["id"] == null)
        {
          //create item
          //insert into movie (genre,director) values('horror','stephen king')
          string tags = "";
          string values = "";
          enumer = item.Attributes.GetEnumerator();
          while (enumer.MoveNext())
          {
            IDbAttribute att = enumer.Current.Value;
            if (att.Name == "id")
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
              attrValue = attrValue.Replace("'", "''");
              tags += String.Format("{0},", att.Name);
              values += String.Format("'{0}',", attrValue);
            }
          }
          if (tags.EndsWith(","))
          {
            tags = tags.Substring(0, tags.Length - 1);
          }
          if (values.EndsWith(","))
          {
            values = values.Substring(0, values.Length - 1);
          }
          cmd.CommandText =
            String.Format("insert into {0} ({1}) values ({2}); SELECT last_insert_rowid() AS RecordID;",
                          item.Database.Name, tags, values);

          Int64 obj = (Int64)cmd.ExecuteScalar();
          item["id"] = (int)obj;
          added = true;
        }
        else
        {
          //save item
          //update movie set genre='horror', director='stephen king' where id=123
          string sql = "";
          enumer = item.Attributes.GetEnumerator();
          while (enumer.MoveNext())
          {
            IDbAttribute att = enumer.Current.Value;
            if (att.Name == "id")
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
              attrValue = attrValue.Replace("'", "''");
              sql += String.Format("{0}='{1}',", att.Name, attrValue);
            }
          }
          if (sql.EndsWith(","))
          {
            sql = sql.Substring(0, sql.Length - 1);
          }
          cmd.CommandText = String.Format("update {0} set {1} where id={2}", item.Database.Name, sql, item["id"]);
          cmd.ExecuteNonQuery();
        }
      }
      UpdateMultiFields(item, cache);

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
      using (SQLiteCommand cmd = new SQLiteCommand())
      {
        cmd.Connection = (SQLiteConnection)_connection.UnderlyingConnection;
        cmd.CommandType = CommandType.Text;
        if (item["id"] != null)
        {
          cmd.CommandText = String.Format("delete from {0} where id={1}", item.Database.Name, item["id"]);
          cmd.ExecuteNonQuery();
          DeleteMultiFields(item);
        }
        item["id"] = null;
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
          using (SQLiteCommand cmd = new SQLiteCommand())
          {
            cmd.Connection = (SQLiteConnection)_connection.UnderlyingConnection;
            cmd.CommandType = CommandType.Text;
            string from = "distinct " + query.FieldNames[0];
            cmd.CommandText = String.Format("select {0} from {1}", from, tableName);

            using (SQLiteDataReader reader = cmd.ExecuteReader())
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

      using (SQLiteCommand cmd = new SQLiteCommand())
      {
        cmd.Connection = (SQLiteConnection)_connection.UnderlyingConnection;
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
            from += String.Format("{0},", key);
          }
          if (from.EndsWith(","))
          {
            from = from.Substring(0, from.Length - 1);
          }
        }
        string where = query.WhereStatement;
        if (where.Length > 0)
        {
          cmd.CommandText = String.Format("select {0} from {1} where {2}", from, db.Name, where);
        }
        else
        {
          cmd.CommandText = String.Format("select {0} from {1}", from, db.Name);
        }

        if (query.Sort != SortOrder.None && query.SortFields.Count>0)
        {
          cmd.CommandText += " order by ";
          string fields = "";
          foreach(string sortfield in query.SortFields)
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
        if (query.Limit > 0)
        {
          cmd.CommandText += String.Format(" limit {0}", query.Limit);
        }
        using ( SQLiteDataReader reader = cmd.ExecuteReader())
        {
          while (reader.Read())
          {
            results.Add(new DbItem(db, reader));
          }
        }
      }

      return results;
    }

    private void DeleteMultiFields(IDbItem newItem)
    {
      IEnumerator<KeyValuePair<string, IDbAttribute>> enumer = newItem.Attributes.GetEnumerator();
      while (enumer.MoveNext())
      {
        //does the attribute has a value
        IDbAttribute att = enumer.Current.Value;
        if (att.IsList)
        {
          DeleteMultiField(newItem, att.Name);
        }
      }
    }

    private void DeleteMultiField(IDbItem item, string attName)
    {
      using (SQLiteCommand cmd = new SQLiteCommand())
      {
        cmd.Connection = (SQLiteConnection)_connection.UnderlyingConnection;
        Dictionary<string, int> fieldsInUse = new Dictionary<string, int>();
        cmd.CommandText = String.Format("select {0} from {1}", attName, item.Database.Name);
        using (SQLiteDataReader reader = cmd.ExecuteReader())
        {
          while (reader.Read())
          {
            string fieldSubs = (string)reader[0];
            string[] parts = fieldSubs.Split(new char[] { '|' });
            for (int i = 0; i < parts.Length; ++i)
            {
              string attrValue = parts[i].Replace("'", "''");
              fieldsInUse[attrValue] = 1;
            }
          }
        }
        Dictionary<string, int>.Enumerator enumer = fieldsInUse.GetEnumerator();
        string where = "";
        while (enumer.MoveNext())
        {
          where += String.Format("({0} <> '{1}') AND ", attName, enumer.Current.Key);
        }
        if (where.EndsWith(" AND "))
        {
          where = where.Substring(0, where.Length - 5);
        }

        string tableName = String.Format("{0}{1}", item.Database.Name, attName);
        cmd.CommandText = String.Format("delete from {0} where {1}", tableName, where);
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
    private void UpdateMultiFields(IDbItem item, SqlCache cache)
    {
      IEnumerator<KeyValuePair<string, IDbAttribute>> enumer;
      using (SQLiteCommand cmd = new SQLiteCommand())
      {
        cmd.Connection = (SQLiteConnection)_connection.UnderlyingConnection;
        cmd.CommandType = CommandType.Text;
        //if this item has a multi-attribute field
        //then:
        // -if seperate table does not exists, then create it
        // -foreach item in the multiattribute field
        // - if item does not exists in the seperate table 
        //     - create new item in the seperate table

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
            //yes, does the seperate table exists?
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
                using (SQLiteDataReader reader = cmd.ExecuteReader())
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
              cmd.CommandText = String.Format("insert into {0} ({1}) values( '{2}')", tableName, columnName, attrValue);
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
      using (SQLiteCommand cmd = new SQLiteCommand())
      {
        string type = attribute.Type.ToString();
        if (attribute.IsList)
        {
          type = typeof(List<string>).ToString();
        }

        cmd.Connection = (SQLiteConnection)_connection.UnderlyingConnection;
        cmd.CommandType = CommandType.Text;
        cmd.CommandText = String.Format("insert into {0} (name,type,size) values( '{1}','{2}','{3}')",
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
      List<IDbAttribute> list = new List<IDbAttribute>();
      list.Add(new DbAttribute("id", typeof(int)));
      using (SQLiteCommand cmd = new SQLiteCommand())
      {
        cmd.Connection = (SQLiteConnection)_connection.UnderlyingConnection;
        cmd.CommandType = CommandType.Text;
        cmd.CommandText = String.Format("select * from {0}", tableName);
        using (SQLiteDataReader reader = cmd.ExecuteReader())
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

    #region system methods

    public void SetPragmas(object aSQLiteConnection)
    {
      SQLiteConnection myConnection = (SQLiteConnection)aSQLiteConnection;
      if (myConnection != null)
      {
        using (SQLiteCommand cmd = new SQLiteCommand())
        {
          cmd.CommandType = CommandType.Text;
          cmd.CommandText = "PRAGMA cache_size=4096; PRAGMA page_size=8192; PRAGMA synchronous='OFF'; PRAGMA count_changes=1; PRAGMA short_column_names=0; PRAGMA full_column_names=0; PRAGMA auto_vacuum=1";
          cmd.Connection = myConnection;
          cmd.ExecuteNonQuery();
        }
      }
    }

    #endregion
  }
}
