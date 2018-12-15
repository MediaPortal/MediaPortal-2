#region Copyright (C) 2007-2018 Team MediaPortal

/*
    Copyright (C) 2007-2018 Team MediaPortal
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

using MediaPortal.Backend.Database;
using MediaPortal.Backend.Services.Database;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Common.Settings;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Data.SqlTypes;

namespace MediaPortal.Database.MSSQL
{
  public class MSSQLDatabase : ISQLDatabase
  {
    #region Constants

    public const string MSSQL_DATABASE_TYPE = "MSSQL";
    public const int MAX_NUM_CHARS_CHAR_VARCHAR = 4000;
    public const int DEFAULT_CONNECTION_TIMEOUT = 15;
    public const int DEFAULT_QUERY_TIMEOUT = 600;
    public const int INITIAL_LOG_SIZE = 50;
    public const int INITIAL_DATA_SIZE = 200;

    #endregion

    #region Variables

    private readonly string _connectionString;
    private readonly MSSQLDatabaseSettings _settings;
    private string _version;

    #endregion

    public MSSQLDatabase()
    {
      try
      {
        _settings = ServiceRegistration.Get<ISettingsManager>().Load<MSSQLDatabaseSettings>();
        _settings.LogSettings();

        _connectionString = CreateDatabaseConnection();

        LogVersionInformation();
      }
      catch (Exception e)
      {
        ServiceRegistration.Get<ILogger>().Critical("MSSQLDatabase: Error establishing database connection", e);
        throw;
      }
    }

    #region Private Methods

    private bool TestConnection(string connection)
    {
      try
      {
        SqlConnection sqlTestConn = new SqlConnection(connection);
        sqlTestConn.Open();
        _version = sqlTestConn.ServerVersion;
        sqlTestConn.Close();
        return true;
      }
      catch (Exception ex)
      {
        ServiceRegistration.Get<ILogger>().Error("Unable to connect to database {0}@{1}.", ex, _settings.DatabaseName, _settings.DatabaseInstance == "." ? "(local)" : _settings.DatabaseInstance);
      }
      return false;
    }

    private string CreateDatabaseConnection()
    {
      string connection = "";

      //Init connection string
      SqlConnectionStringBuilder sqlStr = new SqlConnectionStringBuilder();
      sqlStr.DataSource = _settings.DatabaseInstance;
      sqlStr.PersistSecurityInfo = true;
      sqlStr.WorkstationID = Environment.MachineName;
      sqlStr.ConnectTimeout = DEFAULT_CONNECTION_TIMEOUT;
      sqlStr.MaxPoolSize = _settings.MaxConnectionPoolSize;
      sqlStr.Pooling = _settings.UseConnectionPool;
      sqlStr.ApplicationName = "MP2 Server";
      sqlStr.UserID = _settings.DatabaseUser;
      sqlStr.Password = _settings.DatabasePassword;
      sqlStr.InitialCatalog = _settings.DatabaseName;
      connection = sqlStr.ConnectionString;

      if (TestConnection(connection))
        return connection; //If connection successful presume that database is initialized

      //Init connection string for initialization
      sqlStr.IntegratedSecurity = true;
      sqlStr.InitialCatalog = "master";

      //Open connection
      SqlConnection sqlConn = new SqlConnection(sqlStr.ConnectionString);
      sqlConn.Open();
      _version = sqlConn.ServerVersion;
      try
      {
        SqlCommand sqlCmd = new SqlCommand();
        sqlCmd.CommandTimeout = DEFAULT_QUERY_TIMEOUT;
        sqlCmd.Connection = sqlConn;
        sqlCmd.CommandType = System.Data.CommandType.Text;
        sqlCmd.CommandText = "SELECT COUNT(*) FROM SYSDATABASES WHERE NAME = '" + _settings.DatabaseName + "'";
        int _tableCount = Convert.ToInt32(sqlCmd.ExecuteScalar());
        if (_tableCount == 0)
        {
          ServiceRegistration.Get<ILogger>().Info("Database not found. Creating database.");

          //Create the database
          sqlCmd.CommandText = "CREATE DATABASE [" + _settings.DatabaseName + "] COLLATE Latin1_General_CI_AS";
          sqlCmd.ExecuteNonQuery();
          sqlCmd.CommandText = "ALTER DATABASE [" + _settings.DatabaseName + "] MODIFY FILE (NAME = N'" + _settings.DatabaseName + "', SIZE = " + INITIAL_DATA_SIZE + "MB, FILEGROWTH = " + _settings.DatabaseFileGrowSize + "MB, MAXSIZE = UNLIMITED)";
          sqlCmd.ExecuteNonQuery();
          sqlCmd.CommandText = "ALTER DATABASE [" + _settings.DatabaseName + "] MODIFY FILE (NAME = N'" + _settings.DatabaseName + "_Log', SIZE = " + INITIAL_LOG_SIZE + "MB, FILEGROWTH = " + _settings.DatabaseLogFileGrowSize + "MB, MAXSIZE = UNLIMITED)";
          sqlCmd.ExecuteNonQuery();

          //Ensure that transaction logging is set to simple
          sqlCmd.CommandText = "ALTER DATABASE [" + _settings.DatabaseName + "] SET RECOVERY SIMPLE";
          sqlCmd.ExecuteNonQuery();

          //Ensure that database is always available
          sqlCmd.CommandText = "ALTER DATABASE [" + _settings.DatabaseName + "] SET AUTO_CLOSE OFF";
          sqlCmd.ExecuteNonQuery();

          //Enable snapshot isolation so deadlocks can be avoided during import
          sqlCmd.CommandText = "ALTER DATABASE [" + _settings.DatabaseName + "] SET ALLOW_SNAPSHOT_ISOLATION ON";
          sqlCmd.ExecuteNonQuery();
        }

        sqlCmd.CommandText = "SELECT COUNT(*) FROM SYSLOGINS WHERE LOGINNAME = N'" + _settings.DatabaseUser + "'";
        int _userCount = Convert.ToInt32(sqlCmd.ExecuteScalar());
        if (_userCount == 0)
        {
          //Create MP user
          sqlCmd.CommandText = "CREATE LOGIN [" + _settings.DatabaseUser + "] WITH PASSWORD=N'" + _settings.DatabasePassword + "', DEFAULT_DATABASE=["+ _settings.DatabaseName + "], DEFAULT_LANGUAGE=[us_english], CHECK_EXPIRATION=OFF, CHECK_POLICY=OFF";
          sqlCmd.ExecuteNonQuery();

          //Give user the necessary rights
          sqlCmd.CommandText = "GRANT VIEW ANY DEFINITION TO [" + _settings.DatabaseUser + "]";
          sqlCmd.ExecuteNonQuery();
          sqlCmd.CommandText = "USE [" + _settings.DatabaseName + "]";
          sqlCmd.ExecuteNonQuery();
          sqlCmd.CommandText = "CREATE USER [" + _settings.DatabaseUser + "] FOR LOGIN [" + _settings.DatabaseUser + "] WITH DEFAULT_SCHEMA=dbo";
          sqlCmd.ExecuteNonQuery();
          sqlCmd.CommandText = "EXEC SP_ADDROLEMEMBER N'db_owner', N'" + _settings.DatabaseUser + "'";
          sqlCmd.ExecuteNonQuery();
          sqlCmd.CommandText = "ALTER LOGIN ["+ _settings.DatabaseUser + "] ENABLE";
          sqlCmd.ExecuteNonQuery();

          sqlCmd.CommandText = "EXEC master.sys.xp_loginconfig 'login mode'";
          string loginMode = (string)sqlCmd.ExecuteScalar();
          if (loginMode.IndexOf("mixed", StringComparison.InvariantCultureIgnoreCase) == -1)
            ServiceRegistration.Get<ILogger>().Warn("Mixed mode login must be enabled");
        }
      }
      finally
      {
        sqlConn.Close();
      }

      //Try connecting again with admin connection because user login won't work yet
      sqlStr.InitialCatalog = _settings.DatabaseName;
      connection = sqlStr.ConnectionString;
      TestConnection(connection);
      return connection;
    }

    /// <summary>
    /// Logs version information about the used database
    /// </summary>
    private void LogVersionInformation()
    {
      ServiceRegistration.Get<ILogger>().Info("MSSQLDatabase: Version: {0}", _version);
    }

    #endregion

    #region ISQLDatabase implementation

    public string DatabaseType
    {
      get { return MSSQL_DATABASE_TYPE; }
    }

    public string DatabaseVersion
    {
      get { return _version; }
    }

    public uint MaxObjectNameLength
    {
      get { return 30; }
    }

    public string ConcatOperator
    {
      get { return "+"; }
    }

    public string LengthFunction
    {
      get { return "LEN"; }
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
        return "INTEGER";
      if (dotNetType == typeof(UInt16) || dotNetType == typeof(Int16))
        return "INTEGER";
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
        SqlParameter result = (SqlParameter)command.CreateParameter();
        result.ParameterName = name;
        result.Value = value ?? DBNull.Value;
        result.SqlDbType = SqlDbType.Image;
        command.Parameters.Add(result);
        return result;
      }
      // We need to use NText as parameter type, if the value is of "IsCLOB" type.
      if (type == typeof(string) && value != null && IsCLOB((uint)value.ToString().Length))
      {
        SqlParameter result = (SqlParameter)command.CreateParameter();
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
        SqlBinary result = ((SqlDataReader)reader).GetSqlBinary(colIndex);
        return result.Value;
      }
      return DBUtils.ReadSimpleDBValue(type, reader, colIndex);
    }

    public ITransaction BeginTransaction(IsolationLevel level)
    {
      return new MSSQLTransaction(this, level, _settings);
    }

    public ITransaction BeginTransaction()
    {
      // Always use snapshot isolation level to avoid deadlocks in the database in multi-threaded scenarios like 
      // MP2 does during import. If using other isolation levels where shared read locks are used during queries,
      // the database will get into a deadlock when write locks need to escalate their locks on the same table/row,
      // so we override any requested IsolationLevel other than Snapshot.
      return BeginTransaction(IsolationLevel.Snapshot);
    }

    public bool TableExists(string tableName)
    {
      using (SqlConnection conn = new SqlConnection(_connectionString))
      {
        conn.Open();
        using (IDbCommand cmd = conn.CreateCommand())
        {
          cmd.CommandText = @"SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME=N'" + tableName + "'";
          int cnt = (int)cmd.ExecuteScalar();
          return (cnt == 1);
        }
      }
    }

    public bool BackupDatabase(string backupVersion)
    {
      using (SqlConnection conn = new SqlConnection(_connectionString))
      {
        conn.Open();
        using (IDbCommand cmd = conn.CreateCommand())
        {
          cmd.CommandText = $"BACKUP DATABASE {_settings.DatabaseName} TO DISK='{_settings.DatabaseName}.{backupVersion}.bak' WITH NAME='MediaPortal Database {backupVersion}'";
          cmd.ExecuteNonQuery();
          return true;
        }
      }
    }

    public bool BackupTables(string tableSuffix)
    {
      bool result = false;
      List<string> tables = new List<string>();
      using (var transaction = BeginTransaction())
      {
        using (var cmd = transaction.CreateCommand())
        {
          //Find all tables
          cmd.CommandText = @"SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_TYPE='BASE TABLE' AND TABLE_NAME <> 'sysdiagrams'";
          using (IDataReader reader = cmd.ExecuteReader())
          {
            while (reader.Read())
            {
              string table = reader.GetString(0);
              if (!table.EndsWith(tableSuffix, StringComparison.InvariantCultureIgnoreCase))
              {
                tables.Add(table);
              }
            }
          }

          //Drop all foreign keys
          List<string> dropSqls = new List<string>();
          cmd.CommandText = @"SELECT TABLE_NAME,CONSTRAINT_NAME FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS WHERE CONSTRAINT_TYPE = 'FOREIGN KEY'";
          using (IDataReader reader = cmd.ExecuteReader())
          {
            while (reader.Read())
            {
              string table = reader.GetString(0);
              if (tables.Contains(table))
              {
                dropSqls.Add($"ALTER TABLE [{table}] DROP CONSTRAINT [{reader.GetString(1)}]");
              }
            }
          }
          foreach(string dropSql in dropSqls)
          {
            cmd.CommandText = dropSql;
            cmd.ExecuteNonQuery();
          }

          //Rename all primary keys
          List<string> renameSqls = new List<string>();
          cmd.CommandText = @"SELECT TABLE_NAME,CONSTRAINT_NAME FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS WHERE CONSTRAINT_TYPE='PRIMARY KEY' OR CONSTRAINT_TYPE='UNIQUE'";
          using (IDataReader reader = cmd.ExecuteReader())
          {
            while (reader.Read())
            {
              string table = reader.GetString(0);
              if (tables.Contains(table))
              {
                renameSqls.Add($"EXEC sp_rename '{table}.{reader.GetString(1)}', 'PK_{renameSqls.Count + 1}{tableSuffix}'");
              }
            }
          }
          foreach (string renameSql in renameSqls)
          {
            cmd.CommandText = renameSql;
            cmd.ExecuteNonQuery();
          }

          //Rename all tables and remove constraints
          foreach (var table in tables)
          {
            //Check if backup table exists
            cmd.CommandText = $"SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_TYPE='BASE TABLE' AND TABLE_NAME='{table + tableSuffix}'";
            var cnt = Convert.ToInt64(cmd.ExecuteScalar());
            if (cnt == 0)
            {
              //Rename table
              cmd.CommandText = $"EXEC sp_rename '{table}','{table + tableSuffix}'";
              cmd.ExecuteNonQuery();

              result = true;
            }
            else
            {
              result = true;
            }
          }

          transaction.Commit();
        }
      }
      return result;
    }

    public bool DropBackupTables(string tableSuffix)
    {
      List<string> tables = new List<string>();
      using (var transaction = BeginTransaction())
      {
        using (var cmd = transaction.CreateCommand())
        {
          //Find all tables
          cmd.CommandText = @"SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_TYPE='BASE TABLE' AND TABLE_NAME <> 'sysdiagrams'";
          using (IDataReader reader = cmd.ExecuteReader())
          {
            while (reader.Read())
            {
              string table = reader.GetString(0);
              if (table.EndsWith(tableSuffix, StringComparison.InvariantCultureIgnoreCase))
                tables.Add(table);
            }
          }

          //Drop all backup tables as they are no longer needed
          foreach (var table in tables)
          {
            //Drop table
            cmd.CommandText = $"DROP TABLE {table}";
            cmd.ExecuteNonQuery();
          }

          transaction.Commit();
        }
      }

      using (var connection = new SqlConnection(_connectionString))
      {
        using (var cmd = connection.CreateCommand())
        {
          try
          {
            //Shrink database
            cmd.CommandText = "DBCC SHRINKDATABASE(0)";
            cmd.ExecuteNonQuery();
          }
          catch (Exception e)
          {
            ServiceRegistration.Get<ILogger>().Error("MSSQLDatabase: Error shrinking database", e);
          }
        }
      }
      return true;
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

    #region Private methods

    /// <summary>
    /// Creates a new <see cref="SqlConnection"/> object and opens it
    /// </summary>
    /// <returns>Newly created and opened <see cref="SqlConnection"/></returns>
    internal SqlConnection CreateOpenConnection()
    {
      SqlConnection connection = new SqlConnection(_connectionString);
      connection.Open();
      return connection;
    }

    #endregion
  }
}
