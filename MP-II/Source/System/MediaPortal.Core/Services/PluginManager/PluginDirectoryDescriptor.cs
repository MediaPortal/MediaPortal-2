#region Copyright (C) 2007-2008 Team MediaPortal

/*
 *  Copyright (C) 2007-2008 Team MediaPortal
 *  http://www.team-mediaportal.com
 *
 *  This file is part of MediaPortal II
 *
 *  MediaPortal II is free software: you can redistribute it and/or modify
 *  it under the terms of the GNU General Public License as published by
 *  the Free Software Foundation, either version 3 of the License, or
 *  (at your option) any later version.
 *
 *  MediaPortal II is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *  GNU General Public License for more details.
 * 
 *  You should have received a copy of the GNU General Public License
 *  along with MediaPortal II.  If not, see <http://www.gnu.org/licenses/>.
 * 
 */

#endregion

using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using MediaPortal.Core.Logging;
using MediaPortal.Core.PluginManager;
using MediaPortal.Utilities;

namespace MediaPortal.Core.Services.PluginManager
{
  /// <summary>
  /// Class providing all the static plugin metadata information contained in the plugin.xml file
  /// in a plugin directory. Contains the parser for plugin.xml files.
  /// </summary>
  class PluginDirectoryDescriptor: IPluginMetadata
  {
    public const string PLUGIN_META_FILE = "plugin.xml";

    // FIXME Albert: Change meaning: version-high must match exactly
    public const int MIN_PLUGIN_DESCRIPTOR_VERSION_HIGH = 1;
    public const int MIN_PLUGIN_DESCRIPTOR_VERSION_LOW = 0;

    protected static IDictionary<string, string> EMPTY_BUILDERS_DICTIONARY =
        new Dictionary<string, string>(0);

    #region Protected fields

    protected string _pluginFilePath = null;
    protected string _name = null;
    protected Guid _pluginId;
    protected string _copyright = null;
    protected string _author = null;
    protected string _description = null;
    protected string _version = null;
    protected bool _autoActivate = false;
    protected ICollection<Guid> _dependsOn = new List<Guid>();
    protected ICollection<Guid> _conflictsWith = new List<Guid>();
    protected string _stateTrackerClassName = null;
    protected ICollection<string> _assemblyFilePaths = new List<string>();
    protected IDictionary<string, string> _builders = null;
    protected ICollection<PluginItemMetadata> _itemsMetadata = new List<PluginItemMetadata>();

    #endregion

    #region Ctor

    public PluginDirectoryDescriptor(string pluginDirectoryPath)
    {
      if (!Load(pluginDirectoryPath))
        throw new ArgumentException("Directory '" + pluginDirectoryPath + "' doesn't contain a valid plugin descriptor file");
    }

    #endregion

    /// <summary>
    /// Loads the plugin descriptor file (plugin.xml) from a plugin directory.
    /// </summary>
    /// <param name="pluginDirectoryPath">Root directory path of the plugin to load the metadata.</param>
    /// <returns><c>true</c>, if the plugin descriptor could successfully be loaded, else <c>false</c>.
    /// </returns>
    protected bool Load(string pluginDirectoryPath)
    {
      string path = Path.Combine(pluginDirectoryPath, PLUGIN_META_FILE);
      if (!File.Exists(path))
        return false;
      _pluginFilePath = path;
      try
      {
        XmlDocument doc = new XmlDocument();
        doc.Load(_pluginFilePath);
        XmlElement descriptorElement = doc.DocumentElement;
        if (descriptorElement == null || descriptorElement.Name != "Plugin")
          throw new ArgumentException(
              "File is no plugin descriptor file (document element must be 'Plugin')");

        bool versionOk = false;
        bool pluginIdSet = false;
        foreach (XmlAttribute attr in descriptorElement.Attributes)
        {
          switch (attr.Name)
          {
            case "DescriptorVersion":
              StringUtils.CheckVersionEG(attr.Value, MIN_PLUGIN_DESCRIPTOR_VERSION_HIGH, MIN_PLUGIN_DESCRIPTOR_VERSION_LOW);
              //string specVersion = attr.Value; <- if needed
              versionOk = true;
              break;
            case "Name":
              _name = attr.Value;
              break;
            case "PluginId":
              _pluginId = new Guid(attr.Value);
              pluginIdSet = true;
              break;
            case "Author":
              _author = attr.Value;
              break;
            case "Copyright":
              _copyright = attr.Value;
              break;
            case "Description":
              _description = attr.Value;
              break;
            case "PluginVersion":
              _version = attr.Value;
              break;
            case "AutoActivate":
              _autoActivate = Boolean.Parse(attr.Value);
              break;
            default:
              throw new ArgumentException("'Plugin' element doesn't support an attribute '" + attr.Name + "'");
          }
        }
        if (!versionOk)
          throw new ArgumentException("'Version' attribute not found");

        if (!pluginIdSet)
          throw new ArgumentException("'PluginId' attribute not found");

        foreach (XmlNode child in descriptorElement.ChildNodes)
        {
          XmlElement childElement = child as XmlElement;
          if (childElement == null)
            continue;
          switch (childElement.Name)
          {
            case "Runtime":
              ParseRuntimeElement(childElement, pluginDirectoryPath);
              break;
            case "Builder":
              if (_builders == null)
                _builders = new Dictionary<string, string>();
              _builders.Add(ParseBuilderElement(childElement));
              break;
            case "Register":
              CollectionUtils.AddAll(_itemsMetadata, ParseRegisterElement(childElement));
              break;
            case "DependsOn":
              CollectionUtils.AddAll(_dependsOn, ParsePluginIdEnumeration(childElement));
              break;
            case "ConflictsWith":
              CollectionUtils.AddAll(_conflictsWith, ParsePluginIdEnumeration(childElement));
              break;
            default:
              throw new ArgumentException("'Plugin' element doesn't support a child element '" + child.Name + "'");
          }
        }
      }
      catch (Exception e)
      {
        ServiceScope.Get<ILogger>().Error("Error parsing plugin descriptor file '" + _pluginFilePath + "'", e);
        return false;
      }
      return true;
    }

    #region Parsing methods

    /// <summary>
    /// Processes the <i>Runtime</i> sub element of the <i>Plugin</i> element.
    /// </summary>
    /// <param name="runtimeElement">Runtime element.</param>
    /// <param name="pluginDirectory">Root directory path of the plugin whose metadata is to be parsed.</param>
    protected void ParseRuntimeElement(XmlElement runtimeElement, string pluginDirectory)
    {
      if (runtimeElement.HasAttributes)
        throw new ArgumentException("'Runtime' element mustn't contain any attributes");
      foreach (XmlNode child in runtimeElement.ChildNodes)
      {
        XmlElement childElement = child as XmlElement;
        if (childElement == null)
          continue;
        switch (childElement.Name)
        {
          case "Assembly":
            string fileName = childElement.GetAttribute("FileName");
            if (fileName.Length == 0)
              throw new ArgumentException("'Assembly' element needs an attribute 'FileName'");
            string assemblyFilePath = Path.IsPathRooted(fileName) ? fileName : Path.Combine(pluginDirectory, fileName);
            if (!File.Exists(assemblyFilePath))
              throw new ArgumentException(string.Format("Plugin '{0}': Assembly DLL file '{1}' does not exist", _name, assemblyFilePath));
            _assemblyFilePaths.Add(assemblyFilePath);
            break;
          case "PluginStateTracker":
            _stateTrackerClassName = childElement.GetAttribute("ClassName");
            if (_stateTrackerClassName.Length == 0)
              throw new ArgumentException("'PluginStateTracker' element needs an attribute 'ClassName'");
            break;
          default:
            throw new ArgumentException("'Runtime' element doesn't support a child element '" + childElement.Name + "'");
        }
      }
    }

    /// <summary>
    /// Processes the <i>Builder</i> sub element of the <i>Plugin</i> element.
    /// </summary>
    /// <param name="builderElement">Builder element.</param>
    /// <returns>Parsed builder - name to classname mapping.</returns>
    protected static KeyValuePair<string, string> ParseBuilderElement(XmlElement builderElement)
    {
      string name = null;
      string className = null;
      foreach (XmlAttribute attr in builderElement.Attributes)
      {
        switch (attr.Name)
        {
          case "Name":
            name = attr.Value;
            break;
          case "ClassName":
            className = attr.Value;
            break;
          default:
            throw new ArgumentException("'Builder' element doesn't support an attribute '" + attr.Name + "'");
        }
      }
      if (name == null)
        throw new ArgumentException("'Builder' element needs an attribute 'Name'");
      if (className == null)
        throw new ArgumentException("'Builder' element needs an attribute 'ClassName'");
      if (builderElement.ChildNodes.Count > 0)
        throw new ArgumentException("'Builder' element doesn't support child nodes");
      return new KeyValuePair<string, string>(name, className);
    }

    /// <summary>
    /// Processes the <i>Register</i> sub element of the <i>Plugin</i> element.
    /// </summary>
    /// <param name="registerElement">Register element.</param>
    /// <returns>Metadata structures of all registered items in the given element.</returns>
    protected static IEnumerable<PluginItemMetadata> ParseRegisterElement(XmlElement registerElement)
    {
      string location = null;
      foreach (XmlAttribute attr in registerElement.Attributes)
      {
        switch (attr.Name)
        {
          case "Location":
            location = attr.Value;
            break;
          default:
            throw new ArgumentException("'Register' element doesn't support an attribute '" + attr.Name + "'");
        }
      }
      if (location == null)
        throw new ArgumentException("'Register' element needs an attribute 'Location'");
      foreach (XmlNode child in registerElement.ChildNodes)
      {
        string id = null;
        bool redundant = false;
        XmlElement childElement = child as XmlElement;
        if (childElement == null)
          continue;
        IDictionary<string, string> attributes = new Dictionary<string, string>();
        string builderName = childElement.Name;
        foreach (XmlAttribute attr in childElement.Attributes)
        {
          switch (attr.Name)
          {
            case "Id":
              id = attr.Value;
              break;
            case "Redundant":
              redundant = bool.Parse(attr.Value);
              break;
            default:
              attributes.Add(attr.Name, attr.Value);
              break;
          }
        }
        if (id == null)
          throw new ArgumentException("'Id' attribute has to be given for plugin item '" + childElement.Name + "'");
        yield return new PluginItemMetadata(location, builderName, id, redundant, attributes);
      }
    }

    /// <summary>
    /// Processes an element containing a collection of <i>&lt;PluginReference PluginId="..."/&gt;</i> sub elements and
    /// returns an enumeration of the referenced ids.
    /// </summary>
    /// <param name="enumElement">Element containing the &lt;PluginReference PluginId="..."/&gt; sub elements.</param>
    /// <returns>Enumeration of parsed plugin ids.</returns>
    protected static IEnumerable<Guid> ParsePluginIdEnumeration(XmlElement enumElement)
    {
      if (enumElement.HasAttributes)
        throw new ArgumentException(string.Format("'{0}' element mustn't contain any attributes", enumElement.Name));
      foreach (XmlNode child in enumElement.ChildNodes)
      {
        XmlElement childElement = child as XmlElement;
        if (childElement == null)
          continue;
        switch (childElement.Name)
        {
          case "PluginReference":
            string id = null;
            foreach (XmlAttribute attr in childElement.Attributes)
            {
              switch (attr.Name)
              {
                case "PluginId":
                  id = attr.Value;
                  break;
                default:
                  throw new ArgumentException("'PluginReference' sub element doesn't support an attribute '" + attr.Name + "'");
              }
            }
            if (id == null)
              throw new ArgumentException("'PluginReference' sub element needs an attribute 'PluginId'");
            yield return new Guid(id);
            break;
          default:
            throw new ArgumentException("'" + enumElement.Name + "' element doesn't support a child element '" + child.Name + "'");
        }
      }
    }

    #endregion

    #region IPluginMetadata implementation

    public string Name
    {
      get { return _name; }
    }

    public Guid PluginId
    {
      get { return _pluginId; }
    }

    public string Copyright
    {
      get { return _copyright; }
    }

    public string Author
    {
      get { return _author; }
    }

    public string Description
    {
      get { return _description; }
    }

    public string PluginVersion
    {
      get { return _version; }
    }

    public bool AutoActivate
    {
      get { return _autoActivate; }
    }

    public ICollection<Guid> DependsOn
    {
      get { return _dependsOn; }
    }

    public ICollection<Guid> ConflictsWith
    {
      get { return _conflictsWith; }
    }

    public ICollection<string> AssemblyFilePaths
    {
      get { return _assemblyFilePaths; }
    }

    public string StateTrackerClassName
    {
      get { return _stateTrackerClassName; }
    }

    public IDictionary<string, string> Builders
    {
      get { return _builders ?? EMPTY_BUILDERS_DICTIONARY; }
    }

    public ICollection<PluginItemMetadata> PluginItemsMetadata
    {
      get { return _itemsMetadata; }
    }

    public ICollection<string> GetNecessaryBuilders()
    {
      ICollection<string> result = new List<string>();
      foreach (PluginItemMetadata itemMetadata in _itemsMetadata)
        result.Add(itemMetadata.BuilderName);
      return result;
    }

    public string GetAbsolutePath(string relativePath)
    {
      return relativePath == null ? null :
          Path.Combine(Path.GetDirectoryName(_pluginFilePath), relativePath);
    }

    #endregion

    #region Base overrides

    public override string ToString()
    {
      return string.Format("Plugin '{0}'", _name);
    }

    #endregion
  }
}
