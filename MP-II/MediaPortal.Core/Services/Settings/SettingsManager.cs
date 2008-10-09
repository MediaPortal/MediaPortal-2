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
using System.IO;
using MediaPortal.Core;
using MediaPortal.Core.PathManager;
using MediaPortal.Core.Settings;


namespace MediaPortal.Core.Services.Settings
{
  /// <summary>
  /// Main Config Service.
  /// </summary>
  public class SettingsManager : ISettingsManager
  {

    #region Public Methods

    public SettingsType Load<SettingsType>() where SettingsType : class
    {
      return (SettingsType) Load(typeof(SettingsType));
    }

    public object Load(Type settingsType)
    {
      // TODO: Caching of settings objects
      SettingParser parser = new SettingParser(GetGlobalFilePath(settingsType), GetUserFilePath(settingsType));
      return parser.Deserialize(settingsType);
    }

    public void Save<SettingsType>(SettingsType settingsObject) where SettingsType: class
    {
      if (settingsObject == null)
        throw new ArgumentNullException("settingsObject");
      SettingParser parser = new SettingParser(
          GetGlobalFilePath(settingsObject.GetType()),
          GetUserFilePath(settingsObject.GetType()));
      parser.Serialize(settingsObject);
    }

    public void RemoveAllSettingsData(bool user, bool global)
    {
      if (user)
      {
        string userPath = ServiceScope.Get<IPathManager>().GetPath(string.Format(@"<CONFIG>\{0}", Environment.UserName));
        DirectoryInfo userConfigDirectory = new DirectoryInfo(userPath);
        userConfigDirectory.Delete(true);
      }
      if (global)
      {
        string globalPath = ServiceScope.Get<IPathManager>().GetPath("<CONFIG>");
        DirectoryInfo globalConfigDirectory = new DirectoryInfo(globalPath);
        globalConfigDirectory.Delete(true);
      }
    }

    #endregion

    #region Private Methods

    /// <summary>
    /// Returns the full file path for a user setting object of the specified
    /// <paramref name="settingType"/>.
    /// </summary>
    /// <param name="settingType">Type of the settings class to map to a filename.</param>
    /// <returns>File name without path of a file which will store the setting instance of the
    /// specified <paramref name="settingType"/>.</returns>
    protected static string GetUserFilePath(Type settingType)
    {
      string fullUserFileName = String.Format(@"<CONFIG>\{0}\{1}", Environment.UserName, settingType.FullName + ".xml");
      return ServiceScope.Get<IPathManager>().GetPath(fullUserFileName);
    }

    /// <summary>
    /// Returns the full file path for a global setting object of the specified
    /// <paramref name="settingType"/>.
    /// </summary>
    /// <param name="settingType">Type of the settings class to map to a filename.</param>
    /// <returns>File name without path of a file which will store the setting instance of the
    /// specified <paramref name="settingType"/>.</returns>
    protected static string GetGlobalFilePath(Type settingType)
    {
      string fullFileName = String.Format(@"<CONFIG>\{0}", settingType.FullName + ".xml");
      return ServiceScope.Get<IPathManager>().GetPath(fullFileName);
    }

    #endregion

  }
}
