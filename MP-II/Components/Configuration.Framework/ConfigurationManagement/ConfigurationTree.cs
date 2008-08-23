using System;
using System.Collections.Generic;
using System.Text;

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
