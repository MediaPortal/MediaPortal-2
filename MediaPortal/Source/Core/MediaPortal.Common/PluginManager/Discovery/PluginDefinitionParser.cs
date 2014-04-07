#region Copyright (C) 2007-2014 Team MediaPortal

/*
    Copyright (C) 2007-2014 Team MediaPortal
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
using System.Xml.XPath;
using MediaPortal.Common.General;
using MediaPortal.Common.Logging;
using MediaPortal.Common.PluginManager.Models;
using MediaPortal.Utilities;

namespace MediaPortal.Common.PluginManager.Discovery
{
  /// <summary>
  /// Class providing extension methods for reading static plugin metadata information from plugin.xml files.
  /// </summary>
  public static class PluginDefinitionParserExtensions
  {
    #region Constants
    public const string PLUGIN_META_FILE = "plugin.xml";
    public const int PLUGIN_DESCRIPTOR_VERSION_MAJOR = 1;
    public const int MIN_PLUGIN_DESCRIPTOR_VERSION_MINOR = 0;
    #endregion

    #region Path Helpers
    private static string PluginDefinitionFilePath( this string pluginDirectoryPath )
    {
      return Path.Combine( pluginDirectoryPath, PLUGIN_META_FILE );
    }

    private static void VerifyIsPluginDirectory( this string path )
    {
      var pathExists = Directory.Exists( path );
      if( !pathExists )
        throw new ArgumentException("Directory '" + path + "' does not exist.");

      var definitionFileExists = File.Exists( path.PluginDefinitionFilePath() );
      if( !definitionFileExists )
        throw new ArgumentException("Directory '" + path + "' does not have a plugin descriptor file (plugin.xml).");
    }
    #endregion

    #region TryParsePluginDefinition
    public static bool TryParsePluginDefinition( this string pluginDirectoryPath, out PluginMetadata pluginMetadata )
    {
      pluginDirectoryPath.VerifyIsPluginDirectory();
      try
      {
        var data = File.ReadAllBytes( pluginDirectoryPath.PluginDefinitionFilePath() );
        using( var stream = new MemoryStream( data ) )
        {
          return stream.TryParsePluginDefinition( pluginDirectoryPath, out pluginMetadata );
        }
      }
      catch( IOException )
      {
        // TODO log error here?
        throw;
      }
    }

    /// <summary>
    /// Parses the plugin descriptor file (plugin.xml) and returns true if successful and false otherwise.
    /// Metadata collected during a successful parse is returned in the PluginModel output parameter.
    /// </summary>
    /// <param name="pluginDirectoryPath">The absolute path to the plugin directory containing the plugin 
    /// definition file.</param>
    /// <returns><c>true</c>, if the plugin descriptor file was successfully loaded and parsed, else 
    /// <c>false</c>.</returns>
    public static bool TryParsePluginDefinition( this Stream pluginDefinition, string pluginDirectoryPath, out PluginMetadata pluginMetadata )
    {
      var model = new PluginMetadata();
      model.SourceInfo = new PluginSourceInfo( pluginDirectoryPath );
      try
      {
        var doc = new XPathDocument( pluginDefinition );
        XPathNavigator nav = doc.CreateNavigator();
        nav.MoveToChild(XPathNodeType.Element);
        if (nav.LocalName != "Plugin")
          throw new ArgumentException( "File is no plugin descriptor file (document element must be 'Plugin')");

        bool versionOk = false;
        bool pluginIdSet = false;
        XPathNavigator attrNav = nav.Clone();
        if (attrNav.MoveToFirstAttribute())
        {
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
                model.Name = attrNav.Value;
                break;
              case "PluginId":
                model.PluginId = new Guid(attrNav.Value);
                pluginIdSet = true;
                break;
              case "Author":
                model.Author = attrNav.Value;
                break;
              case "Copyright":
                model.Copyright = attrNav.Value;
                break;
              case "Description":
                model.Description = attrNav.Value;
                break;
              case "AutoActivate":
                if (model.ActivationInfo == null)
                  model.ActivationInfo = new PluginActivationInfo();
                model.ActivationInfo.AutoActivate = Boolean.Parse(attrNav.Value);
                break;
              default:
                throw new ArgumentException("'Plugin' element doesn't support an attribute '" + attrNav.Name + "'");
            }
          } while (attrNav.MoveToNextAttribute());
        }
        if (!versionOk)
          throw new ArgumentException("'Version' attribute not found");
        if (!pluginIdSet)
          throw new ArgumentException("'PluginId' attribute not found");

        XPathNavigator childNav = nav.Clone();
        if (childNav.MoveToChild(XPathNodeType.Element))
        {
          do
          {
            switch (childNav.LocalName)
            {
              case "Version":
                ParseVersionElement(childNav.Clone(), model);
                break;
              case "Runtime":
                ParseRuntimeElement(childNav.Clone(), model);
                break;
              case "Builder":
                if (model.ActivationInfo == null)
                  model.ActivationInfo = new PluginActivationInfo();
                model.ActivationInfo.Builders.Add( ParseBuilderElement( childNav.Clone() ) );
                break;
              case "Register":
                if (model.ActivationInfo == null)
                  model.ActivationInfo = new PluginActivationInfo();
                ParseRegisterElement(childNav.Clone()).ForEach( model.ActivationInfo.Items.Add );
                break;
              case "DependsOn":
                if (model.DependencyInfo == null)
                  model.DependencyInfo = new PluginDependencyInfo( model.PluginId );
                ParsePluginDependencies(childNav.Clone()).ForEach( model.DependencyInfo.DependsOn.Add );
                break;
              case "ConflictsWith":
                if (model.DependencyInfo == null)
                  model.DependencyInfo = new PluginDependencyInfo( model.PluginId );
                ParsePluginIdEnumeration(childNav.Clone()).ForEach( model.DependencyInfo.ConflictsWith.Add );
                break;
              default:
                throw new ArgumentException("'Plugin' element doesn't support a child element '" + childNav.Name + "'");
            }
          } while (childNav.MoveToNext(XPathNodeType.Element));
        }
      }
      catch (Exception e)
      {
        ServiceRegistration.Get<ILogger>().Error("Error parsing plugin descriptor file in directory '" + model.SourceInfo.PluginPath + "'", e);
        pluginMetadata = null;
        return false;
      }
      pluginMetadata = model;
      return true;
    }
    #endregion

    #region Parsing helper methods
    /// <summary>
    /// Processes the <i>Runtime</i> sub element of the <i>Plugin</i> element.
    /// </summary>
    /// <param name="runtimeNavigator">XPath navigator pointing to the <c>Runtime</c> element.</param>
    /// <param name="metadata">Model for collecting metadata from the current parse operation.</param>
    private static void ParseRuntimeElement(XPathNavigator runtimeNavigator, PluginMetadata metadata)
    {
      if (runtimeNavigator.HasAttributes)
        throw new ArgumentException("'Runtime' element mustn't contain any attributes");
      if( metadata.ActivationInfo == null )
        metadata.ActivationInfo = new PluginActivationInfo();

      XPathNavigator childNav = runtimeNavigator.Clone();
      if (childNav.MoveToChild(XPathNodeType.Element))
      {
        do
        {
          switch (childNav.LocalName)
          {
            case "Assembly":
              string fileName = childNav.GetAttribute("FileName", string.Empty);
              if (fileName.Length == 0)
                throw new ArgumentException("'Assembly' element needs an attribute 'FileName'");
              string assemblyFilePath = Path.IsPathRooted(fileName) ? fileName : Path.Combine(metadata.SourceInfo.PluginPath, fileName);
              if (!File.Exists(assemblyFilePath))
                throw new ArgumentException(string.Format("Plugin '{0}': Assembly DLL file '{1}' does not exist", metadata.Name, assemblyFilePath));
              metadata.ActivationInfo.Assemblies.Add(assemblyFilePath);
              break;
            case "PluginStateTracker":
              var stateTrackerClassName = childNav.GetAttribute("ClassName", string.Empty);
              if (stateTrackerClassName.Length == 0)
                throw new ArgumentException("'PluginStateTracker' element needs an attribute 'ClassName'");
              metadata.ActivationInfo.StateTrackerClassName = stateTrackerClassName;
              break;
            default:
              throw new ArgumentException("'Runtime' element doesn't support a child element '" + childNav.Name + "'");
          }
        } while (childNav.MoveToNext(XPathNodeType.Element));
      }
    }

    /// <summary>
    /// Processes the <i>Builder</i> sub element of the <i>Plugin</i> element.
    /// </summary>
    /// <param name="builderNavigator">XPath navigator pointing to the <c>Builder</c> element.</param>
    /// <returns>Parsed builder - name to classname mapping.</returns>
    private static KeyValuePair<string, string> ParseBuilderElement(XPathNavigator builderNavigator)
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
    private static IEnumerable<PluginItemMetadata> ParseRegisterElement(XPathNavigator registerNavigator)
    {
      string location = null;
      XPathNavigator attrNav = registerNavigator.Clone();
      if (attrNav.MoveToFirstAttribute())
      {
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
      }
      if (location == null)
        throw new ArgumentException("'Register' element needs an attribute 'Location'");

      XPathNavigator childNav = registerNavigator.Clone();
      if (childNav.MoveToChild(XPathNodeType.Element))
      {
        do
        {
          string id = null;
          bool redundant = false;
          IDictionary<string, string> attributes = new Dictionary<string, string>();
          string builderName = childNav.LocalName;
          attrNav = childNav.Clone();
          if (attrNav.MoveToFirstAttribute())
          {
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
          }
          if (id == null)
            throw new ArgumentException("'Id' attribute has to be given for plugin item '" + childNav.Name + "'");
          yield return new PluginItemMetadata(location, builderName, id, redundant, attributes);
        } while (childNav.MoveToNext(XPathNodeType.Element));
      }
    }

    /// <summary>
    /// Processes an element containing a collection of <i>&lt;PluginReference PluginId="..."/&gt;</i> sub elements and
    /// returns an enumeration of the referenced ids.
    /// </summary>
    /// <param name="enumNavigator">XPath navigator pointing to an element containing the &lt;PluginReference PluginId="..."/&gt;
    /// sub elements.</param>
    /// <returns>Enumeration of parsed plugin ids.</returns>
    private static IEnumerable<Guid> ParsePluginIdEnumeration(XPathNavigator enumNavigator)
    {
      if (enumNavigator.HasAttributes)
        throw new ArgumentException(string.Format("'{0}' element mustn't contain any attributes", enumNavigator.Name));
      XPathNavigator childNav = enumNavigator.Clone();
      if( !childNav.MoveToChild( XPathNodeType.Element ) ) 
        yield break;
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
    private static IEnumerable<PluginDependency> ParsePluginDependencies(XPathNavigator enumNavigator)
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
              int compatibleApi = -1;
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
                      compatibleApi = int.Parse(attrNav.Value);
                      break;
                    default:
                      throw new ArgumentException("'PluginReference' sub element doesn't support an attribute '" + attrNav.Name + "'");
                  }
                } while (attrNav.MoveToNextAttribute());
              if (id == null)
                throw new ArgumentException("'PluginReference' sub element needs an attribute 'PluginId'");
              if (compatibleApi <= 0)
                throw new ArgumentException("'PluginReference' sub element needs an attribute 'CompatibleAPI'");
              yield return new PluginDependency(id.Value, compatibleApi);
              break;
            case "CoreDependency":
              string name = null;
              int compatibleCoreApi = -1;
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
                      compatibleCoreApi = int.Parse(attrNavCoreDep.Value);
                      break;
                    default:
                      throw new ArgumentException("'CoreDependency' sub element doesn't support an attribute '" + attrNavCoreDep.Name + "'");
                  }
                } while (attrNavCoreDep.MoveToNextAttribute());
              if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("'CoreDependency' sub element needs an attribute 'Name'");
              if (compatibleCoreApi <= 0)
                throw new ArgumentException("'CoreDependency' sub element needs an attribute 'CompatibleAPI'");
              yield return new PluginDependency(name, compatibleCoreApi);
              break;
            default:
              throw new ArgumentException("'" + enumNavigator.Name + "' element doesn't support a child element '" + childNav.Name + "'");
          }
        } while (childNav.MoveToNext(XPathNodeType.Element));
    }

    private static void ParseVersionElement(XPathNavigator versionNavigator, PluginMetadata metadata)
    {
      XPathNavigator attrNav = versionNavigator.Clone();
      string version = null;
      DateTime releaseDate = DateTime.MinValue;
      int currentApi = 0;
      int minCompatibleApi = 0;
      // parse
      if (attrNav.MoveToFirstAttribute())
      {
        do
        {
          switch (attrNav.Name)
          {
            case "PluginVersion":
              version = attrNav.Value;
              break;
            case "ReleaseDate":
              releaseDate = DateTime.ParseExact(attrNav.Value, "yyyy-MM-dd HH:mm:ss \"GMT\"zzz", System.Globalization.CultureInfo.InvariantCulture);
              break;
            case "CurrentAPI":
              currentApi = int.Parse(attrNav.Value);
              break;
            case "MinCompatibleAPI":
              minCompatibleApi = int.Parse(attrNav.Value);
              break;
            default:
              throw new ArgumentException("'Version' element doesn't support an attribute '" + attrNav.Name + "'");
          }
        } while (attrNav.MoveToNextAttribute());
      }
      // verify
      if (string.IsNullOrWhiteSpace(version))
        throw new ArgumentException("'Version' sub element needs an attribute 'PluginVersion'");
      if (releaseDate == DateTime.MinValue)
        throw new ArgumentException("'Version' sub element needs an attribute 'ReleaseDate'");
      if (currentApi <= 0)
        throw new ArgumentException("'Version' sub element needs an attribute 'CurrentAPI'");
      if (minCompatibleApi > currentApi)
        throw new ArgumentException("'Version' sub element's attribute 'MinCompatibleAPI' can't have a higher value than 'CurrentAPI'");
      if (minCompatibleApi <= 0)
        minCompatibleApi = currentApi;
      // all good, assign values to model
      metadata.PluginVersion = version;
      metadata.ReleaseDate = releaseDate;
      if( metadata.DependencyInfo == null )
        metadata.DependencyInfo = new PluginDependencyInfo( metadata.PluginId );
      metadata.DependencyInfo.CurrentApi = currentApi;
      metadata.DependencyInfo.MinCompatibleApi = minCompatibleApi;
    }
    #endregion
  }
}
