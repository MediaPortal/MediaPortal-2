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
using System.IO;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Backend.Database;
using MediaPortal.Utilities.DB;
using MediaPortal.Utilities.Exceptions;
using MediaPortal.Backend.Services.MediaLibrary;
using System.Linq;

namespace MediaPortal.Backend.Services.Database
{
  public class DatabaseManager : IDatabaseManager, IDisposable
  {
    public const string DUMMY_TABLE_NAME = "DUMMY";
    public const string BACKUP_TABLE_SUFFIX = "__OLD";
    public const string IGNORE_TOKEN = "%%IGNORE%%";

    public const int DATABASE_VERSION_MAJOR = 2;
    public const int DATABASE_VERSION_MINOR = 2;

    protected Dictionary<string, string> _migrationScriptPlaceholders = new Dictionary<string, string>()
    {
      { "%SUFFIX%", BACKUP_TABLE_SUFFIX }
    };

    public DatabaseManager()
    {
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

    protected IEnumerable<(Guid Id, string Name)> GetMiaObjects()
    {
      ISQLDatabase database = ServiceRegistration.Get<ISQLDatabase>(false);
      using (ITransaction transaction = database.BeginTransaction())
      {
        List<(Guid Id, string Name)> objects = new List<(Guid Id, string Name)>();
        int idIndex;
        int nameIndex;
        int objectIndex;
        IDbCommand command = MediaLibrary_SubSchema.SelectMIANameAliasesCommand(transaction, out idIndex, out nameIndex, out objectIndex);
        using (command)
        using (IDataReader reader = command.ExecuteReader())
        {
          ICollection<string> result = new List<string>();
          while (reader.Read())
            objects.Add((reader.GetGuid(idIndex), reader.GetString(objectIndex)));
        }
        return objects;
      }
    }

    protected DatabaseMigrationManager GetMiaMigrationManager(string miaName, IEnumerable<string> miaObjects)
    {
      //Check if table for mia can be found
      string tableName = miaObjects.FirstOrDefault(o => o.StartsWith("M_") && !o.Contains("_PK"));
      if (string.IsNullOrEmpty(tableName))
        return null;

      //Check if backup table exists
      ISQLDatabase database = ServiceRegistration.Get<ISQLDatabase>(false);
      if (!database.TableExists($"{tableName}{BACKUP_TABLE_SUFFIX}"))
        return null;

      //Add main table
      Dictionary<string, string> defaultScriptPlaceholders = new Dictionary<string, string>();
      defaultScriptPlaceholders.Add("%ASPECT_TABLE%", tableName);

      //Add collection tables
      tableName = miaObjects.FirstOrDefault(o => o.StartsWith("V_") && !o.Contains("_PK"));
      if (!string.IsNullOrEmpty(tableName) && database.TableExists($"{tableName}{BACKUP_TABLE_SUFFIX}"))
      {
        defaultScriptPlaceholders.Add("%ASPECT_V_TABLE%", tableName);
      }
      else
      {
        //Add it so it gets ignored
        defaultScriptPlaceholders.Add("%ASPECT_V_TABLE%", IGNORE_TOKEN);
      }
      tableName = miaObjects.FirstOrDefault(o => o.StartsWith("NM_") && !o.Contains("_PK"));
      if (!string.IsNullOrEmpty(tableName) && database.TableExists($"{tableName}{BACKUP_TABLE_SUFFIX}"))
      {
        defaultScriptPlaceholders.Add("%ASPECT_NM_TABLE%", tableName);
      }
      else
      {
        //Add it so it gets ignored
        defaultScriptPlaceholders.Add("%ASPECT_NM_TABLE%", IGNORE_TOKEN);
      }

      DatabaseMigrationManager manager = new DatabaseMigrationManager(miaName, "DefaultAspect", defaultScriptPlaceholders);
      manager.AddDirectory(MediaPortal_Basis_Schema.DatabaseUpgradeScriptDirectory);
      return manager;
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

    public string DummyTableName
    {
      get { return DUMMY_TABLE_NAME; }
    }

    public void Startup()
    {
      ISQLDatabase database = ServiceRegistration.Get<ISQLDatabase>(false);
      if (database == null)
        throw new IllegalCallException("There is no database present in the system");

      // Prepare schema
      if (!database.TableExists(MediaPortal_Basis_Schema.MEDIAPORTAL_BASIS_TABLE_NAME))
      {
        ServiceRegistration.Get<ILogger>().Info("DatabaseManager: Creating subschema '{0}'", MediaPortal_Basis_Schema.SUBSCHEMA_NAME);
        using (ITransaction transaction = database.BeginTransaction())
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
        ServiceRegistration.Get<ILogger>().Info("DatabaseManager: Initiating update to database version {0}.{1}", DATABASE_VERSION_MAJOR, DATABASE_VERSION_MINOR);
        Version currrentVersion = new Version(curVersionMajor, curVersionMinor);
        if (database.BackupDatabase(currrentVersion.ToString(2)) && database.BackupTables(BACKUP_TABLE_SUFFIX))
        {
          using (ITransaction transaction = database.BeginTransaction())
          {
            ServiceRegistration.Get<ILogger>().Info("DatabaseManager: Creating subschema '{0}'", MediaPortal_Basis_Schema.SUBSCHEMA_NAME);
            using (TextReader reader = new SqlScriptPreprocessor(MediaPortal_Basis_Schema.SubSchemaCreateScriptPath))
              ExecuteBatch(transaction, new InstructionList(reader));
            transaction.Commit();
          }
          //A newly created database will always be of the latest version
          SetDatabaseVersion(DATABASE_VERSION_MAJOR, DATABASE_VERSION_MINOR);
          return true;
        }
      }
      return false;
    }

    public bool MigrateDatabaseData()
    {
      ISQLDatabase database = ServiceRegistration.Get<ISQLDatabase>(false);
      int curVersionMajor;
      int curVersionMinor;
      if (GetDatabaseVersion(out curVersionMajor, out curVersionMinor, true))
      {
        if (curVersionMajor != DATABASE_VERSION_MAJOR || curVersionMinor != DATABASE_VERSION_MINOR)
        {
          ServiceRegistration.Get<ILogger>().Info("DatabaseManager: Initiating data migration to database version {0}.{1}", DATABASE_VERSION_MAJOR, DATABASE_VERSION_MINOR);
          //Database was not migratable before version 2.1
          if (curVersionMajor == 2 && curVersionMinor == 0)
            curVersionMinor = 1;

          var miaTypes = GetMiaTypes().OrderBy(m => m.Date);
          var oldMiaTypes = GetMiaTypes(true);
          var miaObjects = GetMiaObjects();

          //Add subschemas
          List<DatabaseMigrationManager> schemaManagers = new List<DatabaseMigrationManager>();
          foreach (string subSchema in GetDatabaseSubSchemas())
          {
            DatabaseMigrationManager manager = new DatabaseMigrationManager(subSchema);
            manager.AddDirectory(MediaPortal_Basis_Schema.DatabaseUpgradeScriptDirectory);
            schemaManagers.Add(manager);
          }
          //Add aspects
          List<DatabaseMigrationManager> aspectManagers = new List<DatabaseMigrationManager>();
          foreach (var mia in miaTypes)
          {
            DatabaseMigrationManager manager = GetMiaMigrationManager(mia.Name, miaObjects.Where(o => o.Id == mia.Id).Select(o => o.Name));
            if(manager != null)
              aspectManagers.Add(manager);
            else
              ServiceRegistration.Get<ILogger>().Warn("DatabaseManager: Migration of aspect '{0}' skipped because no migration manager was available", mia.Name);
          }
          foreach (var mia in oldMiaTypes.Where(o => !miaTypes.Any(m => m.Id == o.Id)))
          {
            ServiceRegistration.Get<ILogger>().Warn("DatabaseManager: Migration of aspect '{0}' skipped because it no longer exists", mia.Name);
          }

          using (ITransaction transaction = database.BeginTransaction())
          {
            //Migrate sub schema data. Note that not all sub schemas need to be migrated
            foreach (var manager in schemaManagers)
            {
              if (manager.MigrateData(transaction, curVersionMajor, curVersionMinor, DATABASE_VERSION_MAJOR, DATABASE_VERSION_MINOR))
                ServiceRegistration.Get<ILogger>().Info("DatabaseManager: Migrated subschema '{0}'", manager.MigrationOwnerName);
            }

            //Migrate aspect data
            foreach (var manager in aspectManagers)
            {
              if (manager.MigrateData(transaction, curVersionMajor, curVersionMinor, DATABASE_VERSION_MAJOR, DATABASE_VERSION_MINOR))
                ServiceRegistration.Get<ILogger>().Info("DatabaseManager: Migrated aspect '{0}'", manager.MigrationOwnerName);
              else
                ServiceRegistration.Get<ILogger>().Warn("DatabaseManager: Migration of aspect '{0}' failed", manager.MigrationOwnerName);
            }
            transaction.Commit();
          }
          SetDatabaseVersion(DATABASE_VERSION_MAJOR, DATABASE_VERSION_MINOR, true);
          if (!GetDatabaseVersion(out curVersionMajor, out curVersionMinor, true) || curVersionMajor != DATABASE_VERSION_MAJOR || curVersionMinor != DATABASE_VERSION_MINOR)
            throw new IllegalCallException(string.Format("Unable to migrate database data to version {0}.{1}", DATABASE_VERSION_MAJOR, DATABASE_VERSION_MINOR));
        }
        database.DropBackupTables(BACKUP_TABLE_SUFFIX);
        ServiceRegistration.Get<ILogger>().Info("DatabaseManager: Successfully migrated database data to version {0}.{1}", DATABASE_VERSION_MAJOR, DATABASE_VERSION_MINOR);
        return true;
      }
      return false;
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
        using (ITransaction transaction = database.BeginTransaction())
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
      using (ITransaction transaction = database.BeginTransaction())
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

    public void MigrateData(ITransaction transaction, string subSchemaName, string migrateScriptFilePath, IDictionary<string, string> migrationPlaceholders)
    {
      var placeholders = _migrationScriptPlaceholders.ToDictionary(p => p.Key, p => p.Value);
      if (migrationPlaceholders != null)
        placeholders = placeholders.Concat(migrationPlaceholders).ToDictionary(p => p.Key, p => p.Value);
      using (TextReader reader = new SqlScriptPreprocessor(migrateScriptFilePath, placeholders))
        ExecuteBatch(transaction, new InstructionList(reader));
    }

    public void ExecuteBatch(ITransaction transaction, InstructionList instructions)
    {
      ISQLDatabase database = ServiceRegistration.Get<ISQLDatabase>(false);
      foreach (string instr in instructions)
      {
        if (instr.Contains(IGNORE_TOKEN))
          continue;

        using (IDbCommand cmd = transaction.CreateCommand())
        {
          string sql = instr;
          AppendStorageClause(database, ref sql);
          cmd.CommandText = sql;
          cmd.ExecuteNonQuery();
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
