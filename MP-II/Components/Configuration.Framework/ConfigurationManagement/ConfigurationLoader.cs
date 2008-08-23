using System;
using System.Collections.Generic;
using System.Threading;

using MediaPortal.Core;
using MediaPortal.Core.PluginManager;
using MediaPortal.Core.Settings;


namespace MediaPortal.Configuration
{

  /// <summary>
  /// ConfigurationLoader loads all configuration items from the PluginTree to a ConfigurationTree.
  /// All configuration items are grouped per settingsclass in the SettingFiles property.
  /// </summary>
  internal class ConfigurationLoader
  {

    #region Constants

    /// <summary>
    /// Location to start searching for configuration items, in the plugintree.
    /// </summary>
    private const string PLUGINSTART = "/Configuration/Settings/";

    #endregion

    #region Variables

    /// <summary>
    /// Is the tree loaded?
    /// </summary>
    private bool _isLoaded;
    /// <summary>
    /// Buys loading the tree?
    /// </summary>
    private bool _isLoading;
    /// <summary>
    /// Are the sections loaded?
    /// </summary>
    private bool _sectionsLoaded;
    /// <summary>
    /// Tree to load all data to.
    /// </summary>
    private ConfigurationTree _tree;
    /// <summary>
    /// The index of the next-to-load section.
    /// </summary>
    private int _sectionIndex;
    /// <summary>
    /// All files, with their linked configuration items.
    /// </summary>
    private IList<SettingFile> _files;

    #endregion

    #region Properties

    /// <summary>
    /// Gets the tree.
    /// </summary>
    public ConfigurationTree Tree
    {
      get { return _tree; }
    }

    /// <summary>
    /// Gets all settingfiles, with all configuration classes linked to them.
    /// </summary>
    public ICollection<SettingFile> SettingFiles
    {
      get { return _files; }
    }

    /// <summary>
    /// Gets if the tree has been loaded already.
    /// </summary>
    public bool IsLoaded
    {
      get { return _isLoaded; }
    }

    /// <summary>
    /// Gets if the tree is being loaded at the current time.
    /// </summary>
    public bool IsLoading
    {
      get { return _isLoading; }
    }

    /// <summary>
    /// Gets if the sections are loaded. (with or without content!)
    /// </summary>
    public bool SectionsLoaded
    {
      get { return _sectionsLoaded; }
    }

    #endregion

    #region Events

    /// <summary>
    /// Gets called when the current ConfigurationLoader is done with loading the tree.
    /// </summary>
    public event EventHandler OnTreeLoaded;

    #endregion

    #region Constructors

    /// <summary>
    /// Initializes a new instance of ConfigurationLoader.
    /// </summary>
    public ConfigurationLoader()
    {
      _tree = new ConfigurationTree();
      _files = new List<SettingFile>();
    }

    /// <summary>
    /// Initializes a new instance of ConfigurationLoader.
    /// </summary>
    /// <param name="tree">Tree to load all configurations to.</param>
    public ConfigurationLoader(ConfigurationTree tree)
    {
      _tree = tree;
      _files = new List<SettingFile>();
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// Returns the specified section.
    /// If the sectionPath doesn't link to a section, the upperlying section will be returned.
    /// </summary>
    /// <exception cref="NodeNotFoundException"></exception>
    /// <exception cref="ArgumentNullException"></exception>
    /// <param name="sectionPath"></param>
    /// <returns></returns>
    public IConfigurationNode LoadSection(string sectionPath)
    {
      // Do some basic checks on the path
      if (sectionPath == null)
        throw new ArgumentNullException("sectionPath can't be null");
      string[] path = sectionPath.Split(new char[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
      if (path.Length == 0)
        throw new NodeNotFoundException(String.Format("Invalid path: \"{0}\"", sectionPath));
      int index = IndexOfNode(_tree.Nodes, path[0]);
      if (index == -1)
        throw new NodeNotFoundException(String.Format("Invalid rootnode \"{0}\" specified: \"{1}\"", path[0], sectionPath));
      // Find the requested node
      IConfigurationNode node = _tree.Nodes[index];
      for (int i = 1; i < path.Length; i++)
      {
        index = IndexOfNode(node.Nodes, path[i]);
        if (index == -1)
          throw new NodeNotFoundException(String.Format("Invalid node \"{0}\" specified: \"{1}\"", path[i], sectionPath));
        if (node.Nodes[index].Setting.Type != SettingType.Section) break; // Stop at the last section, don't load a group or item
        node = node.Nodes[index];
      }
      // Load the node if not loaded yet
      if (!_isLoaded && !((ConfigurationNodeCollection)node.Nodes).IsSet)
      {
        if (_isLoading) // If already loading: make a copy of the node to avoid threading conflicts
        {
          if (node.Parent != null)  // Tree will be extracted from the parent
            node = new ConfigurationNode(node.Setting, (ConfigurationNode)node.Parent);
          else                      // This is a rootnode, specify the tree
            node = new ConfigurationNode(node.Setting, ((ConfigurationNode)node).Tree);
        }
        // LoadItem will add the settingfiles to the variable _files.
        // This is intended behaviour: it's the only way to be 100% sure that all nodes share the same settingobjects.
        LoadItem(ServiceScope.Get<IPluginManager>(), PLUGINSTART + sectionPath + "/", (ConfigurationNodeCollection)node.Nodes);
      }
      return node;
    }

    /// <summary>
    /// Loads all the sections.
    /// </summary>
    public void LoadSections()
    {
      LoadSections(ServiceScope.Get<IPluginManager>(), PLUGINSTART, _tree.Nodes);
    }

    /// <summary>
    /// Loads the tree without blocking the calling thread.
    /// </summary>
    /// <returns></returns>
    public void LoadTree()
    {
      if (_isLoaded || _isLoading) return;
      _isLoading = true;
      _files.Clear();
      IPluginManager manager = ServiceScope.Get<IPluginManager>();
      if (!_sectionsLoaded)
        LoadSections(manager, PLUGINSTART, _tree.Nodes);
      _sectionsLoaded = true;
      _sectionIndex = 0;
      if (_tree.Nodes.Count == 0) return;
      Thread t = new Thread(new ThreadStart(LoadAllItems));
      t.IsBackground = true;
      t.Name = "MPII - Configuration Framework Loader";
      t.Start();
    }

    /// <summary>
    /// Reloads the tree.
    /// </summary>
    public void Reload()
    {
      _tree.Nodes.Clear();
      _tree.Nodes.IsSet = false;
      _isLoaded = false;
      _sectionsLoaded = false;
      LoadTree();
    }

    #endregion

    #region Private Methods

    /// <summary>
    /// Loads all sections from the specified location to the specified destination collection,
    /// using the specified IPluginManager.
    /// </summary>
    /// <param name="manager">IPluginManager to load sections with.</param>
    /// <param name="pluginLocation">Location to load sections from.</param>
    /// <param name="destCollection">Collection to load sections to.</param>
    private void LoadSections(IPluginManager manager, string pluginLocation, IList<IConfigurationNode> destCollection)
    {
      IList<ConfigBase> settings = manager.GetAllPluginItems<ConfigBase>(pluginLocation);
      foreach (ConfigBase setting in settings)
      {
        if (setting.Type == SettingType.Section)
        {
          int index = IndexOfNode(destCollection, setting.Id);
          IConfigurationNode node;
          if (index == -1)
          {
            node = new ConfigurationNode(setting, _tree);
            destCollection.Add(node);
          }
          else
          {
            node = destCollection[index];
          }
          LoadSections(manager, pluginLocation + setting.Id + "/", node.Nodes);
        }
      }
    }

    /// <summary>
    /// Loads all items from the specified location.
    /// </summary>
    /// <param name="manager"></param>
    /// <param name="pluginLocation"></param>
    /// <param name="destCollection"></param>
    private void LoadItem(IPluginManager manager, string pluginLocation, ConfigurationNodeCollection destCollection)
    {
      if (destCollection.IsSet)
        return;
      IList<ConfigBase> settings = manager.GetAllPluginItems<ConfigBase>(pluginLocation);
      foreach (ConfigBase setting in settings)
      {
        int index = IndexOfNode(destCollection, setting.Id);
        IConfigurationNode node;
        if (index == -1)
        {
          node = new ConfigurationNode(setting, _tree);
          destCollection.Add(node);
          if (node.Setting.Type != SettingType.Unknown
            && node.Setting.Type != SettingType.Section
            && node.Setting.Type != SettingType.Group)
          {
            SettingFile file = GetSettingFile(node.Setting.SettingsObject);
            lock (file)
            {
              if (!file.LinkedNodes.Contains(node))
              {
                node.Setting.Load(file.SettingObject);
                file.LinkedNodes.Add(node);
              }
            }
          }
        }
        else
        {
          node = destCollection[index];
        }
        LoadItem(manager, pluginLocation + setting.Id + "/", (ConfigurationNodeCollection)node.Nodes);
      }
      destCollection.IsSet = true;
    }

    /// <summary>
    /// Finds the index of the node containing the specified ConfigBase.Id
    /// in the given list of nodes.
    /// </summary>
    /// <param name="collection">List to search through.</param>
    /// <param name="configId">ConfigBase.Id to find.</param>
    /// <returns></returns>
    private int IndexOfNode(IEnumerable<IConfigurationNode> collection, string configId)
    {
      int index = -1;
      foreach (IConfigurationNode node in collection)
      {
        index++;
        if (node.Setting.Id.ToString() == configId)
          return index;
      }
      return -1;
    }

    /// <summary>
    /// Loads all items to the tree.
    /// </summary>
    private void LoadAllItems()
    {
      while (_tree.Nodes.Count > _sectionIndex)
      {
        int currentIndex = _sectionIndex;
        _sectionIndex++;
        IConfigurationNode root = _tree.Nodes[currentIndex];
        // Load the nodes?
        if (!((ConfigurationNodeCollection)root.Nodes).IsSet)
        {
          ConfigurationNodeCollection coll = new ConfigurationNodeCollection(root);
          LoadItem(ServiceScope.Get<IPluginManager>(), PLUGINSTART + root.Setting.Id.ToString() + "/", coll);
          lock (root)
          {
            foreach (IConfigurationNode item in coll)
            {
              int index = root.Nodes.IndexOf(item);
              if (index == -1)
                root.Nodes.Add(item);
              else
                root.Nodes[index] = item;
            }
            ((ConfigurationNodeCollection)root.Nodes).IsSet = true;
          }
        }
      }
      _tree.Nodes.IsSet = true;
      _isLoaded = true;
      _isLoading = false;
      if (OnTreeLoaded != null)
        OnTreeLoaded(this, new EventArgs());
    }

    /// <summary>
    /// Gets the SettingFile for the specified object.
    /// </summary>
    /// <param name="obj"></param>
    /// <returns></returns>
    private SettingFile GetSettingFile(object settingsClass)
    {
      lock (_files)
      {
        foreach (SettingFile file in _files)
        {
          if (file.SettingObject == null && settingsClass == null)
            return file;
          if (file.SettingObject + "" == settingsClass + "")
            return file;
        }
        if (settingsClass != null)
          ServiceScope.Get<ISettingsManager>().Load(settingsClass);
        _files.Add(new SettingFile(settingsClass));
        return _files[_files.Count - 1];
      }
    }

    #endregion

  }
}
