using System;
using System.Collections.Generic;

using MediaPortal.Configuration.Settings;


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
    /// Keys representing the current node.
    /// </summary>
    private ICollection<string> _searchKeys;

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
        if (parent._setting.Type == SettingType.Section)
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
      if (_searchKeys == null)
        UpdateSearchKeys();
      searchValue = searchValue.ToLower();
      if (searchValue == _setting.Text.ToString().ToLower())
        return 1;
      int result = 0;
      lock (_searchKeys)
      {
        foreach (string key in _searchKeys)
        {
          if (key.ToLower().Contains(searchValue))
            result++;
        }
      }
      return (float)((double)result / _searchKeys.Count);
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

    #region Private Methods

    /// <summary>
    /// Updates the searchkeys.
    /// </summary>
    private void UpdateSearchKeys()
    {
      _searchKeys = new List<string>();
      // Add setting information to the keys
      if (_setting.Text.Label != "[system.invalid]")
        _searchKeys.Add(_setting.Text.ToString().ToLower());
      if (_setting.Help.Label != "[system.invalid]")
        _searchKeys.Add(_setting.Help.ToString().ToLower());
      // If the setting is a list, add its items
      if (_setting is ItemList)
      {
        foreach (object o in ((ItemList)_setting).Items)
        {
          string value = o.ToString();
          if (value != null && value != "" && value != "[system.invalid]")
            _searchKeys.Add(value);
        }
      }
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
        _searchKeys = null;
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
        _searchKeys = null;
        _parent = (ConfigurationNode)value;
        if (_parent != null)
        {
          if (_parent.Setting.Type == SettingType.Section)
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
      get { return (_setting != null ? _setting.Enabled : false); }
    }

    /// <summary>
    /// Gets if the node is visible.
    /// </summary>
    public bool IsVisible
    {
      get { return (_setting != null ? _setting.Hidden : false); }
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
