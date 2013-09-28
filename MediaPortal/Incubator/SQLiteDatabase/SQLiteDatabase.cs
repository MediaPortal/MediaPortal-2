#region Copyright (C) 2007-2013 Team MediaPortal

/*
    Copyright (C) 2007-2013 Team MediaPortal
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
using System.Data.SQLite;
using System.IO;
using MediaPortal.Backend.Database;
using MediaPortal.Backend.Services.Database;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Common.PathManager;

namespace MediaPortal.Database.SQLite
{
  /// <summary>
  /// Implementation of ISQLDatabase using the syste.data.sqlite wrapper for the SQLite database
  /// </summary>
  /// <remarks>
  /// The purpose of this database is to have an inbuilt database without the need to install an external
  /// database system (such as MySQL), which at the same time does not have relevant size limitations
  /// (such as MSSQLCE with a maximum database size of 2GB). The limitations of SQLite are much less
  /// restrictive (e.g. a maximum database size of about 140TB, for details see http://www.sqlite.org/limits.html)
  /// </remarks>
  public class SQLiteDatabase : ISQLDatabase
  {

    #region Constants

    private const string DatabaseTypeString = "SQLite"; // Name of this Database
    private const string DatabaseVersionString = "1.0.88.0"; // Version of the sytem.data.sqlite wrapper
    private const int LockTimeout = 30000; // Time in ms the database will wait for a lock
    private const string DefaultDatabaseFileName = "Datastore.s3db"; // Default name of the database file

    #endregion

    #region Variables

    private readonly string _connectionString;

    #endregion

    #region Constructors/Destructors

    public SQLiteDatabase()
    {
      try
      {
        var pathManager = ServiceRegistration.Get<IPathManager>();
        string dataDirectory = pathManager.GetPath("<DATABASE>");
        string databaseFile = Path.Combine(dataDirectory, DefaultDatabaseFileName);

        // We are using the ConnectionStringBuilder to generate the connection string
        // This ensures code compatibility in case of changes to the SQLite connection string parameters
        // More information on the parameters can be found here: http://www.sqlite.org/pragma.html
        var connBuilder = new SQLiteConnectionStringBuilder
        {
          // Name of the database file including path  
          DataSource = databaseFile,
        
          // Use SQLite database version 3.x  
          Version = 3,

          // Store GUIDs as binaries, not as string
          // Saves some space in the database and is said to make search queries on GUIDs faster  
          BinaryGUID = true,
        
          // If a lock cannot be obtained, the database enginge waits LockTimeout ms before it throws an exception  
          DefaultTimeout = LockTimeout,
        
          // Set page size to NTFS cluster size = 4096 bytes; supposed to give better performance
          // For BLOBs > 50kb a page size of 8192 may even give more performance (http://www.sqlite.org/intern-v-extern-blob.html)
          PageSize = 4096,
        
          // Size of the memory cache used by the database expressed in number of pages
          CacheSize = 10000,
        
          // Use the Write Ahead Log mode
          // In this journal mode write locks do not block reads
          // Needed to prevent sluggish behaviour of MP2 client when trying to read from the database (through MP2 server)
          // while MP2 server writes to the database (such as when importing shares)
          // More information can be found here: http://www.sqlite.org/wal.html
          JournalMode = SQLiteJournalModeEnum.Wal,
        
          // Use connection pooling
          // This way connections are not actually closed, but reused later which gives some performance advantages
          Pooling = true,
        
          // Sychronization Mode "Normal" enables parallel database access while at the same time preventing database
          // corruption and is therefore a good compromise between "Off" (more performance) and "On"
          // More information can be found here: http://www.sqlite.org/pragma.html#pragma_synchronous
          SyncMode = SynchronizationModes.Normal
        };

        _connectionString = connBuilder.ToString();

        if (!Directory.Exists(dataDirectory))
          Directory.CreateDirectory(dataDirectory);

        using (var conn = new SQLiteConnection(_connectionString))
        {
          conn.Open();
          conn.Close();
        }
      }
      catch (Exception e)
      {
        ServiceRegistration.Get<ILogger>().Critical("SQLiteDatabase: Error establishing database connection", e);
        throw;
      }
    }

    #endregion

    #region ISQLDatabase implementation

    public string DatabaseType
    {
      get { return DatabaseTypeString; }
    }

    public string DatabaseVersion
    {
      get { return DatabaseVersionString; }
    }

    public uint MaxObjectNameLength
    {
      get { return 30; }
    }

    public string GetSQLType(Type dotNetType)
    {     
      // SQLite only knows five storage classes:
      // TEXT, INTEGER, REAL, BLOB and NULL
      // More information can be found here: http://www.sqlite.org/datatype3.html

      if (dotNetType == typeof(DateTime))
        return "TEXT";
      if (dotNetType == typeof(Char))
        return "TEXT";
      if (dotNetType == typeof(Boolean))
        return "INTEGER";
      if (dotNetType == typeof(Single))
        return "REAL";
      if (dotNetType == typeof(Double))
        return "REAL";
      if (dotNetType == typeof(Byte) || dotNetType == typeof(SByte))
        return "INTEGER";
      if (dotNetType == typeof(UInt16) || dotNetType == typeof(Int16))
        return "INTEGER";
      if (dotNetType == typeof(UInt32) || dotNetType == typeof(Int32))
        return "INTEGER";
      if (dotNetType == typeof(UInt64) || dotNetType == typeof(Int64))
        return "INTEGER";
      if (dotNetType == typeof(Guid))
        return "TEXT";
      if (dotNetType == typeof(byte[]))
        return "BLOB";
      return null;
    }

    public string GetSQLVarLengthStringType(uint maxNumChars)
    {
      return "TEXT";
    }

    public string GetSQLFixedLengthStringType(uint maxNumChars)
    {
      return "TEXT";
    }

    public bool IsCLOB(uint maxNumChars)
    {
      return false;
    }

    public IDbDataParameter AddParameter(IDbCommand command, string name, object value, Type type)
    {
      
      if (type == typeof(byte[]))
      {
        var result = (SQLiteParameter) command.CreateParameter();
        result.ParameterName = name;
        result.Value = value ?? DBNull.Value;
        result.DbType = DbType.Binary;
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
        var result = (byte[])((SQLiteDataReader) reader).GetValue(colIndex);
        return result;
      }

      return DBUtils.ReadSimpleDBValue(type, reader, colIndex);
    }

    public ITransaction BeginTransaction(IsolationLevel level)
    {
      // SQLite only supports IsolationLevels Serializable and ReadCommitted, the standard being Serializable
      // MP2 mostly requests ReadCommitted IsolationLevel, however, this leads to unexplainable dead locks,
      // so we override any requested IsolationLevel other than Serializable
      // As per the code here: http://system.data.sqlite.org/index.html/vpatch?from=ed229ff2b0076a39&to=de60415f960244d7
      // IsolationLevel.Serializable is the same as the obsolete parameter DeferredLock=false
      return new SQLiteTransaction(this, _connectionString, IsolationLevel.Serializable);
    }

    public ITransaction BeginTransaction()
    {
      return new SQLiteTransaction(this, _connectionString, IsolationLevel.Serializable);
    }

    public bool TableExists(string tableName)
    {
      using (var conn = new SQLiteConnection(_connectionString))
      {
        conn.Open();
        using (IDbCommand cmd = conn.CreateCommand())
        {
          cmd.CommandText = @"SELECT count(*) FROM sqlite_master WHERE name='" + tableName + "' AND type='table'";
          var cnt = (long)cmd.ExecuteScalar();
          return (cnt == 1);
        }
      }
    }

    public string CreateStringConcatenationExpression(string str1, string str2)
    {
      return str1 + " || " + str2;
    }

    public string CreateSubstringExpression(string str1, string posExpr)
    {
      return "substr(" + str1 + "," + posExpr + ")";
    }

    public string CreateSubstringExpression(string str1, string posExpr, string lenExpr)
    {
      return "substr(" + str1 + "," + posExpr + "," + lenExpr + ")";
    }

    public string CreateDateToYearProjectionExpression(string selectExpression)
    {
        return "strftime('%Y', " + selectExpression + ")";
    }

    #endregion

  }
}
