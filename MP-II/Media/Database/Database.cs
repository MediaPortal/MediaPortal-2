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
using MediaPortal.Core;
using MediaPortal.Database;
using MediaPortal.Database.Provider;
using MediaPortal.Media.MediaManagement.Views;

namespace Components.Database
{
  public class Database : IDatabase
  {
    #region variables

    private IDatabaseBuilder _builder;
    private string _tableName;
    private IList<IDbAttribute> _attributes;
    private string _attributeTable;

    #endregion

    /// <summary>
    /// Initializes a new instance of the <see cref="Database"/> class.
    /// </summary>
    /// <param name="name">The name.</param>
    /// <param name="builder">The builder.</param>
    public Database(string name, IDatabaseBuilder builder)
    {
      _tableName = name;
      _builder = builder;
      _attributeTable = name + "Types";
      FetchAttributeDefinitions();
    }

    /// <summary>
    /// gets all the attributes/fields defined on this database
    /// from the attribute-type table
    /// </summary>
    private void FetchAttributeDefinitions()
    {
      _attributes = new List<IDbAttribute>();
      using (IDatabaseConnection connect = _builder.CreateConnection())
      {
        using (IDatabaseCommand cmd = _builder.CreateCommand())
        {
          cmd.Connection = connect;
          _attributes = cmd.GetAttributes(_attributeTable);
        }
      }
    }

    /// <summary>
    /// Deletes the database.
    /// </summary>
    public void Delete()
    {
      using (IDatabaseConnection connect = _builder.CreateConnection())
      {
        using (IDatabaseCommand cmd = _builder.CreateCommand())
        {
          //delete any table created for multifield attributes
          foreach (IDbAttribute att in _attributes)
          {
            if (att.IsList)
            {
              string tableName = _tableName + att.Name;
              cmd.Connection = connect;
              cmd.DropTable(tableName);
            }
          }
          //delete the main table 
          cmd.Connection = connect;
          cmd.DropTable(_tableName);

          //delete the table holding attribute information 
          cmd.Connection = connect;
          cmd.DropTable(_attributeTable);
        }
        IDatabaseNotifier notifier = ServiceScope.Get<IDatabaseNotifier>();
        notifier.Notify(this, DatabaseNotificationType.DatabaseDeleted, null);
      }
    }

    /// <summary>
    /// Gets the database name.
    /// </summary>
    /// <value>The database name.</value>
    public string Name
    {
      get { return _tableName; }
    }


    /// <summary>
    /// Gets the attributes for this database
    /// </summary>
    /// <value>The attributes.</value>
    public IList<IDbAttribute> Attributes
    {
      get { return _attributes; }
    }

    /// <summary>
    /// Adds the specified attribute to the database
    /// </summary>
    /// <param name="attribute">The attribute.</param>
    public void Add(IDbAttribute attribute)
    {
      //does attribute already exists?
      foreach (IDbAttribute att in _attributes)
      {
        if (att.Name == attribute.Name && att.Type == attribute.Type)
        {
          //yes, no need to add it
          return;
        }
      }

      using (IDatabaseConnection connect = _builder.CreateConnection())
      {
        //create a new column for this attribute
        using (IDatabaseCommand cmd = _builder.CreateCommand())
        {
          cmd.Connection = connect;
          cmd.AddColumn(_tableName, attribute);
          _attributes.Add(attribute);
        }

        //if this attribute contains a list of values
        //then create a seperate table for it
        if (attribute.IsList)
        {
          string tableName = _tableName + attribute.Name;
          using (IDatabaseCommand cmd = _builder.CreateCommand())
          {
            cmd.Connection = connect;
            if (!cmd.DoesTableExists(tableName))
            {
              //no.. then create it
              cmd.CreateTable(tableName);
              cmd.AddColumn(tableName, attribute);
              // create also an index on the column
              cmd.AddIndex(tableName, attribute.Name, "asc");
            }
          }
        }

        // insert the attribute + type in the attribute type table
        using (IDatabaseCommand cmd = _builder.CreateCommand())
        {
          cmd.Connection = connect;
          cmd.InsertAttribute(_attributeTable, attribute);
        }
      }
      IDatabaseNotifier notifier = ServiceScope.Get<IDatabaseNotifier>();
      notifier.Notify(this, DatabaseNotificationType.AttributeAdded, null);
    }

    /// <summary>
    /// Adds a new attribute to the database
    /// </summary>
    /// <param name="name">The attribute name.</param>
    /// <param name="type">The attribute type.</param>
    /// <param name="size">The attribute size.</param>
    public void Add(string name, Type type, int size)
    {
      DbAttribute attr = new DbAttribute(name, type, size);
      Add(attr);
    }

    /// <summary>
    /// Adds a new attribute to the database
    /// </summary>
    /// <param name="name">The attribute name.</param>
    /// <param name="type">The attribute type.</param>
    public void Add(string name, Type type)
    {
      DbAttribute attr = new DbAttribute(name, type, 1024);
      Add(attr);
    }

    /// <summary>
    /// Adds a new Index on the given table and column
    /// </summary>
    /// <param name="table"></param>
    /// <param name="column"></param>
    /// <param name="order"></param>
    public void AddIndex(string table, string column, string order)
    {
      using (IDatabaseConnection connect = _builder.CreateConnection())
      {
        //create a new column for this attribute
        using (IDatabaseCommand cmd = _builder.CreateCommand())
        {
          cmd.Connection = connect;
          cmd.AddIndex(table, column, order);
        }
      }
    }

    /// <summary>
    /// Gets the database builder.
    /// </summary>
    /// <value>The builder.</value>
    public IDatabaseBuilder Builder
    {
      get { return _builder; }
    }

    /// <summary>
    /// Creates a new item
    /// </summary>
    /// <returns></returns>
    public IDbItem CreateNew()
    {
      return new DbItem(this);
    }

    /// <summary>
    /// Exectues the query and returns all items matching the query.
    /// </summary>
    /// <param name="query">The query.</param>
    /// <returns></returns>
    public IList<IDbItem> Query(IQuery query)
    {
      using (IDatabaseConnection connect = _builder.CreateConnection())
      {
        using (IDatabaseCommand cmd = _builder.CreateCommand())
        {
          cmd.Connection = connect;
          return cmd.Query(this, query);
        }
      }
    }

    /// <summary>
    /// checks if the database can execute this query
    /// </summary>
    /// <param name="query"></param>
    /// <returns></returns>
    public bool CanQuery(IQuery query)
    {
      //check the expressions...
      List<string> fieldNames = query.FieldNames;
      foreach (string fieldName in fieldNames)
      {
        bool found = false;
        foreach (IDbAttribute att in _attributes)
        {
          if (String.Compare(att.Name, fieldName, true) == 0)
          {
            found = true;
            break;
          }
        }
        if (!found)
        {
          return false;
        }
      }

      return true;
    }

    /// <summary>
    /// Saves the a list of items to the database
    /// </summary>
    /// <param name="items">The items.</param>
    public void Save(IList<IDbItem> items)
    {
      using (IDatabaseConnection connect = _builder.CreateConnection())
      {
        using (IDatabaseCommand cmd = _builder.CreateCommand())
        {
          cmd.Connection = connect;
          cmd.Save(items);
        }
      }
    }
  }
}
