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
using MediaPortal.Core.Settings;


namespace MediaPortal.Configuration
{

  /// <summary>
  /// SettingFile groups all configuration items which are linked to the same settingsclass,
  /// and lets them share one instance of that class.
  /// </summary>
  internal class SettingFile : IEquatable<SettingFile>
  {

    #region Variables

    /// <summary>
    /// Shared instance of the mutual settingsclass.
    /// </summary>
    private object _settingObject;
    /// <summary>
    /// All nodes linked to the same settingsclass.
    /// </summary>
    private IList<IConfigurationNode> _linkedNodes;

    #endregion

    #region Properties

    /// <summary>
    /// Gets the shared instance of he mutual settingsclass.
    /// </summary>
    public object SettingObject
    {
      get { return _settingObject; }
    }

    /// <summary>
    /// Gets all instances of IConfigurationNode which are linked to the same settingsclass.
    /// </summary>
    public IList<IConfigurationNode> LinkedNodes
    {
      get { return _linkedNodes; }
    }

    #endregion

    #region Constructors

    /// <summary>
    /// Initializes a new instance of SettingFile.
    /// </summary>
    /// <param name="settingObject"></param>
    public SettingFile(object settingObject)
    {
      _settingObject = settingObject;
      _linkedNodes = new List<IConfigurationNode>();
    }

    /// <summary>
    /// Initializes a new instance of SettingFile.
    /// </summary>
    /// <exception cref="ArgumentNullException">
    /// An ArgumentNullException is thrown when the linkedNodes parameter is null.
    /// </exception>
    /// <param name="settingObject"></param>
    /// <param name="linkedNodes"></param>
    public SettingFile(object settingObject, IList<IConfigurationNode> linkedNodes)
    {
      if (linkedNodes == null)
        throw new ArgumentNullException("The argument linkedNodes can't be null.");
      _settingObject = settingObject;
      _linkedNodes = linkedNodes;
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// Saves the file.
    /// </summary>
    public void Save()
    {
      lock (_linkedNodes)
      {
        foreach (IConfigurationNode node in _linkedNodes)
        {
          if (node.Setting is ConfigItem)
            ((ConfigItem)node.Setting).Save(_settingObject);
        }
      }
      if (_settingObject != null)
        ServiceScope.Get<ISettingsManager>().Save(_settingObject);
    }

    #endregion

    #region IEquatable<SettingFile> Members

    /// <summary>
    /// Determines whether the specified SettingFile object is equal to the current SettingFile object.
    /// </summary>
    /// <param name="other"></param>
    /// <returns></returns>
    public bool Equals(SettingFile other)
    {
      if (((other.SettingObject == null && _settingObject == null) || other._settingObject + "" == _settingObject + "")
          && other._linkedNodes.Count == _linkedNodes.Count)
      {
        for (int i = 0; i < _linkedNodes.Count; i++)
        {
          if (!_linkedNodes[i].Equals(other._linkedNodes[i]))
            return false;
        }
        return true;
      }
      return false;
    }

    #endregion

  }
}
