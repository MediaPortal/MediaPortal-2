#region Copyright (C) 2007-2008 Team MediaPortal

/*
    Copyright (C) 2007-2008 Team MediaPortal
    http://www.team-mediaportal.com
 
    This file is part of MediaPortal II

    MediaPortal II is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    MediaPortal II is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with MediaPortal II.  If not, see <http://www.gnu.org/licenses/>.
*/

#endregion

using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using MediaPortal.Core;

namespace MediaPortal.Database
{
  /// <summary>
  /// Automatic database update script manager for a sub schema.
  /// </summary>
  /// <remarks>
  /// <para>
  /// Given a collectoin of update scripts from several old subschema versions to a newer version, this class
  /// is able to automatically execute those update scripts which are needed to bring the active database subschema
  /// to the current version.
  /// The update script collection can either be added directly by calling <see cref="AddUpdateScript"/> or
  /// by calling <see cref="AddDirectory"/>, this class can search update scripts itself.
  /// </para>
  /// </remarks>
  public class DatabaseSubSchemaUpdater
  {
    protected string _subSchemaName;
    protected IDictionary<string, UpdateOperation> _versionToUpdateOperation = new Dictionary<string, UpdateOperation>();

    protected class UpdateOperation
    {
      protected int? _fromVersionMajor;
      protected int? _fromVersionMinor;
      protected int _toVersionMajor;
      protected int _toVersionMinor;
      protected string _updateScriptFilePath;

      public UpdateOperation(string updateScriptFilePath, int? fromVersionMajor, int? fromVersionMinor,
          int toVersionMajor, int toVersionMinor)
      {
        _updateScriptFilePath = updateScriptFilePath;
        _fromVersionMajor = fromVersionMajor;
        _fromVersionMinor = fromVersionMinor;
        _toVersionMajor = toVersionMajor;
        _toVersionMinor = toVersionMinor;
      }

      public int? FromVersionMajor
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

      public int ToVersionMinor
      {
        get { return _toVersionMinor; }
      }

      public string UpdateScriptFilePath
      {
        get { return _updateScriptFilePath; }
      }
    }

    /// <summary>
    /// Creates a new database subschema updater for the subschema with the given <paramref name="subSchemaName"/>.
    /// </summary>
    public DatabaseSubSchemaUpdater(string subSchemaName)
    {
      _subSchemaName = subSchemaName;
    }

    /// <summary>
    /// Manually adds an update script to the collection of available scripts.
    /// </summary>
    public void AddUpdateScript(string updateScriptFilePath, int? fromVersionMajor, int? fromVersionMinor,
        int toVersionMajor, int toVersionMinor)
    {
      string version = VersionToString(fromVersionMajor, fromVersionMinor);
      _versionToUpdateOperation.Add(version, new UpdateOperation(updateScriptFilePath, fromVersionMajor, fromVersionMinor,
          toVersionMajor, toVersionMinor));
    }

    /// <summary>
    /// Adds all subschema update scripts from the given update script directory.
    /// </summary>
    /// <remarks>
    /// The update files need to have filenames of the format subschemaname-1.0-1.1.sql.
    /// A script which updates all minor versions of a given major version can have the format
    /// subschemaname-1._-2.0.sql. The create script should be of the format subschemaname-create-1.0.sql.
    /// </remarks>
    public void AddDirectory(string updateScriptDirectoryPath)
    {
      string searchPatternOs = _subSchemaName + "-*-*.*.sql";
      Regex rxCreate = new Regex("^" + Regex.Escape(_subSchemaName) + "-create-([0-9]).([0-9]).sql$", RegexOptions.IgnoreCase);
      Regex rxUpdate = new Regex("^" + Regex.Escape(_subSchemaName) + "-(([0-9]).(([0-9])|_))-([0-9]).([0-9]).sql$", RegexOptions.IgnoreCase);
      foreach (string updateScriptPath in Directory.GetFiles(Path.Combine(updateScriptDirectoryPath, searchPatternOs)))
      {
        Match match = rxCreate.Match(updateScriptPath);
        if (match.Success)
        {
          string toVersionMajorStr = match.Groups[1].ToString();
          string toVersionMinorStr = match.Groups[2].ToString();
          int toVersionMajor;
          if (!int.TryParse(toVersionMajorStr, out toVersionMajor))
            continue;
          int toVersionMinor;
          if (!int.TryParse(toVersionMinorStr, out toVersionMinor))
            continue;
          AddUpdateScript(updateScriptPath, null, null, toVersionMajor, toVersionMinor);
        }
        else
        {
          match = rxUpdate.Match(updateScriptPath);
          if (match.Success)
          {
            string fromVersionMajorStr = match.Groups[2].ToString();
            string fromVersionMinorStr = match.Groups[3].ToString();
            string toVersionMajorStr = match.Groups[3].ToString();
            string toVersionMinorStr = match.Groups[4].ToString();
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
            int toVersionMinor;
            if (!int.TryParse(toVersionMinorStr, out toVersionMinor))
              continue;
            AddUpdateScript(updateScriptPath, fromVersionMajor, fromVersionMinor, toVersionMajor, toVersionMinor);
          }
        }
      }
    }

    /// <summary>
    /// Given the collection of available update scripts (which were added before either by explicitly calling
    /// <see cref="AddUpdateScript"/> or by calling <see cref="AddDirectory"/>), this methods updates the database
    /// to the current version.
    /// </summary>
    /// <remarks>
    /// The update is an iterative process. In each step, an update script will be applied which matches the current
    /// subschema version.
    /// If the subschema isn't present in the DB before this method gets called (i.e. its current version isn't present),
    /// the first step will be to apply the "Create" script (i.e. the script with an empty "from" version).
    /// If the subschema currently is at version A.B, potential update scripts are those with a
    /// "from" version of "A.*" or "A.B". "A.*" will be preferrably used. The DB update will take place with the script
    /// which was found (if any) and the next update step starts. If no more matching update script is found,
    /// the update terminates.
    /// </remarks>
    /// <param name="newVersionMajor">Returns the subschema major version after the execution of this method.</param>
    /// <param name="newVersionMinor">Returns the subschema minor version after the execution of this method.</param>
    /// <returns><c>true</c>, if the requested ,
    /// else <c>false</c>.</returns>
    public bool Update(out int newVersionMajor, out int newVersionMinor)
    {
      IDatabaseManager databaseManager = ServiceScope.Get<IDatabaseManager>();
      UpdateOperation nextOperation;
      int curVersionMajor = 0;
      int curVersionMinor = 0;
      try
      {
        if (databaseManager.GetSubSchemaVersion(_subSchemaName, out curVersionMajor, out curVersionMinor))
          nextOperation = GetUpdateOperation(curVersionMajor, curVersionMinor);
        else
        {
          nextOperation = GetCreateOperation();
          if (nextOperation == null)
            return false; // No schema could be created
        }
        while (nextOperation != null)
        {
          // Avoid busy loops on wrong input data
          if (nextOperation.ToVersionMajor < curVersionMajor ||
              (nextOperation.ToVersionMajor == curVersionMajor && nextOperation.ToVersionMinor < curVersionMinor))
            throw new ArgumentException(string.Format("Update script '{0}' seems to decrease the schema version",
                nextOperation.UpdateScriptFilePath));
          string updateScript;
          using (TextReader reader = new StreamReader(nextOperation.UpdateScriptFilePath)) // We should define an encoding here
            updateScript = reader.ReadToEnd();
          databaseManager.UpdateSubSchema(_subSchemaName, curVersionMajor, curVersionMinor,
              updateScript, nextOperation.ToVersionMajor, nextOperation.ToVersionMinor);
          curVersionMajor = nextOperation.ToVersionMajor;
          curVersionMinor = nextOperation.ToVersionMinor;
          nextOperation = GetUpdateOperation(curVersionMajor, curVersionMinor);
        }
        return true;
      }
      finally
      {
        newVersionMajor = curVersionMajor;
        newVersionMinor = curVersionMinor;
      }
    }

    protected UpdateOperation GetCreateOperation()
    {
      UpdateOperation result;
      if (_versionToUpdateOperation.TryGetValue(string.Empty, out result))
        return result;
      return null;
    }

    protected UpdateOperation GetUpdateOperation(int fromVersionMajor, int fromVersionMinor)
    {
      UpdateOperation result;
      if (_versionToUpdateOperation.TryGetValue(VersionToString(fromVersionMajor, null), out result))
        return result;
      else if (_versionToUpdateOperation.TryGetValue(VersionToString(fromVersionMajor, fromVersionMinor), out result))
        return result;
      else
        return null;
    }

    protected string VersionToString(int? versionMajor, int? versionMinor)
    {
      if (!versionMajor.HasValue)
        return string.Empty;
      else if (!versionMinor.HasValue)
        return versionMajor + ".*";
      else
        return versionMajor + "." + versionMinor;
    }
  }
}
