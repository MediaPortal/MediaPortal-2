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
using System.IO;
using System.Reflection;
using FirebirdSql.Data.FirebirdClient;
using MediaPortal.Backend.Database;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Common.Settings;
using MediaPortal.Database.Firebird.Settings;

namespace MediaPortal.Database.Firebird
{
  /// <summary>
  /// Database implementation for the FirebirdDotNet implementation.
  /// </summary>
  /// <remarks>
  /// Per connection, only one parallel transaction is supported.
  /// </remarks>
  public class FirebirdSQLDatabase : ISQLDatabase, IDisposable
  {
    public const string FIREBIRD_DATABASE_TYPE = "Firebird";
    public const string DATABASE_VERSION = "2.1.2";

    public const int PAGE_SIZE = 16384;

    public const int MAX_NUM_CHARS_CHAR_VARCHAR = 8191;

    protected string _connectionString;

    #region Ctor & dtor

    public FirebirdSQLDatabase()
    {
      string dllDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
      // Register DLL and resource file directories - necessary for the driver, else it would try to
      // load resources from the application's directory
      Environment.SetEnvironmentVariable("FIREBIRD", dllDirectory);
      Environment.SetEnvironmentVariable("FIREBIRD_MSG", dllDirectory);
      FirebirdSettings settings = ServiceRegistration.Get<ISettingsManager>().Load<FirebirdSettings>();
      FbConnectionStringBuilder sb = new FbConnectionStringBuilder
        {
            ServerType = settings.ServerType,
            UserID = settings.UserID,
            Password = settings.Password,
            Database = settings.DatabaseFile,
            Dialect = 3,
            Charset = "UTF8",
            Pooling = false, // We use our own pooling mechanism
            ReturnRecordsAffected = true
        };
      _connectionString = sb.ConnectionString;
      try
      {
        string dir = Path.GetDirectoryName(settings.DatabaseFile);
        if (!Directory.Exists(dir))
          Directory.CreateDirectory(dir);
        if (!File.Exists(settings.DatabaseFile))
          CreateDatabase(_connectionString);
      }
      catch (Exception e)
      {
        ServiceRegistration.Get<ILogger>().Critical("Error establishing database connection", e);
        throw;
      }
    }

    ~FirebirdSQLDatabase()
    {
      Dispose();
    }

    public void Dispose()
    {
      FbConnection.ClearAllPools();
    }

    #endregion

    #region Protected methods

    /// <summary>
    /// Creates a new database with the given <paramref name="connectionString"/>.
    /// </summary>
    protected void CreateDatabase(string connectionString)
    {
      FbConnection.CreateDatabase(connectionString, PAGE_SIZE, true, true);
      // Create BOOLEAN domain
      ITransaction transaction = BeginTransaction();
      try
      {
        using (IDbCommand command = transaction.CreateCommand())
        {
          command.CommandText = "CREATE DOMAIN BOOLEAN AS SMALLINT DEFAULT '0' NOT NULL CHECK (value in (0,1))";
          command.ExecuteNonQuery();
        }
        transaction.Commit(); // Seems as if the driver doesn't execute the CREATE DOMAIN statement if we don't commit...
      }
      catch (Exception e)
      {
        ServiceRegistration.Get<ILogger>().Error("FirebirdSQLDatabase: Error creating database (connection string is '{0}')", e, connectionString);
        transaction.Rollback();
      }
    }

    /// <summary>
    /// Builds a restrictions string array to be used with method <see cref="FbConnection.GetSchema(string,string[])"/>.
    /// </summary>
    /// <remarks>
    /// All parameters are case sensitive. If a table wasn't explicitly created case-sensitive, its name must be given in
    /// upper-case.
    /// </remarks>
    /// <param name="table_catalog">Restricts by catalog.</param>
    /// <param name="table_schema">Restricts by table schema.</param>
    /// <param name="table_name">Restricts by table name.</param>
    /// <param name="table_type">Restricts by table type. Supported types: "VIEW", "SYSTEM TABLE", "TABLE".</param>
    /// <returns></returns>
    protected string[] BuildSchemaQueryRestrictions(string table_catalog, string table_schema, string table_name, string table_type)
    {
      return new string[] { table_catalog, table_schema, table_name, table_type };
    }

    protected FbConnection CreateConnection()
    {
      FbConnection result = new FbConnection(_connectionString);
      result.Open();
      return result;
    }

    #endregion

    #region ISQLDatabase implementation

    public string DatabaseType
    {
      get { return FIREBIRD_DATABASE_TYPE; }
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
        return "TIMESTAMP";
      if (dotNetType == typeof(Char))
        return "CHAR(1)";
      if (dotNetType == typeof(Boolean))
        return "BOOLEAN"; // BOOLEAN is not a datatype - it is a domain which was created in method CreateDatabase()
      if (dotNetType == typeof(Single))
        return "FLOAT";
      if (dotNetType == typeof(Double))
        return "DOUBLE PRECISION";
      if (dotNetType == typeof(SByte) || dotNetType == typeof(Byte) || dotNetType == typeof(Int16))
        return "SMALLINT";
      if (dotNetType == typeof(UInt16) || dotNetType == typeof(Int32))
        return "INTEGER";
      if (dotNetType == typeof(UInt32) || dotNetType == typeof(Int64))
        return "BIGINT";
      if (dotNetType == typeof(Guid))
        return "CHAR(38)";
      return null;
    }

    public string GetSQLVarLengthStringType(uint maxNumChars)
    {
      if (maxNumChars > MAX_NUM_CHARS_CHAR_VARCHAR)
        return "BLOB SUB_TYPE 1"; // SUB_TYPE 1 = text
      return "VARCHAR(" + maxNumChars + ")"; // Defaults to the default character set of our DB, see the creation of the DB file
    }

    public bool IsCLOB(uint maxNumChars)
    {
      return maxNumChars > MAX_NUM_CHARS_CHAR_VARCHAR;
    }

    public string GetSQLFixedLengthStringType(uint maxNumChars)
    {
      if (maxNumChars > MAX_NUM_CHARS_CHAR_VARCHAR)
        return "BLOB SUB_TYPE 1"; // SUB_TYPE 1 = text
      return "CHAR(" + maxNumChars + ")"; // Defaults to the default character set of our DB, see the creation of the DB file
    }

    public ITransaction BeginTransaction(IsolationLevel level)
    {
      return FirebirdTransaction.BeginTransaction(this, _connectionString, level);
    }

    public ITransaction BeginTransaction()
    {
      return BeginTransaction(IsolationLevel.ReadCommitted);
    }

    public bool TableExists(string tableName)
    {
      using (FbConnection conn = CreateConnection())
      {
        try
        {
          DataTable dt = conn.GetSchema("TABLES", BuildSchemaQueryRestrictions(null, null, tableName, null));
          using (DataTableReader dtr = dt.CreateDataReader())
            return dtr.Read();
        }
        finally
        {
          conn.Close();
        }
      }
    }

    #endregion
  }
}
