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
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.IO;
using System.Linq;
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
  /// Implementation of ISQLDatabase using the System.Data.SQLite wrapper for the SQLite database
  /// </summary>
  /// <remarks>
  /// The purpose of this database is to have an inbuilt database without the need to install an external
  /// database system (such as MySQL), which at the same time does not have relevant size limitations
  /// (such as MSSQLCE with a maximum database size of 2GB). The limitations of SQLite are much less
  /// restrictive (e.g. a maximum database size of about 140TB, for details see http://www.sqlite.org/limits.html)
  /// </remarks>
  public class SQLiteDatabase : ISQLDatabasePaging, IDisposable
  {
    #region Constants

    private const string DATABASE_TYPE_STRING = "SQLite"; // Name of this Database

    #endregion

    #region Variables

    private readonly string _connectionString;
    private ConnectionPool<SQLiteConnection> _connectionPool;
    private readonly SQLiteSettings _settings;
    private readonly ILogger _sqliteDebugLogger;
    private readonly AsynchronousMessageQueue _messageQueue;
    private readonly ActionBlock<bool> _maintenanceScheduler;

    #endregion

    #region Constructors/Destructors

    public SQLiteDatabase()
    {
      VersionUpgrade upgrade = new VersionUpgrade();
      if (!upgrade.Upgrade())
      {
        ServiceRegistration.Get<ILogger>().Warn("SQLiteDatabase: Could not upgrade existing database");
      }

      try
      {
        _maintenanceScheduler = new ActionBlock<bool>(async _ => await PerformDatabaseMaintenanceAsync(), new ExecutionDataflowBlockOptions { BoundedCapacity = 2 });
        _messageQueue = new AsynchronousMessageQueue(this, new[] { ContentDirectoryMessaging.CHANNEL });
        _messageQueue.MessageReceived += OnMessageReceived;
        _messageQueue.Start();

        _settings = ServiceRegistration.Get<ISettingsManager>().Load<SQLiteSettings>();
        _settings.LogSettings();

        LogVersionInformation();

        if (_settings.EnableTraceLogging)
        {
          _sqliteDebugLogger = FileLogger.CreateFileLogger(ServiceRegistration.Get<IPathManager>().GetPath(@"<LOG>\SQLiteDebug.log"), LogLevel.Debug, false, true);
          SQLiteLog.Initialize();
          SQLiteLog.RemoveDefaultHandler();
          SQLiteLog.Log += MPSQLiteLogEventHandler;
        }

        // We use our own collation sequence which is registered here to be able
        // to sort items taking into account culture specifics
        SQLiteFunction.RegisterFunction(typeof(SQLiteCultureSensitiveCollation));

        var pathManager = ServiceRegistration.Get<IPathManager>();
        string dataDirectory = pathManager.GetPath("<DATABASE>");
        string databaseFile = Path.Combine(dataDirectory, _settings.DatabaseFileName);

        // We use an URI instead of a simple database path and filename. The reason is that
        // only this way we can switch on the shared cache mode of SQLite in System.Data.SQLite
        // However, when using an URI, SQLite ignores the page size value specified in the connection string.
        // Therefore we have to use a workaround below to create a database file with the specified page size.
        string databaseFileForUri = databaseFile.Replace('\\', '/');
        string databaseUri = System.Web.HttpUtility.UrlPathEncode("file:///" + databaseFileForUri + "?cache=shared");

        _connectionPool = new ConnectionPool<SQLiteConnection>(CreateOpenAndInitializeConnection);

        // We are using the ConnectionStringBuilder to generate the connection string
        // This ensures code compatibility in case of changes to the SQLite connection string parameters
        // More information on the parameters can be found here: http://www.sqlite.org/pragma.html
        var connBuilder = new SQLiteConnectionStringBuilder
        {
          // Name of the database file including path as URI 
          FullUri = databaseUri,

          // Use SQLite database version 3.x  
          Version = 3,

          // Store GUIDs as binaries, not as string
          // Saves some space in the database and is said to make search queries on GUIDs faster  
          BinaryGUID = true,

          DefaultTimeout = _settings.LockTimeout,
          CacheSize = _settings.CacheSizeInPages,

          // Use the Write Ahead Log mode
          // In this journal mode write locks do not block reads
          // Needed to prevent sluggish behaviour of MP2 client when trying to read from the database (through MP2 server)
          // while MP2 server writes to the database (such as when importing shares)
          // More information can be found here: http://www.sqlite.org/wal.html
          JournalMode = SQLiteJournalModeEnum.Wal,

          // Do not use the inbuilt connection pooling of System.Data.SQLite
          // We use our own connection pool which is faster.
          Pooling = false,

          // Sychronization Mode "Normal" enables parallel database access while at the same time preventing database
          // corruption and is therefore a good compromise between "Off" (more performance) and "On"
          // More information can be found here: http://www.sqlite.org/pragma.html#pragma_synchronous
          SyncMode = SynchronizationModes.Normal,

          // MP2's database backend uses foreign key constraints to ensure referential integrity.
          // SQLite supports this, but it has to be enabled for each database connection by a PRAGMA command
          // For details see http://www.sqlite.org/foreignkeys.html
          ForeignKeys = true
        };

        if (_settings.EnableTraceLogging)
          connBuilder.Flags = SQLiteConnectionFlags.LogAll;

        _connectionString = connBuilder.ToString();
        ServiceRegistration.Get<ILogger>().Info("SQLiteDatabase: Connection String used: '{0}'", _connectionString);

        if (!File.Exists(databaseFile))
        {
          ServiceRegistration.Get<ILogger>().Info("SQLiteDatabase: Database file does not exists. Creating database file");

          if (!Directory.Exists(dataDirectory))
            Directory.CreateDirectory(dataDirectory);

          // Since we use an URI in the standard connection string and system.data.sqlite therefore
          // ignores the page size value in the connection string, this is a workaroung to make sure
          // the page size value as specified in the settings is used. When the database file does
          // not yet exists, we create a special connection string where we additionally set the
          // datasource (which overrides the URI) and the page size. We then create a connection with
          // that special connection string, open it and close it. That way the database file is created
          // with the desired page size. The page size is permanently stored in the database file so that
          // it is used when as of now we use connections with URI.
          connBuilder.DataSource = databaseFile;
          connBuilder.PageSize = _settings.PageSize;
          using (var connection = new SQLiteConnection(connBuilder.ToString()))
          {
            connection.Open();
            connection.Close();
          }
        }

        // The following is necessary to avoid the creation of of a shared memory index file
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
          connBuilder.JournalMode = SQLiteJournalModeEnum.Off;
          using (var connection = new SQLiteConnection(connBuilder.ToString()))
          {
            connection.Open();
            using (var command = new SQLiteCommand(SQLiteSettings.EXCLUSIVE_MODE_COMMAND, connection))
              command.ExecuteNonQuery();
            connection.Close();
          }
        }

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

    #region Public properties

    public ConnectionPool<SQLiteConnection> ConnectionPool
    {
      get
      {
        return _connectionPool;
      }
    }

    #endregion

    #region Private methods

    /// <summary>
    /// Creates a new <see cref="SQLiteConnection"/> object, opens it and executes the
    /// InitializationCommand via ExecuteNonQuery()
    /// </summary>
    /// <returns>Newly created and initialized <see cref="SQLiteConnection"/></returns>
    private SQLiteConnection CreateOpenAndInitializeConnection()
    {
      var connection = new SQLiteConnection(_connectionString);
      connection.Open();
      connection.SetChunkSize(_settings.ChunkSizeInMegabytes * 1024 * 1024);
      using (var command = new SQLiteCommand(_settings.InitializationCommand, connection))
        command.ExecuteNonQuery();
      return connection;
    }

    /// <summary>
    /// Log event handler for trace logging
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void MPSQLiteLogEventHandler(object sender, LogEventArgs e)
    {
      if (_sqliteDebugLogger == null || e == null)
        return;

      string logText = e.Message;
      if (logText == null)
        logText = "<null>";
      else
      {
        logText = logText.Trim();
        if (logText.Length == 0)
          logText = "<empty>";
      }

      _sqliteDebugLogger.Debug("SQLite ({0}): {1}", e.ErrorCode, logText);
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
        case ContentDirectoryMessaging.MessageType.ShareImportCompleted:
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
        using (var connection = new SQLiteConnection(_connectionString))
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
      LogStatistics();
    }

    /// <summary>
    /// Logs statistical data of the SQLite engine
    /// </summary>
    private void LogStatistics()
    {
      IDictionary<String, long> statistics = new Dictionary<string, long>();
      SQLiteConnection.GetMemoryStatistics(ref statistics);
      var statisticsString = String.Join("; ", statistics.Select(kvp => String.Format("{0}={1:N0}", kvp.Key, kvp.Value)));
      ServiceRegistration.Get<ILogger>().Debug("SQLiteDatabase: Memory Statistics: {0}", statisticsString);
    }

    /// <summary>
    /// Logs version information about the used SQLite libraries
    /// </summary>
    private void LogVersionInformation()
    {
      ServiceRegistration.Get<ILogger>().Info("SQLiteDatabase: ProviderVersion={0} (SourceID: {1})", SQLiteConnection.ProviderVersion, SQLiteConnection.ProviderSourceId);
      ServiceRegistration.Get<ILogger>().Info("SQLiteDatabase: InteropVersion: {0} (SourceID: {1})", SQLiteConnection.InteropVersion, SQLiteConnection.InteropSourceId);
      ServiceRegistration.Get<ILogger>().Debug("SQLiteDatabase: InteropCompileOptions: {0}", SQLiteConnection.InteropCompileOptions);
      ServiceRegistration.Get<ILogger>().Debug("SQLiteDatabase: InteropDefineConstants: {0}", SQLiteConnection.DefineConstants);
      ServiceRegistration.Get<ILogger>().Info("SQLiteDatabase: SQLiteVersion: {0} (SourceID: {1})", SQLiteConnection.SQLiteVersion, SQLiteConnection.SQLiteSourceId);
      ServiceRegistration.Get<ILogger>().Debug("SQLiteDatabase: SQLiteCompileOptions: {0}", SQLiteConnection.SQLiteCompileOptions);
    }

    #endregion

    #region ISQLDatabase implementation

    public string DatabaseType
    {
      get { return DATABASE_TYPE_STRING; }
    }

    public string DatabaseVersion
    {
      get { return SQLiteConnection.ProviderVersion; }
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
      if (type == typeof(byte[]))
      {
        var result = (SQLiteParameter)command.CreateParameter();
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
        var result = (byte[])((SQLiteDataReader)reader).GetValue(colIndex);
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

    public ITransaction CreateTransaction()
    {
      return new SQLiteTransaction(this, _settings);
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

      if (_connectionPool != null)
      {
        _connectionPool.Dispose();
        _connectionPool = null;
      }
    }

    #endregion
  }
}
