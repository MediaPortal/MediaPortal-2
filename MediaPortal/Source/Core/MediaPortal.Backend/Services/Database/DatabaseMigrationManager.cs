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
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using MediaPortal.Backend.Database;
using MediaPortal.Common;
using MediaPortal.Common.Logging;

namespace MediaPortal.Backend.Services.Database
{
  /// <summary>
  /// Automatic database migration script manager.
  /// </summary>
  /// <remarks>
  /// <para>
  /// </para>
  public class DatabaseMigrationManager
  {
    #region Inner Classes

    public class MigrateOperation
    {
      protected int _fromVersionMajor;
      protected int? _fromVersionMinor;
      protected int _toVersionMajor;
      protected int? _toVersionMinor;
      protected string _migrateScriptFilePath;

      public MigrateOperation(string migrateScriptFilePath, int fromVersionMajor, int? fromVersionMinor,
          int toVersionMajor, int? toVersionMinor)
      {
        _migrateScriptFilePath = migrateScriptFilePath;
        _fromVersionMajor = fromVersionMajor;
        _fromVersionMinor = fromVersionMinor;
        _toVersionMajor = toVersionMajor;
        _toVersionMinor = toVersionMinor;
      }

      public int FromVersionMajor
      {
        get { return _fromVersionMajor; }
      }

      public int? FromVersionMinor
      {
        get { return _fromVersionMinor; }
      }

      public int ToVersionMajor
      {
        get { return _toVersionMajor; }
      }

      public int? ToVersionMinor
      {
        get { return _toVersionMinor; }
      }

      public string MigrateScriptFilePath
      {
        get { return _migrateScriptFilePath; }
      }
    }

    #endregion

    protected string _migrationOwnerName;
    protected string _migrationDefaultName;
    protected IDictionary<string, IList<string>> _defaultScriptPlaceholderTables;
    protected IList<MigrateOperation> _migrateOperations = new List<MigrateOperation>();
    protected IList<MigrateOperation> _defaultMigrateOperations = new List<MigrateOperation>();

    public string MigrationOwnerName => _migrationOwnerName;

    /// <summary>
    /// Creates a new database migrator with the given <paramref name="migrationOwnerName"/>.
    /// </summary>
    /// <param name="migrationOwnerName">Name of the object this class works on. This is the name which will be used
    /// as part of the file name search pattern for method <see cref="AddDirectory"/>.</param>
    public DatabaseMigrationManager(string migrationOwnerName, string migrationDefaultName = null, IDictionary<string, IList<string>> defaultScriptPlaceholderTables = null)
    {
      _migrationOwnerName = migrationOwnerName;
      _migrationDefaultName = migrationDefaultName;
      _defaultScriptPlaceholderTables = defaultScriptPlaceholderTables;
    }

    /// <summary>
    /// Gets the best fitting migration script using the following priority chain:
    /// 1. A script for the current version of database while still compatible with target version
    /// 2. A generic script compatible with current version of database while also compatible with target version
    /// 3. A default script for the current version of database while still compatible with target version
    /// 4. A default generic script compatible with current version of database while also compatible with target version
    /// </summary>
    protected MigrateOperation GetMigrateOperation(int fromVersionMajor, int fromVersionMinor, int toVersionMajor, int toVersionMinor)
    {
      //Try to find a specific script
      var ops = _migrateOperations.FirstOrDefault(o =>
        o.FromVersionMajor == fromVersionMajor && o.FromVersionMinor == fromVersionMinor &&
        ((o.ToVersionMajor == toVersionMajor && (!o.ToVersionMinor.HasValue || o.ToVersionMinor >= toVersionMinor)) ||
        o.ToVersionMajor > toVersionMajor));

      if (ops == null) //Try to find a generic script
        ops = _migrateOperations.FirstOrDefault(o =>
        o.FromVersionMajor == fromVersionMajor && (!o.FromVersionMinor.HasValue || o.FromVersionMinor == fromVersionMinor) &&
        ((o.ToVersionMajor == toVersionMajor && (!o.ToVersionMinor.HasValue || o.ToVersionMinor >= toVersionMinor)) ||
        o.ToVersionMajor > toVersionMajor));

      if(ops == null) //Try to find a specific default script
        ops = _defaultMigrateOperations.FirstOrDefault(o =>
          o.FromVersionMajor == fromVersionMajor && o.FromVersionMinor == fromVersionMinor &&
          ((o.ToVersionMajor == toVersionMajor && (!o.ToVersionMinor.HasValue || o.ToVersionMinor >= toVersionMinor)) ||
          o.ToVersionMajor > toVersionMajor));

      if (ops == null) //Try to find a generic default script
        ops = _defaultMigrateOperations.FirstOrDefault(o =>
          o.FromVersionMajor == fromVersionMajor && (!o.FromVersionMinor.HasValue || o.FromVersionMinor == fromVersionMinor) &&
          ((o.ToVersionMajor == toVersionMajor && (!o.ToVersionMinor.HasValue || o.ToVersionMinor >= toVersionMinor)) ||
          o.ToVersionMajor > toVersionMajor));

      return ops;
    }

    /// <summary>
    /// Manually adds a default migration script to the collection of available scripts.
    /// </summary>
    /// <param name="migrateScriptFilePath">File path to the migration script to add.</param>
    /// <param name="fromVersionMajor">Major version number the specified migration script will support.</param>
    /// <param name="fromVersionMinor">Minor version number the specified migration script will support.
    /// Should be <c>null</c> if the specified migration script can work on all minor version numbers
    /// under the given <paramref name="fromVersionMajor"/> version.</param>
    /// <param name="toVersionMajor">Major target version number the specified migration script will support.</param>
    /// <param name="toVersionMinor">Minor target version number the specified migration script will support.
    /// Should be <c>null</c> if the specified migration script can work on all minor target version numbers
    /// under the given <paramref name="toVersionMajor"/> version.</param>
    public void AddDefaultMigrateScript(string migrateScriptFilePath, int fromVersionMajor, int? fromVersionMinor, int toVersionMajor, int? toVersionMinor)
    {
      _defaultMigrateOperations.Add(new MigrateOperation(migrateScriptFilePath, fromVersionMajor, fromVersionMinor, toVersionMajor, toVersionMinor));
    }

    /// <summary>
    /// Manually adds a migration script to the collection of available scripts.
    /// </summary>
    /// <param name="migrateScriptFilePath">File path to the migration script to add.</param>
    /// <param name="fromVersionMajor">Major version number the specified migration script will support.</param>
    /// <param name="fromVersionMinor">Minor version number the specified migration script will support.
    /// Should be <c>null</c> if the specified migration script can work on all minor version numbers
    /// under the given <paramref name="fromVersionMajor"/> version.</param>
    /// <param name="toVersionMajor">Major target version number the specified migration script will support.</param>
    /// <param name="toVersionMinor">Minor target version number the specified migration script will support.
    /// Should be <c>null</c> if the specified migration script can work on all minor target version numbers
    /// under the given <paramref name="toVersionMajor"/> version.</param>
    public void AddMigrateScript(string migrateScriptFilePath, int fromVersionMajor, int? fromVersionMinor, int toVersionMajor, int? toVersionMinor)
    {
      _migrateOperations.Add(new MigrateOperation(migrateScriptFilePath, fromVersionMajor, fromVersionMinor, toVersionMajor, toVersionMinor));
    }

    /// <summary>
    /// Adds all migration scripts from the given script directory.
    /// </summary>
    /// <remarks>
    /// The script filenames must match the following scheme:
    /// <list type="table">  
    /// <listheader><term>Script type</term><description>filename format</description></listheader>  
    /// <item><term>Default</term><description>defaultname-migrate-1.0-1.1.sql or defaultname-migrate-1._-1._.sql</description></item>  
    /// <item><term>Migrate</term><description>objectname-migrate-1.0-1.1.sql or objectname-migrate-1._-1._.sql</description></item>  
    /// </list>
    /// </remarks>
    /// <param name="scriptDirectoryPath">Path of the directory which contains migration scripts.</param>
    public void AddDirectory(string scriptDirectoryPath)
    {
      string searchPatternOs = _migrationOwnerName + "-*-*.sql";
      List<string> scripts = new List<string>(Directory.GetFiles(scriptDirectoryPath, searchPatternOs));
      Regex rxMigrate = new Regex("^" + Regex.Escape(_migrationOwnerName) + "-migrate-([0-9])\\.(([0-9])|_)-([0-9])\\.(([0-9])|_)\\.sql$", RegexOptions.IgnoreCase);
      Regex rxDefaultMigrate = null;
      if (!string.IsNullOrEmpty(_migrationDefaultName))
      {
        searchPatternOs = _migrationDefaultName + "-*-*.sql";
        scripts.AddRange(Directory.GetFiles(scriptDirectoryPath, searchPatternOs));
        rxDefaultMigrate = new Regex("^" + Regex.Escape(_migrationDefaultName) + "-migrate-([0-9])\\.(([0-9])|_)-([0-9])\\.(([0-9])|_)\\.sql$", RegexOptions.IgnoreCase);
      }
      foreach (string scriptPath in scripts)
      {
        string scriptName = Path.GetFileName(scriptPath);
        Match match = rxMigrate.Match(scriptName);
        if (match.Success)
        {
          string fromVersionMajorStr = match.Groups[1].ToString();
          string fromVersionMinorStr = match.Groups[2].ToString();
          string toVersionMajorStr = match.Groups[4].ToString();
          string toVersionMinorStr = match.Groups[5].ToString();
          int fromVersionMajor;
          if (!int.TryParse(fromVersionMajorStr, out fromVersionMajor))
            continue;
          int? fromVersionMinor;
          int help;
          if (fromVersionMinorStr == "_")
            fromVersionMinor = null;
          else if (!int.TryParse(fromVersionMinorStr, out help))
            continue;
          else
            fromVersionMinor = help;
          int toVersionMajor;
          if (!int.TryParse(toVersionMajorStr, out toVersionMajor))
            continue;
          int? toVersionMinor;
          if (toVersionMinorStr == "_")
            toVersionMinor = null;
          else if (!int.TryParse(toVersionMinorStr, out help))
            continue;
          else
            toVersionMinor = help;
          AddMigrateScript(scriptPath, fromVersionMajor, fromVersionMinor, toVersionMajor, toVersionMinor);
        }
        else if (rxDefaultMigrate != null)
        {
          match = rxDefaultMigrate.Match(scriptName);
          if (match.Success)
          {
            string fromVersionMajorStr = match.Groups[1].ToString();
            string fromVersionMinorStr = match.Groups[2].ToString();
            string toVersionMajorStr = match.Groups[4].ToString();
            string toVersionMinorStr = match.Groups[5].ToString();
            int fromVersionMajor;
            if (!int.TryParse(fromVersionMajorStr, out fromVersionMajor))
              continue;
            int? fromVersionMinor;
            int help;
            if (fromVersionMinorStr == "_")
              fromVersionMinor = null;
            else if (!int.TryParse(fromVersionMinorStr, out help))
              continue;
            else
              fromVersionMinor = help;
            int toVersionMajor;
            if (!int.TryParse(toVersionMajorStr, out toVersionMajor))
              continue;
            int? toVersionMinor;
            if (toVersionMinorStr == "_")
              toVersionMinor = null;
            else if (!int.TryParse(toVersionMinorStr, out help))
              continue;
            else
              toVersionMinor = help;
            AddDefaultMigrateScript(scriptPath, fromVersionMajor, fromVersionMinor, toVersionMajor, toVersionMinor);
          }
        }
      }
    }

    /// <summary>
    /// Given the collection of available migration scripts (which were added before either by explicitly calling
    /// <see cref="AddMigrationScript"/> or by calling <see cref="AddDirectory"/>), this methods migrates the
    /// data of this class in the database.
    /// </summary>
    public bool MigrateData(ITransaction transaction, int curVersionMajor, int curVersionMinor, int targetVersionMajor, int targetVersionMinor)
    {
      IDatabaseManager databaseManager = ServiceRegistration.Get<IDatabaseManager>();
      MigrateOperation operation = GetMigrateOperation(curVersionMajor, curVersionMinor, targetVersionMajor, targetVersionMinor);
      if (operation == null)
        return false;
      databaseManager.MigrateData(transaction, _migrationOwnerName, operation.MigrateScriptFilePath, _defaultScriptPlaceholderTables);
      return true;
    }
  }
}
