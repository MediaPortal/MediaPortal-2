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

using System.IO;
using MediaPortal.Database.Provider;

namespace Components.Database.SqlLite
{
  public class SqlLiteDatabaseBuilder : IDatabaseBuilder
  {
    #region IDatabaseBuilder Members

    private string _connectionString;

    /// <summary>
    /// Initializes a new instance of the <see cref="SqlDatabaseBuilder"/> class.
    /// </summary>
    public SqlLiteDatabaseBuilder()
    {
    }

    public IDatabaseBuilder CreateNew()
    {
      return new SqlLiteDatabaseBuilder();
    }
    /// <summary>
    /// Stores the database Connection String
    /// </summary>
    /// <value></value>
    public string ConnectionString
    {
      set
      {
        _connectionString = value;

        // Create Database directory if it doesn't exist
        FileInfo databaseFile = new FileInfo(DatabaseName);
        if (!databaseFile.Directory.Exists)
          databaseFile.Directory.Create();
      }
    }

    /// <summary>
    /// Creates a new connection.
    /// </summary>
    /// <returns></returns>
    public IDatabaseConnection CreateConnection()
    {
      IDatabaseConnection connect = new SqlLiteDatabaseConnection();
      connect.Open(_connectionString);

      using (SqlLiteDatabaseCommand dbCommand = new SqlLiteDatabaseCommand())
      {
        dbCommand.SetPragmas(connect.UnderlyingConnection);
      }
      return connect;
    }

    public IDatabaseConnection CreateConnection(bool systemDatabase)
    {
      SqlLiteDatabaseConnection connect = new SqlLiteDatabaseConnection();
      connect.Open(_connectionString, systemDatabase);

      using (SqlLiteDatabaseCommand dbCommand = new SqlLiteDatabaseCommand())
      {
        dbCommand.SetPragmas(connect.UnderlyingConnection);
      }
      return connect;
    }

    /// <summary>
    /// Creates a new command.
    /// </summary>
    /// <returns></returns>
    public IDatabaseCommand CreateCommand()
    {
      return new SqlLiteDatabaseCommand();
    }

    /// <summary>
    /// Gets the name of the database.
    /// </summary>
    /// <value>The name of the database.</value>
    public string DatabaseName
    {
      get
      {
        int pos = _connectionString.ToLower().IndexOf("data source=");
        pos += "data source=".Length;
        int pos2 = _connectionString.IndexOf(";", pos);
        if (pos2 > 0)
        {
          return _connectionString.Substring(pos, pos2 - pos);
        }
        return _connectionString.Substring(pos);
      }
    }

    #endregion
  }
}
