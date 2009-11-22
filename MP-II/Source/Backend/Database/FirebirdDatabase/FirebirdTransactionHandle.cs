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

using System;
using System.Data;
using FirebirdSql.Data.FirebirdClient;

namespace MediaPortal.Database.Firebird
{
  public class FirebirdTransactionHandle : ITransactionHandle, IDisposable
  {
    protected FirebirdConnectionPool _connectionPool;
    protected FbConnection _connection;
    protected FbTransaction _transaction;
    protected bool _isValid = true;

    public FirebirdTransactionHandle(FirebirdConnectionPool connectionPool, FbConnection connection, FbTransaction transaction)
    {
      _connectionPool = connectionPool;
      _connection = connection;
      _transaction = transaction;
    }

    ~FirebirdTransactionHandle()
    {
      Dispose();
    }

    public void Dispose()
    {
      if (_isValid)
        _connectionPool.ReleaseConnection(_connection);
      _isValid = false;
    }

    public bool IsValid
    {
      get { return _isValid; }
    }

    public FbTransaction Transaction
    {
      get { return _transaction; }
    }

    public FbConnection Connection
    {
      get { return _connection; }
    }

    public IDbConnection AssociatedConnection
    {
      get { return _connection; }
    }

    public static ITransactionHandle BeginTransaction(FirebirdConnectionPool connectionPool, IsolationLevel level)
    {
      FbConnection connection = connectionPool.AcquireConnection();
      return new FirebirdTransactionHandle(connectionPool, connection, connection.BeginTransaction(level));
    }
  }
}