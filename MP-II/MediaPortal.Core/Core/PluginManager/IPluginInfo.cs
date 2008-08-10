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

namespace MediaPortal.Core.PluginManager
{
  /// <summary>
  /// Descriptor interface for a plugin. Exposes the plugin's state and provides access to all
  /// public plugin properties.
  /// </summary>
  public interface IPluginInfo
  {
    #region Properties

    /// <summary>
    /// Returns the plugin's name.
    /// </summary>
    string Name
    {
      get;
    }

    /// <summary>
    /// Returns the plugin's version.
    /// </summary>
    Version Version
    {
      get;
    }

    /// <summary>
    /// Returns the plugin directory path in the file system.
    /// </summary>
    DirectoryInfo PluginPath
    {
      get;
    }

    /// <summary>
    /// Returns the state of this plugin.
    /// </summary>
    /// <seealso cref="PluginState"/>
    PluginState State
    { 
      get; 
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// Creates an instance of a plugin item
    /// </summary>
    /// <param name="className">The name of the class to be instanciated</param>
    /// <returns>the instance as an object</returns>
    object CreateObject(string className);

    #endregion
  }
}
