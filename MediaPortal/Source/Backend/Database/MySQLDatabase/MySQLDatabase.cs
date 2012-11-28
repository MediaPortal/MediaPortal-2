#region Copyright (C) 2007-2012 Team MediaPortal

/*
    Copyright (C) 2007-2012 Team MediaPortal
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
using MySql.Data.MySqlClient;
using MediaPortal.Backend.Services.Database;

namespace MediaPortal.Database.MySQL
{
  public class MySQLDatabase : ISQLDatabase
  {
    #region Constants

    public const string MYSQL_DATABASE_TYPE = "MySQL";
    public const string DATABASE_VERSION = "5.x";
    public const int MAX_NUM_CHARS_CHAR_VARCHAR = 4000;

    #endregion

    #region Fields

    protected readonly string _connectionString;
    protected readonly string _dbSchema;
    protected readonly string _server;
    protected readonly string _username;
    protected readonly string _password;
    protected readonly int _maxPacketSize;

    #endregion

    #region Constructor

    public MySQLDatabase()
    {
      if (!MySQLSettings.Read(ref _server, ref _username, ref _password, ref _dbSchema, ref _maxPacketSize))
        throw new ApplicationException("Cannot read database connection settings from MySQLSettings.xml!");

      _connectionString = string.Format("server={0};User Id={1};password={2};", _server, _username, _password);
      
      // First connect without database parameter, so we can create the schema first.
      CheckOrCreateDatabase(_dbSchema);

      _connectionString += "database=" + _dbSchema;
    }

    #endregion

    #region ISQLDatabase implementation

    public string DatabaseType
    {
      get { return MYSQL_DATABASE_TYPE; }
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
        return "BIT";
      if (dotNetType == typeof(Single))
        return "FLOAT";
      if (dotNetType == typeof(Double))
        return "DOUBLE";
      if (dotNetType == typeof(Byte) || dotNetType == typeof(SByte))
        return "TINYINT";
      if (dotNetType == typeof(UInt16) || dotNetType == typeof(Int16))
        return "SMALLINT";
      if (dotNetType == typeof(UInt32) || dotNetType == typeof(Int32))
        return "INTEGER";
      if (dotNetType == typeof(UInt64) || dotNetType == typeof(Int64))
        return "BIGINT";
      if (dotNetType == typeof(Guid))
        return "BINARY(16)";
      if (dotNetType == typeof(byte[]))
        return "LONGBLOB";
      return null;
    }

    public string GetSQLVarLengthStringType(uint maxNumChars)
    {
      if (maxNumChars > MAX_NUM_CHARS_CHAR_VARCHAR)
        return "TEXT";
      return "VARCHAR(" + maxNumChars + ")";
    }

    public string GetSQLFixedLengthStringType(uint maxNumChars)
    {
      if (maxNumChars > MAX_NUM_CHARS_CHAR_VARCHAR)
        return "TEXT";
      return "CHAR(" + maxNumChars + ")";
    }

    public bool IsCLOB(uint maxNumChars)
    {
      return maxNumChars > MAX_NUM_CHARS_CHAR_VARCHAR;
    }

    public IDbDataParameter AddParameter(IDbCommand command, string name, object value, Type type)
    {
      if (type == typeof(byte[]))
      {
        MySqlParameter result = (MySqlParameter) command.CreateParameter();
        result.ParameterName = name;
        result.Value = value;
        result.MySqlDbType = MySqlDbType.Blob;
        command.Parameters.Add(result);
        return result;
      }
      if (type == typeof(Guid))
      {
        MySqlParameter result = (MySqlParameter) command.CreateParameter();
        result.ParameterName = name;
        result.Value = ((Guid) value).ToByteArray();
        result.MySqlDbType = MySqlDbType.VarBinary;
        command.Parameters.Add(result);
        return result;
      }
      return DBUtils.AddSimpleParameter(command, name, value, type);
    }

    public object ReadDBValue(Type type, IDataReader reader, int colIndex)
    {
      if (reader.IsDBNull(colIndex))
        return null;
      return DBUtils.ReadSimpleDBValue(type, reader, colIndex);
    }

    public ITransaction BeginTransaction(IsolationLevel level)
    {
      return MySQLTransaction.BeginTransaction(this, _connectionString, level);
    }

    public ITransaction BeginTransaction()
    {
      return BeginTransaction(IsolationLevel.ReadCommitted);
    }

    public bool TableExists(string tableName)
    {
      long cnt = (long) ExecuteScalar(@"SELECT COUNT(TABLE_NAME) FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA='" + _dbSchema + "' AND TABLE_NAME='" + tableName + "'");
      return (cnt == 1);
    }

    public void CheckOrCreateDatabase(string databaseName)
    {
      ExecuteScalar("CREATE DATABASE IF NOT EXISTS " + databaseName);
      ExecuteScalar("set global max_allowed_packet=" + _maxPacketSize);
    }

    private object ExecuteScalar(string command)
    {
      using (MySqlConnection connection = new MySqlConnection(_connectionString))
      {
        connection.Open();
        IDbCommand cmd = connection.CreateCommand();
#if DEBUG
        // Return a LoggingDbCommandWrapper to log all CommandText to logfile in DEBUG mode.
        cmd = new LoggingDbCommandWrapper(cmd);
#endif

        using (cmd)
        {
          cmd.CommandText = command;
          var result = cmd.ExecuteScalar();
          return result;
        }
      }
    }

    public string CreateStringConcatenationExpression(string str1, string str2)
    {
      return str1 + "+" + str2;
    }

    public string CreateSubstringExpression(string str1, string posExpr)
    {
      return "SUBSTR(" + str1 + "," + posExpr + "," + Int32.MaxValue + ")"; // Int32.MaxValue seems to be the biggest supported value
    }

    public string CreateSubstringExpression(string str1, string posExpr, string lenExpr)
    {
      return "SUBSTR(" + str1 + "," + posExpr + "," + lenExpr + ")";
    }

    public string CreateDateToYearProjectionExpression(string selectExpression)
    {
      return "EXTRACT(YEAR FROM " + selectExpression + ")";
    }

    #endregion
  }
}
