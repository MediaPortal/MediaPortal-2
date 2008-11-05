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
using System.Data.SqlClient;
using MediaPortal.Database;
using MediaPortal.Database.Provider;
using MediaPortal.Media.MediaManagement.Views;

namespace Components.Database.Sql
{
  public class SqlDatabaseCommand : IDatabaseCommand, IDisposable
  {
    private IDatabaseConnection _connection;
    private string _commandText;

    #region IDatabaseCommand Members

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
      using (SqlCommand cmd = new SqlCommand())
      {
        cmd.CommandType = CommandType.Text;
        cmd.CommandText = CommandText;
        cmd.Connection = (SqlConnection) _connection.UnderlyingConnection;
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

    /// <summary>
    /// Creates a table.
    /// </summary>
    /// <param name="tableName">Name of the table.</param>
    public void CreateTable(string tableName)
    {
      using (SqlCommand cmd = new SqlCommand())
      {
        cmd.CommandType = CommandType.Text;
        cmd.CommandText = String.Format("create table {0} (id int IDENTITY(1,1) primary key)", tableName);
        cmd.Connection = (SqlConnection) _connection.UnderlyingConnection;
        cmd.ExecuteNonQuery();
      }
    }

    /// <summary>
    /// Drops a table.
    /// </summary>
    /// <param name="tableName">Name of the table.</param>
    public void DropTable(string tableName)
    {
      using (SqlCommand cmd = new SqlCommand())
      {
        cmd.CommandType = CommandType.Text;
        cmd.CommandText = String.Format("drop table {0}", tableName);
        cmd.Connection = (SqlConnection) _connection.UnderlyingConnection;
        cmd.ExecuteNonQuery();
      }
    }

    /// <summary>
    /// Returns all column names with their type.
    /// </summary>
    /// <param name="tableName">Name of the table.</param>
    /// <returns></returns>
    public IDictionary<string, Type> GetColumns(string tableName)
    {
      Dictionary<string, Type> columns = new Dictionary<string, Type>();
      using (SqlCommand cmd = new SqlCommand())
      {
        cmd.CommandType = CommandType.Text;
        cmd.CommandText = String.Format("select  top 1 * from {0}", tableName);
        cmd.Connection = (SqlConnection) _connection.UnderlyingConnection;
        using (SqlDataReader reader = cmd.ExecuteReader())
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
      if (attribute.Type == typeof (bool))
      {
        strType = "bit";
      }
      if (attribute.Type == typeof (int))
      {
        strType = "int";
      }
      if (attribute.Type == typeof (long))
      {
        strType = "int";
      }
      if (attribute.Type == typeof (short))
      {
        strType = "int";
      }
      if (attribute.Type == typeof (string))
      {
        strType = String.Format("varchar({0})", attribute.Size);
      }
      using (SqlCommand cmd = new SqlCommand())
      {
        cmd.CommandType = CommandType.Text;
        cmd.CommandText = String.Format("ALTER TABLE {0} ADD {1} {2} NULL ", _tableName, attribute.Name, strType);
        cmd.Connection = (SqlConnection) _connection.UnderlyingConnection;
        cmd.ExecuteNonQuery();
      }
    }

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
      using (SqlCommand cmd = new SqlCommand())
      {
        cmd.CommandType = CommandType.Text;
        cmd.CommandText = String.Format("select * from sys.indexes where name = '{0}'", indexName);
        cmd.Connection = (SqlConnection)_connection.UnderlyingConnection;
        using (SqlDataReader reader = cmd.ExecuteReader())
        {
          if (reader.HasRows)
            return;
        }
      }

      // Index does not exist, create it
      using (SqlCommand cmd = new SqlCommand())
      {
        cmd.CommandType = CommandType.Text;
        cmd.CommandText = String.Format("CREATE INDEX {0} ON {1}({2} {3})", indexName, _tableName, _columnName, _order);
        cmd.Connection = (SqlConnection)_connection.UnderlyingConnection;
        cmd.ExecuteNonQuery();
      }
    }
    #endregion

    /// <summary>
    /// returns if the database exists or not
    /// </summary>
    /// <param name="database">The database.</param>
    /// <returns></returns>
    public bool DoesDatabaseExists(string database)
    {
      using (SqlCommand cmd = new SqlCommand())
      {
        cmd.CommandType = CommandType.Text;
        cmd.CommandText = String.Format("select * from sys.databases where name = '{0}'", database);
        cmd.Connection = (SqlConnection) _connection.UnderlyingConnection;
        using (SqlDataReader reader = cmd.ExecuteReader())
        {
          return (reader.HasRows);
        }
      }
    }

    /// <summary>
    /// Creates the database.
    /// </summary>
    /// <param name="database">The database name.</param>
    public void CreateDatabase(string database)
    {
      using (SqlCommand cmd = new SqlCommand())
      {
        cmd.CommandType = CommandType.Text;
        cmd.CommandText = String.Format("CREATE DATABASE {0}", database);
        cmd.Connection = (SqlConnection) _connection.UnderlyingConnection;
        cmd.ExecuteNonQuery();
      }
    }

    /// <summary>
    /// returns whether the table exists or not
    /// </summary>
    /// <param name="tableName">Name of the table.</param>
    /// <returns></returns>
    public bool DoesTableExists(string tableName)
    {
      using (SqlCommand cmd = new SqlCommand())
      {
        cmd.CommandType = CommandType.Text;
        cmd.CommandText = String.Format("select * from INFORMATION_SCHEMA.tables where table_name = '{0}'", tableName);
        cmd.Connection = (SqlConnection) _connection.UnderlyingConnection;
        using (SqlDataReader reader = cmd.ExecuteReader())
        {
          return (reader.HasRows);
        }
      }
    }

    /// <summary>
    /// Gets all tables in the database.
    /// </summary>
    /// <returns></returns>
    public IList<string> GetTables()
    {
      List<string> list = new List<string>();
      using (SqlCommand cmd = new SqlCommand())
      {
        cmd.Connection = (SqlConnection) _connection.UnderlyingConnection;
        cmd.CommandType = CommandType.Text;
        cmd.CommandText = String.Format("select * from INFORMATION_SCHEMA.tables ");
        using (SqlDataReader reader = cmd.ExecuteReader())
        {
          while (reader.Read())
          {
            list.Add((string) reader["TABLE_NAME"]);
          }
        }
      }
      return list;
    }

    /// <summary>
    /// Saves the specified item.
    /// </summary>
    /// <param name="item">The item.</param>
    public void Save(IDbItem item)
    {
      IEnumerator<KeyValuePair<string, IDbAttribute>> enumer;
      using (SqlCommand cmd = new SqlCommand())
      {
        cmd.Connection = (SqlConnection) _connection.UnderlyingConnection;
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
              // For List fields we need to insert the |" in order to be able to find multiple entries on search
              if (att.IsList)
              {
                attrValue = String.Format("|{0}|", attrValue);
              }
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
            String.Format("insert into {0} ({1}) values ({2});select @@identity", item.Database.Name, tags, values);

          decimal result = (decimal) cmd.ExecuteScalar();
          item["id"] = (int) result;
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
              // For List fields we need to insert the |" in order to be able to find multiple entries on search
              if (att.IsList)
              {
                attrValue = String.Format("|{0}|", attrValue);
              }
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

      //reset...
      enumer = item.Attributes.GetEnumerator();
      while (enumer.MoveNext())
      {
        enumer.Current.Value.IsChanged = false;
      }
    }

    /// <summary>
    /// Deletes the specified item.
    /// </summary>
    /// <param name="item">The item.</param>
    public void Delete(IDbItem item)
    {
      using (SqlCommand cmd = new SqlCommand())
      {
        cmd.Connection = (SqlConnection) _connection.UnderlyingConnection;
        cmd.CommandType = CommandType.Text;
        if (item["id"] != null)
        {
          cmd.CommandText = String.Format("delete from {0} where id={1}", item.Database.Name, item["id"]);
          cmd.ExecuteNonQuery();
        }
      }
    }

    /// <summary>
    /// Exectues the query and returns all items matching the query.
    /// </summary>
    /// <param name="query">The query.</param>
    /// <returns></returns>
    public IList<IDbItem> Query(IDatabase db, IQuery query)
    {
      List<IDbItem> results = new List<IDbItem>();

      using (SqlCommand cmd = new SqlCommand())
      {
        cmd.Connection = (SqlConnection) _connection.UnderlyingConnection;
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

        using (SqlDataReader reader = cmd.ExecuteReader())
        {
          while (reader.Read())
          {
            results.Add(new DbItem(db, reader));
          }
        }
      }

      return results;
    }

    #endregion

    public void InsertAttribute(string tableName, IDbAttribute attribute) {}

    public IList<IDbAttribute> GetAttributes(string tableName)
    {
      return null;
    }

    /// <summary>
    /// Saves the a list of items to the database
    /// </summary>
    /// <param name="items">The items.</param>
    public void Save(IList<IDbItem> items) {}
  }
}
