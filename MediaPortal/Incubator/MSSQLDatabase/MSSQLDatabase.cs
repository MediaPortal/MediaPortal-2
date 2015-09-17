#region Copyright (C) 2007-2015 Team MediaPortal

/*
    Copyright (C) 2007-2015 Team MediaPortal
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
using MediaPortal.Common.PathManager;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Data.SqlTypes;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MediaPortal.Database.MSSQL
{
  public class MSSQLDatabase : ISQLDatabase
  {
    public const string MSSQL_DATABASE_TYPE = "MSSQL";
    public const int MAX_NUM_CHARS_CHAR_VARCHAR = 4000;
    public const int MAX_CONNECTION_POOL_SIZE = 5000;
    public const bool USE_CONNECTION_POOL = true;
    public const int DEFAULT_CONNECTION_TIMEOUT = 15;
    public const int DEFAULT_QUERY_TIMEOUT = 30;
    public const int INITIAL_LOG_SIZE = 50;
    public const int INITIAL_DATA_SIZE = 200;
    public const int LOG_GROWTH_SIZE = 25;
    public const int DATA_GROWTH_SIZE = 100;
    public const string DEFAULT_DATABASE_FILE = "MP2Datastore.mdf";
    public const string DEFAULT_DATABASE_LOG = "MP2Datastore.ldf";

    #region Fields

    protected readonly string _connectionString;
    protected readonly string _version;
    protected readonly string _dbSchema;
    protected readonly string _server;
    protected readonly string _initUsername;
    protected readonly string _initPassword;
    protected readonly string _username;
    protected readonly string _password;

    #endregion

    public MSSQLDatabase()
    {
      try
      {
        MSSQLSettingsReader.Read(out _server, out _initUsername, out _initPassword, out _username, out _password, out _dbSchema);
        _connectionString = CreateDatabaseConnection(out _version);
      }
      catch (Exception e)
      {
        ServiceRegistration.Get<ILogger>().Critical("Error establishing database connection", e);
        throw;
      }
    }

    private string CreateDatabaseConnection(out string version)
    {
      string connection = "";
      version = "";

      //Init connection string
      SqlConnectionStringBuilder sqlStr = new SqlConnectionStringBuilder();
      sqlStr.DataSource = _server;
      sqlStr.PersistSecurityInfo = true;
      sqlStr.WorkstationID = Environment.MachineName;
      sqlStr.ConnectTimeout = DEFAULT_CONNECTION_TIMEOUT;
      sqlStr.MaxPoolSize = MAX_CONNECTION_POOL_SIZE;
      sqlStr.Pooling = USE_CONNECTION_POOL;
      sqlStr.ApplicationName = "MP2 Server";
      sqlStr.UserID = _username;
      sqlStr.Password = _password;
      sqlStr.InitialCatalog = _dbSchema;
      connection = sqlStr.ConnectionString;

      try
      {
        SqlConnection sqlTestConn = new SqlConnection(sqlStr.ConnectionString);
        sqlTestConn.Open();
        version = sqlTestConn.ServerVersion;
        sqlTestConn.Close();

        //If connection successful presume that database is initialized
        return connection;
      }
      catch { }

      //Init connection string for initialization
      sqlStr.UserID = _initUsername;
      sqlStr.Password = _initPassword;
      sqlStr.InitialCatalog = "master";

      //Open connection
      SqlConnection sqlConn = new SqlConnection(sqlStr.ConnectionString);
      sqlConn.Open();
      version = sqlConn.ServerVersion;
      try
      {
        SqlCommand sqlCmd = new SqlCommand();
        sqlCmd.CommandTimeout = DEFAULT_QUERY_TIMEOUT;
        sqlCmd.Connection = sqlConn;
        sqlCmd.CommandType = System.Data.CommandType.Text;
        sqlCmd.CommandText = "SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = '" + _dbSchema + "'";
        int _tableCount = Convert.ToInt32(sqlCmd.ExecuteScalar());
        if (_tableCount == 0)
        {

          //Create the database
          sqlCmd.CommandText = "CREATE DATABASE [" + _dbSchema + "] COLLATE Latin1_General_CI_AS";
          sqlCmd.ExecuteNonQuery();
          sqlCmd.CommandText = "ALTER DATABASE [" + _dbSchema + "] MODIFY FILE (NAME = N'" + _dbSchema + "', SIZE = " + INITIAL_DATA_SIZE + "MB, FILEGROWTH = " + DATA_GROWTH_SIZE + "MB, MAXSIZE = UNLIMITED)";
          sqlCmd.ExecuteNonQuery();
          sqlCmd.CommandText = "ALTER DATABASE [" + _dbSchema + "] MODIFY FILE (NAME = N'" + _dbSchema + "_Log', SIZE = " + INITIAL_LOG_SIZE + "MB, FILEGROWTH = " + LOG_GROWTH_SIZE + "MB, MAXSIZE = UNLIMITED)";
          sqlCmd.ExecuteNonQuery();

          //Ensure that transaction logging is set to simple
          sqlCmd.CommandText = "ALTER DATABASE [" + _dbSchema + "] SET RECOVERY SIMPLE";
          sqlCmd.ExecuteNonQuery();

          //Ensure that database is always available
          sqlCmd.CommandText = "ALTER DATABASE [" + _dbSchema + "] SET AUTO_CLOSE OFF";
          sqlCmd.ExecuteNonQuery();
        }

        sqlCmd.CommandText = "SELECT COUNT(*) FROM SYSLOGINS WHERE LOGINNAME = N'" + _username + "'";
        int _userCount = Convert.ToInt32(sqlCmd.ExecuteScalar());
        if (_userCount == 0)
        {
          //Create MP user
          sqlCmd.CommandText = "CREATE LOGIN [" + _username + "] WITH PASSWORD=N'" + _password + "', DEFAULT_LANGUAGE=[us_english], CHECK_EXPIRATION=OFF, CHECK_POLICY=OFF";
          sqlCmd.ExecuteNonQuery();

          //Give user the necessary rights
          sqlCmd.CommandText = "GRANT VIEW ANY DEFINITION TO [" + _username + "]";
          sqlCmd.ExecuteNonQuery();
          sqlCmd.CommandText = "USE [" + _dbSchema + "]";
          sqlCmd.ExecuteNonQuery();
          sqlCmd.CommandText = "CREATE USER [" + _username + "] FOR LOGIN [" + _username + "] WITH DEFAULT_SCHEMA=dbo";
          sqlCmd.ExecuteNonQuery();
          sqlCmd.CommandText = "EXEC SP_DEFAULTDB N'" + _username + "', N'" + _dbSchema + "'";
          sqlCmd.ExecuteNonQuery();
          sqlCmd.CommandText = "EXEC SP_ADDROLEMEMBER N'db_owner', N'" + _username + "'";
          sqlCmd.ExecuteNonQuery();
        }
      }
      finally
      {
        sqlConn.Close();
      }
      return connection;
    }

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
      return MSSQLTransaction.BeginTransaction(this, _connectionString, level);
    }

    public ITransaction BeginTransaction()
    {
      return BeginTransaction(IsolationLevel.ReadCommitted);
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
