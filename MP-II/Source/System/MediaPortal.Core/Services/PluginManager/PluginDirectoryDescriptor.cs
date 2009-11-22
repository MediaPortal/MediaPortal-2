#region Copyright (C) 2007-2009 Team MediaPortal

/*
 *  Copyright (C) 2007-2009 Team MediaPortal
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
using System.Xml.XPath;
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

    public const int PLUGIN_DESCRIPTOR_VERSION_MAJOR = 1;
    public const int MIN_PLUGIN_DESCRIPTOR_VERSION_MINOR = 0;

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
        XPathDocument doc = new XPathDocument(_pluginFilePath);
        XPathNavigator nav = doc.CreateNavigator();
        nav.MoveToChild(XPathNodeType.Element);
        if (nav.LocalName != "Plugin")
          throw new ArgumentException(
              "File is no plugin descriptor file (document element must be 'Plugin')");

        bool versionOk = false;
        bool pluginIdSet = false;
        XPathNavigator attrNav = nav.Clone();
        if (attrNav.MoveToFirstAttribute())
          do
          {
            switch (attrNav.Name)
            {
              case "DescriptorVersion":
                Versions.CheckVersionCompatible(attrNav.Value, PLUGIN_DESCRIPTOR_VERSION_MAJOR, MIN_PLUGIN_DESCRIPTOR_VERSION_MINOR);
                //string specVersion = attr.Value; <- if needed
                versionOk = true;
                break;
              case "Name":
                _name = attrNav.Value;
                break;
              case "PluginId":
                _pluginId = new Guid(attrNav.Value);
                pluginIdSet = true;
                break;
              case "Author":
                _author = attrNav.Value;
                break;
              case "Copyright":
                _copyright = attrNav.Value;
                break;
              case "Description":
                _description = attrNav.Value;
                break;
              case "PluginVersion":
                _version = attrNav.Value;
                break;
              case "AutoActivate":
                _autoActivate = Boolean.Parse(attrNav.Value);
                break;
              default:
                throw new ArgumentException("'Plugin' element doesn't support an attribute '" + attrNav.Name + "'");
            }
          } while (attrNav.MoveToNextAttribute());
        if (!versionOk)
          throw new ArgumentException("'Version' attribute not found");

        if (!pluginIdSet)
          throw new ArgumentException("'PluginId' attribute not found");

        XPathNavigator childNav = nav.Clone();
        if (childNav.MoveToChild(XPathNodeType.Element))
          do
          {
            switch (childNav.LocalName)
            {
              case "Runtime":
                ParseRuntimeElement(childNav.Clone(), pluginDirectoryPath);
                break;
              case "Builder":
                if (_builders == null)
                  _builders = new Dictionary<string, string>();
                _builders.Add(ParseBuilderElement(childNav.Clone()));
                break;
              case "Register":
                CollectionUtils.AddAll(_itemsMetadata, ParseRegisterElement(childNav.Clone()));
                break;
              case "DependsOn":
                CollectionUtils.AddAll(_dependsOn, ParsePluginIdEnumeration(childNav.Clone()));
                break;
              case "ConflictsWith":
                CollectionUtils.AddAll(_conflictsWith, ParsePluginIdEnumeration(childNav.Clone()));
                break;
              default:
                throw new ArgumentException("'Plugin' element doesn't support a child element '" + childNav.Name + "'");
            }
          } while (childNav.MoveToNext(XPathNodeType.Element));
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
    /// <param name="runtimeNavigator">XPath navigator pointing to the <c>Runtime</c> element.</param>
    /// <param name="pluginDirectory">Root directory path of the plugin whose metadata is to be parsed.</param>
    protected void ParseRuntimeElement(XPathNavigator runtimeNavigator, string pluginDirectory)
    {
      if (runtimeNavigator.HasAttributes)
        throw new ArgumentException("'Runtime' element mustn't contain any attributes");
      XPathNavigator childNav = runtimeNavigator.Clone();
      if (childNav.MoveToChild(XPathNodeType.Element))
        do
        {
          switch (childNav.LocalName)
          {
            case "Assembly":
              string fileName = childNav.GetAttribute("FileName", string.Empty);
              if (fileName.Length == 0)
                throw new ArgumentException("'Assembly' element needs an attribute 'FileName'");
              string assemblyFilePath = Path.IsPathRooted(fileName) ? fileName : Path.Combine(pluginDirectory, fileName);
              if (!File.Exists(assemblyFilePath))
                throw new ArgumentException(string.Format("Plugin '{0}': Assembly DLL file '{1}' does not exist", _name, assemblyFilePath));
              _assemblyFilePaths.Add(assemblyFilePath);
              break;
            case "PluginStateTracker":
              _stateTrackerClassName = childNav.GetAttribute("ClassName", string.Empty);
              if (_stateTrackerClassName.Length == 0)
                throw new ArgumentException("'PluginStateTracker' element needs an attribute 'ClassName'");
              break;
            default:
              throw new ArgumentException("'Runtime' element doesn't support a child element '" + childNav.Name + "'");
          }
        } while (childNav.MoveToNext(XPathNodeType.Element));
    }

    /// <summary>
    /// Processes the <i>Builder</i> sub element of the <i>Plugin</i> element.
    /// </summary>
    /// <param name="builderNavigator">XPath navigator pointing to the <c>Builder</c> element.</param>
    /// <returns>Parsed builder - name to classname mapping.</returns>
    protected static KeyValuePair<string, string> ParseBuilderElement(XPathNavigator builderNavigator)
    {
      string name = null;
      string className = null;
      XPathNavigator attrNav = builderNavigator.Clone();
      if (attrNav.MoveToFirstAttribute())
        do
        {
          switch (attrNav.Name)
          {
            case "Name":
              name = attrNav.Value;
              break;
            case "ClassName":
              className = attrNav.Value;
              break;
            default:
              throw new ArgumentException("'Builder' element doesn't support an attribute '" + attrNav.Name + "'");
          }
        } while (attrNav.MoveToNextAttribute());
      if (name == null)
        throw new ArgumentException("'Builder' element needs an attribute 'Name'");
      if (className == null)
        throw new ArgumentException("'Builder' element needs an attribute 'ClassName'");
      if (builderNavigator.SelectChildren(XPathNodeType.Element).Count > 0)
        throw new ArgumentException("'Builder' element doesn't support child nodes");
      return new KeyValuePair<string, string>(name, className);
    }

    /// <summary>
    /// Processes the <i>Register</i> sub element of the <i>Plugin</i> element.
    /// </summary>
    /// <param name="registerNavigator">XPath navigator pointing to the <c>Register</c> element.</param>
    /// <returns>Metadata structures of all registered items in the given element.</returns>
    protected static IEnumerable<PluginItemMetadata> ParseRegisterElement(XPathNavigator registerNavigator)
    {
      string location = null;
      XPathNavigator attrNav = registerNavigator.Clone();
      if (attrNav.MoveToFirstAttribute())
      do
        {
          switch (attrNav.Name)
          {
            case "Location":
              location = attrNav.Value;
              break;
            default:
              throw new ArgumentException("'Register' element doesn't support an attribute '" + attrNav.Name + "'");
          }
        } while (attrNav.MoveToNextAttribute());
      if (location == null)
        throw new ArgumentException("'Register' element needs an attribute 'Location'");
      XPathNavigator childNav = registerNavigator.Clone();
      if (childNav.MoveToChild(XPathNodeType.Element))
        do
        {
          string id = null;
          bool redundant = false;
          IDictionary<string, string> attributes = new Dictionary<string, string>();
          string builderName = childNav.LocalName;
          attrNav = childNav.Clone();
          if (attrNav.MoveToFirstAttribute())
            do
            {
              switch (attrNav.Name)
              {
                case "Id":
                  id = attrNav.Value;
                  break;
                case "Redundant":
                  redundant = bool.Parse(attrNav.Value);
                  break;
                default:
                  attributes.Add(attrNav.Name, attrNav.Value);
                  break;
              }
            } while (attrNav.MoveToNextAttribute());
          if (id == null)
            throw new ArgumentException("'Id' attribute has to be given for plugin item '" + childNav.Name + "'");
          yield return new PluginItemMetadata(location, builderName, id, redundant, attributes);
        } while (childNav.MoveToNext(XPathNodeType.Element));
    }

    /// <summary>
    /// Processes an element containing a collection of <i>&lt;PluginReference PluginId="..."/&gt;</i> sub elements and
    /// returns an enumeration of the referenced ids.
    /// </summary>
    /// <param name="enumNavigator">XPath navigator pointing to an element containing the &lt;PluginReference PluginId="..."/&gt;
    /// sub elements.</param>
    /// <returns>Enumeration of parsed plugin ids.</returns>
    protected static IEnumerable<Guid> ParsePluginIdEnumeration(XPathNavigator enumNavigator)
    {
      if (enumNavigator.HasAttributes)
        throw new ArgumentException(string.Format("'{0}' element mustn't contain any attributes", enumNavigator.Name));
      XPathNavigator childNav = enumNavigator.Clone();
      if (childNav.MoveToChild(XPathNodeType.Element))
        do
        {
          switch (childNav.LocalName)
          {
            case "PluginReference":
              string id = null;
              XPathNavigator attrNav = childNav.Clone();
              if (attrNav.MoveToFirstAttribute())
                do
                {
                  switch (attrNav.Name)
                  {
                    case "PluginId":
                      id = attrNav.Value;
                      break;
                    default:
                      throw new ArgumentException("'PluginReference' sub element doesn't support an attribute '" + attrNav.Name + "'");
                  }
                } while (attrNav.MoveToNextAttribute());
              if (id == null)
                throw new ArgumentException("'PluginReference' sub element needs an attribute 'PluginId'");
              yield return new Guid(id);
              break;
            default:
              throw new ArgumentException("'" + enumNavigator.Name + "' element doesn't support a child element '" + childNav.Name + "'");
          }
        } while (childNav.MoveToNext(XPathNodeType.Element));
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
