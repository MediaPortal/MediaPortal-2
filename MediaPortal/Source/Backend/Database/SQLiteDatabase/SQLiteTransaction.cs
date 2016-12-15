#region Copyright (C) 2007-2017 Team MediaPortal

/*
    Copyright (C) 2007-2017 Team MediaPortal
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
using System.Data.SQLite;
using MediaPortal.Backend.Database;
using MediaPortal.Backend.Services.Database;

namespace MediaPortal.Database.SQLite
{
  public class SQLiteTransaction : ITransaction
  {
    #region Variables

    private System.Data.SQLite.SQLiteTransaction _transaction;
    private readonly SQLiteDatabase _database;
    private SQLiteConnection _connection;
    private readonly SQLiteSettings _settings;

    #endregion

    #region Constructors/Destructors

    public SQLiteTransaction(SQLiteDatabase database, SQLiteSettings settings)
    {
      _database = database;
      _settings = settings;
      _connection = _database.ConnectionPool.GetConnection();
    }

    public SQLiteTransaction(SQLiteDatabase database, IsolationLevel level, SQLiteSettings settings)
    {
      _database = database;
      _settings = settings;
      _connection = _database.ConnectionPool.GetConnection();
      _transaction = _connection.BeginTransaction(level);
    }

    #endregion

    #region IDisposable implementation

    public void Dispose()
    {
      // Dispose the System.Data.SQLite.SQLiteTransaction. If neither Commit nor Rollback was
      // called before, the standard behaviour of System.Data.SQLite.SQLiteTransaction is to
      // issue a Rollback during disposing.
      if (_transaction != null)
      {
        _transaction.Dispose();
        _transaction = null;
      }

      // Return the underlying connection to the connection pool without closing it
      if (_connection != null)
      {
        _database.ConnectionPool.PutConnection(_connection);
        _connection = null;
      }
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

    public void Begin(IsolationLevel level)
    {
      if (_transaction != null)
        _transaction.Rollback();
      _transaction = _connection.BeginTransaction(IsolationLevel.Serializable);
    }

    public void Commit()
    {
      if (_transaction != null)
        _transaction.Commit();
      Dispose();
    }

    public void Rollback()
    {
      if (_transaction != null)
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
