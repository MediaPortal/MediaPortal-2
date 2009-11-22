#region Copyright (C) 2007-2009 Team MediaPortal

/*
    Copyright (C) 2007-2009 Team MediaPortal
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

using System.Data;
using FirebirdSql.Data.FirebirdClient;
using MediaPortal.Backend.Database;

namespace MediaPortal.BackendComponents.Database.Firebird
{
  /// <summary>
  /// Encapsulates a db transaction which closes the underlaying Firebird connection automatically when the transaction is
  /// committed or rolled back.
  /// </summary>
  public class FirebirdTransaction : ITransaction
  {
    protected FbConnection _connection;
    protected FbTransaction _transaction;

    public FirebirdTransaction(FbConnection connection, FbTransaction transaction)
    {
      _connection = connection;
      _transaction = transaction;
    }

    ~FirebirdTransaction()
    {
      Dispose();
    }

    public bool IsValid
    {
      get { return _connection != null; }
    }

    public FbTransaction Transaction
    {
      get { return _transaction; }
    }

    public static ITransaction BeginTransaction(string connectionString, IsolationLevel level)
    {
      FbConnection connection = new FbConnection(connectionString);
      connection.Open();
      return new FirebirdTransaction(connection, connection.BeginTransaction(level));
    }

    #region ITransaction implementation

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

    public void Dispose()
    {
      if (_connection != null)
      {
        _connection.Close();
        _connection = null;
      }
    }

    public IDbCommand CreateCommand()
    {
      IDbCommand result = _connection.CreateCommand();
      result.Transaction = _transaction;
      return result;
    }

    #endregion
  }
}