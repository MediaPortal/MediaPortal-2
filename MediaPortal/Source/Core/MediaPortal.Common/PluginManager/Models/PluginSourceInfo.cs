#region Copyright (C) 2007-2014 Team MediaPortal

/*
    Copyright (C) 2007-2014 Team MediaPortal
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

using System.IO;

namespace MediaPortal.Common.PluginManager.Models
{
  /// <summary>
  /// Plugin metadata class responsible for storing information on where the metadata for the
  /// associated plugin was collected. Currently this will always be a path to a folder in
  /// the MP2 plugin directory.
  /// </summary>
  public class PluginSourceInfo
  {
    #region Source Details
    /// <summary>
    /// Returns the plugin's directory location (if installed locally).
    /// </summary>
    public string PluginPath { get; internal set; }
    #endregion

    #region Ctor
    public PluginSourceInfo( string pluginPath )
    {
      PluginPath = pluginPath;
    }
    #endregion

    #region Path Helpers (GetAbsolutePath)
    /// <summary>
    /// Returns an absolute path from the relative path expression which is based on the plugin
    /// directory.
    /// </summary>
    /// <param name="relativePath">File path relative to the plugin directory.</param>
    /// <returns>Absolute file path of the specified relative path.</returns>
	  public string GetAbsolutePath( string relativePath )
    {
      return PluginPath == null || relativePath == null ? null : Path.Combine( PluginPath, relativePath );
    }
    #endregion
  }
}
