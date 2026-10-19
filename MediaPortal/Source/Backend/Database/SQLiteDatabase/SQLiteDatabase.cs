#region Copyright (C) 2007-2020 Team MediaPortal

/*
    Copyright (C) 2007-2020 Team MediaPortal
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
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using Microsoft.Data.Sqlite;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using MediaPortal.Backend.Database;
using MediaPortal.Backend.Services.Database;
using MediaPortal.Backend.Services.MediaLibrary;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.Messaging;
using MediaPortal.Common.PathManager;
using MediaPortal.Common.Services.Logging;
using MediaPortal.Common.Settings;

namespace MediaPortal.Database.SQLite
{
  /// <summary>
  /// Implementation of ISQLDatabase using the Microsoft.Data.Sqlite driver for the SQLite database
  /// </summary>
  /// <remarks>
  /// The purpose of this database is to have an inbuilt database without the need to install an external
  /// database system (such as MySQL), which at the same time does not have relevant size limitations
  /// (such as MSSQLCE with a maximum database size of 2GB). The limitations of SQLite are much less
  /// restrictive (e.g. a maximum database size of about 140TB, for details see http://www.sqlite.org/limits.html)
  /// </remarks>
  public class SQLiteDatabase : ISQLDatabasePaging, ISQLDatabaseStorage, ISQLTemporaryTable, IDisposable
  {
    #region Constants

    private const string DATABASE_TYPE_STRING = "SQLite"; // Name of this Database

    #endregion

    #region Variables

    private readonly string _connectionString;

    /// <summary>
    /// The PRAGMA statements every connection runs right after opening. The old driver took journal
    /// mode, synchronous mode and cache size as connection-string keywords; Microsoft.Data.Sqlite
    /// rejects them there (measured: "Connection string keyword 'journal_mode' is not supported"), so
    /// they are statements now, executed at one place together with the user-configurable
    /// InitializationCommand.
    /// </summary>
    private readonly string _connectionPragmas;

    /// <summary>
    /// SQLite engine version, read from the first connection that opens. The old driver had a static
    /// version property; Microsoft.Data.Sqlite only reports it on an open connection.
    /// </summary>
    private string _sqliteVersion;

#if !NO_POOL
    private ConnectionPool<SqliteConnection> _connectionPool;
#endif
    private readonly SQLiteSettings _settings;
    private readonly AsynchronousMessageQueue _messageQueue;
    private readonly ActionBlock<bool> _maintenanceScheduler;
    private readonly ICollection<Guid> _importingShareIds;
    private long _tempTableCount = 0;

    #endregion

    #region Constructors/Destructors

    /// <summary>
    /// Creates a new SQLiteDatabase instance.
    /// </summary>
    public SQLiteDatabase()
    {
      try
      {
        _importingShareIds = new HashSet<Guid>();
        _maintenanceScheduler = new ActionBlock<bool>(async _ => await PerformDatabaseMaintenanceAsync(), new ExecutionDataflowBlockOptions { BoundedCapacity = 2 });
        _messageQueue = new AsynchronousMessageQueue(this, new[] { ContentDirectoryMessaging.CHANNEL });
        _messageQueue.MessageReceived += OnMessageReceived;
        _messageQueue.Start();

        _settings = ServiceRegistration.Get<ISettingsManager>().Load<SQLiteSettings>();
        _settings.LogSettings();

        // LogVersionInformation() is called further down, once a connection exists: the driver only
        // reports the SQLite version on an open connection, and loading the native library is itself
        // the first thing that can fail on a new platform - worth having in the log with a name.
        if (_settings.EnableTraceLogging)
        {
          // System.Data.SQLite surfaced SQLite's "log" hook as a static event, which is how
          // SQLiteDebug.log was written. Microsoft.Data.Sqlite does not expose it, so the setting has
          // nothing to attach to any more. Saying so in the log beats a silently missing file.
          ServiceRegistration.Get<ILogger>().Info("SQLiteDatabase: SQLite trace logging is not available with the Microsoft.Data.Sqlite driver; the EnableTraceLogging setting has no effect.");
        }

        var pathManager = ServiceRegistration.Get<IPathManager>();
        string dataDirectory = pathManager.GetPath("<DATABASE>");
        string databaseFile = Path.Combine(dataDirectory, _settings.DatabaseFileName);

        // The old provider needed a SQLite *URI* ("file:/path?cache=shared") to switch on shared
        // cache mode, and SQLite ignores the page size of a connection string when a URI is used -
        // which is why the original code had a workaround to create the file with the right page
        // size. Microsoft.Data.Sqlite has a Cache keyword for shared cache, so the URI, the
        // HttpUtility encoding of it and that workaround all go away. (The page size still has to be
        // set before the file is written, because SQLite stores it permanently in the header.)
#if !NO_POOL
        _connectionPool = new ConnectionPool<SqliteConnection>(CreateOpenAndInitializeConnection);
#endif

        var connBuilder = new SqliteConnectionStringBuilder
        {
          DataSource = databaseFile,

          // Shared cache mode, the equivalent of the old "?cache=shared" URI parameter: several
          // connections to the same database inside this process share one page cache.
          Cache = SqliteCacheMode.Shared,

          DefaultTimeout = _settings.LockTimeout,

          // The driver's own pool is never used, in every build configuration. This is not a
          // performance preference but a correctness one, measured: pooled connections are reset when
          // returned, and that reset removes registered collations - after overriding the built-in
          // NOCASE once, the *next* consumer of a pooled handle got "no such collation sequence:
          // NOCASE", i.e. not even SQLite's own. MP2's ConnectionPool hands out the connections this
          // class created and initialized, so the collation and PRAGMAs stay attached to them.
          Pooling = false,

          // MP2's database backend uses foreign key constraints to ensure referential integrity.
          // SQLite supports this, but it has to be enabled for each database connection by a PRAGMA command
          // For details see http://www.sqlite.org/foreignkeys.html
          ForeignKeys = true
        };
        _connectionString = connBuilder.ToString();
        ServiceRegistration.Get<ILogger>().Info("SQLiteDatabase: Connection String used: '{0}'", _connectionString);

        // Statements from the first comment: journal mode, synchronous mode and cache size were
        // connection-string keywords in the old driver and are PRAGMAs in this one. The values and
        // their reasoning are unchanged:
        //   - WAL: write locks do not block reads, which is what keeps the client responsive while the
        //     server imports shares (http://www.sqlite.org/wal.html). Persistent per database file.
        //   - synchronous=NORMAL: parallel access with durability against crashes, the compromise
        //     between OFF (faster) and FULL (safest) (http://sqlite.org/pragma.html#pragma_synchronous).
        //   - cache_size: a positive value is pages, which is what SQLiteSettings reports.
        // The user-configurable InitializationCommand goes last so that it can override any of these.
        _connectionPragmas =
          "PRAGMA journal_mode=WAL;" +
          "PRAGMA synchronous=NORMAL;" +
          string.Format(CultureInfo.InvariantCulture, "PRAGMA cache_size={0};", _settings.CacheSizeInPages) +
          _settings.InitializationCommand;


        if (!File.Exists(databaseFile))
        {
          ServiceRegistration.Get<ILogger>().Info("SQLiteDatabase: Database file does not exists. Creating database file");

          if (!Directory.Exists(dataDirectory))
            Directory.CreateDirectory(dataDirectory);

          // The page size is stored permanently in the database header, so it has to be set before the
          // file is written - the old code needed a second connection string for this because the URI
          // form made SQLite ignore the keyword. There is no keyword to set here either, so the pragma
          // is executed and VACUUM forces the first write. On a database that does not exist yet this
          // leaves a valid empty file with the configured page size; measured, the value survives a
          // reopen.
          using (var connection = new SqliteConnection(_connectionString))
          {
            connection.Open();
            using (var command = connection.CreateCommand())
            {
              command.CommandText = string.Format(CultureInfo.InvariantCulture,
                "PRAGMA page_size={0};VACUUM;", _settings.PageSize);
              command.ExecuteNonQuery();
            }
            connection.Close();
          }
        }

        // The following is necessary to avoid the creation of a shared memory index file
        // ("-shm"-file) when using exclusive locking mode. When WAL-mode is used, it is possible
        // to switch between normal and exclusive locking mode at any time. However, the creation
        // of a "-shm"-file can only be avoided, when exclusive locking mode is set BEFORE entering
        // WAL-mode. If this is the case, it is not possible to leave exclusive locking mode
        // without leaving WAL-mode before, because the "-shm"-file was not created.
        // The regular connections in our connection pool use WAL-mode. Therefore we have
        // to open one connection without WAL-Mode (here with JournalMode=OFF) and set locking_mode=
        // EXCLUSIVE before we create the first regular connection that goes into the pool.
        // To use exclusive locking mode, it is additionally necessary to set locking_mode=EXCLUSIVE
        // for every connection in the pool via the InitializationCommand. If "PRAGMA locking_mode=
        // EXCLUSIVE" is not in the InitializationCommand, normal locking mode is used
        // although we issue "PRAGMA locking_mode=EXCLUSIVE" at this point.
        // For details see here: http://sqlite.org/wal.html#noshm
        // Avoiding the creation of an "-shm"-file materially improves the database performance.
        if (_settings.UseExclusiveMode)
        {
          // "JournalMode=Off" was a connection-string keyword in the old driver; as a statement it
          // means the same thing for this one connection, and it still has to run before entering
          // WAL mode, which is what the paragraph above is about.
          using (var connection = new SqliteConnection(_connectionString))
          {
            connection.Open();
            using (var command = connection.CreateCommand())
            {
              command.CommandText = "PRAGMA journal_mode=OFF;" + SQLiteSettings.EXCLUSIVE_MODE_COMMAND;
              command.ExecuteNonQuery();
            }
            connection.Close();
          }
        }

        LogVersionInformation();

        // Just test one "regular" connection, which is the first connection in the pool
        using (var transaction = BeginTransaction())
          transaction.Rollback();
      }
      catch (Exception e)
      {
        ServiceRegistration.Get<ILogger>().Critical("SQLiteDatabase: Error establishing database connection", e);
        throw;
      }
    }

    #endregion

#if !NO_POOL
    #region Public properties

    public ConnectionPool<SqliteConnection> ConnectionPool
    {
      get
      {
        return _connectionPool;
      }
    }

    #endregion
#endif

    #region Private methods

    /// <summary>
    /// Creates a new <see cref="SqliteConnection"/> object, opens it, registers the culture-aware
    /// NOCASE collation and runs <see cref="_connectionPragmas"/> via ExecuteNonQuery()
    /// </summary>
    /// <returns>Newly created and initialized <see cref="SqliteConnection"/></returns>
    public SqliteConnection CreateOpenAndInitializeConnection()
    {
      var connection = new SqliteConnection(_connectionString);
      connection.Open();

      // Two per-connection things the old driver did differently:
      //   - Collations. SQLiteCultureSensitiveCollation replaced SQLite's built-in NOCASE through a
      //     [SQLiteFunction] attribute that System.Data.SQLite registered on every connection; this
      //     driver has an explicit call, and it is per connection - measured, a second connection to
      //     the same database does not see it. MP2's schema declares columns "COLLATE NOCASE" in
      //     almost every table, so registering here is what keeps media library ordering the same.
      //   - SetChunkSize(). It made System.Data.SQLite read large values in chunks; this driver has no
      //     equivalent, so the ChunkSize setting is inert. It stays in the settings class because
      //     existing settings files carry it.
      connection.CreateCollation(SQLiteCultureSensitiveCollation.CollationName, SQLiteCultureSensitiveCollation.Compare);
      using (var command = connection.CreateCommand())
      {
        command.CommandText = _connectionPragmas;
        command.ExecuteNonQuery();
      }

      if (_sqliteVersion == null)
        _sqliteVersion = connection.ServerVersion;
      return connection;
    }

    /// <summary>
    /// Processes system messages
    /// </summary>
    /// <param name="queue">MessagQueue that has received the message</param>
    /// <param name="message">Message received</param>
    /// <remarks>
    /// We only listen to the <see cref="ContentDirectoryMessaging.CHANNEL"/> and react on <see cref="ContentDirectoryMessaging.MessageType.ShareImportCompleted"/> messages.
    /// They are sent whenever a server or client share import is finished (<see cref="ImporterWorkerMessaging.MessageType.ImportCompleted"/> messages
    /// are only sent for server shares).
    /// When an import is completed, we schedule a <see cref="PerformDatabaseMaintenanceAsync"/> call. The execution of these calls
    /// is serialized by the <see cref="_maintenanceScheduler"/>. It has a BoundedCapacity of 2, i.e. one run that is currently
    /// executed and one run that is scheduled. In case that another run is scheduled while one is processed and another one is
    /// waiting to be executed, we do not schedule a third run as the maintenance that is waiting to be executed will perform
    /// everthing we need; we only log that this was the case.
    /// Currently, we do not react on the deletion of shares, which would also be a good time to schedule a maintenance. However,
    /// the <see cref="ContentDirectoryMessaging.MessageType.RegisteredSharesChanged"/> message is not only sent when a share is
    /// deleted, but also when it is added. In the latter case we would schedule a maintenance twice, the first time (too early)
    /// when the share was added and the second time when the share's import finished.
    /// ToDo: Maybe change the RegisteredSharesChanged message to carry an argument signaling what exactly changed
    /// </remarks>
    private void OnMessageReceived(AsynchronousMessageQueue queue, SystemMessage message)
    {
      var messageType = (ContentDirectoryMessaging.MessageType)message.MessageType;
      switch (messageType)
      {
        case ContentDirectoryMessaging.MessageType.ShareImportStarted:
          // Count all importing shares to know if all are finished before we run the maintenance task
          _importingShareIds.Add((Guid)message.MessageData[ContentDirectoryMessaging.SHARE_ID]);
          break;
        case ContentDirectoryMessaging.MessageType.ShareImportCompleted:
          _importingShareIds.Remove((Guid)message.MessageData[ContentDirectoryMessaging.SHARE_ID]);
          if (_importingShareIds.Count == 0)
            if (!_maintenanceScheduler.Post(true))
              ServiceRegistration.Get<ILogger>().Info("SQLiteDatabase: Skipping additional database maintenance. There is already a maintenance run in the works and another one scheduled.");
          break;
      }
    }

    /// <summary>
    /// Runs the "ANALYZE;" SQL command on the database to update the statistics tables for better query performance
    /// </summary>
    /// <returns>Task that completes when the "ANALYZE;" command has finished</returns>
    private async Task PerformDatabaseMaintenanceAsync()
    {
      ServiceRegistration.Get<ILogger>().Info("SQLiteDatabase: Performing database maintenance...");
      try
      {
        using (var connection = new SqliteConnection(_connectionString))
        {
          await connection.OpenAsync();
          using (var command = connection.CreateCommand())
          {
            command.CommandText = "ANALYZE;";
            await command.ExecuteNonQueryAsync();
          }
          connection.Close();
        }
      }
      catch (Exception e)
      {
        ServiceRegistration.Get<ILogger>().Info("SQLiteDatabase: Error while performing database maintenance:", e);
      }
      ServiceRegistration.Get<ILogger>().Info("SQLiteDatabase: Database maintenance finished");
    }

    /// <summary>
    /// Logs version information about the SQLite engine and the driver in front of it.
    /// </summary>
    /// <remarks>
    /// The old provider had static properties for the wrapper version, the interop version, the SQLite
    /// version and each of their source ids; this driver reports only the SQLite engine version, and
    /// only on an open connection - which is why this runs after the database file has been created
    /// and why it opens a connection of its own. What matters on a new platform is that this line
    /// appears at all: getting it here means the native library loaded, and if it does not load, the
    /// exception says so at a known point instead of during somebody's first query.
    /// </remarks>
    private void LogVersionInformation()
    {
      using (var connection = new SqliteConnection(_connectionString))
      {
        connection.Open();
        ServiceRegistration.Get<ILogger>().Info("SQLiteDatabase: SQLiteVersion: {0}", connection.ServerVersion);
        ServiceRegistration.Get<ILogger>().Debug("SQLiteDatabase: Driver: {0}", typeof(SqliteConnection).Assembly.GetName().Version);
        connection.Close();
      }
    }

    #endregion

    #region ISQLDatabase implementation

    public string DatabaseType
    {
      get { return DATABASE_TYPE_STRING; }
    }

    public string DatabaseVersion
    {
      // The SQLite engine version, from the first connection that opened (see _sqliteVersion). Only
      // used in a diagnostic message by SqlScriptPreprocessor.
      get { return _sqliteVersion ?? "unknown"; }
    }

    public uint MaxObjectNameLength
    {
      get { return 30; }
    }

    public uint MaxNumberOfParameters
    {
      //The maximum number of a host parameter is by default 999. We set it a bit lower to have a little headroom.
      get { return 980; }
    }

    public string ConcatOperator
    {
      get { return "||"; }
    }

    public string LengthFunction
    {
      get { return "LENGTH"; }
    }

    public string GetSQLType(Type dotNetType)
    {
      // SQLite only knows five storage classes:
      // TEXT, INTEGER, REAL, BLOB and NULL
      // More information can be found here: http://www.sqlite.org/datatype3.html

      if (dotNetType == typeof(DateTime))
        return "TEXT";
      if (dotNetType == typeof(Char))
        return "TEXT COLLATE NOCASE";
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
      return "TEXT COLLATE NOCASE";
    }

    public string GetSQLFixedLengthStringType(uint maxNumChars)
    {
      return "TEXT COLLATE NOCASE";
    }

    public bool IsCLOB(uint maxNumChars)
    {
      return false;
    }

    public IDbDataParameter AddParameter(IDbCommand command, string name, object value, Type type)
    {
      // The BLOB cases (byte[] and Guid) live in SQLiteValues where the storage decision can be
      // tested against a real database; everything else is the shared binder.
      var binary = SQLiteValues.TryAddBinaryParameter(command, name, value, type);
      if (binary != null)
        return binary;

      return DBUtils.AddSimpleParameter(command, name, value, type);
    }

    public object ReadDBValue(Type type, IDataReader reader, int colIndex)
    {
      if (reader.IsDBNull(colIndex))
        return null;

      if (type == typeof(byte[]))
      {
        var result = (byte[])((SqliteDataReader)reader).GetValue(colIndex);
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
      return new SQLiteTransaction(this, IsolationLevel.Serializable, _settings);
    }

    public ITransaction BeginTransaction()
    {
      return new SQLiteTransaction(this, IsolationLevel.Serializable, _settings);
    }

    public bool TableExists(string tableName)
    {
      using (var transaction = BeginTransaction())
      {
        using (var cmd = transaction.CreateCommand())
        {
          cmd.CommandText = @"SELECT count(*) FROM sqlite_master WHERE name='" + tableName + "' AND type='table'";
          var cnt = (long)cmd.ExecuteScalar();
          return (cnt == 1);
        }
      }
    }

    public bool BackupDatabase(string backupVersion)
    {
      var pathManager = ServiceRegistration.Get<IPathManager>();
      string dataDirectory = pathManager.GetPath("<DATABASE>");
      string databaseFile = Path.Combine(dataDirectory, _settings.DatabaseFileName);
      if (File.Exists(databaseFile))
      {
        string backupFile = Path.ChangeExtension(databaseFile, $".{backupVersion}.bak");
        if (File.Exists(backupFile))
        {
          for (int i = 1; i <= 10; i++)
          {
            backupFile = Path.ChangeExtension(databaseFile, $".{backupVersion} {i}.bak");
            if (!File.Exists(backupFile))
              break;
          }
        }
        File.Copy(databaseFile, backupFile, true);
        return true;
      }
      return false;
    }

    public bool BackupTables(string tableSuffix)
    {
      bool result = false;
      List<string> tables = new List<string>();
      List<string> indexes = new List<string>();
      List<string> constrainedTables = new List<string>();
      Dictionary<string, string> tableSqls = new Dictionary<string, string> ();
      using (var transaction = BeginTransaction())
      {
        using (var cmd = transaction.CreateCommand())
        {
          //Find all tables
          cmd.CommandText = @"SELECT name, sql FROM sqlite_master WHERE type='table'";
          using (IDataReader reader = cmd.ExecuteReader())
          {
            while (reader.Read())
            {
              string table = reader.GetString(0);
              if (!table.StartsWith("sqlite", StringComparison.InvariantCultureIgnoreCase) && !table.EndsWith(tableSuffix, StringComparison.InvariantCultureIgnoreCase))
              {
                tables.Add(table);

                //Get create sql
                tableSqls[table] = reader.GetString(1);
                if (tableSqls[table].IndexOf("REFERENCES", StringComparison.InvariantCultureIgnoreCase) >= 0)
                  constrainedTables.Add(table);
                //Change table name
                tableSqls[table] = tableSqls[table].Replace(" " + table + " ", " " + table + tableSuffix + " ");
                int idx = tableSqls[table].IndexOf("CONSTRAINT", StringComparison.InvariantCultureIgnoreCase);
                if (idx >= 0)
                {
                  //Remove constraint
                  tableSqls[table] = tableSqls[table].Substring(0, idx).Trim();
                  //Remove comma and add parenthesis
                  tableSqls[table] = tableSqls[table].Substring(0, tableSqls[table].Length - 1) + ")";
                }
              }
            }
          }

          //Find all indexes
          cmd.CommandText = @"SELECT name FROM sqlite_master WHERE type='index'";
          using (IDataReader reader = cmd.ExecuteReader())
          {
            while (reader.Read())
            {
              string index = reader.GetString(0);
              indexes.Add(index);
            }
          }

          //Rename all tables and remove constraints
          foreach (var table in tables)
          {
            //Check if backup table exists
            cmd.CommandText = $"SELECT count(*) FROM sqlite_master WHERE name='{table + tableSuffix}' AND type='table'";
            var cnt = (long)cmd.ExecuteScalar();
            if (cnt == 0)
            {
              //Sqlite cannot remove constraints so we need to recreate tables and copy data

              //Create old table without constraints
              cmd.CommandText = tableSqls[table];
              cmd.ExecuteNonQuery();

              //Copy data
              cmd.CommandText = $"INSERT INTO {table + tableSuffix} SELECT * FROM {table}";
              cmd.ExecuteNonQuery();

              result = true;
            }
            else
            {
              result = true;
            }
          }

          //Disable foreign keys to avoid errors
          cmd.CommandText = @"PRAGMA foreign_keys=OFF";
          cmd.ExecuteNonQuery();
          try
          {
            //Drop tables with references first to avoid errors
            foreach (var table in constrainedTables)
            {
              //Drop table
              cmd.CommandText = $"DROP TABLE IF EXISTS {table}";
              cmd.ExecuteNonQuery();
            }

            //Drop all tables so they can be recreated
            foreach (var table in tables)
            {
              //Drop table
              cmd.CommandText = $"DROP TABLE IF EXISTS {table}";
              cmd.ExecuteNonQuery();
            }

            //Drop all indexes so they can be recreated
            foreach (var index in indexes)
            {
              //Drop index
              cmd.CommandText = $"DROP INDEX IF EXISTS {index}";
              cmd.ExecuteNonQuery();
            }
          }
          finally
          {
            //Restore foreign keys
            cmd.CommandText = @"PRAGMA foreign_keys=ON";
            cmd.ExecuteNonQuery();
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
          cmd.CommandText = @"SELECT name FROM sqlite_master WHERE type='table'";
          using (IDataReader reader = cmd.ExecuteReader())
          {
            while (reader.Read())
            {
              string table = reader.GetString(0);
              if (table.EndsWith(tableSuffix, StringComparison.InvariantCultureIgnoreCase))
                tables.Add(table);
            }
          }

          //Disable foreign keys to avoid errors
          cmd.CommandText = @"PRAGMA foreign_keys=OFF";
          cmd.ExecuteNonQuery();
          try
          {
            //Drop all backup tables as they are no longer needed
            foreach (var table in tables)
            {
              //Drop table
              cmd.CommandText = $"DROP TABLE IF EXISTS {table}";
              cmd.ExecuteNonQuery();
            }
          }
          finally
          {
            //Restore foreign keys
            cmd.CommandText = @"PRAGMA foreign_keys=ON";
            cmd.ExecuteNonQuery();
          }

          transaction.Commit();
        }
      }

      using (var connection = CreateOpenAndInitializeConnection())
      {
        using (var cmd = connection.CreateCommand())
        {
          //File based temp storage to avoid OOM errors
          cmd.CommandText = @"PRAGMA temp_store=FILE";
          cmd.ExecuteNonQuery();
          try
          {
            //Shrink and optimize database
            cmd.CommandText = "VACUUM";
            cmd.ExecuteNonQuery();
          }
          catch (Exception e)
          {
            ServiceRegistration.Get<ILogger>().Error("SQLiteDatabase: Error shrinking database", e);
          }
          finally
          {
            //Restore default temp storage
            if (_settings.UseTempStoreMemory)
              cmd.CommandText = @"PRAGMA temp_store=MEMORY";
            else
              cmd.CommandText = @"PRAGMA temp_store=DEFAULT";
            cmd.ExecuteNonQuery();
          }
        }
      }
      //Wait for garbage collection of temporary files
      GC.Collect();
      GC.WaitForPendingFinalizers();
      return true;
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
      // CAST is necessary because we treat the result as int, strftime returns
      // a TEXT value and SQLite refuses to convert TEXT to INT without an
      // explicit CAST.
      return "CAST(strftime('%Y', " + selectExpression + ") AS INTEGER)";
    }

    public string GetStorageClause(string statementStr)
    {
      // We can only append the "WITHOUT ROWID" to tables that are defining a primary key inside the "CREATE TABLE" statement.
      if (string.IsNullOrEmpty(statementStr)
        || statementStr.IndexOf("create table", StringComparison.InvariantCultureIgnoreCase) == -1
        || statementStr.IndexOf("primary key", StringComparison.InvariantCultureIgnoreCase) == -1)
        return null;

      return " WITHOUT ROWID";
    }

    public bool Process(ref string statementStr, ref IList<BindVar> bindVars, ref uint? offset, ref uint? limit)
    {
      if (!offset.HasValue && !limit.HasValue)
        return false;

      string limitClause = string.Format(" LIMIT {0}, {1}", offset ?? 0, limit);
      statementStr += limitClause;
      offset = null; // To avoid manual processing by caller
      limit = null; // To avoid manual processing by caller
      return true;
    }

    #endregion

    #region IDisposable implementation

    public void Dispose()
    {
      _messageQueue.Shutdown();
      _maintenanceScheduler.Complete();
      _maintenanceScheduler.Completion.Wait();

#if !NO_POOL
      if (_connectionPool != null)
      {
        _connectionPool.Dispose();
        _connectionPool = null;
      }
#endif
    }

    public string IdColumn
    {
      get { return "ID"; }
    }

    public string GetTemporaryIdTable(ITransaction transaction)
    {
      var tableNo = Interlocked.Increment(ref _tempTableCount);
      var type = GetSQLType(typeof(Guid));
      var tableName = $"TTABLE{tableNo}";
      var sql = $"CREATE TEMP TABLE {tableName}(ID {type} PRIMARY KEY) WITHOUT ROWID";
      using (var cmd = transaction.CreateCommand())
      {
        cmd.CommandText = sql;
        cmd.ExecuteNonQuery();
      }

      return $"temp.{tableName}";
    }

    public bool SaveTemporaryIds(ITransaction transaction, string temporaryTable, IList<Guid> mediaItemIds)
    {
      using (var cmd = transaction.CreateCommand())
      {
        cmd.CommandText = $"INSERT INTO {temporaryTable}(ID) VALUES(@MI_ID)";
        var parameter = AddParameter(cmd, "MI_ID", null, typeof(Guid));
        cmd.Prepare();

        foreach (var id in mediaItemIds)
        {
          // Not "parameter.Value = id": this parameter was created as DbType.Binary because this provider
          // stores a Guid as its 16 bytes, and assigning the Guid here would let the database driver pick a
          // representation of its own - text - which no id elsewhere in the database is. The ids of a
          // query result would then never compare equal to the ids of the rows they belong to, and every
          // part of a query that reads through this table (all multiple aspects like the provider resource
          // that carries a title's path, and the long texts such as a plot) would come back empty.
          parameter.Value = SQLiteValues.ConvertParameter(typeof(Guid), id);
          cmd.ExecuteNonQuery();
        }
      }

      return true;
    }

    #endregion
  }
}
