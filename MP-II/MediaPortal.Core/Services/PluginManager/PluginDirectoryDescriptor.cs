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

    public const int MIN_PLUGIN_DESCRIPTOR_VERSION_HIGH = 1;
    public const int MIN_PLUGIN_DESCRIPTOR_VERSION_LOW = 0;

    #region Protected fields

    protected FileInfo _pluginFile = null;
    protected string _name = null;
    protected string _copyright = null;
    protected string _author = null;
    protected string _description = null;
    protected string _version = null;
    protected bool _autoActivate;
    protected ICollection<string> _dependsOn = new List<string>();
    protected ICollection<string> _conflictsWith = new List<string>();
    protected string _stateTrackerClassName = null;
    protected ICollection<FileInfo> _assemblyFiles = new List<FileInfo>();
    protected IDictionary<string, string> _builders = new Dictionary<string, string>();
    protected ICollection<PluginItemMetadata> _itemRegistrations = new List<PluginItemMetadata>();

    #endregion

    #region Ctor

    public PluginDirectoryDescriptor(DirectoryInfo pluginDirectory)
    {
      if (!Load(pluginDirectory))
        throw new ArgumentException("Directory '" + pluginDirectory.FullName + "' doesn't contain a valid plugin descriptor file");
    }

    #endregion

    /// <summary>
    /// Loads the plugin descriptor file (plugin.xml) from a plugin directory.
    /// </summary>
    /// <param name="pluginDirectory">Root directory of the plugin to load the metadata.</param>
    /// <returns><c>true</c>, if the plugin descriptor could successfully be loaded, else <c>false</c>.
    /// </returns>
    protected bool Load(DirectoryInfo pluginDirectory)
    {
      FileInfo[] res = pluginDirectory.GetFiles(PLUGIN_META_FILE);
      if (res.Length != 1)
        return false;
      _pluginFile = res[0];
      try
      {
        XmlDocument doc = new XmlDocument();
        using (FileStream fs = _pluginFile.OpenRead())
          doc.Load(fs);
        XmlElement descriptorElement = doc.DocumentElement;
        if (descriptorElement.Name != "Plugin")
          throw new ArgumentException("File is no plugin descriptor (needs to contain a 'Plugin' element)");

        bool versionOk = false;
        foreach (XmlAttribute attr in descriptorElement.Attributes)
        {
          switch (attr.Name)
          {
            case "Version":
              StringUtils.CheckVersionEG(attr.Value, MIN_PLUGIN_DESCRIPTOR_VERSION_HIGH, MIN_PLUGIN_DESCRIPTOR_VERSION_LOW);
              //string specVersion = attr.Value; <- if needed
              versionOk = true;
              break;
            case "Name":
              _name = attr.Value;
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
              throw new ArgumentException("'Plugin' element doesn't define an attribute '" + attr.Name + "'");
          }
        }
        if (!versionOk)
          throw new ArgumentException("'Version' attribute expected");

        foreach (XmlNode child in descriptorElement.ChildNodes)
        {
          XmlElement childElement = child as XmlElement;
          if (childElement == null)
            continue;
          switch (childElement.Name)
          {
            case "Builder":
              ParseBuilderElement(childElement);
              break;
            case "Runtime":
              ParseRuntimeElement(childElement, pluginDirectory);
              break;
            case "Register":
              ParseRegisterElement(childElement);
              break;
            case "DependsOn":
              CollectionUtils.AddAll(_dependsOn, ParsePluginNameEnumeration(childElement));
              break;
            case "ConflictsWith":
              CollectionUtils.AddAll(_conflictsWith, ParsePluginNameEnumeration(childElement));
              break;
            default:
              throw new ArgumentException("'Plugin' element doesn't define a child element '" + child.Name + "'");
          }
        }
      }
      catch (Exception e)
      {
        ServiceScope.Get<ILogger>().Error("Error parsing plugin descriptor file '" + _pluginFile.FullName + "'", e);
        return false;
      }
      return true;
    }

    /// <summary>
    /// Processes the <i>Runtime</i> sub element of the <i>Plugin</i> element.
    /// </summary>
    /// <param name="runtimeElement">Runtime element.</param>
    /// <param name="pluginDirectory">Root directory of the plugin whose metadata is to be parsed.</param>
    protected void ParseRuntimeElement(XmlElement runtimeElement, DirectoryInfo pluginDirectory)
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
            FileInfo fi = new FileInfo(string.Format(@"{0}\{1}", pluginDirectory.FullName, fileName));
            if (!fi.Exists)
              throw new ArgumentException(string.Format("Plugin '{0}': Assembly DLL file '{1}' does not exist", _name, fi.FullName));
            _assemblyFiles.Add(fi);
            break;
          case "PluginStateTracker":
            _stateTrackerClassName = childElement.GetAttribute("ClassName");
            if (_stateTrackerClassName.Length == 0)
              throw new ArgumentException("'PluginStateTracker' element needs an attribute 'ClassName'");
            break;
          default:
            throw new ArgumentException("'Runtime' element doesn't define a child element '" + child.Name + "'");
        }
      }
    }

    /// <summary>
    /// Processes the <i>Builder</i> sub element of the <i>Plugin</i> element.
    /// </summary>
    /// <param name="builderElement">Builder element.</param>
    protected void ParseBuilderElement(XmlElement builderElement)
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
            throw new ArgumentException("'Builder' element doesn't define an attribute '" + attr.Name + "'");
        }
      }
      if (name == null)
        throw new ArgumentException("'Builder' element needs an attribute 'Name'");
      if (className == null)
        throw new ArgumentException("'Builder' element needs an attribute 'ClassName'");
      if (builderElement.ChildNodes.Count > 0)
        throw new ArgumentException("'Builder' element doesn't support child nodes");
      _builders.Add(name, className);
    }

    /// <summary>
    /// Processes the <i>Register</i> sub element of the <i>Plugin</i> element.
    /// </summary>
    /// <param name="registerElement">Register element.</param>
    protected void ParseRegisterElement(XmlElement registerElement)
    {
      string location = null;
      string id = null;
      foreach (XmlAttribute attr in registerElement.Attributes)
      {
        switch (attr.Name)
        {
          case "Location":
            location = attr.Value;
            break;
          default:
            throw new ArgumentException("'Register' element doesn't define an attribute '" + attr.Name + "'");
        }
      }
      if (location == null)
        throw new ArgumentException("'Register' element needs an attribute 'Location'");
      foreach (XmlNode child in registerElement.ChildNodes)
      {
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
            default:
              attributes.Add(attr.Name, attr.Value);
              break;
          }
        }
        if (id == null)
          throw new ArgumentException("'Id' attribute has to be given for plugin item '" + childElement.Name + "'");
        _itemRegistrations.Add(new PluginItemMetadata(location, builderName, id, attributes));
      }
    }

    /// <summary>
    /// Processes an element containing a collection of <i>&lt;PluginReference Name="..."/&gt;</i> sub elements and
    /// returns an enumeration of the names.
    /// </summary>
    /// <param name="enumElement">Element containing the &lt;PluginReference Name="..."/&gt; sub elements.</param>
    /// <returns>Enumeration of parsed plugin names.</returns>
    protected static IEnumerable<string> ParsePluginNameEnumeration(XmlElement enumElement)
    {
      if (enumElement.HasAttributes)
        throw new ArgumentException(string.Format("'{0}' element mustn't contain any attributes", enumElement.Name));
      ICollection<string> result = new List<string>();
      foreach (XmlNode child in enumElement.ChildNodes)
      {
        XmlElement childElement = child as XmlElement;
        if (childElement == null)
          continue;
        switch (childElement.Name)
        {
          case "PluginReference":
            string name = null;
            foreach (XmlAttribute attr in childElement.Attributes)
            {
              switch (attr.Name)
              {
                case "Name":
                  name = attr.Value;
                  break;
                default:
                  throw new ArgumentException("'PluginReference' sub element doesn't define an attribute '" + attr.Name + "'");
              }
            }
            if (name == null)
              throw new ArgumentException("'PluginReference' sub element needs an attribute 'Name'");
            result.Add(name);
            break;
          default:
            throw new ArgumentException("'" + enumElement.Name + "' doesn't define a child element '" + child.Name + "'");
        }
      }
      return result;
    }

    #region IPluginMetadata implementation

    public string Name
    {
      get { return _name; }
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

    public ICollection<string> DependsOn
    {
      get { return _dependsOn; }
    }

    public ICollection<string> ConflictsWith
    {
      get { return _conflictsWith; }
    }

    public ICollection<FileInfo> AssemblyFiles
    {
      get { return _assemblyFiles; }
    }

    public string StateTrackerClassName
    {
      get { return _stateTrackerClassName; }
    }

    public IDictionary<string, string> Builders
    {
      get { return _builders; }
    }

    public ICollection<PluginItemMetadata> PluginItemRegistrations
    {
      get { return _itemRegistrations; }
    }

    public ICollection<string> GetNecessaryBuilders()
    {
      ICollection<string> result = new List<string>();
      foreach (PluginItemMetadata itemMetadata in _itemRegistrations)
        result.Add(itemMetadata.BuilderName);
      return result;
    }

    public string GetAbsolutePath(string relativePath)
    {
      return Path.Combine(_pluginFile.Directory.FullName, relativePath);
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
