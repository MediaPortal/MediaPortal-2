#region Copyright (C) 2007-2012 Team MediaPortal

/*
    Copyright (C) 2007-2012 Team MediaPortal
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
using MySql.Data.MySqlClient;
using MediaPortal.Backend.Services.Database;

namespace MediaPortal.Database.MySQL
{
    public class MySQLTransaction : ITransaction
    {
      #region Protected fields

      protected MySqlTransaction _transaction;
      protected ISQLDatabase _database;
      protected IDbConnection _connection;

      #endregion

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
#if DEBUG
        // Return a LoggingDbCommandWrapper to log all CommandText to logfile in DEBUG mode.
        result = new LoggingDbCommandWrapper(result);
#endif
        result.Transaction = _transaction;
        return result;
      }

      #endregion

      #region IDisposable Member

      public void Dispose()
      {
        if (_connection != null)
        {
          _connection.Close();
          _connection = null;
        }
      }

      #endregion

      public static ITransaction BeginTransaction(MySQLDatabase database, string connectionString, IsolationLevel level)
      {
        MySqlConnection connection = new MySqlConnection(connectionString);
        connection.Open();
        return new MySQLTransaction(database, connection, connection.BeginTransaction(level));
      }

      public MySQLTransaction(ISQLDatabase database, IDbConnection connection, MySqlTransaction transaction)
      {
        _database = database;
        _connection = connection;
        _transaction = transaction;
      }
    }
}
