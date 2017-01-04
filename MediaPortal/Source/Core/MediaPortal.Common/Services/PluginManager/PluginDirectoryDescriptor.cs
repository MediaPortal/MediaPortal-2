#region Copyright (C) 2007-2017 Team MediaPortal

/*
    Copyright (C) 2007-2017 Team MediaPortal
    http://www.team-mediaportal.com

    This file is part of MediaPortal 2

    MediaPortal 2 is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    MediaPortal 2 is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with MediaPortal 2. If not, see <http://www.gnu.org/licenses/>.
*/

#endregion

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.XPath;
using MediaPortal.Common.Logging;
using MediaPortal.Common.PluginManager;
using MediaPortal.Utilities;

namespace MediaPortal.Common.Services.PluginManager
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
    protected DateTime _releaseDate = DateTime.MinValue;
    protected int _currentAPI = -1;
    protected int _minCompatibleAPI = -1;
    protected bool _autoActivate = false;
    protected IList<PluginDependencyInfo> _dependsOn = new List<PluginDependencyInfo>();
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
        using (Stream pluginFileStream = File.OpenRead(_pluginFilePath))
        {
          XPathDocument doc = new XPathDocument(pluginFileStream);
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
                case "Version":
                  ParseVersionElement(childNav.Clone());
                  break;
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
                  CollectionUtils.AddAll(_dependsOn, ParsePluginDependencies(childNav.Clone()));
                  break;
                case "ConflictsWith":
                  CollectionUtils.AddAll(_conflictsWith, ParsePluginIdEnumeration(childNav.Clone()));
                  break;
                default:
                  throw new ArgumentException("'Plugin' element doesn't support a child element '" + childNav.Name + "'");
              }
            } while (childNav.MoveToNext(XPathNodeType.Element));
        }
      }
      catch (Exception e)
      {
        ServiceRegistration.Get<ILogger>().Error("Error parsing plugin descriptor file '" + _pluginFilePath + "'", e);
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

    /// <summary>
    /// Processes an element containing a collection of <i>&lt;PluginReference PluginId="..."/&gt;</i> sub elements and
    /// returns an enumeration of the referenced ids.
    /// </summary>
    /// <param name="enumNavigator">XPath navigator pointing to an element containing the &lt;PluginReference PluginId="..."/&gt;
    /// sub elements.</param>
    /// <returns>Enumeration of parsed plugin ids.</returns>
    protected static IEnumerable<PluginDependencyInfo> ParsePluginDependencies(XPathNavigator enumNavigator)
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
              Guid? id = null;
              int compatibleAPI = -1;
              XPathNavigator attrNav = childNav.Clone();
              if (attrNav.MoveToFirstAttribute())
                do
                {
                  switch (attrNav.Name)
                  {
                    case "PluginId":
                      id = Guid.Parse(attrNav.Value);
                      break;
                    case "CompatibleAPI":
                      compatibleAPI = int.Parse(attrNav.Value);
                      break;
                    default:
                      throw new ArgumentException("'PluginReference' sub element doesn't support an attribute '" + attrNav.Name + "'");
                  }
                } while (attrNav.MoveToNextAttribute());
              if (id == null)
                throw new ArgumentException("'PluginReference' sub element needs an attribute 'PluginId'");
              if (compatibleAPI <= 0)
                throw new ArgumentException("'PluginReference' sub element needs an attribute 'CompatibleAPI'");
              yield return new PluginDependencyInfo(id.Value, compatibleAPI);
              break;
            case "CoreDependency":
              string name = null;
              int compatibleCoreAPI = -1;
              XPathNavigator attrNavCoreDep = childNav.Clone();
              if (attrNavCoreDep.MoveToFirstAttribute())
                do
                {
                  switch (attrNavCoreDep.Name)
                  {
                    case "Name":
                      name = attrNavCoreDep.Value;
                      break;
                    case "CompatibleAPI":
                      compatibleCoreAPI = int.Parse(attrNavCoreDep.Value);
                      break;
                    default:
                      throw new ArgumentException("'CoreDependency' sub element doesn't support an attribute '" + attrNavCoreDep.Name + "'");
                  }
                } while (attrNavCoreDep.MoveToNextAttribute());
              if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("'CoreDependency' sub element needs an attribute 'Name'");
              if (compatibleCoreAPI <= 0)
                throw new ArgumentException("'CoreDependency' sub element needs an attribute 'CompatibleAPI'");
              yield return new PluginDependencyInfo(name, compatibleCoreAPI);
              break;
            default:
              throw new ArgumentException("'" + enumNavigator.Name + "' element doesn't support a child element '" + childNav.Name + "'");
          }
        } while (childNav.MoveToNext(XPathNodeType.Element));
    }

    protected void ParseVersionElement(XPathNavigator versionNavigator)
    {
      XPathNavigator attrNav = versionNavigator.Clone();
      if (attrNav.MoveToFirstAttribute())
        do
        {
          switch (attrNav.Name)
          {
            case "PluginVersion":
              _version = attrNav.Value;
              break;
            case "ReleaseDate":
              _releaseDate = DateTime.ParseExact(attrNav.Value, "yyyy-MM-dd HH:mm:ss \"GMT\"zzz", System.Globalization.CultureInfo.InvariantCulture);
              break;
            case "CurrentAPI":
              _currentAPI = int.Parse(attrNav.Value);
              break;
            case "MinCompatibleAPI":
              _minCompatibleAPI = int.Parse(attrNav.Value);
              break;
            default:
              throw new ArgumentException("'Version' element doesn't support an attribute '" + attrNav.Name + "'");
          }
        } while (attrNav.MoveToNextAttribute());
      if (string.IsNullOrWhiteSpace(_version))
        throw new ArgumentException("'Version' sub element needs an attribute 'PluginVersion'");
      if (_releaseDate == DateTime.MinValue)
        throw new ArgumentException("'Version' sub element needs an attribute 'ReleaseDate'");
      if (_currentAPI <= 0)
        throw new ArgumentException("'Version' sub element needs an attribute 'CurrentAPI'");
      if (_minCompatibleAPI > _currentAPI)
        throw new ArgumentException("'Version' sub element's attribute 'MinCompatibleAPI' can't have a higher value than 'CurrentAPI'");
      if (_minCompatibleAPI <= 0)
        _minCompatibleAPI = _currentAPI;
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

    public DateTime ReleaseDate
    {
      get { return _releaseDate; }
    }

    public int CurrentAPI
    {
      get { return _currentAPI; }
    }

    public int MinCompatibleAPI
    {
      get { return _minCompatibleAPI; }
    }

    public bool AutoActivate
    {
      get { return _autoActivate; }
    }

    public IList<PluginDependencyInfo> DependsOn
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
      return _itemsMetadata.Select(itemMetadata => itemMetadata.BuilderName).ToList();
    }

    public string GetAbsolutePath(string relativePath)
    {
      return relativePath == null ? null : Path.Combine(Path.GetDirectoryName(_pluginFilePath), relativePath);
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
