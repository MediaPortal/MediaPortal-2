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
using MediaPortal.Media.MediaManagement.Views;
using MediaPortal.Database.Provider;

namespace MediaPortal.Database
{
  public interface IDatabase
  {
    /// <summary>
    /// Gets the name.
    /// </summary>
    /// <value>The name.</value>
    string Name { get; }

    /// <summary>
    /// Deletes the database.
    /// </summary>
    void Delete();

    /// <summary>
    /// Gets the attributes for this database
    /// </summary>
    /// <value>The attributes.</value>
    IList<IDbAttribute> Attributes { get; }

    /// <summary>
    /// Adds the specified attribute to the database
    /// </summary>
    /// <param name="attribute">The attribute.</param>
    void Add(IDbAttribute attribute);

    /// <summary>
    /// Adds a new attribute to the database
    /// </summary>
    /// <param name="name">The name.</param>
    /// <param name="type">The type.</param>
    /// <param name="size">The size.</param>
    void Add(string name, Type type, int size);

    /// <summary>
    /// Adds a new attribute to the database
    /// </summary>
    /// <param name="name">The name.</param>
    /// <param name="type">The type.</param>
    void Add(string name, Type type);

    /// <summary>
    /// Adds a new Index on the given table and column
    /// </summary>
    /// <param name="table"></param>
    /// <param name="column"></param>
    /// <param name="order"></param>
    void AddIndex(string table, string column, string order);

    /// <summary>
    /// Creates a new item
    /// </summary>
    /// <returns></returns>
    IDbItem CreateNew();

    /// <summary>
    /// Gets the database builder.
    /// </summary>
    /// <value>The builder.</value>
    IDatabaseBuilder Builder { get; }

    /// <summary>
    /// Exectues the query and returns all items matching the query.
    /// </summary>
    /// <param name="query">The query.</param>
    /// <returns></returns>
    IList<IDbItem> Query(IQuery query);

    /// <summary>
    /// checks if the database can execute this query
    /// </summary>
    /// <param name="query"></param>
    /// <returns></returns>
    bool CanQuery(IQuery query);

    /// <summary>
    /// Saves the a list of items to the database
    /// </summary>
    /// <param name="items">The items.</param>
    void Save(IList<IDbItem> items);
  }
}
