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
using MediaPortal.Core;
using MediaPortal.Core.Logging;
using MediaPortal.Core.PluginManager;
using MediaPortal.Core.Registry;
using MediaPortal.Core.Settings;
using MediaPortal.Utilities;


namespace MediaPortal.Configuration
{
  public delegate void ConfigurationNodeActionDelegate(ConfigurationNode node);

  /// <summary>
  /// Default implementation for a node in the configuration tree. This class supports lazy loading
  /// of child nodes.
  /// </summary>
  public class ConfigurationNode : IConfigurationNode, IDisposable
  {
    #region Constants

    /// <summary>
    /// Location to start searching for configuration items, in the plugintree.
    /// </summary>
    protected const string PLUGINTREEBASELOCATION = "/Configuration/Settings";

    /// <summary>
    /// Section <see cref="ConfigBaseMetadata.Text"/> which will be used when child config objects are
    /// located under a section which was not explicitly defined.
    /// </summary>
    protected const string INVALID_SECTION_TEXT = "[system.invalid]";

    #endregion

    #region Protected fields

    protected bool _childrenLoaded = false;
    protected IPluginItemStateTracker _childPluginItemStateTracker = null;

    /// <summary>
    /// Configuration object linked to this node.
    /// </summary>
    protected ConfigBase _configObj;

    protected ConfigurationNode _parent;
    protected IList<ConfigurationNode> _childNodes;

    #endregion

    #region Constructors

    /// <summary>
    /// Initializes a new instance of ConfigurationNode.
    /// </summary>
    public ConfigurationNode() : this(null, null) { }

    /// <summary>
    /// [Internal Constructor] Initializes a new instance of the ConfigurationNode class.
    /// </summary>
    /// <param name="configObj">Setting to link to the node.</param>
    /// <param name="parent">Parent node of the node.</param>
    public ConfigurationNode(ConfigBase configObj, ConfigurationNode parent)
    {
      _configObj = configObj;
      _parent = parent;
      _childNodes = new List<ConfigurationNode>();
    }

    #endregion

    #region Protected methods

    protected void CheckChildrenLoaded()
    {
      if (!IsChildrenLoaded)
        LoadChildren();
    }

    protected void LoadChildren()
    {
      if (_childrenLoaded)
        return;
      ILogger logger = ServiceScope.Get<ILogger>();
      _childPluginItemStateTracker = new FixedItemStateTracker();
      IPluginManager pluginManager = ServiceScope.Get<IPluginManager>();
      string location = PLUGINTREEBASELOCATION + Location;
      // We'll use a FixedItemStateTracker in the hope that the configuration will be disposed
      // after usage. The alternative would be to use a plugin item state tracker which is able to
      // remove a config element usage. But this would mean to also expose a listener registration
      // to the outside. I think this is not worth the labor.
      ICollection<PluginItemMetadata> items = pluginManager.GetAllPluginItemMetadata(location);
      IDictionary<string, object> childSet = new Dictionary<string, object>();
      foreach (PluginItemMetadata item in items)
      {
        ConfigBaseMetadata metadata = pluginManager.RequestPluginItem<ConfigBaseMetadata>(
            item.RegistrationLocation, item.Id, _childPluginItemStateTracker);
        ConfigBase childObj = Instantiate(metadata, item.PluginRuntime);
        AddChildNode(childObj);
        childSet.Add(metadata.Id, null);
      }
      ICollection<string> childLocations = pluginManager.GetAvailableChildLocations(location);
      foreach (string childLocation in childLocations)
      {
        string childId = RegistryHelper.GetLastPathSegment(childLocation);
        if (childSet.ContainsKey(childId))
          continue;
        logger.Warn("Configuration: Configuration section '{0}' was found in the tree but not explicitly registered as section (config items in this section are registered by those plugins: {1})",
            childLocation, StringUtils.Join(", ", FindPluginRegistrations(childLocation)));
        ConfigSectionMetadata dummyMetadata = new ConfigSectionMetadata(childLocation, INVALID_SECTION_TEXT, null, null);
        ConfigSection dummySection = new ConfigSection();
        dummySection.SetMetadata(dummyMetadata);
        AddChildNode(dummySection);
      }
      _childrenLoaded = true;
    }

    /// <summary>
    /// Helper method to create a meaningful log message. See usage.
    /// </summary>
    /// <param name="location">Parent location to search all child locations where plugins registering
    /// items to.</param>
    /// <returns>List of plugin names registering items under the specified
    /// <paramref name="location"/>.</returns>
    private static IList<string> FindPluginRegistrations(string location)
    {
      IPluginManager pluginManager = ServiceScope.Get<IPluginManager>();
      ICollection<PluginItemMetadata> itemRegistrations = pluginManager.GetAllPluginItemMetadata(location);
      List<string> result = new List<string>();
      foreach (PluginItemMetadata itemRegistration in itemRegistrations)
        result.Add(itemRegistration.PluginRuntime.Metadata.Name);
      foreach (string childLocation in pluginManager.GetAvailableChildLocations(location))
        result.AddRange(FindPluginRegistrations(childLocation));
      return result;
    }

    protected void AddChildNode(ConfigBase childObj)
    {
      _childNodes.Add(new ConfigurationNode(childObj, this));
    }

    protected static ConfigBase Instantiate(ConfigBaseMetadata metadata, PluginRuntime pluginRuntime)
    {
      ISettingsManager settingsManager = ServiceScope.Get<ISettingsManager>();
      ConfigBase result;
      if (metadata.GetType() == typeof(ConfigGroupMetadata))
        result = new ConfigGroup();
      else if (metadata.GetType() == typeof(ConfigSectionMetadata))
        result = new ConfigSection();
      else if (metadata.GetType() == typeof(ConfigSettingMetadata))
      {
        ConfigSettingMetadata csm = (ConfigSettingMetadata) metadata;
        ConfigSetting cs = (ConfigSetting) pluginRuntime.InstanciatePluginObject(csm.ClassName);
        cs.Load(settingsManager.Load(cs.SettingsObjectType));
        result = cs;
      }
      else
        throw new NotImplementedException(string.Format("Unknown child class '{0}' of '{1}'", metadata.GetType().FullName, typeof(ConfigBaseMetadata).FullName));
      result.SetMetadata(metadata);
      return result;
    }

    #endregion

    #region Public properties

    /// <summary>
    /// Lazy load flag. If set to <c>true</c>, this node was loaded and its direct childnodes were
    /// set (the childnodes themselves don't need to be loaded yet). If set to <c>false</c>, this
    /// node needs to be loaded by method <see cref="Load"/>.
    /// </summary>
    public bool IsChildrenLoaded
    {
      get { return _childrenLoaded; }
      set { _childrenLoaded = value; }
    }

    #endregion

    #region Public methods

    public void ForEach(ConfigurationNodeActionDelegate action, bool avoidUnloadedNodes)
    {
      action(this);
      if (!IsChildrenLoaded)
        if (avoidUnloadedNodes)
          return;
        else
          LoadChildren();
      foreach (ConfigurationNode childNode in _childNodes)
        childNode.ForEach(action, avoidUnloadedNodes);
    }

    /// <summary>
    /// Disposes the child nodes and release their registration at the plugin manager.
    /// After this method was called, the node is in the same state like it was before it was
    /// lazy loaded - and will switch back to a fully initialized state automatically when it is used again.
    /// </summary>
    public void DisposeChildren()
    {
      if (!_childrenLoaded)
        return;
      IPluginManager pluginManager = ServiceScope.Get<IPluginManager>();
      string location = PLUGINTREEBASELOCATION + Location;
      foreach (ConfigurationNode node in _childNodes)
      {
        node.DisposeChildren();
        // To fulfil the classes invariant, we need to do the dispose work for our children - like
        // we built up our children in method LoadChildren()
        pluginManager.RevokePluginItem(location, node.Id, _childPluginItemStateTracker);
        _childPluginItemStateTracker = null;
      }
      _childNodes.Clear();
      _childrenLoaded = false;
    }

    #endregion

    #region IDisposable implementation

    public void Dispose()
    {
      DisposeChildren();
    }

    #endregion

    #region IConfigurationNode implementation

    public string Id
    {
      get { return _configObj == null ? string.Empty : _configObj.Metadata.Id; }
    }

    public string Location
    {
      get
      {
        if (_parent == null)
          return Id;
        return String.Format("{0}/{1}", _parent.Location, Id);
      }
    }

    public ConfigBase ConfigObj
    {
      get { return _configObj; }
    }

    public IConfigurationNode Parent
    {
      get { return _parent; }
    }

    public IList<IConfigurationNode> ChildNodes
    {
      get
      {
        CheckChildrenLoaded();
        IList<IConfigurationNode> result = new List<IConfigurationNode>(_childNodes.Count);
        foreach (ConfigurationNode node in _childNodes)
          result.Add(node);
        return result;
      }
    }

    public IConfigurationNode GetSubNodeById(string id)
    {
      CheckChildrenLoaded();
      foreach (IConfigurationNode node in _childNodes)
        if (node.Id == id)
          return node;
      return null;
    }

    #endregion
  }
}
