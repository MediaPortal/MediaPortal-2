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

using System;
using System.Data;
using MediaPortal.Backend.Database;
using System.IO;
using MediaPortal.Common.PathManager;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using Scimore.Data.ScimoreClient;

namespace MediaPortal.Database.ScimoreDb
{
  public class ScimoreDb : ISQLDatabase, IDisposable
  {
    public const string DATABASE_TYPE = "ScimoreDB";
    public const string DATABASE_VERSION = "3.0 RC1";
    public const string DEFAULT_DATABASE_FILE = "Datastore";

    protected readonly ScimoreEmbedded _db = new ScimoreEmbedded();
    protected static string _connectionString;

    public ScimoreDb()
    {
      try
      {
        IPathManager pathManager = ServiceRegistration.Get<IPathManager>();
        string dataDirectory = pathManager.GetPath("<DATA>");
        string databasePath = Path.Combine(dataDirectory, DEFAULT_DATABASE_FILE);

        const string DATABASE_NAME = "MediaPortal";
        _connectionString = "Initial Catalog=" + DATABASE_NAME;

        _db.Create(databasePath);
        _db.Open(databasePath);
        using (ScimoreConnection conn = _db.CreateConnection(_connectionString))
        {
          using (IDbCommand command = conn.CreateCommand())
          {
            command.CommandText = "CREATE DATABASE IF NOT EXISTS " + DATABASE_NAME;
            command.ExecuteNonQuery();
          }
        }
      }
      catch (Exception e)
      {
        ServiceRegistration.Get<ILogger>().Critical("Error establishing database connection", e);
        throw;
      }
    }

    #region IDisposable implementation

    public void Dispose()
    {
      _db.Dispose();
    }

    #endregion

    #region ISQLDatabase implementation

    public string DatabaseType
    {
      get { return DATABASE_TYPE; }
    }

    public string DatabaseVersion
    {
      get { return DATABASE_VERSION; }
    }

    public uint MaxObjectNameLength
    {
      get { return 30; }
    }

    public string GetSQLType(Type dotNetType)
    {
      if (dotNetType == typeof(DateTime))
        return "DATETIME";
      if (dotNetType == typeof(Char))
        return "CHAR(1)";
      if (dotNetType == typeof(Boolean))
        return "TINYINT";
      if (dotNetType == typeof(Single))
        return "FLOAT";
      if (dotNetType == typeof(Double))
        return "DOUBLE";
      if (dotNetType == typeof(Byte) || dotNetType == typeof(SByte) || dotNetType == typeof(Int16))
        return "TINYINT";
      if (dotNetType == typeof(UInt16) || dotNetType == typeof(Int32))
        return "INTEGER";
      if (dotNetType == typeof(UInt32) || dotNetType == typeof(Int64) || dotNetType == typeof(UInt64))
        return "BIGINT";
      if (dotNetType == typeof(Guid))
        return "GUID";
      return null;
    }

    public string GetSQLVarLengthStringType(uint maxNumChars)
    {
      return "NVARCHAR(" + maxNumChars + ")";
    }

    public bool IsCLOB(uint maxNumChars)
    {
      return false;
    }

    public string GetSQLFixedLengthStringType(uint maxNumChars)
    {
      return "NCHAR(" + maxNumChars + ")";
    }

    public ITransaction BeginTransaction(IsolationLevel level)
    {
      return new ScimoreDbTransaction(this, _db.CreateConnection(_connectionString), level);
    }

    public ITransaction BeginTransaction()
    {
      return BeginTransaction(IsolationLevel.ReadCommitted);
    }

    public bool TableExists(string tableName)
    {
      using (ITransaction transaction = BeginTransaction(IsolationLevel.ReadUncommitted))
      {
        using (IDbCommand cmd = transaction.CreateCommand())
        {
          cmd.CommandText = "SELECT COUNT(ID) FROM SYSTEM.SYSTABLES WHERE NAME=@TABLENAME";
          DBUtils.AddParameter(cmd, "TABLENAME", tableName, DbType.AnsiString);
          long cnt = (long) cmd.ExecuteScalar();
          return cnt > 0;
        }
      }
    }

    #endregion
  }
}
