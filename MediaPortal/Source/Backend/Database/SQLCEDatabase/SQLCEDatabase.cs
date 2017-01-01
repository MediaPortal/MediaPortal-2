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
using System.Data.SqlTypes;
using MediaPortal.Backend.Database;
using System.Data.SqlServerCe;
using System.IO;
using MediaPortal.Backend.Services.Database;
using MediaPortal.Common.PathManager;
using MediaPortal.Common;
using MediaPortal.Common.Logging;

namespace MediaPortal.Database.SQLCE
{
  public class SQLCEDatabase : ISQLDatabase
  {
    public const string SQLCE_DATABASE_TYPE = "SQLCE";
    public const string DATABASE_VERSION = "4.0.0.1";
    public const int MAX_NUM_CHARS_CHAR_VARCHAR = 4000;
    public const int LOCK_TIMEOUT = 30000; // Time in ms the database will wait for a lock
    public const int MAX_BUFFER_SIZE = 2048;

    /// <summary>
    /// Maximum size of the shared memory region in MB which SQL CE uses for shared database connections.
    /// The maximum database size defaults to 256 MB, which might be too small for big databases.
    /// </summary>
    public const int INITIAL_MAX_DATABASE_SIZE = 1024;

    /// <summary>
    /// Buffer, the "Max Database Size" parameter must be bigger than the actual database file size, in MB.
    /// </summary>
    public const int DATABASE_SIZE_BUFFER = 256;

    public const string DEFAULT_DATABASE_FILE = "Datastore.sdf";

    protected string _connectionString;

    public SQLCEDatabase()
    {
      try
      {
        IPathManager pathManager = ServiceRegistration.Get<IPathManager>();
        string dataDirectory = pathManager.GetPath("<DATABASE>");
        string databaseFile = Path.Combine(dataDirectory, DEFAULT_DATABASE_FILE);

        int databaseSize = INITIAL_MAX_DATABASE_SIZE;
        FileInfo databaseFileInfo = new FileInfo(databaseFile);
        if (databaseFileInfo.Exists)
        {
          int bufferFileSize = (int) (databaseFileInfo.Length/(1024*1024)) + DATABASE_SIZE_BUFFER;
          if (bufferFileSize > databaseSize)
            databaseSize = bufferFileSize;
        }

        _connectionString = "Data Source='" + databaseFile + "'; Default Lock Timeout=" + LOCK_TIMEOUT + "; Max Buffer Size = " + MAX_BUFFER_SIZE + "; Max Database Size = " + databaseSize;
        SqlCeEngine engine = new SqlCeEngine(_connectionString);
        if (File.Exists(databaseFile))
          CheckUpgrade(engine);
        else
        {
          Directory.CreateDirectory(dataDirectory);
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

    /// <summary>
    /// Tries to establish a connection to the given database. If this fails with a <see cref="SqlCeInvalidDatabaseFormatException"/>, we try
    /// to update the database to current engine version.
    /// </summary>
    /// <param name="engine">Database engine instance to upgrade.</param>
    protected void CheckUpgrade(SqlCeEngine engine)
    {
      try
      {
        using (SqlCeConnection conn = new SqlCeConnection(_connectionString))
        {
          conn.Open();
          conn.Close();
        }
      }
      catch (SqlCeInvalidDatabaseFormatException)
      {
        ServiceRegistration.Get<ILogger>().Warn("Upgrading existing SQL CE database format to v4.0...");
        engine.Upgrade();
        ServiceRegistration.Get<ILogger>().Warn("Upgrade successful!");
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
        return "DATETIME";
      if (dotNetType == typeof(Char))
        return "NCHAR(1)";
      if (dotNetType == typeof(Boolean))
        return "BIT";
      if (dotNetType == typeof(Single))
        return "REAL";
      if (dotNetType == typeof(Double))
        return "FLOAT";
      if (dotNetType == typeof(Byte) || dotNetType == typeof(SByte))
        return "TINYINT";
      if (dotNetType == typeof(UInt16) || dotNetType == typeof(Int16))
        return "SMALLINT";
      if (dotNetType == typeof(UInt32) || dotNetType == typeof(Int32))
        return "INTEGER";
      if (dotNetType == typeof(UInt64) || dotNetType == typeof(Int64))
        return "BIGINT";
      if (dotNetType == typeof(Guid))
        return "UNIQUEIDENTIFIER";
      if (dotNetType == typeof(byte[]))
        return "IMAGE";
      return null;
    }

    public string GetSQLVarLengthStringType(uint maxNumChars)
    {
      if (maxNumChars > MAX_NUM_CHARS_CHAR_VARCHAR)
        return "NTEXT";
      return "NVARCHAR(" + maxNumChars + ")";
    }

    public string GetSQLFixedLengthStringType(uint maxNumChars)
    {
      if (maxNumChars > MAX_NUM_CHARS_CHAR_VARCHAR)
        return "NTEXT";
      return "NCHAR(" + maxNumChars + ")";
    }

    public bool IsCLOB(uint maxNumChars)
    {
      return maxNumChars > MAX_NUM_CHARS_CHAR_VARCHAR;
    }

    public IDbDataParameter AddParameter(IDbCommand command, string name, object value, Type type)
    {
      if (type == typeof(byte[]))
      {
        SqlCeParameter result = (SqlCeParameter) command.CreateParameter();
        result.ParameterName = name;
        result.Value = value ?? DBNull.Value;
        result.SqlDbType = SqlDbType.Image;
        command.Parameters.Add(result);
        return result;
      }
      // We need to use NText as parameter type, if the value is of "IsCLOB" type.
      if (type == typeof(string) && value != null && IsCLOB((uint) value.ToString().Length))
      {
        SqlCeParameter result = (SqlCeParameter) command.CreateParameter();
        result.ParameterName = name;
        result.Value = value;
        result.SqlDbType = SqlDbType.NText;
        command.Parameters.Add(result);
        return result;
      }
      return DBUtils.AddSimpleParameter(command, name, value, type);
    }

    public object ReadDBValue(Type type, IDataReader reader, int colIndex)
    {
      if (reader.IsDBNull(colIndex))
        return null;
      if (type == typeof(byte[]))
      {
        SqlBinary result = ((SqlCeDataReader) reader).GetSqlBinary(colIndex);
        return result.Value;
      }
      return DBUtils.ReadSimpleDBValue(type, reader, colIndex);
    }

    public ITransaction BeginTransaction(IsolationLevel level)
    {
      return SQLCETransaction.BeginTransaction(this, _connectionString, level);
    }

    public ITransaction BeginTransaction()
    {
      return BeginTransaction(IsolationLevel.ReadCommitted);
    }

    public ITransaction CreateTransaction()
    {
      return SQLCETransaction.CreateTransaction(this, _connectionString);
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

    public string CreateStringConcatenationExpression(string str1, string str2)
    {
      return str1 + "+" + str2;
    }

    public string CreateSubstringExpression(string str1, string posExpr)
    {
      return "SUBSTRING(" + str1 + "," + posExpr + "," + Int32.MaxValue + ")"; // Int32.MaxValue seems to be the biggest supported value
    }

    public string CreateSubstringExpression(string str1, string posExpr, string lenExpr)
    {
      return "SUBSTRING(" + str1 + "," + posExpr + "," + lenExpr + ")";
    }

    public string CreateDateToYearProjectionExpression(string selectExpression)
    {
      return "DATEPART(YEAR, " + selectExpression + ")";
    }

    #endregion
  }
}
