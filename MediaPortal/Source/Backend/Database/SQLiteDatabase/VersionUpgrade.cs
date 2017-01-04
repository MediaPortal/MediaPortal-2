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
using System.IO;
using System.Xml.Serialization;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Common.PathManager;
using MediaPortal.Common.Settings;

namespace MediaPortal.Database.SQLite
{
  public class VersionInfo
  {
    public string CompatibleVersion { get; set; }
  }

  class VersionUpgrade
  {
    const string SUPPORTED_VERSION = "2.1";

    private readonly SQLiteSettings _settings;
    private readonly string _dataDirectory;
    private XmlSerializer _serializer;
    private string _databaseFile;
    private string _versionInfoFile;

    public VersionUpgrade()
    {
      _serializer = new XmlSerializer(typeof(VersionInfo));
      _settings = ServiceRegistration.Get<ISettingsManager>().Load<SQLiteSettings>();
      var pathManager = ServiceRegistration.Get<IPathManager>();
      _dataDirectory = pathManager.GetPath("<DATABASE>");
      _databaseFile = Path.Combine(_dataDirectory, _settings.DatabaseFileName);
      _versionInfoFile = Path.ChangeExtension(_databaseFile, ".version.xml");
    }

    public bool Upgrade()
    {
      try
      {
        // If no DB file exists, it will be created after this check, so prepare the compatibility info here.
        if (!File.Exists(_databaseFile))
        {
          // Write compatibility version
          SetCompatibleVersion(new VersionInfo { CompatibleVersion = SUPPORTED_VERSION });
          return true;
        }

        // If there is a database file, but no version info, the DB is not expected to be 2.1 compatible and has to be upgraded
        if (File.Exists(_databaseFile) && !File.Exists(_versionInfoFile))
        {
          return DoUpgrade();
        }

        var versionInfo = GetCompatibleVersion();
        if (versionInfo.CompatibleVersion != SUPPORTED_VERSION)
          return DoUpgrade();
      }
      catch (Exception ex)
      {
        ServiceRegistration.Get<ILogger>().Warn("SQLiteDatabase: Error while upgrading database", ex);
        return false;
      }
      return true;
    }

    private bool DoUpgrade()
    {
      // Rename the existing file
      var backupName = Path.ChangeExtension(_databaseFile, ".2.0.bak");
      if (File.Exists(backupName))
      {
        for (int i = 1; i <= 10; i++)
        {
          backupName = Path.ChangeExtension(_databaseFile, ".2.0 [" + i + "].bak");
          if (!File.Exists(backupName))
            break;
        }
      }
      File.Move(_databaseFile, backupName);

      // Write compatibility version
      SetCompatibleVersion(new VersionInfo { CompatibleVersion = SUPPORTED_VERSION });
      return true;
    }

    private VersionInfo GetCompatibleVersion()
    {
      using (var fileStream = new FileStream(_versionInfoFile, FileMode.Open))
      {
        return (VersionInfo)_serializer.Deserialize(fileStream) ?? new VersionInfo();
      }
    }

    private void SetCompatibleVersion(VersionInfo versionInfo)
    {
      using (var fileStream = new FileStream(_versionInfoFile, FileMode.Create))
      {
        _serializer.Serialize(fileStream, versionInfo);
      }
    }
  }
}
