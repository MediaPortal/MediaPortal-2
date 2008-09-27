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
using MediaPortal.Media.MediaManager.Views;

namespace MediaPortal.Database.Provider
{
  public interface IDatabaseCommand : IDisposable
  {
    /// <summary>
    /// Gets or sets the connection.
    /// </summary>
    /// <value>The connection.</value>
    IDatabaseConnection Connection { get; set; }

    /// <summary>
    /// Gets or sets the command text.
    /// </summary>
    /// <value>The command text.</value>
    string CommandText { get; set; }

    /// <summary>
    /// Executes the query without returning results
    /// </summary>
    void ExecuteNonQuery();

    /// <summary>
    /// returns whether the table exists or not
    /// </summary>
    /// <param name="tableName">Name of the table.</param>
    /// <returns></returns>
    bool DoesTableExists(string tableName);

    /// <summary>
    /// Creates a table.
    /// </summary>
    /// <param name="tableName">Name of the table.</param>
    void CreateTable(string tableName);

    /// <summary>
    /// Drops a table.
    /// </summary>
    /// <param name="tableName">Name of the table.</param>
    void DropTable(string tableName);

    /// <summary>
    /// Returns all column names with their type.
    /// </summary>
    /// <param name="tableName">Name of the table.</param>
    /// <returns></returns>
    IDictionary<string, Type> GetColumns(string tableName);

    /// <summary>
    /// Adds a new column to a table.
    /// </summary>
    /// <param name="_tableName">Name of the _table.</param>
    /// <param name="attribute">The attribute.</param>
    void AddColumn(string _tableName, IDbAttribute attribute);

    /// <summary>
    /// Adds a new index to a table.
    /// </summary>
    /// <param name="_tableName">Name of the _table.</param>
    /// <param name="attribute">The attribute.</param>
    void AddIndex(string _tableName, string _columnName, string _order);

    /// <summary>
    /// Gets all tables in the database.
    /// </summary>
    /// <returns></returns>
    IList<string> GetTables();

    /// <summary>
    /// Saves the specified item.
    /// </summary>
    /// <param name="item">The item.</param>
    void Save(IDbItem item);

    /// <summary>
    /// Deletes the specified item.
    /// </summary>
    /// <param name="item">The item.</param>
    void Delete(IDbItem item);


    /// <summary>
    /// Exectues the query and returns all items matching the query.
    /// </summary>
    /// <param name="query">The query.</param>
    /// <returns></returns>
    IList<IDbItem> Query(IDatabase db, IQuery query);

    /// <summary>
    /// returns if the database exists or not
    /// </summary>
    /// <param name="database">The database.</param>
    /// <returns></returns>
    bool DoesDatabaseExists(string database);

    /// <summary>
    /// Creates the database.
    /// </summary>
    /// <param name="database">The database name.</param>
    void CreateDatabase(string database);

    /// <summary>
    /// Inserts a new record with attribute information in the attribute-type table
    /// </summary>
    /// <param name="tableName">Name of the attribute-type table.</param>
    /// <param name="attribute">The new attribute.</param>
    void InsertAttribute(string tableName, IDbAttribute attribute);

    /// <summary>
    /// Gets all attribute definitions from the attribute-type table
    /// </summary>
    /// <param name="tableName">Name of the attribute-type.</param>
    /// <returns></returns>
    IList<IDbAttribute> GetAttributes(string tableName);

    /// <summary>
    /// Saves the a list of items to the database
    /// </summary>
    /// <param name="items">The items.</param>
    void Save(IList<IDbItem> items);
  }
}
