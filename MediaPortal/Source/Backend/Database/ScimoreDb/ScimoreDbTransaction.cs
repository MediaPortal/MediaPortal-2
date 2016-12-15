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
using MediaPortal.Backend.Database;
using MediaPortal.Backend.Services.Database;
using Scimore.Data.ScimoreClient;

namespace MediaPortal.Database.ScimoreDb
{
    public class ScimoreDbTransaction : ITransaction
    {
      protected ScimoreConnection _connection;
      protected ScimoreTransaction _transaction;
      protected ScimoreDb _database;

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
      }

      public void Rollback()
      {
        _transaction.Rollback();
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

      public ScimoreDbTransaction(ScimoreDb database, ScimoreConnection connection, IsolationLevel level)
      {
        _database = database;
        _connection = connection;
        _transaction = new ScimoreTransaction(level, connection);
      }
    }
}
