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

namespace MediaPortal.Services.DatabaseSchemaVersionManager
{
  /// <summary>
  /// Automatical database sub schema update script manager.
  /// </summary>
  public class DatabaseSubSchemaUpdater : IDatabaseSubSchemaUpdater
  {
    protected string _subSchemaName;
    protected IDictionary<string, UpdateOperation> _versionToUpdateOperation = new Dictionary<string, UpdateOperation>();

    protected class UpdateOperation
    {
      protected int _fromVersionMajor;
      protected int _fromVersionMinor;
      protected int _toVersionMajor;
      protected int _toVersionMinor;
      protected string _updateScriptFilePath;

      protected UpdateOperation(string updateScriptFilePath, int? fromVersionMajor, int? fromVersionMinor,
          int toVersionMajor, int toVersionMinor)
      {
        _updateScriptFilePath = updateScriptFilePath;
        _fromVersionMajor = fromVersionMajor;
        _fromVersionMinor = fromVersionMinor;
        _toVersionMajor = toVersionMajor;
        _toVersionMinor = toVersionMinor;
      }
    }

    public DatabaseSubSchemaUpdater(string subSchemaName)
    {
      _subSchemaName = subSchemaName;
    }

    public void AddUpdateScript(string updateScriptFilePath, int? fromVersionMajor, int? fromVersionMinor,
        int toVersionMajor, int toVersionMinor)
    {
      string version = VersionToString(fromVersionMajor, fromVersionMinor);
      _versionToUpdateOperation.Add(version, new UpdateOperation(updateScriptFilePath, fromVersionMajor, fromVersionMinor,
          toVersionMajor, toVersionMinor);
    }

    public bool Update(out int newVersionMajor, out int newVersionMinor)
    {
      newVersionMajor = 0;
      newVersionMinor = 0;
      IDatabase database = ServiceScope.Get<IDatabase>();
      UpdateOperation nextOperation;
      int curVersionMajor;
      int curVersionMinor;
      if (database.GetSubSchemaVersion(_subSchemaName, out curVersionMajor, out curVersionMinor))
        nextOperation = GetUpdateOperation(curVersionMajor, curVersionMinor);
      else
        nextOperation = GetCreateOperation();
      if (nextOperation == null)
        return false;
      while (nextOperation != null)
      {
        // Avoid busy loops on wrong input data
        if (nextOperation.ToVersionMajor < curVersionMajor)
          return false;
        if (nextOperation.ToVersionMajor == curVersionMajor && nextOperation.ToVersionMinor < curVersionMinor)
          return false;
        curVersionMajor = nextOperation.ToVersionMajor;
        curVersionMinor = nextOperation.ToVersionMinor;
        nextOperation = GetUpdateOperation(curVersionMajor, curVersionMinor);
      }
      return true;
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
      if (_versionToUpdateOperation.TryGetValue(curMajor.ToString() + ".*", out result))
        return result;
      else if (_versionToUpdateOperation.TryGetValue(curMajor.ToString() + "." + curMinor.ToString(), out result))
        return result;
      else
        return null;
    }

    protected string VersionToString(int? versionMajor, int? versionMinor)
    {
      if (!versionMajor.HasValue)
        return string.Empty;
      else if (!versionMinor.HasValue)
        return versionMajor.ToString() + ".*";
      else
        return versionMajor.ToString() + versionMinor.ToString();
    }
  }
}
