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
using System.Collections;
using System.Collections.Generic;

namespace MediaPortal.Core.PluginManager
{
  /// <summary>
  /// Interface for plug-in managers
  /// </summary>
  /// <remarks>
  /// A plug-in manager is responsible for enumerating, starting and stopping plugins</remarks>
  public interface IPluginManager
  {
    object GetPluginItem<T>(string location, string name);

    List<T> GetAllPluginItems<T>(string location);

    /// <summary>
    /// Gets an enumerable list of available plugins
    /// </summary>
    /// <returns>An <see cref="IEnumerable<IPlugin>"/> list.</returns>
    /// <remarks>A configuration program can use this list to present the user a list of available plugins that he can (de)activate.</remarks>
    IEnumerable<IPluginInfo> GetAvailablePlugins();

    /// <summary>
    /// Stops all plug-ins
    /// </summary>
    void StopAll();

    /// <summary>
    /// Starts all plug-ins that are in the /AutoStart path.
    /// </summary>
    void Startup();
  }
}
