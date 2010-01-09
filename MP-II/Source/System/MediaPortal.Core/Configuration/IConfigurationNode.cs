#region Copyright (C) 2007-2010 Team MediaPortal

/*
    Copyright (C) 2007-2010 Team MediaPortal
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

namespace MediaPortal.Core.Configuration
{
  /// <summary>
  /// Represents a node in the configuration tree of the <see cref="IConfigurationManager"/>.
  /// </summary>
  public interface IConfigurationNode
  {
    /// <summary>
    /// Returns the id of this node.
    /// </summary>
    string Id { get; }

    /// <summary>
    /// Returns a string representing the location of the node in the tree. The location is the
    /// concatenation of the ids of all parents on the path from root to this node.
    /// </summary>
    string Location { get; }

    /// <summary>
    /// Gets the setting related to the node.
    /// </summary>
    ConfigBase ConfigObj { get; }

    /// <summary>
    /// Gets the parent node.
    /// </summary>
    IConfigurationNode Parent { get; }

    /// <summary>
    /// Gets the ordered list of ConfigurationNode objects which are the children of this node.
    /// </summary>
    IList<IConfigurationNode> ChildNodes { get; }

    /// <summary>
    /// Returns the subnode under this node with the specified id.
    /// </summary>
    /// <param name="id">Id of the node to return.</param>
    /// <returns>Configuration node or <c>null</c>, if the specified node doesn't exist.</returns>
    IConfigurationNode GetSubNodeById(string id);
  }
}
