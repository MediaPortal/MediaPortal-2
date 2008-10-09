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
using System.Threading;

using MediaPortal.Core;
using MediaPortal.Core.Logging;
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
    //private const string PLUGINSTART = "/Configuration/Settings/";

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
    /// <summary>
    /// Settings which are waiting to be registered to another setting.
    /// </summary>
    private IDictionary<string, ICollection<ConfigItem>> _waiting;
    /// <summary>
    /// Collection of items which have been loaded to the tree already,
    /// used for fast verification of existing items.
    /// </summary>
    private ICollection<string> _passedItems;

    private LoaderHelper _loaderTree;

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
      : this(new ConfigurationTree())
    { }

    /// <summary>
    /// Initializes a new instance of ConfigurationLoader.
    /// </summary>
    /// <param name="tree">Tree to load all configurations to.</param>
    public ConfigurationLoader(ConfigurationTree tree)
    {
      _tree = tree;
      _files = new List<SettingFile>();
      _waiting = new Dictionary<string, ICollection<ConfigItem>>();
      _passedItems = new List<string>();
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
        throw new ArgumentNullException("The parameter sectionPath can't be null.");
      string[] path = sectionPath.Split(new char[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
      if (path.Length == 0)
        throw new NodeNotFoundException(String.Format("Invalid path: \"{0}\"", sectionPath));
      // Make sure that the sections have been loaded already
      if (!_sectionsLoaded)
        LoadSections(ServiceScope.Get<IPluginManager>(), "", _tree.Nodes);
      // Find the requested node
      IConfigurationNode node;
      if (!FindNode(path, out node))
        throw new NodeNotFoundException(String.Format("Invalid path: \"{0}\"", sectionPath));
      // Make sure to return a section
      if (!(node.Setting is ConfigSection))
        node = node.Section;
      // Load the node if not loaded yet
      if (!((ConfigurationNodeCollection)node.Nodes).IsSet)
        LoadItem(node.ToString() + "/", (ConfigurationNodeCollection)node.Nodes);
      return node;
    }

    /// <summary>
    /// Loads all the sections.
    /// </summary>
    public void LoadSections()
    {
      LoadSections(ServiceScope.Get<IPluginManager>(), "", _tree.Nodes);
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
        LoadSections(manager, "", _tree.Nodes);
      _sectionIndex = 0;
      if (_tree.Nodes.Count == 0) return;
      Thread t = new Thread(LoadAllItems)
                   {
                     IsBackground = true,
                     Name = "MPII - Configuration Framework Loader"
                   };
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

    private void LoadLoaderTree(IPluginManager manager)
    {
      _loaderTree = new LoaderHelper();
      foreach (KeyValuePair<string, PluginRuntime> pair in manager.AvailablePlugins)
        _loaderTree.Add(pair.Value);
    }

    /// <summary>
    /// Loads all sections from the specified location to the specified destination collection,
    /// using the specified IPluginManager.
    /// </summary>
    /// <remarks>
    /// This method will allways be the first one to be called after initialization.
    /// </remarks>
    /// <param name="manager">IPluginManager to load sections with.</param>
    /// <param name="pluginLocation">Location to load sections from.</param>
    /// <param name="destCollection">Collection to load sections to.</param>
    private void LoadSections(IPluginManager manager, string pluginLocation, IList<IConfigurationNode> destCollection)
    {
      if (_loaderTree == null)
        LoadLoaderTree(manager);
      ICollection<ConfigBase> settings = _loaderTree.InitializeItems(pluginLocation);
      lock (destCollection)
      {
        foreach (ConfigBase setting in settings)
        {
          if (setting is ConfigItem)
            continue;
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
      _sectionsLoaded = true;
    }

    /// <summary>
    /// Loads all items from the specified location.
    /// </summary>
    /// <param name="pluginLocation"></param>
    /// <param name="destCollection"></param>
    private void LoadItem(string pluginLocation, ConfigurationNodeCollection destCollection)
    {
      lock (destCollection)
      {
        if (destCollection.IsSet) return;
        ICollection<ConfigBase> settings = _loaderTree.InitializeItems(pluginLocation);
        foreach (ConfigBase setting in settings)
        {
          int index = IndexOfNode(destCollection, setting.Id);
          IConfigurationNode node;
          if (index == -1)
          {
            node = new ConfigurationNode(setting, _tree);
            destCollection.Add(node);
            if (node.Setting is ConfigItem)
            {
              SettingFile file = GetSettingFile(((ConfigItem) node.Setting).SettingsObject);
              lock (file)
              {
                int indx = file.LinkedNodes.IndexOf(node);
                if (indx == -1)
                {
                  ((ConfigItem)node.Setting).Load(file.SettingObject);
                  file.LinkedNodes.Add(node);
                }
                else
                {
                  node = file.LinkedNodes[indx];
                }
              }
            }
            else
            {
              node = destCollection[index];
            }
            LoadItem(pluginLocation + setting.Id + "/", (ConfigurationNodeCollection) node.Nodes);
            RegisterNode(node);
          }
          else if (!((ConfigurationNodeCollection)destCollection[index].Nodes).IsSet)
          {
            LoadItem(pluginLocation + setting.Id + "/", (ConfigurationNodeCollection)destCollection[index].Nodes);
          }
        }
        destCollection.IsSet = true;
      }
    }

    /// <summary>
    /// Finds the index of the node containing the specified SettingRegistrationBase.Id
    /// in the given list of nodes.
    /// </summary>
    /// <param name="collection">List to search through.</param>
    /// <param name="configId">SettingRegistrationBase.Id to find.</param>
    /// <returns></returns>
    private int IndexOfNode(IEnumerable<IConfigurationNode> collection, string configId)
    {
      int index = -1;
      lock (collection)
      {
        foreach (IConfigurationNode node in collection)
        {
          index++;
          if (node.Setting.Id.ToString() == configId)
            return index;
        }
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
        Interlocked.Increment(ref _sectionIndex);
        IConfigurationNode root = _tree.Nodes[currentIndex];
        // Load the nodes?
        if (!((ConfigurationNodeCollection)root.Nodes).IsSet)
          LoadItem(root.Setting.Id.ToString() + "/", (ConfigurationNodeCollection)root.Nodes);
      }
      _tree.Nodes.IsSet = true;
      _isLoaded = true;
      _isLoading = false;
      if (_waiting.Count != 0)  // Is everything registered?
      {

      }
      if (OnTreeLoaded != null)
        OnTreeLoaded(this, new EventArgs());
    }

    /// <summary>
    /// Gets the SettingFile for the specified object.
    /// </summary>
    /// <param name="settingsClass"></param>
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
          settingsClass = ServiceScope.Get<ISettingsManager>().Load(settingsClass.GetType());
        _files.Add(new SettingFile(settingsClass));
        return _files[_files.Count - 1];
      }
    }

    /// <summary>
    /// Registeres the node to other nodes as required by the underlying SettingRegistrationBase,
    /// and registers other waiting nodes to this node if necessairy.
    /// </summary>
    /// <param name="node"></param>
    private void RegisterNode(IConfigurationNode node)
    {
      string nodeLocation = node.ToString();
      lock (_passedItems)
        _passedItems.Add(nodeLocation);
      // A setting which isn't a ConfigItem can't be registered.
      if (!(node.Setting is ConfigItem))
        return;
      // Register waiting settings to the current setting.
      lock (_waiting)
      {
        if (_waiting.ContainsKey(nodeLocation))
        {
          IEnumerable<ConfigItem> waiters = _waiting[nodeLocation];
          _waiting.Remove(nodeLocation);
          foreach (ConfigItem waiter in waiters)
            ((ConfigItem) node.Setting).Register(waiter);
        }
      }
      // Register current setting to other settings,
      // or if not found add it to the waiters 'till the setting is loaded too.
      foreach (string location in ((ConfigItem)node.Setting).ListenTo)
      {
        // Convert the plugintree path to a configurationtree path.
        // Always use english as the culture, all .plugin files should be provided with english characters only.
        string loc = location.ToLower(new System.Globalization.CultureInfo("en"));
        bool exists;
        lock (_passedItems)
          exists = _passedItems.Contains(loc);
        if (exists)
        {
          IConfigurationNode rNode;
          if (FindNode(loc.Split(new char[] {'/'}, StringSplitOptions.RemoveEmptyEntries), out rNode))
          {
            ((ConfigItem) rNode.Setting).Register(((ConfigItem) node.Setting));
            continue;
          }
        }
        // Add to waiting if not found/continueed above.
        lock (_waiting)
        {
          if (!_waiting.ContainsKey(loc))
            _waiting.Add(loc, new List<ConfigItem>());
          _waiting[loc].Add((ConfigItem) node.Setting);
        }
      }
    }

    /// <summary>
    /// Returns if the specified location can be found in the tree.
    /// If found, its data is loaded to the specified node.
    /// If not found, a new log entry is added to the debug log.
    /// </summary>
    /// <param name="location">Location to search for.</param>
    /// <param name="node">Node to load the found data to.</param>
    /// <returns>Does the specified location contain data?</returns>
    private bool FindNode(string[] location, out IConfigurationNode node)
    {
      int index = IndexOfNode(_tree.Nodes, location[0]);
      if (index == -1)
      {
        ServiceScope.Get<ILogger>().Debug("Invalid root specified in path: \"{1}\"", string.Join("/", location));
        node = null;
        return false;
      }
      node = _tree.Nodes[index];
      for (int i = 1; i < location.Length; i++)
      {
        index = IndexOfNode(node.Nodes, location[i]);
        if (index == -1)
        {
          ServiceScope.Get<ILogger>().Debug("Invalid ConfigurationNode \"{0}\" specified in path: \"{1}\"", location[i], string.Join("/", location));
          return false;
        }
        node = node.Nodes[index];
      }
      return true;
    }

    #endregion

  }
}
