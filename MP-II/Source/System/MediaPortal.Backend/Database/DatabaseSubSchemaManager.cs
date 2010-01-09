#region Copyright (C) 2007-2010 Team MediaPortal

/*
    Copyright (C) 2007-2010 Team MediaPortal
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
using MediaPortal.Core.Logging;

namespace MediaPortal.Backend.Database
{
  /// <summary>
  /// Automatic database update script manager for a sub schema.
  /// </summary>
  /// <remarks>
  /// <para>
  /// </para>
  /// Database schemas will evolve during time. Schema versions will be updated and update scripts must be executed on
  /// the database to bring it to the current version. Users might have systems with several old versions running.
  /// Developers using this class can support updates of any old version or only updates of a range of old versions by
  /// providing the appropriate update scripts to an instance of this class.
  /// <para>
  /// Given a collection of update scripts from several old subschema versions to a newer version, this class
  /// is able to automatically execute those update scripts which are needed to bring the active database subschema
  /// to the current version.
  /// </para>
  /// <para>
  /// The update script collection can either be added directly by calling <see cref="AddUpdateScript"/> or
  /// by calling <see cref="AddDirectory"/>, this class can search update scripts itself. For each script, there are
  /// metadata necessary: For the create script, its target schema version needs to be known.
  /// For each update script, its source version and its target version needs to be known. And for deletion scripts, their
  /// source versions are necessary. By those information, this class is able to produce a directed graph in form of a tree,
  /// which provides for multiple (old) source database schema versions one or more database scripts producing a newer
  /// version. Those scripts can then be executed in a sequence and will finally produce the (most recent) current database
  /// version.
  /// </para>
  /// <para>
  /// There are two ways of writing create and update scripts:
  /// The first (and recommended) way is to provide a "chain" of update scripts which can run one after another; each of
  /// them updates the database schema to a newer schema, until the target schema version is reached. In this approach,
  /// for a new database schema version to support, only one single update script must be added to be able to update to
  /// that version. But during time, if the database schema evolves, it might not be able to update each old database schema
  /// version to the current version, or it might be necessary to do updates which are less efficient than having a single
  /// update script which skips updates for several versions.
  /// So it is also necessary to provide update scripts which directly contain the "delta" from old database schema versions to
  /// the current version and a create script which provides exactly the commands to produce the new database schema.
  /// This approach will probably produce the most qualitative update process because each script is written for exactly
  /// hat single update. But its quite work intensive because for each new database schema, scripts need to be rewritten
  /// to produce the new schema.
  /// It is also necessary to mix the two approaces.
  /// </para>
  /// <para>
  /// For both approaches, the scripts, which are made available, should focus the current version, to that any
  /// older schema should be updated. That means that each way through the directed graph of schema creation/updates must
  /// lead to a script which produces the target database version.
  /// </para>
  /// </remarks>
  public class DatabaseSubSchemaManager
  {
    #region Inner classes

    protected class CreateOperation
    {
      protected int _toVersionMajor;
      protected int _toVersionMinor;
      protected string _createScriptFilePath;

      public CreateOperation(string updateScriptFilePath,
          int toVersionMajor, int toVersionMinor)
      {
        _createScriptFilePath = updateScriptFilePath;
        _toVersionMajor = toVersionMajor;
        _toVersionMinor = toVersionMinor;
      }

      public int ToVersionMajor
      {
        get { return _toVersionMajor; }
      }

      public int ToVersionMinor
      {
        get { return _toVersionMinor; }
      }

      public string CreateScriptFilePath
      {
        get { return _createScriptFilePath; }
      }
    }

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

    protected class DeleteOperation
    {
      protected int _fromVersionMajor;
      protected int? _fromVersionMinor;
      protected string _deleteScriptFilePath;

      public DeleteOperation(string deleteScriptFilePath, int fromVersionMajor, int? fromVersionMinor)
      {
        _deleteScriptFilePath = deleteScriptFilePath;
        _fromVersionMajor = fromVersionMajor;
        _fromVersionMinor = fromVersionMinor;
      }

      public int FromVersionMajor
      {
        get { return _fromVersionMajor; }
      }

      public int? FromVersionMinor
      {
        get { return _fromVersionMinor; }
      }

      public string DeleteScriptFilePath
      {
        get { return _deleteScriptFilePath; }
      }
    }

    #endregion

    protected string _subSchemaName;
    protected CreateOperation _createOperation = null;
    protected IDictionary<string, UpdateOperation> _versionToUpdateOperation = new Dictionary<string, UpdateOperation>();
    protected IDictionary<string, DeleteOperation> _versionToDeleteOperation = new Dictionary<string, DeleteOperation>();

    /// <summary>
    /// Creates a new database subschema updater for the subschema with the given <paramref name="subSchemaName"/>.
    /// </summary>
    /// <param name="subSchemaName">Name of the subschema this class works on. This is the name which will be used
    /// as part of the file name search pattern for method <see cref="AddDirectory"/>.</param>
    public DatabaseSubSchemaManager(string subSchemaName)
    {
      _subSchemaName = subSchemaName;
    }

    protected CreateOperation GetCreateOperation()
    {
      return _createOperation;
    }

    protected UpdateOperation GetUpdateOperation(int fromVersionMajor, int fromVersionMinor)
    {
      UpdateOperation result;
      if (_versionToUpdateOperation.TryGetValue(WildcardVersionToString(fromVersionMajor, null), out result))
        return result;
      else if (_versionToUpdateOperation.TryGetValue(WildcardVersionToString(fromVersionMajor, fromVersionMinor), out result))
        return result;
      else
        return null;
    }

    protected DeleteOperation GetDeleteOperation(int fromVersionMajor, int fromVersionMinor)
    {
      DeleteOperation result;
      if (_versionToDeleteOperation.TryGetValue(WildcardVersionToString(fromVersionMajor, null), out result))
        return result;
      else if (_versionToDeleteOperation.TryGetValue(WildcardVersionToString(fromVersionMajor, fromVersionMinor), out result))
        return result;
      else
        return null;
    }

    protected string WildcardVersionToString(int versionMajor, int? versionMinor)
    {
      if (!versionMinor.HasValue)
        return versionMajor + ".*";
      else
        return versionMajor + "." + versionMinor.Value;
    }

    /// <summary>
    /// Manually sets the creation script for the subschema of this subschema manager.
    /// </summary>
    /// <param name="createScriptFilePath">File path to the create script to set.</param>
    /// <param name="toVersionMajor">Major target version number of the specified create script.</param>
    /// <param name="toVersionMinor">Minor target version number of the specified create script.</param>
    public void SetCreateScript(string createScriptFilePath, int toVersionMajor, int toVersionMinor)
    {
      _createOperation = new CreateOperation(createScriptFilePath, toVersionMajor, toVersionMinor);
    }

    /// <summary>
    /// Manually adds an update script to the collection of available scripts.
    /// </summary>
    /// <param name="updateScriptFilePath">File path to the update script to add.</param>
    /// <param name="fromVersionMajor">Major version number the specified script will update.</param>
    /// <param name="fromVersionMinor">Minor version number the specified script will update.
    /// Should be <c>null</c> if the script can work on all minor version numbers under the given
    /// <paramref name="fromVersionMajor"/> version.</param>
    /// <param name="toVersionMajor">Major target version number of the specified update script.</param>
    /// <param name="toVersionMinor">Minor target version number of the specified update script.</param>
    public void AddUpdateScript(string updateScriptFilePath, int fromVersionMajor, int? fromVersionMinor,
        int toVersionMajor, int toVersionMinor)
    {
      _versionToUpdateOperation.Add(WildcardVersionToString(fromVersionMajor, fromVersionMinor),
          new UpdateOperation(updateScriptFilePath, fromVersionMajor, fromVersionMinor,
              toVersionMajor, toVersionMinor));
    }

    /// <summary>
    /// Manually adds a sub schema deletion script to the collection of available scripts.
    /// </summary>
    /// <param name="deleteScriptFilePath">File path to the delete script to add.</param>
    /// <param name="fromVersionMajor">Major version number the specified script will delete.</param>
    /// <param name="fromVersionMinor">Minor version number the specified script will delete.
    /// Should be <c>null</c> if the specified deletion script can work on all minor version numbers
    /// under the given <paramref name="fromVersionMajor"/> version.</param>
    public void AddDeleteScript(string deleteScriptFilePath, int fromVersionMajor, int? fromVersionMinor)
    {
      _versionToDeleteOperation.Add(WildcardVersionToString(fromVersionMajor, fromVersionMinor),
          new DeleteOperation(deleteScriptFilePath, fromVersionMajor, fromVersionMinor));
    }

    /// <summary>
    /// Adds all subschema create-, update- and delete-scripts from the given script directory.
    /// </summary>
    /// <remarks>
    /// The script filenames must match the following scheme:
    /// <list type="table">  
    /// <listheader><term>Script type</term><description>filename format</description></listheader>  
    /// <item><term>Create</term><description>subschemaname-create-1.0.sql</description></item>  
    /// <item><term>Update</term><description>subschemaname-1.0-1.1.sql or subschemaname-1._-2.0.sql</description></item>  
    /// <item><term>Delete</term><description>subschemaname-1.0-delete.sql or subschemaname-1._-delete.sql</description></item>  
    /// </list>
    /// </remarks>
    /// <param name="scriptDirectoryPath">Path of the directory which contains sub schema creation-, update- and
    /// deletion-scripts.</param>
    public void AddDirectory(string scriptDirectoryPath)
    {
      string searchPatternOs = _subSchemaName + "-*-*.sql";
      Regex rxCreate = new Regex("^" + Regex.Escape(_subSchemaName) + "-create-([0-9])\\.([0-9])\\.sql$", RegexOptions.IgnoreCase);
      Regex rxUpdate = new Regex("^" + Regex.Escape(_subSchemaName) + "-([0-9])\\.(([0-9])|_)-([0-9])\\.([0-9])\\.sql$", RegexOptions.IgnoreCase);
      Regex rxDelete = new Regex("^" + Regex.Escape(_subSchemaName) + "-([0-9])\\.(([0-9])|_)-delete\\.sql$", RegexOptions.IgnoreCase);
      foreach (string scriptPath in Directory.GetFiles(scriptDirectoryPath, searchPatternOs))
      {
        string scriptName = Path.GetFileName(scriptPath);
        Match match = rxCreate.Match(scriptName);
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
          SetCreateScript(scriptPath, toVersionMajor, toVersionMinor);
        }
        else
        {
          match = rxUpdate.Match(scriptName);
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
            int toVersionMinor;
            if (!int.TryParse(toVersionMinorStr, out toVersionMinor))
              continue;
            AddUpdateScript(scriptPath, fromVersionMajor, fromVersionMinor, toVersionMajor, toVersionMinor);
          }
          else
          {
            match = rxDelete.Match(scriptName);
            if (match.Success)
            {
              string fromVersionMajorStr = match.Groups[1].ToString();
              string fromVersionMinorStr = match.Groups[2].ToString();
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
              AddDeleteScript(scriptPath, fromVersionMajor, fromVersionMinor);
            }
          }
        }
      }
    }

    /// <summary>
    /// Given the collection of available update scripts (which were added before either by explicitly calling
    /// <see cref="AddUpdateScript"/> or by calling <see cref="AddDirectory"/>), this methods updates the sub schema
    /// of this class in the database to the current version.
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
    public bool UpdateSubSchema(out int newVersionMajor, out int newVersionMinor)
    {
      IDatabaseManager databaseManager = ServiceScope.Get<IDatabaseManager>();
      int curVersionMajor = 0;
      int curVersionMinor = 0;
      try
      {
        // Create schema
        if (!databaseManager.GetSubSchemaVersion(_subSchemaName, out curVersionMajor, out curVersionMinor))
        {
          CreateOperation createOperation = GetCreateOperation();
          if (createOperation == null)
            return false; // No schema could be created
          databaseManager.UpdateSubSchema(_subSchemaName, null, null,
              createOperation.CreateScriptFilePath, createOperation.ToVersionMajor, createOperation.ToVersionMinor);
          curVersionMajor = createOperation.ToVersionMajor;
          curVersionMinor = createOperation.ToVersionMinor;
        }
        while (true)
        {
          UpdateOperation nextOperation = GetUpdateOperation(curVersionMajor, curVersionMinor);
          if (nextOperation == null)
            break;
          // Avoid busy loops on wrong input data
          if (nextOperation.ToVersionMajor < curVersionMajor ||
              (nextOperation.ToVersionMajor == curVersionMajor && nextOperation.ToVersionMinor < curVersionMinor))
            throw new ArgumentException(string.Format("Update script '{0}' seems to decrease the schema version",
                nextOperation.UpdateScriptFilePath));
          databaseManager.UpdateSubSchema(_subSchemaName, curVersionMajor, curVersionMinor,
              nextOperation.UpdateScriptFilePath, nextOperation.ToVersionMajor, nextOperation.ToVersionMinor);
          curVersionMajor = nextOperation.ToVersionMajor;
          curVersionMinor = nextOperation.ToVersionMinor;
        }
        ServiceScope.Get<ILogger>().Info("DatabaseSubSchemaManager: Subschema '{0}' present in version {1}.{2}",
            _subSchemaName, curVersionMajor, curVersionMinor);
        return true;
      }
      finally
      {
        newVersionMajor = curVersionMajor;
        newVersionMinor = curVersionMinor;
      }
    }

    /// <summary>
    /// Given the collection of available deletion scripts (which were added before either by explicitly calling
    /// <see cref="AddDeleteScript"/> or by calling <see cref="AddDirectory"/>), this methods deletes the sub schema
    /// of this class from the database.
    /// </summary>
    public bool DeleteSubSchema()
    {
      IDatabaseManager databaseManager = ServiceScope.Get<IDatabaseManager>();
      int curVersionMajor;
      int curVersionMinor;
      if (!databaseManager.GetSubSchemaVersion(_subSchemaName, out curVersionMajor, out curVersionMinor))
        return true;
      DeleteOperation operation = GetDeleteOperation(curVersionMajor, curVersionMinor);
      databaseManager.DeleteSubSchema(_subSchemaName, curVersionMajor, curVersionMinor, operation.DeleteScriptFilePath);
      return true;
    }
  }
}
