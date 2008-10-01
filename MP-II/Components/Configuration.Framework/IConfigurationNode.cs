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


namespace MediaPortal.Configuration
{

  /// <summary>
  /// Represents an element of the <see cref="IConfigurationManager"/>.
  /// </summary>
  public interface IConfigurationNode : IEquatable<IConfigurationNode>
  {

    /// <summary>
    /// Gets the setting related to the node.
    /// </summary>
    ConfigBase Setting
    {
      get;
    }

    /// <summary>
    /// Gets the parent.
    /// </summary>
    IConfigurationNode Parent
    {
      get;
    }

    /// <summary>
    /// Gets the section containing the current node.
    /// </summary>
    IConfigurationNode Section
    {
      get;
    }

    /// <summary>
    /// Gets the collection of ConfigurationNode objects assigned to the current tree node.
    /// </summary>
    IList<IConfigurationNode> Nodes
    {
      get;
    }

    /// <summary>
    /// Gets if the related setting is enabled.
    /// </summary>
    bool IsEnabled
    {
      get;
    }

    /// <summary>
    /// Gets if the related setting is visible.
    /// </summary>
    bool IsVisible
    {
      get;
    }

    /// <summary>
    /// Returns a string representing the location of the node in the tree.
    /// </summary>
    /// <returns></returns>
    string ToString();

  }

}
