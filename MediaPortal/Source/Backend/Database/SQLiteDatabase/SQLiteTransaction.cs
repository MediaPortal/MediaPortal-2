#region Copyright (C) 2007-2020 Team MediaPortal

/*
    Copyright (C) 2007-2020 Team MediaPortal
    http://www.team-mediaportal.com

    This file is part of MediaPortal 2

    MediaPortal 2 is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    MediaPortal 2 is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with MediaPortal 2. If not, see <http://www.gnu.org/licenses/>.
*/

#endregion

using System.Data;
using Microsoft.Data.Sqlite;
using MediaPortal.Backend.Database;
using MediaPortal.Backend.Services.Database;

namespace MediaPortal.Database.SQLite
{
  public class SQLiteTransaction : ITransaction
  {
    #region Variables

    private SqliteTransaction _transaction;
    private readonly SQLiteDatabase _database;
    private SqliteConnection _connection;
    private readonly SQLiteSettings _settings;

    #endregion

    #region Constructors/Destructors

    public SQLiteTransaction(SQLiteDatabase database, IsolationLevel level, SQLiteSettings settings)
    {
      _database = database;
      _settings = settings;
#if NO_POOL
      _connection = _database.CreateOpenAndInitializeConnection();
#else
      _connection = _database.ConnectionPool.GetConnection();
#endif
      _transaction = _connection.BeginTransaction(level);
    }

    #endregion

    #region IDisposable implementation

    public void Dispose()
    {
      // Dispose the SqliteTransaction. If neither Commit nor Rollback was called before, the
      // standard behaviour of Microsoft.Data.Sqlite's transaction is to issue a Rollback while
      // disposing - which is what MP2 relies on here.
      _transaction?.Dispose();
      _transaction = null;
#if NO_POOL
      _connection?.Dispose();
      _connection = null;
#else
      // Return the underlying connection to the connection pool without closing it
      if (_connection != null)
      {
        _database.ConnectionPool.PutConnection(_connection);
        _connection = null;
      }
#endif
    }

    #endregion

    #region ITransaction implementation

    public ISQLDatabase Database
    {
      get { return _database; }
    }

    public IDbConnection Connection
    {
      get { return _connection; }
    }

    public void Commit()
    {
      _transaction.Commit();
      Dispose();
    }

    public void Rollback()
    {
      _transaction.Rollback();
      Dispose();
    }

    public IDbCommand CreateCommand()
    {
      IDbCommand result = _connection.CreateCommand();

      if (_settings.EnableDebugLogging)
        result = new LoggingDbCommandWrapper(result);

      result.Transaction = _transaction;
      return result;
    }

    #endregion
  }
}
