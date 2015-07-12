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
using System.Linq;
using System.Xml.Linq;
using MediaPortal.Common.PluginManager.Models;

namespace MediaPortal.PackageCore.Package
{
  /// <summary>
  /// Release related package meta data.
  /// </summary>
  public class PackageReleaseMetaData
  {
    #region private fields

    private static Dictionary<string, Type> _actionTypes;

    #endregion

    #region ctor

    /// <summary>
    /// Creates a new release meta data item from an XML element.
    /// </summary>
    /// <param name="xRoot">Release meta data XML element.</param>
    public PackageReleaseMetaData(XElement xRoot)
    {
      // set defaults
      ReleaseNotes = String.Empty;
      Version = String.Empty;
      InstallActions = new PackageActionCollection();
      UpdateActions = new PackageActionCollection();
      RemoveActions = new PackageActionCollection();

      foreach (var xAttribute in xRoot.Attributes())
      {
        switch (xAttribute.Name.LocalName)
        {
          case "Version":
            Version = xAttribute.Value;
            break;

          case "ReleaseNotes":
            ReleaseNotes = xAttribute.Value;
            break;

          default:
            throw new PackageParseException(
              String.Format("The attribute '{0}' is not supported for ReleaseInfo", xAttribute.Name.LocalName),
              xAttribute);
        }
      }

      // parse actions
      InstallActions = ParseActions(xRoot, PackageInstallType.Install);
      UpdateActions = ParseActions(xRoot, PackageInstallType.Update);
      RemoveActions = ParseActions(xRoot, PackageInstallType.Remove);

      // parse remaining elements
      foreach (var xElement in xRoot.Elements())
      {
        switch (xElement.Name.LocalName)
        {
          case "InstallActions":
          case "UpdateActions":
          case "UninstallActions": // TODO: remove uninstall
          case "RemoveActions":
            // already parsed
            break;

          default:
            throw new PackageParseException(
              String.Format("The element '{0}' is not supported for ReleaseInfo", xElement.Name.LocalName),
              xElement);
        }
      }
    }

    #endregion

    #region public properties

    /// <summary>
    /// Gets the version of the release
    /// </summary>
    public string Version { get; private set; }

    /// <summary>
    /// Gets the release notes of the release.
    /// </summary>
    public string ReleaseNotes { get; private set; }

    /// <summary>
    /// Gets the collection with all install actions.
    /// </summary>
    public PackageActionCollection InstallActions { get; private set; }

    /// <summary>
    /// Gets the collection with all update actions.
    /// </summary>
    public PackageActionCollection UpdateActions { get; private set; }

    /// <summary>
    /// Gets the collection with all remove actions.
    /// </summary>
    public PackageActionCollection RemoveActions { get; private set; }

    #endregion

    #region public methods

    /// <summary>
    /// Checks if the meta data is valid
    /// </summary>
    /// <param name="packageRoot">Package containing the meta data.</param>
    /// <param name="message">Is set to the error message if the data is not valid.</param>
    /// <returns>Returns <c>true</c> if the data is valid; else <c>false</c>.</returns>
    public bool CheckValid(PackageRoot packageRoot, out string message)
    {
      if (Version == null)
      {
        message = "Version must be specified";
        return false;
      }
      foreach (var action in InstallActions)
      {
        if (!action.CheckValid(packageRoot, out message))
        {
          message = String.Format("Install action {0}: {1}", action.Description ?? action.GetType().Name, message);
          return false;
        }
      }
      foreach (var action in UpdateActions)
      {
        if (!action.CheckValid(packageRoot, out message))
        {
          message = String.Format("Update action {0}: {1}", action.Description ?? action.GetType().Name, message);
          return false;
        }
      }
      foreach (var action in RemoveActions)
      {
        if (!action.CheckValid(packageRoot, out message))
        {
          message = String.Format("Remove action {0}: {1}", action.Description ?? action.GetType().Name, message);
          return false;
        }
      }
      message = null;
      return true;
    }

    /// <summary>
    /// Parses the actions of a specific install type.
    /// </summary>
    /// <param name="xRoot">Release meta data XML element.</param>
    /// <param name="installType">Install type.</param>
    /// <returns>Returns a collection with the actions.</returns>
    public static PackageActionCollection ParseActions(XElement xRoot, PackageInstallType installType)
    {
      XElement xActions;
      bool setOverwrite = false;
      switch (installType)
      {
        case PackageInstallType.Install:
          xActions = xRoot.Element("InstallActions") ?? new XElement("InstallActions", new XElement("Action", new XAttribute("Type", "CopyPackageDirectories")));
          break;

        case PackageInstallType.Update:
          xActions = xRoot.Element("UpdateAction") ?? new XElement("UpdateActions", new XAttribute("UseInstallActions", "true"));
          if (Boolean.Parse((string) xActions.Attribute("UseInstallActions") ?? "false"))
          {
            xActions = xRoot.Element("InstallActions") ?? new XElement("InstallActions", new XElement("Action", new XAttribute("Type", "CopyPackageDirectories")));
            setOverwrite = true;
          }
          break;

        case PackageInstallType.Remove: // TODO: remove uninstall element name
          xActions = xRoot.Element("RemoveActions") ?? xRoot.Element("UninstallActions") ?? new XElement("RemoveActions", new XElement("Action", new XAttribute("Type", "DeletePackageDirectories")));
          break;

        default:
          throw new ArgumentException(String.Format("Unsupported install type: {0}", installType), "installType");
      }

      if (_actionTypes == null)
      {
        RegisterActionTypes();
      }

      var actions = new PackageActionCollection();
      foreach (var xAction in xActions.Elements("Action"))
      {
        actions.Add(CreateActionInstance(xAction, setOverwrite));
      }
      return actions;
    }

    /// <summary>
    /// Creates an instance of a action by its XML element.
    /// </summary>
    /// <param name="xAction">Action XML element.</param>
    /// <param name="setOverwrite"><c>true</c> if the action should be set to overwrite always instead of using an parameter to do so.</param>
    /// <returns>Returns the instance of the action.</returns>
    public static PackageAction CreateActionInstance(XElement xAction, bool setOverwrite)
    {
      var typeName = (string) xAction.Attribute("Type");
      if (String.IsNullOrEmpty(typeName))
      {
        throw new PackageParseException("Action type must be specified", xAction);
      }

      Type type;
      if (!_actionTypes.TryGetValue(typeName, out type))
      {
        throw new PackageParseException(String.Format("Type action type {0} is unknown", typeName), xAction);
      }

      var constructor = type.GetConstructor(new Type[0]);
      if (constructor == null)
      {
        throw new InvalidOperationException(String.Format("The package action {0} does not have an parameterless public constructor!", type.FullName));
      }
      var action = constructor.Invoke(new object[0]) as PackageAction;
      if (action == null)
      {
        // this should in fact never happen
        throw new InvalidOperationException(String.Format("The package action {0} is no package action", type.FullName));
      }
      action.ParseParameters(xAction, setOverwrite);
      return action;
    }

    /// <summary>
    /// Fills missing properties from the actual plugin meta data
    /// </summary>
    /// <param name="pluginMetadata">Plugin meta data.</param>
    public void FillMissingMetadata(PluginMetadata pluginMetadata)
    {
      if (String.IsNullOrEmpty(Version))
        Version = pluginMetadata.PluginVersion;
    }

    /// <summary>
    /// Checks if the property values matches the data in an plugin meta data.
    /// </summary>
    /// <param name="pluginMetadata">Plugin meta data.</param>
    public void CheckMetadataMismatch(PluginMetadata pluginMetadata)
    {
      if (!String.Equals(Version, pluginMetadata.PluginVersion))
        throw new PackageParseException("The Version of the main plugin and the PluginInfo.xml file does not match");
    }

    #endregion

    #region private methods

    /// <summary>
    /// Parses the assembly for all non abstract sub classes of <see cref="PackageAction"/> 
    /// that have the <see cref="PackageActionAttribute"/> attribute.
    /// </summary>
    private static void RegisterActionTypes()
    {
      if (_actionTypes != null)
        return;
      _actionTypes = new Dictionary<string, Type>();

      foreach (var type in typeof(PackageRoot).Assembly.GetTypes())
      {
        if (type.IsSubclassOf(typeof(PackageAction)) && !type.IsAbstract)
        {
          var attr = type.GetCustomAttributes(typeof(PackageActionAttribute), false).FirstOrDefault() as PackageActionAttribute;
          if (attr != null)
          {
            _actionTypes.Add(attr.Type, type);
          }
        }
      }
    }

    #endregion
  }
}