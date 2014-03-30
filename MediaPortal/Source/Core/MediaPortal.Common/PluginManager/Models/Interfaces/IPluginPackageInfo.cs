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

using System;

namespace MediaPortal.Common.PluginManager.Models
{
  /// <summary>
  /// Plugin metadata interface. Provides access to static information (retrieved from plugin.xml 
  /// or the plugin data feed).
  /// </summary>
  public interface IPluginPackageInfo
  {
    /// <summary>
    /// Returns the plugin's name.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Returns the plugin's unique id.
    /// </summary>
    Guid PluginId { get; }

    /// <summary>
    /// Returns the plugin's copyright statement.
    /// </summary>
    string Copyright { get; }

    /// <summary>
    /// Returns the plugin's author.
    /// </summary>
    string Author { get; }

    /// <summary>
    /// Returns a short description of the plugins function.
    /// </summary>
    string Description { get; }

    /// <summary>
    /// Returns the plugin's version.
    /// </summary>
    string PluginVersion { get; }

    /// <summary>
    /// Returns the release date of this version of the plugin.
    /// </summary>
    DateTime ReleaseDate { get; }
  }
}
