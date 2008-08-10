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

using System.Collections.Generic;

using MediaPortal.Core;

namespace MediaPortal.Core.PluginManager
{
  /// <summary>
  /// Interface to access the MediaPortal plugin manager. It exposes the globally available methods
  /// to be called from throughout the application.
  /// </summary>
  /// <remarks>
  /// The plugin manager is responsible for managing all installed plugins. It resolves plugin conflicts,
  /// defines the load order and manages the lifecycle of the plugins.
  /// </remarks>
	public interface IPluginManager : IStatus
  {
    /// <summary>
    /// Gets a single Plugin Item registered at the given location and name
    /// </summary>
    /// <returns>The Plugin Item instance.</returns>
    /// <remarks>
    /// If no instance of the plugin item exists it will be instantiated.
    /// </remarks>
    object GetPluginItem<T>(string location, string name);

    /// <summary>
    /// Gets a List of Plugin Items registered at the given location
    /// </summary>
    /// <returns>A List of Plugin Items.</returns>
    /// <remarks>
    /// If no instance of the plugin items exists they will be instantiated.
    /// </remarks>
    List<T> GetAllPluginItems<T>(string location);

    /// <summary>
    /// Starts the plugin manager. This will initialize internal structures, load the list
    /// of available plugins, initializes the plugins and handles the plugin's autostart.
    /// </summary>
    void Startup();
  }
}
