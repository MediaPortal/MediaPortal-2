#region Copyright (C) 2007-2009 Team MediaPortal

/*
 *  Copyright (C) 2007-2009 Team MediaPortal
 *  http://www.team-mediaportal.com
 *
 *  This file is part of MediaPortal II
 *
 *  MediaPortal II is free software: you can redistribute it and/or modify
 *  it under the terms of the GNU General Public License as published by
 *  the Free Software Foundation, either version 3 of the License, or
 *  (at your option) any later version.
 *
 *  MediaPortal II is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *  GNU General Public License for more details.
 *
 *  You should have received a copy of the GNU General Public License
 *  along with MediaPortal II.  If not, see <http://www.gnu.org/licenses/>.
 * 
 */

#endregion

using System;

namespace MediaPortal.Core.Configuration
{
  /// <summary>
  /// Configuration manager interface. The configuration manager manages all configuration settings,
  /// which were registered in the application. By accessing the configuration manager, you get access
  /// to all configurable items which are present in the system at the moment.
  /// </summary>
  /// <remarks>
  /// <para>
  /// The configuration manager is responsible for finding all configuration items in the system and for
  /// organizing them in a configuration tree, build with <see cref="IConfigurationNode"/> instances.
  /// The tree contains <see cref="ConfigSection"/>s which are hierarchical organized. Each section may
  /// contain one or more <see cref="ConfigGroup"/>s or descendats of <see cref="ConfigSetting"/>.
  /// The structure is built-up by the <see cref="IConfigurationNode"/> instances, which themselves
  /// refer to the <see cref="ConfigBase"/> objects. So a node location can denote a section as well
  /// as a group or a config item.
  /// </para>
  /// <para>
  /// To specify a location in the configuration tree, a notation similar to the unix filesystem is used.
  /// "/" denotes the root node (which isn't explicitly exposed by this interface) and is always the starting
  /// symbol of a location path, all further path elements are added, separated by "/" characters.
  /// </para>
  /// <example>
  /// <code>
  /// IConfigurationNode node = ServiceScope.Get&lt;IConfigurationManager&gt;.GetNode("/System/General");
  /// </code>
  /// </example>
  /// The configuration manager has to be disposed by calling <see cref="IDisposable.Dispose"/> after it was
  /// used. This will the configuration tree and clean internal structures, that will be built-up again when
  /// <see cref="Initialize"/> is called again.
  /// </remarks>
  public interface IConfigurationManager
  {
    /// <summary>
    /// Initializes internal structures. This has to be called before any other methods can be used.
    /// </summary>
    void Initialize();

    /// <summary>
    /// Cleans up internal structures after usage. This has to be done after the configuration manager was used.
    /// To re-use the instance, call <see cref="Initialize"/> again.
    /// </summary>
    void Dispose();

    /// <summary>
    /// Removes all application configuration data from the system.
    /// After calling this, the configuration manager is disposed. If it should be re-used, call
    /// <see cref="Initialize"/>.
    /// </summary>
    void RemoveAllConfigurationData(bool user, bool global);

    /// <summary>
    /// (Re)loads the application settings to the manager.
    /// </summary>
    /// <remarks>
    /// This might load all settings at once or maybe initialize lazy loading structures, depending
    /// on the implementation.
    /// </remarks>
    void Load();

    /// <summary>
    /// Applies and saves all settings managed by this configuration manager.
    /// </summary>
    void Save();

    /// <summary>
    /// Gets the config node at the specified <paramref name="nodeLocation"/>.
    /// </summary>
    /// <param name="nodeLocation">Location of the node to return.</param>
    /// <returns>Configuration node at the specified <paramref name="nodeLocation"/>.</returns>
    /// <exception cref="NodeNotFoundException">If there is no configuration node at the specified
    /// <paramref name="nodeLocation"/>.</exception>
    IConfigurationNode GetNode(string nodeLocation);

    /// <summary>
    /// Searches the given <paramref name="searchText"/> in all configuration objects.
    /// This method returns a <seealso cref="SearchResult"/> containing an enumerable of
    /// all matching configuration nodes and the best matching node.
    /// This can be used for a config application which allows the user to type in a search text to find
    /// nodes in the tree.
    /// </summary>
    /// <param name="searchText">Value to search for.</param>
    /// <returns>SearchResult, containing an enumeration of all matching configuration nodes and the best match.</returns>
    SearchResult Search(string searchText);
  }
}
