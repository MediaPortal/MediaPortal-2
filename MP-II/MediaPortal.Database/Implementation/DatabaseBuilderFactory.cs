#region Copyright (C) 2007 Team MediaPortal

/*
    Copyright (C) 2007 Team MediaPortal
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
using MediaPortal.Core.Database.Interfaces;
using MediaPortal.Core.PathManager;
using MediaPortal.Database.Implementation.Sql;
using MediaPortal.Database.Implementation.SqlLite;

namespace MediaPortal.Database.Implementation
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
      if (connectionString.StartsWith("sqlserver:"))
      {
        IDatabaseBuilder builder = new SqlDatabaseBuilder(connectionString.Substring("sqlserver:".Length));
        return new DatabaseFactory(builder);
      }
      if (connectionString.StartsWith("sqlite:"))
      {
        IDatabaseBuilder builder = new SqlLiteDatabaseBuilder(connectionString.Substring("sqlite:".Length));
        return new DatabaseFactory(builder);
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
      string connection = string.Format(@"sqlite:Data Source={0}\{1}.db3", location, databaseId);

      return Create(connection);
    }
    #endregion
  }
}