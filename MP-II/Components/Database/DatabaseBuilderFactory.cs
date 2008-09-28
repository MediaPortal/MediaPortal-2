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

using MediaPortal.Core;
using MediaPortal.Database;
using MediaPortal.Core.PathManager;
using MediaPortal.Core.PluginManager;

using MediaPortal.Database.Provider;


namespace Components.Database
{
  public class DatabaseBuilderFactory : IDatabaseBuilderFactory
  {
    #region IBuilderFactory Members
    public DatabaseBuilderFactory()
    {
      ServiceScope.Add<IDatabases>(new Databases());
    }

    /// <summary>
    /// Creates a new database builder based for the connection string
    /// </summary>
    public IDatabaseFactory Create(string connectionString)
    {
      int pos;
      if ((pos = connectionString.IndexOf(':')) > 0)
      {
        string database = connectionString.Substring(0, pos);
        IDatabaseBuilder builder = ServiceScope.Get<IPluginManager>().RequestPluginItem<IDatabaseBuilder>("/Databases", database, new FixedItemStateTracker());
        if (builder != null)
        {
          builder = builder.CreateNew();
          builder.ConnectionString = connectionString.Substring(pos + 1);
          return new DatabaseFactory(builder);
        }
      }
      return null;
    }

    /// <summary>
    /// Creates a new database builder from databaseId
    /// </summary>
    public IDatabaseFactory CreateFromId(string databaseId)
    {
      // create database of default type
      string location = ServiceScope.Get<IPathManager>().GetPath("<DATABASE>");
      string connection = string.Format(@"SqlLite:Data Source={0}\{1}.db3", location, databaseId);
      //string connection = string.Format(@"FireBird:ServerType=1;User=SYSDBA;Password=masterkey;Charset=ISO8859_1;Database={0}\{1}.fdb", location, databaseId);

      return Create(connection);
    }
    #endregion
  }
}
