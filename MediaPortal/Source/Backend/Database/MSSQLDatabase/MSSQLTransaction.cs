#region Copyright (C) 2007-2015 Team MediaPortal

/*
    Copyright (C) 2007-2015 Team MediaPortal
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
using MediaPortal.Backend.Database;
using System.Data.SqlClient;
using MediaPortal.Backend.Services.Database;

namespace MediaPortal.Database.MSSQL
{
  public class MSSQLTransaction : ITransaction
  {
    #region Protected fields

    protected SqlTransaction _transaction;
    protected readonly MSSQLDatabase _database;
    protected SqlConnection _connection;
    protected readonly MSSQLDatabaseSettings _settings;

    #endregion

    public MSSQLTransaction(MSSQLDatabase database, IsolationLevel level, MSSQLDatabaseSettings settings)
    {
      _database = database;
      _connection = database.CreateOpenConnection();
      _transaction = _connection.BeginTransaction(level);
      _settings = settings;
    }

    #region ITransaction Member

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
      result.CommandTimeout = MSSQLDatabase.DEFAULT_QUERY_TIMEOUT;
      return result;
    }

    #endregion

    #region IDisposable Member

    public void Dispose()
    {
      _transaction?.Dispose();
      _transaction = null;
      _connection?.Dispose();
      _connection = null;
    }

    #endregion
  }
}
