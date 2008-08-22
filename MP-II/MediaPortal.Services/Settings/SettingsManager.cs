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
using MediaPortal.Core;
using MediaPortal.Core.PathManager;
using MediaPortal.Core.Settings;


namespace MediaPortal.Services.Settings
{
  /// <summary>
  /// Main Config Service.
  /// </summary>
  public class SettingsManager : ISettingsManager
  {

    #region Public Methods

    /// <summary>
    /// Retrieves an object's public properties from an Xml file 
    /// </summary>
    /// <param name="settingsObject">Object's instance</param>
    public void Load(object settingsObject)
    {
      string fileName = GetFilename(settingsObject);
      SettingParser parser = new SettingParser(settingsObject, GetGlobalFilename(fileName), GetUserFilename(fileName));
      parser.Deserialize();
    }

    /// <summary>
    /// Stores an object's public properties to an Xml file 
    /// </summary>
    /// <param name="settingsObject">Object's instance</param>
    public void Save(object settingsObject)
    {
      string fileName = GetFilename(settingsObject);
      SettingParser parser = new SettingParser(settingsObject, GetGlobalFilename(fileName), GetUserFilename(fileName));
      parser.Serialize();
    }

    #endregion

    #region Private Methods

    /// <summary>
    /// Returns a filename based on an setting class name
    /// additionnaly appends an extension to the filename
    /// if the settings class implements INamesSettings interface
    /// see doc for more infos on INamedSettings
    /// </summary>
    /// <param name="obj">settings instance to name</param>
    /// <returns></returns>
    private string GetFilename(object obj)
    {
      string fileName;
      INamedSettings namedSettings = obj as INamedSettings;
      if (namedSettings != null)
      {
        fileName = obj + "." + namedSettings.Name + ".xml";
      }
      else
      {
        fileName = obj + ".xml";
      }
      return fileName;
    }

    /// <summary>
    /// Returns full filename including config path and user subdir.
    /// </summary>
    /// <param name="fileName">Filename, can be retrieved with GetFileName(object).</param>
    /// <returns></returns>
    private string GetUserFilename(string filename)
    {
      string fullUserFileName = String.Format(@"<CONFIG>\{0}\{1}", Environment.UserName, filename);
      return ServiceScope.Get<IPathManager>().GetPath(fullUserFileName);
    }

    /// <summary>
    /// Returns full filename including config path.
    /// </summary>
    /// <param name="fileName">Filename, can be retrieved with GetFileName(object).</param>
    /// <returns></returns>
    private string GetGlobalFilename(string filename)
    {
      string fullFileName = String.Format(@"<CONFIG>\{0}", filename);
      return ServiceScope.Get<IPathManager>().GetPath(fullFileName);
    }

    #endregion

  }
}
