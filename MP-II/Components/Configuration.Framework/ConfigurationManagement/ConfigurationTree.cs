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

namespace MediaPortal.Configuration
{

  /// <summary>
  /// A hierarchical collection of items, each represented by a ConfigurationNode
  /// </summary>
  internal class ConfigurationTree
  {

    #region Variables

    private ConfigurationNodeCollection _nodes;

    #endregion

    #region Properties

    /// <summary>
    /// Gets the collection of tree nodes that are assigned to the ConfigurationTree.
    /// </summary>
    public ConfigurationNodeCollection Nodes
    {
      get { return _nodes; }
    }

    #endregion

    #region Constructors

    /// <summary>
    /// Initializes a new instance of the ConfigurationTree class.
    /// </summary>
    public ConfigurationTree()
    {
      _nodes = new ConfigurationNodeCollection();
    }

    #endregion

  }
}
