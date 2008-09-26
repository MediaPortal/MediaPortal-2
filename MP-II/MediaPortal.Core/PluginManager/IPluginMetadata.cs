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
using System.IO;
using MediaPortal.Core.PluginManager.Settings;

namespace MediaPortal.Core.PluginManager
{
  /// <summary>
  /// Plugin metadata interface. Provides access to all public plugin metadata properties provided
  /// by the plugin implementation.
  /// </summary>
  public interface IPluginMetadata
  {
    /// <summary>
    /// Returns the plugin's name.
    /// </summary>
    string Name { get; }

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
    /// Gets the information if this plugin should be automatically activated when enabled.
    /// </summary>
    bool AutoActivate { get; }

    /// <summary>
    /// Returns a collection of names of plugins, this plugin depends on.
    /// </summary>
    ICollection<string> DependsOn { get; }

    /// <summary>
    /// Returns a collection of names of plugins, this plugin stands in conflict with.
    /// </summary>
    ICollection<string> ConflictsWith { get; }

    /// <summary>
    /// Returns the file paths of all assemblies to be loaded for this plugin.
    /// </summary>
    ICollection<string> AssemblyFilePaths { get; }

    /// <summary>
    /// Gets the name of the state tracker class for this plugin. If no state tracker should be used,
    /// this value is <c>null</c>.
    /// </summary>
    string StateTrackerClassName { get; }

    /// <summary>
    /// Gets all builders defined by this plugin. The value will contain a mapping of builder names
    /// to their builder class names.
    /// </summary>
    IDictionary<string, string> Builders { get; }

    /// <summary>
    /// Returns all plugin's item registration metadata, which contain the item's ids, the registration
    /// locations and the additional attributes of the item.
    /// </summary>
    ICollection<PluginItemMetadata> PluginItemsMetadata { get; }

    /// <summary>
    /// Contains the metadata for all settings exposed by this plugin.
    /// </summary>
    ICollection<SettingRegistrationBase> PluginSettingsMetadata { get; }

    /// <summary>
    /// Returns the names of all builders which are necessary to build the items of this
    /// plugin. This is a convenience method for iterating over <see cref="PluginItemsMetadata.Values"/>
    /// and collecting all builder names.
    /// </summary>
    /// <returns>Collection of builder names.</returns>
    ICollection<string> GetNecessaryBuilders();

    /// <summary>
    /// Returns an absolute path from the relative path expression which is based on the plugin
    /// directory.
    /// </summary>
    /// <param name="relativePath">File path relative to the plugin directory.</param>
    /// <returns>Absolute file path of the specified relative path.</returns>
    string GetAbsolutePath(string relativePath);
  }
}
