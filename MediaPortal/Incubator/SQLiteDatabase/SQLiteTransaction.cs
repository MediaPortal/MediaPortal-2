#region Copyright (C) 2007-2013 Team MediaPortal

/*
    Copyright (C) 2007-2013 Team MediaPortal
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

      private readonly System.Data.SQLite.SQLiteTransaction _transaction;
      private readonly ISQLDatabase _database;
      private SQLiteConnection _connection;

      #endregion

      #region Constructors/Destructors

      public SQLiteTransaction(SQLiteDatabase database, string connectionString, IsolationLevel level)
      {
        _database = database;
        _connection = new SQLiteConnection(connectionString);
        _connection.Open();

        // MP2's database backend uses foreign key constraints to ensure referential integrity.
        // SQLite supports this, but it has to be enabled for each database connection by a PRAGMA command
        // For details see http://www.sqlite.org/foreignkeys.html
        var command = new SQLiteCommand("PRAGMA foreign_keys = ON", _connection);
        command.ExecuteNonQuery();

        _transaction = _connection.BeginTransaction(level);
      }

      #endregion

      #region IDisposable implementation

      public void Dispose()
      {
        if (_connection != null)
        {
          _connection.Close();
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

    }
}
