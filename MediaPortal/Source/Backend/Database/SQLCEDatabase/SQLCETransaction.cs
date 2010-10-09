#region Copyright (C) 2007-2010 Team MediaPortal

/*
    Copyright (C) 2007-2010 Team MediaPortal
    http://www.team-mediaportal.com
 
    This file is part of MediaPortal 2

    MediaPortal 2 is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    MediaPortal 2 is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with MediaPortal 2.  If not, see <http://www.gnu.org/licenses/>.
*/

#endregion

using System.Data;
using MediaPortal.Backend.Database;
using System.Data.SqlServerCe;
using MediaPortal.Backend.Services.Database;

namespace MediaPortal.BackendComponents.Database.SQLCE
{
    public class SQLCETransaction : ITransaction
    {
        SqlCeTransaction _transaction;

        #region ITransaction Member

        public ISQLDatabase Database { get; protected set; }

        public IDbConnection Connection { get; protected set; }

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
            IDbCommand result = Connection.CreateCommand();
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
            if (Connection != null)
            {
                Connection.Close();
                Connection = null;
            }
        }

        #endregion

        public static ITransaction BeginTransaction(SQLCEDatabase database, string connectionString, IsolationLevel level)
        {
            SqlCeConnection connection = new SqlCeConnection(connectionString);
            connection.Open();
            return new SQLCETransaction(database, connection, connection.BeginTransaction(level));
        }

        public SQLCETransaction(SQLCEDatabase database, SqlCeConnection connection, SqlCeTransaction transaction)
        {
            Database = database;
            Connection = connection;
            _transaction = transaction;
        }
    }
}
