using System;
using System.Collections.Generic;
using System.Text;

namespace MediaPortal.Configuration
{
  public interface IConfigurationManager
  {

    /// <summary>
    /// Loads the manager.
    /// </summary>
    void Load();

    /// <summary>
    /// Returns all rootsections.
    /// </summary>
    /// <returns></returns>
    IEnumerable<ConfigBase> GetSections();

    /// <summary>
    /// Returns all sections which are direct members of the setting with the specified ID.
    /// </summary>
    /// <param name="parentLocation"></param>
    /// <returns></returns>
    IEnumerable<ConfigBase> GetSections(string parentLocation);

    /// <summary>
    /// Gets an item with all its subitems.
    /// </summary>
    /// <param name="sectionLocation"></param>
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
