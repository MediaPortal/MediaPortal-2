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

namespace Components.Database.FireBird
{
  class FireBirdDatabaseBuilder : IDatabaseBuilder
  {
    private string _connectionString;


    #region IDatabaseBuilder Members
    
    /// <summary>
    /// Initializes a new instance of the <see cref="FireBirdDatabaseBuilder"/> class.
    /// </summary>
    public FireBirdDatabaseBuilder()
    {
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
      IDatabaseConnection connect = new FireBirdDatabaseConnection();
      connect.Open(_connectionString);
      return connect;
    }

    public IDatabaseConnection CreateConnection(bool systemDatabase)
    {
      IDatabaseConnection connect = new FireBirdDatabaseConnection();
      connect.Open(_connectionString, systemDatabase);
      return connect;
    }

    /// <summary>
    /// Creates a new command.
    /// </summary>
    /// <returns></returns>
    public IDatabaseCommand CreateCommand()
    {
      return new FireBirdDatabaseCommand();
    }

    /// <summary>
    /// Gets the name of the database.
    /// </summary>
    /// <value>The name of the database.</value>
    public string DatabaseName
    {
      get
      {
        int pos = _connectionString.ToLower().IndexOf("database=");
        pos += "database=".Length;
        int pos2 = _connectionString.IndexOf(";", pos);
        if (pos2 > 0)
        {
          return _connectionString.Substring(pos, pos2 - pos);
        }
        return _connectionString.Substring(pos);
      }
    }

    /// <summary>
    /// Creates a new instance of the FireBirdDatabaseBuilder
    /// </summary>
    /// <returns></returns>
    public IDatabaseBuilder CreateNew()
    {
      return new FireBirdDatabaseBuilder();
    }

    #endregion
  }
}
