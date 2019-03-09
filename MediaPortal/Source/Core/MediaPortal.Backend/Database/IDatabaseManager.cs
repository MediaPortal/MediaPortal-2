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

using System.Collections.Generic;
using MediaPortal.Utilities.DB;

namespace MediaPortal.Backend.Database
{
  /// <summary>
  /// Provides database schema management functions. The guidelines for database setup and access must be observed.
  /// </summary>
  /// <remarks>
  /// The batch update methods provide a replacement function for datatypes. Datatype placeholders in the scripts in the form
  /// %DATATYPE% will be replaced by the correct database datatypes of the current database.
  /// For a list of replacements which will be applied to batch scripts, see the <see cref="SqlScriptPreprocessor"/> class.
  /// </remarks>
  public interface IDatabaseManager
  {
    /// <summary>
    /// Returns the table name of a one-collumn, one-row dummy table.
    /// </summary>
    /// <remarks>
    /// The dummy table can be used in queries, where no natural table exists, for example for queries to the database
    /// time or for queries of sequence values. The dummy table works exactly like the Oracle "DUAL" table.
    /// </remarks>
    string DummyTableName { get; }

    /// <summary>
    /// Returns whether a database upgrade is currently in progress.
    /// </summary>
    /// <remarks>
    /// When a database upgrade is in progress client queries should be avoided because data integrity is not garantued.
    /// </remarks>
    bool UpgradeInProgress { get; }

    /// <summary>
    /// Starts the database manager. This must be done after the database service is available (i.e. after the database plugin
    /// was started).
    /// </summary>
    void Startup();

    /// <summary>
    /// Gets a list of all named sub schemas which are currently deployed in the database.
    /// </summary>
    /// <returns>Collection of sub schema names.</returns>
    ICollection<string> GetDatabaseSubSchemas();

    /// <summary>
    /// Gets the managed version number for the sub module's database schema which was created previously by method
    /// <see cref="UpdateSubSchema"/> for the <paramref name="subSchemaName"/>'s ID.
    /// </summary>
    /// <param name="subSchemaName">Identificator for the sub schema whose version is requested.</param>
    /// <param name="versionMajor">Current major version number of the sub schema of the given
    /// <paramref name="subSchemaName"/>.</param>
    /// <param name="versionMinor">Current minor version number of the sub schema of the given
    /// <paramref name="subSchemaName"/>.</param>
    /// <returns><c>true</c>, if the sub schema with the given <paramref name="subSchemaName"/> has a version entry in
    /// this database, i.e. if it ran a schema setup script before.</returns>
    bool GetSubSchemaVersion(string subSchemaName, out int versionMajor, out int versionMinor);

    /// <summary>
    /// Creates or updates the sub schema of the given <paramref name="subSchemaName"/> and sets its major and minor version
    /// number.
    /// The caller should first call <see cref="GetSubSchemaVersion"/> and decide which creation or update script to use.
    /// </summary>
    /// <param name="subSchemaName">Identificator for the sub schema which will be created/updated.</param>
    /// <param name="currentVersionMajor">Current major version number of the schema to be updated by the update script.
    /// If the sub schema isn't present yet, <c>null</c> should be used for this parameter.</param>
    /// <param name="currentVersionMinor">Current minor version number of the schema to be updated by the update script.
    /// If the sub schema isn't present yet, <c>null</c> should be used for this parameter.</param>
    /// <param name="updateScriptFilePath">Path to a file containing the script to create or update the sub schema with the given
    /// <paramref name="subSchemaName"/>
    /// to its current version [<paramref name="newVersionMajor"/>].[<paramref name="newVersionMinor"/>].</param>
    /// <param name="newVersionMajor">Major version number of the new schema.</param>
    /// <param name="newVersionMinor">Minor version number of the new schema.</param>
    /// <exception cref="ArgumentException">If the specified <paramref name="currentVersionMajor"/> and
    /// <paramref name="currentVersionMinor"/> don't match the current sub schema's version.</exception>
    /// <returns><c>true</c>, if the sub schema of the given <paramref name="subSchemaName"/> existed in the given
    /// version and if it could correctly be updated, else <c>false</c>.</returns>
    /// <exception cref="Exception">All exceptions in lower DB layers, which are caused by problems in the
    /// DB connection or malformed scripts, will be re-thrown by this method.</exception>
    bool UpdateSubSchema(string subSchemaName, int? currentVersionMajor, int? currentVersionMinor,
        string updateScriptFilePath, int newVersionMajor, int newVersionMinor);

    /// <summary>
    /// Deletes the sub schema of the given <paramref name="subSchemaName"/>.
    /// The caller should first call <see cref="GetSubSchemaVersion"/> and decide which deletion script to use.
    /// </summary>
    /// <param name="subSchemaName">Identificator for the sub schema which will be deleted.</param>
    /// <param name="currentVersionMajor">Current major version number of the schema to be deleted by the delete script.</param>
    /// <param name="currentVersionMinor">Current minor version number of the schema to be deleted by the delete script.</param>
    /// <param name="deleteScriptFilePath">Path to a file containing the script to delete the sub schema with the given
    /// <paramref name="subSchemaName"/>.</param>
    /// <exception cref="ArgumentException">If the specified <paramref name="currentVersionMajor"/> and
    /// <paramref name="currentVersionMinor"/> don't match the current sub schema's version.</exception>
    /// <exception cref="Exception">All exceptions in lower DB layers, which are caused by problems in the
    /// DB connection or malformed scripts, will be re-thrown by this method.</exception>
    void DeleteSubSchema(string subSchemaName, int currentVersionMajor, int currentVersionMinor, string deleteScriptFilePath);

    /// <summary>
    /// Migrates the sub schema data of the given <paramref name="subSchemaName"/>.
    /// </summary>
    /// <param name="transaction">Transaction to use for migrating data.</param>
    /// <param name="dataOwner">Owner of the data which will be migrated.</param>
    /// <param name="migrateScriptFilePath">Path to a file containing the script to migrate the data of the given
    /// <paramref name="dataOwner"/>.</param>
    /// <param name="migrationPlaceholderTables">Script placeholders that must be replaced during script execution from a list of tables.</param>
    /// <exception cref="Exception">All exceptions in lower DB layers, which are caused by problems in the
    /// DB connection or malformed scripts, will be re-thrown by this method.</exception>
    void MigrateData(ITransaction transaction, string dataOwner, string migrateScriptFilePath, IDictionary<string, IList<string>> migrationPlaceholderTables);

    /// <summary>
    /// Executes an SQL script provided by the given <paramref name="instructions"/>.
    /// </summary>
    /// <param name="transaction">Transaction to use for executing the batch.</param>
    /// <param name="instructions">Instructions to execute in batch mode.</param>
    void ExecuteBatch(ITransaction transaction, InstructionList instructions, IDictionary<string, IList<string>> migrationPlaceholderTables = null);

    /// <summary>
    /// Executes an upgrade of the database if needed.
    /// Normally this involves the following steps:
    /// 1. Make a backup of the database
    /// 2. Backup all tables by renaming them so data is available for migration
    /// </summary>
    /// <returns><c>true</c>, if the database was updated, else <c>false</c>.</returns>
    bool UpgradeDatabase();

    /// <summary>
    /// Executes a migration of the database data if possible. This requires the old data to be present in the form of renamed tables. 
    /// Normally this involves the following steps:
    /// 1. Execute update scripts that will copy data from the old tables to the new tables
    /// 2. Drop all backup tables
    /// </summary>
    /// <returns><c>true</c>, if the data was migrated, else <c>false</c>.</returns>
    bool MigrateDatabaseData();
  }
}
