#region Copyright (C) 2007-2008 Team MediaPortal

/*
 *  Copyright (C) 2007-2008 Team MediaPortal
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

using System.Collections.Generic;


namespace MediaPortal.Configuration
{
  public interface IConfigurationManager
  {

    /// <summary>
    /// Applies all settings managed by the current IConfigurationManager.
    /// </summary>
    void Apply();

    /// <summary>
    /// Loads the manager.
    /// </summary>
    void Load();

    /// <summary>
    /// Saves all settings managed by the current IConfigurationManager.
    /// </summary>
    void Save();

    /// <summary>
    /// Returns all rootsections.
    /// </summary>
    /// <returns></returns>
    IEnumerable<ConfigSection> GetSections();

    /// <summary>
    /// Returns all sections which are direct members of the setting with the specified ID.
    /// </summary>
    /// <param name="parentLocation"></param>
    /// <returns></returns>
    IEnumerable<ConfigSection> GetSections(string parentLocation);

    /// <summary>
    /// Gets an item with all its subitems.
    /// </summary>
    /// <param name="itemLocation"></param>
    /// <returns></returns>
    IConfigurationNode GetItem(string itemLocation);

    /// <summary>
    /// Returns the nodes, in a hiearchical order, matching the searchvalue.
    /// </summary>
    /// <param name="searchValue">Value to search for.</param>
    /// <returns></returns>
    IEnumerable<IConfigurationNode> Search(string searchValue);

    /// <summary>
    /// Returns the nodes, in a hiearchical order, matching the searchvalue.
    /// </summary>
    /// <param name="searchValue">Value to search for.</param>
    /// <param name="bestMatch">The best match, node to select.</param>
    /// <returns></returns>
    IEnumerable<IConfigurationNode> Search(string searchValue, out IConfigurationNode bestMatch);

    /// <summary>
    /// Returns a new default instance of IConfigurationNode.
    /// </summary>
    /// <returns></returns>
    IConfigurationNode CreateNewNode();

  }
}
