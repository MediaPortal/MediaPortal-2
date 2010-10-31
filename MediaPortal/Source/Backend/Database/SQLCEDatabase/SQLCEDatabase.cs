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
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with MediaPortal 2. If not, see <http://www.gnu.org/licenses/>.
*/

#endregion

using System;
using System.Data;
using MediaPortal.Backend.Database;
using System.Data.SqlServerCe;
using System.IO;
using MediaPortal.Core.PathManager;
using MediaPortal.Core;
using MediaPortal.Core.Logging;

namespace MediaPortal.BackendComponents.Database.SQLCE
{
  public class SQLCEDatabase : ISQLDatabase
  {
    public const string SQLCE_DATABASE_TYPE = "SQLCE";
    public const string DATABASE_VERSION = "3.5.1.0";
    public const int MAX_NUM_CHARS_CHAR_VARCHAR = 4000;
    public const int LOCK_TIMEOUT = 30000; // Time in ms the database will wait for a lock
    public const int MAX_BUFFER_SIZE = 2048;

    public const string DEFAULT_DATABASE_FILE = "Datastore.sdf";

    protected string _connectionString;

    public SQLCEDatabase()
    {
      try
      {
        IPathManager pathManager = ServiceRegistration.Get<IPathManager>();
        string dataDirectory = pathManager.GetPath("<DATA>");
        string databaseFile = Path.Combine(dataDirectory, DEFAULT_DATABASE_FILE);

        _connectionString = "Data Source='" + databaseFile + "'; Default Lock Timeout=" + LOCK_TIMEOUT + "; Max Buffer Size = " + MAX_BUFFER_SIZE;
        SqlCeEngine engine = new SqlCeEngine(_connectionString);
        if (!File.Exists(databaseFile))
        {
          engine.CreateDatabase();
          engine.Dispose();
        }
      }
      catch (Exception e)
      {
        ServiceRegistration.Get<ILogger>().Critical("Error establishing database connection", e);
        throw;
      }
    }

    #region ISQLDatabase implementation

    public string DatabaseType
    {
      get { return SQLCE_DATABASE_TYPE; }
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
        return "datetime";
      if (dotNetType == typeof(Char))
        return "nchar(1)";
      if (dotNetType == typeof(Boolean))
        return "bit";
      if (dotNetType == typeof(Single))
        return "real";
      if (dotNetType == typeof(Double))
        return "float";
      if (dotNetType == typeof(Byte) || dotNetType == typeof(SByte))
        return "tinyint";
      if (dotNetType == typeof(UInt16) || dotNetType == typeof(Int16))
        return "smallint";
      if (dotNetType == typeof(UInt32) || dotNetType == typeof(Int32))
        return "integer";
      if (dotNetType == typeof(UInt64) || dotNetType == typeof(Int64))
        return "bigint";
      if (dotNetType == typeof(Guid))
        return "uniqueidentifier";
      return null;
    }

    public string GetSQLVarLengthStringType(uint maxNumChars)
    {
      if (IsCLOBNecessary(maxNumChars))
        return "ntext";
      return "nvarchar(" + maxNumChars + ")";
    }

    public bool IsCLOBNecessary(uint maxNumChars)
    {
      return maxNumChars > MAX_NUM_CHARS_CHAR_VARCHAR;
    }

    public string GetSQLFixedLengthStringType(uint maxNumChars)
    {
      if (maxNumChars > MAX_NUM_CHARS_CHAR_VARCHAR)
        return "ntext";
      return "nchar(" + maxNumChars + ")";
    }

    public ITransaction BeginTransaction(IsolationLevel level)
    {
      return SQLCETransaction.BeginTransaction(this, _connectionString, level);
    }

    public ITransaction BeginTransaction()
    {
      return BeginTransaction(IsolationLevel.ReadCommitted);
    }

    public bool TableExists(string tableName)
    {
      using (SqlCeConnection conn = new SqlCeConnection(_connectionString))
      {
        conn.Open();
        using (IDbCommand cmd = conn.CreateCommand())
        {
          cmd.CommandText = @"SELECT COUNT(TABLE_NAME) FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME='" + tableName + "'";
          int cnt = (int) cmd.ExecuteScalar();
          return (cnt == 1);
        }
      }
    }

    #endregion
  }
}
