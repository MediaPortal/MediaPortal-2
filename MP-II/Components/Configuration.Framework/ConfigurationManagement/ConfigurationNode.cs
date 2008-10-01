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
  /// Represents a node of a <see cref="ConfigurationTree"/>.
  /// This is the default implementation of IConfigurationNode.
  /// </summary>
  internal class ConfigurationNode : IConfigurationNode
  {

    #region Variables

    /// <summary>
    /// ConfigBase linked to the current node.
    /// </summary>
    private ConfigBase _setting;
    /// <summary>
    /// Tree containing the current node.
    /// </summary>
    private ConfigurationTree _tree;
    /// <summary>
    /// Node containing the current node.
    /// </summary>
    private ConfigurationNode _parent;
    /// <summary>
    /// Section containing the current node.
    /// </summary>
    private ConfigurationNode _section;
    /// <summary>
    /// Childnodes.
    /// </summary>
    private ConfigurationNodeCollection _nodes;
    /// <summary>
    /// Matches the current ConfigBase with a specified value.
    /// </summary>
    private NodeMatcher _matcher;

    #endregion

    #region Properties

    /// <summary>
    /// Gets the parent tree that the tree node is assigned to.
    /// </summary>
    public ConfigurationTree Tree
    {
      get { return _tree; }
      internal set { _tree = value; }
    }

    #endregion

    #region Constructors

    /// <summary>
    /// Initializes a new instance of ConfigurationNode.
    /// </summary>
    public ConfigurationNode()
    {
      _setting = null;
      _tree = null;
      _parent = null;
      _section = null;
      _nodes = new ConfigurationNodeCollection(this);
    }

    /// <summary>
    /// Initializes a new instance of the ConfigurationNode class.
    /// </summary>
    /// <param name="setting">Setting to link to the node.</param>
    public ConfigurationNode(ConfigBase setting)
    {
      _setting = setting;
      _tree = null;
      _parent = null;
      _section = null;
      _nodes = new ConfigurationNodeCollection(this);
    }

    /// <summary>
    /// [Internal Constructor] Initializes a new instance of the ConfigurationNode class.
    /// </summary>
    /// <param name="setting">Setting to link to the node.</param>
    /// <param name="tree">Parent tree of the node.</param>
    internal ConfigurationNode(ConfigBase setting, ConfigurationTree tree)
    {
      _setting = setting;
      _tree = tree;
      _parent = null;
      _section = null;
      _nodes = new ConfigurationNodeCollection(this);
    }

    /// <summary>
    /// [Internal Constructor] Initializes a new instance of the ConfigurationNode class.
    /// </summary>
    /// <param name="setting">Setting to link to the node.</param>
    /// <param name="parent">Parent node of the node.</param>
    internal ConfigurationNode(ConfigBase setting, ConfigurationNode parent)
    {
      _setting = setting;
      if (parent != null)
        _tree = parent._tree;
      _parent = parent;
      _nodes = new ConfigurationNodeCollection(this);
      if (parent != null)
      {
        if (parent._setting is ConfigSection)
          _section = _parent;
        else
          _section = _parent._section;
      }
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// Returns a value between 0 and 1 representing how the searchValue matches this node.
    /// </summary>
    /// <param name="searchValue"></param>
    /// <returns></returns>
    public float Matches(string searchValue)
    {
      if (_matcher == null)
        _matcher = new NodeMatcher(_setting);
      return _matcher.Match(searchValue);
    }

    #endregion

    #region IConfigurationNode Members

    /// <summary>
    /// Gets or sets the setting related to the node.
    /// </summary>
    public ConfigBase Setting
    {
      get { return _setting; }
      internal set
      {
        _matcher = null;
        _setting = value;
      }
    }

    /// <summary>
    /// Gets the parent.
    /// </summary>
    public IConfigurationNode Parent
    {
      get { return _parent; }
      internal set
      {
        _parent = (ConfigurationNode)value;
        if (_parent != null)
        {
          if (_parent.Setting is ConfigSection)
            _section = _parent;
          else
            _section = _parent._section;
        }
      }
    }

    /// <summary>
    /// Gets the section containing the current node.
    /// </summary>
    public IConfigurationNode Section
    {
      get { return _section; }
    }

    /// <summary>
    /// Gets the collection of ConfigurationNode objects assigned to the current tree node.
    /// </summary>
    public IList<IConfigurationNode> Nodes
    {
      get { return _nodes; }
    }

    /// <summary>
    /// Gets if the node is enabled.
    /// </summary>
    public bool IsEnabled
    {
      get { return _setting.Disabled; }
    }

    /// <summary>
    /// Gets if the node is visible.
    /// </summary>
    public bool IsVisible
    {
      get { return _setting.Hidden; }
    }

    /// <summary>
    /// Returns a string representing the location of the node in the tree.
    /// </summary>
    /// <returns></returns>
    public override string ToString()
    {
      if (_parent == null)
        return _setting.Id.ToString();
      else
        return String.Format("{0}/{1}", _parent.ToString(), _setting.Id.ToString());
    }

    #endregion

    #region IEquatable<IConfigurationNode> Members

    public bool Equals(IConfigurationNode other)
    {
      // Compares the location in the tree
      return (other is ConfigurationNode
        && this.ToString() == other.ToString());
    }

    #endregion

  }
}
