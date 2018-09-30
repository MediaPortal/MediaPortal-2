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

using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Backend.Database;
using MediaPortal.Utilities.DB;
using MediaPortal.Utilities.Exceptions;
using MediaPortal.Backend.Services.MediaLibrary;
using System.Linq;
using MediaPortal.Backend.ClientCommunication;
using MediaPortal.Common.Services.Database;
using MediaPortal.Backend.MediaLibrary;

namespace MediaPortal.Backend.Services.Database
{
  public class DatabaseManager : IDatabaseManager, IDisposable
  {
    public const string DUMMY_TABLE_NAME = "DUMMY";
    public const string BACKUP_TABLE_SUFFIX = "__OLD";

    public const int DATABASE_VERSION_MAJOR = 2;
    public const int DATABASE_VERSION_MINOR = 2;

    public const string MIA_TABLE_PLACEHOLDER = "%ASPECT_TABLE%";
    public const string MIA_V_TABLE_PLACEHOLDER = "%ASPECT_V_TABLE%";
    public const string MIA_NM_TABLE_PLACEHOLDER = "%ASPECT_NM_TABLE%";
    public const string SQL_CONCAT_PLACEHOLDER = "%SQL_CONCAT_OP%";
    public const string SQL_LEN_PLACEHOLDER = "%SQL_LEN_OP%";

    public const string MIGRATION_USER_PARAM = "MigrationUser";
    public readonly Guid MIGRATION_USER_GUID = Guid.Empty;

    protected Dictionary<string, string> _migrationScriptPlaceholders = new Dictionary<string, string>()
    {
      { "%SUFFIX%", BACKUP_TABLE_SUFFIX },
      { "%MIGRATION_USER%", "@" + MIGRATION_USER_PARAM }
    };

    protected bool _upgradeInProgress = false;
    protected MIA_Management _miaManagement = null;
    protected double _lastProgress = 0;

    public DatabaseManager()
    {
      MIGRATION_USER_GUID = Guid.NewGuid();
    }

    public void Dispose()
    {
    }

    protected string ExplicitVersionToString(int? versionMajor, int? versionMinor)
    {
      if (!versionMajor.HasValue)
        return "-";
      if (!versionMinor.HasValue)
        return versionMajor.Value + ".-";
      return versionMajor.Value + "." + versionMinor.Value;
    }

    #region Migration methods

    protected int GuessMiniorVersion(bool useBackupTable = false)
    {
      //Simple way to check for database version. 
      //Not pretty, but will only be used during initial introduction of database migration.
      int minorVersion = 0;
      ISQLDatabase database = ServiceRegistration.Get<ISQLDatabase>(false);
      using (ITransaction transaction = database.BeginTransaction())
      {
        using (var cmd = transaction.CreateCommand())
        {
          try
          {
            cmd.CommandText = "SELECT MAX(ISPRIMARY) FROM M_PROVIDERRESOURCE" + (useBackupTable ? BACKUP_TABLE_SUFFIX : "");
            cmd.ExecuteScalar();
            minorVersion = 1;
          }
          catch { }
          try
          {
            cmd.CommandText = "SELECT MAX(TYPE) FROM M_PROVIDERRESOURCE" + (useBackupTable ? BACKUP_TABLE_SUFFIX : "");
            cmd.ExecuteScalar();
            minorVersion = 2;
          }
          catch { }
        }
      }

      if (minorVersion == 0)
        throw new ArgumentOutOfRangeException("MinorVersion", "Database version 2.0 is not supported!");

      ServiceRegistration.Get<ILogger>().Info($"DatabaseManager: Detected old 2.{minorVersion} database");
      return minorVersion;
    }

    protected bool GetDatabaseVersion(out int versionMajor, out int versionMinor, bool useBackupTable = false)
    {
      versionMajor = 0;
      versionMinor = 0;
      ISQLDatabase database = ServiceRegistration.Get<ISQLDatabase>(false);
      if (!database.TableExists(MediaPortal_Basis_Schema.DATABASE_VERSION_TABLE_NAME + (useBackupTable ? BACKUP_TABLE_SUFFIX : "")))
      {
        return false;
      }

      ITransaction transaction = database.BeginTransaction();
      try
      {
        int versionIndex;
        IDbCommand command;
        if (useBackupTable)
          command = SelectBackupDatabaseVersionByCommand(transaction, out versionIndex);
        else
          command = MediaPortal_Basis_Schema.SelectDatabaseVersionByCommand(transaction, out versionIndex);
        using (command)
        using (IDataReader reader = command.ExecuteReader(CommandBehavior.SingleRow))
        {
          if (reader.Read())
          {
            if (Version.TryParse(reader.GetString(versionIndex), out Version version))
            {
              versionMajor = version.Major;
              versionMinor = version.Minor;
            }
            return true;
          }
          return false;
        }
      }
      finally
      {
        transaction.Dispose();
      }
    }

    protected bool SetDatabaseVersion(int versionMajor, int versionMinor, bool useBackupTable = false)
    {
      Version newVersion = new Version(versionMajor, versionMinor);
      ISQLDatabase database = ServiceRegistration.Get<ISQLDatabase>(false);
      if (!database.TableExists(MediaPortal_Basis_Schema.DATABASE_VERSION_TABLE_NAME + (useBackupTable ? BACKUP_TABLE_SUFFIX : "")))
      {
        return false;
      }

      ITransaction transaction = database.BeginTransaction();
      try
      {
        IDbCommand command;
        if (useBackupTable)
          command = UpdateBackupDatabaseVersionCommand(transaction, newVersion.ToString(2));
        else
          command = MediaPortal_Basis_Schema.UpdateDatabaseVersionCommand(transaction, newVersion.ToString(2));
        using (command)
        {
          command.ExecuteNonQuery();
        }
        transaction.Commit();
        return true;
      }
      catch (Exception e)
      {
        ServiceRegistration.Get<ILogger>().Error("DatabaseManager: Error updating database version", e);
        transaction.Rollback();
        throw;
      }
    }

    protected IEnumerable<(Guid Id, string Name, DateTime Date)> GetMiaTypes(bool useBackupTable = false)
    {
      ISQLDatabase database = ServiceRegistration.Get<ISQLDatabase>(false);
      using (ITransaction transaction = database.BeginTransaction())
      {
        List<(Guid Id, string Name, DateTime Date)> miams = new List<(Guid Id, string Name, DateTime Date)>();
        int idIndex;
        int nameIndex;
        int dateIndex;
        IDbCommand command;
        if(useBackupTable)
          command = SelectAllBackupMediaItemAspectMetadataNameAndCreationDatesCommand(transaction, out idIndex, out nameIndex, out dateIndex);
        else
          command = MediaLibrary_SubSchema.SelectAllMediaItemAspectMetadataNameAndCreationDatesCommand(transaction, out idIndex, out nameIndex, out dateIndex);
        using (command)
        using (IDataReader reader = command.ExecuteReader())
        {
          ICollection<string> result = new List<string>();
          while (reader.Read())
            miams.Add((reader.GetGuid(idIndex), reader.GetString(nameIndex), reader.GetDateTime(dateIndex)));
        }
        return miams;
      }
    }

    protected DatabaseMigrationManager GetMiaMigrationManager(Guid miaId, string miaName)
    {
      if(_miaManagement == null)
        _miaManagement = new MIA_Management();

      //Check if table for mia can be found
      var metaData = _miaManagement.GetMediaItemAspectMetadata(miaId);
      string tableName = _miaManagement.GetMIATableName(metaData);
      if (string.IsNullOrEmpty(tableName))
        return null;

      //Check if backup table exists
      ISQLDatabase database = ServiceRegistration.Get<ISQLDatabase>(false);
      if (!database.TableExists($"{tableName}{BACKUP_TABLE_SUFFIX}"))
        return null;

      //Add main table
      IDictionary<string, IList<string>> defaultScriptPlaceholderTables = new Dictionary<string, IList<string>>();
      defaultScriptPlaceholderTables.Add(MIA_TABLE_PLACEHOLDER, new List<string> { tableName });

      //Add collection tables
      foreach(var attribute in metaData.AttributeSpecifications)
      {
        if (attribute.Value.IsCollectionAttribute)
        {
          if (attribute.Value.Cardinality == Common.MediaManagement.Cardinality.OneToMany ||
            attribute.Value.Cardinality == Common.MediaManagement.Cardinality.ManyToMany)
          {
            tableName = _miaManagement.GetMIACollectionAttributeTableName(attribute.Value);
            if (string.IsNullOrEmpty(tableName))
              continue;
            if (!database.TableExists($"{tableName}{BACKUP_TABLE_SUFFIX}"))
              continue;

            if (!defaultScriptPlaceholderTables.ContainsKey(MIA_V_TABLE_PLACEHOLDER))
              defaultScriptPlaceholderTables.Add(MIA_V_TABLE_PLACEHOLDER, new List<string>());
            defaultScriptPlaceholderTables[MIA_V_TABLE_PLACEHOLDER].Add(tableName);
          }
          if (attribute.Value.Cardinality == Common.MediaManagement.Cardinality.ManyToMany)
          {
            tableName = _miaManagement.GetMIACollectionAttributeNMTableName(attribute.Value);
            if (string.IsNullOrEmpty(tableName))
              continue;
            if (!database.TableExists($"{tableName}{BACKUP_TABLE_SUFFIX}"))
              continue;

            if (!defaultScriptPlaceholderTables.ContainsKey(MIA_NM_TABLE_PLACEHOLDER))
              defaultScriptPlaceholderTables.Add(MIA_NM_TABLE_PLACEHOLDER, new List<string>());
            defaultScriptPlaceholderTables[MIA_NM_TABLE_PLACEHOLDER].Add(tableName);
          }
        }
      }

      DatabaseMigrationManager manager = new DatabaseMigrationManager(miaName, "DefaultAspect", defaultScriptPlaceholderTables);
      manager.AddDirectory(MediaPortal_Basis_Schema.DatabaseUpgradeScriptDirectory);
      return manager;
    }

    protected void SendUpgradeProgress(double currentStep, double totalSteps)
    {
      try
      {
        double progress = (currentStep / totalSteps) * 100.0;
        if (progress > 100)
          progress = 100;

        if (_lastProgress > progress)
          return;
        _lastProgress = progress;

        var state = new DatabaseUpgradeServerState
        {
          IsUpgrading = progress < 100,
          Progress = (progress < 100) ? Convert.ToInt32(progress) : -1
        };
        ServiceRegistration.Get<ILogger>().Info("DatabaseManager: Database upgrade progress {0}%", progress);
        ServiceRegistration.Get<IServerStateService>().UpdateState(DatabaseUpgradeServerState.STATE_ID, state);
      }
      catch { }
    }

    #endregion

    #region Backup table commands

    protected IDbCommand SelectBackupDatabaseVersionByCommand(ITransaction transaction, out int versionParameterIndex)
    {
      IDbCommand result = transaction.CreateCommand();
      result.CommandText = $"SELECT VERSION FROM PRODUCT_VERSION{BACKUP_TABLE_SUFFIX} WHERE PRODUCT = @PRODUCT_NAME";
      ISQLDatabase database = transaction.Database;
      database.AddParameter(result, "PRODUCT_NAME", "MediaPortal", typeof(string));
      versionParameterIndex = 0;
      return result;
    }

    protected IDbCommand UpdateBackupDatabaseVersionCommand(ITransaction transaction, string version)
    {
      IDbCommand result = transaction.CreateCommand();
      result.CommandText = $"UPDATE PRODUCT_VERSION{BACKUP_TABLE_SUFFIX} SET VERSION=@VERSION WHERE PRODUCT=@PRODUCT_NAME";
      ISQLDatabase database = transaction.Database;
      database.AddParameter(result, "VERSION", version, typeof(string));
      database.AddParameter(result, "PRODUCT_NAME", "MediaPortal", typeof(string));
      return result;
    }

    protected IDbCommand SelectAllBackupMediaItemAspectMetadataNameAndCreationDatesCommand(ITransaction transaction,
        out int aspectIdIndex, out int nameIndex, out int creationDateIndex)
    {
      IDbCommand result = transaction.CreateCommand();
      result.CommandText = "SELECT MIAM_ID, NAME, CREATION_DATE FROM MIA_TYPES";

      aspectIdIndex = 0;
      nameIndex = 1;
      creationDateIndex = 2;
      return result;
    }

    #endregion

    #region IDatabaseManager implementation

    public string DummyTableName => DUMMY_TABLE_NAME;

    public bool UpgradeInProgress => _upgradeInProgress;

    public void Startup()
    {
      ISQLDatabase database = ServiceRegistration.Get<ISQLDatabase>(false);
      if (database == null)
        throw new IllegalCallException("There is no database present in the system");

      if (!_migrationScriptPlaceholders.ContainsKey(SQL_CONCAT_PLACEHOLDER))
        _migrationScriptPlaceholders.Add(SQL_CONCAT_PLACEHOLDER, database.ConcatOperator);
      if (!_migrationScriptPlaceholders.ContainsKey(SQL_LEN_PLACEHOLDER))
        _migrationScriptPlaceholders.Add(SQL_LEN_PLACEHOLDER, database.LengthFunction);

      // Prepare schema
      if (!database.TableExists(MediaPortal_Basis_Schema.MEDIAPORTAL_BASIS_TABLE_NAME))
      {
        ServiceRegistration.Get<ILogger>().Info("DatabaseManager: Creating subschema '{0}'", MediaPortal_Basis_Schema.SUBSCHEMA_NAME);
        using (ITransaction transaction = database.BeginTransaction(IsolationLevel.Serializable))
        {
          using (TextReader reader = new SqlScriptPreprocessor(MediaPortal_Basis_Schema.SubSchemaCreateScriptPath))
            ExecuteBatch(transaction, new InstructionList(reader));
          transaction.Commit();
        }
        //A newly created database will be of the latest version
        SetDatabaseVersion(DATABASE_VERSION_MAJOR, DATABASE_VERSION_MINOR);
      }
      // Hint: Table MEDIAPORTAL_BASIS contains a sub schema entry for "MEDIAPORTAL_BASIS" with version number 1.0
      int versionMajor;
      int versionMinor;
      if (!GetSubSchemaVersion(MediaPortal_Basis_Schema.SUBSCHEMA_NAME, out versionMajor, out versionMinor))
        throw new UnexpectedStateException("{0} schema is not present or corrupted", MediaPortal_Basis_Schema.SUBSCHEMA_NAME);
      ServiceRegistration.Get<ILogger>().Info("DatabaseManager: Subschema '{0}' present in version {1}.{2}",
          MediaPortal_Basis_Schema.SUBSCHEMA_NAME, versionMajor, versionMinor);
    }

    public bool UpgradeDatabase()
    {
      ISQLDatabase database = ServiceRegistration.Get<ISQLDatabase>(false);
      int curVersionMajor;
      int curVersionMinor;
      if (GetDatabaseVersion(out curVersionMajor, out curVersionMinor) &&
        (curVersionMajor < DATABASE_VERSION_MAJOR ||
        (curVersionMajor == DATABASE_VERSION_MAJOR && curVersionMinor < DATABASE_VERSION_MINOR)))
      {
        //Database was not migratable before version 2.1
        if (curVersionMajor == 2 && curVersionMinor == 0)
          curVersionMinor = GuessMiniorVersion();

        _upgradeInProgress = true;
        SendUpgradeProgress(0, 100);
        ServiceRegistration.Get<ILogger>().Info("DatabaseManager: Initiating update to database version {0}.{1}", DATABASE_VERSION_MAJOR, DATABASE_VERSION_MINOR);
        Version currrentVersion = new Version(curVersionMajor, curVersionMinor);
        if (database.BackupDatabase(currrentVersion.ToString(2)))
        {
          SendUpgradeProgress(5, 100);
          if (database.BackupTables(BACKUP_TABLE_SUFFIX))
          {
            SendUpgradeProgress(10, 100);
            using (ITransaction transaction = database.BeginTransaction(IsolationLevel.Serializable))
            {
              ServiceRegistration.Get<ILogger>().Info("DatabaseManager: Creating subschema '{0}'", MediaPortal_Basis_Schema.SUBSCHEMA_NAME);
              using (TextReader reader = new SqlScriptPreprocessor(MediaPortal_Basis_Schema.SubSchemaCreateScriptPath))
                ExecuteBatch(transaction, new InstructionList(reader));
              transaction.Commit();
            }
            //A newly created database will always be of the latest version
            SetDatabaseVersion(DATABASE_VERSION_MAJOR, DATABASE_VERSION_MINOR);

            //Set MediaLibrary in maintenance mode
            if(ServiceRegistration.Get<IMediaLibrary>() is MediaLibrary.MediaLibrary mediaLibrary)
              mediaLibrary.MaintenanceMode = true;
            return true;
          }
        }
      }
      return false;
    }

    public bool MigrateDatabaseData()
    {
      ISQLDatabase database = ServiceRegistration.Get<ISQLDatabase>(false);
      int curVersionMajor;
      int curVersionMinor;
      int totalMigrationSteps = 1;
      try
      {
        if (GetDatabaseVersion(out curVersionMajor, out curVersionMinor, true))
        {
          if (curVersionMajor != DATABASE_VERSION_MAJOR || curVersionMinor != DATABASE_VERSION_MINOR)
          {
            ServiceRegistration.Get<ILogger>().Info("DatabaseManager: Initiating data migration to database version {0}.{1}", DATABASE_VERSION_MAJOR, DATABASE_VERSION_MINOR);
            //Database was not migratable before version 2.1
            if (curVersionMajor == 2 && curVersionMinor == 0)
              curVersionMinor = GuessMiniorVersion(true);

            var miaTypes = GetMiaTypes().OrderBy(m => m.Date);
            var oldMiaTypes = GetMiaTypes(true);

            //Add subschemas
            List<DatabaseMigrationManager> schemaManagers = new List<DatabaseMigrationManager>();
            foreach (string subSchema in GetDatabaseSubSchemas())
            {
              DatabaseMigrationManager schemaManager = new DatabaseMigrationManager(subSchema);
              schemaManager.AddDirectory(MediaPortal_Basis_Schema.DatabaseUpgradeScriptDirectory);
              schemaManagers.Add(schemaManager);
            }
            //Add aspects
            List<DatabaseMigrationManager> aspectManagers = new List<DatabaseMigrationManager>();
            foreach (var mia in miaTypes)
            {
              DatabaseMigrationManager aspectManager = GetMiaMigrationManager(mia.Id, mia.Name);
              if (aspectManager != null)
                aspectManagers.Add(aspectManager);
              else
                ServiceRegistration.Get<ILogger>().Warn("DatabaseManager: Migration of aspect '{0}' skipped because no migration manager was available", mia.Name);
            }
            foreach (var mia in oldMiaTypes.Where(o => !miaTypes.Any(m => m.Id == o.Id)))
            {
              ServiceRegistration.Get<ILogger>().Warn("DatabaseManager: Migration of aspect '{0}' skipped because it no longer exists", mia.Name);
            }
            //Add cleanup script
            List<DatabaseMigrationManager> scriptManagers = new List<DatabaseMigrationManager>();
            DatabaseMigrationManager cleanupManager = new DatabaseMigrationManager("Cleanup");
            cleanupManager.AddDirectory(MediaPortal_Basis_Schema.DatabaseUpgradeScriptDirectory);
            scriptManagers.Add(cleanupManager);

            totalMigrationSteps = schemaManagers.Count + aspectManagers.Count + scriptManagers.Count; //All migrations
            totalMigrationSteps += 2; //Add backup steps that has already been completed
            totalMigrationSteps += 2; //Add commit and drop steps that will be done after migration is complete
            int currentMigrationStep = 2; //Backup is already complete
            using (ITransaction transaction = database.BeginTransaction(IsolationLevel.Serializable))
            {
              //Migrate sub schema data. Note that not all sub schemas need to be migrated
              foreach (var manager in schemaManagers)
              {
                if (manager.MigrateData(transaction, curVersionMajor, curVersionMinor, DATABASE_VERSION_MAJOR, DATABASE_VERSION_MINOR))
                  ServiceRegistration.Get<ILogger>().Info("DatabaseManager: Migrated subschema '{0}'", manager.MigrationOwnerName);
                SendUpgradeProgress(++currentMigrationStep, totalMigrationSteps);
              }

              //Migrate aspect data
              foreach (var manager in aspectManagers)
              {
                if (manager.MigrateData(transaction, curVersionMajor, curVersionMinor, DATABASE_VERSION_MAJOR, DATABASE_VERSION_MINOR))
                  ServiceRegistration.Get<ILogger>().Info("DatabaseManager: Migrated aspect '{0}'", manager.MigrationOwnerName);
                else
                  ServiceRegistration.Get<ILogger>().Warn("DatabaseManager: Migration of aspect '{0}' failed", manager.MigrationOwnerName);
                SendUpgradeProgress(++currentMigrationStep, totalMigrationSteps);
              }

              //Scripts
              foreach (var manager in scriptManagers)
              {
                if (manager.MigrateData(transaction, curVersionMajor, curVersionMinor, DATABASE_VERSION_MAJOR, DATABASE_VERSION_MINOR))
                  ServiceRegistration.Get<ILogger>().Info("DatabaseManager: Executed script '{0}'", manager.MigrationOwnerName);
                else
                  ServiceRegistration.Get<ILogger>().Warn("DatabaseManager: Execution of script '{0}' failed", manager.MigrationOwnerName);
                SendUpgradeProgress(++currentMigrationStep, totalMigrationSteps);
              }

              transaction.Commit();
              SendUpgradeProgress(++currentMigrationStep, totalMigrationSteps);
            }
            SetDatabaseVersion(DATABASE_VERSION_MAJOR, DATABASE_VERSION_MINOR, true);
            if (!GetDatabaseVersion(out curVersionMajor, out curVersionMinor, true) || curVersionMajor != DATABASE_VERSION_MAJOR || curVersionMinor != DATABASE_VERSION_MINOR)
              throw new IllegalCallException(string.Format("Unable to migrate database data to version {0}.{1}", DATABASE_VERSION_MAJOR, DATABASE_VERSION_MINOR));
          }
          database.DropBackupTables(BACKUP_TABLE_SUFFIX);
          ServiceRegistration.Get<ILogger>().Info("DatabaseManager: Successfully migrated database data to version {0}.{1}", DATABASE_VERSION_MAJOR, DATABASE_VERSION_MINOR);

          //Exit MediaLibrary maintenance mode
          if (ServiceRegistration.Get<IMediaLibrary>() is MediaLibrary.MediaLibrary mediaLibrary)
            mediaLibrary.MaintenanceMode = false;
          SendUpgradeProgress(totalMigrationSteps, totalMigrationSteps);
          return true;
        }
        return false;
      }
      finally
      {
        _upgradeInProgress = false;
      }
    }

    public ICollection<string> GetDatabaseSubSchemas()
    {
      ISQLDatabase database = ServiceRegistration.Get<ISQLDatabase>(false);
      using (ITransaction transaction = database.BeginTransaction())
      {
        int nameIndex;
        using (IDbCommand command = MediaPortal_Basis_Schema.SelectAllSubSchemaNames(transaction, out nameIndex))
        using (IDataReader reader = command.ExecuteReader())
        {
          ICollection<string> result = new List<string>();
          while (reader.Read())
            result.Add(database.ReadDBValue<string>(reader, nameIndex));
          return result;
        }
      }
    }

    public bool GetSubSchemaVersion(string subSchemaName, out int versionMajor, out int versionMinor)
    {
      ISQLDatabase database = ServiceRegistration.Get<ISQLDatabase>(false);
      versionMajor = 0;
      versionMinor = 0;
      int versionMajorParameterIndex;
      int versionMinorParameterIndex;
      using (ITransaction transaction = database.BeginTransaction())
      {
        using (IDbCommand command = MediaPortal_Basis_Schema.SelectVersionBySubschemaCommand(transaction, subSchemaName,
            out versionMajorParameterIndex, out versionMinorParameterIndex))
        using (IDataReader reader = command.ExecuteReader(CommandBehavior.SingleRow))
        {
          if (reader.Read())
          {
            // Versions are marked NOT NULL, so it is safe not to check for DBNull
            versionMajor = reader.GetInt32(versionMajorParameterIndex);
            versionMinor = reader.GetInt32(versionMinorParameterIndex);
            return true;
          }
          return false;
        }
      }
    }

    public bool UpdateSubSchema(string subSchemaName, int? currentVersionMajor, int? currentVersionMinor,
        string updateScriptFilePath, int newVersionMajor, int newVersionMinor)
    {
      try
      {
        ISQLDatabase database = ServiceRegistration.Get<ISQLDatabase>(false);
        int versionMajor;
        int versionMinor;
        bool schemaPresent = GetSubSchemaVersion(subSchemaName, out versionMajor, out versionMinor);
        using (ITransaction transaction = database.BeginTransaction(IsolationLevel.Serializable))
        {
          if (schemaPresent)
          {
            if (currentVersionMajor.HasValue && currentVersionMajor.Value == versionMajor &&
                currentVersionMinor.HasValue && currentVersionMinor.Value == versionMinor)
            {
              ServiceRegistration.Get<ILogger>().Debug("DatabaseManager: Updating subschema '{0}' from version {1}.{2} to version {3}.{4}...",
                  subSchemaName, versionMajor, versionMinor, newVersionMajor, newVersionMinor);
              using (TextReader reader = new SqlScriptPreprocessor(updateScriptFilePath))
                ExecuteBatch(transaction, new InstructionList(reader));
            }
            else
            {
              throw new ArgumentException(string.Format(
                  "The current version of sub schema '{0}' is {1}.{2}, but the schema update script is given for version {3}",
                  subSchemaName, versionMajor, versionMinor, ExplicitVersionToString(currentVersionMajor, currentVersionMinor)));
            }
          }
          else // !schemaPresent
          {
            if (!currentVersionMajor.HasValue && !currentVersionMinor.HasValue)
            {
              ServiceRegistration.Get<ILogger>().Debug("DatabaseManager: Creating subschema '{0}' version {1}.{2}...",
                  subSchemaName, newVersionMajor, newVersionMinor);
              using (TextReader reader = new SqlScriptPreprocessor(updateScriptFilePath))
                ExecuteBatch(transaction, new InstructionList(reader));
            }
            else
            {
              throw new ArgumentException(string.Format(
                  "The sub schema '{0}' is not present yet, but the schema update script is given for version {1}",
                  subSchemaName, ExplicitVersionToString(currentVersionMajor, currentVersionMinor)));
            }
          }
          IDbCommand command;
          if (schemaPresent)
            command = MediaPortal_Basis_Schema.UpdateSubSchemaCommand(
                transaction, subSchemaName, newVersionMajor, newVersionMinor);
          else
            command = MediaPortal_Basis_Schema.InsertSubSchemaCommand(
                transaction, subSchemaName, newVersionMajor, newVersionMinor);
          command.ExecuteNonQuery();

          transaction.Commit();
        }
        ServiceRegistration.Get<ILogger>().Info("DatabaseManager: Subschema '{0}' present in version {1}.{2}", subSchemaName,
            newVersionMajor, newVersionMinor);
        return true;
      }
      catch (Exception e)
      {
        ServiceRegistration.Get<ILogger>().Error("DatabaseManager: Error updating subschema '{0}'", e, subSchemaName);
        throw;
      }
    }

    public void DeleteSubSchema(string subSchemaName, int currentVersionMajor, int currentVersionMinor, string deleteScriptFilePath)
    {
      ISQLDatabase database = ServiceRegistration.Get<ISQLDatabase>(false);
      using (ITransaction transaction = database.BeginTransaction(IsolationLevel.Serializable))
      {
        int versionMajor;
        int versionMinor;
        bool schemaPresent = GetSubSchemaVersion(subSchemaName, out versionMajor, out versionMinor);
        if (!schemaPresent)
          return;
        if (currentVersionMajor == versionMajor && currentVersionMinor == versionMinor)
          using (TextReader reader = new SqlScriptPreprocessor(deleteScriptFilePath))
            ExecuteBatch(transaction, new InstructionList(reader));
        else
          throw new ArgumentException(string.Format("The current version of sub schema '{0}' is {1}.{2}, but the schema deletion script works for version {3}.{4}",
              subSchemaName, versionMajor, versionMinor, currentVersionMajor, currentVersionMajor));

          using (IDbCommand command = MediaPortal_Basis_Schema.DeleteSubSchemaCommand(transaction, subSchemaName))
            command.ExecuteNonQuery();

        transaction.Commit();
      }
    }

    public void MigrateData(ITransaction transaction, string subSchemaName, string migrateScriptFilePath, IDictionary<string, IList<string>> migrationPlaceholderTables)
    {
      using (TextReader reader = new SqlScriptPreprocessor(migrateScriptFilePath, _migrationScriptPlaceholders))
        ExecuteBatch(transaction, new InstructionList(reader), migrationPlaceholderTables);
    }

    public void ExecuteBatch(ITransaction transaction, InstructionList instructions, IDictionary<string, IList<string>> migrationPlaceholderTables = null)
    {
      ISQLDatabase database = ServiceRegistration.Get<ISQLDatabase>(false);
      foreach (string instr in instructions)
      {
        List<string> subInstructions = new List<string>() { instr };
        if (migrationPlaceholderTables != null)
        {
          foreach (string placeholder in migrationPlaceholderTables.Keys)
          {
            if (instr.Contains(placeholder))
            {
              subInstructions = new List<string>();
              foreach (string table in migrationPlaceholderTables[placeholder])
              {
                subInstructions.Add(instr.Replace(placeholder, table));
              }
            }
          }
        }

        foreach (string subInstr in subInstructions)
        {
          //Ignore instructions with placeholders
          if (subInstr.Contains(MIA_TABLE_PLACEHOLDER) || subInstr.Contains(MIA_V_TABLE_PLACEHOLDER) || subInstr.Contains(MIA_NM_TABLE_PLACEHOLDER))
            continue;

          using (IDbCommand cmd = transaction.CreateCommand())
          {
            if (subInstr.Contains("@" + MIGRATION_USER_PARAM))
              database.AddParameter(cmd, MIGRATION_USER_PARAM, MIGRATION_USER_GUID, typeof(Guid));

            string sql = subInstr;
            AppendStorageClause(database, ref sql);
            cmd.CommandText = sql;
            ServiceRegistration.Get<ILogger>().Debug("DatabaseManager: Executing command '{0}'", cmd.CommandText);
            cmd.ExecuteNonQuery();
          }
        }
      }
    }

    public static void AppendStorageClause(ISQLDatabase database, ref string createTableStatement)
    {
      ISQLDatabaseStorage storage = database as ISQLDatabaseStorage;
      if (storage == null || string.IsNullOrEmpty(createTableStatement))
        return;

      // Process statement and append clause
      createTableStatement += storage.GetStorageClause(createTableStatement);
    }

    #endregion
  }
}
