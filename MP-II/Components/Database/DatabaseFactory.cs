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

namespace Components.Database
{
  public class DatabaseFactory : IDatabaseFactory
  {
    private readonly IDatabaseBuilder _builder;

    /// <summary>
    /// Initializes a new instance of the <see cref="DatabaseFactory"/> class.
    /// </summary>
    /// <param name="builder">The builder.</param>
    public DatabaseFactory(IDatabaseBuilder builder)
    {
      _builder = builder;

      //create database, if it doesnt exists yet
      using (IDatabaseConnection connect = _builder.CreateConnection(true))
      {
        using (IDatabaseCommand cmd = _builder.CreateCommand())
        {
          cmd.Connection = connect;
          if (false == cmd.DoesDatabaseExists(_builder.DatabaseName))
          {
            cmd.CreateDatabase(_builder.DatabaseName);
          }
        }
      }
    }

    /// <summary>
    /// Opens the specified database.
    /// </summary>
    /// <param name="name">The name.</param>
    /// <returns></returns>
    public IDatabase Open(string name)
    {
      using (IDatabaseConnection connect = _builder.CreateConnection())
      {
        using (IDatabaseCommand cmd = _builder.CreateCommand())
        {
          //create main table if it doesnt exists yet
          cmd.Connection = connect;
          if (!cmd.DoesTableExists(name))
          {
            cmd.CreateTable(name);
          }
          //create table for attribute types if it doesnt exists yet
          string _attributeTable = name + "Types";
          if (false == cmd.DoesTableExists(_attributeTable))
          {
            cmd.CreateTable(_attributeTable);
            cmd.AddColumn(_attributeTable, new DbAttribute("name", typeof(string), 80));
            cmd.AddColumn(_attributeTable, new DbAttribute("type", typeof(string), 80));
            cmd.AddColumn(_attributeTable, new DbAttribute("size", typeof(int)));
          }
          IDatabase dbs = new Database(name, _builder);
          IDatabaseNotifier notifier = ServiceScope.Get<IDatabaseNotifier>();
          notifier.Notify(dbs, DatabaseNotificationType.DatabaseCreated, null);

          IDatabases dbsRegistered = ServiceScope.Get<IDatabases>();
          if (!dbsRegistered.Contains(name))
            dbsRegistered.DatabasesRegistered.Add(dbs);
          return dbs;
        }
      }
    }

    /// <summary>
    /// returns if the specified database exists
    /// </summary>
    /// <param name="name">The name.</param>
    /// <returns></returns>
    public bool Exists(string name)
    {
      ICollection<IDatabase> databases = this.Databases;
      foreach (IDatabase db in databases)
      {
        if (String.Compare(db.Name, name, true) == 0) return true;
      }
      return false;
    }

    /// <summary>
    /// Returns a list of all databases.
    /// </summary>
    /// <value>The databases.</value>
    public ICollection<IDatabase> Databases
    {
      get
      {
        List<IDatabase> list = new List<IDatabase>();
        using (IDatabaseConnection connect = _builder.CreateConnection())
        {
          using (IDatabaseCommand cmd = _builder.CreateCommand())
          {
            cmd.Connection = connect;
            IList<string> tables = cmd.GetTables();
            foreach (string tableName in tables)
            {
              list.Add(new Database(tableName, _builder));
            }
          }
        }
        return list;
      }
    }
  }
}
