using System;
using System.Collections.Generic;


namespace MediaPortal.Configuration
{
  /// <summary>
  /// The default IConfigurationManager service.
  /// </summary>
  /// <remarks>
  /// Main features are:
  /// <list type="bullet">
  ///   <item>Async loading of the ConfigurationTree.</item>
  ///   <item>Grouping of configuration items which share the same settingsclass.</item>
  /// </list>
  /// </remarks>
  public class ConfigurationManager : IConfigurationManager
  {

    #region Variables

    /// <summary>
    /// Tree to load the settings to.
    /// </summary>
    private ConfigurationTree _tree;
    /// <summary>
    /// All files, with their linked configuration items.
    /// </summary>
    private ICollection<SettingFile> _files;
    /// <summary>
    /// The object responsible for loading all items to the tree.
    /// </summary>
    private ConfigurationLoader _loader;

    #endregion

    #region Constructors

    /// <summary>
    /// Initializes a new instance of ConfigurationManager.
    /// </summary>
    public ConfigurationManager()
    {
      _tree = new ConfigurationTree();
      _loader = new ConfigurationLoader(_tree);
      _loader.OnTreeLoaded += new EventHandler(_loader_OnTreeLoaded);
    }

    #endregion

    #region Private Methods

    /// <summary>
    /// Searches for matches in the given ConfigurationNodeCollection.
    /// </summary>
    /// <param name="collection">Collection to search through.</param>
    /// <param name="searchValue">Value to search for.</param>
    /// <param name="bestMatch">Best match to the value.</param>
    /// <param name="bestMatchScore">Score of the best match.</param>
    /// <returns></returns>
    private IEnumerable<IConfigurationNode> Search(IList<IConfigurationNode> collection, string searchValue, out IConfigurationNode bestMatch, out float bestMatchScore)
    {
      List<IConfigurationNode> nodes = new List<IConfigurationNode>();
      bestMatch = null;
      bestMatchScore = 0;
      foreach (ConfigurationNode node in collection)
      {
        ConfigurationNode newNode = new ConfigurationNode(node.Setting, (ConfigurationNode)node.Parent);
        IConfigurationNode bestNode; // needed for recursive call
        float bestScore;             // needed for recursive call
        foreach (IConfigurationNode n in Search(node.Nodes, searchValue, out bestNode, out bestScore))
          newNode.Nodes.Add(n);
        if (bestScore > bestMatchScore)
        {
          bestMatch = bestNode;
          bestMatchScore = bestScore;
        }
        bestScore = node.Matches(searchValue);
        if (bestScore > bestMatchScore)
        {
          bestMatchScore = bestScore;
          bestMatch = newNode;
        }
        if (newNode.Nodes.Count > 0 || bestScore > 0)
          nodes.Add(newNode);
      }
      return nodes;
    }

    #endregion

    #region EventHandlers

    private void _loader_OnTreeLoaded(object sender, EventArgs e)
    {
      _tree = _loader.Tree;
      _files = _loader.SettingFiles;
    }

    #endregion

    #region IConfigurationManager Members

    /// <summary>
    /// Applies all settings managed by the current IConfigurationManager.
    /// </summary>
    public void Apply()
    {
      if (!_loader.IsLoaded)
        _files = _loader.SettingFiles;
      lock (_files)
      {
        foreach (SettingFile file in _files)
        {
          foreach (IConfigurationNode node in file.LinkedNodes)
            node.Setting.Apply();
        }
      }
    }

    /// <summary>
    /// Loads the tree.
    /// </summary>
    public void Load()
    {
      if (_loader.IsLoaded || _loader.IsLoading)
        return;
      _loader.LoadTree();
    }

    /// <summary>
    /// Saves all settings managed by the current ConfigurationManager.
    /// </summary>
    public void Save()
    {
      if (!_loader.IsLoaded)
        _files = _loader.SettingFiles;
      lock (_files)
      {
        foreach (SettingFile file in _files)
          file.Save();
      }
    }

    /// <summary>
    /// Gets all sections.
    /// </summary>
    /// <returns></returns>
    public IEnumerable<ConfigBase> GetSections()
    {
      if (!_loader.SectionsLoaded)
        _loader.LoadSections();
      List<ConfigBase> settings = new List<ConfigBase>();
      foreach (ConfigurationNode node in _tree.Nodes)
      {
        if (node.Setting.Type == SettingType.Section)
          settings.Add(node.Setting);
      }
      return settings;
    }

    /// <summary>
    /// Gets all sections member of the specified location.
    /// </summary>
    /// <param name="parentLocation"></param>
    /// <returns></returns>
    public IEnumerable<ConfigBase> GetSections(string parentLocation)
    {
      if (!_loader.SectionsLoaded)
        _loader.LoadSections();
      List<ConfigBase> sections = new List<ConfigBase>(); // This will be returned
      // Get the collection containing the nodes
      string[] location = parentLocation.Split(new char[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
      ConfigurationNodeCollection coll = _tree.Nodes;
      foreach (string id in location)
      {
        int index = coll.IndexOf(id);
        if (index == -1) return sections;  // Path doesn't exist
        coll = (ConfigurationNodeCollection)(coll[index].Nodes);
      }
      // Section found, get subsections
      foreach (ConfigurationNode node in coll)
      {
        if (node.Setting.Type == SettingType.Section)
          sections.Add(node.Setting);
      }
      return sections;
    }

    /// <summary>
    /// Gets the content from an item.
    /// </summary>
    /// <exception cref="ArgumentNullException">
    /// An ArgumentNullException is thrown when the argument is empty.
    /// </exception>
    /// <exception cref="NodeNotFoundException">
    /// A NodeNotFoundException is thrown when the itemLocation parameter isn't valid for the current ConfigurationTree.
    /// </exception>
    /// <param name="itemLocation">Location of the item in the ConfigurationTree.</param>
    /// <returns>All underlying items for the specified itemLocation.</returns>
    public IConfigurationNode GetItem(string itemLocation)
    {
      // Do some basic checks and split the location to an array
      if (itemLocation == null)
        throw new ArgumentNullException("The parameter \"itemLocation\" can't be null.");
      string[] location = itemLocation.Split(new char[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
      if (location.Length == 0)
        throw new NodeNotFoundException("The parameter \"itemLocation\" can't be empty.");
      // Try to find the requested item
      try
      {
        // Search the requested node
        IConfigurationNode node = _tree.Nodes[_tree.Nodes.IndexOf(location[0])];
        for (int i = 1; i < location.Length; i++)
        {
          if (!((ConfigurationNodeCollection)node.Nodes).IsSet) break;
          node = node.Nodes[((ConfigurationNodeCollection)node.Nodes).IndexOf(location[i])];
        }
        // If not set: load it's section first
        if (!((ConfigurationNodeCollection)node.Nodes).IsSet)
        {
          node = _loader.LoadSection(itemLocation);
          location = itemLocation.Substring(node.ToString().Length).Split(new char[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
          foreach (string loc in location)
            node = node.Nodes[((ConfigurationNodeCollection)node.Nodes).IndexOf(loc)];
        }
        return node;
      }
      catch (IndexOutOfRangeException ex)
      {
        throw new NodeNotFoundException(String.Format("The specified location isn't valid: \"{0}\"", itemLocation), ex);
      }
    }

    /// <summary>
    /// Returns all nodes which match the specified value.
    /// </summary>
    /// <param name="searchValue">Value to search for.</param>
    /// <returns></returns>
    public IEnumerable<IConfigurationNode> Search(string searchValue)
    {
      IConfigurationNode node;
      float score;
      IEnumerable<IConfigurationNode> result;
      lock (_tree.Nodes)
        result = Search(_tree.Nodes, searchValue, out node, out score);
      return result;
    }

    /// <summary>
    /// Returns all nodes which match the specified value.
    /// </summary>
    /// <param name="searchValue">Value to search for.</param>
    /// <param name="bestMatch">The best matching node.</param>
    /// <returns></returns>
    public IEnumerable<IConfigurationNode> Search(string searchValue, out IConfigurationNode bestMatch)
    {
      float score;
      IEnumerable<IConfigurationNode> result;
      lock (_tree.Nodes)
        result = Search(_tree.Nodes, searchValue, out bestMatch, out score);
      return result;
    }

    /// <summary>
    /// Returns a default instance of IConfigurationNode, which can be used with the current manager.
    /// </summary>
    /// <returns></returns>
    public IConfigurationNode CreateNewNode()
    {
      return new ConfigurationNode();
    }

    #endregion

  }
}
