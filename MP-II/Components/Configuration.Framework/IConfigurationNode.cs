using System;
using System.Collections.Generic;
using System.Text;

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

  }

}
